using MustHave.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OutlineObjectCamera : MonoBehaviour
{
    private readonly struct Layer
    {
        public static readonly int OutlineLayer = LayerMask.NameToLayer("Outline");
        public static readonly int OutlineMask = LayerMask.GetMask("Outline");
    }

    public RenderTexture ShapeTexture => shapeTexture;

    [SerializeField]
    private Material outlineMeshMaterial = null;

    private new Camera camera = null;

    private RenderTexture shapeTexture = null;

    private readonly List<OutlineObject> objects = new();

    public void CreateTextures(Vector2Int size)
    {
        shapeTexture = CreateTexture(size, "OutlineObjectsTexture");
    }

    public void ReleaseTextures()
    {
        ReleaseTexture(ref shapeTexture);
    }

    public void Setup(Camera parentCamera)
    {
        camera = GetComponent<Camera>();

        if (parentCamera)
        {
            camera.CopyFrom(parentCamera);
        }
        camera.targetTexture = shapeTexture;
        camera.backgroundColor = Color.clear;
        camera.cullingMask = Layer.OutlineMask;
        camera.allowMSAA = false;
        camera.enabled = false;
        camera.depthTextureMode = DepthTextureMode.Depth;
    }

    public void AddOutlineObject(OutlineObject obj)
    {
        if (!objects.Contains(obj))
        {
            objects.Add(obj);
        }
    }

    public void RemoveOutlineObject(OutlineObject obj)
    {
        objects.Remove(obj);
    }

    private RenderTexture CreateTexture(Vector2Int size, string name = "")
    {
        var texture = new RenderTexture(size.x, size.y, 0)
        {
            name = name,
            enableRandomWrite = true
        };
        texture.Create();
        return texture;
    }

    private void ReleaseTexture(ref RenderTexture texture)
    {
        if (texture)
        {
            texture.Release();
            texture = null;
        }
    }

    private void OnDrawGizmos()
    {
        //foreach (OutlineObject obj in objects)
        //{
        //    obj.DrawBBoxGizmo();
        //}
    }

    private void OnRenderObject()
    {
        foreach (OutlineObject obj in objects)
        {
            obj.Setup(outlineMeshMaterial, Layer.OutlineLayer);
        }
        camera.Render();

        foreach (OutlineObject obj in objects)
        {
            obj.Restore();
        }
    }

    private void OnDestroy()
    {
        ReleaseTextures();
    }
}
