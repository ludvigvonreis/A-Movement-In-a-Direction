using System;
using System.Collections;
using UnityEngine;

public class ChargedProjectileAction : ProjectileAction
{
	[SerializeField] private float chargeTime = 1.4f;

	private readonly float epsilon = 0.04f;
	private float timer = 0f;

	private float chargedDamageMultiplier = 0f;
	private int ammoPerFire = 0;
	private int chargedAmmoPerFire = 0;
	private float cameraFov = 0f;


	public override bool IsSustained => true;

	[SerializeField] private bool canShoot = true;

	public override void Initialize(WeaponBehaviour weapon)
	{
		cameraFov = weapon.Context.GetCameraFov();
		chargedDamageMultiplier = weapon.WeaponStats.chargedDamageMultiplier;
		ammoPerFire = weapon.WeaponStats.ammoPerFire;
		chargedAmmoPerFire = weapon.WeaponStats.chargedAmmoPerFire;

		base.Initialize(weapon);
	}

	protected override float ModifyDamage(float baseDamage)
	{
		return Math.Abs(chargeTime - timer) > epsilon ? baseDamage * chargedDamageMultiplier : baseDamage;
	}

	protected override int GetAmmoUsage()
	{
		return Math.Abs(chargeTime - timer) < epsilon ? chargedAmmoPerFire : ammoPerFire;
	}

	public override IEnumerator StartAction(WeaponBehaviour weapon)
	{
		if (canShoot == false) yield break;
		if (weapon.WeaponAmmo.currentAmmo < 0 || weapon.WeaponAmmo.isReloading) yield break;

		// Increase timer when not at max.
		if (Math.Abs(chargeTime - timer) > epsilon)
		{
			timer += Time.deltaTime;

			// Start zooming after small delay
			if (timer > 0.2f)
				weapon.Context.ChangeCameraFov(Mathf.Lerp(cameraFov, 75, (timer - 0.2f) / chargeTime), false);
		}

		yield return null;
	}

	public override IEnumerator StopAction(WeaponBehaviour weapon)
	{
		if (canShoot == false) yield break;

		if (timer > epsilon)
		{
			weapon.Context.ResetCameraFov();

			if (weapon.WeaponAmmo.currentAmmo > 0 && !weapon.WeaponAmmo.isReloading)
			{
				StartCoroutine(SpawnProjectile(weapon));
				weapon.Context.AddCameraShake((timer / chargeTime));
			}

			timer = 0f;

			canShoot = false;
			yield return new WaitForSeconds(weapon.WeaponStats.Firerate);
			canShoot = true;
		}
	}
}