using System;
using UnityEngine;

public class SmoothFollowTarget : MonoBehaviour
{
    public GameObject target;
    public float[] limitsX;

    private Vector3 offset;

    private void Start()
    {
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player");
        }
        offset = transform.position - target.transform.position;
    }

    private void LateUpdate()
    {
        Vector3 pos = target.transform.position + offset;
        if (limitsX != null && limitsX.Length == 2)
        {
            pos.x = Mathf.Clamp(pos.x, limitsX[0], limitsX[1]);
        }
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * 5);
        transform.LookAt(target.transform);
        return;
    }
}
