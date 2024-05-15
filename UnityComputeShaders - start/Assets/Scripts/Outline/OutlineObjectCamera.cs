using MustHave;
using MustHave.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class OutlineObjectCamera : MonoBehaviour
{
    private const int LineMaxThickness = 100;
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
    public int LineThickness => lineThickness;
    public bool CircleInstancingInitiated => circleInstanceBuffer != null;

    private int RenderersCount => Mathf.Min(RenderersCapacity, renderersData.Count);
    private int ObjectsCount => Mathf.Min(RenderersCapacity, objects.Count);

    [SerializeField]
    private Material outlineShapeMaterial = null;
    [SerializeField]
    private Material circleSpriteMaterial = null;
    [SerializeField]
    private MeshFilter quadMeshFilter = null;
    [SerializeField]
    private Camera circleCamera = null;
    [SerializeField, Range(1, LineMaxThickness)]
    private int lineThickness = 5;
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

    public void CreateRuntimeAssets(Vector2Int texSize, out Vector2Int shapeTexSize, out Vector2Int shapeTexOffset)
    {
        CreateTextures(texSize, out shapeTexSize, out shapeTexOffset);

        CreateMaterials();

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
            shapeCamera.fieldOfView = GetExtendedFieldOfView(parentCamera);
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

    private float GetExtendedFieldOfView(Camera parentCamera)
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
        float destFovVerti;

        if (w > h)
        {
            float dh = LineMaxThickness << 1;
            float tanHalfFovVerti = parentCamera.GetTanHalfFovVerti();
            float destHalfFovVerti = Mathf.Atan2(tanHalfFovVerti * (h + dh), h);

            destFovVerti = destHalfFovVerti * 2f * Mathf.Rad2Deg;
        }
        else
        {
            float dw = LineMaxThickness << 1;
            float tanHalfFovHori = parentCamera.GetTanHalfFovHori();
            float destHalfFovHori = Mathf.Atan2(tanHalfFovHori * (w + dw), w);

            float destFovHori = destHalfFovHori * 2f * Mathf.Rad2Deg;
            destFovVerti = Camera.HorizontalToVerticalFieldOfView(destFovHori, aspect);
        }
        return destFovVerti;
    }


    private void CreateTextures(Vector2Int texSize, out Vector2Int shapeTexSize, out Vector2Int shapeTexOffset)
    {
        Vector2Int extendedSize = default;
        Vector2Int texOffset = default;
        if (texSize.x > texSize.y)
        {
            extendedSize.y = texSize.y + 2 * LineMaxThickness;
            extendedSize.x = extendedSize.y * texSize.x / texSize.y;

            texOffset.y = LineMaxThickness;
            texOffset.x = (int)(texOffset.y * texSize.x / texSize.y + 0.5f);
        }
        else
        {
            extendedSize.x = texSize.x + 2 * LineMaxThickness;
            extendedSize.y = extendedSize.x * texSize.y / texSize.x;

            texOffset.x = LineMaxThickness;
            texOffset.y = (int)(texOffset.x * texSize.y / texSize.x + 0.5f);
        }
        shapeTexOffset = texOffset;
        texSize = shapeTexSize = extendedSize;

        shapeTexture = CreateTexture(texSize, "OutlineObjectsShapeTexture");
        circleTexture = CreateTexture(texSize, "OutlineObjectsCircleTexture");
        circleTexture.filterMode = FilterMode.Point;
    }

    private void CreateMaterials()
    {
        if (!shapeMaterials[0])
        {
            for (int i = 0; i < shapeMaterials.Length; i++)
            {
                shapeMaterials[i] = new Material(outlineShapeMaterial);
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

    public void RenderShapes()
    {
        for (int i = 0; i < ObjectsCount; i++)
        {
            objects[i].Setup(shapeMaterials[i], Layer.OutlineLayer);
        }
        int count = RenderersCount;
        for (int i = 0; i < count; i++)
        {
            //Debug.Log($"{GetType().Name}.{i} | {renderersData[i].CameraDistanceSqr} | {1f - (float)i / count}");
            renderersData[i].SetMaterialDepth((float)i / count);
        }
        shapeCamera.Render();

        foreach (OutlineObject obj in objects)
        {
            obj.Restore();
        }
    }

    private void RenderCircles()
    {
        int count = RenderersCount;

        if (count <= 0)
        {
            return;
        }
        var circlesCamTransform = circleCamera.transform;

        float scale = 2f * lineThickness / circleCamera.pixelHeight;

        for (int i = 0; i < count; i++)
        {
            var renderer = renderersData[i].Renderer;
            var center = renderer.bounds.center;
            var viewPoint = shapeCamera.WorldToViewportPoint(center);
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
        //Graphics.DrawMeshInstancedProcedural(quadMeshFilter.sharedMesh, 0, circleSpriteMaterial,
        //    circleRenderParams.worldBounds, renderersData.Count, circlePropertyBlock,
        //    ShadowCastingMode.Off, false, Layer.OutlineLayer, circlesCamera);
    }

    public void OnUpdate()
    {
        foreach (var obj in objects)
        {
            obj.SetRenderersColor();
        }
        SortRenderers();
        RenderCircles();
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
