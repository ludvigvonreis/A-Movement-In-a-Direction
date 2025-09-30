using System;
using System.Collections;
using UnityEngine;

public struct CameraInput
{
	public Vector2 Look;
}

public struct FovAnimationParams
{
	public float duration;
	public Func<float, float> Easing;

	public static FovAnimationParams Default => new FovAnimationParams
	{
		duration = 0.05f,
		Easing = t => t
	};
}


public class PlayerCamera : MonoBehaviour
{
	private Vector3 _eulerAngles;

	[SerializeField]
	private float sensitivity = 1;

	[SerializeField, Range(0, 180)]
	private float maxXAngle;

	[SerializeField]
	private Camera mainCamera;
	private float originalFov;

	public float OriginalFov => originalFov;

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
		originalFov = mainCamera.fieldOfView;
	}

	public void UpdateRotation(CameraInput input)
	{
		_eulerAngles += new Vector3(-input.Look.y, input.Look.x);

		//if (_eulerAngles.x > 180f) _eulerAngles.x -= 360f;
		var _maxXAngle = maxXAngle * (1 / sensitivity);
		_eulerAngles.x = Mathf.Clamp(_eulerAngles.x, -_maxXAngle, _maxXAngle);

		transform.eulerAngles = _eulerAngles * sensitivity;
	}

	public void UpdatePosition(Transform target)
	{
		transform.position = target.position;
	}


	// FIXME: This should be its own thing, not related to camera directly.
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

	public void ChangeCameraFov(float value, bool animated = false, FovAnimationParams? animParams = null)
	{
		if (mainCamera == null) return;

		if (animated)
			StartCoroutine(FovAnimation(value, animParams ?? FovAnimationParams.Default));
		else
			mainCamera.fieldOfView = value;
	}

	public void ResetCameraFov(bool animated = false, FovAnimationParams? animParams = null)
	{

		if (mainCamera == null) return;

		if (animated)
			StartCoroutine(FovAnimation(originalFov, animParams ?? FovAnimationParams.Default));
		else
			mainCamera.fieldOfView = originalFov;
	}


	IEnumerator FovAnimation(float target, FovAnimationParams animParams)
	{
		if (mainCamera == null)
			yield break;

		float duration = Mathf.Max(0.0001f, animParams.duration);

		float startFov = mainCamera.fieldOfView;
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			float easedT = animParams.Easing(t);
			mainCamera.fieldOfView = Mathf.Lerp(startFov, target, easedT);
			yield return null;
		}

		mainCamera.fieldOfView = target;
	}

}
