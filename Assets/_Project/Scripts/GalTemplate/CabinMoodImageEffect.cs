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
