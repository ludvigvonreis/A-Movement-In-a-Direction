using System.Collections;
using UnityEngine;

public class VisualWeaponMovement : WeaponActionBase
{
	/// <summary>
	/// This class handles multiple sources of movement and applies them additivly to the weapon object each frame.
	/// </summary>


	public override bool IsSustained => false;

	private Transform thisTransform;
	private Vector3 origin;
	private float decay = 10f;

	public override void Initialize(WeaponBehaviour weapon)
	{
		thisTransform = transform;
		origin = transform.localPosition;
		base.Initialize(weapon);
	}

	public override IEnumerator Execute(WeaponBehaviour weapon)
	{
		// Smoothing alpha
		float alpha = 1f - Mathf.Exp(-decay * Time.deltaTime);

		// Move object towards new offset smoothly.
		var smoothedMovement = Vector3.Lerp(thisTransform.localPosition, origin + weapon.ModelObjectMovement, alpha);
		thisTransform.localPosition = smoothedMovement;

		// Reset movement offset.
		weapon.ModelObjectMovement = Vector3.zero;

		return base.Execute(weapon);
	}
}