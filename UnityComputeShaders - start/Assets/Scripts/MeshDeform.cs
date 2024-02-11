using UnityEngine;
using System.Collections;


public class MeshDeform : MonoBehaviour
{
    public ComputeShader shader;
    [Range(0.5f, 2.0f)]
    public float radius;

    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;

        public Vertex(Vector3 position, Vector3 normal)
        {
            this.position = position;
            this.normal = normal;
        }
    }

    private int kernelHandle;
    private Mesh mesh = null;
    private Vertex[] vertexArray = null;
    private Vertex[] initialArray = null;
    private ComputeBuffer vertexBuffer = null;
    private ComputeBuffer initialBuffer = null;

    // Use this for initialization
    private void Start()
    {
        if (InitData())
        {
            InitShader();
        }
    }

    private bool InitData()
    {
        kernelHandle = shader.FindKernel("CSMain");

        if (!TryGetComponent<MeshFilter>(out var mf))
        {
            Debug.Log("No MeshFilter found");
            return false;
        }

        InitVertexArrays(mf.mesh);
        InitGPUBuffers();

        mesh = mf.mesh;

        return true;
    }

    private void InitShader()
    {
        shader.SetFloat("radius", radius);
    }

    private void InitVertexArrays(Mesh mesh)
    {
        vertexArray = new Vertex[mesh.vertices.Length];
        initialArray = new Vertex[mesh.vertices.Length];

        for (int i = 0; i < vertexArray.Length; i++)
        {
            var v = new Vertex(mesh.vertices[i], mesh.normals[i]);
            vertexArray[i] = v;
            initialArray[i] = v;
        }
    }

    private void InitGPUBuffers()
    {
        vertexBuffer = new ComputeBuffer(vertexArray.Length, sizeof(float) * 6);
        vertexBuffer.SetData(vertexArray);

        initialBuffer = new ComputeBuffer(initialArray.Length, sizeof(float) * 6);
        initialBuffer.SetData(initialArray);

        shader.SetBuffer(kernelHandle, "vertexBuffer", vertexBuffer);
        shader.SetBuffer(kernelHandle, "initialBuffer", initialBuffer);
    }

    private void GetVerticesFromGPU()
    {
        vertexBuffer.GetData(vertexArray);

        var vertices = new Vector3[vertexArray.Length];
        var normals = new Vector3[vertexArray.Length];

        for (int i = 0; i < vertexArray.Length; i++)
        {
            vertices[i] = vertexArray[i].position;
            normals[i] = vertexArray[i].normal;
        }
        mesh.vertices = vertices;
        mesh.normals = normals;
    }

    private void Update()
    {
        if (shader)
        {
            shader.SetFloat("radius", radius);
            float delta = (Mathf.Sin(Time.time) + 1) / 2;
            shader.SetFloat("delta", delta);
            shader.Dispatch(kernelHandle, vertexArray.Length, 1, 1);

            GetVerticesFromGPU();
        }
    }

    private void OnDestroy()
    {
        vertexBuffer.Dispose();
        initialBuffer.Dispose();
    }
}

