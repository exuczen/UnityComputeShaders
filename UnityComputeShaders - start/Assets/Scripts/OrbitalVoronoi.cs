using MustHave;
using MustHave.Utils;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

[ExecuteInEditMode]
public class OrbitalVoronoi : ComputeShaderBehaviour
{
    private const int TexResolution = 1 << 10;
    private const int ParticlesCapacity = 1 << 6;

    private readonly int ParticleSize = Marshal.SizeOf<Particle>();

    private enum Kernel
    {
        InitParticles,
        ComputeForces,
        UpdateParticles,
        PixelColors,
        NormalizeColors,
        DrawForces,
    }

    private readonly struct ShaderData
    {
        public static readonly int CursorPositionID = Shader.PropertyToID("CursorPosition");
        public static readonly int CursorStaticLerpID = Shader.PropertyToID("CursorStaticLerp");
        public static readonly int DeltaTimeID = Shader.PropertyToID("DeltaTime");
        public static readonly int ParticlesCountID = Shader.PropertyToID("ParticlesCount");
        public static readonly int BegParticleIndexID = Shader.PropertyToID("BegParticleIndex");
    }

    private struct Particle
    {
        public Vector2 repForce;
        public Vector2 position;

        public Particle(Vector2 position) : this()
        {
            this.position = position;
            repForce = Vector2.zero;
        }
    };

    private RenderTexture forcesTexture = null;
    private RenderTexture indexTexture = null;

    private ComputeBuffer particlesBuffer = null;

    private readonly Particle[] singleParticle = new Particle[1];

    private int particlesCount = 1; // ParticlesCapacity;

    private Vector3Int pixelThreadGroups = Vector3Int.one;
    private Vector3Int pairsThreadGroups = Vector3Int.one;
    private Vector3Int particleThreadGroups = Vector3Int.one;

    private Vector2 mousePositionPrev = default;
    private float cursorStaticStartTime = -1f;

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        UpdateCursor();
        UpdateAddParticle();
        DispatchKernels();
    }

    protected override void CreateTextures()
    {
        outputTexture = CreateTexture(TexResolution, TexResolution, FilterMode.Point);

        forcesTexture = new RenderTexture(ParticlesCapacity, ParticlesCapacity, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        forcesTexture.Create();

        indexTexture = new RenderTexture(outputTexture.descriptor)
        {
            format = RenderTextureFormat.RInt
        };
    }

    protected override void CreateComputeBuffers()
    {
        particlesBuffer = CreateAddComputeBuffer(ParticlesCapacity, ParticleSize);
    }

    protected override void InitOnStart()
    {
        if (Application.isPlaying)
        {
            FindKernels<Kernel>();
            GetThreadGroupSizes();
            InitShader();
        }
    }

    private void GetThreadGroupSizes()
    {
        var numthreads = GetKernelNumThreads(Kernel.PixelColors);
        pixelThreadGroups.x = GetThreadGroupCount(numthreads.x, TexResolution);
        pixelThreadGroups.y = GetThreadGroupCount(numthreads.y, TexResolution);
    }

    private void InitShader()
    {
        shader.SetInt("TexResolution", TexResolution);
        shader.SetFloat("TexelSize", 1f / TexResolution);

        ForEachKernel(kernelID => {
            shader.SetTexture(kernelID, "OutputTexture", outputTexture);
            shader.SetTexture(kernelID, "ForcesTexture", forcesTexture);
            shader.SetTexture(kernelID, "IndexTexture", indexTexture);
            shader.SetBuffer(kernelID, "ParticlesBuffer", particlesBuffer);
        });
        renderer.material.SetTexture("_MainTex", outputTexture);

        int particlesCount = this.particlesCount;

        SetParticlesCount(ParticlesCapacity);

        DispatchKernel(Kernel.InitParticles, particleThreadGroups);

        SetParticlesCount(this.particlesCount = particlesCount);

        SetCursorParticlePosition(0.5f * Vector2.one);
    }

    private void SetParticlesCount(int count)
    {
        particlesCount = count;

        var numThreads = GetKernelNumThreads(Kernel.InitParticles);
        particleThreadGroups.x = GetThreadGroupCount(numThreads.x, particlesCount);

        numThreads = GetKernelNumThreads(Kernel.ComputeForces);
        pairsThreadGroups.x = GetThreadGroupCount(numThreads.x, particlesCount);
        pairsThreadGroups.y = pairsThreadGroups.x;

        shader.SetInt(ShaderData.ParticlesCountID, particlesCount);
    }

    private void UpdateAddParticle()
    {
        if (Input.GetMouseButtonDown(0) && particlesCount < ParticlesCapacity)
        {
            var cursorPosition = GetCursorPosition(Input.mousePosition, false);
            if (cursorPosition.x >= 0f && cursorPosition.x < 1f &&
                cursorPosition.y >= 0f && cursorPosition.y < 1f)
            {
                Debug.Log($"{GetType().Name}.{cursorPosition:f2}");

                SetParticlePosition(cursorPosition, particlesCount++);
                SetParticlesCount(particlesCount);
            }
        }
    }

    private void UpdateCursor()
    {
        Vector2 mousePos = Input.mousePosition;

        bool cursorButtonDown = Input.GetMouseButton(1);

        if (cursorButtonDown)
        {
            SetCursorParticlePosition(GetCursorPosition(mousePos, false));
        }
        float cursorStaticLerp = GetCursorStaticLerp(mousePos, mousePositionPrev);
        shader.SetFloat(ShaderData.CursorStaticLerpID, cursorStaticLerp);
        shader.SetInt(ShaderData.BegParticleIndexID, cursorButtonDown ? 1 : 0);

        mousePositionPrev = mousePos;
    }

    private Vector2 GetCursorPosition(Vector2 mousePos, bool modulo)
    {
        float cursorPosY = mousePos.y / Screen.height;
        float offsetX = (Screen.width - Screen.height) * 0.5f;
        float cursorPosX = (mousePos.x - offsetX) / Screen.height;
        if (modulo)
        {
            cursorPosX = (1 + (cursorPosX % 1)) % 1;
            cursorPosY = (1 + (cursorPosY % 1)) % 1;
        }
        return new Vector2(cursorPosX, cursorPosY);
    }

    private void SetCursorParticlePosition(Vector2 pos)
    {
        shader.SetVector(ShaderData.CursorPositionID, pos);
        SetParticlePosition(pos, 0);
    }

    private void SetParticlePosition(Vector2 pos, int index)
    {
        particlesBuffer.GetData(singleParticle, 0, index, 1);
        singleParticle[0].position = pos;
        particlesBuffer.SetData(singleParticle, 0, index, 1);
    }

    private float GetCursorStaticLerp(Vector2 mousePos, Vector2 mousePosPrev)
    {
        float cursorStaticLerp = 0f;

        if ((mousePosPrev - mousePos).sqrMagnitude < 1f)
        {
            if (cursorStaticStartTime < 0f)
            {
                cursorStaticStartTime = Time.time;
            }
            else
            {
                cursorStaticLerp = Mathf.Clamp01(Time.time - cursorStaticStartTime);
            }
        }
        else
        {
            cursorStaticStartTime = -1f;
        }
        return Maths.GetTransition(TransitionType.SIN_IN_PI2_RANGE, cursorStaticLerp);
    }

    private void DispatchKernels()
    {
        //outputTexture.Clear();

        shader.SetFloat(ShaderData.DeltaTimeID, Time.deltaTime);

        DispatchKernel(Kernel.ComputeForces, pairsThreadGroups);
        DispatchKernel(Kernel.UpdateParticles, particleThreadGroups);
        DispatchKernel(Kernel.PixelColors, pixelThreadGroups);
    }
}
