using MustHave;
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
    [SerializeField]
    private Material interiorMaterial = null;

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
        var lossyScale = transform.lossyScale;
        float maxScale = Mathf.Max(lossyScale.x, lossyScale.y, lossyScale.z);
        if (material)
        {
            //CullMode cullMode = (CullMode)material.GetInteger(ShaderData.CullID);
            //material.SetInteger(ShaderData.InteriorEnabledID, cullMode == CullMode.Back ? 1 : 0);
            //material.SetVector("_CamForward", GetCameraForward());

            //Debug.Log($"{GetType().Name}.{material.FindPass(InteriorPassName)} | {material.passCount} | {material.GetPassName(1)} | {cullMode == CullMode.Back}");

            material.SetFloat(ShaderData.ObjectScaleID, maxScale);

            if (interiorMaterial)
            {
                SetInteriorShaderProperties();
            }
        }
    }

    private void OnValidate()
    {
        Debug.Log($"{GetType().Name}.OnValidate");
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
        Vector3 cameraForward;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView && sceneView.camera)
            {
                cameraForward = sceneView.camera.transform.forward;
            }
            else
            {
                cameraForward = Vector3.forward;
            }
        }
        else
#endif
        {
            cameraForward = CameraUtils.MainOrCurrent.transform.forward;
        }
        return cameraForward;
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
