using UnityEngine;

public class RendererData
{
    public Renderer Renderer => renderer;

    private Renderer renderer;
    private Material[] materials;
    private uint layerMask;
    private int layer;

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

    public void Restore()
    {
        renderer.gameObject.layer = layer;
        renderer.materials = materials;
    }
}
