using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0649

public class QuadParticles : MonoBehaviour
{
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
    }

    struct Vertex
    {
        public Vector3 position;
        public Vector2 uv;
        public float life;
    }

    const int SIZE_PARTICLE = 7 * sizeof(float);
    const int SIZE_VERTEX = 6 * sizeof(float);

    public int particleCount = 10000;
    public Material material;
    public ComputeShader shader;
    [Range(0.01f, 1.0f)]
    public float quadSize = 0.1f;

    private int numParticles;
    private int numVerticesInMesh;
    private int kernelID;
    private ComputeBuffer particleBuffer = null;
    private ComputeBuffer vertexBuffer = null;

    private int groupSizeX;

    private Vector3 cursorPosition;

    // Use this for initialization
    private void Start()
    {
        Init();
    }

    private void Init()
    {
        GetCursorPosition();

        // find the id of the kernel
        kernelID = shader.FindKernel("CSMain");

        shader.GetKernelThreadGroupSizes(kernelID, out uint threadsX, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)particleCount / (float)threadsX);
        numParticles = groupSizeX * (int)threadsX;

        // initialize the particles
        Particle[] particleArray = new Particle[numParticles];

        int numVertices = numParticles * 6;
        var vertexArray = new Vertex[numVertices];

        int index;

        for (int i = 0; i < numParticles; i++)
        {
            var pos = new Vector3
            {
                x = Random.Range(-1f, 1f),
                y = Random.Range(-1f, 1f),
                z = Random.Range(-1f, 1f)
            };
            pos = Random.Range(0f, 0.5f) * pos.normalized;
            pos.z += cursorPosition.z;
            particleArray[i].position = pos;
            particleArray[i].velocity.Set(0, 0, 0);

            // Initial life value
            particleArray[i].life = Random.value * 5.0f + 1.0f;

            index = i * 6;
            vertexArray[index + 0].uv.Set(0, 0);
            vertexArray[index + 1].uv.Set(0, 1);
            vertexArray[index + 2].uv.Set(1, 1);
            vertexArray[index + 3].uv.Set(0, 0);
            vertexArray[index + 4].uv.Set(1, 1);
            vertexArray[index + 5].uv.Set(1, 0);
        }

        // create compute buffers
        particleBuffer = new ComputeBuffer(numParticles, SIZE_PARTICLE);
        particleBuffer.SetData(particleArray);

        vertexBuffer = new ComputeBuffer(numVertices, SIZE_VERTEX);
        vertexBuffer.SetData(vertexArray);

        // bind the compute buffers to the shader and the compute shader
        shader.SetBuffer(kernelID, "particleBuffer", particleBuffer);
        shader.SetBuffer(kernelID, "vertexBuffer", vertexBuffer);

        shader.SetFloat("halfSize", quadSize * 0.5f);

        material.SetBuffer("vertexBuffer", vertexBuffer);
    }

    private void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, numParticles);
    }

    private void OnDestroy()
    {
        particleBuffer?.Release();
        vertexBuffer?.Release();
    }

    // Update is called once per frame
    private void Update()
    {
        // Send datas to the compute shader
        shader.SetFloat("deltaTime", Time.deltaTime);
        shader.SetVector("mousePosition", cursorPosition);

        // Update the Particles
        shader.Dispatch(kernelID, groupSizeX, 1, 1);
    }

    private void OnGUI()
    {
        GetCursorPosition();
    }

    private Vector3 GetCursorPosition()
    {
        Camera c = Camera.main;
        Event e = Event.current;
        // Get the mouse position from Event.
        // Note that the y position from Event is inverted.
        Vector2 mousePos = e != null ? new(e.mousePosition.x, c.pixelHeight - e.mousePosition.y) : Input.mousePosition;
        cursorPosition = c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, c.nearClipPlane + 10));
        return cursorPosition;
    }
}
