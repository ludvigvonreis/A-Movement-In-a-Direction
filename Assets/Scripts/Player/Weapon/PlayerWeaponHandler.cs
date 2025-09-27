using System;
using System.Collections.Generic;
using UnityEngine;

public struct PlayerWeaponInput
{
	public bool PrimaryAction;
	public bool PrimaryActionSustain;
	public bool SecondaryAction;
	public bool SecondaryActionSustain;

	public Vector2 Scroll;
	public Vector2 Mouse;

	public bool Reload;
}

public class PlayerWeaponHandler : MonoBehaviour
{
	[SerializeField] private PlayerCamera playerCamera;
	[SerializeField] private PlayerCharacter playerCharacter;
	
	[Header("Weapons")]
	[SerializeField] private List<GameObject> weaponObjects = new List<GameObject>();
	private List<WeaponBehaviour> weaponBehaviours = new List<WeaponBehaviour>();

	[SerializeField] private int currentWeaponIndex = 0;


	private WeaponBehaviour CurrentWeaponBehaviour => weaponBehaviours[currentWeaponIndex];

	void Start()
	{
		// Find all equipped weapons and add them to the list.
		foreach (var weaponObject in weaponObjects) {
			if (!weaponObject.TryGetComponent<WeaponBehaviour>(out var weaponBehaviour)) continue;

			weaponBehaviours.Add(weaponBehaviour);

			weaponObject.SetActive(false);
			weaponBehaviour.isEnabled = false;
		}

		// Equip first weapon if any are equipped.
		if (weaponBehaviours.Count > 0)
			SwitchWeaponToIndex(0);
	}

	public void UpdateInput(PlayerWeaponInput playerWeaponInput)
	{
		CurrentWeaponBehaviour.RequestPrimaryAction(playerWeaponInput.PrimaryAction);
		CurrentWeaponBehaviour.RequestPrimaryActionSustain(playerWeaponInput.PrimaryActionSustain);
		CurrentWeaponBehaviour.RequestSecondaryAction(playerWeaponInput.SecondaryAction);
		CurrentWeaponBehaviour.RequestSecondaryActionSustain(playerWeaponInput.SecondaryActionSustain);
		CurrentWeaponBehaviour.RequestReload(playerWeaponInput.Reload);
		CurrentWeaponBehaviour.ProvideMouseDelta(playerWeaponInput.Mouse);

		var scrollDelta = Mathf.CeilToInt(Math.Clamp(playerWeaponInput.Scroll.y, -1, 1));
		int newIndex = (currentWeaponIndex + scrollDelta + weaponBehaviours.Count) % weaponBehaviours.Count;

		if (scrollDelta != 0)
			SwitchWeaponToIndex(newIndex);
	}

	void SwitchWeaponToIndex(int newIndex) {
		if (newIndex < 0 || newIndex > (weaponBehaviours.Count - 1)) {
			Debug.LogError("Invalid weapon index");
			return;
		}

		// We cannot change weapon as it would disturb some kind of process running.
		if (CurrentWeaponBehaviour.canUnequip == false) return;

		currentWeaponIndex = newIndex;

		// Equipping game object.
		for (int i = 0; i < weaponBehaviours.Count; i++)
		{	
			// Only perform these actions on this weapon.
			if (i == currentWeaponIndex) continue;

			weaponBehaviours[i].isEnabled = false;
			weaponObjects[i].SetActive(false);
		}

		// Works once
		// var weaponPos = weaponObjects[currentWeaponIndex].transform.localPosition;
		// transform.localPosition = weaponPos;
		// weaponObjects[currentWeaponIndex].transform.localPosition = Vector3.zero;

		weaponObjects[currentWeaponIndex].SetActive(true);

		// Only initialize when needed.
		//if (!CurrentWeaponBehaviour.hasBeenInitialized)
		CurrentWeaponBehaviour.Initialize(new PlayerWeaponContext(playerCamera, playerCharacter));
	}
}