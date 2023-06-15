using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassTerrain : MonoBehaviour
{
    private static readonly int SIZE_GRASS_CLUMP = 5 * sizeof(float);

    private struct GrassClump
    {
        public Vector3 position;
        public float lean;
        public float noise;

        public GrassClump(Vector3 pos)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            lean = 0;
            noise = Random.Range(0.5f, 1);
            if (Random.value < 0.5f) noise = -noise;
        }
    }

    public Mesh mesh;
    public Material material;
    public ComputeShader shader;
    [Range(0, 3)]
    public float density = 0.8f;
    [Range(0.1f, 3)]
    public float scale = 0.2f;
    [Range(10, 45)]
    public float maxLean = 25;
    [Range(0, 1)]
    public float heightAffect = 0.5f;

    private ComputeBuffer clumpsBuffer;
    private ComputeBuffer argsBuffer;
    private GrassClump[] clumpsArray;
    private readonly uint[] argsArray = new uint[] { 0, 0, 0, 0, 0 };
    private Bounds bounds;
    private int timeID;
    private int groupSize;
    private int kernelLeanGrass;

    // Start is called before the first frame update
    private void Start()
    {
        bounds = new Bounds(Vector3.zero, new Vector3(30, 30, 30));
        InitShader();
    }

    private void InitShader()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Bounds bounds = mf.sharedMesh.bounds;

        Vector3 clumps = bounds.extents;
        Vector3 vec = transform.localScale / 0.1f * density;
        clumps.x *= vec.x;
        clumps.z *= vec.z;

        int total = (int)clumps.x * (int)clumps.z;

        kernelLeanGrass = shader.FindKernel("LeanGrass");

        shader.GetKernelThreadGroupSizes(kernelLeanGrass, out uint threadGroupSize, out _, out _);
        groupSize = Mathf.CeilToInt((float)total / threadGroupSize);
        int count = groupSize * (int)threadGroupSize;

        InitPositionsArray(count, bounds);

        count = clumpsArray.Length;

        clumpsBuffer = new ComputeBuffer(count, SIZE_GRASS_CLUMP);
        clumpsBuffer.SetData(clumpsArray);

        shader.SetBuffer(kernelLeanGrass, "clumpsBuffer", clumpsBuffer);
        shader.SetFloat("maxLean", maxLean * Mathf.PI / 180);
        timeID = Shader.PropertyToID("time");

        argsArray[0] = mesh.GetIndexCount(0);
        argsArray[1] = (uint)count;
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        material.SetBuffer("clumpsBuffer", clumpsBuffer);
        material.SetFloat("_Scale", scale);
    }

    private void InitPositionsArray(int count, Bounds bounds)
    {
        clumpsArray = new GrassClump[count];

        gameObject.AddComponent<MeshCollider>();

        var v = new Vector3(0f, bounds.max.y, 0f);
        v = transform.TransformPoint(v);
        float maxY = v.y;

        v.Set(0f, bounds.min.y, 0f);
        v = transform.TransformPoint(v);
        float minY = v.y;

        float range = bounds.size.y; //maxY - minY; 
        float castY = maxY + 10f;

        int loopCount = 0;
        int index = 0;

        while (index < count && loopCount < 10 * count)
        {
            loopCount++;
            var pos = new Vector3
            {
                x = Random.Range(bounds.min.x, bounds.max.x),
                y = 0,
                z = Random.Range(bounds.min.z, bounds.max.z),
            };
            pos = transform.TransformPoint(pos);
            pos.y = castY;

            if (Physics.Raycast(pos, Vector3.down, out var hit))
            {
                pos.y = hit.point.y;

                float deltaHeight = heightAffect * (pos.y - minY) / range;
                if (Random.value > deltaHeight)
                {
                    clumpsArray[index++] = new GrassClump(pos);
                }
            }
        }
    }

    // Update is called once per frame
    private void Update()
    {
        shader.SetFloat(timeID, Time.time);
        shader.Dispatch(kernelLeanGrass, groupSize, 1, 1);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private void OnDestroy()
    {
        clumpsBuffer.Release();
        argsBuffer.Release();
    }
}
