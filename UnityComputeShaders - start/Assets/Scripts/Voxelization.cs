using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxelization : MonoBehaviour
{
    private void Start()
    {
        var voxelizeMesh = GetComponent<VoxelizeMesh>();
        voxelizeMesh.Voxelize(voxelizeMesh.meshToVoxelize);
        float pS = voxelizeMesh.ParticleSize;
        var scale = new Vector3(pS, pS, pS);

        for (int i = 0; i < voxelizeMesh.PositionList.Count; i++)
        {
            var pos = voxelizeMesh.PositionList[i];
            var particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.transform.position = pos;
            particle.transform.localScale = scale;
            particle.transform.parent = gameObject.transform;
        }
    }
}
