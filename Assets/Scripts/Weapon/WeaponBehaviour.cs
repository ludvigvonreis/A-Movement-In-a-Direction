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

	// The weapon stats that are actually used. will be reset to weaponStats every "frame"
	private WeaponStats workingWeaponStats;

	[SerializeField]
	private Transform projectileFirePoint;
	[SerializeField]
	private GameObject modelObject;
	private Vector3 modelObjectMovement;

	[Space]
	public bool isEnabled = false;
	public bool hasBeenInitialized = false;
	public bool canUnequip = true;

	public WeaponStats WeaponStats => workingWeaponStats;
	public Transform ProjectileFirePoint => projectileFirePoint;
	public GameObject ModelObject => modelObject;
	public Vector3 ModelObjectMovement
	{
		get => modelObjectMovement;
		set => modelObjectMovement = value;
	}

	private WeaponAmmo weaponAmmo;
	public WeaponAmmo WeaponAmmo
	{
		get => weaponAmmo;
		set
		{
			OnAmmoUpdate(value);
			weaponAmmo = value;
		}
	}

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

	// Outgoing messages, usually. Goes to player.
	public MessageBus ownerMessageBus;
	// Internal messages.
	public MessageBus weaponMessageBus;

	// Modular action handlers
	[Space]
	[SerializeField] private MonoBehaviour primaryActionSource;
	[SerializeField] private MonoBehaviour secondaryActionSource;
	[SerializeField] private MonoBehaviour reloadActionSource;

	private IWeaponAction primaryAction;
	private IWeaponAction secondaryAction;
	private IWeaponAction reloadAction;

	[SerializeField]
	private MonoBehaviour[] continuousActionSources;

	private IWeaponAction[] continuousActions;

	public void Initialize(IWeaponContext newContext)
	{
		if (hasBeenInitialized) return;

		workingWeaponStats = Instantiate(weaponStats);

		primaryAction = primaryActionSource as IWeaponAction;
		secondaryAction = secondaryActionSource as IWeaponAction;
		reloadAction = reloadActionSource as IWeaponAction;

		continuousActions = continuousActionSources
			.Select(e => e as IWeaponAction)
			.ToArray();

		weaponAmmo = new()
		{
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
			continuousAction.Execute(this, () => OnActionComplete());
		}
	}

	public void OnEnable()
	{
		// Will run even though this weapon has been initialized.
		OnAmmoUpdate(weaponAmmo);

		if (hasBeenInitialized == false) return;

		primaryAction.Initialize(this);
		secondaryAction.Initialize(this);
		reloadAction.Initialize(this);

	}

	public void OnDisable()
	{
		//playerMessageBus.Unsubscribe<OnUpdateAmmo>();

		ownerMessageBus = null;
	}

	public void RequestReload(bool value)
	{
		if (!value) return;

		reloadActionEvent.Invoke();

		StartCoroutine(reloadAction.Execute(this, () => OnActionComplete()));
	}

	public void RequestPrimaryAction(bool value)
	{
		if (!value) return;
		if (weaponAmmo.currentAmmo <= 0 || weaponAmmo.isReloading)
			return;

		if (WeaponStats.fireMode is FireMode.Automatic || weaponStats.fireMode is FireMode.Charged) return;

		StartCoroutine(primaryAction.Execute(this, () => OnActionComplete()));
	}

	public void RequestSecondaryAction(bool value)
	{
		if (!value) return;

		secondaryActionEvent.Invoke();

		StartCoroutine(secondaryAction.Execute(this, () => OnActionComplete()));
	}

	// Holding fire button
	public void RequestPrimaryActionSustain(bool value)
	{
		if (weaponStats.fireMode is FireMode.Single) return;

		if (!primaryAction.IsSustained) return;


		if (value)
		{
			StartCoroutine(primaryAction.StartAction(this, () => OnActionComplete()));
		}
		else
			StartCoroutine(primaryAction.StopAction(this, () => OnActionComplete()));
	}

	// Holding secondary button
	public void RequestSecondaryActionSustain(bool value)
	{
		if (!secondaryAction.IsSustained) return;


		if (value)
		{
			StartCoroutine(secondaryAction.StartAction(this, () => OnActionComplete()));
		}
		else
			StartCoroutine(secondaryAction.StopAction(this, () => OnActionComplete()));
	}

	void OnActionComplete()
	{
		workingWeaponStats.CopyFrom(weaponStats);
	}

	public void ProvideMouseDelta(Vector2 _mouseDelta)
	{
		mouseDelta = _mouseDelta;
	}

	void OnAmmoUpdate(WeaponAmmo weaponAmmo)
	{
		if (ownerMessageBus == null) return;
		ownerMessageBus.Publish(new OnUpdateAmmo { CurrentAmmo = weaponAmmo.currentAmmo, AmmoReserves = weaponAmmo.currentCarriedAmmo});
	}
}
