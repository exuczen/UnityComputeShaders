// StableFluids - A GPU implementation of Jos Stam's Stable Fluids on Unity
// adapted from https://github.com/keijiro/StableFluids

using UnityEngine;


public class StableFluids : MonoBehaviour
{
    public Texture2D initial;
    public ComputeShader compute;
    public Material material;

    public int resolution = 512;
    public float viscosity = 1e-6f;
    public float force = 300;
    public float exponent = 200;

    private Vector2 previousInput;

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
        RenderTextureFormat format;
        if (componentCount == 1)
        {
            format = RenderTextureFormat.RHalf;
        }
        else if (componentCount == 2)
        {
            format = RenderTextureFormat.RGHalf;
        }
        else
        {
            format = RenderTextureFormat.ARGBHalf;
        }
        if (width <= 0)
        {
            width = ResolutionX;
        }
        if (height <= 0)
        {
            height = ResolutionY;
        };
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
        InitBuffers();
        InitShader();

        Graphics.Blit(initial, colorRT1);
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

        material.SetFloat("_ForceExponent", exponent);
        material.SetTexture("_VelocityField", vfbRTV1);
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
        float dt = Time.deltaTime;
        float dx = 1.0f / ResolutionY;

        // Input point
        var input = new Vector2(
            (Input.mousePosition.x - Screen.width * 0.5f) / Screen.height,
            (Input.mousePosition.y - Screen.height * 0.5f) / Screen.height
        );

        // Common variables
        compute.SetFloat("Time", Time.time);
        compute.SetFloat("DeltaTime", dt);

        //Add code here
        compute.Dispatch(kernelAdvect, ThreadCountX, ThreadCountY, 1);

        float difalpha = dx * dx / (viscosity * dt);
        compute.SetFloat("Alpha", difalpha);
        compute.SetFloat("Beta", 4 + difalpha);
        Graphics.CopyTexture(vfbRTV2, vfbRTV1);

        for (int i = 0; i < 20; i++)
        {
            compute.SetTexture(kernelDiffuse2, "X2_in", vfbRTV2);
            compute.SetTexture(kernelDiffuse2, "X2_out", vfbRTV3);
            compute.Dispatch(kernelDiffuse2, ThreadCountX, ThreadCountY, 1);

            compute.SetTexture(kernelDiffuse2, "X2_in", vfbRTV3);
            compute.SetTexture(kernelDiffuse2, "X2_out", vfbRTV2);
            compute.Dispatch(kernelDiffuse2, ThreadCountX, ThreadCountY, 1);
        }

        compute.SetVector("ForceOrigin", input);

        if (Input.GetMouseButton(1))
        {
            compute.SetVector("ForceVector", 0.025f * force * Random.insideUnitCircle);
        }
        else if (Input.GetMouseButton(0))
        {
            compute.SetVector("ForceVector", force * (input - previousInput));
        }
        else
        {
            compute.SetVector("ForceVector", Vector4.zero);
        }
        compute.Dispatch(kernelForce, ThreadCountX, ThreadCountY, 1);

        compute.Dispatch(kernelProjectSetup, ThreadCountX, ThreadCountY, 1);

        compute.SetFloat("Alpha", -dx * dx);
        compute.SetFloat("Beta", 4);

        for (int i = 0; i < 20; i++)
        {
            compute.SetTexture(kernelDiffuse1, "X1_in", vfbRTP1);
            compute.SetTexture(kernelDiffuse1, "X1_out", vfbRTP2);
            compute.Dispatch(kernelDiffuse1, ThreadCountX, ThreadCountY, 1);

            compute.SetTexture(kernelDiffuse1, "X1_in", vfbRTP2);
            compute.SetTexture(kernelDiffuse1, "X1_out", vfbRTP1);
            compute.Dispatch(kernelDiffuse1, ThreadCountX, ThreadCountY, 1);
        }

        compute.Dispatch(kernelProject, ThreadCountX, ThreadCountY, 1);

        var offs = Vector2.one * (Input.GetMouseButton(1) ? 0 : 1e+7f);
        material.SetVector("_ForceOrigin", input + offs);
        Graphics.Blit(colorRT1, colorRT2, material, 0);

        (colorRT2, colorRT1) = (colorRT1, colorRT2);

        previousInput = input;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //Graphics.Blit(vfbRTV3, destination, material, 1);
        //Graphics.Blit(colorRT1, destination, material, 0);
        Graphics.Blit(colorRT1, destination, material, 1);
    }
}
