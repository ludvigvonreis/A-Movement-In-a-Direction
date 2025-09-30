using System.Collections;
using UnityEngine;

public struct CameraInput
{
	public Vector2 Look;
}

public struct FovAnimationParams
{
	public float duration;
	public System.Func<float, float> Easing;

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
	[SerializeField] private float decay = 10f;

	private Quaternion targetRotation;
	private Vector3 targetPosition;

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

	public void Shake(Vector3 vector)
	{
		targetRotation *= Quaternion.Euler(vector);
	}

	void Update()
	{
		float alpha = 1f - Mathf.Exp(-decay * Time.deltaTime);

		shakeable.SetLocalPositionAndRotation(
			Vector3.Lerp(shakeable.localPosition, targetPosition, alpha),
			Quaternion.Slerp(shakeable.localRotation, targetRotation, alpha)
		);
		targetPosition = Vector3.zero;
		targetRotation = Quaternion.identity;
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
