using System;
using System.Linq;
using UnityEngine;

[Serializable]
public struct StringToFloat {
	public string name;
	public float value;
}

public class EnemyHealth : MonoBehaviour, IDamageable
{
	[SerializeField]
	private float health = 100;
	public float Health => health;

	[SerializeField]
	private bool isDead = false;
	public bool IsDead => isDead;

	[SerializeField]
	private StringToFloat[] bodyPartToDamageMultiplier;

	public void TakeDamage(float amount, string hitLocation)
	{
		if (isDead) return;

		var entry = bodyPartToDamageMultiplier.FirstOrDefault(x => 
			hitLocation.Split('_')[0].Equals(x.name, StringComparison.OrdinalIgnoreCase)
		);

		if (!string.IsNullOrEmpty(entry.name)) {
			float value = entry.value;
			amount *= value;
		}

		health -= amount;

		// Set health to 0 if its lower than 0.
		if (health <= 0) {	
			health = 0;

			Die();
		}
	}

	public void Die() {
		isDead = true;
		Destroy(gameObject);
	}
}