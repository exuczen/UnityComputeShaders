using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePhysics : MonoBehaviour
{
    private struct Ball
    {
        public Vector3 position;
        public Vector3 velocity;
        public Color color;

        public Ball(float posRange, float maxVel)
        {
            position.x = Random.value * posRange - posRange * 0.5f;
            position.y = Random.value * posRange;
            position.z = Random.value * posRange - posRange * 0.5f;
            //velocity = Random.insideUnitSphere * maxVel;
            velocity.x = Random.value * maxVel - maxVel * 0.5f;
            velocity.y = Random.value * maxVel - maxVel * 0.5f;
            velocity.z = Random.value * maxVel - maxVel * 0.5f;
            color.r = Random.value;
            color.g = Random.value;
            color.b = Random.value;
            color.a = 1;
        }
    }

    public ComputeShader shader;

    public Mesh ballMesh;
    public Material ballMaterial;
    public int ballsCount;
    public float radius = 0.08f;

    private int kernelHandle;
    private ComputeBuffer ballsBuffer;
    private ComputeBuffer argsBuffer;
    private readonly uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private Ball[] ballsArray;
    private int groupSizeX;
    private int numOfBalls;
    private Bounds bounds;

    private MaterialPropertyBlock props;

    private void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        shader.GetKernelThreadGroupSizes(kernelHandle, out uint x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)ballsCount / x);
        numOfBalls = groupSizeX * (int)x;

        props = new MaterialPropertyBlock();
        props.SetFloat("_UniqueID", Random.value);

        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        InitBalls();
        InitShader();
    }

    private void InitBalls()
    {
        ballsArray = new Ball[numOfBalls];

        for (int i = 0; i < numOfBalls; i++)
        {
            ballsArray[i] = new Ball(4, 1.0f);
        }
    }

    private void InitShader()
    {
        ballsBuffer = new ComputeBuffer(numOfBalls, 10 * sizeof(float));
        ballsBuffer.SetData(ballsArray);

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (ballMesh != null)
        {
            args[0] = ballMesh.GetIndexCount(0);
            args[1] = (uint)numOfBalls;
            args[2] = ballMesh.GetIndexStart(0);
            args[3] = ballMesh.GetBaseVertex(0);
        }
        argsBuffer.SetData(args);

        shader.SetBuffer(kernelHandle, "ballsBuffer", ballsBuffer);
        shader.SetInt("ballsCount", numOfBalls);
        shader.SetVector("limitsXZ", new Vector4(-2.5f + radius, 2.5f - radius, -2.5f + radius, 2.5f - radius));
        shader.SetFloat("floorY", -2.5f + radius);
        shader.SetFloat("radius", radius);

        ballMaterial.SetFloat("_Radius", radius * 2);
        ballMaterial.SetBuffer("ballsBuffer", ballsBuffer);
    }

    private void Update()
    {
        int iterations = 5;
        shader.SetFloat("deltaTime", Time.deltaTime / iterations);

        for (int i = 0; i < iterations; i++)
        {
            shader.Dispatch(kernelHandle, groupSizeX, 1, 1);
        }

        Graphics.DrawMeshInstancedIndirect(ballMesh, 0, ballMaterial, bounds, argsBuffer, 0, props);
    }

    private void OnDestroy()
    {
        ballsBuffer?.Dispose();

        argsBuffer?.Dispose();
    }
}

