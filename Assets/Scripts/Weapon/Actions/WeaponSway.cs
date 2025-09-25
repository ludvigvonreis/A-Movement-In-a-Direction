using System.Collections;
using UnityEngine;

public class WeaponSway : WeaponActionBase
{
	[SerializeField, InspectorName("Sway Multiplier (inverse)")] float swayMultiplier = 5f;
	[SerializeField] float maxSway = 0.5f;
	[SerializeField] float verticalSwayMultiplier = 0.5f;

	private Vector2 currentSway;
	private Transform parent;
	private Vector3 origin;

	// Mouse Average delta
	private Vector2 AverageDelta;
	private readonly Vector2[] buffer = new Vector2[16];
    private int index = 0;
    private int count = 0;
    private Vector2 sum = Vector2.zero;

	public override bool IsSustained => false;

	public override void Initialize(WeaponBehaviour weapon)
	{

		parent = transform;
		origin = transform.localPosition;
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
		if (weapon.canUnequip == false)
			DoSway();


		return base.Execute(weapon);
	}

	public void DoSway()
	{
		var mouseDelta = AverageDelta;

		// Sway multiplier is inverse, to increase Dev experince.
		// 500f is more readable than 0.005
		Vector2 targetSway = mouseDelta * (1 / swayMultiplier);
		targetSway = Vector2.ClampMagnitude(targetSway, maxSway);

		float decay = 10f;     
		float dt = Time.deltaTime;

		float alpha = 1f - Mathf.Exp(-decay * dt);

		// Update current sway
		currentSway = Vector2.Lerp(currentSway, targetSway, alpha);

		// Limit vertical sway component
		currentSway.y *= verticalSwayMultiplier;

		// Apply limit
		currentSway = Vector2.ClampMagnitude(currentSway, maxSway);

		parent.localPosition = origin + (Vector3)currentSway;
	}
}