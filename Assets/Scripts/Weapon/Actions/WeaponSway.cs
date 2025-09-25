using System.Collections;
using UnityEngine;

public class WeaponSway : WeaponActionBase
{
	[SerializeField, InspectorName("Sway Multiplier (inverse)")] float swayMultiplier = 5f;
	[SerializeField] float maxSway = 0.5f;
	[SerializeField] float verticalSwayMultiplier = 0.5f;

	// Mouse Average delta
	private Vector2 AverageDelta;
	private readonly Vector2[] buffer = new Vector2[16];
    private int index = 0;
    private int count = 0;
    private Vector2 sum = Vector2.zero;

	public override bool IsSustained => false;

	public override void Initialize(WeaponBehaviour weapon)
	{
		base.Initialize(weapon);
	}

	public override IEnumerator Execute(WeaponBehaviour weapon)
	{
		Vector2 current = weapon.MouseDelta;

        // subtract oldest value
        sum -= buffer[index];

        // add new value
        buffer[index] = current;
        sum += current;

        // advance index
        index = (index + 1) % buffer.Length;

        // track number of samples (max 16)
        if (count < buffer.Length) count++;

        AverageDelta = sum / count;

		// Can unequip is often used for when the weapon is "busy"
		// Like when performing reloading or other animations
		// Do not interrupt them with sway.
		if (weapon.canUnequip == true)
			// Add the movement to main movement to allow for multiple sources of movement.
			weapon.ModelObjectMovement += DoSway();


		return base.Execute(weapon);
	}

	public Vector3 DoSway()
	{
		var mouseDelta = AverageDelta;

		// Sway multiplier is inverse, to increase Dev experince.
		// 500f is more readable than 0.005
		Vector2 targetSway = mouseDelta * (1 / swayMultiplier);
		targetSway = Vector2.ClampMagnitude(targetSway, maxSway);

		// Limit vertical sway component
		targetSway.y *= verticalSwayMultiplier;

		return (Vector3)targetSway;
	}
}