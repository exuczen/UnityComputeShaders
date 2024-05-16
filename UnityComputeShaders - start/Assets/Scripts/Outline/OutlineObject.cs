﻿using MustHave.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[ExecuteInEditMode]
public class OutlineObject : MonoBehaviour
{
    public Color32 Color32 { get => color; set => color = value; }
    public Color Color { get => color; set => color = value; }

    [SerializeField]
    private Color color = Color.white;

    private static readonly ObjectPool<RendererData> rendererDataPool = new(() => new RendererData(), null, data => data.Clear());

    private readonly List<Renderer> renderers = new();
    private readonly List<RendererData> renderersData = new();

    private OutlineObjectCamera objectCamera = null;

    public void SetRenderersColor()
    {
        foreach (var data in renderersData)
        {
            data.Color = color;
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

    public void ForEachRendererData(Action<RendererData> action)
    {
        foreach (var data in renderersData)
        {
            action(data);
        }
    }

    private void OnEnable()
    {
        GetComponentsInChildren(renderers);
        foreach (var renderer in renderers)
        {
            var data = rendererDataPool.Get();
            data.SetRenderer(renderer);
            renderersData.Add(data);
        }
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
