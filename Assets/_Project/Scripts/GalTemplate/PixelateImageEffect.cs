using UnityEngine;

[ExecuteInEditMode]
public class PixelateImageEffect : MonoBehaviour
{
    [Range(1f, 24f)]
    public float pixelSize = 7f;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        int width = Mathf.Max(1, Mathf.RoundToInt(source.width / Mathf.Max(1f, pixelSize)));
        int height = Mathf.Max(1, Mathf.RoundToInt(source.height / Mathf.Max(1f, pixelSize)));

        RenderTexture lowResolution = RenderTexture.GetTemporary(width, height, 0, source.format);
        lowResolution.filterMode = FilterMode.Point;
        source.filterMode = FilterMode.Point;

        Graphics.Blit(source, lowResolution);
        Graphics.Blit(lowResolution, destination);
        RenderTexture.ReleaseTemporary(lowResolution);
    }
}
