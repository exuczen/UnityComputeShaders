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
    public Material OutlineMeshMaterial => outlineMeshMaterial;
    public Camera Camera => camera;
    public int LineWidth => lineWidth;

    [SerializeField]
    private Material outlineMeshMaterial = null;
    [SerializeField, Range(1, 100)]
    private int lineWidth = 5;

    private new Camera camera = null;

    private RenderTexture shapeTexture = null;

    private readonly List<OutlineObject> objects = new();
    private readonly List<RendererData> renderersData = new();

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
            obj.ForEachRendererData(data => {
                renderersData.Add(data);
            });
        }
    }

    public void RemoveOutlineObject(OutlineObject obj)
    {
        objects.Remove(obj);
        obj.ForEachRendererData(data => {
            renderersData.Remove(data);
        });
    }

    private RenderTexture CreateTexture(Vector2Int size, string name = "")
    {
        var texture = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32)
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

    private void SortRenderers()
    {
        foreach (var data in renderersData)
        {
            data.GetDistanceFromCamera(camera.transform.position);
        }
        renderersData.Sort((a, b) => a.CameraDistanceSqr.CompareTo(b.CameraDistanceSqr));
    }

    private void RenderShapes()
    {
        foreach (OutlineObject obj in objects)
        {
            obj.Setup(outlineMeshMaterial, Layer.OutlineLayer);
        }
        int count = renderersData.Count;
        for (int i = 0; i < count; i++)
        {
            //Debug.Log($"{GetType().Name}.{i} | {renderersData[i].CameraDistanceSqr} | {1f - (float)i / count}");
            renderersData[i].SetMaterialDepth((float)i / count);
        }
        camera.Render();

        foreach (OutlineObject obj in objects)
        {
            obj.Restore();
        }
    }


    private void Update()
    {
        foreach (var obj in objects)
        {
            obj.SetRenderersColor();
        }
        SortRenderers();
    }

    private void OnRenderObject()
    {
        RenderShapes();
    }

    private void OnDrawGizmos()
    {
        //foreach (OutlineObject obj in objects)
        //{
        //    obj.DrawBBoxGizmo();
        //}
    }

    private void OnDestroy()
    {
        ReleaseTextures();
    }
}
