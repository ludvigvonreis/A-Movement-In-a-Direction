using UnityEngine;

public enum ObjectMaterial {
	Metal,
	Flesh,
}

public abstract class ProjectileObject : MonoBehaviour {
	public abstract void Initialize(
		Vector3 _direction, 
		Vector3 _origin, 
		Projectile projectileStats,
		WeaponStats weaponStats
	);

	public abstract void OnHit(ObjectMaterial material, GameObject hitObject, Collision collision);
}