using System.Collections;
using UnityEngine;

public interface IWeaponAction {
	IEnumerator Execute(WeaponBehaviour weapon);
	IEnumerator StartAction(WeaponBehaviour weapon);
	IEnumerator StopAction(WeaponBehaviour weapon);
	bool IsSustained { get; }
}


public abstract class WeaponActionBase : MonoBehaviour, IWeaponAction
{
	public virtual IEnumerator Execute(WeaponBehaviour weapon) { yield return null; }
	public virtual IEnumerator StartAction(WeaponBehaviour weapon) { yield return null; }
	public virtual IEnumerator StopAction(WeaponBehaviour weapon) { yield return null; }
	public abstract bool IsSustained { get; }
}