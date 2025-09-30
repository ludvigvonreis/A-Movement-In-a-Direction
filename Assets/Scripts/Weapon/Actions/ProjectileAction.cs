using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class ProjectileAction : WeaponActionBase
{
	public override bool IsSustained => false;

	// Full auto fire.
	private Coroutine currentRoutine;
	private bool isRunning = false;

	public override IEnumerator Execute(WeaponBehaviour weapon)
	{
		yield return SpawnProjectile(weapon);
	}

	protected virtual float ModifyDamage(float baseDamage)
	{
		return baseDamage;
	}

	protected virtual int GetAmmoUsage()
	{
		return 1;
	}

	protected IEnumerator SpawnProjectile(WeaponBehaviour weapon)
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

				weaponStats.damage = ModifyDamage(weaponStats.damage);

				var projectileObject = spawnedProjectile.GetComponent<ProjectileObject>();
				projectileObject.Initialize(
					rotation,
					weapon.ProjectileFirePoint.position,
					projectileStats,
					weaponStats
				);

				Physics.IgnoreCollision(projectileObject.GetComponent<Collider>(), weapon.Context.GetOwnerCollider());
			}
		}
		// Decrease ammo.
		weapon.WeaponAmmo.currentAmmo -= GetAmmoUsage();

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
			if (weapon.WeaponAmmo.currentAmmo > 0 && !weapon.WeaponAmmo.isReloading)
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