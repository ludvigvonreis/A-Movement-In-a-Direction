using System;
using UnityEngine;

public struct PlayerWeaponInput
{
	public bool PrimaryAction;
	public bool PrimaryActionSustain;
	public bool SecondaryAction;
	public bool SecondaryActionSustain;
}

public class PlayerWeaponHandler : MonoBehaviour
{
	[SerializeField] private WeaponBehaviour[] weaponBehaviours;
	[SerializeField] private int currentWeaponIndex = 0;

	private WeaponBehaviour currentWeaponBehaviour => weaponBehaviours[currentWeaponIndex];

	void Start()
	{
		SwitchWeaponToIndex(0);
	}

	public void UpdateInput(PlayerWeaponInput playerWeaponInput)
	{
		currentWeaponBehaviour.RequestPrimaryAction(playerWeaponInput.PrimaryAction || playerWeaponInput.PrimaryActionSustain);
		currentWeaponBehaviour.RequestSecondaryAction(playerWeaponInput.SecondaryAction || playerWeaponInput.SecondaryActionSustain);
	}

	void SwitchWeaponToIndex(int newIndex) {
		if (newIndex < 0 || newIndex > (weaponBehaviours.Length - 1)) {
			Debug.LogError("Invalid weapon index");
			return;
		}

		currentWeaponIndex = newIndex;
		currentWeaponBehaviour.Initialize();

		// Equipping game object.


		// Disabling all other weapons.

		// Hiding all other weapons.
	}
}