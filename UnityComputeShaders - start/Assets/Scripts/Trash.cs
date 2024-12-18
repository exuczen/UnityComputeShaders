﻿using MustHave;
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
            //Stopwatch sw = Stopwatch.StartNew();
            //int n = 1000000;
            //for (int i = 0; i < n; i++)
            //{
            //    float x = Mathf.Clamp01((((i >> 0) & 0xff) + 0.5f) / 255f);
            //    float y = Mathf.Clamp01((((i >> 8) & 0xff) + 0.5f) / 255f);
            //    float z = Mathf.Clamp01((((i >> 16) & 0xff) + 0.5f) / 255f);
            //
            //    int X = (int)(x * 255f) << 0;
            //    int Y = (int)(y * 255f) << 8;
            //    int Z = (int)(z * 255f) << 16;
            //
            //    int index = X | Y | Z;
            //
            //    if (index != i)
            //    {
            //        Debug.LogWarning($"{GetType().Name}.:   {i}  != {index}");
            //    }
            //}
            //Debug.Log($"{GetType().Name}.: Finish: {n} {sw.Elapsed.Milliseconds} {0xff}");

            //float4 indexColor = indexTexture[p.position];
            //if (indexColor.w > 0.0)
            //{
            //    int x = (int)(indexColor.x * 255) << 0;
            //    int y = (int)(indexColor.y * 255) << 8;
            //    int z = (int)(indexColor.z * 255) << 16;
            //    int j = x | y | z;
            //    if (i != j)
            //    {
            //        particlesBuffer[j].position = int2(-1, -1);
            //        particlesBuffer[j].endTime = 0.0;
            //        particlesBuffer[j].indexColor = 0.0;
            //    }
            //}
            //float x = (((i >> 0) & 0xff) + 0.5) / 255.0;
            //float y = (((i >> 8) & 0xff) + 0.5) / 255.0;
            //float z = (((i >> 16) & 0xff) + 0.5) / 255.0;
            //p.indexColor = float4(x, y, z, 1.0);
        }
    }

    private void Update()
    {
        if (false)
        {
            Debug.Log($"{GetType().Name}.{Noise1(Vector3.one * Time.time)}");
        }
    }

    private float Hash(float n)
    {
        return Frac(Mathf.Sin(n) * 43758.5453f);
    }

    private float Lerp(float a, float b, float t)
    {
        return Mathf.Lerp(a, b, t);
    }

    private float Frac(float x)
    {
        return x - Mathf.Floor(x);
    }

    private float Noise1(Vector3 v)
    {
        Vector3 p = new(Mathf.Floor(v.x), Mathf.Floor(v.y), Mathf.Floor(v.z));
        Vector3 f = new(Frac(v.x), Frac(v.y), Frac(v.z));
        //f = f * f * (3.0 - 2.0 * f);
        f = Mathv.Mul(Mathv.Mul(f, f), 3.0f * Vector3.one - 2.0f * f);
        float n = p.x + p.y * 57.0f + 113.0f * p.z;

        return Lerp(Lerp(Lerp(Hash(n + 0.0f), Hash(n + 1.0f), f.x),
                         Lerp(Hash(n + 57.0f), Hash(n + 58.0f), f.x), f.y),
                    Lerp(Lerp(Hash(n + 113.0f), Hash(n + 114.0f), f.x),
                         Lerp(Hash(n + 170.0f), Hash(n + 171.0f), f.x), f.y), f.z);
    }

    /*
    private readonly int[] indexArray = new int[TexResolution * TexResolution];
    private Texture2D indexTexture2D = null;

    private void CreateTextures()
    {
        indexTexture2D = new Texture2D(indexTexture.width, indexTexture.height);
    }

    private void DispatchKernels()
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
