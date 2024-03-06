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
}
#pragma warning restore CS0162 // Unreachable code detected
