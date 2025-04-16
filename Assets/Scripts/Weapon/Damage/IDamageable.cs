using UnityEngine;

interface IDamageable 
{
	void TakeDamage(float amount, /*GameObject source,*/ string hitLocation);
}