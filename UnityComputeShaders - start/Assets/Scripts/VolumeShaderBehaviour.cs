﻿using MustHave;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(UpdateInEditMode))]
public class VolumeShaderBehaviour : MonoBehaviour
{
    //private const string InteriorPassName = "Interior";

    private readonly struct ShaderData
    {
        public static readonly int SampleAlphaID = Shader.PropertyToID("_SampleAlpha");
        public static readonly int FragAlphaID = Shader.PropertyToID("_FragAlpha");
        public static readonly int StepSizeID = Shader.PropertyToID("_StepSize");
        public static readonly int StepCountID = Shader.PropertyToID("_StepCount");
        public static readonly int ObjectScaleID = Shader.PropertyToID("_ObjectScale");

        public static readonly int CullID = Shader.PropertyToID("_Cull");
        //public static readonly int InteriorEnabledID = Shader.PropertyToID("_InteriorEnabled");
    }

    [SerializeField]
    private Material material = null;
    [SerializeField, HideInInspector]
    private Material interiorMaterial = null;
    [SerializeField]
    private VolumeCrossSection crossSection = null;

    //private MeshRenderer meshRenderer = null;
    private MeshFilter meshFilter = null;

    private void OnEnable()
    {
        //meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        //Camera.onPostRender -= RenderMeshWithCamera;
        //Camera.onPostRender -= RenderMeshWithCamera;
        //Camera.onPostRender -= OnPostRenderWithCamera;
        //Camera.onPostRender += OnPostRenderWithCamera;
        //Camera.onPreCull -= RenderMeshWithCamera;
        //Camera.onPreCull += RenderMeshWithCamera;
    }

    private void Update()
    {
        UpdateShader();
    }

    private void OnValidate()
    {
        Debug.Log($"{GetType().Name}.OnValidate");
    }

    private void OnMouseDown()
    {
        Debug.Log($"{GetType().Name}.OnMouseDown");
    }

    private void OnRenderObject() { }

    private void OnPreRender() { }

    private void OnPostRenderWithCamera(Camera camera) { }

    private void OnGUI()
    {
        int dy = 15;
        int y = -dy;
        Rect getTextLineRect(int width = 400, int height = 20) => new(10, y += dy, width, height);

        GUI.Label(getTextLineRect(), $"({GetCameraForward()})");
    }

    private Vector3 GetCameraForward()
    {
        var camera = GetCamera();
        return camera ? camera.transform.forward : Vector3.forward;
    }

    private Vector3 GetCameraPosition()
    {
        var camera = GetCamera();
        return camera ? camera.transform.position : Vector3.zero;
    }

    private Camera GetCamera()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView && sceneView.camera)
            {
                return sceneView.camera;
            }
            else
            {
                return null;
            }
        }
        else
#endif
        {
            return CameraUtils.MainOrCurrent;
        }
    }

    public void UpdateShader()
    {
        if (material)
        {
            //var lossyScale = transform.lossyScale;
            //float maxScale = Mathf.Max(lossyScale.x, lossyScale.y, lossyScale.z);

            //CullMode cullMode = (CullMode)material.GetInteger(ShaderData.CullID);
            //material.SetInteger(ShaderData.InteriorEnabledID, cullMode == CullMode.Back ? 1 : 0);
            //material.SetFloat(ShaderData.ObjectScaleID, maxScale);

            //Debug.Log($"{GetType().Name}.{material.FindPass(InteriorPassName)} | {material.passCount} | {material.GetPassName(1)} | {cullMode == CullMode.Back}");

            if (interiorMaterial)
            {
                SetInteriorShaderProperties();
            }
        }
        if (crossSection)
        {
            var csTransform = crossSection.transform;
            var crossSectionNormal = transform.InverseTransformDirection(-csTransform.forward);
            var crossSectionPoint = csTransform.localPosition;
            //var crossSectionPoint = transform.InverseTransformPoint(csTransform.position);

            material.SetVector("LocalCrossSectionNormal", crossSectionNormal);
            material.SetVector("LocalCrossSectionPoint", crossSectionPoint);

            material.SetVector("WorldCrossSectionNormal", -csTransform.forward);
            material.SetVector("WorldCrossSectionPoint", csTransform.position);
        }
        if (material.GetInteger("_DebugModelView") > 0)
        {
            material.EnableKeyword("DEBUG_MODEL_VIEW");

            material.SetMatrix("ModelMatrix", transform.localToWorldMatrix);
            material.SetMatrix("ModelMatrixInv", transform.worldToLocalMatrix); // !!! transform.worldToLocalMatrix axes are scaled !!!
            material.SetVector("ModelPosition", transform.position);

            material.SetVector("ModelCameraForward", transform.InverseTransformVector(GetCameraForward()).normalized);
            material.SetVector("WorldCameraForward", GetCameraForward());
            material.SetVector("WorldCameraPosition", GetCameraPosition());
        }
        else
        {
            material.DisableKeyword("DEBUG_MODEL_VIEW");
        }
        if (material.GetInteger("_BlendEnabled") > 0)
        {
            material.EnableKeyword("BLEND_ENABLED");
        }
        else
        {
            material.DisableKeyword("BLEND_ENABLED");
        }
    }

    private void SetInteriorShaderProperties()
    {
        interiorMaterial.SetFloat(ShaderData.SampleAlphaID, material.GetFloat(ShaderData.SampleAlphaID));
        interiorMaterial.SetFloat(ShaderData.FragAlphaID, material.GetFloat(ShaderData.FragAlphaID));
        interiorMaterial.SetFloat(ShaderData.StepSizeID, material.GetFloat(ShaderData.StepSizeID));
        interiorMaterial.SetInt(ShaderData.StepCountID, material.GetInt(ShaderData.StepCountID));
        interiorMaterial.SetFloat(ShaderData.ObjectScaleID, material.GetFloat(ShaderData.ObjectScaleID));
    }

    private void RenderMeshWithCamera(Camera camera)
    {
        var matrix = transform.localToWorldMatrix;
        matrix.SetColumn(3, new Vector4(20, 0, 0, matrix.m33));
        //Graphics.DrawMeshNow(meshFilter.sharedMesh, matrix);
        //Graphics.DrawMesh(meshFilter.sharedMesh, matrix, material, (int)meshRenderer.renderingLayerMask);
        var renderParams = new RenderParams(material);
        Graphics.RenderMesh(renderParams, meshFilter.sharedMesh, 0, matrix);
    }
}
