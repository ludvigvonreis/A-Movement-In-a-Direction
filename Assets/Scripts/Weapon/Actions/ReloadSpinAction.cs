using System.Collections;
using UnityEngine;

public class ReloadSpinAction : WeaponActionBase
{
	public override bool IsSustained => false;

	public override IEnumerator Execute(WeaponBehaviour weapon)
	{
		if (weapon.WeaponAmmo.isReloading) yield break;

		// Magazine or the alike is full, we cannot reload.
		if (weapon.WeaponAmmo.currentAmmo > weapon.WeaponStats.magazineAmount) yield break;

		yield return Reload(weapon);
	}


	IEnumerator Reload(WeaponBehaviour weapon)
	{
		// Start delayed weapon reloading.
		var weaponAmmo = weapon.WeaponAmmo;

		weaponAmmo.isReloading = true;
		weapon.canUnequip = false;

		// Empty current magazine.
		var ammoBeforeReload = weaponAmmo.currentAmmo;
		weaponAmmo.currentAmmo = 0;

		float reloadTime = weapon.WeaponStats.reloadDuration;
		float elapsed = 0f;
		Quaternion initialRotation = weapon.ModelObject.transform.localRotation;

		while (elapsed < reloadTime)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / reloadTime);

			// Rotate 360 degrees around Z (or change axis as needed)
			float rotationAngle = Mathf.Lerp(0f, 360f, t);
			weapon.ModelObject.transform.localRotation = initialRotation * Quaternion.Euler(rotationAngle, 0f, 0f);

			yield return null;
		}

		var magazineAmount = weapon.WeaponStats.magazineAmount;
		var difference = magazineAmount - ammoBeforeReload;
		// Remove the difference between magazine contents and max content.
		weaponAmmo.currentCarriedAmmo -= difference;

		// Fill magazine to full or take all that is left.
		if (weaponAmmo.currentCarriedAmmo < magazineAmount) magazineAmount = weaponAmmo.currentCarriedAmmo;
		weaponAmmo.currentAmmo = magazineAmount;

		// End reloading process
		weaponAmmo.isReloading = false;
		weapon.canUnequip = true;
	}
}