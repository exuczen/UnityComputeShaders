using UnityEditor;
using UnityEngine;
using MustHave.Utils;
using MustHave;
using log4net.Util;

[CustomEditor(typeof(VolumeCrossSection))]
public class VolumeCrossSectionEditor : Editor
{
    private void OnEnable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    private void OnSceneGUI()
    {
        //DuringSceneGUI(SceneView.lastActiveSceneView);
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        VolumeCrossSection crossSection = target as VolumeCrossSection;

        bool selected = Selection.activeGameObject == crossSection.gameObject;

        Event currEvent = Event.current;

        if (selected && currEvent.type != EventType.Used)
        {
            var cameraTransform = sceneView.camera.transform;
            var transform = crossSection.transform;

            bool mouseButtonPressed = currEvent.button > 0;

            //Debug.Log($"{GetType().Name}.{currEvent.shift} | {currEvent.control}");
            if (currEvent.isMouse && currEvent.shift && !mouseButtonPressed)
            {
                var mouseDelta = -currEvent.delta;

                if (currEvent.control)
                {
                    var viewRay = transform.forward - Vector3.Dot(transform.forward, cameraTransform.forward) * cameraTransform.forward;
                    viewRay = cameraTransform.InverseTransformVector(viewRay);

                    float delta = mouseDelta.magnitude * transform.lossyScale.x * 0.002f;
                    mouseDelta.x *= -1f;
                    float sign = Mathf.Sign(Vector2.Dot(viewRay, mouseDelta));

                    transform.Translate(0f, 0f, sign * delta);
                }
                else
                {
                    mouseDelta *= 0.5f;

                    transform.Rotate(cameraTransform.right, mouseDelta.y, Space.World);
                    transform.Rotate(cameraTransform.up, mouseDelta.x, Space.World);
                }
            }
            if (currEvent.type == EventType.KeyDown && currEvent.keyCode == KeyCode.C)
            {
                transform.Reset(false);
            }
        }
    }
}
