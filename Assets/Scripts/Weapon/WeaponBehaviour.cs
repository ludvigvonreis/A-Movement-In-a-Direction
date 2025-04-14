using UnityEngine;

public abstract class WeaponBehaviour<T> : MonoBehaviour where T : WeaponStats
{
	[SerializeField] protected T WeaponStats;
	[SerializeField] protected Transform projectileFirePoint;

	// What the weapon will do when the primary fire action is pressed, 
	// usually by a player using Mouse1
	public abstract void PrimaryAction();

	// What the weapon will do when the secondary fire action is pressed.
	public abstract void SecondaryAction();
}