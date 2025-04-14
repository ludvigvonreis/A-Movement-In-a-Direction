using UnityEngine;

public struct WeaponAmmo {
	public int currentAmmo;
	public int currentCarriedAmmo;
	public bool canReload;
}

public abstract class WeaponBehaviour : MonoBehaviour
{
	[SerializeField] protected WeaponStats WeaponStats;
	[SerializeField] protected Transform projectileFirePoint;
	[SerializeField] protected WeaponAmmo weaponAmmo;
	[SerializeField] protected bool	isEnabled = false;

	// What the weapon will do when the primary fire action is pressed, 
	// usually by a player using Mouse1
	protected abstract void PrimaryAction();

	// What the weapon will do when the secondary fire action is pressed.
	protected abstract void SecondaryAction();

	// What needs to happen before we can shoot.
	public abstract void Initialize();

	public abstract void RequestPrimaryAction(bool value);
	public abstract void RequestSecondaryAction(bool value);
}