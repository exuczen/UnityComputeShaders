﻿using System.Collections.Generic;
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

    private static readonly ObjectPool<RendererData> rendererDataPool = new(() => new RendererData(), null, data => data.Clear());

    private readonly List<Renderer> renderers = new();
    private readonly List<RendererData> renderersData = new();

    private OutlineObjectCamera objectCamera = null;

    public void Setup(Material material, int layer)
    {
        foreach (var data in renderersData)
        {
            var renderer = data.Renderer;
            data.Setup(material, layer);
            renderer.material.SetColor(ShaderData.ColorID, Color);

            //float distance = Vector3.Distance(objectCamera.transform.position, renderer.transform.position);
            //float oneMinusDistance = Mathf.Clamp01(1f - distance / 100f);
            //renderer.material.SetFloat("_OneMinusMeshCenterDistance", oneMinusDistance);
            //Debug.Log($"{GetType().Name}.{renderer.name}: {distance}");
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
            data.Restore();
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
            data.SetRenderer(renderer);
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
