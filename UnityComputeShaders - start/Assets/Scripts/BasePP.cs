using MustHave;
using UnityEngine;
using UnityEngine.Rendering;

public class BasePP : ComputeShaderPostProcess
{
    protected void SetShaderVectorCenter(Transform trackedObject)
    {
        if (trackedObject && thisCamera)
        {
            Vector2 center = thisCamera.WorldToScreenPoint(trackedObject.position);

            shader.SetVector("center", center);
        }
    }

    protected void SetShaderFloatTime()
    {
        shader.SetFloat("time", Time.time);
    }
}
