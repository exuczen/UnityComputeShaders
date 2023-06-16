using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Challenge6 : MonoBehaviour
{
    private static readonly int SIZE_GRASS_CLUMP = 10 * sizeof(float);

    private struct GrassClump
    {
        public Vector3 position;
        public float lean;
        public float trample;
        public Quaternion quaternion;
        public float noise;

        public GrassClump(Vector3 pos)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            lean = 0;
            noise = Random.Range(0.5f, 1);
            if (Random.value < 0.5f) noise = -noise;
            quaternion = Quaternion.identity;
            trample = 0;
        }
    }

    public Mesh mesh;
    public Material material;
    public Material visualizeNoise;
    public bool viewNoise = false;
    public ComputeShader shader;
    [Range(0, 1)]
    public float density;
    [Range(0.1f, 3)]
    public float scale;
    [Range(10, 45)]
    public float maxLean;
    public Transform trampler;
    [Range(0.1f, 2)]
    public float trampleRadius = 0.5f;
    //TODO: Add wind direction (0-360), speed (0-2)  and scale (10-1000)
    [Range(0, 2)]
    public float windSpeed = 0.2f;
    [Range(0, 360)]
    public float windDirection = 130f;
    [Range(10, 1000)]
    public float windScale = 100f;

    private ComputeBuffer clumpsBuffer;
    private ComputeBuffer argsBuffer;
    private GrassClump[] clumpsArray;
    private readonly uint[] argsArray = new uint[] { 0, 0, 0, 0, 0 };
    private Bounds bounds;
    private int timeID;
    private int tramplePosID;
    private int groupSize;
    private int kernelUpdateGrass;
    private Material groundMaterial;

    // Start is called before the first frame update
    private void Start()
    {
        bounds = new Bounds(Vector3.zero, new Vector3(30, 30, 30));

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        groundMaterial = renderer.material;

        InitShader();
    }

    private void OnValidate()
    {
        if (groundMaterial != null)
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();

            renderer.material = (viewNoise) ? visualizeNoise : groundMaterial;

            //TODO: Set wind vector
            var wind = new Vector4
            {
                x = Mathf.Cos(windDirection * Mathf.Deg2Rad),
                y = Mathf.Sin(windDirection * Mathf.Deg2Rad),
                z = windSpeed,
                w = windScale
            };
            shader.SetVector("wind", wind);
            visualizeNoise.SetVector("wind", wind);
        }
    }

    void InitShader()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Bounds bounds = mf.sharedMesh.bounds;
        var size = new Vector2(bounds.extents.x * transform.localScale.x, bounds.extents.z * transform.localScale.z);

        Vector2 clumps = size;
        Vector3 vec = transform.localScale / 0.1f * density;
        clumps.x *= vec.x;
        clumps.y *= vec.z;

        int total = (int)clumps.x * (int)clumps.y;

        kernelUpdateGrass = shader.FindKernel("UpdateGrass");

        shader.GetKernelThreadGroupSizes(kernelUpdateGrass, out uint threadGroupSize, out _, out _);
        groupSize = Mathf.CeilToInt((float)total / threadGroupSize);
        int count = groupSize * (int)threadGroupSize;

        clumpsArray = new GrassClump[count];

        for (int i = 0; i < count; i++)
        {
            var pos = new Vector3(Random.value * size.x * 2 - size.x, 0, Random.value * size.y * 2 - size.y);
            clumpsArray[i] = new GrassClump(pos);
        }

        clumpsBuffer = new ComputeBuffer(count, SIZE_GRASS_CLUMP);
        clumpsBuffer.SetData(clumpsArray);

        shader.SetBuffer(kernelUpdateGrass, "clumpsBuffer", clumpsBuffer);
        shader.SetFloat("maxLean", maxLean * Mathf.PI / 180);
        shader.SetFloat("trampleRadius", trampleRadius);
        //TODO: Set wind vector
        var wind = new Vector4
        {
            x = Mathf.Cos(windDirection * Mathf.Deg2Rad),
            y = Mathf.Sin(windDirection * Mathf.Deg2Rad),
            z = windSpeed,
            w = windScale
        };
        shader.SetVector("wind", wind);
        timeID = Shader.PropertyToID("time");
        tramplePosID = Shader.PropertyToID("tramplePos");

        argsArray[0] = mesh.GetIndexCount(0);
        argsArray[1] = (uint)count;
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        material.SetBuffer("clumpsBuffer", clumpsBuffer);
        material.SetFloat("_Scale", scale);

        visualizeNoise.SetVector("wind", wind);
    }

    // Update is called once per frame
    private void Update()
    {
        shader.SetFloat(timeID, Time.time);
        shader.SetVector(tramplePosID, trampler.position);

        shader.Dispatch(kernelUpdateGrass, groupSize, 1, 1);

        if (!viewNoise)
        {
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
        }
    }

    private void OnDestroy()
    {
        clumpsBuffer.Release();
        argsBuffer.Release();
    }
}
