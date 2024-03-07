using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

#pragma warning disable CS0162 // Unreachable code detected
public class Trash : MonoBehaviour
{
    private void Start()
    {
        if (false)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int n = 1000000;
            for (int i = 0; i < n; i++)
            {
                float x = Mathf.Clamp01((((i >> 0) & 0xff) + 0.5f) / 255f);
                float y = Mathf.Clamp01((((i >> 8) & 0xff) + 0.5f) / 255f);
                float z = Mathf.Clamp01((((i >> 16) & 0xff) + 0.5f) / 255f);

                int X = (int)(x * 255f) << 0;
                int Y = (int)(y * 255f) << 8;
                int Z = (int)(z * 255f) << 16;

                int index = X | Y | Z;

                if (index != i)
                {
                    Debug.LogWarning($"{GetType().Name}.:   {i}  != {index}");
                }
            }
            Debug.Log($"{GetType().Name}.: Finish: {n} {sw.Elapsed.Milliseconds} {0xff}");
        }
    }

    /*
    private readonly int[] indexArray = new int[TexResolution * TexResolution];
    private Texture2D indexTexture2D = null;

    void CreateTextures()
    {
        indexTexture2D = new Texture2D(indexTexture.width, indexTexture.height);
    }

    void DispatchKernels()
    {
        //RenderTexture.active = outputTexture;
        //indexTexture2D.ReadPixels(new Rect(0, 0, indexTexture.width, indexTexture.height), 0, 0);
        //var pixels = indexTexture2D.GetRawTextureData<Color32>();

        indexBuffer.GetData(indexArray);
        for (int y = 1; y < TexResolution; y++)
        {
            int yOffset = y * TexResolution;
            for (int x = 1; x < TexResolution; x++)
            {
                int i = x + yOffset;
                int index = indexArray[i];
            }
        }
    }
    */
}
#pragma warning restore CS0162 // Unreachable code detected
