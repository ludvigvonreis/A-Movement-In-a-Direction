using System.Collections;
using UnityEngine;

public class ChargedAction : WeaponActionBase
{
	[SerializeField, WeaponProperty]
	private float chargeTime = 1.4f;
	[SerializeField, WeaponProperty]
	private float zoomedCameraFov = 75f;

	[SerializeField, WeaponProperty]
	private WeaponActionBase subAction;

	private readonly float epsilon = 0.04f;
	private float timer = 0f;

	private float chargedDamageMultiplier = 0f;
	private int ammoPerFire = 0;
	private int chargedAmmoPerFire = 0;

	private float cameraFov = 0f;

	public override bool IsSustained => true;

	protected bool canShoot = true;

	public override void Initialize(WeaponBehaviour weapon)
	{
		cameraFov = weapon.Context.GetCameraFov();
		chargedDamageMultiplier = weapon.WeaponStats.chargedDamageMultiplier;
		ammoPerFire = weapon.WeaponStats.ammoPerFire;
		chargedAmmoPerFire = weapon.WeaponStats.chargedAmmoPerFire;
		chargeTime = weapon.WeaponStats.chargeTime;

		base.Initialize(weapon);
	}

	void ModifyWeaponStats(WeaponStats weaponStats)
	{
		var baseDamage = weaponStats.damage;
		// Increase damage by chargedDamageMultiplier when fully charged.
		weaponStats.damage = Mathf.Abs(chargeTime - timer) > epsilon ? baseDamage * chargedDamageMultiplier : baseDamage;
	}

	protected int GetAmmoUsage()
	{
		return Mathf.Abs(chargeTime - timer) < epsilon ? chargedAmmoPerFire : ammoPerFire;
	}

	public override IEnumerator StartAction(WeaponBehaviour weapon, System.Action onComplete)
	{
		if (canShoot == false) yield break;
		if (weapon.WeaponAmmo.currentAmmo < 0 || weapon.WeaponAmmo.isReloading) yield break;

		// Increase timer when not at max.
		if (Mathf.Abs(chargeTime - timer) > epsilon)
		{
			timer += Time.deltaTime;

			// Start zooming after small delay
			if (timer > 0.2f)
				weapon.Context.ChangeCameraFov(Mathf.Lerp(cameraFov, zoomedCameraFov, (timer - 0.2f) / chargeTime), false);
		}

		onComplete?.Invoke();
		yield return null;
	}

	public override IEnumerator StopAction(WeaponBehaviour weapon, System.Action onComplete)
	{
		if (canShoot == false) yield break;

		if (timer > epsilon)
		{
			weapon.Context.ResetCameraFov(true);

			if (weapon.WeaponAmmo.currentAmmo > 0 && !weapon.WeaponAmmo.isReloading)
			{
				ModifyWeaponStats(weapon.WeaponStats);
				StartCoroutine(subAction.Execute(weapon, onComplete));
			}

			timer = 0f;

			canShoot = false;
			yield return new WaitForSeconds(weapon.WeaponStats.Firerate);
			canShoot = true;
		}

		onComplete?.Invoke();
	}
}