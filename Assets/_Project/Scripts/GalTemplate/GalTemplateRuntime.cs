using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class GalStoryFile
{
    public string title = "GAL Template";
    public string startNode = "start_001";
    public string defaultBackground = "bedroom";
    public string textTable = "Text/story_text.csv";
    public List<GalBackgroundEntry> backgrounds = new List<GalBackgroundEntry>();
    public List<GalArtProfile> artProfiles = new List<GalArtProfile>();
    public List<GalPortraitEntry> portraits = new List<GalPortraitEntry>();
    public List<GalLanguageEntry> languages = new List<GalLanguageEntry>();
    public List<GalExplorePoint> explorePoints = new List<GalExplorePoint>();
    public List<GalStoryNode> nodes = new List<GalStoryNode>();
}

[Serializable]
public class GalBackgroundEntry
{
    public string id;
    public string displayName;
    public string path;
}

[Serializable]
public class GalArtProfile
{
    public string id;
    public string displayName;
    public string uiSkin;
    public string backgroundFolder;
}

[Serializable]
public class GalLanguageEntry
{
    public string id;
    public string displayName;
    public string tablePath;
    public string textTable;
}

[Serializable]
public class GalExplorePoint
{
    public string id;
    public string displayName;
    public string scene;
    public string nodeId;
    public string background;
    public string requiredFlag;
    public float x = 0.5f;
    public float y = 0.5f;
    public float width = 170f;
    public float height = 48f;
    public List<GalStoryCommand> commands = new List<GalStoryCommand>();
}

[Serializable]
public class GalStoryNode
{
    public string id;
    public string speaker;
    [TextArea(2, 8)] public string text;
    public string background;
    public string portraitSlot;
    public string portraitCharacter;
    public string portraitExpression;
    public string portraitFacing;
    public string portraitAnimation;
    public string portraitPath;
    public string nextId;
    public List<GalStoryChoice> choices = new List<GalStoryChoice>();
    public List<GalStoryCommand> commands = new List<GalStoryCommand>();
}

[Serializable]
public class GalStoryChoice
{
    public string id;
    public string text;
    public string nextId;
    public string requiredFlag;
    public List<GalStoryCommand> commands = new List<GalStoryCommand>();
}

[Serializable]
public class GalStoryCommand
{
    public string command;
    public string key;
    public string value;
    public string slot;
    public string character;
    public string expression;
    public string facing;
    public string animation;
    public string path;
    public float amount;
}

[Serializable]
public class GalTemplateSaveData
{
    public int version = 1;
    public bool isExploring;
    public bool isExternalScene;
    public string currentNodeId;
    public string currentBackgroundId;
    public string externalSceneResourcePath;
    public float externalScenePixelSize;
    public string savedAt;
    public List<string> flags = new List<string>();
    public List<string> inventory = new List<string>();
    public List<string> readNodes = new List<string>();
}

[Serializable]
public class GalTemplateSettings
{
    public float textSpeed = 42f;
    public float autoDelay = 1.2f;
    public float masterVolume = 0.8f;
    public float bgmVolume = 0.55f;
    public float fbxCameraHeight = 0.18f;
    public float cabinMoodIntensity = 0.75f;
    public float titleSaturation = 1f;
    public string fbxCharacterImageId = GalFbxSceneController.DefaultCharacterImageId;
    public float fbxCharacterViewportX = GalFbxSceneController.DefaultCharacterViewportX;
    public float fbxCharacterViewportY = GalFbxSceneController.DefaultCharacterViewportY;
    public float fbxCharacterViewportDepth = GalFbxSceneController.DefaultCharacterViewportDepth;
    public float fbxCharacterScreenHeight = GalFbxSceneController.DefaultCharacterScreenHeight;
    public float fbxCharacterPixelSize = GalFbxSceneController.DefaultCharacterPixelSize;
    public float fbxCharacterPixelRefinement = GalFbxSceneController.DefaultCharacterPixelRefinement;
    public float fbxCharacterMoodBlend = GalFbxSceneController.DefaultCharacterMoodBlend;
    public bool fullscreen = true;
    public bool skipUnreadText;
    public string language = "zh-CN";
    public string artProfile = "default";
}

public class GalDraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform target;

    private RectTransform parentRect;
    private Vector2 pointerOffset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }

        parentRect = target == null ? null : target.parent as RectTransform;
        if (target == null || parentRect == null)
        {
            return;
        }

        Vector2 localPointer;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out localPointer))
        {
            pointerOffset = target.anchoredPosition - localPointer;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (target == null || parentRect == null)
        {
            return;
        }

        Vector2 localPointer;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out localPointer))
        {
            return;
        }

        target.anchoredPosition = ClampToParent(localPointer + pointerOffset);
    }

    private Vector2 ClampToParent(Vector2 value)
    {
        Rect parent = parentRect.rect;
        Vector2 size = target.rect.size;
        Vector2 pivot = target.pivot;
        Vector2 anchorReference = new Vector2(
            Mathf.Lerp(parent.xMin, parent.xMax, (target.anchorMin.x + target.anchorMax.x) * 0.5f),
            Mathf.Lerp(parent.yMin, parent.yMax, (target.anchorMin.y + target.anchorMax.y) * 0.5f));
        float minX = parent.xMin - anchorReference.x + size.x * pivot.x;
        float maxX = parent.xMax - anchorReference.x - size.x * (1f - pivot.x);
        float minY = parent.yMin - anchorReference.y + size.y * pivot.y;
        float maxY = parent.yMax - anchorReference.y - size.y * (1f - pivot.y);

        if (minX > maxX)
        {
            minX = maxX = 0f;
        }

        if (minY > maxY)
        {
            minY = maxY = 0f;
        }

        return new Vector2(Mathf.Clamp(value.x, minX, maxX), Mathf.Clamp(value.y, minY, maxY));
    }
}

public class GalButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Image targetImage;
    private Image sweepImage;
    private Sprite normalSprite;
    private Sprite hoverSprite;
    private Sprite pressedSprite;
    private RectTransform rectTransform;
    private RectTransform sweepRect;
    private Button button;
    private Vector3 baseScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private float hoverScale = 1.035f;
    private float pressedScale = 0.965f;
    private float sweepDuration = 0.38f;
    private float sweepTimer;
    private bool pointerInside;
    private bool pointerDown;
    private bool sweepActive;

    public void Configure(Image image, Sprite normal, Sprite hover, Sprite pressed, float hoverAmount, float pressedAmount)
    {
        targetImage = image;
        normalSprite = normal;
        hoverSprite = hover;
        pressedSprite = pressed;
        hoverScale = hoverAmount;
        pressedScale = pressedAmount;
        ApplyState();
    }

    public void ConfigureSweep(Image image, float duration)
    {
        sweepImage = image;
        sweepRect = sweepImage == null ? null : sweepImage.GetComponent<RectTransform>();
        sweepDuration = Mathf.Max(0.12f, duration);
        sweepTimer = 0f;
        sweepActive = false;
        SetSweepAlpha(0f);
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        baseScale = rectTransform == null ? transform.localScale : rectTransform.localScale;
        targetScale = baseScale;
    }

    private void OnEnable()
    {
        pointerInside = false;
        pointerDown = false;
        sweepActive = false;
        sweepTimer = 0f;
        ApplyState();
        SetSweepAlpha(0f);
        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale;
        }
    }

    private void Update()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.unscaledDeltaTime * 16f);
        }
        UpdateSweep(Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        ApplyState();
        if (button == null || button.interactable)
        {
            StartSweep();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        pointerDown = false;
        ApplyState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDown = true;
        ApplyState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerDown = false;
        ApplyState();
    }

    private void ApplyState()
    {
        bool interactable = button == null || button.interactable;
        if (targetImage != null)
        {
            if (!interactable)
            {
                targetImage.sprite = normalSprite;
                targetImage.color = new Color(0.78f, 0.74f, 0.84f, 0.36f);
            }
            else if (pointerDown)
            {
                targetImage.sprite = pressedSprite == null ? normalSprite : pressedSprite;
                targetImage.color = new Color(0.96f, 0.84f, 1f, 0.88f);
            }
            else if (pointerInside)
            {
                targetImage.sprite = hoverSprite == null ? normalSprite : hoverSprite;
                targetImage.color = new Color(1f, 0.9f, 1f, 0.76f);
            }
            else
            {
                targetImage.sprite = normalSprite;
                targetImage.color = new Color(0.96f, 0.9f, 1f, 0.48f);
            }
        }

        float scale = interactable ? pointerDown ? pressedScale : pointerInside ? hoverScale : 1f : 1f;
        targetScale = baseScale * scale;
    }

    private void StartSweep()
    {
        if (sweepImage == null || sweepRect == null)
        {
            return;
        }

        sweepTimer = 0f;
        sweepActive = true;
    }

    private void UpdateSweep(float delta)
    {
        if (sweepImage == null || sweepRect == null)
        {
            return;
        }

        if (!sweepActive)
        {
            SetSweepAlpha(0f);
            return;
        }

        sweepTimer += delta;
        float t = Mathf.Clamp01(sweepTimer / sweepDuration);
        float eased = t * t * (3f - 2f * t);
        float width = rectTransform == null ? 448f : Mathf.Max(1f, rectTransform.rect.width);
        float height = rectTransform == null ? 132f : Mathf.Max(1f, rectTransform.rect.height);
        sweepRect.sizeDelta = new Vector2(Mathf.Max(54f, width * 0.18f), height * 1.7f);
        sweepRect.anchoredPosition = new Vector2(Mathf.Lerp(-width * 0.74f, width * 0.74f, eased), 0f);
        sweepRect.localEulerAngles = new Vector3(0f, 0f, -13f);
        SetSweepAlpha(Mathf.Sin(t * Mathf.PI) * (pointerInside ? 0.34f : 0.18f));

        if (t >= 1f)
        {
            sweepActive = false;
        }
    }

    private void SetSweepAlpha(float alpha)
    {
        if (sweepImage == null)
        {
            return;
        }

        sweepImage.color = new Color(1f, 0.84f, 1f, Mathf.Clamp01(alpha));
    }
}

public class GalBreathingAnimator : MonoBehaviour
{
    private RectTransform rectTransform;
    private Graphic graphic;
    private Vector3 baseScale = Vector3.one;
    private Color baseColor = Color.white;
    private float scaleAmplitude = 0.015f;
    private float alphaAmplitude;
    private float speed = 1f;
    private float phase;
    private bool initialized;

    public void Configure(float scaleAmount, float alphaAmount, float cyclesPerSecond)
    {
        scaleAmplitude = scaleAmount;
        alphaAmplitude = alphaAmount;
        speed = Mathf.Max(0.01f, cyclesPerSecond);
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        graphic = GetComponent<Graphic>();
        if (rectTransform != null)
        {
            baseScale = rectTransform.localScale;
        }
        if (graphic != null)
        {
            baseColor = graphic.color;
        }
        phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            Awake();
        }
    }

    private void Update()
    {
        float wave = Mathf.Sin((Time.unscaledTime * speed) + phase);
        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale * (1f + wave * scaleAmplitude);
        }

        if (graphic != null && alphaAmplitude > 0f)
        {
            Color color = baseColor;
            color.a = Mathf.Clamp01(baseColor.a + wave * alphaAmplitude);
            graphic.color = color;
        }
    }
}

public class GalFadeInAnimator : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private float startDelay;
    private float duration = 0.28f;
    private float startScale = 0.98f;
    private float elapsed;
    private bool initialized;

    public void Configure(float delay, float fadeDuration, float fromScale)
    {
        startDelay = Mathf.Max(0f, delay);
        duration = Mathf.Max(0.05f, fadeDuration);
        startScale = Mathf.Clamp(fromScale, 0.85f, 1f);
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        if (rectTransform != null)
        {
            baseScale = rectTransform.localScale;
        }
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            Awake();
        }

        elapsed = 0f;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale * startScale;
        }
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01((elapsed - startDelay) / duration);
        float eased = t * t * (3f - 2f * t);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = eased;
        }

        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.Lerp(baseScale * startScale, baseScale, eased);
        }

        if (elapsed > startDelay + duration)
        {
            enabled = false;
        }
    }
}

public static class GalUiRuntimeSprites
{
    private static Sprite softSweepSprite;
    private static Sprite softParticleSprite;
    private static Sprite horizontalFadeSprite;
    private static Sprite verticalFadeSprite;
    private static Sprite trianglePointerSprite;

    public static Sprite SoftSweepSprite
    {
        get
        {
            if (softSweepSprite == null)
            {
                Texture2D texture = new Texture2D(96, 8, TextureFormat.RGBA32, false);
                texture.name = "GAL Runtime Soft Sweep";
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < texture.height; y++)
                {
                    float vertical = Mathf.Sin(((float)y + 0.5f) / texture.height * Mathf.PI);
                    for (int x = 0; x < texture.width; x++)
                    {
                        float horizontal = Mathf.Sin(((float)x + 0.5f) / texture.width * Mathf.PI);
                        float alpha = Mathf.Pow(Mathf.Clamp01(horizontal), 2.15f) * Mathf.Lerp(0.55f, 1f, vertical);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                texture.Apply(false, true);
                softSweepSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }

            return softSweepSprite;
        }
    }

    public static Sprite SoftParticleSprite
    {
        get
        {
            if (softParticleSprite == null)
            {
                Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                texture.name = "GAL Runtime Soft Particle";
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                Vector2 center = new Vector2((texture.width - 1f) * 0.5f, (texture.height - 1f) * 0.5f);
                float radius = texture.width * 0.5f;
                for (int y = 0; y < texture.height; y++)
                {
                    for (int x = 0; x < texture.width; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                        float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.4f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                texture.Apply(false, true);
                softParticleSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }

            return softParticleSprite;
        }
    }

    public static Sprite HorizontalFadeSprite
    {
        get
        {
            if (horizontalFadeSprite == null)
            {
                Texture2D texture = new Texture2D(128, 4, TextureFormat.RGBA32, false);
                texture.name = "GAL Runtime Horizontal Fade";
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < texture.height; y++)
                {
                    for (int x = 0; x < texture.width; x++)
                    {
                        float t = (float)x / (texture.width - 1f);
                        float alpha = Mathf.Pow(1f - Mathf.Clamp01(t), 1.7f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                texture.Apply(false, true);
                horizontalFadeSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }

            return horizontalFadeSprite;
        }
    }

    public static Sprite VerticalFadeSprite
    {
        get
        {
            if (verticalFadeSprite == null)
            {
                Texture2D texture = new Texture2D(4, 96, TextureFormat.RGBA32, false);
                texture.name = "GAL Runtime Vertical Fade";
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < texture.height; y++)
                {
                    float t = (float)y / (texture.height - 1f);
                    float alpha = Mathf.Pow(Mathf.Clamp01(t), 1.55f);
                    for (int x = 0; x < texture.width; x++)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                texture.Apply(false, true);
                verticalFadeSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }

            return verticalFadeSprite;
        }
    }

    public static Sprite TrianglePointerSprite
    {
        get
        {
            if (trianglePointerSprite == null)
            {
                Texture2D texture = new Texture2D(48, 48, TextureFormat.RGBA32, false);
                texture.name = "GAL Runtime Triangle Pointer";
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Point;
                for (int y = 0; y < texture.height; y++)
                {
                    for (int x = 0; x < texture.width; x++)
                    {
                        float left = 10f;
                        float right = 38f;
                        float centerY = 24f;
                        float halfHeight = Mathf.Lerp(2f, 18f, Mathf.Clamp01((x - left) / (right - left)));
                        bool inside = x >= left && x <= right && Mathf.Abs(y - centerY) <= halfHeight;
                        bool outline = x >= left - 3f && x <= right + 3f && Mathf.Abs(y - centerY) <= halfHeight + 3f;
                        Color color = Color.clear;
                        if (outline)
                        {
                            color = new Color(0.78f, 0.48f, 1f, 0.72f);
                        }
                        if (inside)
                        {
                            color = new Color(1f, 0.86f, 1f, 1f);
                        }
                        texture.SetPixel(x, y, color);
                    }
                }
                texture.Apply(false, true);
                trianglePointerSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }

            return trianglePointerSprite;
        }
    }

}

public class GalLoopSweepAnimator : MonoBehaviour
{
    private Image image;
    private RectTransform rectTransform;
    private Vector2 basePosition;
    private float startX = -320f;
    private float endX = 320f;
    private float duration = 0.9f;
    private float intervalMin = 4.2f;
    private float intervalMax = 6.8f;
    private float nextSweepTime = 1f;
    private float elapsed;
    private float sweepTimer;
    private bool sweeping;

    public void Configure(Image targetImage, float fromX, float toX, float sweepDuration, float minInterval, float maxInterval, float initialDelay)
    {
        image = targetImage;
        rectTransform = image == null ? GetComponent<RectTransform>() : image.GetComponent<RectTransform>();
        startX = fromX;
        endX = toX;
        duration = Mathf.Max(0.12f, sweepDuration);
        intervalMin = Mathf.Max(0.2f, minInterval);
        intervalMax = Mathf.Max(intervalMin, maxInterval);
        basePosition = rectTransform == null ? Vector2.zero : rectTransform.anchoredPosition;
        ResetSweep(Mathf.Max(0f, initialDelay));
    }

    private void Awake()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        if (rectTransform != null)
        {
            basePosition = rectTransform.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        ResetSweep(nextSweepTime);
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (!sweeping)
        {
            if (elapsed >= nextSweepTime)
            {
                sweeping = true;
                sweepTimer = 0f;
            }
            else
            {
                SetAlpha(0f);
            }
            return;
        }

        sweepTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(sweepTimer / duration);
        float eased = t * t * (3f - 2f * t);
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = basePosition + new Vector2(Mathf.Lerp(startX, endX, eased), 0f);
        }
        SetAlpha(Mathf.Sin(t * Mathf.PI) * 0.34f);

        if (t >= 1f)
        {
            sweeping = false;
            nextSweepTime = elapsed + UnityEngine.Random.Range(intervalMin, intervalMax);
        }
    }

    private void ResetSweep(float delay)
    {
        elapsed = 0f;
        sweepTimer = 0f;
        sweeping = false;
        nextSweepTime = delay;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = basePosition + new Vector2(startX, 0f);
        }
        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }
}

public class GalTitleParticleAnimator : MonoBehaviour
{
    private class Particle
    {
        public RectTransform rect;
        public Image image;
        public Vector2 origin;
        public float phase;
        public float speed;
        public float driftX;
        public float driftY;
        public float size;
        public float alpha;
    }

    private readonly List<Particle> particles = new List<Particle>();
    private RectTransform rectTransform;
    private int particleCount = 22;
    private bool built;

    public void Configure(int count)
    {
        particleCount = Mathf.Clamp(count, 6, 48);
        rectTransform = GetComponent<RectTransform>();
        BuildParticles();
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (!built && rectTransform != null)
        {
            BuildParticles();
        }
    }

    private void BuildParticles()
    {
        if (built || rectTransform == null)
        {
            return;
        }

        built = true;
        Rect area = rectTransform.rect;
        float width = Mathf.Max(1f, area.width);
        float height = Mathf.Max(1f, area.height);
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particleObject = new GameObject("Title Dust " + i.ToString("00"), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            particleObject.transform.SetParent(transform, false);
            RectTransform particleRect = particleObject.GetComponent<RectTransform>();
            particleRect.anchorMin = new Vector2(0.5f, 0.5f);
            particleRect.anchorMax = new Vector2(0.5f, 0.5f);
            particleRect.pivot = new Vector2(0.5f, 0.5f);

            Image particleImage = particleObject.GetComponent<Image>();
            particleImage.sprite = GalUiRuntimeSprites.SoftParticleSprite;
            particleImage.raycastTarget = false;

            float size = UnityEngine.Random.Range(2.2f, 5.8f);
            float x = UnityEngine.Random.Range(-width * 0.44f, width * 0.18f);
            float y = UnityEngine.Random.Range(-height * 0.42f, height * 0.42f);
            Particle particle = new Particle
            {
                rect = particleRect,
                image = particleImage,
                origin = new Vector2(x, y),
                phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                speed = UnityEngine.Random.Range(0.28f, 0.72f),
                driftX = UnityEngine.Random.Range(8f, 26f),
                driftY = UnityEngine.Random.Range(4f, 18f),
                size = size,
                alpha = UnityEngine.Random.Range(0.025f, 0.075f)
            };
            particleRect.sizeDelta = new Vector2(size, size);
            particles.Add(particle);
        }
    }

    private void Update()
    {
        float time = Time.unscaledTime;
        for (int i = 0; i < particles.Count; i++)
        {
            Particle particle = particles[i];
            if (particle == null || particle.rect == null || particle.image == null)
            {
                continue;
            }

            float wave = time * particle.speed + particle.phase;
            particle.rect.anchoredPosition = particle.origin + new Vector2(Mathf.Sin(wave) * particle.driftX, Mathf.Cos(wave * 0.73f) * particle.driftY);
            float pulse = 0.55f + Mathf.Sin(wave * 1.37f) * 0.45f;
            float scale = 0.78f + pulse * 0.42f;
            particle.rect.localScale = new Vector3(scale, scale, 1f);
            particle.image.color = new Color(1f, 0.78f, 1f, particle.alpha * (0.38f + pulse * 0.62f));
        }
    }
}

public class GalTitleLogoAnimator : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private Image logoImage;
    private Image glowImage;
    private Image bloomImage;
    private Image glitchImageA;
    private Image glitchImageB;
    private readonly List<Image> stableEffectImages = new List<Image>();
    private readonly List<Color> stableEffectBaseColors = new List<Color>();
    private RectTransform rootRect;
    private RectTransform logoRect;
    private RectTransform glowRect;
    private RectTransform bloomRect;
    private RectTransform glitchRectA;
    private RectTransform glitchRectB;
    private Vector3 rootBaseScale = Vector3.one;
    private Vector2 rootBasePosition;
    private Vector3 logoBaseScale = Vector3.one;
    private Vector3 glowBaseScale = Vector3.one;
    private Vector3 bloomBaseScale = Vector3.one;
    private Vector3 glitchBaseScaleA = Vector3.one;
    private Vector3 glitchBaseScaleB = Vector3.one;
    private Vector2 logoBasePosition;
    private Vector2 glowBasePosition;
    private Vector2 bloomBasePosition;
    private Vector2 glitchBasePositionA;
    private Vector2 glitchBasePositionB;
    private Color logoBaseColor = Color.white;
    private Color glowBaseColor = Color.white;
    private Color bloomBaseColor = Color.white;
    private Color glitchBaseColorA = Color.white;
    private Color glitchBaseColorB = Color.white;
    private float elapsed;
    private float flickerTimer;
    private float flickerDuration;
    private float nextFlickerTime = 8f;
    private bool configured;
    private Sprite[] frameSequence;
    private float frameFps = 16f;
    private float frameTimer;
    private int frameIndex;

    public void Configure(Image logo, Image glow, Image bloom, Image glitchA = null, Image glitchB = null, IList<Image> stableEffects = null)
    {
        rootRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        logoImage = logo;
        glowImage = glow;
        bloomImage = bloom;
        glitchImageA = glitchA;
        glitchImageB = glitchB;
        logoRect = logoImage == null ? null : logoImage.GetComponent<RectTransform>();
        glowRect = glowImage == null ? null : glowImage.GetComponent<RectTransform>();
        bloomRect = bloomImage == null ? null : bloomImage.GetComponent<RectTransform>();
        glitchRectA = glitchImageA == null ? null : glitchImageA.GetComponent<RectTransform>();
        glitchRectB = glitchImageB == null ? null : glitchImageB.GetComponent<RectTransform>();

        if (rootRect != null)
        {
            rootBaseScale = rootRect.localScale;
            rootBasePosition = rootRect.anchoredPosition;
        }

        if (logoRect != null)
        {
            logoBaseScale = logoRect.localScale;
            logoBasePosition = logoRect.anchoredPosition;
        }
        if (glowRect != null)
        {
            glowBaseScale = glowRect.localScale;
            glowBasePosition = glowRect.anchoredPosition;
        }
        if (bloomRect != null)
        {
            bloomBaseScale = bloomRect.localScale;
            bloomBasePosition = bloomRect.anchoredPosition;
        }
        if (glitchRectA != null)
        {
            glitchBaseScaleA = glitchRectA.localScale;
            glitchBasePositionA = glitchRectA.anchoredPosition;
        }
        if (glitchRectB != null)
        {
            glitchBaseScaleB = glitchRectB.localScale;
            glitchBasePositionB = glitchRectB.anchoredPosition;
        }
        if (logoImage != null)
        {
            logoBaseColor = logoImage.color;
        }
        if (glowImage != null)
        {
            glowBaseColor = glowImage.color;
        }
        if (bloomImage != null)
        {
            bloomBaseColor = bloomImage.color;
        }
        stableEffectImages.Clear();
        stableEffectBaseColors.Clear();
        if (stableEffects != null)
        {
            for (int i = 0; i < stableEffects.Count; i++)
            {
                Image effect = stableEffects[i];
                if (effect == null)
                {
                    continue;
                }

                stableEffectImages.Add(effect);
                stableEffectBaseColors.Add(effect.color);
            }
        }
        if (glitchImageA != null)
        {
            glitchBaseColorA = glitchImageA.color;
        }
        if (glitchImageB != null)
        {
            glitchBaseColorB = glitchImageB.color;
        }

        configured = true;
        ResetTimeline();
    }

    public void ConfigureFrameSequence(Sprite[] frames, float fps)
    {
        frameSequence = frames != null && frames.Length > 0 ? frames : null;
        frameFps = Mathf.Clamp(fps, 1f, 60f);
        frameTimer = 0f;
        frameIndex = 0;
        if (logoImage != null && frameSequence != null && frameSequence.Length > 0)
        {
            logoImage.sprite = frameSequence[0];
        }
    }

    private void OnEnable()
    {
        if (configured)
        {
            ResetTimeline();
        }
    }

    private void Update()
    {
        if (!configured)
        {
            return;
        }

        float delta = Time.unscaledDeltaTime;
        elapsed += delta;
        UpdateFrameSequence(delta);
        if (elapsed >= nextFlickerTime)
        {
            flickerDuration = UnityEngine.Random.Range(0.045f, 0.085f);
            flickerTimer = flickerDuration;
            nextFlickerTime = elapsed + UnityEngine.Random.Range(7f, 12f);
        }

        float flicker = 0f;
        if (flickerTimer > 0f)
        {
            flickerTimer = Mathf.Max(0f, flickerTimer - delta);
            float flickerProgress = flickerDuration <= 0.001f ? 1f : 1f - flickerTimer / flickerDuration;
            flicker = Mathf.Sin(Mathf.Clamp01(flickerProgress) * Mathf.PI);
        }

        ApplyVisuals(flicker);
    }

    private void UpdateFrameSequence(float delta)
    {
        if (logoImage == null || frameSequence == null || frameSequence.Length == 0)
        {
            return;
        }

        float interval = 1f / Mathf.Max(1f, frameFps);
        frameTimer += delta;
        while (frameTimer >= interval)
        {
            frameTimer -= interval;
            frameIndex = (frameIndex + 1) % frameSequence.Length;
            logoImage.sprite = frameSequence[frameIndex];
        }
    }

    private void ResetTimeline()
    {
        elapsed = 0f;
        flickerTimer = 0f;
        flickerDuration = 0f;
        nextFlickerTime = UnityEngine.Random.Range(6.5f, 10.5f);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        if (rootRect != null)
        {
            rootRect.localScale = rootBaseScale * 0.94f;
            rootRect.anchoredPosition = rootBasePosition + new Vector2(0f, 18f);
        }
        ApplyVisuals(0f);
    }

    private void ApplyVisuals(float flicker)
    {
        float rootReveal = Smooth01(elapsed / 0.28f);
        float bloomReveal = Smooth01(elapsed / 0.58f);
        float glowReveal = Smooth01((elapsed - 0.03f) / 0.48f);
        float logoReveal = Smooth01((elapsed - 0.07f) / 0.38f);
        float startupFlash = Smooth01(1f - Mathf.Abs(elapsed - 0.18f) / 0.10f);
        float slowWave = Mathf.Sin(elapsed * Mathf.PI * 2f * 0.18f);
        float glowWave = Mathf.Sin(elapsed * Mathf.PI * 2f * 0.28f + 1.35f);
        float settlePop = Mathf.Sin(Mathf.Clamp01(elapsed / 0.52f) * Mathf.PI) * 0.085f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(rootReveal + startupFlash * 0.16f + flicker * 0.16f);
        }
        if (rootRect != null)
        {
            rootRect.localScale = rootBaseScale * (0.96f + rootReveal * 0.04f + startupFlash * 0.008f + flicker * 0.004f);
            rootRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(elapsed * 0.25f) * 0.18f + flicker * 0.35f);
            rootRect.anchoredPosition = rootBasePosition + new Vector2(Mathf.Sin(elapsed * 0.52f) * 1.1f, 8f * (1f - rootReveal) + Mathf.Cos(elapsed * 1.1f) * 0.65f + startupFlash * 2.2f);
        }

        if (logoRect != null)
        {
            float logoScale = Mathf.Lerp(0.92f, 1f, logoReveal) + settlePop * 0.36f + slowWave * 0.004f + flicker * 0.004f;
            logoRect.localScale = logoBaseScale * logoScale;
            logoRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(elapsed * 0.33f) * 0.18f + flicker * 0.28f);
            logoRect.anchoredPosition = logoBasePosition + new Vector2(Mathf.Sin(elapsed * 1.1f) * 0.28f, Mathf.Cos(elapsed * 0.8f) * 0.18f);
        }
        if (glowRect != null)
        {
            float glowScale = Mathf.Lerp(0.96f, 1f, glowReveal) + glowWave * 0.006f + flicker * 0.012f;
            float jitter = Mathf.Sign(Mathf.Sin(elapsed * 97.13f)) * flicker * 0.9f;
            glowRect.localScale = glowBaseScale * glowScale;
            glowRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(elapsed * 0.24f + 0.7f) * 0.24f + flicker * 0.42f);
            glowRect.anchoredPosition = glowBasePosition + new Vector2(Mathf.Sin(elapsed * 0.82f) * 0.8f + jitter, Mathf.Cos(elapsed * 0.64f) * 0.45f);
        }
        if (bloomRect != null)
        {
            float bloomScale = Mathf.Lerp(0.97f, 1f, bloomReveal) + slowWave * 0.008f + flicker * 0.018f;
            bloomRect.localScale = bloomBaseScale * bloomScale;
            bloomRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(elapsed * 0.19f + 1.15f) * 0.16f);
            bloomRect.anchoredPosition = bloomBasePosition + new Vector2(Mathf.Sin(elapsed * 0.53f + 0.4f) * 0.9f, Mathf.Cos(elapsed * 0.49f) * 0.55f);
        }

        SetLayerColor(bloomImage, bloomBaseColor, bloomReveal * (0.2f + slowWave * 0.035f + flicker * 0.42f), 0.06f + startupFlash * 0.24f + flicker * 0.1f);
        SetLayerColor(glowImage, glowBaseColor, glowReveal * (0.34f + glowWave * 0.055f + flicker * 0.45f), 0.05f + startupFlash * 0.26f + flicker * 0.12f);
        SetLayerColor(logoImage, logoBaseColor, logoReveal * (1f + slowWave * 0.018f), 0.04f + startupFlash * 0.18f + flicker * 0.12f);
        ApplyStableEffects(logoReveal, startupFlash, flicker);
        ApplyGlitchLayer(glitchImageA, glitchRectA, glitchBasePositionA, glitchBaseScaleA, glitchBaseColorA, flicker, 1f);
        ApplyGlitchLayer(glitchImageB, glitchRectB, glitchBasePositionB, glitchBaseScaleB, glitchBaseColorB, flicker, -1f);
    }

    private static float Smooth01(float value)
    {
        float t = Mathf.Clamp01(value);
        return t * t * (3f - 2f * t);
    }

    private static void SetLayerColor(Image image, Color baseColor, float alphaMultiplier, float whiteMix)
    {
        if (image == null)
        {
            return;
        }

        Color color = Color.Lerp(baseColor, Color.white, Mathf.Clamp01(whiteMix));
        color.a = Mathf.Clamp01(baseColor.a * alphaMultiplier);
        image.color = color;
    }

    private void ApplyStableEffects(float reveal, float startupFlash, float flicker)
    {
        for (int i = 0; i < stableEffectImages.Count; i++)
        {
            Image effect = stableEffectImages[i];
            if (effect == null)
            {
                continue;
            }

            Color color = stableEffectBaseColors[i];
            color.a = Mathf.Clamp01(color.a * reveal * (1f + startupFlash * 0.2f + flicker * 0.08f));
            effect.color = color;
        }
    }

    private void ApplyGlitchLayer(Image image, RectTransform rect, Vector2 basePosition, Vector3 baseScale, Color baseColor, float flicker, float direction)
    {
        if (image == null || rect == null)
        {
            return;
        }

        float pulse = flicker * (0.72f + Mathf.Abs(Mathf.Sin(elapsed * 113.7f)) * 0.28f);
        rect.localScale = baseScale * (1f + pulse * 0.008f);
        rect.localEulerAngles = new Vector3(0f, 0f, direction * pulse * 0.35f);
        rect.anchoredPosition = basePosition + new Vector2(direction * pulse * (3f + Mathf.Abs(Mathf.Sin(elapsed * 71.3f)) * 5f), Mathf.Sign(Mathf.Sin(elapsed * 53.9f)) * pulse * 1.2f);

        Color color = baseColor;
        color.a = Mathf.Clamp01(pulse * 0.24f);
        image.color = color;
    }
}

public class GalHistoryLine
{
    public string speaker;
    public string text;
}

public class GalTextEntry
{
    public string key;
    public string speaker;
    public string text;
    public string portraitSlot;
    public string portraitCharacter;
    public string portraitExpression;
    public string portraitFacing;
    public string portraitAnimation;
    public string portraitPath;
}

public class GalRawTextRow
{
    public string key;
    public readonly Dictionary<string, string> values = new Dictionary<string, string>();
}

[Serializable]
public class GalUiSkinFile
{
    public string id;
    public string displayName;
    public string spriteFolder = "Sprites";
    public GalUiSkinSprites sprites = new GalUiSkinSprites();
    public GalUiSkinColors colors = new GalUiSkinColors();
    public GalUiSkinAnimation animation = new GalUiSkinAnimation();
}

[Serializable]
public class GalUiSkinSprites
{
    public string titleBackground;
    public string titleLogo;
    public GalUiSkinFrameSequence titleLogoFrames;
    public List<GalUiSkinLocalizedFrameSequence> titleLogoLocalizedFrames = new List<GalUiSkinLocalizedFrameSequence>();
    public string buttonNormal;
    public string buttonHover;
    public string buttonPressed;
    public string dialogueBox;
}

[Serializable]
public class GalUiSkinLocalizedFrameSequence
{
    public string language;
    public GalUiSkinFrameSequence sequence = new GalUiSkinFrameSequence();
}

[Serializable]
public class GalUiSkinFrameSequence
{
    public string folder;
    public string prefix;
    public string extension = ".png";
    public int count;
    public int startIndex = 1;
    public int digits = 2;
    public float fps = 16f;
}

[Serializable]
public class GalUiSkinColors
{
    public string buttonText;
    public string buttonTextShadow;
    public string panelText;
    public string dialogueText;
    public string dialogueSpeaker;
    public string hudText;
}

[Serializable]
public class GalUiSkinAnimation
{
    public float hoverScale = 1.035f;
    public float pressedScale = 0.965f;
    public float fadeDuration = 0.12f;
    public float titleButtonStagger = 0.045f;
}

public class GalTemplateRuntime : MonoBehaviour
{
    private const string StoryRelativePath = "GAL/gal_story.json";
    private const string DefaultTextTableRelativePath = "Text/story_text.csv";
    private const string DefaultBgmResourcePath = "GAL/Audio/hallway_loop_82bpm_up2";
    private const string SaveFolderName = "GalTemplate";
    private const int SaveSlotCount = 6;
    private const int QuickSaveSlot = 1;
    private const string ExternalCharacterDialogueNodeId = "__fbx_unauthorized_passenger_test_dialogue";
    private static readonly string[] CharacterImportExtensions = { ".png", ".jpg", ".jpeg" };
    private static readonly Color UiInk = new Color(0.11f, 0.075f, 0.13f, 1f);
    private static readonly Color UiInkMuted = new Color(0.28f, 0.22f, 0.32f, 0.82f);
    private static readonly Color UiPanel = new Color(0.975f, 0.965f, 0.94f, 0.985f);
    private static readonly Color UiPanelAlt = new Color(0.93f, 0.89f, 0.96f, 0.28f);
    private static readonly Color UiPanelLine = new Color(0.38f, 0.23f, 0.42f, 0.16f);
    private static readonly Color UiGlassNormal = new Color(0.92f, 0.86f, 1f, 0.46f);
    private static readonly Color UiGlassHover = new Color(1f, 0.9f, 1f, 0.7f);
    private static readonly Color UiGlassPressed = new Color(0.76f, 0.62f, 0.9f, 0.82f);
    private static readonly Color UiGlassDisabled = new Color(0.75f, 0.7f, 0.78f, 0.32f);
    private static readonly Color UiAccent = new Color(0.72f, 0.42f, 0.82f, 0.9f);
    private static readonly Color UiAccentSoft = new Color(0.84f, 0.56f, 0.88f, 0.55f);

    private static GalTemplateRuntime instance;

    private enum GalOverlayPage
    {
        None,
        Settings,
        CharacterSettings,
        SaveLoad,
        History,
        PortraitDebug
    }

    private readonly Dictionary<string, GalStoryNode> nodesById = new Dictionary<string, GalStoryNode>();
    private readonly Dictionary<string, GalBackgroundEntry> backgroundsById = new Dictionary<string, GalBackgroundEntry>();
    private readonly Dictionary<string, GalExplorePoint> explorePointsById = new Dictionary<string, GalExplorePoint>();
    private readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    private readonly HashSet<string> flags = new HashSet<string>();
    private readonly HashSet<string> inventory = new HashSet<string>();
    private readonly HashSet<string> readNodes = new HashSet<string>();
    private readonly List<GalHistoryLine> history = new List<GalHistoryLine>();
    private readonly Dictionary<string, GalRawTextRow> rawTextRowsByKey = new Dictionary<string, GalRawTextRow>();
    private readonly Dictionary<string, GalTextEntry> textEntriesByKey = new Dictionary<string, GalTextEntry>();

    private GalStoryFile story;
    private GalTemplateSettings settings = new GalTemplateSettings();
    private string storyPath;
    private string textTablePath;
    private DateTime storyLastWriteTimeUtc;
    private DateTime textTableLastWriteTimeUtc;
    private float hotReloadTimer;
    private GalStoryNode currentNode;
    private string currentNodeId;
    private string currentBackgroundId;
    private string currentLine;
    private bool currentNodeCommandsExecuted;
    private bool currentNodeWasReadBefore;
    private bool isExternalSceneDialogue;
    private string externalSceneReturnNodeId;
    private GalStoryNode externalSceneReturnNode;
    private bool externalSceneReturnWasExploring;
    private bool externalSceneReturnCommandsExecuted;
    private bool externalSceneReturnWasReadBefore;
    private int externalSceneDialogueOpenedFrame = -1;
    private bool isTyping;
    private bool isInGame;
    private bool isExploring;
    private bool isAwaitingChoice;
    private bool isSettingsOpen;
    private bool isSaveLoadOpen;
    private GalOverlayPage currentOverlayPage = GalOverlayPage.None;
    private GalOverlayPage previousOverlayPage = GalOverlayPage.None;
    private bool isAutoMode;
    private bool isSkipMode;
    private bool isDialogueHidden;
    private Coroutine typingRoutine;
    private Coroutine autoRoutine;
    private Coroutine toastRoutine;
    private Font uiFont;
    private Font titleFont;

    private Canvas canvas;
    private GameObject backgroundRoot;
    private GameObject backgroundWashRoot;
    private RawImage backgroundImage;
    private AspectRatioFitter backgroundAspect;
    private GalPortraitController portraitController;
    private Coroutine sceneTransitionRoutine;
    private GameObject transitionRoot;
    private Image transitionImage;
    private Text transitionText;
    private bool isTransitioning;
    private GameObject mainMenuRoot;
    private Text menuTitleText;
    private Image menuTitleLogoImage;
    private Image menuTitleGlowImage;
    private Image menuTitleBloomImage;
    private GalTitleLogoAnimator menuTitleLogoAnimator;
    private Text primaryActionLabel;
    private Text saveInfoText;
    private Button primaryActionButton;
    private Button newGameButton;
    private Text newGameButtonLabel;
    private Text mainMenuSettingsButtonLabel;
    private Text quitButtonLabel;
    private RectTransform titleMenuPointerGlowRect;
    private RectTransform titleMenuPointerRect;
    private float titleMenuPointerCurrentY;
    private float titleMenuPointerTargetY;
    private bool titleMenuPointerReady;
    private const float TitleMenuPointerX = 94f;
    private GameObject dialogueRoot;
    private Text speakerText;
    private Text dialogueText;
    private Text continueHintText;
    private Transform choiceContainer;
    private GameObject exploreRoot;
    private Transform exploreButtonContainer;
    private RectTransform exploreButtonAreaRect;
    private AspectRatioFitter exploreButtonAreaAspect;
    private Text exploreTitleText;
    private GameObject hudRoot;
    private Text autoButtonLabel;
    private Text skipButtonLabel;
    private Text hudSaveButtonLabel;
    private Text hudLoadButtonLabel;
    private Text hudHideButtonLabel;
    private Text hudHistoryButtonLabel;
    private Text hudSettingsButtonLabel;
    private Text hudDebugButtonLabel;
    private Text hudTitleButtonLabel;
    private GameObject fbxHudRoot;
    private Text fbxBackButtonLabel;
    private Text fbxSaveButtonLabel;
    private Text fbxLoadButtonLabel;
    private Text fbxHistoryButtonLabel;
    private Text fbxSettingsButtonLabel;
    private Text fbxTitleButtonLabel;
    private GameObject toastRoot;
    private Text toastText;
    private GameObject historyRoot;
    private Text historyText;
    private Text historyTitleText;
    private Text historyBackButtonLabel;
    private Text historyExitButtonLabel;
    private Button historyBackButton;
    private GameObject saveLoadRoot;
    private Text saveLoadTitleText;
    private Transform saveSlotContainer;
    private Button saveLoadBackButton;
    private Text saveLoadBackButtonLabel;
    private Text saveLoadExitButtonLabel;
    private bool saveLoadPanelForSaving;
    private GameObject settingsRoot;
    private Text settingsTitleText;
    private Text textSpeedValueText;
    private Text autoDelayValueText;
    private Text volumeValueText;
    private Text bgmVolumeValueText;
    private Text fbxCameraHeightValueText;
    private Text cabinMoodValueText;
    private Text titleSaturationValueText;
    private Text languageValueText;
    private Text settingsTextSpeedLabel;
    private Text settingsAutoDelayLabel;
    private Text settingsVolumeLabel;
    private Text settingsBgmVolumeLabel;
    private Text settingsFbxCameraHeightLabel;
    private Text settingsCabinMoodLabel;
    private Text settingsTitleSaturationLabel;
    private Text settingsFullscreenLabel;
    private Text settingsSkipUnreadLabel;
    private Text settingsSavePanelButtonLabel;
    private Text settingsLoadPanelButtonLabel;
    private Text settingsHistoryButtonLabel;
    private Text settingsReloadButtonLabel;
    private Text settingsDeleteButtonLabel;
    private Text settingsDebugButtonLabel;
    private Text settingsCharacterButtonLabel;
    private Text settingsExitButtonLabel;
    private Button settingsSavePanelButton;
    private Button settingsCharacterButton;
    private Toggle fullscreenToggle;
    private Toggle skipUnreadToggle;
    private Slider textSpeedSlider;
    private Slider autoDelaySlider;
    private Slider volumeSlider;
    private Slider bgmVolumeSlider;
    private Slider fbxCameraHeightSlider;
    private Slider cabinMoodSlider;
    private Slider titleSaturationSlider;
    private GameObject characterSettingsRoot;
    private RectTransform characterSettingsPanelRect;
    private GameObject characterPositionPage;
    private GameObject characterImagePage;
    private Text characterSettingsTitleText;
    private Text characterDragHintText;
    private Text characterPositionTabLabel;
    private Text characterImageTabLabel;
    private Text characterImageButtonLabel;
    private Text characterViewportXLabel;
    private Text characterViewportYLabel;
    private Text characterViewportDepthLabel;
    private Text characterScreenHeightLabel;
    private Text characterPixelSizeLabel;
    private Text characterPixelRefinementLabel;
    private Text characterMoodBlendLabel;
    private Text characterViewportXValueText;
    private Text characterViewportYValueText;
    private Text characterViewportDepthValueText;
    private Text characterScreenHeightValueText;
    private Text characterPixelSizeValueText;
    private Text characterPixelRefinementValueText;
    private Text characterMoodBlendValueText;
    private Text characterImportPathLabel;
    private Text characterImportButtonLabel;
    private Text characterOpenImportFolderButtonLabel;
    private Text characterRefreshImagesButtonLabel;
    private Text characterImportDirectoryText;
    private Text characterResetPanelButtonLabel;
    private Text characterSettingsBackButtonLabel;
    private Text characterSettingsExitButtonLabel;
    private Button characterPositionTabButton;
    private Button characterImageTabButton;
    private Button characterSettingsBackButton;
    private InputField characterImportPathInput;
    private Slider characterViewportXSlider;
    private Slider characterViewportYSlider;
    private Slider characterViewportDepthSlider;
    private Slider characterScreenHeightSlider;
    private Slider characterPixelSizeSlider;
    private Slider characterPixelRefinementSlider;
    private Slider characterMoodBlendSlider;
    private bool characterSettingsShowingImagePage;
    private GameObject portraitDebugRoot;
    private Text portraitDebugTitleText;
    private Text portraitDebugSlotLabel;
    private Text portraitDebugCharacterLabel;
    private Text portraitDebugExpressionLabel;
    private Text portraitDebugFacingLabel;
    private Text portraitDebugAnimationLabel;
    private Text portraitDebugBackButtonLabel;
    private Text portraitDebugExitButtonLabel;
    private string debugPortraitSlot = "center";
    private string debugPortraitCharacter = "test";
    private string debugPortraitExpression = "neutral";
    private string debugPortraitFacing = "auto";
    private string debugPortraitAnimation = "shake";
    private GalUiSkinFile activeUiSkin;
    private Sprite uiTitleBackgroundSprite;
    private Sprite uiTitleLogoSprite;
    private Sprite[] uiTitleLogoFrameSprites;
    private float uiTitleLogoFrameFps = 16f;
    private readonly Dictionary<string, Sprite[]> uiTitleLogoFramesByLanguage = new Dictionary<string, Sprite[]>();
    private readonly Dictionary<string, float> uiTitleLogoFrameFpsByLanguage = new Dictionary<string, float>();
    private readonly List<Image> titleSaturationImages = new List<Image>();
    private Material titleSaturationMaterial;
    private Sprite uiButtonNormalSprite;
    private Sprite uiButtonHoverSprite;
    private Sprite uiButtonPressedSprite;
    private Sprite uiDialogueBoxSprite;
    private readonly Dictionary<string, Sprite> titleGlassSpriteCache = new Dictionary<string, Sprite>();
    private Color uiButtonTextColor = new Color(0.98f, 0.95f, 1f, 1f);
    private Color uiButtonTextShadowColor = new Color(0.12f, 0.08f, 0.18f, 0.82f);
    private Color uiPanelTextColor = new Color(0.11f, 0.075f, 0.13f, 1f);
    private Color uiDialogueTextColor = new Color(0.16f, 0.09f, 0.17f, 1f);
    private Color uiDialogueSpeakerColor = new Color(0.37f, 0.17f, 0.42f, 1f);
    private AudioSource bgmSource;
    private AudioClip bgmClip;
    private bool bgmLoadAttempted;

    private string SaveDirectory
    {
        get { return Path.Combine(Application.persistentDataPath, SaveFolderName); }
    }

    private string GetSavePath(int slot)
    {
        return Path.Combine(SaveDirectory, "save_" + Mathf.Clamp(slot, 1, SaveSlotCount).ToString("00") + ".json");
    }

    private string LastSessionPath
    {
        get { return Path.Combine(SaveDirectory, "last_session.json"); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindObjectOfType<GalTemplateRuntime>() != null)
        {
            return;
        }

        GameObject runtimeObject = new GameObject("GAL Template Runtime");
        DontDestroyOnLoad(runtimeObject);
        runtimeObject.AddComponent<GalTemplateRuntime>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;
        LoadSettings();
        EnsureBgmSource();
        LoadStory();
        BuildUi();
        ApplySettings();
        GalFbxSceneController.Instance.CharacterDialogueRequested += HandleFbxCharacterDialogueRequested;
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            GalFbxSceneController controller = FindObjectOfType<GalFbxSceneController>();
            if (controller != null)
            {
                controller.CharacterDialogueRequested -= HandleFbxCharacterDialogueRequested;
            }

            if (titleSaturationMaterial != null)
            {
                Destroy(titleSaturationMaterial);
                titleSaturationMaterial = null;
            }

            StopBgm();
        }
    }

    private void OnApplicationQuit()
    {
        SaveLastSession();
    }

    private void Update()
    {
        CheckHotReload();
        UpdateTitleMenuPointer();

        if (GalFbxSceneController.IsSceneActive)
        {
            UpdateExternalSceneInput();
            return;
        }

        if (isSettingsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitOverlayPages();
            }

            return;
        }

        if (isSaveLoadOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                ExitOverlayPages();
            }

            return;
        }

        if (historyRoot != null && historyRoot.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.H) || Input.GetMouseButtonDown(1))
            {
                ExitOverlayPages();
            }

            return;
        }

        if (portraitDebugRoot != null && portraitDebugRoot.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Input.GetMouseButtonDown(1))
            {
                ExitOverlayPages();
            }

            return;
        }

        if (isInGame && Input.GetKeyDown(KeyCode.Escape))
        {
            ShowMainMenu();
            return;
        }

        if (!isInGame)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            ToggleAutoMode();
        }

        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            ToggleSkipMode();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                SaveGame();
            }
            else
            {
                ShowSavePanel();
            }
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                LoadLatestGame();
            }
            else
            {
                ShowLoadPanel();
            }
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            ShowHistory();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ShowPortraitDebug();
        }

        if (Input.GetMouseButtonDown(1))
        {
            ToggleDialogueHidden();
        }

        if (isExploring || currentNode == null || isAwaitingChoice || isDialogueHidden)
        {
            return;
        }

        bool pressedContinue = Input.GetKeyDown(KeyCode.Space) || (Input.GetMouseButtonDown(0) && !IsPointerOverInteractiveUi());
        if (Time.frameCount == externalSceneDialogueOpenedFrame)
        {
            pressedContinue = false;
        }

        if (pressedContinue)
        {
            ContinueStory();
        }
    }

    private void UpdateExternalSceneInput()
    {
        bool overlayOpen = IsOverlayPageOpen();
        GalFbxSceneController.Instance.SetControlsEnabled(!overlayOpen && !isExternalSceneDialogue && !IsPointerOverInteractiveUi());

        if (overlayOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                ExitOverlayPages();
            }

            return;
        }

        if (isExternalSceneDialogue)
        {
            UpdateExternalSceneDialogueInput();
            return;
        }

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                SaveGame();
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                LoadLatestGame();
            }
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            ShowLoadPanel();
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            ShowHistory();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitFbxScene();
        }
    }

    private void UpdateExternalSceneDialogueInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndExternalSceneDialogue();
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            ToggleDialogueHidden();
            return;
        }

        if (isDialogueHidden || currentNode == null || isAwaitingChoice)
        {
            return;
        }

        bool pressedContinue = Input.GetKeyDown(KeyCode.Space) || (Input.GetMouseButtonDown(0) && !IsPointerOverInteractiveUi());
        if (pressedContinue)
        {
            ContinueStory();
        }
    }

    private bool IsOverlayPageOpen()
    {
        return isSettingsOpen ||
            isSaveLoadOpen ||
            (historyRoot != null && historyRoot.activeSelf) ||
            (portraitDebugRoot != null && portraitDebugRoot.activeSelf);
    }

    private void QuitGame()
    {
        SaveLastSession();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void StartNewGame()
    {
        flags.Clear();
        inventory.Clear();
        readNodes.Clear();
        history.Clear();
        currentNode = null;
        currentNodeId = null;
        currentBackgroundId = null;
        if (portraitController != null)
        {
            portraitController.HideAll();
        }

        isInGame = true;
        isExploring = false;
        isAutoMode = false;
        isSkipMode = false;
        isDialogueHidden = false;
        mainMenuRoot.SetActive(false);
        hudRoot.SetActive(true);
        exploreRoot.SetActive(false);
        dialogueRoot.SetActive(true);
        ClearChoices();
        RefreshModeLabels();
        SetBackground(story.defaultBackground);
        PlayNode(story.startNode);
    }

    public void ContinueFromSave()
    {
        if (!LoadLatestGame())
        {
            ShowToast(T("ui.toast.no_save_start_new", "没有找到可读取的存档，已开始新游戏。"));
            StartNewGame();
        }
    }

    public void SaveGame()
    {
        SaveGameToSlot(QuickSaveSlot);
    }

    public void SaveGameToSlot(int slot)
    {
        if (!isInGame || string.IsNullOrEmpty(currentNodeId))
        {
            ShowToast(T("ui.toast.no_progress_to_save", "当前没有可保存的进度。"));
            return;
        }

        Directory.CreateDirectory(SaveDirectory);
        GalTemplateSaveData data = CreateSaveData(false);

        File.WriteAllText(GetSavePath(slot), JsonUtility.ToJson(data, true), Encoding.UTF8);
        RefreshMenuState();
        RefreshSaveLoadPanel();
        ShowToast(string.Format(T("ui.toast.saved_slot", "已保存到槽位 {0}。"), slot));
    }

    public bool LoadGame()
    {
        return LoadLatestGame();
    }

    public bool LoadLatestGame()
    {
        if (LoadLastSession())
        {
            return true;
        }

        int latestSlot = FindLatestSaveSlot();
        if (latestSlot < 1)
        {
            return false;
        }

        return LoadGameFromSlot(latestSlot);
    }

    public bool LoadGameFromSlot(int slot)
    {
        EndExternalSceneDialogue();
        string path = GetSavePath(slot);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            GalTemplateSaveData data = JsonUtility.FromJson<GalTemplateSaveData>(json);
            if (!LoadGameData(data, false))
            {
                return false;
            }

            ShowToast(string.Format(T("ui.toast.loaded_slot", "已读取槽位 {0}。"), slot));
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load save: " + exception.Message);
            return false;
        }
    }

    private GalTemplateSaveData CreateSaveData(bool includeExternalScene)
    {
        GalTemplateSaveData data = new GalTemplateSaveData();
        data.isExploring = isExternalSceneDialogue ? externalSceneReturnWasExploring : isExploring;
        data.currentNodeId = isExternalSceneDialogue ? externalSceneReturnNodeId : currentNodeId;
        data.currentBackgroundId = currentBackgroundId;
        data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.flags.AddRange(flags);
        data.inventory.AddRange(inventory);
        data.readNodes.AddRange(readNodes);

        if (includeExternalScene && GalFbxSceneController.IsSceneActive)
        {
            data.isExternalScene = true;
            data.externalSceneResourcePath = GalFbxSceneController.Instance.ActiveResourcePath;
            data.externalScenePixelSize = GalFbxSceneController.Instance.ActivePixelSize;
        }

        return data;
    }

    private void SaveLastSession()
    {
        if (!isInGame || string.IsNullOrEmpty(currentNodeId))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllText(LastSessionPath, JsonUtility.ToJson(CreateSaveData(true), true), Encoding.UTF8);
            RefreshMenuState();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to save last GAL session: " + exception.Message);
        }
    }

    private bool LoadLastSession()
    {
        EndExternalSceneDialogue();
        if (!File.Exists(LastSessionPath))
        {
            return false;
        }

        try
        {
            GalTemplateSaveData data = JsonUtility.FromJson<GalTemplateSaveData>(File.ReadAllText(LastSessionPath, Encoding.UTF8));
            return LoadGameData(data, true);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load last GAL session: " + exception.Message);
            return false;
        }
    }

    private bool LoadGameData(GalTemplateSaveData data, bool allowExternalSceneRestore)
    {
        if (data == null || string.IsNullOrEmpty(data.currentNodeId))
        {
            return false;
        }

        bool wasExternalScene = GalFbxSceneController.IsSceneActive;
        flags.Clear();
        inventory.Clear();
        readNodes.Clear();
        history.Clear();
        if (data.flags != null)
        {
            foreach (string flag in data.flags)
            {
                AddNonEmpty(flags, flag);
            }
        }

        if (data.inventory != null)
        {
            foreach (string item in data.inventory)
            {
                AddNonEmpty(inventory, item);
            }
        }

        if (data.readNodes != null)
        {
            foreach (string nodeId in data.readNodes)
            {
                AddNonEmpty(readNodes, nodeId);
            }
        }

        isInGame = true;
        isExploring = data.isExploring;
        currentNode = null;
        currentNodeId = data.currentNodeId;
        currentLine = string.Empty;
        isAutoMode = false;
        isSkipMode = false;
        isDialogueHidden = false;
        mainMenuRoot.SetActive(false);
        hudRoot.SetActive(true);
        exploreRoot.SetActive(false);
        dialogueRoot.SetActive(!isExploring);
        ClearChoices();
        RefreshModeLabels();
        SetBackground(string.IsNullOrEmpty(data.currentBackgroundId) ? story.defaultBackground : data.currentBackgroundId);
        if (portraitController != null)
        {
            portraitController.HideAll();
        }

        if (isExploring)
        {
            ShowExplore();
        }
        else
        {
            PlayNode(data.currentNodeId);
        }

        ExitOverlayPages();

        if (allowExternalSceneRestore && data.isExternalScene)
        {
            string resourcePath = string.IsNullOrEmpty(data.externalSceneResourcePath) ? GalFbxSceneController.DefaultSceneResourcePath : data.externalSceneResourcePath;
            float pixelSize = data.externalScenePixelSize > 0f ? data.externalScenePixelSize : 0f;
            if (wasExternalScene)
            {
                SetGalSceneLayersVisible(false);
                SetExternalSceneHudVisible(true);
            }
            else
            {
                GalFbxSceneController.Instance.Enter(resourcePath, pixelSize, HideGalForExternalScene);
            }
        }
        else if (wasExternalScene)
        {
            SetExternalSceneHudVisible(false);
            GalFbxSceneController.Instance.Exit(delegate
            {
                SetGalSceneLayersVisible(true);
            });
        }

        return true;
    }

    public bool HasSave()
    {
        return HasLastSession() || FindLatestSaveSlot() > 0;
    }

    private bool HasLastSession()
    {
        return File.Exists(LastSessionPath);
    }

    public void DeleteSave()
    {
        DeleteAllSaves();
    }

    public void DeleteSaveSlot(int slot)
    {
        string path = GetSavePath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        RefreshMenuState();
        RefreshSaveLoadPanel();
        ShowToast(string.Format(T("ui.toast.deleted_slot", "槽位 {0} 已删除。"), slot));
    }

    public void DeleteAllSaves()
    {
        for (int slot = 1; slot <= SaveSlotCount; slot++)
        {
            string path = GetSavePath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        if (File.Exists(LastSessionPath))
        {
            File.Delete(LastSessionPath);
        }

        RefreshMenuState();
        RefreshSaveLoadPanel();
        ShowToast(T("ui.toast.all_saves_deleted", "本地存档已删除。"));
    }

    private int FindLatestSaveSlot()
    {
        int latestSlot = -1;
        DateTime latestTime = DateTime.MinValue;

        for (int slot = 1; slot <= SaveSlotCount; slot++)
        {
            string path = GetSavePath(slot);
            if (!File.Exists(path))
            {
                continue;
            }

            DateTime writeTime = File.GetLastWriteTime(path);
            if (writeTime > latestTime)
            {
                latestTime = writeTime;
                latestSlot = slot;
            }
        }

        return latestSlot;
    }

    public bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
    }

    public void SetFlag(string flag)
    {
        AddNonEmpty(flags, flag);
    }

    public void RemoveFlag(string flag)
    {
        if (!string.IsNullOrEmpty(flag))
        {
            flags.Remove(flag);
        }
    }

    public bool HasItem(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && inventory.Contains(itemId);
    }

    public void AddItem(string itemId)
    {
        AddNonEmpty(inventory, itemId);
    }

    public void RemoveItem(string itemId)
    {
        if (!string.IsNullOrEmpty(itemId))
        {
            inventory.Remove(itemId);
        }
    }

    private void ContinueStory()
    {
        if (isTyping)
        {
            FinishTypingImmediately();
            return;
        }

        if (currentNode == null)
        {
            if (isExternalSceneDialogue)
            {
                EndExternalSceneDialogue();
            }

            return;
        }

        CancelAutoAdvance();

        if (!TryExecuteCurrentNodeCommands(out string jumpNode))
        {
            return;
        }

        if (!string.IsNullOrEmpty(jumpNode))
        {
            PlayNode(jumpNode);
            return;
        }

        List<GalStoryChoice> availableChoices = GetAvailableChoices(currentNode);
        if (availableChoices.Count > 0)
        {
            ShowChoices(availableChoices);
            return;
        }

        AdvanceFromNode(currentNode);
    }

    private void PlayNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || !nodesById.TryGetValue(nodeId, out GalStoryNode node))
        {
            Debug.LogWarning("Missing story node: " + nodeId);
            EndStory();
            return;
        }

        currentNode = node;
        currentNodeId = node.id;
        currentNodeCommandsExecuted = false;
        currentNodeWasReadBefore = readNodes.Contains(node.id);
        readNodes.Add(node.id);
        AddHistory(node);
        isExploring = false;
        isDialogueHidden = false;
        isAwaitingChoice = false;
        exploreRoot.SetActive(false);
        ClearChoices();

        if (!string.IsNullOrEmpty(node.background))
        {
            SetBackground(node.background);
        }

        ApplyNodePortrait(node);
        speakerText.text = string.IsNullOrEmpty(node.speaker) ? " " : node.speaker;
        currentLine = node.text ?? string.Empty;
        dialogueRoot.SetActive(true);
        StartTyping(currentLine);
    }

    private void AdvanceFromNode(GalStoryNode node)
    {
        if (!TryExecuteCurrentNodeCommands(out string jumpNode))
        {
            return;
        }

        if (!string.IsNullOrEmpty(jumpNode))
        {
            PlayNode(jumpNode);
            return;
        }

        if (!string.IsNullOrEmpty(node.nextId))
        {
            PlayNode(node.nextId);
            return;
        }

        if (isExternalSceneDialogue)
        {
            EndExternalSceneDialogue();
            return;
        }

        EndStory();
    }

    private bool TryExecuteCurrentNodeCommands(out string jumpNode)
    {
        jumpNode = null;
        if (currentNodeCommandsExecuted)
        {
            return true;
        }

        currentNodeCommandsExecuted = true;
        return ExecuteCommands(currentNode.commands, out jumpNode);
    }

    private bool ExecuteCommands(List<GalStoryCommand> commands, out string jumpNode)
    {
        jumpNode = null;
        if (commands == null)
        {
            return true;
        }

        foreach (GalStoryCommand command in commands)
        {
            if (command == null || string.IsNullOrEmpty(command.command))
            {
                continue;
            }

            string commandName = command.command.Trim().ToLowerInvariant();
            string target = FirstNonEmpty(command.value, command.key);

            switch (commandName)
            {
                case "set_flag":
                    SetFlag(command.key);
                    break;
                case "remove_flag":
                    RemoveFlag(command.key);
                    break;
                case "add_item":
                    AddItem(command.key);
                    break;
                case "remove_item":
                    RemoveItem(command.key);
                    break;
                case "set_background":
                    SetBackground(target);
                    break;
                case "portrait":
                case "show_portrait":
                    ShowPortrait(command);
                    break;
                case "hide_portrait":
                    HidePortrait(command);
                    break;
                case "hide_portraits":
                case "clear_portraits":
                    if (portraitController != null)
                    {
                        portraitController.HideAll();
                    }
                    break;
                case "portrait_animation":
                case "animate_portrait":
                    AnimatePortrait(command);
                    break;
                case "enter_fbx_scene":
                case "load_fbx_scene":
                    EnterFbxScene(command);
                    return false;
                case "exit_fbx_scene":
                    ExitFbxScene();
                    return false;
                case "jump":
                case "goto":
                    jumpNode = target;
                    break;
                case "save":
                    SaveGame();
                    break;
                case "show_explore":
                case "explore":
                    ShowExplore();
                    return false;
                case "hide_dialogue":
                    HideDialogueWindow();
                    break;
                case "show_dialogue":
                    ShowDialogueWindow();
                    break;
                case "auto":
                    SetAutoMode(target == "true" || target == "on" || target == "1");
                    break;
                case "skip":
                    SetSkipMode(target == "true" || target == "on" || target == "1");
                    break;
                case "settings":
                case "open_settings":
                    ShowSettings();
                    break;
                case "menu":
                case "main_menu":
                    ShowMainMenu();
                    return false;
                case "end":
                case "end_dialogue":
                    EndStory();
                    return false;
                default:
                    Debug.LogWarning("Unknown GAL command: " + command.command);
                    break;
            }
        }

        return true;
    }

    private bool IsPointerOverInteractiveUi()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == null)
            {
                continue;
            }

            if (result.gameObject.GetComponentInParent<Button>() != null ||
                result.gameObject.GetComponentInParent<Slider>() != null ||
                result.gameObject.GetComponentInParent<Toggle>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void ShowChoices(List<GalStoryChoice> choices)
    {
        isAwaitingChoice = true;
        continueHintText.text = T("ui.dialogue.choose", "请选择");
        ClearChoices();

        for (int i = 0; i < choices.Count; i++)
        {
            GalStoryChoice choice = choices[i];
            Button button = CreateButton(choiceContainer, choice.text, delegate { Choose(choice); }, out _);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 42f;
        }
    }

    private void Choose(GalStoryChoice choice)
    {
        isAwaitingChoice = false;
        ClearChoices();
        continueHintText.text = T("ui.dialogue.continue", "点击或空格继续");

        if (!ExecuteCommands(choice.commands, out string jumpNode))
        {
            return;
        }

        string targetNode = FirstNonEmpty(jumpNode, choice.nextId);
        if (!string.IsNullOrEmpty(targetNode))
        {
            PlayNode(targetNode);
        }
        else
        {
            ContinueStory();
        }
    }

    private void ApplyNodePortrait(GalStoryNode node)
    {
        if (portraitController == null || node == null || string.IsNullOrEmpty(node.portraitCharacter))
        {
            return;
        }

        portraitController.Show(new GalPortraitPose
        {
            slot = node.portraitSlot,
            character = node.portraitCharacter,
            expression = node.portraitExpression,
            facing = node.portraitFacing,
            animation = node.portraitAnimation,
            path = node.portraitPath
        });
    }

    private void ShowPortrait(GalStoryCommand command)
    {
        if (portraitController == null || command == null)
        {
            return;
        }

        portraitController.Show(new GalPortraitPose
        {
            slot = FirstNonEmpty(command.slot, command.key),
            character = FirstNonEmpty(command.character, command.value),
            expression = command.expression,
            facing = command.facing,
            animation = command.animation,
            path = command.path
        });
    }

    private void HidePortrait(GalStoryCommand command)
    {
        if (portraitController == null)
        {
            return;
        }

        portraitController.Hide(command == null ? null : FirstNonEmpty(command.slot, command.key));
    }

    private void AnimatePortrait(GalStoryCommand command)
    {
        if (portraitController == null || command == null)
        {
            return;
        }

        portraitController.PlayAnimation(FirstNonEmpty(command.slot, command.key), FirstNonEmpty(command.animation, command.value));
    }

    private void EnterFbxScene(GalStoryCommand command)
    {
        string resourcePath = command == null ? null : FirstNonEmpty(command.path, command.value);
        float pixelSize = command != null ? command.amount : 0f;
        GalFbxSceneController.Instance.Enter(resourcePath, pixelSize, HideGalForExternalScene);
    }

    private void ExitFbxScene()
    {
        EndExternalSceneDialogue();
        GalFbxSceneController.Instance.Exit(ShowGalAfterExternalScene);
    }

    private void ShowMainMenuFromExternalScene()
    {
        EndExternalSceneDialogue();
        if (!GalFbxSceneController.IsSceneActive)
        {
            ShowMainMenu();
            return;
        }

        CloseOverlayPages(true);
        SetExternalSceneHudVisible(false);
        GalFbxSceneController.Instance.Exit(delegate
        {
            SetGalSceneLayersVisible(true);
            ShowMainMenu();
        });
    }

    private void HideGalForExternalScene()
    {
        CancelAutoAdvance();
        if (canvas != null)
        {
            canvas.enabled = true;
        }

        SetGalSceneLayersVisible(false);
        if (mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(false);
        }

        if (hudRoot != null)
        {
            hudRoot.SetActive(false);
        }

        if (exploreRoot != null)
        {
            exploreRoot.SetActive(false);
        }

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(false);
        }

        if (portraitController != null)
        {
            portraitController.HideAll();
        }

        CloseOverlayPages(true);
        SetExternalSceneHudVisible(true);
    }

    private void ShowGalAfterExternalScene()
    {
        if (canvas != null)
        {
            canvas.enabled = true;
        }

        SetExternalSceneHudVisible(false);
        SetGalSceneLayersVisible(true);

        if (!isInGame)
        {
            ShowMainMenu();
            return;
        }

        if (hudRoot != null)
        {
            hudRoot.SetActive(true);
        }

        ShowExplore();
    }

    private void HandleFbxCharacterDialogueRequested(string characterId)
    {
        if (!GalFbxSceneController.IsSceneActive || isExternalSceneDialogue || IsOverlayPageOpen())
        {
            return;
        }

        if (!string.Equals(characterId, GalFbxSceneController.DefaultCharacterImageId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        StartExternalSceneDialogue(CreateUnauthorizedPassengerTestNode());
    }

    private void StartExternalSceneDialogue(GalStoryNode node)
    {
        if (node == null || dialogueRoot == null)
        {
            return;
        }

        FinishTypingForMenu();
        CancelAutoAdvance();
        externalSceneReturnNode = currentNode;
        externalSceneReturnNodeId = currentNodeId;
        externalSceneReturnWasExploring = isExploring;
        externalSceneReturnCommandsExecuted = currentNodeCommandsExecuted;
        externalSceneReturnWasReadBefore = currentNodeWasReadBefore;
        isExternalSceneDialogue = true;
        isExploring = false;
        isDialogueHidden = false;
        isAwaitingChoice = false;
        ClearChoices();

        if (fbxHudRoot != null)
        {
            fbxHudRoot.SetActive(true);
        }

        currentNode = node;
        currentNodeId = node.id;
        currentNodeCommandsExecuted = true;
        currentNodeWasReadBefore = true;
        externalSceneDialogueOpenedFrame = Time.frameCount;
        speakerText.text = string.IsNullOrEmpty(node.speaker) ? " " : node.speaker;
        currentLine = node.text ?? string.Empty;
        AddHistory(node);
        dialogueRoot.SetActive(true);
        StartTyping(currentLine);
    }

    private void EndExternalSceneDialogue()
    {
        if (!isExternalSceneDialogue)
        {
            return;
        }

        FinishTypingForMenu();
        CancelAutoAdvance();
        ClearChoices();
        isExternalSceneDialogue = false;
        isAwaitingChoice = false;
        isDialogueHidden = false;
        isExploring = externalSceneReturnWasExploring;
        currentNode = externalSceneReturnNode;
        currentNodeId = externalSceneReturnNodeId;
        currentNodeCommandsExecuted = externalSceneReturnCommandsExecuted;
        currentNodeWasReadBefore = externalSceneReturnWasReadBefore;
        currentLine = currentNode == null ? string.Empty : currentNode.text ?? string.Empty;
        externalSceneReturnNode = null;
        externalSceneReturnNodeId = null;
        externalSceneDialogueOpenedFrame = -1;

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(false);
        }

        if (GalFbxSceneController.IsSceneActive)
        {
            SetExternalSceneHudVisible(true);
            GalFbxSceneController.Instance.SetControlsEnabled(true);
        }
    }

    private GalStoryNode CreateUnauthorizedPassengerTestNode()
    {
        return new GalStoryNode
        {
            id = ExternalCharacterDialogueNodeId,
            speaker = GetUnauthorizedPassengerTestSpeaker(),
            text = GetUnauthorizedPassengerTestText()
        };
    }

    private string GetUnauthorizedPassengerTestSpeaker()
    {
        switch (settings.language)
        {
            case "en-US":
            case "en":
                return "Unauthorized Passenger";
            case "ja-JP":
            case "ja":
                return "未許可の乗客";
            default:
                return "未许客";
        }
    }

    private string GetUnauthorizedPassengerTestText()
    {
        switch (settings.language)
        {
            case "en-US":
            case "en":
                return "Test placeholder dialogue. You found the passenger peeking out from behind the seat.";
            case "ja-JP":
            case "ja":
                return "テスト用の仮テキストです。座席の陰から顔を出した乗客を見つけました。";
            default:
                return "测试占位文案。你发现了从座椅侧面探出头来的未许客。";
        }
    }

    private void SetGalSceneLayersVisible(bool visible)
    {
        if (backgroundRoot != null)
        {
            backgroundRoot.SetActive(visible);
        }

        if (backgroundWashRoot != null)
        {
            backgroundWashRoot.SetActive(visible);
        }
    }

    private void SetExternalSceneHudVisible(bool visible)
    {
        if (fbxHudRoot != null)
        {
            fbxHudRoot.SetActive(visible);
        }
    }

    private void ShowExplore()
    {
        FinishTypingForMenu();
        CancelAutoAdvance();
        isExploring = true;
        currentNode = null;
        isAwaitingChoice = false;
        isDialogueHidden = false;
        dialogueRoot.SetActive(false);
        exploreRoot.SetActive(true);
        if (portraitController != null)
        {
            portraitController.HideAll();
        }

        RebuildExploreButtons();
    }

    private void RebuildExploreButtons()
    {
        for (int i = exploreButtonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(exploreButtonContainer.GetChild(i).gameObject);
        }

        exploreTitleText.text = T("ui.explore.title", "选择调查地点");
        string activeScene = string.IsNullOrEmpty(currentBackgroundId) ? story.defaultBackground : currentBackgroundId;

        foreach (GalExplorePoint point in story.explorePoints)
        {
            bool hasCommand = point != null && point.commands != null && point.commands.Count > 0;
            if (point == null || string.IsNullOrEmpty(point.displayName) || (string.IsNullOrEmpty(point.nodeId) && string.IsNullOrEmpty(point.background) && !hasCommand))
            {
                continue;
            }

            string pointScene = string.IsNullOrEmpty(point.scene) ? FirstNonEmpty(point.background, activeScene) : point.scene;
            if (!string.IsNullOrEmpty(pointScene) && pointScene != activeScene)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(point.requiredFlag) && !HasFlag(point.requiredFlag) && !HasItem(point.requiredFlag))
            {
                continue;
            }

            CreateExploreHotspot(point);
        }
    }

    private void CreateExploreHotspot(GalExplorePoint point)
    {
        Button button = CreateButton(exploreButtonContainer, point.displayName, delegate { ChooseExplorePoint(point); }, out Text label);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        float width = point.width > 0f ? point.width : 170f;
        float height = point.height > 0f ? point.height : 48f;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;

        Image image = button.GetComponent<Image>();
        image.color = new Color(1f, 0.98f, 0.82f, 0.72f);

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 0.98f, 0.82f, 0.72f);
        colors.highlightedColor = new Color(0.75f, 0.94f, 0.78f, 0.95f);
        colors.pressedColor = new Color(0.48f, 0.68f, 0.52f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        label.fontSize = Mathf.RoundToInt(Mathf.Clamp(height * 0.42f, 16f, 22f));
        label.resizeTextMinSize = 12;
        label.color = new Color(0.06f, 0.06f, 0.055f, 1f);
    }

    private void ChooseExplorePoint(GalExplorePoint point)
    {
        if (isTransitioning)
        {
            return;
        }

        StartCoroutine(ChooseExplorePointRoutine(point));
    }

    private IEnumerator ChooseExplorePointRoutine(GalExplorePoint point)
    {
        if (point.commands != null && point.commands.Count > 0)
        {
            if (!ExecuteCommands(point.commands, out string jumpNode))
            {
                yield break;
            }

            if (!string.IsNullOrEmpty(jumpNode))
            {
                isExploring = false;
                exploreRoot.SetActive(false);
                PlayNode(jumpNode);
                yield break;
            }
        }

        string targetBackground = FirstNonEmpty(point.background, currentBackgroundId);
        if (string.IsNullOrEmpty(targetBackground))
        {
            targetBackground = story.defaultBackground;
        }
        isExploring = false;
        exploreRoot.SetActive(false);
        yield return TransitionToBackground(targetBackground, point.displayName);
        if (string.IsNullOrEmpty(point.nodeId))
        {
            isExploring = true;
            dialogueRoot.SetActive(false);
            exploreRoot.SetActive(true);
            RebuildExploreButtons();
            yield break;
        }

        PlayNode(point.nodeId);
    }

    private IEnumerator TransitionToBackground(string backgroundId, string caption)
    {
        if (string.IsNullOrEmpty(backgroundId) || backgroundId == currentBackgroundId || transitionImage == null)
        {
            SetBackground(backgroundId);
            yield break;
        }

        if (sceneTransitionRoutine != null)
        {
            StopCoroutine(sceneTransitionRoutine);
            sceneTransitionRoutine = null;
        }

        isTransitioning = true;
        transitionRoot.SetActive(true);
        transitionRoot.transform.SetAsLastSibling();
        transitionText.text = string.IsNullOrEmpty(caption) ? "切换场景" : caption;

        yield return FadeTransition(0f, 1f, 0.22f);
        SetBackground(backgroundId);
        yield return new WaitForSecondsRealtime(0.08f);
        yield return FadeTransition(1f, 0f, 0.28f);

        transitionRoot.SetActive(false);
        isTransitioning = false;
    }

    private IEnumerator FadeTransition(float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, Mathf.Clamp01(time / duration));
            SetTransitionAlpha(alpha);
            yield return null;
        }

        SetTransitionAlpha(to);
    }

    private void SetTransitionAlpha(float alpha)
    {
        Color imageColor = transitionImage.color;
        imageColor.a = alpha;
        transitionImage.color = imageColor;

        Color textColor = transitionText.color;
        textColor.a = alpha;
        transitionText.color = textColor;
    }

    private List<GalStoryChoice> GetAvailableChoices(GalStoryNode node)
    {
        List<GalStoryChoice> result = new List<GalStoryChoice>();
        if (node.choices == null)
        {
            return result;
        }

        foreach (GalStoryChoice choice in node.choices)
        {
            if (choice == null || string.IsNullOrEmpty(choice.text))
            {
                continue;
            }

            if (string.IsNullOrEmpty(choice.requiredFlag) || HasFlag(choice.requiredFlag) || HasItem(choice.requiredFlag))
            {
                result.Add(choice);
            }
        }

        return result;
    }

    private void EndStory()
    {
        isTyping = false;
        isAwaitingChoice = false;
        currentNode = null;
        currentLine = string.Empty;
        ClearChoices();
        ShowExplore();
    }

    private void StartTyping(string line)
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        CancelAutoAdvance();
        typingRoutine = StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = string.Empty;
        continueHintText.text = T("ui.dialogue.typing", "显示中...");

        if (isSkipMode && (settings.skipUnreadText || currentNodeWasReadBefore))
        {
            dialogueText.text = line;
            isTyping = false;
            typingRoutine = null;
            continueHintText.text = T("ui.dialogue.skipping", "跳过中");
            QueueAutoOrSkipAdvance();
            yield break;
        }

        float secondsPerCharacter = 1f / Mathf.Max(1f, settings.textSpeed);
        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            yield return new WaitForSecondsRealtime(secondsPerCharacter);
        }

        isTyping = false;
        typingRoutine = null;
        continueHintText.text = T("ui.dialogue.continue", "点击或空格继续");
        QueueAutoOrSkipAdvance();
    }

    private void FinishTypingImmediately()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        dialogueText.text = currentLine;
        isTyping = false;
        continueHintText.text = T("ui.dialogue.continue", "点击或空格继续");
        QueueAutoOrSkipAdvance();
    }

    private void QueueAutoOrSkipAdvance()
    {
        CancelAutoAdvance();

        if (!isInGame || isExploring || isAwaitingChoice || isDialogueHidden || currentNode == null)
        {
            return;
        }

        if (isSkipMode)
        {
            if (settings.skipUnreadText || currentNodeWasReadBefore)
            {
                autoRoutine = StartCoroutine(AutoContinueAfter(0.05f));
                return;
            }

            SetSkipMode(false);
        }

        if (isAutoMode)
        {
            autoRoutine = StartCoroutine(AutoContinueAfter(settings.autoDelay));
        }
    }

    private IEnumerator AutoContinueAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, delay));

        if (isInGame && !isExploring && !isAwaitingChoice && !isTyping && !isDialogueHidden && currentNode != null)
        {
            ContinueStory();
        }

        autoRoutine = null;
    }

    private void CancelAutoAdvance()
    {
        if (autoRoutine != null)
        {
            StopCoroutine(autoRoutine);
            autoRoutine = null;
        }
    }

    private void ToggleAutoMode()
    {
        SetAutoMode(!isAutoMode);
    }

    private void SetAutoMode(bool value)
    {
        isAutoMode = value;
        if (isAutoMode)
        {
            isSkipMode = false;
        }

        RefreshModeLabels();
        QueueAutoOrSkipAdvance();
        ShowToast(isAutoMode ? T("ui.toast.auto_on", "自动播放：开") : T("ui.toast.auto_off", "自动播放：关"));
    }

    private void ToggleSkipMode()
    {
        SetSkipMode(!isSkipMode);
    }

    private void SetSkipMode(bool value)
    {
        isSkipMode = value;
        if (isSkipMode)
        {
            isAutoMode = false;
        }

        RefreshModeLabels();
        QueueAutoOrSkipAdvance();
        ShowToast(isSkipMode ? T("ui.toast.skip_on", "跳过：开") : T("ui.toast.skip_off", "跳过：关"));
    }

    private void RefreshModeLabels()
    {
        if (autoButtonLabel != null)
        {
            autoButtonLabel.text = isAutoMode ? T("ui.hud.auto_on", "自动中") : T("ui.hud.auto", "自动");
        }

        if (skipButtonLabel != null)
        {
            skipButtonLabel.text = isSkipMode ? T("ui.hud.skip_on", "跳过中") : T("ui.hud.skip", "跳过");
        }
    }

    private void ToggleDialogueHidden()
    {
        if (!isInGame || isExploring || currentNode == null)
        {
            return;
        }

        if (isDialogueHidden)
        {
            ShowDialogueWindow();
        }
        else
        {
            HideDialogueWindow();
        }
    }

    private void HideDialogueWindow()
    {
        if (dialogueRoot == null)
        {
            return;
        }

        isDialogueHidden = true;
        dialogueRoot.SetActive(false);
        CancelAutoAdvance();
    }

    private void ShowDialogueWindow()
    {
        if (dialogueRoot == null || isExploring)
        {
            return;
        }

        isDialogueHidden = false;
        dialogueRoot.SetActive(true);
        QueueAutoOrSkipAdvance();
    }

    private void AddHistory(GalStoryNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.text))
        {
            return;
        }

        history.Add(new GalHistoryLine
        {
            speaker = string.IsNullOrEmpty(node.speaker) ? "旁白" : node.speaker,
            text = node.text
        });

        if (history.Count > 100)
        {
            history.RemoveAt(0);
        }
    }

    private void ShowHistory()
    {
        ShowHistory(false);
    }

    private void ShowHistory(bool returnToSettings)
    {
        if (historyRoot == null)
        {
            return;
        }

        previousOverlayPage = returnToSettings ? GalOverlayPage.Settings : GalOverlayPage.None;
        CloseOverlayPages(true);
        StringBuilder builder = new StringBuilder();
        if (history.Count == 0)
        {
            builder.Append(T("ui.history.empty", "暂无历史文本。"));
        }
        else
        {
            foreach (GalHistoryLine line in history)
            {
                builder.Append(line.speaker);
                builder.Append("：");
                builder.AppendLine(line.text);
                builder.AppendLine();
            }
        }

        historyText.text = builder.ToString();
        currentOverlayPage = GalOverlayPage.History;
        historyRoot.SetActive(true);
        RefreshOverlayNavigationButtons();
    }

    private void HideHistory()
    {
        ExitOverlayPages();
    }

    private void ReturnToPreviousOverlayPage()
    {
        if (previousOverlayPage == GalOverlayPage.Settings)
        {
            previousOverlayPage = GalOverlayPage.None;
            ShowSettings();
            return;
        }

        ExitOverlayPages();
    }

    private void ExitOverlayPages()
    {
        previousOverlayPage = GalOverlayPage.None;
        currentOverlayPage = GalOverlayPage.None;
        CloseOverlayPages(true);
        RefreshMenuState();
    }

    private void RefreshOverlayNavigationButtons()
    {
        bool canReturn = previousOverlayPage != GalOverlayPage.None;
        if (saveLoadBackButton != null)
        {
            saveLoadBackButton.gameObject.SetActive(canReturn && currentOverlayPage == GalOverlayPage.SaveLoad);
        }

        if (historyBackButton != null)
        {
            historyBackButton.gameObject.SetActive(canReturn && currentOverlayPage == GalOverlayPage.History);
        }

        if (portraitDebugBackButtonLabel != null)
        {
            portraitDebugBackButtonLabel.transform.parent.gameObject.SetActive(canReturn && currentOverlayPage == GalOverlayPage.PortraitDebug);
        }

        if (characterSettingsBackButton != null)
        {
            characterSettingsBackButton.gameObject.SetActive(canReturn && currentOverlayPage == GalOverlayPage.CharacterSettings);
        }
    }

    private void CloseOverlayPages(bool includeSettings = true)
    {
        if (includeSettings && settingsRoot != null)
        {
            settingsRoot.SetActive(false);
            isSettingsOpen = false;
        }

        if (saveLoadRoot != null)
        {
            saveLoadRoot.SetActive(false);
            isSaveLoadOpen = false;
        }

        if (historyRoot != null)
        {
            historyRoot.SetActive(false);
        }

        if (portraitDebugRoot != null)
        {
            portraitDebugRoot.SetActive(false);
        }

        if (characterSettingsRoot != null)
        {
            characterSettingsRoot.SetActive(false);
        }

        currentOverlayPage = GalOverlayPage.None;
        RefreshOverlayNavigationButtons();
    }

    private void LoadStory()
    {
        textureCache.Clear();
        storyPath = Path.Combine(Application.streamingAssetsPath, StoryRelativePath);
        if (!File.Exists(storyPath))
        {
            story = CreateFallbackStory();
            Debug.LogWarning("Story JSON not found, using fallback story: " + storyPath);
        }
        else
        {
            string json = File.ReadAllText(storyPath, Encoding.UTF8);
            story = JsonUtility.FromJson<GalStoryFile>(json);
            if (story == null)
            {
                story = CreateFallbackStory();
            }
        }

        NormalizeStory();
        LoadTextTable();
        ApplyTextTable();
        storyLastWriteTimeUtc = GetFileWriteTimeUtc(storyPath);
        textTableLastWriteTimeUtc = GetFileWriteTimeUtc(textTablePath);
    }

    private void CheckHotReload()
    {
        hotReloadTimer += Time.unscaledDeltaTime;
        if (hotReloadTimer < 1f)
        {
            return;
        }

        hotReloadTimer = 0f;
        if (string.IsNullOrEmpty(storyPath))
        {
            return;
        }

        if (GetFileWriteTimeUtc(storyPath) == storyLastWriteTimeUtc && GetFileWriteTimeUtc(textTablePath) == textTableLastWriteTimeUtc)
        {
            return;
        }

        ReloadStoryFilesInPlace();
    }

    private void ReloadStoryFilesInPlace()
    {
        string nodeId = currentNodeId;
        bool wasInGame = isInGame;
        bool wasExploring = isExploring;

        LoadStory();
        RefreshLocalizedUi();
        RefreshTitleLogoLanguage();
        if (portraitController != null)
        {
            portraitController.Configure(story.portraits);
        }

        if (!wasInGame)
        {
            RefreshTitleText();
            SetBackground(string.IsNullOrEmpty(currentBackgroundId) ? story.defaultBackground : currentBackgroundId);
            ShowToast(T("ui.toast.reloaded_story", "已热重载剧本表。"));
            return;
        }

        if (wasExploring)
        {
            RebuildExploreButtons();
            ShowToast(T("ui.toast.reloaded_explore", "已热重载探索点。"));
            return;
        }

        if (!string.IsNullOrEmpty(nodeId) && nodesById.TryGetValue(nodeId, out GalStoryNode node))
        {
            currentNode = node;
            speakerText.text = string.IsNullOrEmpty(node.speaker) ? " " : node.speaker;
            currentLine = node.text ?? string.Empty;
            ApplyNodePortrait(node);
            if (isTyping)
            {
                FinishTypingImmediately();
            }
            else
            {
                dialogueText.text = currentLine;
            }

            if (isAwaitingChoice)
            {
                ShowChoices(GetAvailableChoices(node));
            }
        }

        ShowToast(T("ui.toast.reloaded_text", "已热重载文案。"));
    }

    private void NormalizeStory()
    {
        if (story.backgrounds == null)
        {
            story.backgrounds = new List<GalBackgroundEntry>();
        }

        if (story.nodes == null)
        {
            story.nodes = new List<GalStoryNode>();
        }

        if (story.explorePoints == null)
        {
            story.explorePoints = new List<GalExplorePoint>();
        }

        if (story.artProfiles == null)
        {
            story.artProfiles = new List<GalArtProfile>();
        }

        if (story.portraits == null)
        {
            story.portraits = new List<GalPortraitEntry>();
        }

        if (story.languages == null)
        {
            story.languages = new List<GalLanguageEntry>();
        }

        backgroundsById.Clear();
        foreach (GalBackgroundEntry background in story.backgrounds)
        {
            if (background != null && !string.IsNullOrEmpty(background.id))
            {
                backgroundsById[background.id] = background;
            }
        }

        explorePointsById.Clear();
        foreach (GalExplorePoint point in story.explorePoints)
        {
            if (point != null && !string.IsNullOrEmpty(point.id))
            {
                explorePointsById[point.id] = point;
            }
        }

        nodesById.Clear();
        foreach (GalStoryNode node in story.nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.id))
            {
                continue;
            }

            if (node.choices == null)
            {
                node.choices = new List<GalStoryChoice>();
            }

            if (node.commands == null)
            {
                node.commands = new List<GalStoryCommand>();
            }

            nodesById[node.id] = node;
        }
    }

    private void LoadTextTable()
    {
        rawTextRowsByKey.Clear();
        textEntriesByKey.Clear();
        string configuredPath = GetConfiguredTextTablePath();
        string relativePath = string.IsNullOrEmpty(configuredPath) ? DefaultTextTableRelativePath : configuredPath;
        textTablePath = Path.IsPathRooted(relativePath) ? relativePath : Path.Combine(Application.streamingAssetsPath, "GAL", relativePath);

        if (!File.Exists(textTablePath))
        {
            return;
        }

        List<string[]> rows = ParseCsv(File.ReadAllText(textTablePath, Encoding.UTF8));
        if (rows.Count == 0)
        {
            return;
        }

        Dictionary<string, int> headerIndexes = new Dictionary<string, int>();
        for (int i = 0; i < rows[0].Length; i++)
        {
            string header = rows[0][i].Trim().TrimStart('\uFEFF');
            if (!string.IsNullOrEmpty(header))
            {
                headerIndexes[header] = i;
            }
        }

        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];
            string key = GetCsvValue(row, headerIndexes, "key");
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            GalRawTextRow entry = new GalRawTextRow();
            entry.key = key;
            foreach (KeyValuePair<string, int> pair in headerIndexes)
            {
                if (pair.Key == "key" || pair.Key == "category" || pair.Key == "description" || pair.Key == "note")
                {
                    continue;
                }

                entry.values[pair.Key] = GetCsvValue(row, headerIndexes, pair.Key);
            }

            rawTextRowsByKey[key] = entry;
        }

        BuildLocalizedTextEntries();
    }

    private void BuildLocalizedTextEntries()
    {
        textEntriesByKey.Clear();
        foreach (GalRawTextRow row in rawTextRowsByKey.Values)
        {
            GalTextEntry entry = new GalTextEntry();
            entry.key = row.key;
            entry.speaker = GetLocalizedCell(row, "speaker");
            entry.text = GetLocalizedCell(row, "text");
            entry.portraitSlot = GetLocalizedCell(row, "portrait_slot");
            entry.portraitCharacter = GetLocalizedCell(row, "portrait_character");
            entry.portraitExpression = GetLocalizedCell(row, "portrait_expression");
            entry.portraitFacing = GetLocalizedCell(row, "portrait_facing");
            entry.portraitAnimation = GetLocalizedCell(row, "portrait_animation");
            entry.portraitPath = GetLocalizedCell(row, "portrait_path");
            textEntriesByKey[row.key] = entry;
        }
    }

    private string GetConfiguredTextTablePath()
    {
        string languageId = string.IsNullOrEmpty(settings.language) ? "zh-CN" : settings.language;
        if (story != null && story.languages != null)
        {
            foreach (GalLanguageEntry language in story.languages)
            {
                if (language != null && language.id == languageId && !string.IsNullOrEmpty(language.textTable))
                {
                    return language.textTable;
                }
            }
        }

        return story != null ? story.textTable : null;
    }

    private string GetLocalizedCell(GalRawTextRow row, string field)
    {
        string language = string.IsNullOrEmpty(settings.language) ? "zh-CN" : settings.language;
        string value = GetRawTextValue(row, language + "." + field);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        value = GetRawTextValue(row, "zh-CN." + field);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        return GetRawTextValue(row, field);
    }

    private static string GetRawTextValue(GalRawTextRow row, string column)
    {
        return row.values.TryGetValue(column, out string value) ? value : string.Empty;
    }

    private string T(string key, string fallback)
    {
        if (textEntriesByKey.TryGetValue(key, out GalTextEntry entry) && !string.IsNullOrEmpty(entry.text))
        {
            return entry.text;
        }

        return fallback;
    }

    private void ApplyTextTable()
    {
        if (textEntriesByKey.Count == 0)
        {
            return;
        }

        if (textEntriesByKey.TryGetValue("game.title", out GalTextEntry titleEntry) && !string.IsNullOrEmpty(titleEntry.text))
        {
            story.title = titleEntry.text;
        }

        foreach (GalStoryNode node in story.nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.id))
            {
                continue;
            }

            if (textEntriesByKey.TryGetValue("node." + node.id, out GalTextEntry nodeEntry))
            {
                if (!string.IsNullOrEmpty(nodeEntry.speaker))
                {
                    node.speaker = nodeEntry.speaker;
                }

                if (!string.IsNullOrEmpty(nodeEntry.text))
                {
                    node.text = nodeEntry.text;
                }

                if (!string.IsNullOrEmpty(nodeEntry.portraitSlot))
                {
                    node.portraitSlot = nodeEntry.portraitSlot;
                }

                if (!string.IsNullOrEmpty(nodeEntry.portraitCharacter))
                {
                    node.portraitCharacter = nodeEntry.portraitCharacter;
                }

                if (!string.IsNullOrEmpty(nodeEntry.portraitExpression))
                {
                    node.portraitExpression = nodeEntry.portraitExpression;
                }

                if (!string.IsNullOrEmpty(nodeEntry.portraitFacing))
                {
                    node.portraitFacing = nodeEntry.portraitFacing;
                }

                if (!string.IsNullOrEmpty(nodeEntry.portraitAnimation))
                {
                    node.portraitAnimation = nodeEntry.portraitAnimation;
                }

                if (!string.IsNullOrEmpty(nodeEntry.portraitPath))
                {
                    node.portraitPath = nodeEntry.portraitPath;
                }
            }

            if (node.choices == null)
            {
                continue;
            }

            for (int i = 0; i < node.choices.Count; i++)
            {
                GalStoryChoice choice = node.choices[i];
                if (choice == null)
                {
                    continue;
                }

                string choiceKey = string.IsNullOrEmpty(choice.id) ? "choice." + node.id + "." + (i + 1).ToString("00") : "choice." + choice.id;
                if (textEntriesByKey.TryGetValue(choiceKey, out GalTextEntry choiceEntry) && !string.IsNullOrEmpty(choiceEntry.text))
                {
                    choice.text = choiceEntry.text;
                }
            }
        }

        foreach (GalExplorePoint point in story.explorePoints)
        {
            if (point == null || string.IsNullOrEmpty(point.id))
            {
                continue;
            }

            if (textEntriesByKey.TryGetValue("explore." + point.id, out GalTextEntry pointEntry) && !string.IsNullOrEmpty(pointEntry.text))
            {
                point.displayName = pointEntry.text;
            }
        }
    }

    private void ApplyLanguageToRuntime()
    {
        BuildLocalizedTextEntries();
        ApplyTextTable();
        RefreshLocalizedUi();
        RefreshTitleLogoLanguage();

        if (isExploring)
        {
            RebuildExploreButtons();
        }
        else if (currentNode != null)
        {
            if (nodesById.TryGetValue(currentNodeId, out GalStoryNode node))
            {
                currentNode = node;
                speakerText.text = string.IsNullOrEmpty(node.speaker) ? " " : node.speaker;
                currentLine = node.text ?? string.Empty;
                if (isTyping)
                {
                    FinishTypingImmediately();
                }
                else
                {
                    dialogueText.text = currentLine;
                }

                if (isAwaitingChoice)
                {
                    ShowChoices(GetAvailableChoices(node));
                }
            }
        }
    }

    private void RefreshLocalizedUi()
    {
        if (menuTitleText != null)
        {
            RefreshTitleText();
        }

        SetText(newGameButtonLabel, T("ui.main.new_game", "新游戏"));
        SetText(mainMenuSettingsButtonLabel, T("ui.common.settings", "设置"));
        SetText(quitButtonLabel, T("ui.common.quit", "退出"));

        SetText(hudSaveButtonLabel, T("ui.hud.save", "存档"));
        SetText(hudLoadButtonLabel, T("ui.hud.load", "读档"));
        SetText(hudHideButtonLabel, T("ui.hud.hide", "隐藏"));
        SetText(hudHistoryButtonLabel, T("ui.hud.history", "历史"));
        SetText(hudSettingsButtonLabel, T("ui.common.settings", "设置"));
        SetText(hudDebugButtonLabel, T("ui.hud.portrait_debug", "立绘"));
        SetText(hudTitleButtonLabel, T("ui.hud.title", "标题"));
        RefreshModeLabels();

        SetText(historyTitleText, T("ui.history.title", "历史文本"));
        SetText(historyBackButtonLabel, T("ui.common.back", "返回"));
        SetText(historyExitButtonLabel, T("ui.common.exit", "退出"));

        SetText(saveLoadBackButtonLabel, T("ui.common.back", "返回"));
        SetText(saveLoadExitButtonLabel, T("ui.common.exit", "退出"));
        RefreshSaveLoadPanel();

        SetText(settingsTitleText, T("ui.common.settings", "设置"));
        SetText(settingsTextSpeedLabel, T("ui.settings.text_speed", "文本速度"));
        SetText(settingsAutoDelayLabel, T("ui.settings.auto_delay", "自动间隔"));
        SetText(settingsVolumeLabel, T("ui.settings.volume", "主音量"));
        SetText(settingsBgmVolumeLabel, T("ui.settings.bgm_volume", "音乐音量"));
        SetText(settingsFbxCameraHeightLabel, T("ui.settings.camera_height", "摄像头高度"));
        SetText(settingsCabinMoodLabel, T("ui.settings.cabin_mood", "氛围强度"));
        SetText(settingsTitleSaturationLabel, T("ui.settings.title_saturation", "标题饱和度"));
        SetText(settingsFullscreenLabel, T("ui.settings.fullscreen", "全屏显示"));
        SetText(settingsSkipUnreadLabel, T("ui.settings.skip_unread", "允许跳过未读文本"));
        SetText(languageValueText, GetLanguageButtonText());
        SetText(settingsSavePanelButtonLabel, T("ui.settings.open_save", "打开存档"));
        SetText(settingsLoadPanelButtonLabel, T("ui.settings.open_load", "打开读档"));
        SetText(settingsHistoryButtonLabel, T("ui.settings.open_history", "查看历史"));
        SetText(settingsReloadButtonLabel, T("ui.settings.reload_text", "重载文案"));
        SetText(settingsDeleteButtonLabel, T("ui.settings.delete_save", "删除存档"));
        SetText(settingsDebugButtonLabel, T("ui.settings.portrait_debug", "立绘调试"));
        SetText(settingsCharacterButtonLabel, T("ui.settings.character", "角色配置"));
        SetText(settingsExitButtonLabel, T("ui.common.back", "返回"));
        RefreshCharacterSettingsLabels();
        RefreshPortraitDebugLabels();
        RefreshExternalSceneHudLabels();

        RefreshMenuState();
    }

    private void RefreshExternalSceneHudLabels()
    {
        SetText(fbxBackButtonLabel, T("ui.common.back", "Back"));
        SetText(fbxSaveButtonLabel, T("ui.hud.save", "Save"));
        SetText(fbxLoadButtonLabel, T("ui.hud.load", "Load"));
        SetText(fbxHistoryButtonLabel, T("ui.hud.history", "History"));
        SetText(fbxSettingsButtonLabel, T("ui.common.settings", "Settings"));
        SetText(fbxTitleButtonLabel, T("ui.hud.title", "Title"));
    }

    private static void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static void SetInputPlaceholder(InputField target, string value)
    {
        if (target != null && target.placeholder is Text placeholderText)
        {
            placeholderText.text = value;
        }
    }

    private static void SetButtonVisual(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            image.color = image.sprite == null
                ? selected ? new Color(0.96f, 0.84f, 1f, 0.78f) : UiGlassNormal
                : selected ? new Color(1f, 0.9f, 1f, 0.86f) : new Color(1f, 1f, 1f, 0.92f);
        }
    }

    private static List<string[]> ParseCsv(string content)
    {
        List<string[]> rows = new List<string[]>();
        List<string> row = new List<string>();
        StringBuilder cell = new StringBuilder();
        bool quoted = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    cell.Append(c);
                }

                continue;
            }

            if (c == '"')
            {
                quoted = true;
            }
            else if (c == ',')
            {
                row.Add(cell.ToString());
                cell.Length = 0;
            }
            else if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(cell.ToString());
                cell.Length = 0;
                if (row.Count > 1 || !string.IsNullOrEmpty(row[0]))
                {
                    rows.Add(row.ToArray());
                }

                row.Clear();
            }
            else
            {
                cell.Append(c);
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row.ToArray());
        }

        return rows;
    }

    private static string GetCsvValue(string[] row, Dictionary<string, int> headerIndexes, string column)
    {
        if (!headerIndexes.TryGetValue(column, out int index) || index < 0 || index >= row.Length)
        {
            return string.Empty;
        }

        return row[index].Replace("\\n", "\n");
    }

    private static DateTime GetFileWriteTimeUtc(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return DateTime.MinValue;
        }

        return File.GetLastWriteTimeUtc(path);
    }

    private GalStoryFile CreateFallbackStory()
    {
        GalStoryFile fallback = new GalStoryFile();
        fallback.title = "GAL Template";
        fallback.startNode = "start_001";
        fallback.artProfiles.Add(new GalArtProfile { id = "default", displayName = "默认线稿", backgroundFolder = "Backgrounds" });
        fallback.languages.Add(new GalLanguageEntry { id = "zh-CN", displayName = "简体中文", tablePath = "Localization/zh-CN.json" });
        fallback.nodes.Add(new GalStoryNode
        {
            id = "start_001",
            speaker = "旁白",
            text = "没有找到 gal_story.json，所以这里显示了内置备用文本。",
            nextId = "start_002"
        });
        fallback.nodes.Add(new GalStoryNode
        {
            id = "start_002",
            speaker = "系统",
            text = "请编辑 Assets/StreamingAssets/GAL/gal_story.json 来替换剧情。",
            commands = new List<GalStoryCommand>
            {
                new GalStoryCommand { command = "show_explore" }
            }
        });
        fallback.explorePoints.Add(new GalExplorePoint
        {
            id = "fallback_point",
            displayName = "示例地点",
            nodeId = "start_001",
            x = 0.5f,
            y = 0.5f
        });
        return fallback;
    }

    private void SetBackground(string backgroundId)
    {
        if (string.IsNullOrEmpty(backgroundId))
        {
            return;
        }

        currentBackgroundId = backgroundId;

        if (!backgroundsById.TryGetValue(backgroundId, out GalBackgroundEntry background))
        {
            Debug.LogWarning("Missing background id: " + backgroundId);
            return;
        }

        Texture2D texture = LoadBackgroundTexture(background);
        if (texture == null)
        {
            return;
        }

        backgroundImage.texture = texture;
        backgroundAspect.aspectRatio = texture.width / (float)texture.height;
        if (exploreButtonAreaAspect != null)
        {
            exploreButtonAreaAspect.aspectRatio = backgroundAspect.aspectRatio;
        }
    }

    private Texture2D LoadBackgroundTexture(GalBackgroundEntry background)
    {
        if (textureCache.TryGetValue(background.id, out Texture2D cachedTexture))
        {
            return cachedTexture;
        }

        string path = background.path;
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(Application.streamingAssetsPath, "GAL", path);
        }

        if (!File.Exists(path))
        {
            Debug.LogWarning("Missing background file: " + path);
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Destroy(texture);
            return null;
        }

        texture.name = background.id;
        textureCache[background.id] = texture;
        return texture;
    }

    private void LoadUiSkin()
    {
        uiTitleLogoFramesByLanguage.Clear();
        uiTitleLogoFrameFpsByLanguage.Clear();

        string skinId = GetActiveUiSkinId();
        if (string.IsNullOrWhiteSpace(skinId))
        {
            return;
        }

        string skinDirectory = Path.Combine(Application.streamingAssetsPath, "GAL", "UISkins", skinId);
        string skinPath = Path.Combine(skinDirectory, "ui_skin.json");
        if (!File.Exists(skinPath))
        {
            const string fallbackSkinId = "unauthorized_passenger";
            if (string.Equals(skinId, fallbackSkinId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            skinDirectory = Path.Combine(Application.streamingAssetsPath, "GAL", "UISkins", fallbackSkinId);
            skinPath = Path.Combine(skinDirectory, "ui_skin.json");
            if (!File.Exists(skinPath))
            {
                return;
            }
        }

        try
        {
            activeUiSkin = JsonUtility.FromJson<GalUiSkinFile>(File.ReadAllText(skinPath, Encoding.UTF8));
            if (activeUiSkin == null)
            {
                return;
            }

            ApplyUiSkinColors(activeUiSkin.colors);
            string spriteFolder = string.IsNullOrWhiteSpace(activeUiSkin.spriteFolder) ? "Sprites" : activeUiSkin.spriteFolder;
            string spriteDirectory = Path.Combine(skinDirectory, spriteFolder);
            GalUiSkinSprites sprites = activeUiSkin.sprites ?? new GalUiSkinSprites();
            uiTitleBackgroundSprite = LoadUiSkinSprite(spriteDirectory, sprites.titleBackground, Vector4.zero);
            uiTitleLogoSprite = LoadUiSkinSprite(spriteDirectory, sprites.titleLogo, Vector4.zero);
            uiTitleLogoFrameSprites = LoadUiSkinFrameSequence(spriteDirectory, sprites.titleLogoFrames, out uiTitleLogoFrameFps);
            LoadUiSkinLocalizedFrameSequences(spriteDirectory, sprites.titleLogoLocalizedFrames);
            uiButtonNormalSprite = LoadUiSkinSprite(spriteDirectory, sprites.buttonNormal, new Vector4(32f, 32f, 32f, 32f));
            uiButtonHoverSprite = LoadUiSkinSprite(spriteDirectory, sprites.buttonHover, new Vector4(32f, 32f, 32f, 32f));
            uiButtonPressedSprite = LoadUiSkinSprite(spriteDirectory, sprites.buttonPressed, new Vector4(32f, 32f, 32f, 32f));
            uiDialogueBoxSprite = LoadUiSkinSprite(spriteDirectory, sprites.dialogueBox, new Vector4(34f, 34f, 34f, 34f));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("GAL UI skin load failed: " + exception.Message);
        }
    }

    private Sprite[] LoadUiSkinFrameSequence(string spriteDirectory, GalUiSkinFrameSequence sequence, out float fps)
    {
        fps = 16f;
        if (sequence == null || string.IsNullOrWhiteSpace(spriteDirectory) || string.IsNullOrWhiteSpace(sequence.folder) || string.IsNullOrWhiteSpace(sequence.prefix) || sequence.count <= 0)
        {
            return null;
        }

        fps = Mathf.Clamp(sequence.fps <= 0f ? 16f : sequence.fps, 1f, 60f);
        string directory = Path.Combine(spriteDirectory, sequence.folder);
        if (!Directory.Exists(directory))
        {
            return null;
        }

        List<Sprite> frames = new List<Sprite>();
        string extension = string.IsNullOrWhiteSpace(sequence.extension) ? ".png" : sequence.extension;
        if (!extension.StartsWith(".", StringComparison.Ordinal))
        {
            extension = "." + extension;
        }

        int startIndex = sequence.startIndex <= 0 ? 1 : sequence.startIndex;
        int digits = Mathf.Clamp(sequence.digits <= 0 ? 2 : sequence.digits, 1, 8);
        for (int i = 0; i < sequence.count; i++)
        {
            string number = (startIndex + i).ToString(new string('0', digits));
            Sprite frame = LoadUiSkinSprite(directory, sequence.prefix + number + extension, Vector4.zero);
            if (frame != null)
            {
                frames.Add(frame);
            }
        }

        return frames.Count == 0 ? null : frames.ToArray();
    }

    private void LoadUiSkinLocalizedFrameSequences(string spriteDirectory, List<GalUiSkinLocalizedFrameSequence> localizedSequences)
    {
        if (localizedSequences == null)
        {
            return;
        }

        for (int i = 0; i < localizedSequences.Count; i++)
        {
            GalUiSkinLocalizedFrameSequence localizedSequence = localizedSequences[i];
            if (localizedSequence == null)
            {
                continue;
            }

            string languageKey = NormalizeTitleLanguageKey(localizedSequence.language);
            if (string.IsNullOrWhiteSpace(languageKey))
            {
                continue;
            }

            Sprite[] frames = LoadUiSkinFrameSequence(spriteDirectory, localizedSequence.sequence, out float fps);
            if (frames == null || frames.Length == 0)
            {
                continue;
            }

            uiTitleLogoFramesByLanguage[languageKey] = frames;
            uiTitleLogoFrameFpsByLanguage[languageKey] = fps;
        }
    }

    private Sprite[] GetActiveTitleLogoFrames(out float fps)
    {
        fps = uiTitleLogoFrameFps;
        string languageKey = NormalizeTitleLanguageKey(settings.language);
        if (!string.IsNullOrWhiteSpace(languageKey) &&
            uiTitleLogoFramesByLanguage.TryGetValue(languageKey, out Sprite[] localizedFrames) &&
            localizedFrames != null &&
            localizedFrames.Length > 0)
        {
            if (uiTitleLogoFrameFpsByLanguage.TryGetValue(languageKey, out float localizedFps))
            {
                fps = localizedFps;
            }

            return localizedFrames;
        }

        return uiTitleLogoFrameSprites;
    }

    private static string NormalizeTitleLanguageKey(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "zh";
        }

        string key = language.Trim().Replace('_', '-').ToLowerInvariant();
        if (key.StartsWith("zh", StringComparison.Ordinal))
        {
            return "zh";
        }

        if (key.StartsWith("en", StringComparison.Ordinal))
        {
            return "en";
        }

        if (key.StartsWith("ja", StringComparison.Ordinal) || key.StartsWith("jp", StringComparison.Ordinal))
        {
            return "ja";
        }

        return key;
    }

    private string GetActiveUiSkinId()
    {
        GalArtProfile profile = FindActiveArtProfile();
        return profile == null ? null : profile.uiSkin;
    }

    private GalArtProfile FindActiveArtProfile()
    {
        if (story == null || story.artProfiles == null || story.artProfiles.Count == 0)
        {
            return null;
        }

        string activeId = string.IsNullOrWhiteSpace(settings.artProfile) ? "default" : settings.artProfile;
        for (int i = 0; i < story.artProfiles.Count; i++)
        {
            GalArtProfile profile = story.artProfiles[i];
            if (profile != null && string.Equals(profile.id, activeId, StringComparison.OrdinalIgnoreCase))
            {
                return profile;
            }
        }

        return story.artProfiles[0];
    }

    private Sprite LoadUiSkinSprite(string directory, string fileName, Vector4 border)
    {
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        string path = Path.Combine(directory, fileName);
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

            texture.name = Path.GetFileNameWithoutExtension(fileName);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("GAL UI skin sprite load failed: " + path + " / " + exception.Message);
            return null;
        }
    }

    private void ApplyUiSkinColors(GalUiSkinColors colors)
    {
        if (colors == null)
        {
            return;
        }

        uiButtonTextColor = ParseSkinColor(colors.buttonText, uiButtonTextColor);
        uiButtonTextShadowColor = ParseSkinColor(colors.buttonTextShadow, uiButtonTextShadowColor);
        uiPanelTextColor = ParseSkinColor(colors.panelText, uiPanelTextColor);
        uiDialogueTextColor = ParseSkinColor(colors.dialogueText, uiDialogueTextColor);
        uiDialogueSpeakerColor = ParseSkinColor(colors.dialogueSpeaker, uiDialogueSpeakerColor);
    }

    private Color ParseSkinColor(string html, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return fallback;
        }

        Color color;
        return ColorUtility.TryParseHtmlString(html, out color) ? color : fallback;
    }

    private void BuildUi()
    {
        EnsureEventSystem();
        LoadUiSkin();

        GameObject canvasObject = new GameObject("GAL Template Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildBackground(canvasObject.transform);
        BuildPortraitLayer(canvasObject.transform);
        BuildMainMenu(canvasObject.transform);
        BuildDialogue(canvasObject.transform);
        BuildExplore(canvasObject.transform);
        BuildHud(canvasObject.transform);
        BuildExternalSceneHud(canvasObject.transform);
        BuildTransition(canvasObject.transform);
        BuildToast(canvasObject.transform);
        BuildHistory(canvasObject.transform);
        BuildSaveLoadPanel(canvasObject.transform);
        BuildSettings(canvasObject.transform);
        BuildCharacterSettings(canvasObject.transform);
        BuildPortraitDebug(canvasObject.transform);
    }

    private void BuildBackground(Transform parent)
    {
        GameObject frame = CreateUiObject("Background Frame", parent);
        backgroundRoot = frame;
        Stretch(frame.GetComponent<RectTransform>());
        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = new Color(0.96f, 0.95f, 0.92f, 1f);

        GameObject imageObject = CreateUiObject("Story Background", frame.transform);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = new Vector2(1920f, 1080f);
        backgroundImage = imageObject.AddComponent<RawImage>();
        backgroundImage.color = Color.white;
        backgroundAspect = imageObject.AddComponent<AspectRatioFitter>();
        backgroundAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        backgroundAspect.aspectRatio = 16f / 9f;

        GameObject wash = CreateUiObject("Subtle Wash", parent);
        backgroundWashRoot = wash;
        Stretch(wash.GetComponent<RectTransform>());
        Image washImage = wash.AddComponent<Image>();
        washImage.color = new Color(1f, 0.98f, 0.92f, 0.18f);
    }

    private void BuildPortraitLayer(Transform parent)
    {
        GameObject layer = CreateUiObject("Portrait Layer", parent);
        portraitController = layer.AddComponent<GalPortraitController>();
        portraitController.Initialize(GetFont(24));
        portraitController.Configure(story.portraits);
    }

    private void BuildMainMenu(Transform parent)
    {
        mainMenuRoot = CreateUiObject("Main Menu", parent);
        Stretch(mainMenuRoot.GetComponent<RectTransform>());

        BuildTitleBackground(mainMenuRoot.transform);

        GameObject shade = CreateUiObject("Title Soft Shade", mainMenuRoot.transform);
        Stretch(shade.GetComponent<RectTransform>());
        Image shadeImage = shade.AddComponent<Image>();
        shadeImage.color = uiTitleBackgroundSprite == null ? new Color(0.055f, 0.04f, 0.06f, 0.28f) : new Color(0.035f, 0.03f, 0.08f, 0.24f);

        GameObject design = CreateUiObject("Title Sprite Layout", mainMenuRoot.transform);
        RectTransform designRect = design.GetComponent<RectTransform>();
        designRect.anchorMin = new Vector2(0f, 0f);
        designRect.anchorMax = new Vector2(1f, 1f);
        designRect.offsetMin = Vector2.zero;
        designRect.offsetMax = Vector2.zero;
        AspectRatioFitter designAspect = design.AddComponent<AspectRatioFitter>();
        designAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        designAspect.aspectRatio = 2266f / 1488f;

        BuildTitleMenuShade(design.transform);

        GameObject titleStage = CreateUiObject("Title Stage", design.transform);
        RectTransform stageRect = titleStage.GetComponent<RectTransform>();
        SetTitleDesignRect(stageRect, 520f, 748f, 700f, 1260f);
        Image stageImage = titleStage.AddComponent<Image>();
        if (uiButtonNormalSprite != null)
        {
            stageImage.sprite = uiButtonNormalSprite;
            stageImage.type = Image.Type.Sliced;
            stageImage.pixelsPerUnitMultiplier = 1f;
        }
        stageImage.color = uiButtonNormalSprite == null ? new Color(1f, 0.98f, 0.99f, 0.1f) : new Color(1f, 1f, 1f, 0.11f);
        stageImage.raycastTarget = false;
        Shadow stageShadow = titleStage.AddComponent<Shadow>();
        stageShadow.effectColor = new Color(0.16f, 0.05f, 0.17f, 0.22f);
        stageShadow.effectDistance = new Vector2(10f, -10f);

        GameObject interaction = CreateUiObject("Title Interaction Layer", design.transform);
        Stretch(interaction.GetComponent<RectTransform>());

        menuTitleText = null;
        BuildTitleParticles(interaction.transform);
        BuildTitleLogo(interaction.transform);

        primaryActionButton = CreateTitleDesignButton(interaction.transform, T("ui.main.start", "开始游戏"), OnPrimaryAction, 430f, 454f, 448f, 132f, out primaryActionLabel);
        AddTitleButtonEntrance(primaryActionButton, 0);
        BuildTitleMenuPointer(interaction.transform, 454f);
        BindTitleMenuPointer(primaryActionButton, 454f);
        newGameButton = CreateTitleDesignButton(interaction.transform, T("ui.main.new_game", "新游戏"), StartNewGame, 430f, 690f, 448f, 132f, out newGameButtonLabel);
        AddTitleButtonEntrance(newGameButton, 1);
        BindTitleMenuPointer(newGameButton, 690f);
        Button settingsButton = CreateTitleDesignButton(interaction.transform, T("ui.common.settings", "设置"), ShowSettings, 430f, 924f, 448f, 132f, out mainMenuSettingsButtonLabel);
        AddTitleButtonEntrance(settingsButton, 2);
        BindTitleMenuPointer(settingsButton, 924f);
        Button quitButton = CreateTitleDesignButton(interaction.transform, T("ui.common.quit", "退出"), QuitGame, 430f, 1161f, 448f, 132f, out quitButtonLabel);
        AddTitleButtonEntrance(quitButton, 3);
        BindTitleMenuPointer(quitButton, 1161f);

        saveInfoText = null;
    }

    private void BuildTitleMenuShade(Transform parent)
    {
        GameObject shadeObject = CreateUiObject("Title Left Blue Violet Shade", parent);
        RectTransform shadeRect = shadeObject.GetComponent<RectTransform>();
        SetTitleDesignRect(shadeRect, 520f, 744f, 1040f, 1488f);

        Image shadeImage = shadeObject.AddComponent<Image>();
        shadeImage.sprite = GalUiRuntimeSprites.HorizontalFadeSprite;
        shadeImage.color = new Color(0.035f, 0.03f, 0.12f, 0.42f);
        shadeImage.raycastTarget = false;
    }

    private void BuildTitleBackground(Transform parent)
    {
        if (uiTitleBackgroundSprite == null)
        {
            return;
        }

        GameObject frame = CreateUiObject("Title Background Frame", parent);
        Stretch(frame.GetComponent<RectTransform>());
        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = new Color(0.88f, 0.84f, 0.91f, 1f);
        frameImage.raycastTarget = false;

        GameObject imageObject = CreateUiObject("Title Background Image", frame.transform);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = new Vector2(1920f, 1080f);
        imageRect.anchoredPosition = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.sprite = uiTitleBackgroundSprite;
        image.color = Color.white;
        image.raycastTarget = false;

        AspectRatioFitter fitter = imageObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 16f / 9f;
    }

    private void BuildTitleLogo(Transform parent)
    {
        titleSaturationImages.Clear();
        Sprite[] activeLogoFrames = GetActiveTitleLogoFrames(out float activeLogoFps);
        bool hasFrameLogo = activeLogoFrames != null && activeLogoFrames.Length > 0;
        if (uiTitleLogoSprite == null && !hasFrameLogo)
        {
            return;
        }

        List<Image> titleStableEffects = new List<Image>();
        Sprite logoSprite = hasFrameLogo ? activeLogoFrames[0] : uiTitleLogoSprite;
        Vector2 logoCenter = hasFrameLogo ? new Vector2(448f, 194f) : new Vector2(440f, 184f);
        Vector2 logoSize = hasFrameLogo ? new Vector2(650f, 275f) : new Vector2(560f, 235f);
        float effectAlpha = hasFrameLogo ? 0.42f : 1f;
        titleStableEffects.Add(CreateTitleLogoImage(parent, "Title Logo Soft Shadow A", logoSprite, logoCenter + new Vector2(4f, 4f), logoSize, new Color(0.118f, 0.106f, 0.165f, 0.22f * effectAlpha)));
        titleStableEffects.Add(CreateTitleLogoImage(parent, "Title Logo Soft Shadow B", logoSprite, logoCenter + new Vector2(6f, 6f), logoSize, new Color(0.118f, 0.106f, 0.165f, 0.12f * effectAlpha)));
        titleStableEffects.Add(CreateTitleLogoImage(parent, "Title Logo Soft Shadow C", logoSprite, logoCenter + new Vector2(2f, 2f), logoSize, new Color(0.118f, 0.106f, 0.165f, 0.14f * effectAlpha)));
        menuTitleBloomImage = CreateTitleLogoImage(parent, "Title Logo Bloom", logoSprite, logoCenter, logoSize * 1.055f, new Color(0.725f, 0.655f, 1f, 0.26f * effectAlpha));
        menuTitleGlowImage = CreateTitleLogoImage(parent, "Title Logo Glow", logoSprite, logoCenter, logoSize * 1.025f, new Color(0.725f, 0.655f, 1f, 0.26f * effectAlpha));
        titleStableEffects.Add(menuTitleBloomImage);
        titleStableEffects.Add(menuTitleGlowImage);
        titleStableEffects.Add(CreateTitleLogoImage(parent, "Title Logo Stroke Left", logoSprite, logoCenter + new Vector2(-4f, 0f), logoSize, new Color(0.227f, 0.192f, 0.282f, 0.72f * effectAlpha)));
        titleStableEffects.Add(CreateTitleLogoImage(parent, "Title Logo Stroke Right", logoSprite, logoCenter + new Vector2(4f, 0f), logoSize, new Color(0.227f, 0.192f, 0.282f, 0.72f * effectAlpha)));
        titleStableEffects.Add(CreateTitleLogoImage(parent, "Title Logo Stroke Up", logoSprite, logoCenter + new Vector2(0f, -4f), logoSize, new Color(0.227f, 0.192f, 0.282f, 0.64f * effectAlpha)));
        titleStableEffects.Add(CreateTitleLogoImage(parent, "Title Logo Stroke Down", logoSprite, logoCenter + new Vector2(0f, 4f), logoSize, new Color(0.227f, 0.192f, 0.282f, 0.64f * effectAlpha)));
        Image titleGlitchA = CreateTitleLogoImage(parent, "Title Logo Glitch Magenta", logoSprite, logoCenter, logoSize, new Color(1f, 0.22f, 0.88f, 0f));
        Image titleGlitchB = CreateTitleLogoImage(parent, "Title Logo Glitch Cyan", logoSprite, logoCenter, logoSize, new Color(0.46f, 0.88f, 1f, 0f));
        menuTitleLogoImage = CreateTitleLogoImage(parent, "Title Logo", logoSprite, logoCenter, logoSize, new Color(0.949f, 0.914f, 1f, 1f));
        BuildTitleLogoSweep(parent, logoCenter, logoSize, hasFrameLogo ? 0.42f : 1f);

        menuTitleLogoAnimator = menuTitleLogoImage.gameObject.AddComponent<GalTitleLogoAnimator>();
        menuTitleLogoAnimator.Configure(menuTitleLogoImage, menuTitleGlowImage, menuTitleBloomImage, titleGlitchA, titleGlitchB, titleStableEffects);
        if (hasFrameLogo)
        {
            menuTitleLogoAnimator.ConfigureFrameSequence(activeLogoFrames, activeLogoFps);
        }

        ApplyTitleSaturation();
    }

    private void BuildTitleLogoSweep(Transform parent, Vector2 logoCenter, Vector2 logoSize, float alphaScale)
    {
        GameObject sweepArea = CreateUiObject("Title Logo Sweep Area", parent);
        RectTransform areaRect = sweepArea.GetComponent<RectTransform>();
        SetTitleDesignRect(areaRect, logoCenter.x, logoCenter.y, logoSize.x * 1.12f, logoSize.y * 1.08f);
        sweepArea.AddComponent<RectMask2D>();

        GameObject sweepObject = CreateUiObject("Title Logo Sweep", sweepArea.transform);
        RectTransform sweepRect = sweepObject.GetComponent<RectTransform>();
        sweepRect.anchorMin = new Vector2(0.5f, 0.5f);
        sweepRect.anchorMax = new Vector2(0.5f, 0.5f);
        sweepRect.pivot = new Vector2(0.5f, 0.5f);
        sweepRect.sizeDelta = new Vector2(42f, 286f);
        sweepRect.anchoredPosition = new Vector2(-330f, 0f);
        sweepRect.localEulerAngles = new Vector3(0f, 0f, -15f);

        Image sweepImage = sweepObject.AddComponent<Image>();
        sweepImage.sprite = GalUiRuntimeSprites.SoftSweepSprite;
        sweepImage.color = new Color(1f, 0.85f, 1f, 0f);
        sweepImage.raycastTarget = false;
        GalLoopSweepAnimator sweepAnimator = sweepObject.AddComponent<GalLoopSweepAnimator>();
        float sweepDistance = Mathf.Max(330f, logoSize.x * 0.54f);
        sweepAnimator.Configure(sweepImage, -sweepDistance, sweepDistance, 0.78f * alphaScale, 5.2f, 8.2f, 1.6f);
    }

    private void BuildTitleParticles(Transform parent)
    {
        GameObject particleField = CreateUiObject("Title Particle Field", parent);
        RectTransform fieldRect = particleField.GetComponent<RectTransform>();
        SetTitleDesignRect(fieldRect, 520f, 734f, 760f, 1160f);
        GalTitleParticleAnimator particleAnimator = particleField.AddComponent<GalTitleParticleAnimator>();
        particleAnimator.Configure(16);
    }

    private void BuildTitleMenuPointer(Transform parent, float centerY)
    {
        GameObject glowObject = CreateUiObject("Title Menu Pointer Glow", parent);
        titleMenuPointerGlowRect = glowObject.GetComponent<RectTransform>();
        SetTitleDesignRect(titleMenuPointerGlowRect, TitleMenuPointerX, centerY, 68f, 68f);
        titleMenuPointerGlowRect.localScale = new Vector3(-1f, 1f, 1f);
        Image glowImage = glowObject.AddComponent<Image>();
        glowImage.sprite = GalUiRuntimeSprites.TrianglePointerSprite;
        glowImage.color = new Color(0.72f, 0.42f, 1f, 0.48f);
        glowImage.raycastTarget = false;
        GalBreathingAnimator glowBreath = glowObject.AddComponent<GalBreathingAnimator>();
        glowBreath.Configure(0.09f, 0.1f, 1.2f);

        GameObject pointerObject = CreateUiObject("Title Menu Pointer", parent);
        titleMenuPointerRect = pointerObject.GetComponent<RectTransform>();
        SetTitleDesignRect(titleMenuPointerRect, TitleMenuPointerX, centerY, 44f, 44f);
        titleMenuPointerRect.localScale = new Vector3(-1f, 1f, 1f);
        Image pointerImage = pointerObject.AddComponent<Image>();
        pointerImage.sprite = GalUiRuntimeSprites.TrianglePointerSprite;
        pointerImage.color = new Color(0.949f, 0.914f, 1f, 1f);
        pointerImage.raycastTarget = false;
        GalBreathingAnimator pointerBreath = pointerObject.AddComponent<GalBreathingAnimator>();
        pointerBreath.Configure(0.045f, 0.06f, 1.4f);

        titleMenuPointerCurrentY = centerY;
        titleMenuPointerTargetY = centerY;
        titleMenuPointerReady = true;
    }

    private void BindTitleMenuPointer(Button button, float centerY)
    {
        if (button == null)
        {
            return;
        }

        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => MoveTitleMenuPointer(centerY, true));
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry selectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        selectEntry.callback.AddListener(_ => MoveTitleMenuPointer(centerY, true));
        trigger.triggers.Add(selectEntry);
    }

    private void MoveTitleMenuPointer(float centerY, bool animated)
    {
        if (!titleMenuPointerReady)
        {
            return;
        }

        titleMenuPointerTargetY = centerY;
        if (!animated)
        {
            titleMenuPointerCurrentY = centerY;
            ApplyTitleMenuPointerPosition(centerY);
        }
    }

    private void UpdateTitleMenuPointer()
    {
        if (!titleMenuPointerReady || titleMenuPointerRect == null || titleMenuPointerGlowRect == null)
        {
            return;
        }

        titleMenuPointerCurrentY = Mathf.Lerp(titleMenuPointerCurrentY, titleMenuPointerTargetY, Time.unscaledDeltaTime * 18f);
        ApplyTitleMenuPointerPosition(titleMenuPointerCurrentY);
    }

    private void ApplyTitleMenuPointerPosition(float centerY)
    {
        if (titleMenuPointerGlowRect != null)
        {
            SetTitleDesignRect(titleMenuPointerGlowRect, TitleMenuPointerX, centerY, 68f, 68f);
        }
        if (titleMenuPointerRect != null)
        {
            SetTitleDesignRect(titleMenuPointerRect, TitleMenuPointerX, centerY, 44f, 44f);
        }
    }

    private Image CreateTitleLogoImage(Transform parent, string name, Vector2 center, Vector2 size, Color color)
    {
        return CreateTitleLogoImage(parent, name, uiTitleLogoSprite, center, size, color);
    }

    private Image CreateTitleLogoImage(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color)
    {
        GameObject logoObject = CreateUiObject(name, parent);
        RectTransform logoRect = logoObject.GetComponent<RectTransform>();
        SetTitleDesignRect(logoRect, center.x, center.y, size.x, size.y);

        Image image = logoObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        RegisterTitleSaturationImage(image);
        return image;
    }

    private void RegisterTitleSaturationImage(Image image)
    {
        if (image == null)
        {
            return;
        }

        EnsureTitleSaturationMaterial();
        if (titleSaturationMaterial != null)
        {
            image.material = titleSaturationMaterial;
        }

        titleSaturationImages.Add(image);
    }

    private void RefreshTitleLogoLanguage()
    {
        if (menuTitleLogoImage == null)
        {
            return;
        }

        Sprite[] activeLogoFrames = GetActiveTitleLogoFrames(out float activeLogoFps);
        Sprite logoSprite = activeLogoFrames != null && activeLogoFrames.Length > 0 ? activeLogoFrames[0] : uiTitleLogoSprite;
        if (logoSprite == null)
        {
            return;
        }

        for (int i = 0; i < titleSaturationImages.Count; i++)
        {
            Image image = titleSaturationImages[i];
            if (image != null)
            {
                image.sprite = logoSprite;
            }
        }

        if (menuTitleLogoAnimator != null)
        {
            menuTitleLogoAnimator.ConfigureFrameSequence(activeLogoFrames, activeLogoFps);
        }
    }

    private void EnsureTitleSaturationMaterial()
    {
        if (titleSaturationMaterial != null)
        {
            return;
        }

        Shader shader = Resources.Load<Shader>("Shaders/UiTitleSaturation");
        if (shader == null)
        {
            return;
        }

        titleSaturationMaterial = new Material(shader);
        titleSaturationMaterial.hideFlags = HideFlags.HideAndDontSave;
        ApplyTitleSaturation();
    }

    private void ApplyTitleSaturation()
    {
        settings.titleSaturation = Mathf.Clamp(settings.titleSaturation, 0f, 2f);
        if (titleSaturationMaterial == null)
        {
            return;
        }

        titleSaturationMaterial.SetFloat("_Saturation", settings.titleSaturation);
        for (int i = 0; i < titleSaturationImages.Count; i++)
        {
            Image image = titleSaturationImages[i];
            if (image != null && image.material != titleSaturationMaterial)
            {
                image.material = titleSaturationMaterial;
            }
        }
    }

    private Sprite CreateTitleGlassSprite(float centerX, float centerY, float width, float height)
    {
        if (uiTitleBackgroundSprite == null || uiTitleBackgroundSprite.texture == null)
        {
            return null;
        }

        string key = Mathf.RoundToInt(centerX).ToString() + "_" + Mathf.RoundToInt(centerY).ToString() + "_" + Mathf.RoundToInt(width).ToString() + "_" + Mathf.RoundToInt(height).ToString();
        Sprite cached;
        if (titleGlassSpriteCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        Texture2D source = uiTitleBackgroundSprite.texture;
        int sourceWidth = source.width;
        int sourceHeight = source.height;
        int sampleWidth = 224;
        int sampleHeight = 66;
        Color[] pixels = new Color[sampleWidth * sampleHeight];

        const float designWidth = 2266f;
        const float designHeight = 1488f;
        float normalizedCenterX = centerX / designWidth;
        float normalizedCenterY = 1f - centerY / designHeight;
        float normalizedWidth = width / designWidth;
        float normalizedHeight = height / designHeight;
        float sampleLeft = (normalizedCenterX - normalizedWidth * 0.5f) * sourceWidth;
        float sampleTop = (1f - normalizedCenterY - normalizedHeight * 0.5f) * sourceHeight;
        float sampleSourceWidth = normalizedWidth * sourceWidth;
        float sampleSourceHeight = normalizedHeight * sourceHeight;

        for (int y = 0; y < sampleHeight; y++)
        {
            for (int x = 0; x < sampleWidth; x++)
            {
                float u = sampleLeft + ((float)x + 0.5f) / sampleWidth * sampleSourceWidth;
                float v = sampleTop + ((float)y + 0.5f) / sampleHeight * sampleSourceHeight;
                pixels[y * sampleWidth + x] = SampleBlurred(source, u, v);
            }
        }

        Texture2D texture = new Texture2D(sampleWidth, sampleHeight, TextureFormat.RGBA32, false);
        texture.name = "GAL Title Glass " + key;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.SetPixels(pixels);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(18f, 18f, 18f, 18f));
        titleGlassSpriteCache[key] = sprite;
        return sprite;
    }

    private static Color SampleBlurred(Texture2D source, float x, float y)
    {
        Color color = Color.clear;
        float total = 0f;
        for (int oy = -2; oy <= 2; oy++)
        {
            for (int ox = -2; ox <= 2; ox++)
            {
                float distance = Mathf.Abs(ox) + Mathf.Abs(oy);
                float weight = distance <= 0.01f ? 4f : distance <= 1.01f ? 2.6f : distance <= 2.01f ? 1.35f : 0.6f;
                Color sample = source.GetPixelBilinear(Mathf.Clamp01((x + ox * 6f) / source.width), Mathf.Clamp01(1f - (y + oy * 6f) / source.height));
                color += sample * weight;
                total += weight;
            }
        }

        color /= Mathf.Max(0.001f, total);
        color = Color.Lerp(color, new Color(0.88f, 0.82f, 1f, 1f), 0.2f);
        color.a = 1f;
        return color;
    }

    private Button CreateTitleDesignButton(Transform parent, string label, UnityAction onClick, float centerX, float centerY, float width, float height, out Text labelText)
    {
        GameObject buttonObject = CreateUiObject(label + " Title Hit Button", parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        SetTitleDesignRect(buttonRect, centerX, centerY, width, height);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = uiButtonNormalSprite;
        image.type = Image.Type.Sliced;
        image.color = uiButtonNormalSprite == null ? new Color(1f, 1f, 1f, 0.001f) : new Color(1f, 0.78f, 1f, 0.62f);

        GameObject sweepObject = CreateUiObject("Hover Sweep", buttonObject.transform);
        RectTransform sweepRect = sweepObject.GetComponent<RectTransform>();
        sweepRect.anchorMin = new Vector2(0.5f, 0.5f);
        sweepRect.anchorMax = new Vector2(0.5f, 0.5f);
        sweepRect.pivot = new Vector2(0.5f, 0.5f);
        sweepRect.sizeDelta = new Vector2(72f, height * 1.65f);
        sweepRect.anchoredPosition = new Vector2(-width * 0.7f, 0f);
        Image sweepImage = sweepObject.AddComponent<Image>();
        sweepImage.sprite = GalUiRuntimeSprites.SoftSweepSprite;
        sweepImage.color = new Color(1f, 0.84f, 1f, 0f);
        sweepImage.raycastTarget = false;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        int buttonFontSize = label.Length > 12 ? 26 : label.Length > 6 ? 32 : 38;
        labelText = CreateText(buttonObject.transform, label, buttonFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.949f, 0.914f, 1f, 1f));
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        Stretch(labelRect);
        labelRect.offsetMin = new Vector2(18f, 0f);
        labelRect.offsetMax = new Vector2(-18f, 0f);
        labelText.resizeTextForBestFit = true;
        labelText.resizeTextMinSize = 16;
        labelText.resizeTextMaxSize = buttonFontSize;
        Shadow shadow = labelText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.08f, 0.05f, 0.18f, 0.86f);
        shadow.effectDistance = new Vector2(3f, -3f);

        GalButtonAnimator animator = buttonObject.AddComponent<GalButtonAnimator>();
        GalUiSkinAnimation animation = activeUiSkin == null || activeUiSkin.animation == null ? new GalUiSkinAnimation() : activeUiSkin.animation;
        animator.Configure(image, uiButtonNormalSprite, uiButtonHoverSprite, uiButtonPressedSprite, Mathf.Min(animation.hoverScale, 1.018f), animation.pressedScale);
        animator.ConfigureSweep(sweepImage, 0.36f);
        return button;
    }

    private void CreateTitleButtonGlowLayer(Transform parent, string name, float offsetX, float offsetY, float width, float height, float alpha)
    {
        if (uiButtonNormalSprite == null)
        {
            return;
        }

        GameObject glowObject = CreateUiObject(name, parent);
        RectTransform glowRect = glowObject.GetComponent<RectTransform>();
        Stretch(glowRect);
        glowRect.offsetMin = new Vector2(-Mathf.Abs(offsetX) - 3f + offsetX, -Mathf.Abs(offsetY) - 3f + offsetY);
        glowRect.offsetMax = new Vector2(Mathf.Abs(offsetX) + 3f + offsetX, Mathf.Abs(offsetY) + 3f + offsetY);
        Image glowImage = glowObject.AddComponent<Image>();
        glowImage.sprite = uiButtonNormalSprite;
        glowImage.type = Image.Type.Sliced;
        glowImage.color = new Color(0.72f, 0.42f, 1f, alpha);
        glowImage.raycastTarget = false;
    }

    private void SetTitleDesignRect(RectTransform rect, float centerX, float centerY, float width, float height)
    {
        const float designWidth = 2266f;
        const float designHeight = 1488f;
        rect.anchorMin = new Vector2(centerX / designWidth, 1f - centerY / designHeight);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;
    }

    private void AddTitleButtonEntrance(Button button, int index)
    {
        if (button == null)
        {
            return;
        }

        GalUiSkinAnimation animation = activeUiSkin == null || activeUiSkin.animation == null ? new GalUiSkinAnimation() : activeUiSkin.animation;
        GalFadeInAnimator fade = button.gameObject.AddComponent<GalFadeInAnimator>();
        fade.Configure(0.08f + index * Mathf.Max(0.01f, animation.titleButtonStagger), 0.32f, 0.965f);
    }

    private void BuildDialogue(Transform parent)
    {
        dialogueRoot = CreateUiObject("Dialogue", parent);
        RectTransform rootRect = dialogueRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.offsetMin = new Vector2(96f, 52f);
        rootRect.offsetMax = new Vector2(-96f, 304f);

        Image panelImage = dialogueRoot.AddComponent<Image>();
        panelImage.color = uiDialogueBoxSprite == null ? new Color(0.975f, 0.96f, 0.93f, 0.94f) : new Color(1f, 1f, 1f, 0.94f);
        if (uiDialogueBoxSprite != null)
        {
            panelImage.sprite = uiDialogueBoxSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.pixelsPerUnitMultiplier = 1f;
        }
        Shadow panelShadow = dialogueRoot.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0.18f, 0.07f, 0.2f, 0.2f);
        panelShadow.effectDistance = new Vector2(8f, -8f);

        speakerText = CreateText(dialogueRoot.transform, string.Empty, 28, FontStyle.Bold, TextAnchor.MiddleLeft, uiDialogueSpeakerColor);
        RectTransform speakerRect = speakerText.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(0.36f, 1f);
        speakerRect.pivot = new Vector2(0f, 1f);
        speakerRect.offsetMin = new Vector2(42f, -74f);
        speakerRect.offsetMax = new Vector2(0f, -18f);
        AddTextShadow(speakerText, new Color(1f, 1f, 1f, 0.08f), new Vector2(1.2f, -1.2f));

        dialogueText = CreateText(dialogueRoot.transform, string.Empty, 32, FontStyle.Normal, TextAnchor.UpperLeft, uiDialogueTextColor);
        dialogueText.resizeTextForBestFit = false;
        dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dialogueText.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform lineRect = dialogueText.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0f, 0f);
        lineRect.anchorMax = new Vector2(1f, 1f);
        lineRect.offsetMin = new Vector2(42f, 46f);
        AddTextShadow(dialogueText, new Color(1f, 1f, 1f, 0.05f), new Vector2(1f, -1f));
        lineRect.offsetMax = new Vector2(-430f, -84f);

        continueHintText = CreateText(dialogueRoot.transform, T("ui.dialogue.continue", "点击或空格继续"), 19, FontStyle.Normal, TextAnchor.LowerRight, new Color(uiDialogueTextColor.r, uiDialogueTextColor.g, uiDialogueTextColor.b, 0.72f));
        RectTransform hintRect = continueHintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.sizeDelta = new Vector2(320f, 38f);
        hintRect.anchoredPosition = new Vector2(-38f, 24f);

        GameObject choices = CreateUiObject("Choices", dialogueRoot.transform);
        choiceContainer = choices.transform;
        RectTransform choiceRect = choices.GetComponent<RectTransform>();
        choiceRect.anchorMin = new Vector2(1f, 0f);
        choiceRect.anchorMax = new Vector2(1f, 1f);
        choiceRect.pivot = new Vector2(1f, 0.5f);
        choiceRect.offsetMin = new Vector2(-398f, 60f);
        choiceRect.offsetMax = new Vector2(-34f, -42f);
        VerticalLayoutGroup choiceLayout = choices.AddComponent<VerticalLayoutGroup>();
        choiceLayout.spacing = 12f;
        choiceLayout.childControlWidth = true;
        choiceLayout.childControlHeight = false;
        choiceLayout.childForceExpandHeight = false;

        dialogueRoot.SetActive(false);
    }

    private void BuildExplore(Transform parent)
    {
        exploreRoot = CreateUiObject("Explore", parent);
        Stretch(exploreRoot.GetComponent<RectTransform>());

        GameObject titleBackground = CreateUiObject("Explore Title Background", exploreRoot.transform);
        RectTransform backgroundRect = titleBackground.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 1f);
        backgroundRect.anchorMax = new Vector2(0.5f, 1f);
        backgroundRect.pivot = new Vector2(0.5f, 1f);
        backgroundRect.sizeDelta = new Vector2(420f, 54f);
        backgroundRect.anchoredPosition = new Vector2(0f, -92f);
        Image titleImage = titleBackground.AddComponent<Image>();
        titleImage.color = new Color(0.98f, 0.97f, 0.93f, 0.88f);

        exploreTitleText = CreateText(exploreRoot.transform, "选择调查地点", 28, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.08f, 0.08f, 0.08f, 1f));
        RectTransform titleRect = exploreTitleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(420f, 54f);
        titleRect.anchoredPosition = new Vector2(0f, -92f);

        GameObject buttons = CreateUiObject("Explore Points", exploreRoot.transform);
        exploreButtonContainer = buttons.transform;
        exploreButtonAreaRect = buttons.GetComponent<RectTransform>();
        exploreButtonAreaRect.anchorMin = new Vector2(0.5f, 0.5f);
        exploreButtonAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        exploreButtonAreaRect.pivot = new Vector2(0.5f, 0.5f);
        exploreButtonAreaRect.sizeDelta = new Vector2(1920f, 1080f);
        exploreButtonAreaAspect = buttons.AddComponent<AspectRatioFitter>();
        exploreButtonAreaAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        exploreButtonAreaAspect.aspectRatio = 16f / 9f;
        exploreRoot.SetActive(false);
    }

    private void BuildTransition(Transform parent)
    {
        transitionRoot = CreateUiObject("Scene Transition", parent);
        Stretch(transitionRoot.GetComponent<RectTransform>());
        transitionImage = transitionRoot.AddComponent<Image>();
        transitionImage.color = new Color(0.04f, 0.04f, 0.035f, 0f);

        transitionText = CreateText(transitionRoot.transform, string.Empty, 30, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.98f, 0.96f, 0.86f, 0f));
        RectTransform textRect = transitionText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(520f, 70f);
        textRect.anchoredPosition = Vector2.zero;

        transitionRoot.SetActive(false);
    }

    private void BuildHud(Transform parent)
    {
        hudRoot = CreateUiObject("HUD", parent);
        RectTransform hudRect = hudRoot.GetComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(1f, 1f);
        hudRect.anchorMax = new Vector2(1f, 1f);
        hudRect.pivot = new Vector2(1f, 1f);
        hudRect.sizeDelta = new Vector2(1120f, 60f);
        hudRect.anchoredPosition = new Vector2(-38f, -28f);

        HorizontalLayoutGroup layout = hudRoot.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleRight;

        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.save", "存档"), ShowSavePanel, out hudSaveButtonLabel), 48f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.load", "读档"), ShowLoadPanel, out hudLoadButtonLabel), 48f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.auto", "自动"), ToggleAutoMode, out autoButtonLabel), 48f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.skip", "跳过"), ToggleSkipMode, out skipButtonLabel), 48f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.hide", "隐藏"), ToggleDialogueHidden, out hudHideButtonLabel), 48f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.history", "历史"), ShowHistory, out hudHistoryButtonLabel), 48f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.common.settings", "设置"), ShowSettings, out hudSettingsButtonLabel), 48f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.portrait_debug", "立绘"), ShowPortraitDebug, out hudDebugButtonLabel), 48f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.title", "标题"), ShowMainMenu, out hudTitleButtonLabel), 48f, 104f);
        hudRoot.SetActive(false);
    }

    private void BuildExternalSceneHud(Transform parent)
    {
        fbxHudRoot = CreateUiObject("External Scene HUD", parent);
        RectTransform hudRect = fbxHudRoot.GetComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(1f, 1f);
        hudRect.anchorMax = new Vector2(1f, 1f);
        hudRect.pivot = new Vector2(1f, 1f);
        hudRect.sizeDelta = new Vector2(820f, 60f);
        hudRect.anchoredPosition = new Vector2(-38f, -28f);

        Image background = fbxHudRoot.AddComponent<Image>();
        background.color = new Color(0.03f, 0.025f, 0.05f, 0.34f);

        HorizontalLayoutGroup layout = fbxHudRoot.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 6, 6);
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleRight;

        AddButtonLayout(CreateButton(fbxHudRoot.transform, T("ui.common.back", "Back"), ExitFbxScene, out fbxBackButtonLabel), 48f, 104f);
        AddButtonLayout(CreateButton(fbxHudRoot.transform, T("ui.hud.save", "Save"), ShowSavePanel, out fbxSaveButtonLabel), 48f, 104f);
        AddButtonLayout(CreateButton(fbxHudRoot.transform, T("ui.hud.load", "Load"), ShowLoadPanel, out fbxLoadButtonLabel), 48f, 104f);
        AddButtonLayout(CreateButton(fbxHudRoot.transform, T("ui.hud.history", "History"), ShowHistory, out fbxHistoryButtonLabel), 48f, 116f);
        AddButtonLayout(CreateButton(fbxHudRoot.transform, T("ui.common.settings", "Settings"), ShowSettings, out fbxSettingsButtonLabel), 48f, 128f);
        AddButtonLayout(CreateButton(fbxHudRoot.transform, T("ui.hud.title", "Title"), ShowMainMenuFromExternalScene, out fbxTitleButtonLabel), 48f, 104f);
        fbxHudRoot.SetActive(false);
    }

    private void BuildToast(Transform parent)
    {
        toastRoot = CreateUiObject("Toast", parent);
        RectTransform toastRect = toastRoot.GetComponent<RectTransform>();
        toastRect.anchorMin = new Vector2(0.5f, 1f);
        toastRect.anchorMax = new Vector2(0.5f, 1f);
        toastRect.pivot = new Vector2(0.5f, 1f);
        toastRect.sizeDelta = new Vector2(760f, 52f);
        toastRect.anchoredPosition = new Vector2(0f, -32f);
        Image toastBg = toastRoot.AddComponent<Image>();
        toastBg.color = new Color(0.08f, 0.045f, 0.11f, 0.78f);

        toastText = CreateText(toastRoot.transform, string.Empty, 21, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.98f, 0.95f, 1f, 1f));
        Stretch(toastText.GetComponent<RectTransform>());
        toastRoot.transform.SetAsLastSibling();
        toastRoot.SetActive(false);
    }

    private void BuildHistory(Transform parent)
    {
        historyRoot = CreateUiObject("History Overlay", parent);
        Stretch(historyRoot.GetComponent<RectTransform>());
        Image overlay = historyRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.52f);

        GameObject panel = CreateUiObject("History Panel", historyRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1080f, 740f);
        Image panelImage = panel.AddComponent<Image>();
        StylePanelSurface(panelImage, UiPanel);

        historyTitleText = CreateText(panel.transform, T("ui.history.title", "历史文本"), 34, FontStyle.Bold, TextAnchor.MiddleLeft, uiPanelTextColor);
        RectTransform titleRect = historyTitleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(44f, -80f);
        titleRect.offsetMax = new Vector2(-44f, -24f);

        GameObject scrollObject = CreateUiObject("History Scroll", panel.transform);
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(44f, 96f);
        scrollRectTransform.offsetMax = new Vector2(-44f, -108f);
        Image scrollImage = scrollObject.AddComponent<Image>();
        scrollImage.color = UiPanelAlt;
        Mask scrollMask = scrollObject.AddComponent<Mask>();
        scrollMask.showMaskGraphic = false;
        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 34f;

        GameObject contentObject = CreateUiObject("History Content", scrollObject.transform);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(0f, 0f);
        contentRect.offsetMax = new Vector2(0f, 0f);
        VerticalLayoutGroup contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(14, 14, 14, 14);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        ContentSizeFitter contentFitter = contentObject.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        historyText = CreateText(contentObject.transform, string.Empty, 24, FontStyle.Normal, TextAnchor.UpperLeft, uiPanelTextColor);
        historyText.resizeTextForBestFit = false;
        historyText.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement historyLayout = historyText.gameObject.AddComponent<LayoutElement>();
        historyLayout.minHeight = 540f;
        scrollRect.content = contentRect;

        historyBackButton = CreateButton(panel.transform, T("ui.common.back", "返回"), ReturnToPreviousOverlayPage, out historyBackButtonLabel);
        RectTransform backRect = historyBackButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(1f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(1f, 0f);
        backRect.sizeDelta = new Vector2(158f, 52f);
        backRect.anchoredPosition = new Vector2(-218f, 34f);

        Button closeButton = CreateButton(panel.transform, T("ui.common.exit", "退出"), ExitOverlayPages, out historyExitButtonLabel);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = new Vector2(1f, 0f);
        closeRect.pivot = new Vector2(1f, 0f);
        closeRect.sizeDelta = new Vector2(158f, 52f);
        closeRect.anchoredPosition = new Vector2(-44f, 34f);

        historyRoot.SetActive(false);
    }

    private void BuildSaveLoadPanel(Transform parent)
    {
        saveLoadRoot = CreateUiObject("Save Load Overlay", parent);
        Stretch(saveLoadRoot.GetComponent<RectTransform>());
        Image overlay = saveLoadRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.5f);

        GameObject panel = CreateUiObject("Save Load Panel", saveLoadRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1120f, 740f);
        Image panelImage = panel.AddComponent<Image>();
        StylePanelSurface(panelImage, UiPanel);

        saveLoadTitleText = CreateText(panel.transform, T("ui.save.title", "存档"), 34, FontStyle.Bold, TextAnchor.MiddleLeft, uiPanelTextColor);
        RectTransform titleRect = saveLoadTitleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(44f, -82f);
        titleRect.offsetMax = new Vector2(-44f, -24f);

        GameObject slots = CreateUiObject("Save Slots", panel.transform);
        saveSlotContainer = slots.transform;
        RectTransform slotsRect = slots.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0f, 0f);
        slotsRect.anchorMax = new Vector2(1f, 1f);
        slotsRect.offsetMin = new Vector2(44f, 98f);
        slotsRect.offsetMax = new Vector2(-44f, -104f);
        VerticalLayoutGroup slotLayout = slots.AddComponent<VerticalLayoutGroup>();
        slotLayout.spacing = 12f;
        slotLayout.childControlWidth = true;
        slotLayout.childControlHeight = false;
        slotLayout.childForceExpandHeight = false;

        saveLoadBackButton = CreateButton(panel.transform, T("ui.common.back", "返回"), ReturnToPreviousOverlayPage, out saveLoadBackButtonLabel);
        RectTransform backRect = saveLoadBackButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(1f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(1f, 0f);
        backRect.sizeDelta = new Vector2(158f, 52f);
        backRect.anchoredPosition = new Vector2(-218f, 34f);

        Button closeButton = CreateButton(panel.transform, T("ui.common.exit", "退出"), ExitOverlayPages, out saveLoadExitButtonLabel);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = new Vector2(1f, 0f);
        closeRect.pivot = new Vector2(1f, 0f);
        closeRect.sizeDelta = new Vector2(158f, 52f);
        closeRect.anchoredPosition = new Vector2(-44f, 34f);

        saveLoadRoot.SetActive(false);
    }

    private void ShowSavePanel()
    {
        ShowSavePanel(false);
    }

    private void ShowSavePanel(bool returnToSettings)
    {
        if (!isInGame)
        {
            ShowToast(T("ui.toast.cannot_save_on_title", "标题界面不能存档。"));
            return;
        }

        saveLoadPanelForSaving = true;
        previousOverlayPage = returnToSettings ? GalOverlayPage.Settings : GalOverlayPage.None;
        ShowSaveLoadPanel();
    }

    private void ShowLoadPanel()
    {
        ShowLoadPanel(false);
    }

    private void ShowLoadPanel(bool returnToSettings)
    {
        saveLoadPanelForSaving = false;
        previousOverlayPage = returnToSettings ? GalOverlayPage.Settings : GalOverlayPage.None;
        ShowSaveLoadPanel();
    }

    private void ShowSaveLoadPanel()
    {
        CloseOverlayPages(true);
        isSaveLoadOpen = true;
        currentOverlayPage = GalOverlayPage.SaveLoad;
        saveLoadRoot.SetActive(true);
        RefreshSaveLoadPanel();
        RefreshOverlayNavigationButtons();
    }

    private void HideSaveLoadPanel()
    {
        ExitOverlayPages();
    }

    private void RefreshSaveLoadPanel()
    {
        if (saveSlotContainer == null || saveLoadRoot == null || !saveLoadRoot.activeSelf)
        {
            return;
        }

        saveLoadTitleText.text = saveLoadPanelForSaving ? T("ui.save.title", "存档") : T("ui.load.title", "读档");

        for (int i = saveSlotContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(saveSlotContainer.GetChild(i).gameObject);
        }

        for (int slot = 1; slot <= SaveSlotCount; slot++)
        {
            CreateSaveSlotRow(slot);
        }
    }

    private void CreateSaveSlotRow(int slot)
    {
        GameObject row = CreateUiObject("Save Slot " + slot, saveSlotContainer);
        LayoutElement rowElement = row.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 84f;
        Image rowImage = row.AddComponent<Image>();
        rowImage.color = new Color(0.965f, 0.93f, 0.99f, 0.34f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 13, 13);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        Text infoText = CreateText(row.transform, GetSlotDescription(slot), 21, FontStyle.Normal, TextAnchor.MiddleLeft, uiPanelTextColor);
        infoText.resizeTextForBestFit = false;
        LayoutElement infoLayout = infoText.gameObject.AddComponent<LayoutElement>();
        infoLayout.preferredWidth = 690f;

        bool hasSave = File.Exists(GetSavePath(slot));
        string actionLabel = saveLoadPanelForSaving ? T("ui.save.action", "保存") : T("ui.load.action", "读取");
        Button actionButton = CreateButton(row.transform, actionLabel, delegate
        {
            if (saveLoadPanelForSaving)
            {
                SaveGameToSlot(slot);
            }
            else
            {
                if (!LoadGameFromSlot(slot))
                {
                    ShowToast(string.Format(T("ui.toast.empty_slot", "槽位 {0} 没有存档。"), slot));
                }
            }
        }, out _);
        actionButton.interactable = saveLoadPanelForSaving || hasSave;
        AddButtonLayout(actionButton, 50f, 118f);

        Button deleteButton = CreateButton(row.transform, T("ui.common.delete", "删除"), delegate { DeleteSaveSlot(slot); }, out _);
        deleteButton.interactable = hasSave;
        AddButtonLayout(deleteButton, 50f, 118f);
    }

    private string GetSlotDescription(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path))
        {
            return string.Format(T("ui.save.slot_empty", "槽位 {0}  空"), slot);
        }

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            GalTemplateSaveData data = JsonUtility.FromJson<GalTemplateSaveData>(json);
            string savedAt = data != null && !string.IsNullOrEmpty(data.savedAt) ? data.savedAt : File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm");
            string location = data != null && data.isExploring
                ? T("ui.save.location_explore", "探索中")
                : T("ui.save.location_dialogue", "对话中");
            string node = data != null ? data.currentNodeId : "unknown";
            return string.Format(T("ui.save.slot_filled", "槽位 {0}  {1}  {2}  {3}"), slot, savedAt, location, node);
        }
        catch
        {
            return string.Format(T("ui.save.slot_broken", "槽位 {0}  存档损坏"), slot);
        }
    }

    private void BuildSettings(Transform parent)
    {
        settingsRoot = CreateUiObject("Settings Overlay", parent);
        Stretch(settingsRoot.GetComponent<RectTransform>());
        Image overlay = settingsRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.48f);

        GameObject panel = CreateUiObject("Settings Panel", settingsRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1120f, 820f);
        Image panelImage = panel.AddComponent<Image>();
        StylePanelSurface(panelImage, UiPanel);

        settingsTitleText = CreateText(panel.transform, T("ui.common.settings", "设置"), 42, FontStyle.Bold, TextAnchor.MiddleLeft, uiPanelTextColor);
        RectTransform titleRect = settingsTitleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(56f, -84f);
        titleRect.offsetMax = new Vector2(-56f, -20f);

        CreateSettingsWash(panel.transform, "Settings Control Wash", new Vector2(318f, -404f), new Vector2(530f, 594f), new Color(1f, 0.9f, 1f, 0.07f));
        CreateSettingsWash(panel.transform, "Settings Action Wash", new Vector2(844f, -390f), new Vector2(440f, 542f), new Color(0.9f, 0.82f, 1f, 0.08f));
        CreateSettingsWash(panel.transform, "Settings Divider", new Vector2(586f, -404f), new Vector2(2f, 590f), new Color(0.28f, 0.2f, 0.3f, 0.1f));

        textSpeedSlider = CreateSettingsSlider(panel.transform, T("ui.settings.text_speed", "文本速度"), new Vector2(318f, -140f), 12f, 80f, settings.textSpeed, true, out settingsTextSpeedLabel, out textSpeedValueText);
        textSpeedSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.textSpeed = Mathf.Round(value);
            textSpeedValueText.text = settings.textSpeed.ToString("0");
            SaveSettings();
        });

        autoDelaySlider = CreateSettingsSlider(panel.transform, T("ui.settings.auto_delay", "自动间隔"), new Vector2(318f, -224f), 0.3f, 3f, settings.autoDelay, false, out settingsAutoDelayLabel, out autoDelayValueText);
        autoDelaySlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.autoDelay = value;
            autoDelayValueText.text = value.ToString("0.0") + "s";
            SaveSettings();
        });

        volumeSlider = CreateSettingsSlider(panel.transform, T("ui.settings.volume", "主音量"), new Vector2(318f, -308f), 0f, 1f, settings.masterVolume, false, out settingsVolumeLabel, out volumeValueText);
        volumeSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.masterVolume = value;
            volumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
            ApplySettings();
            SaveSettings();
        });

        bgmVolumeSlider = CreateSettingsSlider(panel.transform, T("ui.settings.bgm_volume", "音乐音量"), new Vector2(318f, -388f), 0f, 1f, settings.bgmVolume, false, out settingsBgmVolumeLabel, out bgmVolumeValueText);
        bgmVolumeSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.bgmVolume = value;
            bgmVolumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
            ApplyBgmVolume();
            SaveSettings();
        });

        fbxCameraHeightSlider = CreateSettingsSlider(panel.transform, T("ui.settings.camera_height", "摄像头高度"), new Vector2(318f, -468f), -0.4f, 0.6f, settings.fbxCameraHeight, false, out settingsFbxCameraHeightLabel, out fbxCameraHeightValueText);
        fbxCameraHeightSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.fbxCameraHeight = value;
            fbxCameraHeightValueText.text = FormatCameraHeight(value);
            ApplySettings();
            SaveSettings();
        });

        cabinMoodSlider = CreateSettingsSlider(panel.transform, T("ui.settings.cabin_mood", "氛围强度"), new Vector2(318f, -548f), 0f, 1f, settings.cabinMoodIntensity, false, out settingsCabinMoodLabel, out cabinMoodValueText);
        cabinMoodSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.cabinMoodIntensity = value;
            cabinMoodValueText.text = FormatPercent(value);
            ApplySettings();
            SaveSettings();
        });

        titleSaturationSlider = CreateSettingsSlider(panel.transform, T("ui.settings.title_saturation", "标题饱和度"), new Vector2(318f, -628f), 0f, 2f, settings.titleSaturation, false, out settingsTitleSaturationLabel, out titleSaturationValueText);
        titleSaturationSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.titleSaturation = value;
            titleSaturationValueText.text = FormatSaturationPercent(value);
            ApplySettings();
            SaveSettings();
        });

        fullscreenToggle = CreateSettingsToggle(panel.transform, T("ui.settings.fullscreen", "全屏显示"), new Vector2(626f, -140f), settings.fullscreen, out settingsFullscreenLabel);
        fullscreenToggle.onValueChanged.AddListener(delegate(bool value)
        {
            settings.fullscreen = value;
            ApplySettings();
            SaveSettings();
        });

        skipUnreadToggle = CreateSettingsToggle(panel.transform, T("ui.settings.skip_unread", "允许跳过未读文本"), new Vector2(626f, -204f), settings.skipUnreadText, out settingsSkipUnreadLabel);
        skipUnreadToggle.onValueChanged.AddListener(delegate(bool value)
        {
            settings.skipUnreadText = value;
            SaveSettings();
        });

        Button languageButton = CreateSettingsButton(panel.transform, GetLanguageButtonText(), new Vector2(844f, -278f), new Vector2(420f, 56f), CycleLanguage, out languageValueText);
        settingsSavePanelButton = CreateSettingsButton(panel.transform, T("ui.settings.open_save", "打开存档"), new Vector2(738f, -354f), new Vector2(198f, 52f), ShowSavePanelFromSettings, out settingsSavePanelButtonLabel);
        Button loadPanelButton = CreateSettingsButton(panel.transform, T("ui.settings.open_load", "打开读档"), new Vector2(950f, -354f), new Vector2(198f, 52f), ShowLoadPanelFromSettings, out settingsLoadPanelButtonLabel);
        Button historyButton = CreateSettingsButton(panel.transform, T("ui.settings.open_history", "查看历史"), new Vector2(738f, -420f), new Vector2(198f, 52f), ShowHistoryFromSettings, out settingsHistoryButtonLabel);
        Button reloadTextButton = CreateSettingsButton(panel.transform, T("ui.settings.reload_text", "重载文案"), new Vector2(950f, -420f), new Vector2(198f, 52f), ReloadStoryFilesInPlace, out settingsReloadButtonLabel);
        Button deleteButton = CreateSettingsButton(panel.transform, T("ui.settings.delete_save", "删除存档"), new Vector2(738f, -486f), new Vector2(198f, 52f), DeleteSave, out settingsDeleteButtonLabel);
        Button debugButton = CreateSettingsButton(panel.transform, T("ui.settings.portrait_debug", "立绘调试"), new Vector2(950f, -486f), new Vector2(198f, 52f), ShowPortraitDebugFromSettings, out settingsDebugButtonLabel);
        settingsCharacterButton = CreateSettingsButton(panel.transform, T("ui.settings.character", "角色配置"), new Vector2(844f, -560f), new Vector2(420f, 56f), ShowCharacterSettingsFromSettings, out settingsCharacterButtonLabel);

        Button closeButton = CreateButton(panel.transform, T("ui.common.back", "返回"), ExitOverlayPages, out settingsExitButtonLabel);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = new Vector2(1f, 0f);
        closeRect.pivot = new Vector2(1f, 0f);
        closeRect.sizeDelta = new Vector2(176f, 54f);
        closeRect.anchoredPosition = new Vector2(-54f, 38f);

        settingsRoot.SetActive(false);
    }

    private void BuildCharacterSettings(Transform parent)
    {
        characterSettingsRoot = CreateUiObject("Character Settings Overlay", parent);
        Stretch(characterSettingsRoot.GetComponent<RectTransform>());
        Image overlay = characterSettingsRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.12f);

        GameObject panel = CreateUiObject("Character Settings Panel", characterSettingsRoot.transform);
        characterSettingsPanelRect = panel.GetComponent<RectTransform>();
        characterSettingsPanelRect.anchorMin = new Vector2(1f, 0.5f);
        characterSettingsPanelRect.anchorMax = new Vector2(1f, 0.5f);
        characterSettingsPanelRect.pivot = new Vector2(1f, 0.5f);
        characterSettingsPanelRect.sizeDelta = new Vector2(1040f, 700f);
        characterSettingsPanelRect.anchoredPosition = new Vector2(-36f, 0f);
        Image panelImage = panel.AddComponent<Image>();
        StylePanelSurface(panelImage, UiPanel);

        GameObject dragHandle = CreateUiObject("Character Settings Drag Handle", panel.transform);
        RectTransform dragRect = dragHandle.GetComponent<RectTransform>();
        dragRect.anchorMin = new Vector2(0f, 1f);
        dragRect.anchorMax = new Vector2(1f, 1f);
        dragRect.pivot = new Vector2(0.5f, 1f);
        dragRect.offsetMin = new Vector2(0f, -88f);
        dragRect.offsetMax = new Vector2(0f, 0f);
        Image dragImage = dragHandle.AddComponent<Image>();
        dragImage.color = new Color(0.22f, 0.1f, 0.24f, 0.08f);
        GalDraggablePanel draggablePanel = dragHandle.AddComponent<GalDraggablePanel>();
        draggablePanel.target = characterSettingsPanelRect;

        characterSettingsTitleText = CreateText(panel.transform, T("ui.character.title", "角色配置"), 36, FontStyle.Bold, TextAnchor.MiddleLeft, uiPanelTextColor);
        RectTransform titleRect = characterSettingsTitleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(42f, -68f);
        titleRect.offsetMax = new Vector2(-292f, -18f);

        characterDragHintText = CreateText(panel.transform, T("ui.character.drag_hint", "拖动顶部空白处移动面板"), 18, FontStyle.Normal, TextAnchor.MiddleRight, UiInkMuted);
        RectTransform hintRect = characterDragHintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(1f, 1f);
        hintRect.sizeDelta = new Vector2(350f, 38f);
        hintRect.anchoredPosition = new Vector2(-38f, -24f);

        characterPositionTabButton = CreateSettingsButton(panel.transform, T("ui.character.tab_position", "位置"), new Vector2(170f, -116f), new Vector2(196f, 44f), ShowCharacterPositionPage, out characterPositionTabLabel);
        characterImageTabButton = CreateSettingsButton(panel.transform, T("ui.character.tab_images", "图片"), new Vector2(392f, -116f), new Vector2(196f, 44f), ShowCharacterImagePage, out characterImageTabLabel);
        CreateSettingsButton(panel.transform, T("ui.character.reset_panel", "贴右显示"), new Vector2(826f, -116f), new Vector2(250f, 44f), ResetCharacterSettingsPanelPosition, out characterResetPanelButtonLabel);

        characterPositionPage = CreateUiObject("Character Position Page", panel.transform);
        Stretch(characterPositionPage.GetComponent<RectTransform>());

        characterViewportXSlider = CreateSettingsSlider(characterPositionPage.transform, T("ui.character.viewport_x", "横向位置"), new Vector2(290f, -202f), GalFbxSceneController.MinCharacterViewportX, GalFbxSceneController.MaxCharacterViewportX, settings.fbxCharacterViewportX, false, out characterViewportXLabel, out characterViewportXValueText);
        characterViewportXSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.fbxCharacterViewportX = value;
            characterViewportXValueText.text = FormatViewport(value);
            ApplyCharacterSettings();
        });

        characterViewportYSlider = CreateSettingsSlider(characterPositionPage.transform, T("ui.character.viewport_y", "纵向位置"), new Vector2(290f, -292f), GalFbxSceneController.MinCharacterViewportY, GalFbxSceneController.MaxCharacterViewportY, settings.fbxCharacterViewportY, false, out characterViewportYLabel, out characterViewportYValueText);
        characterViewportYSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.fbxCharacterViewportY = value;
            characterViewportYValueText.text = FormatViewport(value);
            ApplyCharacterSettings();
        });

        characterViewportDepthSlider = CreateSettingsSlider(characterPositionPage.transform, T("ui.character.depth", "前后距离"), new Vector2(290f, -382f), GalFbxSceneController.MinCharacterViewportDepth, GalFbxSceneController.MaxCharacterViewportDepth, settings.fbxCharacterViewportDepth, false, out characterViewportDepthLabel, out characterViewportDepthValueText);
        characterViewportDepthSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.fbxCharacterViewportDepth = value;
            characterViewportDepthValueText.text = FormatMeters(value);
            ApplyCharacterSettings();
        });

        characterScreenHeightSlider = CreateSettingsSlider(characterPositionPage.transform, T("ui.character.size", "显示大小"), new Vector2(780f, -202f), GalFbxSceneController.MinCharacterScreenHeight, GalFbxSceneController.MaxCharacterScreenHeight, settings.fbxCharacterScreenHeight, false, out characterScreenHeightLabel, out characterScreenHeightValueText);
        characterScreenHeightSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.fbxCharacterScreenHeight = value;
            characterScreenHeightValueText.text = FormatScalePercent(value);
            ApplyCharacterSettings();
        });

        characterPixelSizeSlider = CreateSettingsSlider(characterPositionPage.transform, T("ui.character.pixel_size", "像素尺寸"), new Vector2(780f, -292f), GalFbxSceneController.MinCharacterPixelSize, GalFbxSceneController.MaxCharacterPixelSize, settings.fbxCharacterPixelSize, true, out characterPixelSizeLabel, out characterPixelSizeValueText);
        characterPixelSizeSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.fbxCharacterPixelSize = Mathf.Round(value);
            characterPixelSizeValueText.text = FormatPixels(settings.fbxCharacterPixelSize);
            ApplyCharacterSettings();
        });

        characterPixelRefinementSlider = CreateSettingsSlider(characterPositionPage.transform, T("ui.character.pixel_refinement", "像素细化"), new Vector2(780f, -382f), GalFbxSceneController.MinCharacterPixelRefinement, GalFbxSceneController.MaxCharacterPixelRefinement, settings.fbxCharacterPixelRefinement, false, out characterPixelRefinementLabel, out characterPixelRefinementValueText);
        characterPixelRefinementSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.fbxCharacterPixelRefinement = value;
            characterPixelRefinementValueText.text = FormatMultiplier(value);
            ApplyCharacterSettings();
        });

        characterMoodBlendSlider = CreateSettingsSlider(characterPositionPage.transform, T("ui.character.mood_blend", "融合程度"), new Vector2(780f, -472f), 0f, 1f, settings.fbxCharacterMoodBlend, false, out characterMoodBlendLabel, out characterMoodBlendValueText);
        characterMoodBlendSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.fbxCharacterMoodBlend = value;
            characterMoodBlendValueText.text = FormatPercent(value);
            ApplyCharacterSettings();
        });

        characterImagePage = CreateUiObject("Character Image Page", panel.transform);
        Stretch(characterImagePage.GetComponent<RectTransform>());

        CreateSettingsButton(characterImagePage.transform, GetCharacterImageButtonText(), new Vector2(520f, -202f), new Vector2(930f, 54f), CycleCharacterImageId, out characterImageButtonLabel);

        characterImportPathLabel = CreateText(characterImagePage.transform, T("ui.character.import_path", "本地图片路径"), 23, FontStyle.Bold, TextAnchor.MiddleLeft, uiPanelTextColor);
        RectTransform importPathLabelRect = characterImportPathLabel.GetComponent<RectTransform>();
        importPathLabelRect.anchorMin = new Vector2(0f, 1f);
        importPathLabelRect.anchorMax = new Vector2(0f, 1f);
        importPathLabelRect.pivot = new Vector2(0f, 0.5f);
        importPathLabelRect.sizeDelta = new Vector2(930f, 42f);
        importPathLabelRect.anchoredPosition = new Vector2(54f, -276f);

        characterImportPathInput = CreateInputField(characterImagePage.transform, T("ui.character.import_path_placeholder", "粘贴 png / jpg / jpeg 文件路径"), new Vector2(520f, -334f), new Vector2(930f, 48f));

        CreateSettingsButton(characterImagePage.transform, T("ui.character.import_from_path", "导入路径"), new Vector2(238f, -414f), new Vector2(270f, 52f), ImportCharacterImageFromPath, out characterImportButtonLabel);
        CreateSettingsButton(characterImagePage.transform, T("ui.character.open_import_folder", "打开导入目录"), new Vector2(532f, -414f), new Vector2(280f, 52f), OpenCharacterImportFolder, out characterOpenImportFolderButtonLabel);
        CreateSettingsButton(characterImagePage.transform, T("ui.character.refresh_images", "刷新列表"), new Vector2(810f, -414f), new Vector2(230f, 52f), RefreshCharacterImageList, out characterRefreshImagesButtonLabel);

        characterImportDirectoryText = CreateText(characterImagePage.transform, string.Empty, 19, FontStyle.Normal, TextAnchor.UpperLeft, UiInkMuted);
        RectTransform directoryRect = characterImportDirectoryText.GetComponent<RectTransform>();
        directoryRect.anchorMin = new Vector2(0f, 1f);
        directoryRect.anchorMax = new Vector2(0f, 1f);
        directoryRect.pivot = new Vector2(0f, 1f);
        directoryRect.sizeDelta = new Vector2(930f, 122f);
        directoryRect.anchoredPosition = new Vector2(54f, -482f);

        Button backButton = CreateButton(panel.transform, T("ui.common.back", "返回"), ReturnToPreviousOverlayPage, out characterSettingsBackButtonLabel);
        characterSettingsBackButton = backButton;
        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(1f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(1f, 0f);
        backRect.sizeDelta = new Vector2(158f, 52f);
        backRect.anchoredPosition = new Vector2(-220f, 34f);

        Button exitButton = CreateButton(panel.transform, T("ui.common.exit", "退出"), ExitOverlayPages, out characterSettingsExitButtonLabel);
        RectTransform exitRect = exitButton.GetComponent<RectTransform>();
        exitRect.anchorMin = new Vector2(1f, 0f);
        exitRect.anchorMax = new Vector2(1f, 0f);
        exitRect.pivot = new Vector2(1f, 0f);
        exitRect.sizeDelta = new Vector2(158f, 52f);
        exitRect.anchoredPosition = new Vector2(-44f, 34f);

        ShowCharacterSettingsPage(false);
        RefreshCharacterSettingsPanel();
        characterSettingsRoot.SetActive(false);
    }

    private void BuildPortraitDebug(Transform parent)
    {
        portraitDebugRoot = CreateUiObject("Portrait Debug Overlay", parent);
        Stretch(portraitDebugRoot.GetComponent<RectTransform>());
        Image overlay = portraitDebugRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.48f);

        GameObject panel = CreateUiObject("Portrait Debug Panel", portraitDebugRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(760f, 560f);
        Image panelImage = panel.AddComponent<Image>();
        StylePanelSurface(panelImage, new Color(0.97f, 0.96f, 0.92f, 0.98f));

        portraitDebugTitleText = CreateText(panel.transform, T("ui.portrait_debug.title", "立绘调试"), 32, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.07f, 0.07f, 0.08f, 1f));
        RectTransform titleRect = portraitDebugTitleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(38f, -72f);
        titleRect.offsetMax = new Vector2(-38f, -24f);

        CreateSettingsButton(panel.transform, GetPortraitDebugSlotText(), new Vector2(220f, -140f), new Vector2(320f, 52f), CycleDebugPortraitSlot, out portraitDebugSlotLabel);
        CreateSettingsButton(panel.transform, GetPortraitDebugCharacterText(), new Vector2(540f, -140f), new Vector2(320f, 52f), CycleDebugPortraitCharacter, out portraitDebugCharacterLabel);
        CreateSettingsButton(panel.transform, GetPortraitDebugExpressionText(), new Vector2(220f, -215f), new Vector2(320f, 52f), CycleDebugPortraitExpression, out portraitDebugExpressionLabel);
        CreateSettingsButton(panel.transform, GetPortraitDebugFacingText(), new Vector2(540f, -215f), new Vector2(320f, 52f), CycleDebugPortraitFacing, out portraitDebugFacingLabel);
        CreateSettingsButton(panel.transform, GetPortraitDebugAnimationText(), new Vector2(220f, -290f), new Vector2(320f, 52f), CycleDebugPortraitAnimation, out portraitDebugAnimationLabel);
        CreateSettingsButton(panel.transform, T("ui.portrait_debug.show", "显示/刷新"), new Vector2(540f, -290f), new Vector2(320f, 52f), ShowDebugPortrait, out _);
        CreateSettingsButton(panel.transform, T("ui.portrait_debug.play_animation", "播放动画"), new Vector2(220f, -365f), new Vector2(320f, 52f), PlayDebugPortraitAnimation, out _);
        CreateSettingsButton(panel.transform, T("ui.portrait_debug.hide_slot", "隐藏当前槽位"), new Vector2(540f, -365f), new Vector2(320f, 52f), HideDebugPortraitSlot, out _);
        CreateSettingsButton(panel.transform, T("ui.portrait_debug.hide_all", "隐藏全部立绘"), new Vector2(220f, -440f), new Vector2(320f, 52f), HideAllPortraitsFromDebug, out _);

        Button backButton = CreateButton(panel.transform, T("ui.common.back", "返回"), ReturnToPreviousOverlayPage, out portraitDebugBackButtonLabel);
        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(1f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(1f, 0f);
        backRect.sizeDelta = new Vector2(140f, 50f);
        backRect.anchoredPosition = new Vector2(-196f, 28f);

        Button exitButton = CreateButton(panel.transform, T("ui.common.exit", "退出"), ExitOverlayPages, out portraitDebugExitButtonLabel);
        RectTransform exitRect = exitButton.GetComponent<RectTransform>();
        exitRect.anchorMin = new Vector2(1f, 0f);
        exitRect.anchorMax = new Vector2(1f, 0f);
        exitRect.pivot = new Vector2(1f, 0f);
        exitRect.sizeDelta = new Vector2(140f, 50f);
        exitRect.anchoredPosition = new Vector2(-38f, 28f);

        portraitDebugRoot.SetActive(false);
    }

    private void ShowMainMenu()
    {
        SaveLastSession();
        CancelAutoAdvance();
        isInGame = false;
        isExploring = false;
        isAutoMode = false;
        isSkipMode = false;
        isDialogueHidden = false;
        isAwaitingChoice = false;
        currentNode = null;
        ClearChoices();
        FinishTypingForMenu();
        mainMenuRoot.SetActive(true);
        hudRoot.SetActive(false);
        exploreRoot.SetActive(false);
        dialogueRoot.SetActive(false);
        CloseOverlayPages(true);
        if (portraitController != null)
        {
            portraitController.HideAll();
        }

        RefreshModeLabels();
        RefreshMenuState();
        SetBackground(string.IsNullOrEmpty(currentBackgroundId) ? story.defaultBackground : currentBackgroundId);
    }

    private void OnPrimaryAction()
    {
        if (HasSave())
        {
            ContinueFromSave();
        }
        else
        {
            StartNewGame();
        }
    }

    private void RefreshMenuState()
    {
        RefreshTitleText();
        primaryActionLabel.text = HasSave() ? T("ui.main.continue", "继续游戏") : T("ui.main.start", "开始游戏");
        newGameButton.interactable = true;
        MoveTitleMenuPointer(454f, false);
    }

    private void RefreshTitleText()
    {
        if (menuTitleText == null)
        {
            return;
        }

        menuTitleText.text = string.Empty;
    }

    private void ShowSettings()
    {
        CloseOverlayPages(true);
        isSettingsOpen = true;
        currentOverlayPage = GalOverlayPage.Settings;
        settingsRoot.SetActive(true);
        textSpeedSlider.SetValueWithoutNotify(settings.textSpeed);
        autoDelaySlider.SetValueWithoutNotify(settings.autoDelay);
        volumeSlider.SetValueWithoutNotify(settings.masterVolume);
        bgmVolumeSlider.SetValueWithoutNotify(settings.bgmVolume);
        fbxCameraHeightSlider.SetValueWithoutNotify(settings.fbxCameraHeight);
        cabinMoodSlider.SetValueWithoutNotify(settings.cabinMoodIntensity);
        titleSaturationSlider.SetValueWithoutNotify(settings.titleSaturation);
        fullscreenToggle.SetIsOnWithoutNotify(settings.fullscreen);
        skipUnreadToggle.SetIsOnWithoutNotify(settings.skipUnreadText);
        textSpeedValueText.text = settings.textSpeed.ToString("0");
        autoDelayValueText.text = settings.autoDelay.ToString("0.0") + "s";
        volumeValueText.text = Mathf.RoundToInt(settings.masterVolume * 100f) + "%";
        bgmVolumeValueText.text = Mathf.RoundToInt(settings.bgmVolume * 100f) + "%";
        fbxCameraHeightValueText.text = FormatCameraHeight(settings.fbxCameraHeight);
        cabinMoodValueText.text = FormatPercent(settings.cabinMoodIntensity);
        titleSaturationValueText.text = FormatSaturationPercent(settings.titleSaturation);
        languageValueText.text = GetLanguageButtonText();
        settingsSavePanelButton.interactable = isInGame;
        RefreshOverlayNavigationButtons();
    }

    private void ShowCharacterSettingsFromSettings()
    {
        ShowCharacterSettings(true);
    }

    private void ShowCharacterSettings()
    {
        ShowCharacterSettings(false);
    }

    private void ShowCharacterSettings(bool returnToSettings)
    {
        if (characterSettingsRoot == null)
        {
            return;
        }

        previousOverlayPage = returnToSettings ? GalOverlayPage.Settings : GalOverlayPage.None;
        CloseOverlayPages(true);
        currentOverlayPage = GalOverlayPage.CharacterSettings;
        characterSettingsRoot.SetActive(true);
        RefreshCharacterSettingsPanel();
        RefreshOverlayNavigationButtons();
    }

    private void ShowCharacterPositionPage()
    {
        ShowCharacterSettingsPage(false);
    }

    private void ShowCharacterImagePage()
    {
        ShowCharacterSettingsPage(true);
    }

    private void ShowCharacterSettingsPage(bool imagePage)
    {
        characterSettingsShowingImagePage = imagePage;
        if (characterPositionPage != null)
        {
            characterPositionPage.SetActive(!imagePage);
        }

        if (characterImagePage != null)
        {
            characterImagePage.SetActive(imagePage);
        }

        RefreshCharacterTabState();
    }

    private void RefreshCharacterTabState()
    {
        SetButtonVisual(characterPositionTabButton, !characterSettingsShowingImagePage);
        SetButtonVisual(characterImageTabButton, characterSettingsShowingImagePage);
    }

    private void ResetCharacterSettingsPanelPosition()
    {
        if (characterSettingsPanelRect != null)
        {
            characterSettingsPanelRect.anchorMin = new Vector2(1f, 0.5f);
            characterSettingsPanelRect.anchorMax = new Vector2(1f, 0.5f);
            characterSettingsPanelRect.pivot = new Vector2(1f, 0.5f);
            characterSettingsPanelRect.anchoredPosition = new Vector2(-36f, 0f);
        }
    }

    private void HideSettings()
    {
        ExitOverlayPages();
    }

    private void ShowSavePanelFromSettings()
    {
        ShowSavePanel(true);
    }

    private void ShowLoadPanelFromSettings()
    {
        ShowLoadPanel(true);
    }

    private void ShowHistoryFromSettings()
    {
        ShowHistory(true);
    }

    private void ShowPortraitDebug()
    {
        ShowPortraitDebug(false);
    }

    private void ShowPortraitDebugFromSettings()
    {
        ShowPortraitDebug(true);
    }

    private void ShowPortraitDebug(bool returnToSettings)
    {
        if (portraitDebugRoot == null)
        {
            return;
        }

        previousOverlayPage = returnToSettings ? GalOverlayPage.Settings : GalOverlayPage.None;
        CloseOverlayPages(true);
        currentOverlayPage = GalOverlayPage.PortraitDebug;
        portraitDebugRoot.SetActive(true);
        RefreshPortraitDebugLabels();
        RefreshOverlayNavigationButtons();
    }

    private void RefreshCharacterSettingsPanel()
    {
        if (characterSettingsRoot == null)
        {
            return;
        }

        SetText(characterSettingsTitleText, T("ui.character.title", "角色配置"));
        SetText(characterDragHintText, T("ui.character.drag_hint", "拖动顶部空白处移动面板"));
        SetText(characterPositionTabLabel, T("ui.character.tab_position", "位置"));
        SetText(characterImageTabLabel, T("ui.character.tab_images", "图片"));
        SetText(characterImageButtonLabel, GetCharacterImageButtonText());
        SetText(characterViewportXLabel, T("ui.character.viewport_x", "横向位置"));
        SetText(characterViewportYLabel, T("ui.character.viewport_y", "纵向位置"));
        SetText(characterViewportDepthLabel, T("ui.character.depth", "前后距离"));
        SetText(characterScreenHeightLabel, T("ui.character.size", "显示大小"));
        SetText(characterPixelSizeLabel, T("ui.character.pixel_size", "像素尺寸"));
        SetText(characterPixelRefinementLabel, T("ui.character.pixel_refinement", "像素细化"));
        SetText(characterMoodBlendLabel, T("ui.character.mood_blend", "融合程度"));
        SetText(characterImportPathLabel, T("ui.character.import_path", "本地图片路径"));
        SetInputPlaceholder(characterImportPathInput, T("ui.character.import_path_placeholder", "粘贴 png / jpg / jpeg 文件路径"));
        SetText(characterImportButtonLabel, T("ui.character.import_from_path", "导入路径"));
        SetText(characterOpenImportFolderButtonLabel, T("ui.character.open_import_folder", "打开导入目录"));
        SetText(characterRefreshImagesButtonLabel, T("ui.character.refresh_images", "刷新列表"));
        SetText(characterResetPanelButtonLabel, T("ui.character.reset_panel", "贴右显示"));
        SetText(characterSettingsBackButtonLabel, T("ui.common.back", "返回"));
        SetText(characterSettingsExitButtonLabel, T("ui.common.exit", "退出"));

        characterViewportXSlider.SetValueWithoutNotify(settings.fbxCharacterViewportX);
        characterViewportYSlider.SetValueWithoutNotify(settings.fbxCharacterViewportY);
        characterViewportDepthSlider.SetValueWithoutNotify(settings.fbxCharacterViewportDepth);
        characterScreenHeightSlider.SetValueWithoutNotify(settings.fbxCharacterScreenHeight);
        characterPixelSizeSlider.SetValueWithoutNotify(settings.fbxCharacterPixelSize);
        characterPixelRefinementSlider.SetValueWithoutNotify(settings.fbxCharacterPixelRefinement);
        characterMoodBlendSlider.SetValueWithoutNotify(settings.fbxCharacterMoodBlend);

        characterViewportXValueText.text = FormatViewport(settings.fbxCharacterViewportX);
        characterViewportYValueText.text = FormatViewport(settings.fbxCharacterViewportY);
        characterViewportDepthValueText.text = FormatMeters(settings.fbxCharacterViewportDepth);
        characterScreenHeightValueText.text = FormatScalePercent(settings.fbxCharacterScreenHeight);
        characterPixelSizeValueText.text = FormatPixels(settings.fbxCharacterPixelSize);
        characterPixelRefinementValueText.text = FormatMultiplier(settings.fbxCharacterPixelRefinement);
        characterMoodBlendValueText.text = FormatPercent(settings.fbxCharacterMoodBlend);
        SetText(characterImportDirectoryText, string.Format(T("ui.character.import_directory", "导入目录：{0}\n支持格式：png / jpg / jpeg\n列表数量：{1}"), GalFbxSceneController.GetLocalCharacterImportDirectory(), GetCharacterImageIds().Length));
        RefreshCharacterTabState();
    }

    private void RefreshCharacterSettingsLabels()
    {
        SetText(characterSettingsTitleText, T("ui.character.title", "角色配置"));
        SetText(characterDragHintText, T("ui.character.drag_hint", "拖动顶部空白处移动面板"));
        SetText(characterPositionTabLabel, T("ui.character.tab_position", "位置"));
        SetText(characterImageTabLabel, T("ui.character.tab_images", "图片"));
        SetText(characterImageButtonLabel, GetCharacterImageButtonText());
        SetText(characterViewportXLabel, T("ui.character.viewport_x", "横向位置"));
        SetText(characterViewportYLabel, T("ui.character.viewport_y", "纵向位置"));
        SetText(characterViewportDepthLabel, T("ui.character.depth", "前后距离"));
        SetText(characterScreenHeightLabel, T("ui.character.size", "显示大小"));
        SetText(characterPixelSizeLabel, T("ui.character.pixel_size", "像素尺寸"));
        SetText(characterPixelRefinementLabel, T("ui.character.pixel_refinement", "像素细化"));
        SetText(characterMoodBlendLabel, T("ui.character.mood_blend", "融合程度"));
        SetText(characterImportPathLabel, T("ui.character.import_path", "本地图片路径"));
        SetInputPlaceholder(characterImportPathInput, T("ui.character.import_path_placeholder", "粘贴 png / jpg / jpeg 文件路径"));
        SetText(characterImportButtonLabel, T("ui.character.import_from_path", "导入路径"));
        SetText(characterOpenImportFolderButtonLabel, T("ui.character.open_import_folder", "打开导入目录"));
        SetText(characterRefreshImagesButtonLabel, T("ui.character.refresh_images", "刷新列表"));
        SetText(characterResetPanelButtonLabel, T("ui.character.reset_panel", "贴右显示"));
        SetText(characterSettingsBackButtonLabel, T("ui.common.back", "返回"));
        SetText(characterSettingsExitButtonLabel, T("ui.common.exit", "退出"));
    }

    private void ApplyCharacterSettings()
    {
        NormalizeCharacterSettings();
        GalFbxSceneController.Instance.SetCharacterSettings(
            settings.fbxCharacterImageId,
            settings.fbxCharacterViewportX,
            settings.fbxCharacterViewportY,
            settings.fbxCharacterViewportDepth,
            settings.fbxCharacterScreenHeight,
            settings.fbxCharacterPixelSize,
            settings.fbxCharacterPixelRefinement,
            settings.fbxCharacterMoodBlend);
        SaveSettings();
    }

    private void ImportCharacterImageFromPath()
    {
        if (characterImportPathInput == null)
        {
            return;
        }

        string sourcePath = CleanLocalPath(characterImportPathInput.text);
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            ShowToast(T("ui.toast.character_import_missing", "没有找到这个图片文件。"));
            return;
        }

        if (!IsSupportedCharacterImagePath(sourcePath))
        {
            ShowToast(T("ui.toast.character_import_format", "仅支持 png / jpg / jpeg。"));
            return;
        }

        try
        {
            string importDirectory = GalFbxSceneController.GetLocalCharacterImportDirectory();
            Directory.CreateDirectory(importDirectory);
            string targetName = GetUniqueCharacterImportFileName(importDirectory, Path.GetFileName(sourcePath));
            string targetPath = Path.Combine(importDirectory, targetName);
            File.Copy(sourcePath, targetPath, false);

            settings.fbxCharacterImageId = GalFbxSceneController.LocalCharacterImagePrefix + targetName;
            ApplyCharacterSettings();
            RefreshCharacterSettingsPanel();
            ShowToast(string.Format(T("ui.toast.character_imported", "已导入角色图片：{0}"), targetName));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GAL character import failed: " + ex.Message);
            ShowToast(T("ui.toast.character_import_failed", "角色图片导入失败。"));
        }
    }

    private void OpenCharacterImportFolder()
    {
        string importDirectory = GalFbxSceneController.GetLocalCharacterImportDirectory();
        Directory.CreateDirectory(importDirectory);
        Application.OpenURL("file:///" + importDirectory.Replace('\\', '/'));
        ShowToast(string.Format(T("ui.toast.character_import_folder", "导入目录：{0}"), importDirectory));
    }

    private void RefreshCharacterImageList()
    {
        RefreshCharacterSettingsPanel();
        ShowToast(string.Format(T("ui.toast.character_images_refreshed", "角色图片列表已刷新：{0} 项"), GetCharacterImageIds().Length));
    }

    private void CycleCharacterImageId()
    {
        string[] imageIds = GetCharacterImageIds();
        if (imageIds.Length == 0)
        {
            return;
        }

        settings.fbxCharacterImageId = NextValue(settings.fbxCharacterImageId, imageIds);
        ApplyCharacterSettings();
        RefreshCharacterSettingsPanel();
        ShowToast(string.Format(T("ui.toast.character_image_changed", "角色图像：{0}"), settings.fbxCharacterImageId));
    }

    private string GetCharacterImageButtonText()
    {
        return string.Format(T("ui.character.image_id", "图像ID：{0}"), GetCharacterImageDisplayName(settings.fbxCharacterImageId));
    }

    private string[] GetCharacterImageIds()
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>("Characters");
        List<string> ids = new List<string>();
        if (textures != null)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                if (texture != null && !string.IsNullOrEmpty(texture.name))
                {
                    ids.Add(texture.name);
                }
            }
        }

        AddLocalCharacterImageIds(ids);

        if (!ids.Contains(GalFbxSceneController.DefaultCharacterImageId))
        {
            ids.Insert(0, GalFbxSceneController.DefaultCharacterImageId);
        }

        return ids.Count == 0 ? new[] { GalFbxSceneController.DefaultCharacterImageId } : ids.ToArray();
    }

    private void AddLocalCharacterImageIds(List<string> ids)
    {
        string importDirectory = GalFbxSceneController.GetLocalCharacterImportDirectory();
        if (!Directory.Exists(importDirectory))
        {
            return;
        }

        string[] files = Directory.GetFiles(importDirectory);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < files.Length; i++)
        {
            string path = files[i];
            if (!IsSupportedCharacterImagePath(path))
            {
                continue;
            }

            string id = GalFbxSceneController.LocalCharacterImagePrefix + Path.GetFileName(path);
            if (!ids.Contains(id))
            {
                ids.Add(id);
            }
        }
    }

    private static string GetCharacterImageDisplayName(string imageId)
    {
        if (string.IsNullOrWhiteSpace(imageId))
        {
            return GalFbxSceneController.DefaultCharacterImageId;
        }

        if (imageId.StartsWith(GalFbxSceneController.LocalCharacterImagePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return imageId;
        }

        return imageId;
    }

    private static string CleanLocalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string cleaned = path.Trim();
        if (cleaned.Length >= 2 && cleaned[0] == '"' && cleaned[cleaned.Length - 1] == '"')
        {
            cleaned = cleaned.Substring(1, cleaned.Length - 2);
        }

        return cleaned;
    }

    private static bool IsSupportedCharacterImagePath(string path)
    {
        string extension = Path.GetExtension(path);
        for (int i = 0; i < CharacterImportExtensions.Length; i++)
        {
            if (string.Equals(extension, CharacterImportExtensions[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetUniqueCharacterImportFileName(string directory, string fileName)
    {
        string cleanFileName = Path.GetFileName(fileName);
        string name = Path.GetFileNameWithoutExtension(cleanFileName);
        string extension = Path.GetExtension(cleanFileName);
        string candidate = cleanFileName;
        int index = 2;
        while (File.Exists(Path.Combine(directory, candidate)))
        {
            candidate = name + "_" + index.ToString("00") + extension;
            index++;
        }

        return candidate;
    }

    private void CycleDebugPortraitSlot()
    {
        debugPortraitSlot = NextValue(debugPortraitSlot, new[] { "left", "center", "right" });
        RefreshPortraitDebugLabels();
    }

    private void CycleDebugPortraitCharacter()
    {
        debugPortraitCharacter = NextValue(debugPortraitCharacter, GetPortraitIds());
        RefreshPortraitDebugLabels();
    }

    private void CycleDebugPortraitExpression()
    {
        debugPortraitExpression = NextValue(debugPortraitExpression, new[] { "neutral", "happy", "sad", "angry", "surprised" });
        RefreshPortraitDebugLabels();
    }

    private void CycleDebugPortraitFacing()
    {
        debugPortraitFacing = NextValue(debugPortraitFacing, new[] { "auto", "right", "left" });
        RefreshPortraitDebugLabels();
    }

    private void CycleDebugPortraitAnimation()
    {
        debugPortraitAnimation = NextValue(debugPortraitAnimation, new[] { "none", "shake", "bounce", "pop", "fade" });
        RefreshPortraitDebugLabels();
    }

    private void ShowDebugPortrait()
    {
        if (portraitController == null)
        {
            return;
        }

        portraitController.Show(new GalPortraitPose
        {
            slot = debugPortraitSlot,
            character = debugPortraitCharacter,
            expression = debugPortraitExpression,
            facing = debugPortraitFacing,
            animation = debugPortraitAnimation
        });
    }

    private void PlayDebugPortraitAnimation()
    {
        if (portraitController != null)
        {
            portraitController.PlayAnimation(debugPortraitSlot, debugPortraitAnimation);
        }
    }

    private void HideDebugPortraitSlot()
    {
        if (portraitController != null)
        {
            portraitController.Hide(debugPortraitSlot);
        }
    }

    private void HideAllPortraitsFromDebug()
    {
        if (portraitController != null)
        {
            portraitController.HideAll();
        }
    }

    private void RefreshPortraitDebugLabels()
    {
        SetText(portraitDebugTitleText, T("ui.portrait_debug.title", "立绘调试"));
        SetText(portraitDebugSlotLabel, GetPortraitDebugSlotText());
        SetText(portraitDebugCharacterLabel, GetPortraitDebugCharacterText());
        SetText(portraitDebugExpressionLabel, GetPortraitDebugExpressionText());
        SetText(portraitDebugFacingLabel, GetPortraitDebugFacingText());
        SetText(portraitDebugAnimationLabel, GetPortraitDebugAnimationText());
        SetText(portraitDebugBackButtonLabel, T("ui.common.back", "返回"));
        SetText(portraitDebugExitButtonLabel, T("ui.common.exit", "退出"));
    }

    private string GetPortraitDebugSlotText()
    {
        return string.Format(T("ui.portrait_debug.slot", "位置：{0}"), debugPortraitSlot);
    }

    private string GetPortraitDebugCharacterText()
    {
        return string.Format(T("ui.portrait_debug.character", "角色：{0}"), debugPortraitCharacter);
    }

    private string GetPortraitDebugExpressionText()
    {
        return string.Format(T("ui.portrait_debug.expression", "差分：{0}"), debugPortraitExpression);
    }

    private string GetPortraitDebugFacingText()
    {
        return string.Format(T("ui.portrait_debug.facing", "朝向：{0}"), debugPortraitFacing);
    }

    private string GetPortraitDebugAnimationText()
    {
        return string.Format(T("ui.portrait_debug.animation", "动画：{0}"), debugPortraitAnimation);
    }

    private string[] GetPortraitIds()
    {
        if (story == null || story.portraits == null || story.portraits.Count == 0)
        {
            return new[] { "test" };
        }

        List<string> ids = new List<string>();
        foreach (GalPortraitEntry portrait in story.portraits)
        {
            if (portrait != null && !string.IsNullOrEmpty(portrait.id))
            {
                ids.Add(portrait.id);
            }
        }

        return ids.Count == 0 ? new[] { "test" } : ids.ToArray();
    }

    private static string NextValue(string current, string[] values)
    {
        if (values == null || values.Length == 0)
        {
            return current;
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == current)
            {
                return values[(i + 1) % values.Length];
            }
        }

        return values[0];
    }

    private void CycleLanguage()
    {
        if (story.languages == null || story.languages.Count == 0)
        {
            ShowToast(T("ui.toast.no_languages", "尚未配置多语言表。"));
            return;
        }

        int index = FindLanguageIndex(settings.language);
        index = (index + 1) % story.languages.Count;
        settings.language = story.languages[index].id;
        SaveSettings();
        ReloadStoryFilesInPlace();
        ShowToast(string.Format(T("ui.toast.language_changed", "语言预设：{0}"), GetLanguageDisplayName(settings.language)));
    }

    private string GetLanguageButtonText()
    {
        return string.Format(T("ui.settings.language", "语言：{0}"), GetLanguageDisplayName(settings.language));
    }

    private string GetLanguageDisplayName(string languageId)
    {
        if (story.languages != null)
        {
            foreach (GalLanguageEntry language in story.languages)
            {
                if (language != null && language.id == languageId)
                {
                    return string.IsNullOrEmpty(language.displayName) ? language.id : language.displayName;
                }
            }
        }

        return string.IsNullOrEmpty(languageId) ? "未配置" : languageId;
    }

    private int FindLanguageIndex(string languageId)
    {
        if (story.languages == null)
        {
            return -1;
        }

        for (int i = 0; i < story.languages.Count; i++)
        {
            if (story.languages[i] != null && story.languages[i].id == languageId)
            {
                return i;
            }
        }

        return -1;
    }

    private void LoadSettings()
    {
        settings.textSpeed = PlayerPrefs.GetFloat("GalTemplate.TextSpeed", settings.textSpeed);
        settings.autoDelay = PlayerPrefs.GetFloat("GalTemplate.AutoDelay", settings.autoDelay);
        settings.masterVolume = PlayerPrefs.GetFloat("GalTemplate.MasterVolume", settings.masterVolume);
        settings.bgmVolume = PlayerPrefs.GetFloat("GalTemplate.BgmVolume", settings.bgmVolume);
        settings.fbxCameraHeight = PlayerPrefs.GetFloat("GalTemplate.FbxCameraHeight", settings.fbxCameraHeight);
        settings.cabinMoodIntensity = PlayerPrefs.GetFloat("GalTemplate.CabinMoodIntensity", settings.cabinMoodIntensity);
        settings.titleSaturation = PlayerPrefs.GetFloat("GalTemplate.TitleSaturation", settings.titleSaturation);
        settings.fbxCharacterImageId = PlayerPrefs.GetString("GalTemplate.FbxCharacterImageId", settings.fbxCharacterImageId);
        settings.fbxCharacterViewportX = PlayerPrefs.GetFloat("GalTemplate.FbxCharacterViewportX", settings.fbxCharacterViewportX);
        settings.fbxCharacterViewportY = PlayerPrefs.GetFloat("GalTemplate.FbxCharacterViewportY", settings.fbxCharacterViewportY);
        settings.fbxCharacterViewportDepth = PlayerPrefs.GetFloat("GalTemplate.FbxCharacterViewportDepth", settings.fbxCharacterViewportDepth);
        settings.fbxCharacterScreenHeight = PlayerPrefs.GetFloat("GalTemplate.FbxCharacterScreenHeight", settings.fbxCharacterScreenHeight);
        settings.fbxCharacterPixelSize = PlayerPrefs.GetFloat("GalTemplate.FbxCharacterPixelSize", settings.fbxCharacterPixelSize);
        settings.fbxCharacterPixelRefinement = PlayerPrefs.GetFloat("GalTemplate.FbxCharacterPixelRefinement", settings.fbxCharacterPixelRefinement);
        settings.fbxCharacterMoodBlend = PlayerPrefs.GetFloat("GalTemplate.FbxCharacterMoodBlend", settings.fbxCharacterMoodBlend);
        settings.fullscreen = PlayerPrefs.GetInt("GalTemplate.Fullscreen", settings.fullscreen ? 1 : 0) == 1;
        settings.skipUnreadText = PlayerPrefs.GetInt("GalTemplate.SkipUnreadText", settings.skipUnreadText ? 1 : 0) == 1;
        settings.language = PlayerPrefs.GetString("GalTemplate.Language", settings.language);
        settings.artProfile = PlayerPrefs.GetString("GalTemplate.ArtProfile", settings.artProfile);
        NormalizeCharacterSettings();
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("GalTemplate.TextSpeed", settings.textSpeed);
        PlayerPrefs.SetFloat("GalTemplate.AutoDelay", settings.autoDelay);
        PlayerPrefs.SetFloat("GalTemplate.MasterVolume", settings.masterVolume);
        PlayerPrefs.SetFloat("GalTemplate.BgmVolume", settings.bgmVolume);
        PlayerPrefs.SetFloat("GalTemplate.FbxCameraHeight", settings.fbxCameraHeight);
        PlayerPrefs.SetFloat("GalTemplate.CabinMoodIntensity", settings.cabinMoodIntensity);
        PlayerPrefs.SetFloat("GalTemplate.TitleSaturation", settings.titleSaturation);
        PlayerPrefs.SetString("GalTemplate.FbxCharacterImageId", settings.fbxCharacterImageId);
        PlayerPrefs.SetFloat("GalTemplate.FbxCharacterViewportX", settings.fbxCharacterViewportX);
        PlayerPrefs.SetFloat("GalTemplate.FbxCharacterViewportY", settings.fbxCharacterViewportY);
        PlayerPrefs.SetFloat("GalTemplate.FbxCharacterViewportDepth", settings.fbxCharacterViewportDepth);
        PlayerPrefs.SetFloat("GalTemplate.FbxCharacterScreenHeight", settings.fbxCharacterScreenHeight);
        PlayerPrefs.SetFloat("GalTemplate.FbxCharacterPixelSize", settings.fbxCharacterPixelSize);
        PlayerPrefs.SetFloat("GalTemplate.FbxCharacterPixelRefinement", settings.fbxCharacterPixelRefinement);
        PlayerPrefs.SetFloat("GalTemplate.FbxCharacterMoodBlend", settings.fbxCharacterMoodBlend);
        PlayerPrefs.SetInt("GalTemplate.Fullscreen", settings.fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("GalTemplate.SkipUnreadText", settings.skipUnreadText ? 1 : 0);
        PlayerPrefs.SetString("GalTemplate.Language", settings.language);
        PlayerPrefs.SetString("GalTemplate.ArtProfile", settings.artProfile);
        PlayerPrefs.Save();
    }

    private void ApplySettings()
    {
        NormalizeCharacterSettings();
        settings.titleSaturation = Mathf.Clamp(settings.titleSaturation, 0f, 2f);
        settings.bgmVolume = Mathf.Clamp01(settings.bgmVolume);
        AudioListener.volume = Mathf.Clamp01(settings.masterVolume);
        ApplyBgmVolume();
        Screen.fullScreen = settings.fullscreen;
        ApplyTitleSaturation();
        GalFbxSceneController controller = GalFbxSceneController.Instance;
        controller.SetCameraHeightOffset(settings.fbxCameraHeight);
        controller.SetMoodIntensity(settings.cabinMoodIntensity);
        controller.SetCharacterSettings(
            settings.fbxCharacterImageId,
            settings.fbxCharacterViewportX,
            settings.fbxCharacterViewportY,
            settings.fbxCharacterViewportDepth,
            settings.fbxCharacterScreenHeight,
            settings.fbxCharacterPixelSize,
            settings.fbxCharacterPixelRefinement,
            settings.fbxCharacterMoodBlend);
    }

    private void EnsureBgmSource()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.GetComponent<AudioSource>();
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }

            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;
            bgmSource.priority = 128;
        }

        if (bgmClip == null && !bgmLoadAttempted)
        {
            bgmLoadAttempted = true;
            bgmClip = Resources.Load<AudioClip>(DefaultBgmResourcePath);
            if (bgmClip == null)
            {
                Debug.LogWarning("GAL BGM clip not found: Resources/" + DefaultBgmResourcePath);
            }
        }

        if (bgmClip != null && bgmSource.clip != bgmClip)
        {
            bgmSource.clip = bgmClip;
        }

        ApplyBgmVolume();

        if (bgmSource.clip != null && !bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    private void ApplyBgmVolume()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.volume = Mathf.Clamp01(settings.bgmVolume);
    }

    private void StopBgm()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    private void NormalizeCharacterSettings()
    {
        if (string.IsNullOrWhiteSpace(settings.fbxCharacterImageId))
        {
            settings.fbxCharacterImageId = GalFbxSceneController.DefaultCharacterImageId;
        }

        settings.fbxCharacterViewportX = Mathf.Clamp(settings.fbxCharacterViewportX, GalFbxSceneController.MinCharacterViewportX, GalFbxSceneController.MaxCharacterViewportX);
        settings.fbxCharacterViewportY = Mathf.Clamp(settings.fbxCharacterViewportY, GalFbxSceneController.MinCharacterViewportY, GalFbxSceneController.MaxCharacterViewportY);
        settings.fbxCharacterViewportDepth = Mathf.Clamp(settings.fbxCharacterViewportDepth, GalFbxSceneController.MinCharacterViewportDepth, GalFbxSceneController.MaxCharacterViewportDepth);
        settings.fbxCharacterScreenHeight = Mathf.Clamp(settings.fbxCharacterScreenHeight, GalFbxSceneController.MinCharacterScreenHeight, GalFbxSceneController.MaxCharacterScreenHeight);
        settings.fbxCharacterPixelSize = Mathf.Clamp(Mathf.Round(settings.fbxCharacterPixelSize), GalFbxSceneController.MinCharacterPixelSize, GalFbxSceneController.MaxCharacterPixelSize);
        settings.fbxCharacterPixelRefinement = Mathf.Clamp(settings.fbxCharacterPixelRefinement, GalFbxSceneController.MinCharacterPixelRefinement, GalFbxSceneController.MaxCharacterPixelRefinement);
        settings.fbxCharacterMoodBlend = Mathf.Clamp01(settings.fbxCharacterMoodBlend);
    }

    private static string FormatCameraHeight(float value)
    {
        return (value >= 0f ? "+" : string.Empty) + value.ToString("0.00") + "m";
    }

    private static string FormatPercent(float value)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }

    private static string FormatSaturationPercent(float value)
    {
        return Mathf.RoundToInt(Mathf.Clamp(value, 0f, 2f) * 100f) + "%";
    }

    private static string FormatViewport(float value)
    {
        return value.ToString("0.000");
    }

    private static string FormatScalePercent(float value)
    {
        return Mathf.RoundToInt(Mathf.Clamp(value, GalFbxSceneController.MinCharacterScreenHeight, GalFbxSceneController.MaxCharacterScreenHeight) * 100f) + "%";
    }

    private static string FormatMeters(float value)
    {
        return value.ToString("0.00") + "m";
    }

    private static string FormatPixels(float value)
    {
        return Mathf.RoundToInt(Mathf.Max(1f, value)).ToString("0") + "px";
    }

    private static string FormatMultiplier(float value)
    {
        return Mathf.Clamp(value, GalFbxSceneController.MinCharacterPixelRefinement, GalFbxSceneController.MaxCharacterPixelRefinement).ToString("0.0") + "x";
    }

    private void ShowToast(string message)
    {
        if (toastRoutine != null)
        {
            StopCoroutine(toastRoutine);
        }

        toastRoutine = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastText.text = message;
        toastRoot.SetActive(true);
        yield return new WaitForSecondsRealtime(1.8f);
        toastRoot.SetActive(false);
        toastRoutine = null;
    }

    private void ClearChoices()
    {
        if (choiceContainer == null)
        {
            return;
        }

        for (int i = choiceContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(choiceContainer.GetChild(i).gameObject);
        }
    }

    private void FinishTypingForMenu()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        isTyping = false;
    }

    private Text CreateText(Transform parent, string text, int size, FontStyle style, TextAnchor anchor, Color color)
    {
        GameObject textObject = CreateUiObject("Text", parent);
        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.font = GetFont(size);
        uiText.fontSize = size;
        uiText.fontStyle = style;
        uiText.alignment = anchor;
        uiText.color = color;
        uiText.supportRichText = true;
        uiText.raycastTarget = false;
        uiText.lineSpacing = 1.08f;
        uiText.resizeTextForBestFit = true;
        uiText.resizeTextMinSize = Mathf.Max(12, size - 10);
        uiText.resizeTextMaxSize = size;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Truncate;
        return uiText;
    }

    private InputField CreateInputField(Transform parent, string placeholder, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject inputObject = CreateUiObject("Input Field", parent);
        RectTransform inputRect = inputObject.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 1f);
        inputRect.anchorMax = new Vector2(0f, 1f);
        inputRect.pivot = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = size;
        inputRect.anchoredPosition = anchoredPosition;

        Image background = inputObject.AddComponent<Image>();
        background.color = new Color(0.98f, 0.95f, 0.98f, 0.9f);

        InputField inputField = inputObject.AddComponent<InputField>();
        inputField.targetGraphic = background;
        inputField.lineType = InputField.LineType.SingleLine;
        inputField.characterLimit = 1024;
        inputField.caretColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        inputField.selectionColor = new Color(0.72f, 0.44f, 0.78f, 0.32f);

        Text textComponent = CreateText(inputObject.transform, string.Empty, 19, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.08f, 0.08f, 0.08f, 1f));
        RectTransform textRect = textComponent.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 0f);
        textRect.offsetMax = new Vector2(-16f, 0f);
        textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        textComponent.raycastTarget = false;

        Text placeholderText = CreateText(inputObject.transform, placeholder, 18, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.35f, 0.35f, 0.3f, 0.55f));
        RectTransform placeholderRect = placeholderText.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(16f, 0f);
        placeholderRect.offsetMax = new Vector2(-16f, 0f);
        placeholderText.horizontalOverflow = HorizontalWrapMode.Overflow;
        placeholderText.verticalOverflow = VerticalWrapMode.Truncate;
        placeholderText.raycastTarget = false;

        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderText;
        return inputField;
    }

    private void StylePanelSurface(Image image, Color color)
    {
        if (image == null)
        {
            return;
        }

        image.color = color;
        Outline outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = UiPanelLine;
        outline.effectDistance = new Vector2(1.2f, 1.2f);

        Shadow shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.17f, 0.07f, 0.18f, 0.18f);
        shadow.effectDistance = new Vector2(8f, -8f);
    }

    private void AddTextShadow(Text text, Color color, Vector2 distance)
    {
        if (text == null)
        {
            return;
        }

        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    private void CreateSettingsWash(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject wash = CreateUiObject(name, parent);
        RectTransform rect = wash.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        Image image = wash.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private Button CreateButton(Transform parent, string label, UnityAction onClick, out Text labelText)
    {
        GameObject buttonObject = CreateUiObject(label + " Button", parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = uiButtonNormalSprite == null ? UiGlassNormal : new Color(1f, 0.95f, 1f, 0.54f);
        if (uiButtonNormalSprite != null)
        {
            image.sprite = uiButtonNormalSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
        }

        Outline buttonOutline = buttonObject.AddComponent<Outline>();
        buttonOutline.effectColor = UiAccentSoft;
        buttonOutline.effectDistance = new Vector2(1.4f, 1.4f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.96f, 1f, 1f);
        colors.pressedColor = new Color(0.94f, 0.82f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.48f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        int buttonFontSize = label.Length > 12 ? 20 : label.Length > 6 ? 22 : 24;
        labelText = CreateText(buttonObject.transform, label, buttonFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, uiButtonTextColor);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        Stretch(labelRect);
        labelRect.offsetMin = new Vector2(28f, 0f);
        labelRect.offsetMax = new Vector2(-28f, 0f);
        labelText.resizeTextForBestFit = true;
        labelText.resizeTextMinSize = 16;
        labelText.resizeTextMaxSize = buttonFontSize;
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Truncate;
        Shadow shadow = labelText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = uiButtonTextShadowColor;
        shadow.effectDistance = new Vector2(2f, -2f);

        Outline outline = labelText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(uiButtonTextShadowColor.r, uiButtonTextShadowColor.g, uiButtonTextShadowColor.b, 0.36f);
        outline.effectDistance = new Vector2(1f, 1f);

        if (uiButtonNormalSprite != null)
        {
            GalButtonAnimator animator = buttonObject.AddComponent<GalButtonAnimator>();
            GalUiSkinAnimation animation = activeUiSkin == null || activeUiSkin.animation == null ? new GalUiSkinAnimation() : activeUiSkin.animation;
            animator.Configure(image, uiButtonNormalSprite, uiButtonHoverSprite, uiButtonPressedSprite, animation.hoverScale, animation.pressedScale);
        }
        return button;
    }

    private Slider CreateSettingsSlider(Transform parent, string label, Vector2 anchoredPosition, float min, float max, float value, bool wholeNumbers, out Text labelText, out Text valueText)
    {
        GameObject row = CreateUiObject(label + " Setting Row", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(500f, 62f);
        rowRect.anchoredPosition = anchoredPosition;

        labelText = CreateText(row.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleLeft, uiPanelTextColor);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(166f, 44f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);

        Slider slider = CreateSlider(row.transform);
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(0f, 0.5f);
        sliderRect.pivot = new Vector2(0f, 0.5f);
        sliderRect.sizeDelta = new Vector2(244f, 32f);
        sliderRect.anchoredPosition = new Vector2(178f, 0f);
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.value = value;

        string displayValue = wholeNumbers ? Mathf.Round(value).ToString("0") : value.ToString("0.0");
        valueText = CreateText(row.transform, displayValue, 20, FontStyle.Bold, TextAnchor.MiddleRight, UiInkMuted);
        RectTransform valueRect = valueText.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.sizeDelta = new Vector2(88f, 44f);
        valueRect.anchoredPosition = new Vector2(0f, 0f);

        return slider;
    }

    private Toggle CreateSettingsToggle(Transform parent, string label, Vector2 anchoredPosition, bool value, out Text labelText)
    {
        GameObject row = CreateUiObject(label + " Setting Toggle", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 0.5f);
        rowRect.sizeDelta = new Vector2(424f, 52f);
        rowRect.anchoredPosition = anchoredPosition;

        labelText = CreateText(row.transform, label, 21, FontStyle.Bold, TextAnchor.MiddleLeft, uiPanelTextColor);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(360f, 44f);
        labelRect.anchoredPosition = Vector2.zero;

        GameObject toggleObject = CreateUiObject("Toggle", row.transform);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 0.5f);
        toggleRect.anchorMax = new Vector2(1f, 0.5f);
        toggleRect.pivot = new Vector2(1f, 0.5f);
        toggleRect.sizeDelta = new Vector2(34f, 34f);
        toggleRect.anchoredPosition = Vector2.zero;
        Toggle toggle = toggleObject.AddComponent<Toggle>();

        Image box = toggleObject.AddComponent<Image>();
        box.color = new Color(0.97f, 0.91f, 0.99f, 0.74f);
        Outline boxOutline = toggleObject.AddComponent<Outline>();
        boxOutline.effectColor = UiAccentSoft;
        boxOutline.effectDistance = new Vector2(1.2f, 1.2f);

        GameObject checkObject = CreateUiObject("Checkmark", toggleObject.transform);
        RectTransform checkRect = checkObject.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(20f, 20f);
        Image check = checkObject.AddComponent<Image>();
        check.color = UiAccent;

        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = value;
        return toggle;
    }

    private Button CreateSettingsButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 size, UnityAction onClick, out Text labelText)
    {
        Button button = CreateButton(parent, label, onClick, out labelText);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        return button;
    }

    private Slider CreateSliderRow(Transform parent, string label, float min, float max, float value, bool wholeNumbers, out Text valueText)
    {
        GameObject row = CreateUiObject(label + " Row", parent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 54f;
        HorizontalLayoutGroup rowGroup = row.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 16f;
        rowGroup.childAlignment = TextAnchor.MiddleLeft;
        rowGroup.childControlHeight = true;
        rowGroup.childControlWidth = false;

        Text labelText = CreateText(row.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.1f, 0.1f, 0.1f, 1f));
        LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 130f;

        Slider slider = CreateSlider(row.transform);
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.value = value;
        LayoutElement sliderLayout = slider.gameObject.AddComponent<LayoutElement>();
        sliderLayout.preferredWidth = 300f;
        sliderLayout.preferredHeight = 38f;

        string displayValue = wholeNumbers ? Mathf.Round(value).ToString("0") : value.ToString("0.0");
        valueText = CreateText(row.transform, displayValue, 20, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.1f, 0.1f, 0.1f, 1f));
        LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 86f;

        return slider;
    }

    private Slider CreateSlider(Transform parent)
    {
        GameObject sliderObject = CreateUiObject("Slider", parent);
        RectTransform sliderRootRect = sliderObject.GetComponent<RectTransform>();
        sliderRootRect.sizeDelta = new Vector2(244f, 32f);
        Slider slider = sliderObject.AddComponent<Slider>();

        GameObject background = CreateUiObject("Background", sliderObject.transform);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.offsetMin = new Vector2(8f, -5f);
        backgroundRect.offsetMax = new Vector2(-8f, 5f);
        Image backgroundImageComponent = background.AddComponent<Image>();
        backgroundImageComponent.color = new Color(0.28f, 0.22f, 0.3f, 0.16f);

        GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-8f, 0f);

        GameObject fill = CreateUiObject("Fill", fillArea.transform);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(1f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(0f, -5f);
        fillRect.offsetMax = new Vector2(0f, 5f);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = UiAccentSoft;

        GameObject handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
        Stretch(handleArea.GetComponent<RectTransform>());

        GameObject handle = CreateUiObject("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(24f, 34f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(1f, 0.86f, 1f, 0.92f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private Toggle CreateToggleRow(Transform parent, string label, bool value)
    {
        GameObject row = CreateUiObject(label + " Row", parent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 48f;
        HorizontalLayoutGroup rowGroup = row.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 16f;
        rowGroup.childAlignment = TextAnchor.MiddleLeft;
        rowGroup.childControlHeight = false;
        rowGroup.childControlWidth = false;

        Text labelText = CreateText(row.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.1f, 0.1f, 0.1f, 1f));
        LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 450f;
        labelLayout.preferredHeight = 44f;

        GameObject toggleObject = CreateUiObject("Toggle", row.transform);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(42f, 42f);
        Toggle toggle = toggleObject.AddComponent<Toggle>();

        Image box = toggleObject.AddComponent<Image>();
        box.color = new Color(0.98f, 0.93f, 0.98f, 0.94f);

        GameObject checkObject = CreateUiObject("Checkmark", toggleObject.transform);
        RectTransform checkRect = checkObject.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(24f, 24f);
        Image check = checkObject.AddComponent<Image>();
        check.color = new Color(0.78f, 0.42f, 0.82f, 0.95f);

        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = value;
        return toggle;
    }

    private void AddButtonLayout(Button button, float height, float width = -1f)
    {
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        if (width > 0f)
        {
            layout.preferredWidth = width;
        }
    }

    private GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private Font GetFont(int size)
    {
        if (uiFont != null)
        {
            return uiFont;
        }

        try
        {
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "Microsoft YaHei", "Yu Gothic UI", "Meiryo", "Noto Sans CJK SC", "SimHei", "Arial" }, size);
        }
        catch
        {
            uiFont = null;
        }

        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return uiFont;
    }

    private Font GetTitleFont(int size)
    {
        if (titleFont != null)
        {
            return titleFont;
        }

        try
        {
            titleFont = Font.CreateDynamicFontFromOSFont(new[] { "STXingkai", "YouYuan", "KaiTi", "Microsoft YaHei UI", "Arial" }, size);
        }
        catch
        {
            titleFont = null;
        }

        return titleFont == null ? GetFont(size) : titleFont;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private static void AddNonEmpty(HashSet<string> set, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            set.Add(value);
        }
    }

    private static string FirstNonEmpty(string first, string second)
    {
        if (!string.IsNullOrEmpty(first))
        {
            return first;
        }

        return second;
    }
}
