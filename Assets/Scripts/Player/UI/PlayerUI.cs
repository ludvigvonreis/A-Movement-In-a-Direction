using TMPro;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
	[SerializeField] private Canvas canvas;

	// Elements
	[SerializeField] private TextMeshProUGUI ammoCounter;

	public void Initialize(MessageBus playerMessageBus)
	{
		playerMessageBus.Subscribe<OnUpdateAmmo>((x) => UpdateAmmoCounter(x.CurrentAmmo, x.AmmoReserves));
	}

	void UpdateAmmoCounter(int currentAmmo, int ammoReserves)
	{
		ammoCounter.text = string.Format("{0} <color=#ccc><size=70%>{1}</size></color>", currentAmmo, ammoReserves);
	}
}