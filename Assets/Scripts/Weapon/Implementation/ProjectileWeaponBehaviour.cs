using System.Collections;
using UnityEngine;

public class ProjectileWeaponBehaviour : WeaponBehaviour
{
	private bool _requestPrimaryAction;
	private bool _requestSecondaryAction;

	protected override void PrimaryAction()
	{
		var projectileStats = WeaponStats.projectile;
		var projectilePrefab = projectileStats.projectilePrefab;

		// Spawn the required amount of projectiles for this weapon cycle.
		for (int i = 0; i < WeaponStats.projectileCount; i++)
		{

			// Spread.
			var rotation = Quaternion.AngleAxis(
					Random.Range(0f, WeaponStats.spreadAngle), Random.onUnitSphere
				) * projectileFirePoint.forward;

			// Summon projectile
			var spawnedProjectile = Instantiate
			(
				original: projectilePrefab,
				position: projectileFirePoint.position,
				rotation: Quaternion.Euler(rotation)
			);

			var projectileObject = spawnedProjectile.GetComponent<ProjectileObject>();
			projectileObject.Initialize(
				rotation, 
				projectileFirePoint.position, 
				projectileStats, 
				WeaponStats
			);
		}
	}

	protected override void SecondaryAction()
	{
		Debug.Log("Hello im secondary action");
	}

	IEnumerator EventHandler()
	{
		while (true)
		{

			if (_requestPrimaryAction)
			{	
				// To make you relase the button after shooting.
				if (WeaponStats.fireMode is FireMode.Single) {
					_requestPrimaryAction = false;
				}

				PrimaryAction();
				yield return new WaitForSeconds(WeaponStats.Firerate);
			}

			if (_requestSecondaryAction)
			{
				SecondaryAction();
			}

			yield return null;
		}
	}

	public override void Initialize()
	{
		// Initialize ammo.
		weaponAmmo.currentAmmo = WeaponStats.magazineAmount;
		weaponAmmo.currentCarriedAmmo = WeaponStats.maxCarriedAmmo;
		weaponAmmo.canReload = false;

		isEnabled = true;

		StartCoroutine(EventHandler());
	}

	public override void RequestPrimaryAction(bool value)
	{
		if (value == true) Debug.Log("Hello shoot");
		_requestPrimaryAction = value;
	}

	public override void RequestSecondaryAction(bool value)
	{
		_requestSecondaryAction = value;
	}
}