using UnityEngine;
using NaughtyAttributes;

public enum FireMode {
	Single,
	Automatic,
	Burst,
}

[CreateAssetMenu(fileName = "New WeaponStats", menuName = "Weapon/Weapon Stats")]
public class WeaponStats : ScriptableObject
{

	public new string name;
	public string description;

	[Space, Header("Damage & Range")]
	// Range in unity units.
	public float range = 100f;
	public float damageMultiplier = 1f;

	[Header("Fire Rate")]
	[Tooltip("Rounds per minute")]
	[SerializeField]
	private float RPM = 16.6666f;
	public FireMode fireMode;

	// Internal firerate.
	// Delay between shots in seconds.
	// Smallest value is epsilon.
	[ShowNativeProperty]
	public float Firerate => Mathf.Max(60 / RPM, 0e-12f);

	[Space]
	[Header("Projectiles")]
	// Projectile contains damage, falloff, splash and so forth.
	public Projectile projectile;
	public int projectileCount = 1;
	public float spreadAngle;
}