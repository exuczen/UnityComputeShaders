using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class GrassBlades : MonoBehaviour
{
    private static readonly int SIZE_GRASS_BLADE = Marshal.SizeOf<GrassBlade>(); //6 * sizeof(float);

    private struct GrassBlade
    {
        public Vector3 position;
        public float bend;
        public float noise;
        public float fade;

        public GrassBlade(Vector3 pos)
        {
            position = pos;
            bend = 0;
            noise = Random.value;
            fade = Random.Range(0.5f, 1);
        }
    }

    public Material material;
    public ComputeShader shader;
    public Material visualizeNoise;
    public bool viewNoise = false;
    [Range(0, 1)]
    public float density;
    [Range(0.1f, 3)]
    public float scale;
    [Range(10, 45)]
    public float maxBend;
    [Range(0, 2)]
    public float windSpeed;
    [Range(0, 360)]
    public float windDirection;
    [Range(10, 1000)]
    public float windScale;

    private ComputeBuffer bladesBuffer;
    private ComputeBuffer argsBuffer;
    private GrassBlade[] bladesArray;
    private readonly uint[] argsArray = new uint[] { 0, 0, 0, 0, 0 };
    private Bounds bounds;
    private int timeID;
    private int groupSize;
    private int kernelBendGrass;
    private Mesh blade;
    private Material groundMaterial;

    Mesh Blade
    {
        get
        {
            Mesh mesh;

            if (blade != null)
            {
                mesh = blade;
            }
            else
            {
                mesh = new Mesh();

                float height = 0.2f;
                float rowHeight = height / 4f;
                float halfWidth = height / 10f;

                //1. Use the above variables to define the vertices array
                var vertices = new Vector3[]
                {
                    new(-halfWidth, 0f, 0f),
                    new( halfWidth, 0f, 0f),
                    new(-halfWidth, rowHeight, 0f),
                    new( halfWidth, rowHeight, 0f),
                    new(-halfWidth * 0.9f, rowHeight * 2f, 0f),
                    new( halfWidth * 0.9f, rowHeight * 2f, 0f),
                    new(-halfWidth * 0.8f, rowHeight * 3f, 0f),
                    new( halfWidth * 0.8f, rowHeight * 3f, 0f),
                    new( 0f, rowHeight * 4f, 0f),
                };

                //2. Define the normals array, hint: each vertex uses the same normal
                var normal = new Vector3(0, 0, -1);
                var normals = new Vector3[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    normals[i] = normal;
                }

                //3. Define the uvs array
                var uvs = new Vector2[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    uvs[i].x = 0.5f + 0.5f * vertices[i].x / halfWidth; //(vertices[i].x + halfWidth) / (2f * halfWidth)
                    uvs[i].y = vertices[i].y / height;
                }

                //4. Define the indices array
                var indices = new int[]
                {
                    0, 1, 2, 1, 3, 2,
                    2, 3, 4, 3, 5, 4,
                    4, 5, 6, 5, 7, 6,
                    6, 7, 8
                };

                //5. Assign the mesh properties using the arrays
                //   for indices use
                //   mesh.SetIndices(indices, MeshTopology.Triangles, 0);
                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.uv = uvs;
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            }

            return mesh;
        }
    }
    // Start is called before the first frame update
    private void Start()
    {
        bounds = new Bounds(Vector3.zero, new Vector3(30, 30, 30));
        blade = Blade;

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

            //TODO: set wind using wind direction, speed and noise scale
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

    private void InitShader()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Bounds bounds = mf.sharedMesh.bounds;

        Vector3 blades = bounds.extents;
        Vector3 vec = transform.localScale / 0.1f * density;
        blades.x *= vec.x;
        blades.z *= vec.z;

        int total = (int)blades.x * (int)blades.z * 20;

        kernelBendGrass = shader.FindKernel("BendGrass");

        shader.GetKernelThreadGroupSizes(kernelBendGrass, out uint threadGroupSize, out _, out _);
        groupSize = Mathf.CeilToInt((float)total / threadGroupSize);
        int count = groupSize * (int)threadGroupSize;

        bladesArray = new GrassBlade[count];

        for (int i = 0; i < count; i++)
        {
            var pos = new Vector3
            {
                x = Random.Range(-1f, 1f) * bounds.extents.x + bounds.center.x,
                y = 0,
                z = Random.Range(-1f, 1f) * bounds.extents.z + bounds.center.z
            };
            pos = transform.TransformPoint(pos);
            bladesArray[i] = new GrassBlade(pos);
        }

        bladesBuffer = new ComputeBuffer(count, SIZE_GRASS_BLADE);
        bladesBuffer.SetData(bladesArray);

        shader.SetBuffer(kernelBendGrass, "bladesBuffer", bladesBuffer);
        shader.SetFloat("maxBend", maxBend * Mathf.Deg2Rad);
        //TODO: set wind using wind direction, speed and noise scale
        var wind = new Vector4
        {
            x = Mathf.Cos(windDirection * Mathf.Deg2Rad),
            y = Mathf.Sin(windDirection * Mathf.Deg2Rad),
            z = windSpeed,
            w = windScale
        };
        shader.SetVector("wind", wind);

        timeID = Shader.PropertyToID("time");

        argsArray[0] = blade.GetIndexCount(0);
        argsArray[1] = (uint)count;
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        material.SetBuffer("bladesBuffer", bladesBuffer);
        material.SetFloat("_Scale", scale);
    }

    private void Update()
    {
        shader.SetFloat(timeID, Time.time);
        shader.Dispatch(kernelBendGrass, groupSize, 1, 1);

        if (!viewNoise)
        {
            Graphics.DrawMeshInstancedIndirect(blade, 0, material, bounds, argsBuffer);
        }
    }

    private void OnDestroy()
    {
        bladesBuffer.Release();
        argsBuffer.Release();
    }
}
