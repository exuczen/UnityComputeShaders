using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class GrassTrample : MonoBehaviour
{
    private readonly static int SIZE_GRASS_CLUMP = Marshal.SizeOf<GrassClump>(); //10 * sizeof(float);

    struct GrassClump
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
            trample = 0;
            quaternion = Quaternion.identity;
        }
    }

    public Mesh mesh;
    public Material material;
    public ComputeShader shader;
    [Range(0, 1)]
    public float density;
    [Range(0.1f, 3)]
    public float scale;
    [Range(0.5f, 3)]
    public float speed;
    [Range(10, 45)]
    public float maxLean;
    public Transform trampler;
    [Range(0.1f, 2)]
    public float trampleRadius = 0.5f;

    private ComputeBuffer clumpsBuffer;
    private ComputeBuffer argsBuffer;
    private GrassClump[] clumpsArray;
    private readonly uint[] argsArray = new uint[] { 0, 0, 0, 0, 0 };
    private Bounds bounds;
    private int timeID;
    private int tramplePosID;
    private int groupSize;
    private int kernelUpdateGrass;
    private Vector4 pos = new Vector4();

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
            var pos = new Vector3(Random.Range(-size.x, size.x), 0, Random.Range(-size.y, size.y));
            clumpsArray[i] = new GrassClump(pos);
        }

        clumpsBuffer = new ComputeBuffer(count, SIZE_GRASS_CLUMP);
        clumpsBuffer.SetData(clumpsArray);

        shader.SetBuffer(kernelUpdateGrass, "clumpsBuffer", clumpsBuffer);
        shader.SetFloat("maxLean", maxLean * Mathf.PI / 180);
        shader.SetFloat("trampleRadius", trampleRadius);
        shader.SetFloat("speed", speed);
        timeID = Shader.PropertyToID("time");
        tramplePosID = Shader.PropertyToID("tramplePos");

        argsArray[0] = mesh.GetIndexCount(0);
        argsArray[1] = (uint)count;
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        material.SetBuffer("clumpsBuffer", clumpsBuffer);
        material.SetFloat("_Scale", scale);
    }

    private void Update()
    {
        shader.SetFloat(timeID, Time.time);
        pos = trampler.position;
        shader.SetVector(tramplePosID, pos);

        shader.Dispatch(kernelUpdateGrass, groupSize, 1, 1);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private void OnDestroy()
    {
        clumpsBuffer.Release();
        argsBuffer.Release();
    }
}
