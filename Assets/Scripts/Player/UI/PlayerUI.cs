using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
	[SerializeField] private Canvas canvas;

	// Elements
	[SerializeField] private TextMeshProUGUI ammoCounter;
	[SerializeField] private Image dashBar;

	private Coroutine dashBarCoroutine;

	public void Initialize(MessageBus playerMessageBus)
	{
		playerMessageBus.Subscribe<OnUpdateAmmo>((x) => UpdateAmmoCounter(x.CurrentAmmo, x.AmmoReserves));
		playerMessageBus.Subscribe<OnUpdateDashTimeout>((x) => UpdateDashBar(x.Timeout));
	}

	void UpdateAmmoCounter(int currentAmmo, int ammoReserves)
	{
		ammoCounter.text = string.Format("{0} <color=#ccc><size=70%>{1}</size></color>", currentAmmo, ammoReserves);
	}


	void UpdateDashBar(float value)
	{
		Color c = dashBar.color;
		dashBar.fillAmount = Mathf.Min(value, 1) * 0.25f;
	}
}