using System.Collections;
using UnityEngine;

public class AimAction : WeaponActionBase
{
	[SerializeField]
	private Transform aimPointBack;
	[SerializeField]
	private Transform aimPointFront;
	[SerializeField]
	private float weaponDistance = 0.05f;
	[SerializeField]
	private float aimFov = 55f;

	private Vector3 origin;
	private Quaternion originRotation;

	// Its just zero...
	private Vector3 crosshairMiddle;

	private bool isAiming = false;

	public override bool IsSustained => true;


	public override IEnumerator StartAction(WeaponBehaviour weapon)
	{
		if (isAiming == true)
		{
			yield break;
		}

		// Start aiming
		isAiming = true;
		CancelCurrentMovement();
		yield return currentMovement = StartCoroutine(AimTransition(weapon.Context));
	}

	public override IEnumerator StopAction(WeaponBehaviour weapon)
	{
		if (isAiming == false)
		{
			yield break;
		}

		// Stop aiming
		isAiming = false;
		CancelCurrentMovement();
		yield return currentMovement = StartCoroutine(AimTransition(weapon.Context));
	}

	void Awake()
	{
		origin = transform.localPosition;
		originRotation = transform.localRotation;
	}
	Coroutine currentMovement;

	void CancelCurrentMovement()
	{
		if (currentMovement != null)
		{
			StopCoroutine(currentMovement);
			currentMovement = null;
		}
	}

	IEnumerator AimTransition(IWeaponContext weaponContext)
	{
		Vector3 start = transform.localPosition;
		Vector3 target;
		Quaternion targetRotation = Quaternion.Euler(0, 0, 0);

		var animParams = new FovAnimationParams
		{
			duration = 0.24f,
			Easing = Ease,
		};

		if (isAiming)
		{
			Vector3 frontSight = aimPointFront.localPosition;
			Vector3 rearSight = aimPointBack.localPosition;
			Vector3 middlePoint = ((frontSight + rearSight) / 2f) - (Vector3.forward * weaponDistance);
			target = crosshairMiddle - middlePoint;

			weaponContext.ChangeCameraFov(
				value: aimFov,
				animated: true,
				animParams: animParams
			);
		}
		else
		{
			target = origin;
			targetRotation = originRotation;

			// Reset fov before weapon movement.
			weaponContext.ResetCameraFov(
				animated: true,
				animParams: animParams
			);
		}

		yield return null;
		yield return null;
		yield return null;

		float duration = 0.27f;
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);

			transform.localPosition = Vector3.Lerp(start, target, animParams.Easing(t));
			transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, animParams.Easing(t));

			yield return null;
		}

		transform.localPosition = target; // snap to end
		transform.localRotation = targetRotation;
		currentMovement = null;
	}


	static float Ease(float x) {
		return x < 0.5 ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
	}
}