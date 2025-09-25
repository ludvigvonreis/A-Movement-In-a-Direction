using System.Collections;
using UnityEngine;

public class WeaponKick : WeaponActionBase
{
	public override bool IsSustained => false;

	public override void Initialize(WeaponBehaviour weapon)
	{
		weapon.primaryActionEvent.AddListener(() => Kick(weapon));

		base.Initialize(weapon);
	}

	public void Kick(WeaponBehaviour weapon)
	{
		weapon.ModelObjectMovement -= new Vector3(0, 0, 1);
	}
}