using MustHave;
using UnityEngine;

public class FollowTargetAtDistance : MonoBehaviour
{
    [SerializeField]
    private Transform target = null;

    [SerializeField, Min(0f)]
    private float distance = 10f;

    [SerializeField]
    private Vector3 eulerAngles = Vector3.zero;

    private void OnValidate()
    {
        if (!target)
        {
            return;
        }
        eulerAngles = Maths.AnglesModulo360(eulerAngles);

        if (!Application.isPlaying)
        {
            UpdateTransform();
        }
    }

    private void LateUpdate()
    {
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        var targetRotation = target.rotation;
        var destRotation = Quaternion.Euler(eulerAngles);
        target.rotation = destRotation;
        var destDirection = -target.transform.forward;
        var destPosition = target.position + destDirection * distance;
        target.rotation = targetRotation;

        Vector3 position;
        Quaternion rotation;
        if (Application.isPlaying)
        {
            float transition = Time.deltaTime * 5;
            position = Vector3.Lerp(transform.position, destPosition, transition);
            rotation = Quaternion.Slerp(transform.rotation, destRotation, transition);
        }
        else
        {
            position = destPosition;
            rotation = destRotation;
        }
        transform.SetPositionAndRotation(position, rotation);
    }
}
