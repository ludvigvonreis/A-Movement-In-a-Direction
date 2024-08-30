using UnityEngine;

public struct CameraInput
{
    public Vector2 Look;
}

public class PlayerCamera : MonoBehaviour
{
    private Vector3 _eulerAngels;

    [SerializeField]
    private float sensitivity = 12;

    public void Initialize(Transform cameraTarget)
    {
        transform.position = cameraTarget.position;
        _eulerAngels = transform.eulerAngles = cameraTarget.eulerAngles;
    }

    public void UpdateRotation(CameraInput input)
    {
        _eulerAngels += new Vector3(-input.Look.y, input.Look.x);
        transform.eulerAngles = _eulerAngels * sensitivity;
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }
}
