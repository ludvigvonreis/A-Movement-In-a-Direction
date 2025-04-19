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
		float duration = 0.08f;
		float elapsed = 0f;

		Vector3 start = transform.localPosition;
		Vector3 target;

		var animParams = new FovAnimationParams
		{
			duration = 0.07f,
			Easing = t => -(Mathf.Cos(Mathf.PI * t) - 1) / 2,
		};

		if (isAiming)
		{
			Vector3 frontSight = aimPointFront.localPosition;
			Vector3 rearSight = aimPointBack.localPosition;
			Vector3 middlePoint = ((frontSight + rearSight) / 2f) - (Vector3.forward * weaponDistance);
			target = crosshairMiddle - middlePoint;
		}
		else
		{
			target = origin;

			// Reset fov before weapon movement.
			weaponContext.ResetCameraFov(
				animated: true, 
				animParams: animParams
			);
		}


		// Move weapon to target.
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);

			transform.localPosition = Vector3.Lerp(start, target, animParams.Easing(t));
			yield return null;
		}

		// Change fov after weapon has been moved.
		if (isAiming)
		{	
			weaponContext.ChangeCameraFov(
				value: aimFov, 
				animated: true, 
				animParams: animParams
			);
		}

		transform.localPosition = target; // snap to end
		currentMovement = null;
	}
}