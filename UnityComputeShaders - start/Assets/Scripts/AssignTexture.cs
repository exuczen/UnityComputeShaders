using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssignTexture : MonoBehaviour
{
    public ComputeShader shader = null;
    public int texResolution = 256;

    private new Renderer renderer = null;
    private RenderTexture outputTexture = null;
    private int kernelHandle;

    // Start is called before the first frame update
    private void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point
        };
        outputTexture.Create();

        renderer = GetComponent<Renderer>();
        renderer.enabled = true;

        InitShader();
    }

    private void InitShader()
    {
        kernelHandle = shader.FindKernel("CSMain");
        shader.SetTexture(kernelHandle, "Result", outputTexture);
        renderer.material.SetTexture("_MainTex", outputTexture);

        DispatchShader(texResolution >> 4, texResolution >> 4);
    }

    private void DispatchShader(int x, int y)
    {
        shader.Dispatch(kernelHandle, x, y, 1);
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.U))
        {
            Debug.Log($"{GetType().Name}.Update: KeyCode.U"); 
            DispatchShader(texResolution >> 3, texResolution >> 3);
        }
    }
}
