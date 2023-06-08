using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinnedFlocking : MonoBehaviour
{
    private struct Boid
    {
        public Vector3 position;
        public Vector3 direction;
        public float noise_offset;
        public float frame;

        public Boid(Vector3 pos, Vector3 dir, float offset)
        {
            position = pos;
            direction = dir;
            noise_offset = offset;
            frame = 0;
        }
    }

    public ComputeShader shader;

    private SkinnedMeshRenderer boidSMR;
    public GameObject boidObject;
    private Animator animator;
    public AnimationClip animationClip;

    private int numOfFrames;
    public int boidsCount;
    public float spawnRadius;
    public Transform target;
    public float rotationSpeed = 1f;
    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    public float boidSpeedVariation = 1f;
    public float boidFrameSpeed = 10f;
    public bool frameInterpolation = true;

    private Mesh boidMesh;

    private int kernelHandle;
    private ComputeBuffer boidsBuffer;
    private ComputeBuffer vertexAnimationBuffer;
    public Material boidMaterial;
    private ComputeBuffer argsBuffer;
    private MaterialPropertyBlock props;
    readonly uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private Boid[] boidsArray;
    private int groupSizeX;
    private int numOfBoids;
    private Bounds bounds;

    private void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        shader.GetKernelThreadGroupSizes(kernelHandle, out uint x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)boidsCount / x);
        numOfBoids = groupSizeX * (int)x;

        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        // This property block is used only for avoiding an instancing bug.
        props = new MaterialPropertyBlock();
        props.SetFloat("_UniqueID", Random.value);

        InitBoids();
        GenerateVertexAnimationBuffer();
        InitShader();
    }

    private void InitBoids()
    {
        boidsArray = new Boid[numOfBoids];

        for (int i = 0; i < numOfBoids; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            Quaternion rot = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
            float offset = Random.value * 1000.0f;
            boidsArray[i] = new Boid(pos, rot.eulerAngles, offset);
        }
    }

    private void InitShader()
    {
        // Initialize the indirect draw args buffer.
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

        if (boidMesh)//Set by the GenerateSkinnedAnimationForGPUBuffer
        {
            args[0] = boidMesh.GetIndexCount(0);
            args[1] = (uint)numOfBoids;
            argsBuffer.SetData(args);
        }

        boidsBuffer = new ComputeBuffer(numOfBoids, 8 * sizeof(float));
        boidsBuffer.SetData(boidsArray);

        shader.SetFloat("rotationSpeed", rotationSpeed);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetFloat("boidFrameSpeed", boidFrameSpeed);
        shader.SetInt("boidsCount", numOfBoids);
        shader.SetInt("numOfFrames", numOfFrames);
        shader.SetBuffer(kernelHandle, "boidsBuffer", boidsBuffer);

        boidMaterial.SetBuffer("boidsBuffer", boidsBuffer);
        boidMaterial.SetInt("numOfFrames", numOfFrames);

        if (frameInterpolation && !boidMaterial.IsKeywordEnabled("FRAME_INTERPOLATION"))
        {
            boidMaterial.EnableKeyword("FRAME_INTERPOLATION");
        }
        if (!frameInterpolation && boidMaterial.IsKeywordEnabled("FRAME_INTERPOLATION"))
        {
            boidMaterial.DisableKeyword("FRAME_INTERPOLATION");
        }
    }

    private void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);

        boidsBuffer.GetData(boidsArray);

        Camera.main.transform.LookAt(GetFlockCenter());

        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, bounds, argsBuffer, 0, props);
    }

    private void OnDestroy()
    {
        boidsBuffer?.Release();
        argsBuffer?.Release();
        vertexAnimationBuffer?.Release();
    }

    private void GenerateVertexAnimationBuffer()
    {
        boidSMR = boidObject.GetComponentInChildren<SkinnedMeshRenderer>();

        animator = boidObject.GetComponentInChildren<Animator>();

        var animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);

        var bakedMesh = new Mesh();
        float sampleTime = 0f;

        numOfFrames = Mathf.ClosestPowerOfTwo((int)(animationClip.frameRate * animationClip.length));
        float frameDuration = animationClip.length / numOfFrames;

        boidMesh = boidSMR.sharedMesh;

        int vertexCount = boidMesh.vertexCount;
        var vertexAnimationData = new Vector4[vertexCount * numOfFrames];

        for (int i = 0; i < numOfFrames; i++)
        {
            animator.Play(animatorStateInfo.shortNameHash, 0, sampleTime);
            animator.Update(0f);

            boidSMR.BakeMesh(bakedMesh);

            var bakedMeshVertices = bakedMesh.vertices;
            for (int j = 0; j < vertexCount; j++)
            {
                Vector4 vertex = bakedMeshVertices[j];
                vertex.w = 1f;
                vertexAnimationData[j * numOfFrames + i] = vertex;
            }
            sampleTime += frameDuration;
        }
        vertexAnimationBuffer = new ComputeBuffer(vertexCount * numOfFrames, 16);
        vertexAnimationBuffer.SetData(vertexAnimationData);

        boidMaterial.SetBuffer("vertexAnimation", vertexAnimationBuffer);

        boidObject.SetActive(false);
    }

    private Vector3 GetFlockCenter()
    {
        var center = Vector3.zero;
        for (int i = 0; i < boidsCount; i++)
        {
            center += boidsArray[i].position;
        }
        center /= boidsCount;
        return center;
    }
}
