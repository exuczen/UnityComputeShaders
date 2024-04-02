using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class GrassClumps : MonoBehaviour
{
    private static readonly int SIZE_GRASS_CLUMP = Marshal.SizeOf<GrassClump>(); //5 * sizeof(float);

    private struct GrassClump
    {
        public Vector3 position;
        public float lean;
        public float noise;

        public GrassClump(Vector3 pos)
        {
            position = pos;
            lean = 0;
            noise = Random.Range(0.5f, 1);
            if (Random.value < 0.5f)
            {
                noise = -noise;
            }
        }
    }

    public Mesh mesh;
    public Material material;
    public ComputeShader shader;
    [Range(0, 1)]
    public float density = 0.8f;
    [Range(0.1f, 3)]
    public float scale = 0.2f;
    [Range(10, 45)]
    public float maxLean = 25;

    private ComputeBuffer clumpsBuffer;
    private ComputeBuffer argsBuffer;
    private GrassClump[] clumpsArray;
    private readonly uint[] argsArray = new uint[] { 0, 0, 0, 0, 0 };
    private Bounds bounds;
    private int timeID;
    private int groupSize;
    private int kernelLeanGrass;

    private void Start()
    {
        bounds = new Bounds(Vector3.zero, new Vector3(30, 30, 30));
        InitShader();
    }

    private void InitShader()
    {
        var mf = GetComponent<MeshFilter>();
        var bounds = mf.sharedMesh.bounds;

        var clumps = bounds.extents;
        var clumpsMlp = 10f * density * transform.localScale;
        clumps.x *= clumpsMlp.x;
        clumps.z *= clumpsMlp.z;

        int total = (int)(clumps.x * clumps.z);

        kernelLeanGrass = shader.FindKernel("LeanGrass");
        shader.GetKernelThreadGroupSizes(kernelLeanGrass, out uint threadGroupSize, out _, out _);
        groupSize = Mathf.CeilToInt((float)total / threadGroupSize);
        int count = groupSize * (int)threadGroupSize;

        clumpsArray = new GrassClump[count];

        for (int i = 0; i < count; i++)
        {
            var pos = new Vector3
            {
                x = Random.Range(-1f, 1f) * bounds.extents.x + bounds.center.x,
                y = 0,
                z = Random.Range(-1f, 1f) * bounds.extents.z + bounds.center.z
            };
            pos = transform.TransformPoint(pos);
            clumpsArray[i] = new GrassClump(pos);
        }
        clumpsBuffer = new ComputeBuffer(count, SIZE_GRASS_CLUMP);
        clumpsBuffer.SetData(clumpsArray);

        shader.SetBuffer(kernelLeanGrass, "clumpsBuffer", clumpsBuffer);
        shader.SetFloat("maxLean", maxLean * Mathf.Deg2Rad);
        timeID = Shader.PropertyToID("time");

        material.SetBuffer("clumpsBuffer", clumpsBuffer);
        material.SetFloat("_Scale", scale);

        argsArray[0] = mesh.GetIndexCount(0); //(uint)mesh.vertexCount;
        argsArray[1] = (uint)count;
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);
    }

    private void Update()
    {
        shader.SetFloat(timeID, Time.time);
        shader.Dispatch(kernelLeanGrass, groupSize, 1, 1);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private void OnDestroy()
    {
        clumpsBuffer?.Release();
        argsBuffer?.Release();
    }
}
