using UnityEngine;

public class RendererData
{
    private static class ShaderData
    {
        public static readonly int ColorID = Shader.PropertyToID("_Color");
        public static readonly int OneMinusDepthID = Shader.PropertyToID("_OneMinusDepth");
    }

    public Renderer Renderer => renderer;
    public float CameraDistanceSqr => cameraDistanceSqr;
    public Color Color { get; set; } = Color.white;
    public float OneMinusDepth { get; set; }

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

    public void Setup(Material material, int layer)
    {
        material.SetColor(ShaderData.ColorID, Color);
        material.SetFloat(ShaderData.OneMinusDepthID, OneMinusDepth);

        renderer.gameObject.layer = layer;
        renderer.sharedMaterial = material;
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
