using KinematicCharacterController;
using UnityEngine;

public class EnemyCharacter : MonoBehaviour, ICharacterController
{
	[SerializeField]
	private KinematicCharacterMotor motor;

	[SerializeField]
	private Transform target;
	private Vector3 lastTargetPosition;

	// Character settings
	[SerializeField]
	private float gravity = -90f;

	[SerializeField]
	private float speed = 10f;
	float acceleration = 20f;
	float recoverMultiplier = 3f;      // stronger acceleration while recovering from push
	float pushThreshold = 0.5f;        // treat base velocity above this as a push
	float baseVelocityDecay = 4f;      // how fast external push decays
	float stopDistance = 0.15f;
	float minVelocityToKeep = 0.01f;   // below this, zero the velocity

	// Pathing
	[SerializeField]
	private NavigationPath NavPath;
	[SerializeField]
	private int currentPathIndex = 0;

	private NavMeshProvider navMeshProvider;
	[SerializeField]
	private Vector3 _requestedMovement;

	void Start()
	{
		motor.CharacterController = this;
		navMeshProvider = NavMeshProvider.Instance;
		
		// Init navigation.
		NavPath = new();
		FetchPath();
	}

	void Update()
	{
		if (Vector3.Distance(target.position, lastTargetPosition) > 1f)
		{
			FetchPath();
			lastTargetPosition = target.position;
		}
	}

	void FetchPath()
	{
		if (navMeshProvider.GetPath(transform.position, target.position, NavPath))
		{
			if (NavPath.path.Length < 1)
			{
				currentPathIndex = 0;
				return;
			}

			// Only reset path if the goal moved significantly
			//if (Vector3.Distance(path.goalPosition, NavPath.goalPosition) > 0.1f)
			{
				// Find the closest point on the new path to our current position
				float closestDistance = float.MaxValue;
				int closestIndex = 0;
				for (int i = 0; i < NavPath.path.Length; i++)
				{
					float dist = Vector3.Distance(transform.position, NavPath.path[i]);
					if (dist < closestDistance)
					{
						closestDistance = dist;
						closestIndex = i;
					}
				}

				currentPathIndex = closestIndex;
				return;
			}
		}
	}

	public void AfterCharacterUpdate(float deltaTime) { }

	public void BeforeCharacterUpdate(float deltaTime) { }

	public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
	{
		var forward = Vector3.ProjectOnPlane
		(
			_requestedMovement,
			motor.CharacterUp
		);
		if (forward != Vector3.zero)
			currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
	}

	public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
	{
		currentVelocity = CalculateVelocityAlongPath(currentVelocity, deltaTime);

		_requestedMovement = currentVelocity;

		currentVelocity += deltaTime * gravity * motor.CharacterUp;
	}

	Vector3 CalculateVelocityAlongPath(Vector3 currentVelocity, float deltaTime)
	{
		bool atGoal = Vector3.Distance(motor.TransientPosition, target.position) < 2f;

		if (!atGoal)
		{
			// no path
			if (NavPath.path == null || NavPath.path.Length == 0)
			{
				// gently remove external velocity if any
				motor.BaseVelocity = Vector3.MoveTowards(motor.BaseVelocity, Vector3.zero, baseVelocityDecay * deltaTime);
				currentVelocity = Vector3.zero;
			}

			Vector3 targetPos = NavPath.path[currentPathIndex];
			Vector3 toTarget = targetPos - motor.TransientPosition;
			float distance = toTarget.magnitude;

			// close enough -> stop and optionally advance or snap
			if (distance < stopDistance)
			{
				currentVelocity = Vector3.zero;
				motor.BaseVelocity = Vector3.MoveTowards(motor.BaseVelocity, Vector3.zero, baseVelocityDecay * deltaTime);

				if (currentPathIndex < NavPath.path.Length - 1)
				{
					currentPathIndex++;
				}
				else
				{
					// final goal reached. snap to avoid being shoved around by collisions.
					motor.SetTransientPosition(targetPos);
				}
				return currentVelocity;
			}

			// compute desired velocity safely
			Vector3 desiredVelocity;
			// if very close, compute exact needed velocity to avoid overshoot
			if (distance < speed * deltaTime)
				desiredVelocity = toTarget / Mathf.Max(deltaTime, 1e-6f);
			else
				desiredVelocity = toTarget.normalized * speed;

			// project to ground tangent only when stable on ground
			if (motor.GroundingStatus.IsStableOnGround)
			{
				desiredVelocity = motor.GetDirectionTangentToSurface(desiredVelocity, motor.GroundingStatus.GroundNormal).normalized * desiredVelocity.magnitude;
			}

			// detect external pushes (e.g., bullets, collisions, moving platforms)
			Vector3 external = motor.BaseVelocity;
			bool beingPushed = external.sqrMagnitude > (pushThreshold * pushThreshold);

			if (beingPushed)
			{
				// decay the external push so it doesn't persist forever
				motor.BaseVelocity = Vector3.MoveTowards(motor.BaseVelocity, Vector3.zero, baseVelocityDecay * deltaTime);

				// recover aggressively toward desired velocity
				float recoverAccel = acceleration * recoverMultiplier;
				currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, recoverAccel * deltaTime);
			}
			else
			{
				// normal movement with smooth acceleration
				currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, acceleration * deltaTime);
			}

			// prevent tiny jitter velocities
			if (currentVelocity.sqrMagnitude < (minVelocityToKeep * minVelocityToKeep))
				currentVelocity = Vector3.zero;
		}
		else
		{
			currentVelocity = Vector3.zero;
		}

		return currentVelocity;
	}

	public bool IsColliderValidForCollisions(Collider coll) { return true; }

	public void OnDiscreteCollisionDetected(Collider hitCollider) { }

	public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

	public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

	public void PostGroundingUpdate(float deltaTime) { }

	public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

	void OnDrawGizmos()
	{
		if (!Application.isPlaying) return;

		if (!NavPath.Equals(default(NavigationPath)))
		{
			navMeshProvider.DrawVisualization(NavPath);
		}
	}
}