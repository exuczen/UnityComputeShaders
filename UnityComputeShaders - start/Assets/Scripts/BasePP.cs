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

            if (HasCommandBuffer)
            {
                cmdBuffer.SetComputeVectorParam(shader, "center", center);
            }
            else
            {
                shader.SetVector("center", center);
            }
        }
    }

    protected void SetShaderFloatTime()
    {
        if (HasCommandBuffer)
        {
            cmdBuffer.SetComputeFloatParam(shader, "time", Time.time);
        }
        else
        {
            shader.SetFloat("time", Time.time);
        }
    }
}
