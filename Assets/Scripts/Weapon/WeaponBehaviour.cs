using UnityEditor.Rendering.LookDev;
using UnityEngine;

[System.Serializable]
public class WeaponAmmo {
	public int currentAmmo;
	public int currentCarriedAmmo;
	public bool isReloading;
}

public class WeaponBehaviour : MonoBehaviour
{
	[SerializeField]
	private WeaponStats weaponStats;
	[SerializeField]
	private Transform projectileFirePoint;
	[SerializeField]
	private GameObject modelObject;

	public WeaponStats WeaponStats => weaponStats;
	public Transform ProjectileFirePoint => projectileFirePoint;
	public GameObject ModelObject => modelObject;

	// Modular action handlers
	[SerializeField] 
	private MonoBehaviour primaryActionSource;
	[SerializeField] 
	private MonoBehaviour secondaryActionSource;
	[SerializeField] 
	private MonoBehaviour reloadActionSource;

	private IWeaponAction primaryAction;
	private IWeaponAction secondaryAction;
	private IWeaponAction reloadAction;

	public bool isEnabled = false;
	public bool hasBeenInitialized = false;
	public bool canUnequip = true;

	private WeaponAmmo weaponAmmo;
	public WeaponAmmo WeaponAmmo => weaponAmmo;

	private IWeaponContext context;
	public IWeaponContext Context => context;

	public void Initialize(IWeaponContext newContext)
	{
		if (hasBeenInitialized) return;

		primaryAction = primaryActionSource as IWeaponAction;
		secondaryAction = secondaryActionSource as IWeaponAction;
		reloadAction = reloadActionSource as IWeaponAction;

		weaponAmmo = new() {
			currentAmmo = WeaponStats.magazineAmount,
			currentCarriedAmmo = WeaponStats.maxCarriedAmmo,
			isReloading = false
		};

		context = newContext;

		hasBeenInitialized = true;
	}

	public void RequestReload(bool value)
	{
		if (!value) return;

		StartCoroutine(reloadAction.Execute(this));
	}

	public void RequestPrimaryAction(bool value)
	{
		if (!value) return;
		if (weaponAmmo.currentAmmo <= 0 || weaponAmmo.isReloading)
			return;

		if (WeaponStats.fireMode is FireMode.Automatic) return;

		StartCoroutine(primaryAction.Execute(this));
	}

	public void RequestSecondaryAction(bool value)
	{
		if (!value) return;

		StartCoroutine(secondaryAction.Execute(this));
	}

	// Holding fire button
	public void RequestPrimaryActionSustain(bool value)
	{
		if (WeaponStats.fireMode is not FireMode.Automatic) 
			if (!primaryAction.IsSustained) return;

		if (value)
			StartCoroutine(primaryAction.StartAction(this));
		else
			StartCoroutine(primaryAction.StopAction(this));
	}

	// Holding secondary button
	public void RequestSecondaryActionSustain(bool value)
	{
		if (!secondaryAction.IsSustained) return;

		if (value)
			StartCoroutine(secondaryAction.StartAction(this));
		else
			StartCoroutine(secondaryAction.StopAction(this));
	}
}
