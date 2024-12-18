using MustHave;
using System;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(UpdateInEditMode))]
public class VolumeShaderBehaviour : MonoBehaviour
{
    private readonly struct ShaderData
    {
        public const string BLEND_ENABLED = "BLEND_ENABLED";
        public const string CROSS_SECTION = "CROSS_SECTION";
        public const string DEBUG_MODEL_VIEW = "DEBUG_MODEL_VIEW";

        public static readonly int SampleAlphaID = Shader.PropertyToID("_SampleAlpha");
        public static readonly int FragAlphaID = Shader.PropertyToID("_FragAlpha");
        public static readonly int StepSizeID = Shader.PropertyToID("_StepSize");
        public static readonly int StepCountID = Shader.PropertyToID("_StepCount");
        public static readonly int CullID = Shader.PropertyToID("_Cull");
        public static readonly int BlendEnabledID = Shader.PropertyToID("_BlendEnabled");
        public static readonly int CrossSectionID = Shader.PropertyToID("_CrossSection");
        public static readonly int DebugModelViewID = Shader.PropertyToID("_DebugModelView");

        public static readonly int LocalCrossSectionNormalID = Shader.PropertyToID("LocalCrossSectionNormal");
        public static readonly int LocalCrossSectionPointID = Shader.PropertyToID("LocalCrossSectionPoint");
        public static readonly int WorldCrossSectionNormalID = Shader.PropertyToID("WorldCrossSectionNormal");
        public static readonly int WorldCrossSectionPointID = Shader.PropertyToID("WorldCrossSectionPoint");

        public static readonly int ModelMatrixID = Shader.PropertyToID("ModelMatrix");
        public static readonly int ModelMatrixInvID = Shader.PropertyToID("ModelMatrixInv");
        public static readonly int ModelPositionID = Shader.PropertyToID("ModelPosition");

        public static readonly int ModelCameraForwardID = Shader.PropertyToID("ModelCameraForward");
        public static readonly int WorldCameraForwardID = Shader.PropertyToID("WorldCameraForward");
        public static readonly int WorldCameraPositionID = Shader.PropertyToID("WorldCameraPosition");
    }

    [SerializeField]
    private Material material = null;
    [SerializeField]
    private VolumeCrossSection crossSection = null;

    private void OnEnable()
    {
        var camera = GetCamera();
        if (camera)
        {
            camera.depthTextureMode = DepthTextureMode.Depth;
        }
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

    private void OnGUI()
    {
        int dy = 15;
        int y = -dy;
        Rect getTextLineRect(int width = 400, int height = 20) => new(10, y += dy, width, height);

        GUI.Label(getTextLineRect(), $"{GetCameraForward()}");
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
        if (crossSection && material.GetInteger(ShaderData.CrossSectionID) > 0)
        {
            if (!material.IsKeywordEnabled(ShaderData.CROSS_SECTION))
            {
                throw new Exception("CrossSection != CROSS_SECTION");
            }
            var csTransform = crossSection.transform;
            var crossSectionNormal = transform.InverseTransformDirection(-csTransform.forward);
            var crossSectionPoint = csTransform.localPosition;
            //var crossSectionPoint = transform.InverseTransformPoint(csTransform.position);

            material.SetVector(ShaderData.LocalCrossSectionNormalID, crossSectionNormal);
            material.SetVector(ShaderData.LocalCrossSectionPointID, crossSectionPoint);

            material.SetVector(ShaderData.WorldCrossSectionNormalID, -csTransform.forward);
            material.SetVector(ShaderData.WorldCrossSectionPointID, csTransform.position);
        }
        if (material.GetInteger(ShaderData.DebugModelViewID) > 0)
        {
            if (!material.IsKeywordEnabled(ShaderData.DEBUG_MODEL_VIEW))
            {
                throw new Exception("DebugModelView != DEBUG_MODEL_VIEW");
            }
            material.SetMatrix(ShaderData.ModelMatrixID, transform.localToWorldMatrix);
            material.SetMatrix(ShaderData.ModelMatrixInvID, transform.worldToLocalMatrix); // !!! transform.worldToLocalMatrix axes are scaled !!!
            material.SetVector(ShaderData.ModelPositionID, transform.position);

            material.SetVector(ShaderData.ModelCameraForwardID, transform.InverseTransformVector(GetCameraForward()).normalized);
            material.SetVector(ShaderData.WorldCameraForwardID, GetCameraForward());
            material.SetVector(ShaderData.WorldCameraPositionID, GetCameraPosition());
        }
        if (material.GetInteger(ShaderData.BlendEnabledID) > 0 && !material.IsKeywordEnabled(ShaderData.BLEND_ENABLED))
        {
            throw new Exception("BlendEnabled != BLEND_ENABLED");
        }
    }
}
