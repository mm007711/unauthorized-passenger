using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GalFbxSceneController : MonoBehaviour
{
    private const string DefaultResourcePath = "FbxScenes/car";

    private static GalFbxSceneController instance;

    private GameObject sceneRoot;
    private Camera sceneCamera;
    private Transform sceneCameraTransform;
    private Vector3 sceneCameraBasePosition;
    private Quaternion sceneCameraBaseRotation;
    private Vector3 sceneCameraInputOffset;
    private Vector2 sceneCameraLookOffset;
    private Vector2 sceneCameraMouseLookOffset;
    private float sceneCameraBodyYaw;
    private float sceneCameraMoveBlend;
    private float sceneCameraTurnLean;
    private float sceneCameraStepTime;
    private Canvas overlayCanvas;
    private Image blackImage;
    private bool isActive;
    private readonly List<Camera> disabledCameras = new List<Camera>();

    public static GalFbxSceneController Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject controllerObject = new GameObject("GAL FBX Scene Controller");
                DontDestroyOnLoad(controllerObject);
                instance = controllerObject.AddComponent<GalFbxSceneController>();
            }

            return instance;
        }
    }

    public bool IsActive
    {
        get { return isActive; }
    }

    public static bool IsSceneActive
    {
        get { return instance != null && instance.isActive; }
    }

    public void Enter(string resourcePath, float pixelSize, Action onBlackout = null)
    {
        if (isActive)
        {
            return;
        }

        StartCoroutine(EnterRoutine(string.IsNullOrEmpty(resourcePath) ? DefaultResourcePath : resourcePath, Mathf.Max(0f, pixelSize), onBlackout));
    }

    public void Exit(Action onBlackout = null)
    {
        if (!isActive)
        {
            return;
        }

        StartCoroutine(ExitRoutine(onBlackout));
    }

    private IEnumerator EnterRoutine(string resourcePath, float pixelSize, Action onBlackout)
    {
        isActive = true;
        EnsureBlackOverlay();
        SetBlackAlpha(0f);
        overlayCanvas.gameObject.SetActive(true);
        yield return FadeBlack(0f, 1f, 0.45f);

        onBlackout?.Invoke();
        BuildScene(resourcePath, pixelSize);
        yield return new WaitForSecondsRealtime(0.2f);
        yield return FadeBlack(1f, 0f, 0.55f);
        SetBlackAlpha(0f);
        overlayCanvas.gameObject.SetActive(false);
        Debug.Log("GAL FBX blackout hidden, alpha=" + (blackImage == null ? -1f : blackImage.color.a));
    }

    private IEnumerator ExitRoutine(Action onBlackout)
    {
        EnsureBlackOverlay();
        overlayCanvas.gameObject.SetActive(true);
        yield return FadeBlack(0f, 1f, 0.35f);
        DestroyScene();
        onBlackout?.Invoke();
        yield return FadeBlack(1f, 0f, 0.35f);
        overlayCanvas.gameObject.SetActive(false);
        isActive = false;
    }

    private void BuildScene(string resourcePath, float pixelSize)
    {
        DestroyScene();
        DisableOtherCameras();

        sceneRoot = new GameObject("Runtime FBX Scene");
        DontDestroyOnLoad(sceneRoot);

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        GameObject importedScene = null;
        if (prefab != null)
        {
            importedScene = Instantiate(prefab, sceneRoot.transform);
            importedScene.name = "Imported FBX Scene";
            importedScene.transform.localPosition = Vector3.zero;
            importedScene.transform.localRotation = Quaternion.identity;
            importedScene.transform.localScale = Vector3.one;
            Debug.Log("GAL FBX scene loaded: " + resourcePath);
        }
        else
        {
            Debug.LogWarning("GAL FBX scene missing Resources asset: " + resourcePath);
        }

        int lightCount = EnableImportedLights(importedScene);
        sceneCamera = SelectImportedCamera(importedScene);
        if (sceneCamera != null)
        {
            ConfigureImportedCamera(sceneCamera);
        }
        else
        {
            Bounds sceneBounds = GetSceneBounds(importedScene);
            sceneCamera = CreateFallbackCamera(sceneBounds);
        }

        if (sceneCamera != null && pixelSize > 1f)
        {
            PixelateImageEffect pixelate = sceneCamera.GetComponent<PixelateImageEffect>();
            if (pixelate == null)
            {
                pixelate = sceneCamera.gameObject.AddComponent<PixelateImageEffect>();
            }

            pixelate.pixelSize = pixelSize;
        }
        else if (sceneCamera != null)
        {
            PixelateImageEffect pixelate = sceneCamera.GetComponent<PixelateImageEffect>();
            if (pixelate != null)
            {
                Destroy(pixelate);
            }
        }

        if (sceneCamera != null)
        {
            sceneCameraTransform = sceneCamera.transform;
            sceneCameraBasePosition = sceneCameraTransform.position;
            sceneCameraBaseRotation = sceneCameraTransform.rotation;
            sceneCameraInputOffset = Vector3.zero;
            sceneCameraLookOffset = Vector2.zero;
            sceneCameraMouseLookOffset = Vector2.zero;
            sceneCameraBodyYaw = 0f;
            sceneCameraMoveBlend = 0f;
            sceneCameraTurnLean = 0f;
            sceneCameraStepTime = 0f;
            CabinMoodImageEffect mood = sceneCamera.GetComponent<CabinMoodImageEffect>();
            if (mood == null)
            {
                mood = sceneCamera.gameObject.AddComponent<CabinMoodImageEffect>();
            }

            mood.intensity = 0.9f;
        }

        AddReferenceMoodLights(importedScene);

        if (lightCount == 0)
        {
            AddLight("FBX Fallback Key Light", new Vector3(0f, 3.2f, -2.2f), new Color(0.95f, 0.82f, 1f, 1f), 1.4f);
            AddLight("FBX Fallback Fill Light", new Vector3(2f, 1.8f, 1.8f), new Color(0.45f, 0.65f, 1f, 1f), 0.85f);
            RenderSettings.ambientLight = new Color(0.22f, 0.2f, 0.28f, 1f);
        }
    }

    private void LateUpdate()
    {
        if (!isActive || sceneCameraTransform == null)
        {
            return;
        }

        UpdateCameraControlInput();

        float time = Time.unscaledTime;
        float step = Mathf.Sin(sceneCameraStepTime);
        float stepDouble = Mathf.Sin(sceneCameraStepTime * 2f);
        Vector3 sway = new Vector3(
            Mathf.Sin(time * 1.35f) * 0.018f,
            Mathf.Sin(time * 2.05f + 0.7f) * 0.012f,
            Mathf.Sin(time * 0.95f + 1.4f) * 0.014f);
        Vector3 walkBob = new Vector3(
            stepDouble * 0.012f,
            Mathf.Abs(step) * 0.02f,
            Mathf.Cos(sceneCameraStepTime) * 0.01f) * sceneCameraMoveBlend;
        Quaternion roll = Quaternion.Euler(
            Mathf.Sin(time * 1.2f + 0.4f) * 0.45f,
            Mathf.Sin(time * 0.85f) * 0.35f,
            Mathf.Sin(time * 1.65f + 1.1f) * 0.65f - sceneCameraTurnLean);
        Vector2 totalLook = sceneCameraLookOffset + sceneCameraMouseLookOffset;
        Quaternion bodyTurn = Quaternion.Euler(0f, sceneCameraBodyYaw, 0f);
        Quaternion manualLook = Quaternion.Euler(totalLook.y, totalLook.x, 0f);

        sceneCameraTransform.position = sceneCameraBasePosition + sceneCameraBaseRotation * (sceneCameraInputOffset + sway + walkBob);
        sceneCameraTransform.rotation = sceneCameraBaseRotation * bodyTurn * manualLook * roll;
    }

    private void UpdateCameraControlInput()
    {
        float deltaTime = Time.unscaledDeltaTime;
        float forwardInput = 0f;
        if (Input.GetKey(KeyCode.W))
        {
            forwardInput += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            forwardInput -= 1f;
        }

        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A))
        {
            horizontalInput -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            horizontalInput += 1f;
        }

        bool sidestep = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float turnInput = sidestep ? 0f : horizontalInput;
        float strafeInput = sidestep ? horizontalInput : 0f;

        sceneCameraBodyYaw += turnInput * (34f * deltaTime);
        sceneCameraBodyYaw = Mathf.Clamp(sceneCameraBodyYaw, -24f, 24f);
        sceneCameraTurnLean = Mathf.Lerp(sceneCameraTurnLean, turnInput * 1.4f, GetDampedInterpolation(8f));

        Quaternion movementYaw = Quaternion.Euler(0f, sceneCameraBodyYaw, 0f);
        Vector3 moveInput = movementYaw * new Vector3(strafeInput * 0.42f, 0f, forwardInput);
        if (Input.GetKey(KeyCode.Q))
        {
            moveInput.y -= 0.45f;
        }

        if (Input.GetKey(KeyCode.E))
        {
            moveInput.y += 0.45f;
        }

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        sceneCameraInputOffset += moveInput * (0.62f * deltaTime);
        sceneCameraInputOffset.x = Mathf.Clamp(sceneCameraInputOffset.x, -0.42f, 0.42f);
        sceneCameraInputOffset.y = Mathf.Clamp(sceneCameraInputOffset.y, -0.16f, 0.18f);
        sceneCameraInputOffset.z = Mathf.Clamp(sceneCameraInputOffset.z, -0.72f, 0.72f);

        float moveAmount = Mathf.Clamp01(Mathf.Abs(forwardInput) + Mathf.Abs(strafeInput) * 0.7f);
        sceneCameraMoveBlend = Mathf.Lerp(sceneCameraMoveBlend, moveAmount, GetDampedInterpolation(6f));
        sceneCameraStepTime += deltaTime * Mathf.Lerp(2.2f, 7.4f, sceneCameraMoveBlend);
        sceneCameraMouseLookOffset = Vector2.Lerp(sceneCameraMouseLookOffset, GetMouseLookTarget(), GetDampedInterpolation(5.5f));

        float lookX = 0f;
        float lookY = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            lookX -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            lookX += 1f;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            lookY -= 1f;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            lookY += 1f;
        }

        if (Input.GetMouseButton(1))
        {
            lookX += Input.GetAxisRaw("Mouse X") * 5.5f;
            lookY -= Input.GetAxisRaw("Mouse Y") * 4f;
        }

        Vector2 manualLookInput = new Vector2(lookX, lookY);
        if (manualLookInput.sqrMagnitude > 0.001f)
        {
            sceneCameraLookOffset += manualLookInput * (38f * deltaTime);
        }
        else
        {
            sceneCameraLookOffset = Vector2.Lerp(sceneCameraLookOffset, Vector2.zero, GetDampedInterpolation(1.8f));
        }

        sceneCameraLookOffset.x = Mathf.Clamp(sceneCameraLookOffset.x, -12f, 12f);
        sceneCameraLookOffset.y = Mathf.Clamp(sceneCameraLookOffset.y, -7f, 7f);

        if (Input.GetKeyDown(KeyCode.R))
        {
            sceneCameraInputOffset = Vector3.zero;
            sceneCameraLookOffset = Vector2.zero;
            sceneCameraMouseLookOffset = Vector2.zero;
            sceneCameraBodyYaw = 0f;
            sceneCameraMoveBlend = 0f;
            sceneCameraTurnLean = 0f;
            sceneCameraStepTime = 0f;
        }
    }

    private static Vector2 GetMouseLookTarget()
    {
        Vector3 mousePosition = Input.mousePosition;
        if (mousePosition.x < 0f || mousePosition.y < 0f || mousePosition.x > Screen.width || mousePosition.y > Screen.height)
        {
            return Vector2.zero;
        }

        float normalizedX = Mathf.Clamp((mousePosition.x / Mathf.Max(1f, Screen.width) - 0.5f) * 2f, -1f, 1f);
        float normalizedY = Mathf.Clamp((mousePosition.y / Mathf.Max(1f, Screen.height) - 0.5f) * 2f, -1f, 1f);
        normalizedX = Mathf.Sign(normalizedX) * Mathf.Pow(Mathf.Abs(normalizedX), 1.35f);
        normalizedY = Mathf.Sign(normalizedY) * Mathf.Pow(Mathf.Abs(normalizedY), 1.35f);

        return new Vector2(normalizedX * 7.5f, -normalizedY * 4.5f);
    }

    private static float GetDampedInterpolation(float speed)
    {
        return 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
    }

    private static Camera SelectImportedCamera(GameObject importedScene)
    {
        if (importedScene == null)
        {
            return null;
        }

        Camera[] cameras = importedScene.GetComponentsInChildren<Camera>(true);
        if (cameras.Length == 0)
        {
            return null;
        }

        Camera selected = cameras[0];
        foreach (Camera camera in cameras)
        {
            if (camera != null && camera.name.ToLowerInvariant().Contains("camera"))
            {
                selected = camera;
                break;
            }
        }

        foreach (Camera camera in cameras)
        {
            if (camera == null)
            {
                continue;
            }

            camera.gameObject.SetActive(true);
            camera.enabled = camera == selected;
        }

        Debug.Log("GAL FBX using imported camera: " + GetHierarchyPath(selected.transform));
        return selected;
    }

    private static void ConfigureImportedCamera(Camera camera)
    {
        if (camera == null)
        {
            return;
        }

        camera.gameObject.SetActive(true);
        camera.enabled = true;
        camera.targetTexture = null;
        camera.depth = 100f;
        camera.nearClipPlane = Mathf.Max(0.01f, camera.nearClipPlane);
        camera.farClipPlane = Mathf.Max(100f, camera.farClipPlane);
        camera.rect = new Rect(0f, 0f, 1f, 1f);
    }

    private Camera CreateFallbackCamera(Bounds bounds)
    {
        GameObject cameraObject = new GameObject("Fallback FBX Camera");
        cameraObject.transform.SetParent(sceneRoot.transform, false);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.025f, 0.04f, 1f);
        camera.fieldOfView = 50f;
        camera.depth = 100f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 250f;

        Vector3 size = bounds.size;
        bool lengthIsX = size.x > size.z;
        Vector3 longAxis = lengthIsX ? Vector3.right : Vector3.forward;
        Vector3 sideAxis = lengthIsX ? Vector3.forward : Vector3.right;
        float longSize = Mathf.Max(lengthIsX ? size.x : size.z, 2f);
        float sideSize = Mathf.Max(lengthIsX ? size.z : size.x, 1f);
        float eyeHeight = bounds.min.y + Mathf.Clamp(size.y * 0.48f, 0.85f, 2.2f);
        Vector3 cameraPosition = bounds.center - longAxis * longSize * 0.42f - sideAxis * sideSize * 0.05f;
        cameraPosition.y = eyeHeight;
        Vector3 lookTarget = bounds.center + longAxis * longSize * 0.25f;
        lookTarget.y = eyeHeight;
        cameraObject.transform.position = cameraPosition;
        cameraObject.transform.rotation = Quaternion.LookRotation((lookTarget - cameraPosition).normalized, Vector3.up);
        Debug.Log("GAL FBX fallback camera bounds=" + bounds + " position=" + cameraPosition + " target=" + lookTarget);
        return camera;
    }

    private static Bounds GetSceneBounds(GameObject importedScene)
    {
        if (importedScene == null)
        {
            return new Bounds(Vector3.zero, new Vector3(3f, 2f, 7f));
        }

        Renderer[] renderers = importedScene.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(importedScene.transform.position, new Vector3(3f, 2f, 7f));
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static int EnableImportedLights(GameObject importedScene)
    {
        if (importedScene == null)
        {
            return 0;
        }

        Light[] lights = importedScene.GetComponentsInChildren<Light>(true);
        foreach (Light light in lights)
        {
            if (light == null)
            {
                continue;
            }

            light.gameObject.SetActive(true);
            light.enabled = true;
        }

        Debug.Log("GAL FBX imported lights enabled: " + lights.Length);
        return lights.Length;
    }

    private void AddReferenceMoodLights(GameObject importedScene)
    {
        Bounds bounds = GetSceneBounds(importedScene);
        Vector3 center = bounds.center;
        float height = bounds.min.y + Mathf.Max(1.4f, bounds.size.y * 0.72f);
        float forward = Mathf.Max(bounds.size.x, bounds.size.z) * 0.24f;

        AddLight("Reference Pink Aisle Wash", center + new Vector3(-0.35f, height, -forward), new Color(1f, 0.38f, 0.9f, 1f), 1.35f);
        AddLight("Reference Violet Forward Glow", center + new Vector3(0.2f, height * 0.82f, forward), new Color(0.62f, 0.45f, 1f, 1f), 1.1f);
        AddLight("Reference Cool Window Fill", center + new Vector3(bounds.extents.x * 0.78f, height * 0.72f, 0f), new Color(0.28f, 0.45f, 0.72f, 1f), 0.75f);

        RenderSettings.ambientLight = new Color(0.16f, 0.12f, 0.24f, 1f);
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
        {
            return string.Empty;
        }

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private void DisableOtherCameras()
    {
        disabledCameras.Clear();
        Camera[] cameras = Camera.allCameras;
        foreach (Camera camera in cameras)
        {
            if (camera != null && camera.enabled)
            {
                camera.enabled = false;
                disabledCameras.Add(camera);
            }
        }
    }

    private void RestoreOtherCameras()
    {
        foreach (Camera camera in disabledCameras)
        {
            if (camera != null)
            {
                camera.enabled = true;
            }
        }

        disabledCameras.Clear();
    }

    private void AddLight(string lightName, Vector3 position, Color color, float intensity)
    {
        GameObject lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(sceneRoot.transform, false);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = 8f;
    }

    private void DestroyScene()
    {
        if (sceneRoot != null)
        {
            Destroy(sceneRoot);
            sceneRoot = null;
        }

        sceneCamera = null;
        sceneCameraTransform = null;
        RestoreOtherCameras();
    }

    private void EnsureBlackOverlay()
    {
        if (overlayCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("FBX Scene Blackout Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);
        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject blackObject = new GameObject("Black", typeof(RectTransform));
        blackObject.transform.SetParent(canvasObject.transform, false);
        RectTransform blackRect = blackObject.GetComponent<RectTransform>();
        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        blackRect.offsetMin = Vector2.zero;
        blackRect.offsetMax = Vector2.zero;
        blackImage = blackObject.AddComponent<Image>();
        blackImage.color = Color.black;
        overlayCanvas.gameObject.SetActive(false);
    }

    private IEnumerator FadeBlack(float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            SetBlackAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(time / duration)));
            yield return null;
        }

        SetBlackAlpha(to);
    }

    private void SetBlackAlpha(float alpha)
    {
        if (blackImage == null)
        {
            return;
        }

        Color color = blackImage.color;
        color.a = alpha;
        blackImage.color = color;
    }
}
