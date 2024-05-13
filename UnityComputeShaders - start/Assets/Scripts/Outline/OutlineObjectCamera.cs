using MustHave.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OutlineObjectCamera : MonoBehaviour
{
    private const int CicleInstanceCapacity = 1 << 10;

    private readonly struct Layer
    {
        public static readonly int OutlineLayer = LayerMask.NameToLayer("Outline");
        public static readonly int OutlineMask = LayerMask.GetMask("Outline");
    }

    public RenderTexture ShapeTexture => shapeTexture;
    public RenderTexture CircleTexture => circleTexture;
    public Material OutlineShapeMaterial => outlineShapeMaterial;
    public Camera Camera => camera;
    public int LineWidth => lineWidth;

    [SerializeField]
    private Material outlineShapeMaterial = null;
    [SerializeField]
    private Material circleSpriteMaterial = null;
    [SerializeField]
    private MeshFilter quadMeshFilter = null;
    [SerializeField]
    private Camera circlesCamera = null;
    [SerializeField, Range(1, 100)]
    private int lineWidth = 5;

    //TODO: rename to shapeCamera
    private new Camera camera = null;

    private RenderTexture shapeTexture = null;
    private RenderTexture circleTexture = null;

    private readonly List<OutlineObject> objects = new();
    private readonly List<RendererData> renderersData = new();

    private readonly InstanceData[] circleInstanceData = new InstanceData[CicleInstanceCapacity];

    private GraphicsBuffer circleInstanceBuffer = null;
    private MaterialPropertyBlock circlePropertyBlock = null;
    private RenderParams circleRenderParams = default;

    private struct InstanceData
    {
        public Matrix4x4 objectToWorld;
        public Vector3 clipPosition;
        public Vector4 color;
        public float scale;
    }

    private void Start()
    {
        InitCircleInstancing();
    }

    public void CreateTextures(Vector2Int size)
    {
        shapeTexture = CreateTexture(size, "OutlineObjectsShapeTexture");
        circleTexture = CreateTexture(size, "OutlineObjectsCircleTexture");
        circleTexture.filterMode = FilterMode.Point;
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

        if (parentCamera)
        {
            circlesCamera.CopyFrom(camera);
        }
        circlesCamera.targetTexture = circleTexture;
        circlesCamera.orthographic = true;
        circlesCamera.cullingMask = Layer.OutlineMask;
        circlesCamera.enabled = Application.isPlaying;
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

    private void InitCircleInstancing()
    {
        circleInstanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, circleInstanceData.Length, Marshal.SizeOf<InstanceData>());
        circlePropertyBlock = new MaterialPropertyBlock();
        circlePropertyBlock.SetBuffer("_InstancesData", circleInstanceBuffer);
        circleRenderParams = new RenderParams(circleSpriteMaterial)
        {
            camera = circlesCamera,
            matProps = circlePropertyBlock,
            layer = Layer.OutlineLayer,
            //renderingLayerMask = (uint)Layer.OutlineMask,
            worldBounds = new Bounds(Vector3.zero, Vector3.one)
        };
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
            obj.Setup(outlineShapeMaterial, Layer.OutlineLayer);
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

    private void RenderCircles()
    {
        var circlesCamTransform = circlesCamera.transform;

        float scale = 2f * lineWidth / circlesCamera.pixelHeight;

        int count = renderersData.Count;
        for (int i = 0; i < count; i++)
        {
            var renderer = renderersData[i].Renderer;
            var center = renderer.bounds.center;
            var viewPoint = camera.WorldToViewportPoint(center);
            //var worldPoint = circlesCamera.ViewportToWorldPoint(viewPoint);
            //objectToWorld.SetTRS(worldPoint, Quaternion.LookRotation(circlesCamTransform.forward, circlesCamTransform.up), Vector3.one);
            var clipPoint = new Vector3()
            {
                x = (viewPoint.x - 0.5f) * 2f,
                y = -(viewPoint.y - 0.5f) * 2f,
                z = 1f - (float)i / count
            };
            var color = renderersData[i].Color;
            color.a = clipPoint.z;
            //Debug.Log($"{GetType().Name}.{i} | {renderersData[i].CameraDistanceSqr} | {clipPoint.z}");
            circleInstanceData[i] = new InstanceData()
            {
                objectToWorld = Matrix4x4.identity,
                clipPosition = clipPoint,
                color = color,
                scale = scale
            };
        }
        circleInstanceBuffer.SetData(circleInstanceData, 0, 0, renderersData.Count);
        circleRenderParams.worldBounds = new Bounds(circlesCamTransform.position, Vector3.one);

        Graphics.RenderMeshPrimitives(circleRenderParams, quadMeshFilter.sharedMesh, 0, renderersData.Count);
        //Graphics.RenderMeshInstanced(circleRenderParams, quadMeshFilter.sharedMesh, 0, circleInstanceData, renderersData.Count);
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
        RenderCircles();
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

        circleInstanceBuffer?.Release();
    }
}
