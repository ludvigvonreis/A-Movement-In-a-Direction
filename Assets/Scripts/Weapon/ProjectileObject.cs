using UnityEngine;

public abstract class ProjectileObject : MonoBehaviour {
	public abstract void Initialize(
		Vector3 _direction, 
		Vector3 _origin, 
		Projectile projectileStats,
		WeaponStats weaponStats
	);
}