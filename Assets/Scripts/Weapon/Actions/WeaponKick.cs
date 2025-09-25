using System.Collections;
using UnityEngine;

public class WeaponKick : WeaponActionBase
{
	public override bool IsSustained => false;

	public override IEnumerator Execute(WeaponBehaviour weapon)
	{
		


		return base.Execute(weapon);
	}
}