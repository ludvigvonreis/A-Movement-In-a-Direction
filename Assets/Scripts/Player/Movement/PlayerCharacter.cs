using System;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.UI;

public enum CrouchInput
{
	None,
	Toggle,
}

public enum Stance
{
	Stand,
	Crouch,
	Slide,
}

public struct CharacterState
{
	public Stance Stance;
	public bool Grounded;
	public Vector3 Velocity;
}

public struct CharacterInput
{
	public Quaternion Rotation;
	public Vector2 Move;
	public bool Jump;
	public bool JumpSustain;
	public bool Sprint;

	public CrouchInput Crouch;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController, IKnockbackable
{
	[SerializeField]
	private KinematicCharacterMotor motor;

	[SerializeField]
	private Transform cameraTarget;

	[SerializeField]
	private Transform root;

	[Space]
	[SerializeField]
	private float walkSpeed = 15f;
	[SerializeField]
	private float sprintSpeed = 25f;

	[SerializeField]
	private float crouchSpeed = 7f;

	[SerializeField]
	private float crouchResponse = 25f;

	[SerializeField]
	private float walkResponse = 20f;

	[Space]
	[SerializeField]
	private float airSpeed = 15f;

	[SerializeField]
	private float airAcceleration = 25f;

	[Space]
	[SerializeField]
	private float jumpSpeed = 10f;

	[SerializeField]
	[Range(0f, 1f)]
	private float jumpSustainGravity = 0.4f;

	[SerializeField]
	private float gravity = -90f;

	[Space]
	[SerializeField]
	private float slideStartSpeed = 25f;

	[SerializeField]
	private float slideEndSpeed = 15f;

	[SerializeField]
	private float slideFriction = 0.8f;

	[SerializeField]
	private float slideSteerAcceleration = 5f;

	[SerializeField]
	private float slideGravity = -90f;

	[Space]
	[SerializeField]
	private float standHeight = 2f;

	[SerializeField]
	private float crouchHeight = 1f;

	[SerializeField]
	private float standCameraTargetHeight = 0.9f;

	[SerializeField]
	private float crouchCameraTargetHeight = 0.7f;

	[SerializeField]
	private float crouchHeightResponse = 15f;

	private CharacterState _state;
	private CharacterState _lastState;
	private CharacterState _tempState;

	private Quaternion _requestedRotation;
	private Vector3 _requestedMovement;
	private Vector3 _requestedKnockback;
	private bool _requestedSprint;
	private bool _requestedJump;
	private bool _requestedSustainedJump;
	private bool _requestedCrouch;
	private Collider[] _uncrouchOverlapResults;

	public void Initialize()
	{
		_state.Stance = Stance.Stand;
		_uncrouchOverlapResults = new Collider[8];

		motor.CharacterController = this;
	}

	public void UpdateInput(CharacterInput characterInput)
	{
		_requestedRotation = characterInput.Rotation;

		_requestedMovement = new Vector3(characterInput.Move.x, 0, characterInput.Move.y);
		_requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
		_requestedMovement = characterInput.Rotation * _requestedMovement;
		_requestedSprint = characterInput.Sprint;

		_requestedJump = _requestedJump || characterInput.Jump;
		_requestedSustainedJump = characterInput.JumpSustain;

		_requestedCrouch = characterInput.Crouch switch
		{
			CrouchInput.Toggle => !_requestedCrouch,
			CrouchInput.None => _requestedCrouch,
			_ => _requestedCrouch,
		};
	}

	public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
	{
		var forward = Vector3.ProjectOnPlane
		(
			_requestedRotation * Vector3.forward,
			motor.CharacterUp
		);
		if (forward != Vector3.zero)
			currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
	}

	public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
	{
		var isGrounded = motor.GroundingStatus.IsStableOnGround;

		// Is on ground...
		if (isGrounded)
		{
			var groundedMovement = motor.GetDirectionTangentToSurface
			(
				direction: _requestedMovement,
				surfaceNormal: motor.GroundingStatus.GroundNormal
			);


			// Start sliding.
			{
				var moving = groundedMovement.sqrMagnitude > 0f;
				var crouching = _state.Stance is Stance.Crouch;
				var wasStanding = _lastState.Stance is Stance.Stand;
				var wasInAir = !_lastState.Grounded;
				if (moving && crouching && (wasStanding || wasInAir))
				{
					_state.Stance = Stance.Slide;

					if (wasInAir) {
						currentVelocity = Vector3.ProjectOnPlane
						(
							vector: _lastState.Velocity,
							planeNormal: motor.GroundingStatus.GroundNormal
						);
					}

					var slideSpeed = Mathf.Max(slideStartSpeed, currentVelocity.magnitude);

					currentVelocity = motor.GetDirectionTangentToSurface
					(
						direction: currentVelocity,
						surfaceNormal: motor.GroundingStatus.GroundNormal
					) * slideSpeed;
				}
			}

			// Move.
			if (_state.Stance is Stance.Stand || _state.Stance is Stance.Crouch)
			{

				var requestedSpeed = _requestedSprint ? sprintSpeed : walkSpeed;
				var speed = _state.Stance is Stance.Stand ? requestedSpeed : crouchSpeed;

				var response = _state.Stance is Stance.Stand ? walkResponse : crouchResponse;

				var targetVelocity = groundedMovement * speed;

				currentVelocity = Vector3.Lerp
				(
					a: currentVelocity,
					b: targetVelocity,
					t: 1f - Mathf.Exp(-response * deltaTime)
				);
			}
			// Continue Sliding.
			else
			{	
				// Friction.
				currentVelocity -= currentVelocity * (slideFriction * deltaTime);

				// Slope.
				{
					var force = Vector3.ProjectOnPlane
					(
						vector: -motor.CharacterUp, 
						planeNormal: motor.GroundingStatus.GroundNormal
					) * slideGravity;

					currentVelocity -= force;
				}

				// Steer.
				{
					var currentSpeed = currentVelocity.magnitude;
					var targetVelocity = groundedMovement * currentSpeed;
					var steerForce = (targetVelocity - currentVelocity) * slideSteerAcceleration * deltaTime;
					// Add steerforce but dont change velocity.
					currentVelocity += steerForce;
					currentVelocity = Vector3.ClampMagnitude(currentVelocity, currentSpeed);
				}

				// Stop.
				if (currentVelocity.magnitude < slideEndSpeed)
				{
					_state.Stance = Stance.Crouch;
				}
			}

			// Is in air...
		}
		else
		{
			// Move..

			if (_requestedMovement.sqrMagnitude > 0)
			{
				// Requested movement normalized
				var planarMovement =
					Vector3.ProjectOnPlane
					(
						vector: _requestedMovement,
						planeNormal: motor.CharacterUp
					) * _requestedMovement.magnitude;

				// Current movement
				var currentPlanarMovement = Vector3.ProjectOnPlane
				(
					vector: currentVelocity,
					planeNormal: motor.CharacterUp
				);

				var movementForce = planarMovement * airAcceleration * deltaTime;

				if (currentPlanarMovement.magnitude < airSpeed) {
					var targetPlanarVelocity = currentPlanarMovement + movementForce;

					targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);
					movementForce = targetPlanarVelocity - currentPlanarMovement;
				}
				else if (Vector3.Dot(currentPlanarMovement, movementForce) > 0f) {
					var constrainedMovementForce = Vector3.ProjectOnPlane
					(
						vector: movementForce,
						planeNormal: currentPlanarMovement.normalized
					);

					movementForce = constrainedMovementForce;
				}

				// Prevent air-climbing.
				if (motor.GroundingStatus.FoundAnyGround) {
					// If moving in the same direction as velocity
					if (Vector3.Dot(movementForce, currentVelocity) > 0f) {
						var obstructionNormal = Vector3.Cross
						(
							motor.CharacterUp, 
							Vector3.Cross
							(
								motor.CharacterUp,
								motor.GroundingStatus.GroundNormal
							)
						).normalized;
					}
				}


				currentVelocity += movementForce;
			}

			// Gravity
			var effectiveGravity = gravity;
			var verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
			if (_requestedSustainedJump && verticalSpeed > 0f)
				effectiveGravity *= jumpSustainGravity;

			currentVelocity += effectiveGravity * motor.CharacterUp * deltaTime;
		}

		// Jump is requested
		if (_requestedJump && isGrounded)
		{
			_requestedJump = false;
			_requestedCrouch = false;

			// Allow character to unstick from ground.
			motor.ForceUnground(0);

			// Calculate minimum vertical speed.
			var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
			var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);

			// Add the difference to current velocity.
			currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
		}

		// Add knockback.
		currentVelocity += _requestedKnockback;
		_requestedKnockback = Vector3.zero;
	}

	public void UpdateBody(float deltaTime)
	{
		var currentHeight = motor.Capsule.height;
		var normalizedHeight = currentHeight / standHeight;
		var cameraTargetHeight =
			currentHeight
			* ((_state.Stance is Stance.Stand) ? standCameraTargetHeight : crouchCameraTargetHeight);

		var rootTargetScale = new Vector3(1f, normalizedHeight, 1f);

		cameraTarget.localPosition = Vector3.Lerp(
			a: cameraTarget.localPosition,
			b: new Vector3(0f, cameraTargetHeight, 0f),
			t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
		);

		root.localScale = Vector3.Lerp(
			a: root.localScale,
			b: rootTargetScale,
			t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
		);
	}

	public void AfterCharacterUpdate(float deltaTime)
	{
		// Crouch..
		if (_requestedCrouch && (_state.Stance is Stance.Stand))
		{
			_state.Stance = Stance.Crouch;

			motor.SetCapsuleDimensions(
				radius: motor.Capsule.radius,
				height: crouchHeight,
				yOffset: crouchHeight * 0.5f
			);
		}

		// Update state to use current values.
		_state.Grounded = motor.GroundingStatus.IsStableOnGround;

		_state.Velocity = motor.Velocity;
		// Store the temp state as last state after last state has been used during this frame.
		_lastState = _tempState;
	}

	public void BeforeCharacterUpdate(float deltaTime)
	{
		_tempState = _state;
		// Uncrouch..
		if (!_requestedCrouch && _state.Stance is Stance.Crouch)
		{
			motor.SetCapsuleDimensions(
				radius: motor.Capsule.radius,
				height: standHeight,
				yOffset: standHeight * 0.5f
			);

			var pos = motor.TransientPosition;
			var rot = motor.TransientRotation;
			var res = _uncrouchOverlapResults;
			var mask = motor.CollidableLayers;
			if (motor.CharacterOverlap(pos, rot, res, mask, QueryTriggerInteraction.Ignore) > 0)
			{
				_requestedCrouch = true;

				motor.SetCapsuleDimensions(
					radius: motor.Capsule.radius,
					height: crouchHeight,
					yOffset: crouchHeight * 0.5f
				);
			}
			else
			{
				_state.Stance = Stance.Stand;
			}
		}
	}

	public bool IsColliderValidForCollisions(Collider coll) => true;

	public void OnDiscreteCollisionDetected(Collider hitCollider) { }

	public void OnGroundHit(
		Collider hitCollider,
		Vector3 hitNormal,
		Vector3 hitPoint,
		ref HitStabilityReport hitStabilityReport
	)
	{ }

	public void OnMovementHit(
		Collider hitCollider,
		Vector3 hitNormal,
		Vector3 hitPoint,
		ref HitStabilityReport hitStabilityReport
	)
	{ }

	public void PostGroundingUpdate(float deltaTime)
	{
		if (!motor.GroundingStatus.IsStableOnGround && _state.Stance is Stance.Slide)
		{
			_state.Stance = Stance.Crouch;
		}

	}

	public void ProcessHitStabilityReport(
		Collider hitCollider,
		Vector3 hitNormal,
		Vector3 hitPoint,
		Vector3 atCharacterPosition,
		Quaternion atCharacterRotation,
		ref HitStabilityReport hitStabilityReport
	)
	{ }

	public Transform GetCameraTarget() => cameraTarget;

	public void AddKnockback(Vector3 force)
	{	
		_requestedKnockback = force;
	}
}
