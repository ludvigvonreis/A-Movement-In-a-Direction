using UnityEngine;
using NaughtyAttributes;

public enum FireMode {
	Single,
	Automatic,
	Charged,
}

[CreateAssetMenu(fileName = "New WeaponStats", menuName = "Weapon/Weapon Stats")]
public class WeaponStats : ScriptableObject
{
	public new string name;
	public string description;

	[Space, Header("Damage & Range")]
	// Range in unity units.
	public float range = 100f;
	public AnimationCurve damageFalloff;

	public float damage = 1f;
	[EnableIf("isCharged")]
	public float chargedDamageMultiplier = 0f;

	[Header("Fire Rate")]
	[Tooltip("Rounds per minute")]
	[SerializeField]
	private float RPM = 16.6666f;
	public FireMode fireMode;
	private bool isCharged => fireMode is FireMode.Charged;

	// Internal firerate.
	// Delay between shots in seconds.
	// Smallest value is epsilon.
	[ShowNativeProperty]
	public float Firerate => Mathf.Max(60 / RPM, 0e-12f);

	[Space]
	[Header("Projectiles")]
	public Projectile projectile;
	public int projectileCount = 1;
	public float spreadAngle;

	[Space]
	[Header("Ammo & Reloading")]
	public int magazineAmount = 10;
	public int maxCarriedAmmo = 150;
	public float reloadDuration = 1.5f;

	// How many bullets used per shoot
	[Space]
	public int ammoPerFire = 1;
	[EnableIf("isCharged")]
	public int chargedAmmoPerFire = 2;

	[Space]
	[Header("Weapon Specific")]
	[EnableIf("isCharged")]
	public float chargeTime;
}