using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFlocking : MonoBehaviour
{
    private struct Boid
    {
        public Vector3 position;
        public Vector3 direction;

        public Boid(Vector3 pos)
        {
            position = pos;
            direction = Vector3.zero;
        }
    }

    public ComputeShader shader;
    public GameObject boidPrefab;
    public Transform target;

    public float rotationSpeed = 1f;
    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    public float boidSpeedVariation = 1f;
    public int boidsCount;
    public float spawnRadius;

    private int kernelHandle;
    private ComputeBuffer boidsBuffer;
    private Boid[] boidsArray;
    private GameObject[] boids;
    private int groupSizeX;
    private int numOfBoids;

    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        shader.GetKernelThreadGroupSizes(kernelHandle, out uint x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)boidsCount / x);
        numOfBoids = groupSizeX * (int)x;

        InitBoids();
        InitShader();
    }

    private void InitBoids()
    {
        boids = new GameObject[numOfBoids];
        boidsArray = new Boid[numOfBoids];

        for (int i = 0; i < numOfBoids; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            boidsArray[i] = new Boid(pos);
            boids[i] = Instantiate(boidPrefab, pos, Quaternion.identity);
            boidsArray[i].direction = boids[i].transform.forward;
        }
    }

    void InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, 6 * sizeof(float));
        boidsBuffer.SetData(boidsArray);

        shader.SetBuffer(kernelHandle, "boidsBuffer", boidsBuffer);
        shader.SetFloat("rotationSpeed", rotationSpeed);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetInt("boidsCount", boidsCount);
    }

    void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);

        boidsBuffer.GetData(boidsArray);

        for (int i = 0; i < boidsArray.Length; i++)
        {
            boids[i].transform.localPosition = boidsArray[i].position;

            if (!boidsArray[i].direction.Equals(Vector3.zero))
            {
                boids[i].transform.rotation = Quaternion.LookRotation(boidsArray[i].direction);
            }

        }
    }

    void OnDestroy()
    {
        boidsBuffer?.Dispose();
    }
}

