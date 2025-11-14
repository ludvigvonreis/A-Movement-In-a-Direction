using System;
using System.Collections;
using UnityEngine;

public interface IWeaponAction {
	IEnumerator Execute(WeaponBehaviour weapon, Action onComplete);
	IEnumerator StartAction(WeaponBehaviour weapon, Action onComplete);
	IEnumerator StopAction(WeaponBehaviour weapon, Action onComplete);
	bool IsSustained { get; }

	void Initialize(WeaponBehaviour weapon);
}


public abstract class WeaponActionBase : MonoBehaviour, IWeaponAction
{
	public virtual IEnumerator Execute(WeaponBehaviour weapon, Action onComplete) { onComplete?.Invoke(); yield return null; }
	public virtual IEnumerator StartAction(WeaponBehaviour weapon, Action onComplete) { onComplete?.Invoke(); yield return null; }
	public virtual IEnumerator StopAction(WeaponBehaviour weapon, Action onComplete) { onComplete?.Invoke(); yield return null; }

	public virtual void Initialize(WeaponBehaviour weapon) {}

	public abstract bool IsSustained { get; }
}