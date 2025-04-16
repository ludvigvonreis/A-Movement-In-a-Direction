using System.Collections;
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

	[SerializeField] private Transform shakeable;


	[Header("Shake Settings")]
	public float traumaDecay = 1.0f;
	public float maxShakeMagnitude = 1.0f;
	public float noiseFrequency = 25f;

	private float trauma = 0f;
	private float seed;

	private Vector3 initialPosition;
	private Vector3 initialRotation;

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


	public void Shake(float intensity)
	{
		trauma = Mathf.Clamp01(trauma + intensity);
	}

	void Update()
	{
		if (trauma <= 0f)
		{
			shakeable.SetLocalPositionAndRotation(
				initialPosition,
				Quaternion.Euler(initialRotation)
			);
			return;
		}

		float shakeAmount = trauma * trauma * maxShakeMagnitude;

		float time = Time.time * noiseFrequency;
		float x = (Mathf.PerlinNoise(seed, time) - 0.5f) * 2f;
		float y = (Mathf.PerlinNoise(seed + 1, time) - 0.5f) * 2f;
		float z = (Mathf.PerlinNoise(seed + 2, time) - 0.5f) * 2f;

		shakeable.SetLocalPositionAndRotation(
			initialPosition + new Vector3(x, y, 0) * shakeAmount,
			Quaternion.Euler(initialRotation + new Vector3(0, 0, z * 5f * shakeAmount))
		);
		trauma = Mathf.Clamp01(trauma - Time.deltaTime * traumaDecay);
	}

}
