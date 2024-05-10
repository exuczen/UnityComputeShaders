using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class OutlineObject : MonoBehaviour
{
    public Color Color { get => color; set => color = value; }

    [SerializeField]
    private Color color = Color.white;

    private static class ShaderData
    {
        public static readonly int ColorID = Shader.PropertyToID("_Color");
    }

    private class RendererData
    {
        public Renderer Renderer;
        public Material[] Materials;
        public uint LayerMask;
        public int Layer;

        public void Clear()
        {
            Renderer = null;
            Materials = null;
            LayerMask = 0;
            Layer = 0;
        }
    }

    private static readonly ObjectPool<RendererData> rendererDataPool = new(() => new RendererData(), null, data => data.Clear());

    private readonly List<Renderer> renderers = new();
    private readonly List<RendererData> renderersData = new();

    private OutlineObjectCamera objectCamera = null;

    public void Setup(Material material, int layer)
    {
        foreach (var data in renderersData)
        {
            data.Renderer.gameObject.layer = layer;
            data.Renderer.material = material;
            data.Renderer.material.SetColor(ShaderData.ColorID, Color);
        }
    }

    public void DrawBBoxGizmo()
    {
        foreach (var data in renderersData)
        {
            var bounds = data.Renderer.bounds;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    public void Restore()
    {
        foreach (var data in renderersData)
        {
            data.Renderer.gameObject.layer = data.Layer;
            data.Renderer.materials = data.Materials;
        }
    }

    private void OnEnable()
    {
        var camera = MustHave.CameraUtils.MainOrCurrent;
        if (camera)
        {
            var outlineCamera = camera.GetComponent<OutlineCamera>();
            if (outlineCamera)
            {
                objectCamera = outlineCamera.ObjectCamera;
                objectCamera.AddOutlineObject(this);
            }
        }
        GetComponentsInChildren(renderers);
        foreach (var renderer in renderers)
        {
            var data = rendererDataPool.Get();
            data.Renderer = renderer;
            data.Materials = renderer.materials;
            data.LayerMask = renderer.renderingLayerMask;
            data.Layer = renderer.gameObject.layer;
            renderersData.Add(data);
        }
    }

    private void OnDisable()
    {
        if (objectCamera)
        {
            objectCamera.RemoveOutlineObject(this);
        }
        foreach (var data in renderersData)
        {
            rendererDataPool.Release(data);
        }
        renderersData.Clear();
        renderers.Clear();
    }
}
