using System.Collections;
using UnityEngine;

public class ProjectileWeaponBehaviour : WeaponBehaviour
{
	private bool _requestPrimaryAction = false;
	private bool _requestSecondaryAction = false;

	public override void Initialize()
	{
		// Initialize ammo.
		weaponAmmo.currentAmmo = WeaponStats.magazineAmount;
		weaponAmmo.currentCarriedAmmo = WeaponStats.maxCarriedAmmo;
		weaponAmmo.isReloading = false;

		isEnabled = true;


		// FIXME: This might run more than once.
		StartCoroutine(EventHandler());
	}

	protected override void PrimaryAction()
	{
		// Spawn projectile(s)
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

		// Decrease ammo.
		weaponAmmo.currentAmmo -= 1;
	}

	protected override void SecondaryAction()
	{
		Debug.Log("Hello im secondary action");
	}

	protected override void ReloadAction()
	{
		if (weaponAmmo.isReloading) return;

		// Magazine or the alike is full, we cannot reload.
		if (weaponAmmo.currentAmmo > WeaponStats.magazineAmount) return;

		

		// Start delayed weapon reloading.
		StartCoroutine(ReloadBehaviour());
	}

	IEnumerator ReloadBehaviour()
	{
		// Start reload process
		weaponAmmo.isReloading = true;

		Debug.LogFormat("Reloading!!!, {0}", weaponAmmo.isReloading);
		// Empty current magazine.
		var ammoBeforeReload = weaponAmmo.currentAmmo;
		weaponAmmo.currentAmmo = 0;

		float reloadTime = WeaponStats.reloadDuration;
		float elapsed = 0f;
		Quaternion initialRotation = modelObject.transform.localRotation;

		while (elapsed < reloadTime)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / reloadTime);

			// Rotate 360 degrees around Z (or change axis as needed)
			float rotationAngle = Mathf.Lerp(0f, 360f, t);
			modelObject.transform.localRotation = initialRotation * Quaternion.Euler(rotationAngle, 0f, 0f);

			yield return null;
		}

		var magazineAmount = WeaponStats.magazineAmount;
		var difference = magazineAmount - ammoBeforeReload;
		// Remove the difference between magazine contents and max content.
		weaponAmmo.currentCarriedAmmo -= difference;

		// Fill magazine to full or take all that is left.
		if (weaponAmmo.currentCarriedAmmo < magazineAmount) magazineAmount = weaponAmmo.currentCarriedAmmo;
		weaponAmmo.currentAmmo = magazineAmount;

		// End reloading process
		weaponAmmo.isReloading = false;
	}


	IEnumerator EventHandler()
	{
		while (true)
		{

			if (_requestSecondaryAction)
			{
				SecondaryAction();
			}

			if (_requestPrimaryAction)
			{
				// To make you relase the button after shooting.
				if (WeaponStats.fireMode is FireMode.Single)
				{
					_requestPrimaryAction = false;
				}

				// Prevent shooting when empty.
				if (weaponAmmo.currentAmmo <= 0 || weaponAmmo.isReloading)
				{
					_requestPrimaryAction = false;
				}
				else
				{
					PrimaryAction();
				}

				yield return new WaitForSeconds(WeaponStats.Firerate);
			}

			yield return null;
		}
	}


	public override void RequestReload(bool value)
	{
		if (value) ReloadAction();
	}

	public override void RequestPrimaryAction(bool value)
	{
		_requestPrimaryAction = value;
	}

	public override void RequestSecondaryAction(bool value)
	{
		_requestSecondaryAction = value;
	}

	// Holding fire button
	public override void RequestPrimaryActionSustain(bool value)
	{
		if (WeaponStats.fireMode is FireMode.Automatic) _requestPrimaryAction = value;
	}

	// Holding secondary button
	public override void RequestSecondaryActionSustain(bool value)
	{
		if (WeaponStats.fireMode is FireMode.Automatic) _requestSecondaryAction = value;
	}
}