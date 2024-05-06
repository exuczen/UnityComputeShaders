using MustHave.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

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

    public void CreateTextures()
    {
        shapeTexture = new RenderTexture(Screen.width, Screen.height, 0)
        {
            name = "OutlineObjectsTexture",
            enableRandomWrite = true
        };
        shapeTexture.Create();
    }

    public void ReleaseTextures()
    {
        TextureUtils.Release(ref shapeTexture);
    }

    public void Setup(Camera parentCamera)
    {
        camera = GetComponent<Camera>();

        camera.CopyFrom(parentCamera);
        camera.targetTexture = shapeTexture;
        camera.backgroundColor = Color.clear;
        camera.cullingMask = Layer.OutlineMask;
        camera.allowMSAA = false;
        camera.enabled = false;
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

    private void ReleaseTexture(ref RenderTexture texture)
    {
        if (texture)
        {
            texture.Release();
            texture = null;
        }
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
