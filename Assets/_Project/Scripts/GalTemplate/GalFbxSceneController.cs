using System;
using System.Collections;
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

        sceneRoot = new GameObject("Runtime FBX Scene");
        DontDestroyOnLoad(sceneRoot);

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        GameObject model;
        if (prefab != null)
        {
            model = Instantiate(prefab, sceneRoot.transform);
        }
        else
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.transform.SetParent(sceneRoot.transform, false);
            model.name = "Missing FBX Placeholder";
        }

        if (model != null)
        {
            model.transform.position = Vector3.zero;
            model.transform.rotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            NormalizeModel(model);
        }

        GameObject cameraObject = new GameObject("Passenger View Camera");
        cameraObject.transform.SetParent(sceneRoot.transform, false);
        sceneCamera = cameraObject.AddComponent<Camera>();
        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = new Color(0.02f, 0.025f, 0.04f, 1f);
        sceneCamera.fieldOfView = 50f;
        sceneCamera.nearClipPlane = 0.03f;
        sceneCamera.farClipPlane = 250f;
        cameraObject.transform.position = new Vector3(0f, 1.05f, -4.6f);
        cameraObject.transform.rotation = Quaternion.Euler(7f, 0f, 0f);

        PixelateImageEffect pixelate = cameraObject.AddComponent<PixelateImageEffect>();
        pixelate.pixelSize = pixelSize;

        AddLight("Key Light", new Vector3(-2.6f, 4f, -3.2f), new Color(0.95f, 0.7f, 1f, 1f), 2.4f);
        AddLight("Aisle Light", new Vector3(0.5f, 2.1f, 2.6f), new Color(0.65f, 0.74f, 1f, 1f), 1.15f);
        RenderSettings.ambientLight = new Color(0.25f, 0.18f, 0.33f, 1f);
    }

    private static void NormalizeModel(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
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
