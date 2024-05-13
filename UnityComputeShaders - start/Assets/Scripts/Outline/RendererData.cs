using UnityEngine;

public class RendererData
{
    private static class ShaderData
    {
        public static readonly int ColorID = Shader.PropertyToID("_Color");
        public static readonly int OneMinusDepthID = Shader.PropertyToID("_OneMinusDepth");
    }

    public Renderer Renderer => renderer;
    public Material Material => renderer.material;
    public float CameraDistanceSqr => cameraDistanceSqr;

    private Renderer renderer;
    private Material[] materials;
    private uint layerMask;
    private int layer;
    private float cameraDistanceSqr;

    public void Clear()
    {
        renderer = null;
        materials = null;
        layerMask = 0;
        layer = 0;
    }

    public void SetRenderer(Renderer renderer)
    {
        this.renderer = renderer;
        materials = renderer.materials;
        layerMask = renderer.renderingLayerMask;
        layer = renderer.gameObject.layer;
    }

    public void Setup(Material material, int layer)
    {
        renderer.gameObject.layer = layer;
        renderer.material = material;
    }

    public void SetMaterialDepth(float depth)
    {
        renderer.material.SetFloat(ShaderData.OneMinusDepthID, 1f - depth);
    }

    public void SetColor(Color color)
    {
        renderer.material.SetColor(ShaderData.ColorID, color);
    }

    public void GetDistanceFromCamera(Vector3 camPos)
    {
        cameraDistanceSqr = (renderer.transform.position - camPos).sqrMagnitude;
    }

    public void Restore()
    {
        renderer.gameObject.layer = layer;
        renderer.materials = materials;
    }
}
