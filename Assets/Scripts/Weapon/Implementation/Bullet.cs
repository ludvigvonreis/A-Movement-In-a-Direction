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

		if (currentDistance > weaponStats.range) {
			Destroy(gameObject);
		}
	}

	void OnCollisionEnter(Collision collision) {
		Destroy(gameObject);
	}
}