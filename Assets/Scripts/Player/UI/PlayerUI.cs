using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
	[SerializeField] private Canvas canvas;

	// Elements
	[SerializeField]
	private TextMeshProUGUI ammoCounter;

	[Space]
	[SerializeField]
	private Image staminaBar;

	[SerializeField, Range(0, 1), Tooltip("Amount of fill that the dashbar will have at full.")]
	private float staminaBarFillAmount = 0.5f;

	[SerializeField]
	private Color staminaBarColor;

	[SerializeField]
	private Color staminaBarAltColor;

	public void Initialize(MessageBus playerMessageBus)
	{
		playerMessageBus.Subscribe<OnUpdateAmmo>((x) => UpdateAmmoCounter(x.CurrentAmmo, x.AmmoReserves));
		playerMessageBus.Subscribe<OnUpdateStamina>((x) => UpdateStaminaBar(x.Stamina, x.MaxStamina));
	}

	void OnValidate()
	{
		staminaBar.color = staminaBarColor;
	}

	void UpdateAmmoCounter(int currentAmmo, int ammoReserves)
	{
		ammoCounter.text = string.Format("{0} <color=#ccc><size=70%>{1}</size></color>", currentAmmo, ammoReserves);
	}


	void UpdateStaminaBar(float value, float max)
	{
		var fillPercentage = value / max;

		if (fillPercentage <= 0.3f)
		{
			staminaBar.color = staminaBarAltColor;
		}
		else
		{
			staminaBar.color = staminaBarColor;
		}

		staminaBar.fillAmount = Mathf.Min(fillPercentage, 1) * staminaBarFillAmount;
	}
}