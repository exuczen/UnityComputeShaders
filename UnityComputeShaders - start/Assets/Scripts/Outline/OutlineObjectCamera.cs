using MustHave;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OutlineObjectCamera : MonoBehaviour
{
    private const int RenderersCapacity = 1 << 10;

    private readonly struct Layer
    {
        public const string OutlineLayerName = "Outline-MustHave";

        public static int OutlineLayer = LayerMask.NameToLayer(OutlineLayerName);
        public static int OutlineMask = LayerMask.GetMask(OutlineLayerName);

        public static void Refresh()
        {
            OutlineLayer = LayerMask.NameToLayer(OutlineLayerName);
            OutlineMask = LayerMask.GetMask(OutlineLayerName);
        }
    }

    public RenderTexture ShapeTexture => shapeTexture;
    public RenderTexture CircleTexture => circleTexture;
    public Material OutlineShapeMaterial => outlineShapeMaterial;
    public Camera ShapeCamera => shapeCamera;
    public bool CircleInstancingInitiated => circleInstanceBuffer != null;

    private int RenderersCount => Mathf.Min(RenderersCapacity, renderersData.Count);

    [SerializeField]
    private Material outlineShapeMaterial = null;
    [SerializeField]
    private Material circleSpriteMaterial = null;
    [SerializeField]
    private MeshFilter quadMeshFilter = null;
    [SerializeField]
    private Camera circleCamera = null;
    [SerializeField, HideInInspector]
    private bool layerAdded = false;

    private Camera shapeCamera = null;

    private RenderTexture shapeTexture = null;
    private RenderTexture circleTexture = null;

    private readonly List<OutlineObject> objects = new();
    private readonly List<RendererData> renderersData = new();

    private readonly InstanceData[] circleInstanceData = new InstanceData[RenderersCapacity];
    private readonly Material[] shapeMaterials = new Material[RenderersCapacity];

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

    public void CreateRuntimeAssets(Vector2Int texSize)
    {
        CreateTextures(texSize);

        CreateMissingShapeMaterials(10);

        InitCircleInstancing();
    }

    public void DestroyRuntimeAssets()
    {
        ReleaseTexture(ref shapeTexture);
        ReleaseTexture(ref circleTexture);

        circleInstanceBuffer?.Release();
        circleInstanceBuffer = null;
    }

    public void Setup(OutlineCamera outlineCamera)
    {
#if UNITY_EDITOR
        if (!layerAdded)
        {
            layerAdded = EditorUtils.AddLayer(Layer.OutlineLayerName, out bool layerExists) || layerExists;

            if (layerAdded)
            {
                Layer.Refresh();
            }
        }
#endif
        shapeCamera = GetComponent<Camera>();

        if (outlineCamera)
        {
            var parentCamera = outlineCamera.Camera;
            shapeCamera.CopyFrom(parentCamera);
            shapeCamera.fieldOfView = GetExtendedFieldOfView(parentCamera, OutlineCamera.LineMaxThickness);
            //Debug.Log($"{GetType().Name}.Setup: srcFov: {parentCamera.fieldOfView:f2} dstFov: {shapeCamera.fieldOfView:f2}");
        }
        shapeCamera.targetTexture = shapeTexture;
        shapeCamera.backgroundColor = Color.clear;
        shapeCamera.cullingMask = Layer.OutlineMask;
        shapeCamera.allowMSAA = false;
        shapeCamera.enabled = false;
        shapeCamera.depthTextureMode = DepthTextureMode.Depth;

        if (outlineCamera)
        {
            circleCamera.CopyFrom(shapeCamera);
        }
        circleCamera.targetTexture = circleTexture;
        circleCamera.orthographic = true;
        circleCamera.cullingMask = Layer.OutlineMask;
        circleCamera.enabled = true;
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

    private float GetExtendedFieldOfView(Camera parentCamera, int pixelOffset)
    {
        // tanHalfFov = 0.5 * h / r
        // r = 0.5 * h / tanHalfFov
        // r = 0.5 * (h + dh) / tanHalfFov2
        // h / tanHalfFov = (h + dh) / tanHalfFov2
        // h / (h + dh) = tanHalfFov / tanHalfFov2
        // tanHalfFov2 = tanHalfFov * (h + dh) / h

        float aspect = parentCamera.aspect;
        float w = parentCamera.pixelWidth;
        float h = parentCamera.pixelHeight;
        float destFovY;

        if (w > h)
        {
            float dh = pixelOffset << 1;
            float tanHalfFovY = parentCamera.GetTanHalfFovVerti();
            float destHalfFovY = Mathf.Atan2(tanHalfFovY * (h + dh), h);

            destFovY = destHalfFovY * 2f * Mathf.Rad2Deg;
        }
        else
        {
            float dw = pixelOffset << 1;
            float tanHalfFovX = parentCamera.GetTanHalfFovHori();
            float destHalfFovX = Mathf.Atan2(tanHalfFovX * (w + dw), w);

            float destFovX = destHalfFovX * 2f * Mathf.Rad2Deg;
            destFovY = Camera.HorizontalToVerticalFieldOfView(destFovX, aspect);
        }
        return destFovY;
    }


    private void CreateTextures(Vector2Int texSize)
    {
        shapeTexture = CreateTexture(texSize, "OutlineObjectsShapeTexture");
        circleTexture = CreateTexture(texSize, "OutlineObjectsCircleTexture");
        circleTexture.filterMode = FilterMode.Point;
    }

    private void CreateMissingShapeMaterials(int excess)
    {
        int matIndex = Mathf.Clamp(renderersData.Count - 1, 0, shapeMaterials.Length - 1);
        if (!shapeMaterials[matIndex])
        {
            matIndex = Mathf.Clamp(renderersData.Count + excess - 1, 0, shapeMaterials.Length - 1);
            while (matIndex >= 0 && !shapeMaterials[matIndex])
            {
                //Debug.Log($"{GetType().Name}.CreateMissingShapeMaterials: {matIndex}");
                shapeMaterials[matIndex--] = new Material(outlineShapeMaterial);
            }
        }
    }

    private void InitCircleInstancing()
    {
        circleInstanceBuffer?.Release();
        circleInstanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, circleInstanceData.Length, Marshal.SizeOf<InstanceData>());
        circlePropertyBlock = new MaterialPropertyBlock();
        circlePropertyBlock.SetBuffer("_InstancesData", circleInstanceBuffer);
        circleRenderParams = new RenderParams(circleSpriteMaterial)
        {
            camera = circleCamera,
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
            data.GetDistanceFromCamera(shapeCamera.transform.position);
        }
        renderersData.Sort((a, b) => a.CameraDistanceSqr.CompareTo(b.CameraDistanceSqr));
    }

    private void SetSortedRenderersDepth()
    {
        int count = renderersData.Count;
        if (count <= 0)
        {
            return;
        }
        for (int i = 1; i <= count; i++)
        {
            renderersData[i - 1].Depth = (float)i / count;
        }
    }

    public void RenderShapes()
    {
        int count = RenderersCount;
        if (count <= 0)
        {
            return;
        }
        CreateMissingShapeMaterials(10);

        float minDepth = 1f / count;

        // At this point renderers are sorted by distance from camera
        for (int i = 0; i < count; i++)
        {
            renderersData[i].Setup(shapeMaterials[i], Layer.OutlineLayer, minDepth);
        }
        shapeCamera.Render();

        foreach (var data in renderersData)
        {
            data.Restore();
        }
    }

    private void RenderCircles(int radius)
    {
        int count = RenderersCount;
        if (count <= 0)
        {
            return;
        }
        var circlesCamTransform = circleCamera.transform;

        float scale = 2f * radius / circleCamera.pixelHeight;
        float minDepth = 1f / count;

        // At this point renderers are sorted by distance from camera
        for (int i = 0; i < count; i++)
        {
            var data = renderersData[i];
            var renderer = data.Renderer;
            var center = renderer.bounds.center;
            var viewPoint = shapeCamera.WorldToViewportPoint(center);
            //var worldPoint = circlesCamera.ViewportToWorldPoint(viewPoint);
            //objectToWorld.SetTRS(worldPoint, Quaternion.LookRotation(circlesCamTransform.forward, circlesCamTransform.up), Vector3.one);
            var clipPoint = new Vector3()
            {
                x = (viewPoint.x - 0.5f) * 2f,
                y = (viewPoint.y - 0.5f) * 2f,
                z = data.Depth
            };
            var color = data.Color;
            color.a = Mathf.Clamp01(1f - data.Depth + minDepth);
            //Debug.Log($"{GetType().Name}.{i} | {data.CameraDistanceSqr} | {clipPoint.z}");
            circleInstanceData[i] = new InstanceData()
            {
                objectToWorld = Matrix4x4.identity,
                clipPosition = clipPoint,
                color = color,
                scale = scale
            };
        }
        circleInstanceBuffer.SetData(circleInstanceData, 0, 0, renderersData.Count);
        circleRenderParams.material.SetFloat("_MinDepth", minDepth);
        circleRenderParams.worldBounds = new Bounds(circlesCamTransform.position, Vector3.one);

        Graphics.RenderMeshPrimitives(circleRenderParams, quadMeshFilter.sharedMesh, 0, renderersData.Count);
        //Graphics.RenderMeshInstanced(circleRenderParams, quadMeshFilter.sharedMesh, 0, circleInstanceData, renderersData.Count);
        //Graphics.DrawMeshInstancedProcedural(quadMeshFilter.sharedMesh, 0, circleSpriteMaterial,
        //    circleRenderParams.worldBounds, renderersData.Count, circlePropertyBlock,
        //    ShadowCastingMode.Off, false, Layer.OutlineLayer, circlesCamera);
    }

    public void OnUpdate(OutlineCamera outlineCamera)
    {
        foreach (var obj in objects)
        {
            obj.SetRenderersColor();
        }
        SortRenderers();
        SetSortedRenderersDepth();
        RenderCircles(outlineCamera.LineThickness);
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
        DestroyRuntimeAssets();
    }
}
