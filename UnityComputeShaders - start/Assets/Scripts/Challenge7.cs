﻿// StableFluids - Smoke

using UnityEngine;


public class Challenge7 : MonoBehaviour
{
    public int resolution = 512;
    public float viscosity = 1e-6f;
    public float force = 300;
    public float exponent = 200;
    public ComputeShader compute;
    public Shader shader;
    public Vector2 forceOrigin;
    public Vector2 forceVector;

    private Material material;

    private int kernelAdvect;
    private int kernelForce;
    private int kernelProjectSetup;
    private int kernelProject;
    private int kernelDiffuse1;
    private int kernelDiffuse2;

    private int ThreadCountX => (resolution + 7) / 8;
    private int ThreadCountY => (resolution * Screen.height / Screen.width + 7) / 8;

    private int ResolutionX => ThreadCountX * 8;
    private int ResolutionY => ThreadCountY * 8;

    // Vector field buffers
    private RenderTexture vfbRTV1;
    private RenderTexture vfbRTV2;
    private RenderTexture vfbRTV3;
    private RenderTexture vfbRTP1;
    private RenderTexture vfbRTP2;

    // Color buffers (for double buffering)
    private RenderTexture colorRT1;
    private RenderTexture colorRT2;

    private RenderTexture CreateRenderTexture(int componentCount, int width = 0, int height = 0)
    {
        var format = RenderTextureFormat.ARGBHalf;
        if (componentCount == 1) format = RenderTextureFormat.RHalf;
        if (componentCount == 2) format = RenderTextureFormat.RGHalf;

        if (width == 0) width = ResolutionX;
        if (height == 0) height = ResolutionY;

        var rt = new RenderTexture(width, height, 0, format)
        {
            enableRandomWrite = true
        };
        rt.Create();
        return rt;
    }


    private void OnValidate()
    {
        resolution = Mathf.Max(resolution, 8);
    }

    private void Start()
    {
        material = new Material(shader);

        InitBuffers();
        InitShader();
    }

    private void InitBuffers()
    {
        vfbRTV1 = CreateRenderTexture(2);
        vfbRTV2 = CreateRenderTexture(2);
        vfbRTV3 = CreateRenderTexture(2);
        vfbRTP1 = CreateRenderTexture(1);
        vfbRTP2 = CreateRenderTexture(1);

        colorRT1 = CreateRenderTexture(4, Screen.width, Screen.height);
        colorRT2 = CreateRenderTexture(4, Screen.width, Screen.height);
    }

    private void InitShader()
    {
        kernelAdvect = compute.FindKernel("Advect");
        kernelForce = compute.FindKernel("Force");
        kernelProjectSetup = compute.FindKernel("ProjectSetup");
        kernelProject = compute.FindKernel("Project");
        kernelDiffuse1 = compute.FindKernel("Diffuse1");
        kernelDiffuse2 = compute.FindKernel("Diffuse2");

        compute.SetTexture(kernelAdvect, "U_in", vfbRTV1);
        compute.SetTexture(kernelAdvect, "W_out", vfbRTV2);

        compute.SetTexture(kernelDiffuse2, "B2_in", vfbRTV1);

        compute.SetTexture(kernelForce, "W_in", vfbRTV2);
        compute.SetTexture(kernelForce, "W_out", vfbRTV3);

        compute.SetTexture(kernelProjectSetup, "W_in", vfbRTV3);
        compute.SetTexture(kernelProjectSetup, "DivW_out", vfbRTV2);
        compute.SetTexture(kernelProjectSetup, "P_out", vfbRTP1);

        compute.SetTexture(kernelDiffuse1, "B1_in", vfbRTV2);

        compute.SetTexture(kernelProject, "W_in", vfbRTV3);
        compute.SetTexture(kernelProject, "P_in", vfbRTP1);
        compute.SetTexture(kernelProject, "U_out", vfbRTV1);
        compute.SetFloat("ForceExponent", exponent);

        //TODO: 1 - Setup the correct force origin.
        //The StableFluids.compute shader wants the input to have the origin at the centre of the quad.
        //The public property forceOrigin has uv coordinates, with the origin at bottom left
        var inputForceOrigin = forceOrigin - 0.5f * Vector2.one;
        compute.SetVector("ForceOrigin", inputForceOrigin);

        material.SetVector("_ForceOrigin", inputForceOrigin);
        material.SetFloat("_ForceExponent", exponent);
        material.SetTexture("_VelocityField", vfbRTV1);

        //TODO: 2 - Get the material attached to this object and set colorRT1 as its _MainTex property
        var renderer = GetComponent<MeshRenderer>();
        renderer.material.SetTexture("_MainTex", colorRT1);
    }

    private void OnDestroy()
    {
        Destroy(vfbRTV1);
        Destroy(vfbRTV2);
        Destroy(vfbRTV3);
        Destroy(vfbRTP1);
        Destroy(vfbRTP2);

        Destroy(colorRT1);
        Destroy(colorRT2);
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        var dx = 1.0f / ResolutionY;

        // Common variables
        compute.SetFloat("Time", Time.time);
        compute.SetFloat("DeltaTime", dt);

        // Advection
        compute.Dispatch(kernelAdvect, ThreadCountX, ThreadCountY, 1);

        // Diffuse setup
        var difalpha = dx * dx / (viscosity * dt);
        compute.SetFloat("Alpha", difalpha);
        compute.SetFloat("Beta", 4 + difalpha);
        Graphics.CopyTexture(vfbRTV2, vfbRTV1);

        // Jacobi iteration
        for (var i = 0; i < 20; i++)
        {
            compute.SetTexture(kernelDiffuse2, "X2_in", vfbRTV2);
            compute.SetTexture(kernelDiffuse2, "X2_out", vfbRTV3);
            compute.Dispatch(kernelDiffuse2, ThreadCountX, ThreadCountY, 1);

            compute.SetTexture(kernelDiffuse2, "X2_in", vfbRTV3);
            compute.SetTexture(kernelDiffuse2, "X2_out", vfbRTV2);
            compute.Dispatch(kernelDiffuse2, ThreadCountX, ThreadCountY, 1);
        }

        //TODO: 3 - Add random vector to the forceVector
        var deltaForce = new Vector2
        {
            x = Random.Range(-10f, 10f),
            y = Random.Range(-3f, 3f)
        };
        compute.SetVector("ForceVector", forceVector * Random.value + deltaForce);

        // Add external force
        compute.Dispatch(kernelForce, ThreadCountX, ThreadCountY, 1);

        // Projection setup
        compute.Dispatch(kernelProjectSetup, ThreadCountX, ThreadCountY, 1);

        // Jacobi iteration
        compute.SetFloat("Alpha", -dx * dx);
        compute.SetFloat("Beta", 4);

        for (var i = 0; i < 20; i++)
        {
            compute.SetTexture(kernelDiffuse1, "X1_in", vfbRTP1);
            compute.SetTexture(kernelDiffuse1, "X1_out", vfbRTP2);
            compute.Dispatch(kernelDiffuse1, ThreadCountX, ThreadCountY, 1);

            compute.SetTexture(kernelDiffuse1, "X1_in", vfbRTP2);
            compute.SetTexture(kernelDiffuse1, "X1_out", vfbRTP1);
            compute.Dispatch(kernelDiffuse1, ThreadCountX, ThreadCountY, 1);
        }

        // Projection finish
        compute.Dispatch(kernelProject, ThreadCountX, ThreadCountY, 1);

        // Apply the velocity field to the color buffer.
        Graphics.Blit(colorRT1, colorRT2, material, 0);

        // Swap the color buffers.
        (colorRT2, colorRT1) = (colorRT1, colorRT2);
    }
}
