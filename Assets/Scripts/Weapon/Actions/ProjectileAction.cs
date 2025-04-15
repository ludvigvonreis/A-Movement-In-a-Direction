using System.Collections;
using UnityEngine;

public class ProjectileAction : WeaponActionBase
{
	public override bool IsSustained => false;

	// Full auto fire.
	private Coroutine currentRoutine;
	private bool isRunning = false;

	public override IEnumerator Execute(WeaponBehaviour weapon)
	{
		// Spawn projectile(s)
		{
			var projectileStats = weapon.WeaponStats.projectile;
			var projectilePrefab = projectileStats.projectilePrefab;

			// Spawn the required amount of projectiles for this weapon cycle.
			for (int i = 0; i < weapon.WeaponStats.projectileCount; i++)
			{

				// Spread.
				var rotation = Quaternion.AngleAxis(
						Random.Range(0f, weapon.WeaponStats.spreadAngle), Random.onUnitSphere
					) * weapon.projectileFirePoint.forward;

				// Summon projectile
				var spawnedProjectile = Instantiate
				(
					original: projectilePrefab,
					position: weapon.projectileFirePoint.position,
					rotation: Quaternion.Euler(rotation)
				);

				var projectileObject = spawnedProjectile.GetComponent<ProjectileObject>();
				projectileObject.Initialize(
					rotation,
					weapon.projectileFirePoint.position,
					projectileStats,
					weapon.WeaponStats
				);
			}
		}

		// Decrease ammo.
		weapon.weaponAmmo.currentAmmo -= 1;

		yield return new WaitForSeconds(weapon.WeaponStats.Firerate);
	}


	public override IEnumerator StartAction(WeaponBehaviour weapon)
	{
		if (!isRunning)
		{
			isRunning = true;
			currentRoutine = weapon.StartCoroutine(SustainedFireLoop(weapon));
		}
		yield return null;
	}

	public override IEnumerator StopAction(WeaponBehaviour weapon)
	{
		if (isRunning)
		{
			weapon.StopCoroutine(currentRoutine);
			currentRoutine = null;
			isRunning = false;
		}
		yield return null;
	}

	private IEnumerator SustainedFireLoop(WeaponBehaviour weapon)
	{
		while (true)
		{
			// Prevent firing when no ammo or reloading
			if (weapon.weaponAmmo.currentAmmo > 0 && !weapon.weaponAmmo.isReloading)
			{
				yield return Execute(weapon);
			}
			else
			{
				yield return null;
			}
		}
	}

}