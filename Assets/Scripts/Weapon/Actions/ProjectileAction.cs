using System.Collections;
using UnityEngine;

public class ProjectileAction : WeaponActionBase
{
	public override bool IsSustained => true;

	// Full auto fire.
	private Coroutine currentRoutine;
	private bool isRunning = false;
	protected bool canShoot = true;

	public override IEnumerator Execute(WeaponBehaviour weapon, System.Action onComplete)
	{
		if (canShoot == false) yield break;

		SpawnProjectile(weapon);

		canShoot = false;
		yield return new WaitForSeconds(weapon.WeaponStats.Firerate);
		canShoot = true;

		onComplete?.Invoke();
	}

	protected void SpawnProjectile(WeaponBehaviour weapon)
	{
		// Spawn projectile(s)
		{
			var projectileStats = weapon.WeaponStats.projectile;
			var weaponStats = Instantiate(weapon.WeaponStats);
			var projectilePrefab = projectileStats.projectilePrefab;

			weapon.primaryActionEvent.Invoke();

			// Spawn the required amount of projectiles for this weapon cycle.
			for (int i = 0; i < weapon.WeaponStats.projectileCount; i++)
			{
				// Spread.
				var rotation = Quaternion.AngleAxis(
						Random.Range(0f, weapon.WeaponStats.spreadAngle), Random.onUnitSphere
					) * weapon.ProjectileFirePoint.forward;

				// Summon projectile
				var spawnedProjectile = Instantiate
				(
					original: projectilePrefab,
					position: weapon.ProjectileFirePoint.position,
					rotation: Quaternion.Euler(rotation)
				);

				Physics.IgnoreCollision(weapon.Context.GetOwnerCollider(), spawnedProjectile.GetComponent<Collider>());

				var projectileObject = spawnedProjectile.GetComponent<ProjectileObject>();
				projectileObject.Initialize(
					rotation,
					weapon.ProjectileFirePoint.position,
					projectileStats,
					weaponStats
				);
			}
		}
		// Decrease ammo.
		weapon.WeaponAmmo.currentAmmo -= 1;
		weapon.WeaponAmmo = weapon.WeaponAmmo;
	}


	public override IEnumerator StartAction(WeaponBehaviour weapon, System.Action onComplete)
	{
		if (!isRunning)
		{
			isRunning = true;
			currentRoutine = weapon.StartCoroutine(SustainedFireLoop(weapon));
		}

		onComplete?.Invoke();
		yield return null;
	}

	public override IEnumerator StopAction(WeaponBehaviour weapon, System.Action onComplete)
	{
		if (isRunning)
		{
			weapon.StopCoroutine(currentRoutine);
			currentRoutine = null;
			isRunning = false;
		}

		onComplete?.Invoke();
		yield return null;
	}

	private IEnumerator SustainedFireLoop(WeaponBehaviour weapon)
	{
		while (true)
		{
			// Prevent firing when no ammo or reloading
			if (weapon.WeaponAmmo.currentAmmo > 0 && !weapon.WeaponAmmo.isReloading)
			{
				yield return Execute(weapon, null);
			}
			else
			{
				yield return null;
			}
		}
	}

}