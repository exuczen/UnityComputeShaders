using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelizeMesh : MonoBehaviour
{
    public Mesh meshToVoxelize;
    public int yParticleCount = 4;
    public int layer = 9;

    private float particleSize = 0;

    public float ParticleSize
    {
        get
        {
            return particleSize;
        }
    }

    private readonly List<Vector3> positions = new();

    public List<Vector3> PositionList
    {
        get
        {
            return positions;
        }
    }

    public void Voxelize(Mesh mesh)
    {
        var go = new GameObject
        {
            layer = layer
        };
        var meshFilter = go.AddComponent<MeshFilter>();
        var collider = go.AddComponent<MeshCollider>();
        meshFilter.sharedMesh = mesh;
        collider.sharedMesh = mesh;

        var minExtents = mesh.bounds.min;
        var maxExtents = mesh.bounds.max;

        float radius = mesh.bounds.extents.y / yParticleCount;
        particleSize = radius * 2f;

        var rayOffset = minExtents;
        var counts = mesh.bounds.extents / radius;
        var particleCounts = new Vector3Int((int)counts.x, (int)counts.y, (int)counts.z);

        minExtents.x += particleCounts.x % 2 == 0 ? mesh.bounds.extents.x - particleCounts.x * radius : 0f;
        //float offsetZ = particleCounts.z % 2 == 0 ? mesh.bounds.extents.z - particleCounts.z * radius : 0f;
        rayOffset.y += radius;

        int layerMask = 1 << layer;

        while (rayOffset.y < maxExtents.y)
        {
            rayOffset.x = minExtents.x;
            while (rayOffset.x < maxExtents.x)
            {
                var rayOrigin = go.transform.position + rayOffset;

                if (Physics.Raycast(rayOrigin, Vector3.forward, out var hit, 100f, layerMask))
                {
                    var frontPt = hit.point;
                    rayOrigin.z += maxExtents.z * 2f;

                    if (Physics.Raycast(rayOrigin, Vector3.back, out hit, 100f, layerMask))
                    {
                        var backPt = hit.point;
                        int n = Mathf.CeilToInt(frontPt.z / particleSize);
                        frontPt.z = n * particleSize;
                        while (frontPt.z < backPt.z)
                        {
                            float gap = backPt.z - frontPt.z;
                            if (gap < radius * 0.5f)
                            {
                                break;
                            }
                            positions.Add(frontPt);
                            frontPt.z += particleSize;
                        }
                    }
                }
                rayOffset.x += particleSize;
            }
            rayOffset.y += particleSize;
        }
    }
}
