using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

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
	private Vector3 modelObjectMovement;

	public WeaponStats WeaponStats => weaponStats;
	public Transform ProjectileFirePoint => projectileFirePoint;
	public GameObject ModelObject => modelObject;
	public Vector3 ModelObjectMovement
	{
		get => modelObjectMovement;
		set => modelObjectMovement = value;
	}

	private WeaponAmmo weaponAmmo;
	public WeaponAmmo WeaponAmmo => weaponAmmo;

	private IWeaponContext context;
	public IWeaponContext Context => context;

	// Mouse delta from player.
	private Vector2 mouseDelta;
	[HideInInspector]
	public Vector2 MouseDelta => mouseDelta;

	// Action Events
	[HideInInspector] public UnityEvent primaryActionEvent;
	[HideInInspector] public UnityEvent secondaryActionEvent;
	[HideInInspector] public UnityEvent reloadActionEvent;

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

	[SerializeField] 
	private MonoBehaviour[] continuousActionSources;

	private IWeaponAction[] continuousActions;

	[Space]
	public bool isEnabled = false;
	public bool hasBeenInitialized = false;
	public bool canUnequip = true;

	public void Initialize(IWeaponContext newContext)
	{
		if (hasBeenInitialized) return;

		primaryAction = primaryActionSource as IWeaponAction;
		secondaryAction = secondaryActionSource as IWeaponAction;
		reloadAction = reloadActionSource as IWeaponAction;

		continuousActions = continuousActionSources
			.Select(e => e as IWeaponAction)
			.ToArray();

		weaponAmmo = new() {
			currentAmmo = WeaponStats.magazineAmount,
			currentCarriedAmmo = WeaponStats.maxCarriedAmmo,
			isReloading = false
		};

		primaryActionEvent ??= new UnityEvent();
		secondaryActionEvent ??= new UnityEvent();
		reloadActionEvent ??= new UnityEvent();

		context = newContext;

		primaryAction.Initialize(this);
		secondaryAction.Initialize(this);
		reloadAction.Initialize(this);

		// Initialize all continuous actions
		foreach (var continuousAction in continuousActions)
		{
			continuousAction.Initialize(this);
		}

		hasBeenInitialized = true;
	}

	void Update()
	{
		// Run every continuous action if it exists.
		foreach (var continuousAction in continuousActions)
		{
			StartCoroutine(continuousAction.Execute(this));
		}
	}

	public void OnEnable()
	{
		if (hasBeenInitialized == false) return;

		primaryAction.Initialize(this);
		secondaryAction.Initialize(this);
		reloadAction.Initialize(this);
	}

	public void RequestReload(bool value)
	{
		if (!value) return;

		reloadActionEvent.Invoke();

		StartCoroutine(reloadAction.Execute(this));
	}

	public void RequestPrimaryAction(bool value)
	{
		if (!value) return;
		if (weaponAmmo.currentAmmo <= 0 || weaponAmmo.isReloading)
			return;

		if (WeaponStats.fireMode is FireMode.Automatic) return;

		primaryActionEvent.Invoke();

		StartCoroutine(primaryAction.Execute(this));
	}

	public void RequestSecondaryAction(bool value)
	{
		if (!value) return;

		secondaryActionEvent.Invoke();

		StartCoroutine(secondaryAction.Execute(this));
	}

	// Holding fire button
	public void RequestPrimaryActionSustain(bool value)
	{
		if (WeaponStats.fireMode is not FireMode.Automatic) 
		if (!primaryAction.IsSustained) return;


		if (value) {
			StartCoroutine(primaryAction.StartAction(this));
		}
		else
			StartCoroutine(primaryAction.StopAction(this));
	}

	// Holding secondary button
	public void RequestSecondaryActionSustain(bool value)
	{
		if (!secondaryAction.IsSustained) return;


		if (value) {
			StartCoroutine(secondaryAction.StartAction(this));
		}
		else
			StartCoroutine(secondaryAction.StopAction(this));
	}

	public void ProvideMouseDelta(Vector2 _mouseDelta) {
		mouseDelta = _mouseDelta;
	}
}
