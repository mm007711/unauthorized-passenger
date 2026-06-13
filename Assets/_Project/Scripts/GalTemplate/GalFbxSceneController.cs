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
        if (prefab != null)
        {
            GameObject importedReference = Instantiate(prefab, sceneRoot.transform);
            importedReference.name = "Imported FBX Reference (Hidden)";
            importedReference.SetActive(false);
            Debug.Log("GAL FBX scene loaded: " + resourcePath);
        }
        else
        {
            Debug.LogWarning("GAL FBX scene missing Resources asset: " + resourcePath);
        }

        Bounds sceneBounds = new Bounds(new Vector3(0f, 1.25f, 0f), new Vector3(4.4f, 2.5f, 9.2f));
        BuildPassengerCabin(sceneBounds);

        GameObject cameraObject = new GameObject("Passenger View Camera");
        cameraObject.transform.SetParent(sceneRoot.transform, false);
        sceneCamera = cameraObject.AddComponent<Camera>();
        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = new Color(0.015f, 0.018f, 0.035f, 1f);
        sceneCamera.fieldOfView = 48f;
        sceneCamera.depth = 100f;
        sceneCamera.nearClipPlane = 0.03f;
        sceneCamera.farClipPlane = 250f;
        ConfigurePassengerCamera(cameraObject.transform, sceneBounds);

        PixelateImageEffect pixelate = cameraObject.AddComponent<PixelateImageEffect>();
        pixelate.pixelSize = pixelSize;

        AddLight("Magenta Cabin Light", new Vector3(-0.8f, 2.35f, -1.8f), new Color(1f, 0.42f, 0.92f, 1f), 2.2f);
        AddLight("Forward Violet Light", new Vector3(0.5f, 1.75f, 2.5f), new Color(0.7f, 0.58f, 1f, 1f), 1.45f);
        AddLight("Window Blue Light", new Vector3(2.1f, 1.8f, -0.6f), new Color(0.3f, 0.55f, 0.95f, 1f), 1.15f);
        RenderSettings.ambientLight = new Color(0.12f, 0.08f, 0.2f, 1f);
    }

    private static void ConfigurePassengerCamera(Transform cameraTransform, Bounds bounds)
    {
        Vector3 cameraPosition = new Vector3(0.08f, 1.22f, bounds.center.z - bounds.extents.z * 0.82f);
        Vector3 lookTarget = new Vector3(0.05f, 1.06f, bounds.center.z + bounds.extents.z * 0.38f);
        cameraTransform.position = cameraPosition;
        cameraTransform.rotation = Quaternion.LookRotation((lookTarget - cameraPosition).normalized, Vector3.up);
        Debug.Log("GAL FBX passenger camera bounds=" + bounds + " position=" + cameraPosition + " target=" + lookTarget);
    }

    private void BuildPassengerCabin(Bounds bounds)
    {
        Material aisleMaterial = CreateSceneMaterial("Aisle Violet", new Color(0.46f, 0.29f, 0.62f, 1f), new Color(0.08f, 0.02f, 0.1f, 1f));
        Material seatMaterial = CreateSceneMaterial("Deep Seat Fabric", new Color(0.055f, 0.055f, 0.13f, 1f), new Color(0.01f, 0.005f, 0.025f, 1f));
        Material seatSideMaterial = CreateSceneMaterial("Seat Rim Highlight", new Color(0.16f, 0.12f, 0.26f, 1f), new Color(0.04f, 0.01f, 0.06f, 1f));
        Material wallMaterial = CreateSceneMaterial("Cool Wall Panel", new Color(0.15f, 0.2f, 0.28f, 1f), new Color(0.015f, 0.025f, 0.04f, 1f));
        Material windowMaterial = CreateSceneMaterial("Muted Window", new Color(0.24f, 0.36f, 0.48f, 1f), new Color(0.02f, 0.06f, 0.1f, 1f));
        Material poleMaterial = CreateSceneMaterial("Yellow Handrail", new Color(0.9f, 0.68f, 0.23f, 1f), new Color(0.16f, 0.08f, 0.02f, 1f));
        Material glowMaterial = CreateSceneMaterial("Pink Light Strip", new Color(0.72f, 0.36f, 0.9f, 1f), new Color(0.35f, 0.08f, 0.45f, 1f));
        Material frontMaterial = CreateSceneMaterial("Forward Bulkhead", new Color(0.33f, 0.2f, 0.42f, 1f), new Color(0.08f, 0.02f, 0.08f, 1f));

        float floorY = bounds.min.y + 0.03f;
        CreateCabinBox("Aisle Floor", new Vector3(0f, floorY, 0.05f), new Vector3(1.08f, 0.06f, 8.4f), aisleMaterial);
        CreateCabinBox("Left Raised Floor", new Vector3(-1.48f, floorY + 0.12f, 0.02f), new Vector3(1.05f, 0.22f, 8.2f), seatSideMaterial);
        CreateCabinBox("Right Raised Floor", new Vector3(1.48f, floorY + 0.12f, 0.02f), new Vector3(1.05f, 0.22f, 8.2f), seatSideMaterial);

        CreateCabinBox("Left Wall", new Vector3(-2.17f, 1.25f, 0f), new Vector3(0.08f, 2.2f, 8.8f), wallMaterial);
        CreateCabinBox("Right Wall", new Vector3(2.17f, 1.25f, 0f), new Vector3(0.08f, 2.2f, 8.8f), wallMaterial);
        CreateCabinBox("Right Window Band", new Vector3(2.12f, 1.78f, -0.15f), new Vector3(0.08f, 0.62f, 7.8f), windowMaterial);
        CreateCabinBox("Left Window Band", new Vector3(-2.12f, 1.78f, -0.15f), new Vector3(0.08f, 0.46f, 7.8f), windowMaterial);
        CreateCabinBox("Ceiling", new Vector3(0f, 2.46f, 0f), new Vector3(4.35f, 0.08f, 8.9f), wallMaterial);
        CreateCabinBox("Center Pink Light", new Vector3(0f, 2.38f, -0.2f), new Vector3(0.16f, 0.05f, 7.6f), glowMaterial);

        float[] rows = { -3.15f, -2.25f, -1.35f, -0.45f, 0.45f, 1.35f, 2.25f };
        foreach (float z in rows)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 1.32f;
                CreateCabinBox("Seat Back " + z + " " + side, new Vector3(x, 0.98f, z + 0.24f), new Vector3(0.72f, 1.15f, 0.18f), seatMaterial);
                CreateCabinBox("Seat Cushion " + z + " " + side, new Vector3(x, 0.48f, z - 0.08f), new Vector3(0.76f, 0.18f, 0.58f), seatSideMaterial);
                CreateCabinBox("Seat Side Shadow " + z + " " + side, new Vector3(side * 0.78f, 0.72f, z), new Vector3(0.08f, 0.64f, 0.5f), seatMaterial);
            }
        }

        CreateCabinBox("Left Foreground Seat", new Vector3(-1.55f, 0.95f, -4.05f), new Vector3(1.05f, 1.35f, 0.24f), seatMaterial);
        CreateCabinBox("Right Foreground Seat", new Vector3(1.55f, 0.95f, -4.05f), new Vector3(1.05f, 1.35f, 0.24f), seatMaterial);
        CreateCabinBox("Forward Bulkhead", new Vector3(0f, 0.95f, 3.75f), new Vector3(1.28f, 1.1f, 0.24f), frontMaterial);
        CreateCabinBox("Forward Glow Panel", new Vector3(0f, 1.45f, 3.62f), new Vector3(0.82f, 0.36f, 0.08f), glowMaterial);

        CreateCabinCylinder("Front Yellow Pole", new Vector3(0.74f, 1.2f, 2.25f), new Vector3(0.035f, 1.15f, 0.035f), Quaternion.identity, poleMaterial);
        CreateCabinCylinder("Middle Yellow Pole", new Vector3(0.5f, 1.2f, 0.65f), new Vector3(0.03f, 1.05f, 0.03f), Quaternion.identity, poleMaterial);
        CreateCabinCylinder("Right Vertical Trim", new Vector3(1.92f, 1.34f, -0.4f), new Vector3(0.026f, 1.24f, 0.026f), Quaternion.identity, poleMaterial);
        CreateCabinBox("Right Wall Poster", new Vector3(2.08f, 1.08f, -2.15f), new Vector3(0.07f, 0.9f, 0.55f), frontMaterial);
    }

    private static Material CreateSceneMaterial(string materialName, Color color, Color emission)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Diffuse");
        }

        Material material = new Material(shader);
        material.name = materialName;
        if (material.HasProperty("_Color"))
        {
            Color finalColor = color + emission * 0.35f;
            finalColor.a = color.a;
            material.color = finalColor;
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        return material;
    }

    private void CreateCabinBox(string objectName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(sceneRoot.transform, false);
        box.transform.localPosition = position;
        box.transform.localScale = scale;

        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private void CreateCabinCylinder(string objectName, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = objectName;
        cylinder.transform.SetParent(sceneRoot.transform, false);
        cylinder.transform.localPosition = position;
        cylinder.transform.localRotation = rotation;
        cylinder.transform.localScale = scale;

        Renderer renderer = cylinder.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
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
