using System.Collections;
using UnityEngine;

public class WeaponSway : WeaponActionBase
{
	[SerializeField] float swayMultiplier = 0.02f;
	[SerializeField] float maxSway = 2f;
	[SerializeField] float smooth = 8f;
	[SerializeField] float springiness = 4f;

	private Vector2 currentSway;
	private Vector2 swayVelocity;

	public override bool IsSustained => throw new System.NotImplementedException();

	public override IEnumerator Execute(WeaponBehaviour weapon)
	{
		var mouseDelta = weapon.MouseDelta;


		// Add input force to sway
		Vector2 targetSway = mouseDelta * swayMultiplier;
		targetSway = Vector2.ClampMagnitude(targetSway, maxSway);

		// SmoothDamp behaves like a spring
		currentSway = Vector2.SmoothDamp(currentSway, targetSway, ref swayVelocity, 1f / springiness);

		// Create rotation based on current sway
		Quaternion rotX = Quaternion.AngleAxis(-currentSway.y, Vector3.right);
		Quaternion rotY = Quaternion.AngleAxis(currentSway.x, Vector3.up);
		Quaternion finalRotation = rotX * rotY;

		// Smoothly rotate
		transform.localRotation = Quaternion.Slerp(transform.localRotation, finalRotation, smooth * Time.deltaTime);

		return base.Execute(weapon);
	}
}