using UnityEngine;

[ExecuteInEditMode]
public class CabinMoodImageEffect : MonoBehaviour
{
    private const string ShaderResourcePath = "Shaders/CabinMood";

    [Range(0f, 1f)]
    public float intensity = 0.9f;

    private Material moodMaterial;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        EnsureMaterial();
        if (moodMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        moodMaterial.SetFloat("_Intensity", Mathf.Clamp01(intensity));
        Graphics.Blit(source, destination, moodMaterial);
    }

    private void EnsureMaterial()
    {
        if (moodMaterial != null)
        {
            return;
        }

        Shader shader = Resources.Load<Shader>(ShaderResourcePath);
        if (shader == null)
        {
            shader = Shader.Find("Hidden/GalTemplate/CabinMood");
        }

        if (shader == null)
        {
            Debug.LogWarning("CabinMoodImageEffect missing shader: " + ShaderResourcePath);
            return;
        }

        moodMaterial = new Material(shader);
        moodMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    private void OnDestroy()
    {
        if (moodMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(moodMaterial);
        }
        else
        {
            DestroyImmediate(moodMaterial);
        }

        moodMaterial = null;
    }
}

[ExecuteInEditMode]
public class DialogueFocusImageEffect : MonoBehaviour
{
    private const string ShaderResourcePath = "Shaders/DialogueFocus";

    [Range(0f, 1f)]
    public float targetIntensity;

    [Range(0f, 1f)]
    public float currentIntensity;

    [Range(0.5f, 8f)]
    public float blurPixels = 3.5f;

    private Material focusMaterial;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        currentIntensity = Application.isPlaying
            ? Mathf.MoveTowards(currentIntensity, targetIntensity, Time.unscaledDeltaTime * 4f)
            : targetIntensity;

        if (currentIntensity <= 0.001f)
        {
            Graphics.Blit(source, destination);
            return;
        }

        EnsureMaterial();
        if (focusMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        float intensity = Mathf.Clamp01(currentIntensity);
        focusMaterial.SetFloat("_Intensity", intensity);
        focusMaterial.SetFloat("_BlurSize", blurPixels);
        focusMaterial.SetFloat("_Darken", 0.18f);
        focusMaterial.SetColor("_Tint", new Color(0.09f, 0.055f, 0.14f, 0.18f));
        Graphics.Blit(source, destination, focusMaterial);
    }

    private void EnsureMaterial()
    {
        if (focusMaterial != null)
        {
            return;
        }

        Shader shader = Resources.Load<Shader>(ShaderResourcePath);
        if (shader == null)
        {
            shader = Shader.Find("Hidden/GalTemplate/DialogueFocus");
        }

        if (shader == null)
        {
            Debug.LogWarning("DialogueFocusImageEffect missing shader: " + ShaderResourcePath);
            return;
        }

        focusMaterial = new Material(shader);
        focusMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    private void OnDestroy()
    {
        if (focusMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(focusMaterial);
        }
        else
        {
            DestroyImmediate(focusMaterial);
        }

        focusMaterial = null;
    }
}
