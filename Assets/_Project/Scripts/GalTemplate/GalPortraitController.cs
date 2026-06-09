using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class GalPortraitEntry
{
    public string id;
    public string displayName;
    public string folder;
    public string defaultExpression = "neutral";
    public float width = 520f;
    public float height = 900f;
    public float scale = 1f;
}

public class GalPortraitPose
{
    public string slot;
    public string character;
    public string expression;
    public string facing;
    public string animation;
    public string path;
    public bool visible = true;
}

public class GalPortraitController : MonoBehaviour
{
    private const string DefaultCharacter = "test";
    private const string DefaultExpression = "neutral";

    private class PortraitSlotState
    {
        public string slot;
        public RectTransform root;
        public RectTransform imageRect;
        public RawImage image;
        public AspectRatioFitter imageAspect;
        public Image placeholder;
        public Text placeholderText;
        public CanvasGroup group;
        public Vector2 basePosition;
        public Vector3 baseScale = Vector3.one;
        public Coroutine animationRoutine;
        public string character;
        public string expression;
    }

    private readonly Dictionary<string, PortraitSlotState> slots = new Dictionary<string, PortraitSlotState>();
    private readonly Dictionary<string, GalPortraitEntry> library = new Dictionary<string, GalPortraitEntry>();
    private readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    private Font labelFont;

    public void Initialize(Font font)
    {
        labelFont = font;
        RectTransform rect = GetComponent<RectTransform>();
        Stretch(rect);
        CreateSlot("left", new Vector2(0.23f, 0f));
        CreateSlot("center", new Vector2(0.5f, 0f));
        CreateSlot("right", new Vector2(0.77f, 0f));
        HideAll();
    }

    public void Configure(IEnumerable<GalPortraitEntry> entries)
    {
        library.Clear();
        if (entries == null)
        {
            return;
        }

        foreach (GalPortraitEntry entry in entries)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.id))
            {
                library[entry.id] = entry;
            }
        }
    }

    public void Show(GalPortraitPose pose)
    {
        if (pose == null)
        {
            return;
        }

        string slotId = NormalizeSlot(pose.slot);
        if (!slots.TryGetValue(slotId, out PortraitSlotState slot))
        {
            return;
        }

        if (!pose.visible)
        {
            Hide(slotId);
            return;
        }

        string character = string.IsNullOrEmpty(pose.character) ? FirstNonEmpty(slot.character, DefaultCharacter) : pose.character;
        string expression = string.IsNullOrEmpty(pose.expression) ? GetDefaultExpression(character) : pose.expression;
        slot.character = character;
        slot.expression = expression;

        GalPortraitEntry entry = GetEntry(character);
        float width = entry != null && entry.width > 0f ? entry.width : 520f;
        float height = entry != null && entry.height > 0f ? entry.height : 900f;
        float scale = entry != null && entry.scale > 0f ? entry.scale : 1f;
        slot.root.sizeDelta = new Vector2(width, height);
        slot.baseScale = Vector3.one * scale;
        slot.root.localScale = slot.baseScale;

        Texture2D texture = LoadPortraitTexture(pose, character, expression);
        if (texture != null)
        {
            slot.image.texture = texture;
            slot.image.enabled = true;
            slot.imageAspect.aspectRatio = texture.width / (float)texture.height;
            slot.placeholder.enabled = false;
            slot.placeholderText.enabled = false;
        }
        else
        {
            slot.image.texture = null;
            slot.image.enabled = false;
            slot.placeholder.enabled = true;
            slot.placeholderText.enabled = true;
            slot.placeholderText.text = character + "\n" + expression;
        }

        float facing = GetFacingSign(slotId, pose.facing);
        slot.imageRect.localScale = new Vector3(facing, 1f, 1f);
        slot.root.anchoredPosition = slot.basePosition;
        slot.group.alpha = 1f;
        slot.group.blocksRaycasts = false;

        if (!string.IsNullOrEmpty(pose.animation) && !IsNone(pose.animation))
        {
            PlayAnimation(slotId, pose.animation);
        }
    }

    public void Hide(string slot)
    {
        string slotId = NormalizeSlot(slot);
        if (!slots.TryGetValue(slotId, out PortraitSlotState state))
        {
            return;
        }

        StopSlotAnimation(state);
        state.group.alpha = 0f;
        state.image.texture = null;
        state.image.enabled = false;
        state.placeholder.enabled = false;
        state.placeholderText.enabled = false;
        state.character = null;
        state.expression = null;
        state.root.anchoredPosition = state.basePosition;
        state.root.localScale = state.baseScale;
    }

    public void HideAll()
    {
        Hide("left");
        Hide("center");
        Hide("right");
    }

    public void PlayAnimation(string slot, string animation)
    {
        string slotId = NormalizeSlot(slot);
        if (!slots.TryGetValue(slotId, out PortraitSlotState state) || string.IsNullOrEmpty(animation) || IsNone(animation))
        {
            return;
        }

        StopSlotAnimation(state);
        string name = animation.Trim().ToLowerInvariant();
        state.animationRoutine = StartCoroutine(AnimateSlot(state, name));
    }

    private void CreateSlot(string slotId, Vector2 anchor)
    {
        GameObject slotObject = new GameObject("Portrait Slot " + slotId, typeof(RectTransform), typeof(CanvasGroup));
        slotObject.transform.SetParent(transform, false);

        RectTransform rect = slotObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(520f, 900f);
        rect.anchoredPosition = new Vector2(0f, 90f);

        CanvasGroup group = slotObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        Image placeholder = slotObject.AddComponent<Image>();
        placeholder.color = SlotColor(slotId);
        placeholder.enabled = false;
        placeholder.raycastTarget = false;

        GameObject imageObject = new GameObject("Portrait Image", typeof(RectTransform));
        imageObject.transform.SetParent(slotObject.transform, false);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        Stretch(imageRect);
        RawImage image = imageObject.AddComponent<RawImage>();
        image.enabled = false;
        image.raycastTarget = false;
        AspectRatioFitter imageAspect = imageObject.AddComponent<AspectRatioFitter>();
        imageAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        imageAspect.aspectRatio = 0.58f;

        GameObject labelObject = new GameObject("Portrait Placeholder Label", typeof(RectTransform));
        labelObject.transform.SetParent(slotObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        Stretch(labelRect);
        labelRect.offsetMin = new Vector2(24f, 24f);
        labelRect.offsetMax = new Vector2(-24f, -24f);
        Text label = labelObject.AddComponent<Text>();
        label.font = labelFont;
        label.fontSize = 28;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.color = new Color(0.08f, 0.08f, 0.08f, 0.82f);
        label.raycastTarget = false;
        label.enabled = false;

        slots[slotId] = new PortraitSlotState
        {
            slot = slotId,
            root = rect,
            imageRect = imageRect,
            image = image,
            imageAspect = imageAspect,
            placeholder = placeholder,
            placeholderText = label,
            group = group,
            basePosition = rect.anchoredPosition,
            baseScale = Vector3.one
        };
    }

    private IEnumerator AnimateSlot(PortraitSlotState slot, string animation)
    {
        Vector2 origin = slot.basePosition;
        Vector3 scale = slot.baseScale;
        float duration = animation == "shake" || animation == "抖动" ? 0.34f : 0.42f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            if (animation == "shake" || animation == "抖动")
            {
                float strength = Mathf.Lerp(12f, 0f, t);
                slot.root.anchoredPosition = origin + new Vector2(Mathf.Sin(t * 60f) * strength, Mathf.Cos(t * 42f) * strength * 0.35f);
            }
            else if (animation == "bounce" || animation == "跳动")
            {
                slot.root.anchoredPosition = origin + new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 28f);
            }
            else if (animation == "pop" || animation == "弹出")
            {
                float pulse = 0.92f + Mathf.Sin(t * Mathf.PI) * 0.13f;
                slot.root.localScale = scale * pulse;
            }
            else if (animation == "fade" || animation == "淡入")
            {
                slot.group.alpha = Mathf.Clamp01(t);
            }

            yield return null;
        }

        slot.root.anchoredPosition = origin;
        slot.root.localScale = scale;
        slot.group.alpha = 1f;
        slot.animationRoutine = null;
    }

    private Texture2D LoadPortraitTexture(GalPortraitPose pose, string character, string expression)
    {
        string explicitPath = pose != null ? pose.path : null;
        List<string> candidates = new List<string>();
        if (!string.IsNullOrEmpty(explicitPath))
        {
            candidates.Add(ResolveGalPath(explicitPath));
        }

        GalPortraitEntry entry = GetEntry(character);
        string folder = entry != null && !string.IsNullOrEmpty(entry.folder) ? entry.folder : "Portraits/" + character;
        AddPortraitCandidates(candidates, folder, expression);
        AddPortraitCandidates(candidates, "Portraits", character + "_" + expression);
        AddPortraitCandidates(candidates, "Portraits", character);

        foreach (string candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate) || !File.Exists(candidate))
            {
                continue;
            }

            if (textureCache.TryGetValue(candidate, out Texture2D cached))
            {
                return cached;
            }

            byte[] bytes = File.ReadAllBytes(candidate);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Destroy(texture);
                continue;
            }

            texture.name = Path.GetFileNameWithoutExtension(candidate);
            textureCache[candidate] = texture;
            return texture;
        }

        return null;
    }

    private void AddPortraitCandidates(List<string> candidates, string folder, string filename)
    {
        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(filename))
        {
            return;
        }

        string[] extensions = { ".png", ".jpg", ".jpeg" };
        foreach (string extension in extensions)
        {
            candidates.Add(ResolveGalPath(Path.Combine(folder, filename + extension)));
        }
    }

    private GalPortraitEntry GetEntry(string character)
    {
        return !string.IsNullOrEmpty(character) && library.TryGetValue(character, out GalPortraitEntry entry) ? entry : null;
    }

    private string GetDefaultExpression(string character)
    {
        GalPortraitEntry entry = GetEntry(character);
        if (entry != null && !string.IsNullOrEmpty(entry.defaultExpression))
        {
            return entry.defaultExpression;
        }

        return DefaultExpression;
    }

    private static string ResolveGalPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        return Path.IsPathRooted(path) ? path : Path.Combine(Application.streamingAssetsPath, "GAL", path);
    }

    private static float GetFacingSign(string slot, string facing)
    {
        if (string.IsNullOrEmpty(facing) || facing == "auto" || facing == "towards_center")
        {
            return slot == "right" ? -1f : 1f;
        }

        string normalized = facing.Trim().ToLowerInvariant();
        if (normalized == "left" || normalized == "flip" || normalized == "mirror" || normalized == "左")
        {
            return -1f;
        }

        return 1f;
    }

    public static string NormalizeSlot(string slot)
    {
        if (string.IsNullOrEmpty(slot))
        {
            return "center";
        }

        string value = slot.Trim().ToLowerInvariant();
        if (value == "l" || value == "left" || value == "左" || value == "左侧")
        {
            return "left";
        }

        if (value == "r" || value == "right" || value == "右" || value == "右侧")
        {
            return "right";
        }

        return "center";
    }

    private void StopSlotAnimation(PortraitSlotState slot)
    {
        if (slot.animationRoutine != null)
        {
            StopCoroutine(slot.animationRoutine);
            slot.animationRoutine = null;
        }
    }

    private static bool IsNone(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        string normalized = value.Trim().ToLowerInvariant();
        return normalized == "none" || normalized == "off" || normalized == "no" || normalized == "无";
    }

    private static Color SlotColor(string slot)
    {
        if (slot == "left")
        {
            return new Color(0.92f, 0.96f, 1f, 0.62f);
        }

        if (slot == "right")
        {
            return new Color(1f, 0.93f, 0.9f, 0.62f);
        }

        return new Color(0.92f, 1f, 0.91f, 0.62f);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static string FirstNonEmpty(string first, string second)
    {
        return string.IsNullOrEmpty(first) ? second : first;
    }
}
