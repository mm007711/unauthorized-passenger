using UnityEngine;

[ExecuteInEditMode]
public class CabinMoodImageEffect : MonoBehaviour
{
    [Range(0f, 1f)]
    public float intensity = 0.9f;

    private Material overlayMaterial;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        EnsureMaterial();
        Graphics.Blit(source, destination);

        if (overlayMaterial == null)
        {
            return;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = destination;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, destination.width, destination.height, 0);
        overlayMaterial.SetPass(0);

        float width = destination.width;
        float height = destination.height;
        float strength = Mathf.Clamp01(intensity);

        DrawMoodWash(width, height, strength);
        DrawVignette(width, height, strength);

        GL.PopMatrix();
        RenderTexture.active = previous;
    }

    private void EnsureMaterial()
    {
        if (overlayMaterial != null)
        {
            return;
        }

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            return;
        }

        overlayMaterial = new Material(shader);
        overlayMaterial.hideFlags = HideFlags.HideAndDontSave;
        overlayMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        overlayMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        overlayMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        overlayMaterial.SetInt("_ZWrite", 0);
    }

    private void DrawMoodWash(float width, float height, float strength)
    {
        const int columns = 28;
        const int rows = 16;
        float cellWidth = width / columns;
        float cellHeight = height / rows;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                float left = x / (float)columns;
                float right = (x + 1) / (float)columns;
                float top = y / (float)rows;
                float bottom = (y + 1) / (float)rows;

                Rect rect = new Rect(x * cellWidth, y * cellHeight, cellWidth + 1f, cellHeight + 1f);
                DrawGradientQuad(
                    rect,
                    EvaluateMoodColor(left, top, strength),
                    EvaluateMoodColor(right, top, strength),
                    EvaluateMoodColor(right, bottom, strength),
                    EvaluateMoodColor(left, bottom, strength));
            }
        }
    }

    private Color EvaluateMoodColor(float u, float v, float strength)
    {
        float aisle = Gaussian(u, 0.5f, 0.32f) * Gaussian(v, 0.46f, 0.58f);
        float frontGlow = Gaussian(u, 0.52f, 0.2f) * Gaussian(v, 0.22f, 0.22f);
        float windowCool = Mathf.SmoothStep(0.5f, 1f, u) * (1f - Mathf.SmoothStep(0.66f, 1f, v));

        Color pink = new Color(0.96f, 0.26f, 0.88f, 1f);
        Color violet = new Color(0.46f, 0.32f, 1f, 1f);
        Color cool = new Color(0.14f, 0.32f, 0.78f, 1f);
        Color tint = Color.Lerp(pink, violet, Mathf.Clamp01(frontGlow * 0.8f + windowCool * 0.35f));
        tint = Color.Lerp(tint, cool, windowCool * 0.35f);

        float alpha = (0.13f + aisle * 0.3f + frontGlow * 0.24f + windowCool * 0.14f) * strength;
        tint.a = Mathf.Clamp01(alpha);
        return tint;
    }

    private void DrawVignette(float width, float height, float strength)
    {
        Color dark = new Color(0.01f, 0.01f, 0.04f, 0.36f * strength);
        Color clear = new Color(0.01f, 0.01f, 0.04f, 0f);

        DrawGradientQuad(new Rect(0f, 0f, width * 0.34f, height), dark, clear, clear, dark);
        DrawGradientQuad(new Rect(width * 0.66f, 0f, width * 0.34f, height), clear, dark, dark, clear);
        DrawGradientQuad(new Rect(0f, 0f, width, height * 0.18f), dark, dark, clear, clear);
        DrawGradientQuad(new Rect(0f, height * 0.8f, width, height * 0.2f), clear, clear, dark, dark);
    }

    private float Gaussian(float value, float center, float width)
    {
        float normalized = (value - center) / Mathf.Max(0.001f, width);
        return Mathf.Exp(-(normalized * normalized));
    }

    private void DrawGradientQuad(Rect rect, Color topLeft, Color topRight, Color bottomRight, Color bottomLeft)
    {
        GL.Begin(GL.QUADS);
        GL.Color(topLeft);
        GL.Vertex3(rect.xMin, rect.yMin, 0f);
        GL.Color(topRight);
        GL.Vertex3(rect.xMax, rect.yMin, 0f);
        GL.Color(bottomRight);
        GL.Vertex3(rect.xMax, rect.yMax, 0f);
        GL.Color(bottomLeft);
        GL.Vertex3(rect.xMin, rect.yMax, 0f);
        GL.End();
    }

    private void OnDestroy()
    {
        if (overlayMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(overlayMaterial);
            }
            else
            {
                DestroyImmediate(overlayMaterial);
            }

            overlayMaterial = null;
        }
    }
}
