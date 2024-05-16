using MustHave.Utils;
using UnityEngine;

public class RendererData
{
    private static class ShaderData
    {
        public static readonly int ColorID = Shader.PropertyToID("_Color");
        public static readonly int OneMinusDepthID = Shader.PropertyToID("_OneMinusDepth");
        public static readonly int DepthID = Shader.PropertyToID("_Depth");
        public static readonly int MinDepthID = Shader.PropertyToID("_MinDepth");
    }

    public Renderer Renderer => renderer;
    public float CameraDistanceSqr => cameraDistanceSqr;
    public Color Color { get; set; } = Color.white;
    public float Depth { get; set; }

    private Renderer renderer;
    private Material sharedMaterial;
    private uint layerMask;
    private int layer;
    private float cameraDistanceSqr;

    public void Clear()
    {
        renderer = null;
        sharedMaterial = null;
        layerMask = 0;
        layer = 0;
    }

    public void SetRenderer(Renderer renderer)
    {
        this.renderer = renderer;
        sharedMaterial = renderer.sharedMaterial;
        layerMask = renderer.renderingLayerMask;
        layer = renderer.gameObject.layer;
    }

    public void Setup(Material material, int layer, float minDepth)
    {
        var color = GetColorWithAlphaDepth(minDepth);

        material.SetColor(ShaderData.ColorID, color);
        material.SetFloat(ShaderData.DepthID, Depth);
        material.SetFloat(ShaderData.MinDepthID, minDepth);

        renderer.gameObject.layer = layer;
        renderer.sharedMaterial = material;
    }

    public Color GetColorWithAlphaDepth(float minDepth)
    {
        var color = Color;
        color.a = Mathf.Clamp01(1f - Depth + minDepth);
        return color;
    }

    public float GetOneMinusDepth(float minDepth)
    {
        return Mathf.Clamp01(1f - Depth + minDepth);
    }

    public void GetDistanceFromCamera(Vector3 camPos)
    {
        cameraDistanceSqr = (renderer.transform.position - camPos).sqrMagnitude;
    }

    public void Restore()
    {
        renderer.gameObject.layer = layer;
        renderer.sharedMaterial = sharedMaterial;
    }
}
