using UnityEngine;

[System.Serializable]
public struct WeaponAmmo {
	public int currentAmmo;
	public int currentCarriedAmmo;
	public bool isReloading;
}

public abstract class WeaponBehaviour : MonoBehaviour
{
	[SerializeField] protected WeaponStats WeaponStats;
	[SerializeField] protected Transform projectileFirePoint;
	[SerializeField] protected WeaponAmmo weaponAmmo;
	[SerializeField] protected GameObject modelObject;
	public bool	isEnabled = false;

	// What the weapon will do when the primary fire action is pressed, 
	// usually by a player using Mouse1
	protected abstract void PrimaryAction();

	// What the weapon will do when the secondary fire action is pressed.
	protected abstract void SecondaryAction();

	protected abstract void ReloadAction();

	// What needs to happen before we can shoot.
	public abstract void Initialize();

	public abstract void RequestReload(bool value);

	public abstract void RequestPrimaryAction(bool value);
	public abstract void RequestPrimaryActionSustain(bool value);

	public abstract void RequestSecondaryAction(bool value);
	public abstract void RequestSecondaryActionSustain(bool value);
}