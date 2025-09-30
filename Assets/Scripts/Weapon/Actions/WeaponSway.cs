using System.Collections;
using UnityEngine;

public class WeaponSway : WeaponActionBase
{
	[SerializeField, InspectorName("Sway Multiplier (inverse)")] float swayMultiplier = 5f;
	[SerializeField] float maxSway = 0.5f;
	[SerializeField] float verticalSwayMultiplier = 0.5f;
	[SerializeField] Vector2 figure8Frequency = new Vector2(1.0f, 2.0f);
	[SerializeField] Vector2 figure8Amplitude = new Vector2(0.05f, 0.03f);

	// Mouse Average delta
	private Vector2 AverageDelta;
	private readonly Vector2[] buffer = new Vector2[16];
    private int index = 0;
    private int count = 0;
    private Vector2 sum = Vector2.zero;
	private Vector2 proceduralSwayOffset = Vector2.zero;

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

		// --- Procedural figure-8 sway ---
		float time = Time.time;
		Vector2 figure8Frequency = new Vector2(1.0f, 2.0f);   // X/Y frequencies
		Vector2 figure8Amplitude = new Vector2(0.02f, 0.01f);  // small offsets

		float figure8X = Mathf.Sin(time * figure8Frequency.x) * figure8Amplitude.x;
		float figure8Y = Mathf.Sin(time * figure8Frequency.y + Mathf.PI / 2) * figure8Amplitude.y;

		// Optional: add slow Perlin noise on top for randomness
		float noiseX = (Mathf.PerlinNoise(time * 0.3f, 0f) - 0.5f) * 0.01f;
		float noiseY = (Mathf.PerlinNoise(0f, time * 0.3f) - 0.5f) * 0.01f;

		Vector2 proceduralTarget = new Vector2(figure8X + noiseX, figure8Y + noiseY);

		// --- Smooth procedural motion ---
		float proceduralSmoothSpeed = 2f; // higher = snappier, lower = floaty
		proceduralSwayOffset = Vector2.Lerp(proceduralSwayOffset, proceduralTarget, Time.deltaTime * proceduralSmoothSpeed);

		// --- Combine mouse sway and procedural sway ---
		Vector2 finalSway = targetSway + proceduralSwayOffset;


		return (Vector3)finalSway;
	}
}