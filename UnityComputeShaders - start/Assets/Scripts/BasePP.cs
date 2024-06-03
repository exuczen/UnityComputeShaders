using MustHave;
using UnityEngine;
using UnityEngine.Rendering;

public class BasePP : ComputeShaderPostProcess
{
    protected void SetShaderVectorCenter(CommandBuffer cmd, Transform trackedObject)
    {
        if (trackedObject && thisCamera)
        {
            Vector2 center = thisCamera.WorldToScreenPoint(trackedObject.position);
            cmd.SetComputeVectorParam(shader, "center", center);
        }
    }

    protected void SetShaderVectorCenter(Transform trackedObject)
    {
        if (trackedObject && thisCamera)
        {
            Vector2 center = thisCamera.WorldToScreenPoint(trackedObject.position);
            shader.SetVector("center", center);
        }
    }

    protected void SetShaderFloatTime(CommandBuffer cmd)
    {
        cmd.SetComputeFloatParam(shader, "time", Time.time);
    }

    protected void SetShaderFloatTime()
    {
        shader.SetFloat("time", Time.time);
    }
}
