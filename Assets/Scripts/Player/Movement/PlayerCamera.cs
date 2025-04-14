using UnityEngine;

public struct CameraInput
{
    public Vector2 Look;
}

public class PlayerCamera : MonoBehaviour
{
    private Vector3 _eulerAngles;

    [SerializeField]
    private float sensitivity = 1;

	[SerializeField, Range(0, 180)]
	private float maxXAngle;

    public void Initialize(Transform cameraTarget)
    {
        transform.position = cameraTarget.position;
        _eulerAngles = transform.eulerAngles = cameraTarget.eulerAngles;
    }

    public void UpdateRotation(CameraInput input)
    {
        _eulerAngles += new Vector3(-input.Look.y, input.Look.x);

		if (_eulerAngles.x > 180f) _eulerAngles.x -= 360f;

		_eulerAngles.x = Mathf.Clamp(_eulerAngles.x, -maxXAngle, maxXAngle);

        transform.eulerAngles = _eulerAngles * sensitivity;
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }
}
