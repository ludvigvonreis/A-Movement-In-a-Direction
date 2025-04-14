using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Bullet : ProjectileObject
{
	// Projectile fields.
	private Vector3 direction;
	private Vector3 origin;
	private Projectile projectileStats;
	private WeaponStats weaponStats;


	private Rigidbody Rigidbody;

	public override void Initialize(
		Vector3 _direction,
		Vector3 _origin,
		Projectile _projectileStats,
		WeaponStats _weaponStats
	)
	{
		direction = _direction;
		origin = _origin;
		projectileStats = _projectileStats;
		weaponStats = _weaponStats;

		Rigidbody.useGravity = projectileStats.useGravity;
		Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

		Rigidbody.AddForce(direction * projectileStats.speed, ForceMode.Impulse);
	}

	void Awake()
	{
		Rigidbody = GetComponent<Rigidbody>();
	}

	void Update()
	{
		var currentDistance = Vector3.Distance(origin, transform.position);

		if (currentDistance > weaponStats.range)
		{
			Destroy(gameObject);
		}
	}

	void OnCollisionEnter(Collision collision)
	{
		OnHit(ObjectMaterial.Metal, collision.gameObject, collision);
	}

	public override void OnHit(ObjectMaterial material, GameObject hitObject, Collision collision)
	{
		// VFX.
		{
			var contactPoint = collision.GetContact(0);
			// Forward vector of VFX object should be the normal of the hit
			var rotation = Quaternion.LookRotation(contactPoint.normal);

			// Spawn vfx object, expect that it handles its own removal and activation
			Instantiate(projectileStats.projectileHitVFX, contactPoint.point, rotation);
		}

		// Damage handling.
		// FIXME: Implement
		{
			
		}
	}
}