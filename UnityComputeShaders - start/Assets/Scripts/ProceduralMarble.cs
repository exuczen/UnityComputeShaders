using UnityEngine;
using System.Collections;

public class ProceduralMarble : MonoBehaviour
{
    public ComputeShader shader;
    public int texResolution = 256;

    private Renderer rend = null;
    private RenderTexture outputTexture = null;

    private int kernelHandle;
    private bool marble = true;

    // Use this for initialization
    private void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0)
        {
            enableRandomWrite = true
        };
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitShader();
    }

    private void InitShader()
    {
        kernelHandle = shader.FindKernel("CSMain");

        shader.SetInt("TexResolution", texResolution);
        shader.SetTexture(kernelHandle, "Result", outputTexture);

        rend.material.SetTexture("_MainTex", outputTexture);

        shader.SetBool("Marble", marble);
        marble = !marble;

        DispatchShader(texResolution / 8, texResolution / 8);
    }

    private void DispatchShader(int x, int y)
    {
        shader.Dispatch(kernelHandle, x, y, 1);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.U))
        {
            shader.SetBool("Marble", marble);
            marble = !marble;
        }
        shader.SetFloat("Time", Time.time);
        DispatchShader(texResolution / 8, texResolution / 8);
    }
}
