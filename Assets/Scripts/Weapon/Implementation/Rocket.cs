using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Rocket : ProjectileObject
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
		Rigidbody.mass = projectileStats.mass;

		Rigidbody.AddForce(direction * projectileStats.speed, ForceMode.Impulse);

		transform.forward = _direction;
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

		// Rocket stuff
		{
			float radius = projectileStats.radiusOfEffect;
			var hits = Physics.OverlapSphere(transform.position, radius);

			foreach (var hit in hits)
			{
				hit.transform.gameObject.TryGetComponent<Rigidbody>(out var rigidbody);

				var distance = Vector3.Distance(hit.transform.position, transform.position);
				var force = (1 - distance / radius) * 65f;

				if (rigidbody == null)
				{
					hit.transform.gameObject.TryGetComponent<IKnockbackable>(out var knockbackable);

					if (knockbackable == null) continue;

					var knockbackVelocity = CalculateExplosionVelocity(transform.position, hit.transform.position, radius, force, 3f);
					knockbackable.AddKnockback(knockbackVelocity);

					continue;
				};
				var propWeightCompensation = 5f;
				rigidbody.AddExplosionForce(force * propWeightCompensation, transform.position, radius, 3f);
			}
		}

		Destroy(gameObject);
	}

	public static Vector3 CalculateExplosionVelocity(Vector3 explosionPosition, Vector3 targetPosition, float radius, float force, float upwardsModifier = 0f)
	{
		Vector3 direction = targetPosition - explosionPosition;
		float distance = direction.magnitude;

		if (distance > radius || distance == 0f)
			return Vector3.zero;

		// Normalize the direction vector
		direction /= distance;

		// Apply upwards modifier (simulates a lift from explosion)
		if (upwardsModifier != 0f)
		{
			Vector3 upwards = Vector3.up * upwardsModifier;
			direction += upwards;
			direction.Normalize();
		}

		// Force is scaled by distance from explosion
		float scaledForce = (1f - (distance / radius)) * force;

		// Return the calculated velocity vector
		return direction * scaledForce;
	}
}