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

        StartCoroutine(EnterRoutine(string.IsNullOrEmpty(resourcePath) ? DefaultResourcePath : resourcePath, pixelSize <= 0f ? 7f : pixelSize, onBlackout));
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
        overlayCanvas.gameObject.SetActive(false);
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
        GameObject model;
        if (prefab != null)
        {
            model = Instantiate(prefab, sceneRoot.transform);
            Debug.Log("GAL FBX scene loaded: " + resourcePath);
        }
        else
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.transform.SetParent(sceneRoot.transform, false);
            model.name = "Missing FBX Placeholder";
            Debug.LogWarning("GAL FBX scene missing Resources asset: " + resourcePath);
        }

        Bounds sceneBounds = new Bounds(Vector3.zero, new Vector3(3f, 2f, 7f));
        if (model != null)
        {
            model.transform.position = Vector3.zero;
            model.transform.rotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            DisableImportedViewComponents(model);
            NormalizeModel(model, out sceneBounds);
        }

        GameObject cameraObject = new GameObject("Passenger View Camera");
        cameraObject.transform.SetParent(sceneRoot.transform, false);
        sceneCamera = cameraObject.AddComponent<Camera>();
        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = new Color(0.02f, 0.025f, 0.04f, 1f);
        sceneCamera.fieldOfView = 50f;
        sceneCamera.depth = 100f;
        sceneCamera.nearClipPlane = 0.03f;
        sceneCamera.farClipPlane = 250f;
        ConfigurePassengerCamera(cameraObject.transform, sceneBounds);

        PixelateImageEffect pixelate = cameraObject.AddComponent<PixelateImageEffect>();
        pixelate.pixelSize = pixelSize;

        AddLight("Key Light", new Vector3(-2.6f, 4f, -3.2f), new Color(0.95f, 0.7f, 1f, 1f), 2.4f);
        AddLight("Aisle Light", new Vector3(0.5f, 2.1f, 2.6f), new Color(0.65f, 0.74f, 1f, 1f), 1.15f);
        RenderSettings.ambientLight = new Color(0.25f, 0.18f, 0.33f, 1f);
    }

    private static void NormalizeModel(GameObject model, out Bounds bounds)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            bounds = new Bounds(Vector3.zero, new Vector3(3f, 2f, 7f));
            return;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxSize > 0.01f)
        {
            float scale = 7.2f / maxSize;
            model.transform.localScale *= scale;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 offset = bounds.center;
        model.transform.position -= new Vector3(offset.x, bounds.min.y, offset.z);

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
    }

    private static void ConfigurePassengerCamera(Transform cameraTransform, Bounds bounds)
    {
        Vector3 size = bounds.size;
        bool lengthIsX = size.x > size.z;
        Vector3 longAxis = lengthIsX ? Vector3.right : Vector3.forward;
        Vector3 sideAxis = lengthIsX ? Vector3.forward : Vector3.right;
        float longSize = Mathf.Max(lengthIsX ? size.x : size.z, 2f);
        float sideSize = Mathf.Max(lengthIsX ? size.z : size.x, 1f);
        float eyeHeight = bounds.min.y + Mathf.Clamp(size.y * 0.48f, 0.85f, 2.2f);

        Vector3 cameraPosition = bounds.center - longAxis * longSize * 0.43f - sideAxis * sideSize * 0.05f;
        cameraPosition.y = eyeHeight;

        Vector3 lookTarget = bounds.center + longAxis * longSize * 0.28f;
        lookTarget.y = eyeHeight + Mathf.Clamp(size.y * 0.03f, 0.02f, 0.22f);

        Vector3 direction = lookTarget - cameraPosition;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector3.forward;
        }

        cameraTransform.position = cameraPosition;
        cameraTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Debug.Log("GAL FBX passenger camera bounds=" + bounds + " position=" + cameraPosition + " target=" + lookTarget);
    }

    private static void DisableImportedViewComponents(GameObject model)
    {
        Camera[] importedCameras = model.GetComponentsInChildren<Camera>(true);
        foreach (Camera importedCamera in importedCameras)
        {
            importedCamera.enabled = false;
        }

        AudioListener[] audioListeners = model.GetComponentsInChildren<AudioListener>(true);
        foreach (AudioListener listener in audioListeners)
        {
            listener.enabled = false;
        }
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
