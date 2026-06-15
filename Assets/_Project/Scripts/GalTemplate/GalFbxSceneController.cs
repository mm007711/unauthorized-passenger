using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class GalFbxSceneController : MonoBehaviour
{
    private const string DefaultResourcePath = "FbxScenes/car_glb";
    private const float DefaultCameraHeightOffset = 0.18f;
    private const float MinCameraHeightOffset = -0.4f;
    private const float MaxCameraHeightOffset = 0.6f;
    private const float DefaultMoodIntensity = 0.75f;
    public const string DefaultCharacterImageId = "unauthorized_passenger";
    public const string LocalCharacterImagePrefix = "local:";
    private const string CharacterResourceFolder = "Characters";
    private const string DefaultCharacterTexturePath = CharacterResourceFolder + "/" + DefaultCharacterImageId;
    private const string CharacterShaderResourcePath = "Shaders/CharacterBillboard";
    public const float DefaultCharacterViewportX = 0.335f;
    public const float DefaultCharacterViewportY = 0.56f;
    public const float DefaultCharacterViewportDepth = 3.2f;
    public const float DefaultCharacterScreenHeight = 0.58f;
    public const float DefaultCharacterPixelSize = 1f;
    public const float DefaultCharacterMoodBlend = 0.3f;
    public const float MinCharacterViewportX = -0.35f;
    public const float MaxCharacterViewportX = 1.35f;
    public const float MinCharacterViewportY = -0.25f;
    public const float MaxCharacterViewportY = 1.35f;
    public const float MinCharacterViewportDepth = 0.45f;
    public const float MaxCharacterViewportDepth = 20f;
    public const float MinCharacterScreenHeight = 0.05f;
    public const float MaxCharacterScreenHeight = 2.2f;
    public const float MinCharacterPixelSize = 1f;
    public const float MaxCharacterPixelSize = 64f;
    private const float CharacterDepthNearScale = 1.08f;
    private const float CharacterDepthFarScale = 0.92f;

    private static readonly Color ClearAmbientLight = new Color(0.42f, 0.42f, 0.46f, 1f);
    private static readonly Color EerieAmbientLight = new Color(0.16f, 0.12f, 0.24f, 1f);

    private static GalFbxSceneController instance;

    private class MoodLightBinding
    {
        public Light light;
        public Color clearColor;
        public Color eerieColor;
        public float clearIntensity;
        public float eerieIntensity;
    }

    private GameObject sceneRoot;
    private Camera sceneCamera;
    private CabinMoodImageEffect sceneMoodEffect;
    private GameObject sceneCharacter;
    private Material sceneCharacterMaterial;
    private Texture2D sceneCharacterTexture;
    private bool sceneCharacterTextureIsRuntime;
    private Bounds sceneBounds;
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
    private float cameraHeightOffset = DefaultCameraHeightOffset;
    private float moodIntensity = DefaultMoodIntensity;
    private string characterImageId = DefaultCharacterImageId;
    private string characterTexturePath = DefaultCharacterTexturePath;
    private float characterViewportX = DefaultCharacterViewportX;
    private float characterViewportY = DefaultCharacterViewportY;
    private float characterViewportDepth = DefaultCharacterViewportDepth;
    private float characterScreenHeight = DefaultCharacterScreenHeight;
    private float characterPixelSize = DefaultCharacterPixelSize;
    private float characterMoodBlend = DefaultCharacterMoodBlend;
    private Canvas overlayCanvas;
    private Image blackImage;
    private bool isActive;
    private bool controlsEnabled = true;
    private readonly List<Camera> disabledCameras = new List<Camera>();
    private readonly List<MoodLightBinding> moodLights = new List<MoodLightBinding>();

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

    public static string GetLocalCharacterImportDirectory()
    {
        return Path.Combine(Application.persistentDataPath, "GalTemplate", "Characters");
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
    }

    public void SetCameraHeightOffset(float offset)
    {
        cameraHeightOffset = Mathf.Clamp(offset, MinCameraHeightOffset, MaxCameraHeightOffset);
    }

    public void SetMoodIntensity(float intensity)
    {
        moodIntensity = Mathf.Clamp01(intensity);
        ApplyMoodSettings();
    }

    public void SetCharacterSettings(string imageId, float viewportX, float viewportY, float viewportDepth, float screenHeight, float pixelSize, float moodBlend)
    {
        string normalizedImageId = NormalizeCharacterImageId(imageId);
        bool imageChanged = normalizedImageId != characterImageId;
        characterImageId = normalizedImageId;
        characterTexturePath = GetCharacterTexturePath(characterImageId);
        characterViewportX = Mathf.Clamp(viewportX, MinCharacterViewportX, MaxCharacterViewportX);
        characterViewportY = Mathf.Clamp(viewportY, MinCharacterViewportY, MaxCharacterViewportY);
        characterViewportDepth = Mathf.Clamp(viewportDepth, MinCharacterViewportDepth, MaxCharacterViewportDepth);
        characterScreenHeight = Mathf.Clamp(screenHeight, MinCharacterScreenHeight, MaxCharacterScreenHeight);
        characterPixelSize = Mathf.Clamp(pixelSize, MinCharacterPixelSize, MaxCharacterPixelSize);
        characterMoodBlend = Mathf.Clamp01(moodBlend);

        if (sceneCharacterMaterial != null)
        {
            sceneCharacterMaterial.SetFloat("_PixelSize", characterPixelSize);
        }

        if (sceneCharacter != null && sceneCamera != null)
        {
            if (imageChanged)
            {
                bool isRuntimeTexture;
                Texture2D texture = LoadCharacterTexture(characterImageId, out isRuntimeTexture);
                if (texture != null)
                {
                    ReleaseRuntimeCharacterTexture();
                    sceneCharacterTexture = texture;
                    sceneCharacterTextureIsRuntime = isRuntimeTexture;
                    sceneCharacterMaterial.SetTexture("_MainTex", sceneCharacterTexture);
                }
            }

            PositionSceneCharacter(sceneBounds, sceneCharacterTexture);
            ApplyCharacterMoodSettings(Mathf.Clamp01(moodIntensity));
        }
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
        sceneBounds = GetSceneBounds(importedScene);
        sceneCamera = SelectImportedCamera(importedScene);
        if (sceneCamera != null)
        {
            ConfigureImportedCamera(sceneCamera);
        }
        else
        {
            sceneCamera = CreateFallbackCamera(sceneBounds, importedScene);
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
            sceneMoodEffect = sceneCamera.GetComponent<CabinMoodImageEffect>();
            if (sceneMoodEffect == null)
            {
                sceneMoodEffect = sceneCamera.gameObject.AddComponent<CabinMoodImageEffect>();
            }

            ApplyPixelateEffect(sceneCamera, pixelSize);
            AddSceneCharacter(sceneBounds);
        }

        AddReferenceMoodLights(sceneBounds);

        if (lightCount == 0)
        {
            AddFallbackLights(sceneBounds);
        }

        ApplyMoodSettings();
    }

    private void ApplyPixelateEffect(Camera camera, float pixelSize)
    {
        if (camera == null)
        {
            return;
        }

        PixelateImageEffect pixelate = camera.GetComponent<PixelateImageEffect>();
        if (pixelSize > 1f)
        {
            if (pixelate == null)
            {
                pixelate = camera.gameObject.AddComponent<PixelateImageEffect>();
            }

            pixelate.pixelSize = pixelSize;
        }
        else if (pixelate != null)
        {
            Destroy(pixelate);
        }
    }

    private void AddSceneCharacter(Bounds bounds)
    {
        if (sceneRoot == null || sceneCamera == null)
        {
            return;
        }

        bool isRuntimeTexture;
        Texture2D characterTexture = LoadCharacterTexture(characterImageId, out isRuntimeTexture);
        if (characterTexture == null)
        {
            Debug.LogWarning("GAL FBX character texture missing Resources asset: " + characterTexturePath);
            return;
        }

        sceneCharacterTexture = characterTexture;
        sceneCharacterTextureIsRuntime = isRuntimeTexture;

        Shader shader = Resources.Load<Shader>(CharacterShaderResourcePath);
        if (shader == null)
        {
            shader = Shader.Find("GalTemplate/CharacterBillboard");
        }

        if (shader == null)
        {
            Debug.LogWarning("GAL FBX character shader missing Resources asset: " + CharacterShaderResourcePath);
            return;
        }

        sceneCharacterMaterial = new Material(shader);
        sceneCharacterMaterial.hideFlags = HideFlags.HideAndDontSave;
        sceneCharacterMaterial.SetTexture("_MainTex", characterTexture);
        sceneCharacterMaterial.SetFloat("_Opacity", 0.9f);
        sceneCharacterMaterial.SetFloat("_WhiteCutoff", 0.93f);
        sceneCharacterMaterial.SetFloat("_WhiteSoftness", 0.055f);
        sceneCharacterMaterial.SetFloat("_BlackCutoff", 0.035f);
        sceneCharacterMaterial.SetFloat("_BlackSoftness", 0.035f);
        sceneCharacterMaterial.SetFloat("_ZTest", 4f);
        sceneCharacterMaterial.SetFloat("_PixelSize", characterPixelSize);
        sceneCharacterMaterial.renderQueue = 2490;
        ApplyCharacterMoodSettings(Mathf.Clamp01(moodIntensity));

        sceneCharacter = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sceneCharacter.name = "Unauthorized Passenger Character";
        sceneCharacter.transform.SetParent(sceneRoot.transform, true);

        Collider collider = sceneCharacter.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        MeshRenderer renderer = sceneCharacter.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = sceneCharacterMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 0;
        }

        PositionSceneCharacter(bounds, characterTexture);
        Debug.Log("GAL FBX character placed at viewport=(" + characterViewportX.ToString("0.000") + ", " + characterViewportY.ToString("0.000") + ") texture=" + characterTexturePath);
    }

    private Texture2D LoadCharacterTexture(string imageId, out bool isRuntimeTexture)
    {
        isRuntimeTexture = false;
        string normalizedImageId = NormalizeCharacterImageId(imageId);
        if (IsLocalCharacterImageId(normalizedImageId))
        {
            Texture2D localTexture = LoadLocalCharacterTexture(normalizedImageId);
            if (localTexture != null)
            {
                isRuntimeTexture = true;
                characterTexturePath = normalizedImageId;
                return localTexture;
            }

            Debug.LogWarning("GAL FBX local character texture missing: " + normalizedImageId);
        }

        string resourcePath = GetCharacterTexturePath(normalizedImageId);
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null || normalizedImageId == DefaultCharacterImageId)
        {
            characterTexturePath = resourcePath;
            return texture;
        }

        texture = Resources.Load<Texture2D>(DefaultCharacterTexturePath);
        if (texture != null)
        {
            Debug.LogWarning("GAL FBX character texture missing Resources asset: " + resourcePath + ", using " + DefaultCharacterTexturePath);
            characterImageId = DefaultCharacterImageId;
            characterTexturePath = DefaultCharacterTexturePath;
        }

        return texture;
    }

    private static bool IsLocalCharacterImageId(string imageId)
    {
        return !string.IsNullOrEmpty(imageId) && imageId.StartsWith(LocalCharacterImagePrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static Texture2D LoadLocalCharacterTexture(string imageId)
    {
        string fileName = imageId.Substring(LocalCharacterImagePrefix.Length);
        string path = Path.Combine(GetLocalCharacterImportDirectory(), fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Destroy(texture);
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GAL FBX failed to load local character texture: " + path + "\n" + ex.Message);
            return null;
        }
    }

    private static string NormalizeCharacterImageId(string imageId)
    {
        if (string.IsNullOrWhiteSpace(imageId))
        {
            return DefaultCharacterImageId;
        }

        string normalized = imageId.Replace('\\', '/').Trim();
        if (normalized.StartsWith(LocalCharacterImagePrefix, StringComparison.OrdinalIgnoreCase))
        {
            string localName = normalized.Substring(LocalCharacterImagePrefix.Length).Trim();
            localName = Path.GetFileName(localName.Replace('\\', '/'));
            return string.IsNullOrEmpty(localName) ? DefaultCharacterImageId : LocalCharacterImagePrefix + localName;
        }

        int slashIndex = normalized.LastIndexOf('/');
        if (slashIndex >= 0)
        {
            normalized = normalized.Substring(slashIndex + 1);
        }

        int dotIndex = normalized.LastIndexOf('.');
        if (dotIndex > 0)
        {
            normalized = normalized.Substring(0, dotIndex);
        }

        return string.IsNullOrEmpty(normalized) ? DefaultCharacterImageId : normalized;
    }

    private static string GetCharacterTexturePath(string imageId)
    {
        string normalizedImageId = NormalizeCharacterImageId(imageId);
        if (IsLocalCharacterImageId(normalizedImageId))
        {
            return normalizedImageId;
        }

        return CharacterResourceFolder + "/" + normalizedImageId;
    }

    private void PositionSceneCharacter(Bounds bounds, Texture2D texture)
    {
        if (sceneCharacter == null || sceneCamera == null || texture == null)
        {
            return;
        }

        float minDepth = sceneCamera.nearClipPlane + 0.08f;
        float maxDepth = Mathf.Max(minDepth + 0.5f, sceneCamera.farClipPlane - 0.5f);
        float depth = Mathf.Clamp(characterViewportDepth, minDepth, maxDepth);
        float depthT = Mathf.InverseLerp(MinCharacterViewportDepth, MaxCharacterViewportDepth, depth);
        float easedDepthT = Mathf.SmoothStep(0f, 1f, depthT);

        Vector3 worldPosition = sceneCamera.ViewportToWorldPoint(new Vector3(characterViewportX, characterViewportY, depth));
        sceneCharacter.transform.position = worldPosition;

        float viewHeightAtDepth = sceneCamera.orthographic
            ? sceneCamera.orthographicSize * 2f
            : 2f * depth * Mathf.Tan(sceneCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float depthScale = Mathf.Lerp(CharacterDepthNearScale, CharacterDepthFarScale, easedDepthT);
        float characterHeight = Mathf.Max(0.01f, viewHeightAtDepth * characterScreenHeight * depthScale);
        float aspect = Mathf.Clamp(texture.width / Mathf.Max(1f, (float)texture.height), 0.35f, 1.25f);
        sceneCharacter.transform.localScale = new Vector3(characterHeight * aspect, characterHeight, 1f);
        UpdateSceneCharacterBillboard();
    }

    private void UpdateSceneCharacterBillboard()
    {
        if (sceneCharacter == null || sceneCameraTransform == null)
        {
            return;
        }

        Vector3 toCamera = sceneCameraTransform.position - sceneCharacter.transform.position;
        if (toCamera.sqrMagnitude < 0.001f)
        {
            return;
        }

        sceneCharacter.transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
    }

    private void ApplyCharacterMoodSettings(float strength)
    {
        if (sceneCharacterMaterial == null)
        {
            return;
        }

        Color characterTint = Color.Lerp(new Color(0.82f, 0.9f, 1f, 1f), new Color(0.72f, 0.56f, 1f, 1f), strength);
        sceneCharacterMaterial.SetColor("_MoodTint", characterTint);
        sceneCharacterMaterial.SetFloat("_MoodBlend", Mathf.Lerp(0.02f, 0.18f, strength) * characterMoodBlend);
        sceneCharacterMaterial.SetFloat("_MoodCounter", Mathf.Lerp(0.08f, 0.42f, strength) * characterMoodBlend);
        sceneCharacterMaterial.SetFloat("_EdgeDarkness", Mathf.Lerp(0.04f, 0.24f, strength) * characterMoodBlend);
        sceneCharacterMaterial.SetFloat("_RimStrength", Mathf.Lerp(0.03f, 0.15f, strength) * characterMoodBlend);
        sceneCharacterMaterial.SetFloat("_PixelSize", characterPixelSize);
    }

    private void LateUpdate()
    {
        if (!isActive || sceneCameraTransform == null)
        {
            return;
        }

        if (controlsEnabled)
        {
            UpdateCameraControlInput();
        }
        else
        {
            sceneCameraMoveBlend = Mathf.Lerp(sceneCameraMoveBlend, 0f, GetDampedInterpolation(7f));
            sceneCameraTurnLean = Mathf.Lerp(sceneCameraTurnLean, 0f, GetDampedInterpolation(8f));
            sceneCameraMouseLookOffset = Vector2.Lerp(sceneCameraMouseLookOffset, Vector2.zero, GetDampedInterpolation(5f));
        }

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

        sceneCameraTransform.position = sceneCameraBasePosition + Vector3.up * cameraHeightOffset + sceneCameraBaseRotation * (sceneCameraInputOffset + sway + walkBob);
        sceneCameraTransform.rotation = sceneCameraBaseRotation * bodyTurn * manualLook * roll;
        UpdateSceneCharacterBillboard();
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

    private Camera CreateFallbackCamera(Bounds bounds, GameObject importedScene)
    {
        GameObject cameraObject = new GameObject("Fallback FBX Camera");
        cameraObject.transform.SetParent(sceneRoot.transform, false);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.025f, 0.04f, 1f);
        camera.fieldOfView = 64f;
        camera.depth = 100f;
        camera.nearClipPlane = 0.02f;
        camera.farClipPlane = 250f;

        Vector3 size = bounds.size;
        bool lengthIsX = size.x > size.z;
        Vector3 longAxis = lengthIsX ? Vector3.right : Vector3.forward;
        Vector3 sideAxis = lengthIsX ? Vector3.forward : Vector3.right;
        float longSize = Mathf.Max(lengthIsX ? size.x : size.z, 2f);
        float sideSize = Mathf.Max(lengthIsX ? size.z : size.x, 1f);
        float eyeHeight = bounds.min.y + Mathf.Clamp(size.y * 0.54f, 1.45f, 2.65f);
        Vector3 cameraPosition;
        Vector3 lookTarget;

        if (TryGetVehicleInteriorCameraPose(importedScene, bounds, longAxis, sideAxis, longSize, sideSize, eyeHeight, out cameraPosition, out lookTarget))
        {
            cameraObject.transform.position = cameraPosition;
            cameraObject.transform.rotation = Quaternion.LookRotation((lookTarget - cameraPosition).normalized, Vector3.up);
            Debug.Log("GAL FBX vehicle fallback camera bounds=" + bounds + " position=" + cameraPosition + " target=" + lookTarget);
            return camera;
        }

        cameraPosition = bounds.center - longAxis * longSize * 0.34f - sideAxis * sideSize * 0.02f;
        cameraPosition.y = eyeHeight;
        lookTarget = bounds.center + longAxis * longSize * 0.22f;
        lookTarget.y = eyeHeight - 0.05f;
        cameraObject.transform.position = cameraPosition;
        cameraObject.transform.rotation = Quaternion.LookRotation((lookTarget - cameraPosition).normalized, Vector3.up);
        Debug.Log("GAL FBX fallback camera bounds=" + bounds + " position=" + cameraPosition + " target=" + lookTarget);
        return camera;
    }

    private static bool TryGetVehicleInteriorCameraPose(
        GameObject importedScene,
        Bounds bounds,
        Vector3 longAxis,
        Vector3 sideAxis,
        float longSize,
        float sideSize,
        float eyeHeight,
        out Vector3 cameraPosition,
        out Vector3 lookTarget)
    {
        cameraPosition = Vector3.zero;
        lookTarget = Vector3.zero;
        if (importedScene == null)
        {
            return false;
        }

        bool hasSteering = TryFindNamedRendererCenter(importedScene, new[] { "steering", "driver", "gps_monitor" }, out Vector3 frontHint);
        bool hasBackDoor = TryFindNamedRendererCenter(importedScene, new[] { "backdoor", "rear" }, out Vector3 rearHint);
        bool hasFrontDoor = TryFindNamedRendererCenter(importedScene, new[] { "frontdoor" }, out _);
        if (!hasSteering && !hasBackDoor && !hasFrontDoor)
        {
            return false;
        }

        Vector3 frontDirection = longAxis;
        if (hasSteering && hasBackDoor)
        {
            float frontDistance = Vector3.Dot(frontHint - bounds.center, longAxis);
            float rearDistance = Vector3.Dot(rearHint - bounds.center, longAxis);
            frontDirection = frontDistance >= rearDistance ? longAxis : -longAxis;
        }
        else if (hasSteering)
        {
            float steeringSide = Vector3.Dot(frontHint - bounds.center, longAxis);
            frontDirection = steeringSide > 0f ? longAxis : -longAxis;
        }

        if (frontDirection.sqrMagnitude < 0.001f)
        {
            return false;
        }

        Vector3 aisleCenter = bounds.center;
        float standingOffset = Mathf.Clamp(longSize * 0.24f, 2.6f, 3.6f);
        cameraPosition = aisleCenter - frontDirection * standingOffset;
        cameraPosition.y = eyeHeight;

        lookTarget = aisleCenter + frontDirection * Mathf.Clamp(longSize * 0.36f, 3.2f, 5f);
        lookTarget.y = eyeHeight - 0.14f;
        return true;
    }

    private static Vector3 FlattenHorizontal(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    private static bool TryFindNamedRendererCenter(GameObject root, string[] patterns, out Vector3 center)
    {
        center = Vector3.zero;
        if (root == null || patterns == null || patterns.Length == 0)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string hierarchyPath = GetHierarchyPath(renderer.transform).ToLowerInvariant();
            for (int patternIndex = 0; patternIndex < patterns.Length; patternIndex++)
            {
                string pattern = patterns[patternIndex];
                if (!string.IsNullOrEmpty(pattern) && hierarchyPath.Contains(pattern))
                {
                    sum += renderer.bounds.center;
                    count++;
                    break;
                }
            }
        }

        if (count == 0)
        {
            return false;
        }

        center = sum / count;
        return true;
    }

    private static Bounds GetSceneBounds(GameObject importedScene)
    {
        if (importedScene == null)
        {
            return new Bounds(Vector3.zero, new Vector3(3f, 2f, 7f));
        }

        Renderer[] renderers = importedScene.GetComponentsInChildren<Renderer>(true);
        List<Renderer> usableRenderers = GetUsableSceneRenderers(renderers);
        if (usableRenderers.Count == 0)
        {
            if (renderers.Length == 0)
            {
                return new Bounds(importedScene.transform.position, new Vector3(3f, 2f, 7f));
            }

            usableRenderers.AddRange(renderers);
        }

        Bounds bounds = usableRenderers[0].bounds;
        for (int i = 1; i < usableRenderers.Count; i++)
        {
            bounds.Encapsulate(usableRenderers[i].bounds);
        }

        if (usableRenderers.Count != renderers.Length)
        {
            Debug.Log("GAL FBX filtered scene bounds renderers=" + usableRenderers.Count + "/" + renderers.Length + " bounds=" + bounds);
        }

        return bounds;
    }

    private static List<Renderer> GetUsableSceneRenderers(Renderer[] renderers)
    {
        List<Renderer> result = new List<Renderer>();
        if (renderers == null)
        {
            return result;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            Vector3 size = bounds.size;
            if (!IsFinite(bounds.center) || !IsFinite(size))
            {
                continue;
            }

            string hierarchyPath = GetHierarchyPath(renderer.transform).ToLowerInvariant();
            float maxDimension = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (hierarchyPath.Contains("skfb_offset") || maxDimension > 250f || Mathf.Abs(bounds.center.y) > 1000f)
            {
                continue;
            }

            result.Add(renderer);
        }

        return result;
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
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

    private void AddReferenceMoodLights(Bounds bounds)
    {
        Vector3 center = bounds.center;
        float lightY = bounds.min.y + Mathf.Max(1.4f, bounds.size.y * 0.72f);
        float forward = Mathf.Max(bounds.size.x, bounds.size.z) * 0.24f;

        AddMoodLight("Reference Pink Aisle Wash", new Vector3(center.x - 0.35f, lightY, center.z - forward), new Color(0.95f, 0.9f, 1f, 1f), new Color(1f, 0.38f, 0.9f, 1f), 0.25f, 1.35f);
        AddMoodLight("Reference Violet Forward Glow", new Vector3(center.x + 0.2f, Mathf.Lerp(bounds.min.y, lightY, 0.82f), center.z + forward), new Color(0.82f, 0.9f, 1f, 1f), new Color(0.62f, 0.45f, 1f, 1f), 0.22f, 1.1f);
        AddMoodLight("Reference Cool Window Fill", new Vector3(center.x + bounds.extents.x * 0.78f, Mathf.Lerp(bounds.min.y, lightY, 0.72f), center.z), new Color(0.86f, 0.92f, 1f, 1f), new Color(0.28f, 0.45f, 0.72f, 1f), 0.35f, 0.75f);
    }

    private void AddFallbackLights(Bounds bounds)
    {
        Vector3 center = bounds.center;
        float lightY = bounds.min.y + Mathf.Max(1.35f, bounds.size.y * 0.68f);
        float longSize = Mathf.Max(bounds.size.x, bounds.size.z);
        Vector3 longAxis = bounds.size.x > bounds.size.z ? Vector3.right : Vector3.forward;
        Vector3 sideAxis = bounds.size.x > bounds.size.z ? Vector3.forward : Vector3.right;
        Vector3 keyPosition = center - longAxis * longSize * 0.22f;
        keyPosition.y = lightY;
        Vector3 fillPosition = center + sideAxis * Mathf.Max(1.2f, bounds.extents.x * 0.55f);
        fillPosition.y = Mathf.Lerp(bounds.min.y, lightY, 0.8f);

        AddMoodLight("FBX Fallback Key Light", keyPosition, new Color(1f, 0.96f, 0.9f, 1f), new Color(0.95f, 0.82f, 1f, 1f), 0.75f, 1.4f);
        AddMoodLight("FBX Fallback Fill Light", fillPosition, new Color(0.9f, 0.96f, 1f, 1f), new Color(0.45f, 0.65f, 1f, 1f), 0.45f, 0.85f);
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

    private void AddMoodLight(string lightName, Vector3 position, Color clearColor, Color eerieColor, float clearIntensity, float eerieIntensity)
    {
        GameObject lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(sceneRoot.transform, false);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 8f;

        moodLights.Add(new MoodLightBinding
        {
            light = light,
            clearColor = clearColor,
            eerieColor = eerieColor,
            clearIntensity = clearIntensity,
            eerieIntensity = eerieIntensity
        });
    }

    private void ApplyMoodSettings()
    {
        float strength = Mathf.Clamp01(moodIntensity);
        if (sceneRoot == null)
        {
            return;
        }

        if (sceneMoodEffect != null)
        {
            sceneMoodEffect.intensity = strength;
        }

        ApplyCharacterMoodSettings(strength);
        RenderSettings.ambientLight = Color.Lerp(ClearAmbientLight, EerieAmbientLight, strength);
        for (int i = 0; i < moodLights.Count; i++)
        {
            MoodLightBinding binding = moodLights[i];
            if (binding == null || binding.light == null)
            {
                continue;
            }

            binding.light.color = Color.Lerp(binding.clearColor, binding.eerieColor, strength);
            binding.light.intensity = Mathf.Lerp(binding.clearIntensity, binding.eerieIntensity, strength);
        }
    }

    private void DestroyScene()
    {
        ReleaseRuntimeCharacterTexture();

        if (sceneCharacterMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(sceneCharacterMaterial);
            }
            else
            {
                DestroyImmediate(sceneCharacterMaterial);
            }

            sceneCharacterMaterial = null;
        }

        sceneCharacter = null;
        sceneCharacterTexture = null;
        sceneBounds = default;

        if (sceneRoot != null)
        {
            Destroy(sceneRoot);
            sceneRoot = null;
        }

        sceneCamera = null;
        sceneMoodEffect = null;
        sceneCameraTransform = null;
        moodLights.Clear();
        RestoreOtherCameras();
    }

    private void ReleaseRuntimeCharacterTexture()
    {
        if (!sceneCharacterTextureIsRuntime || sceneCharacterTexture == null)
        {
            sceneCharacterTextureIsRuntime = false;
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(sceneCharacterTexture);
        }
        else
        {
            DestroyImmediate(sceneCharacterTexture);
        }

        sceneCharacterTexture = null;
        sceneCharacterTextureIsRuntime = false;
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
