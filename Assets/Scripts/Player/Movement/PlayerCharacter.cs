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
}

public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;

    public CrouchInput Crouch;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
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
    private float gravity = -90f;

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

    private Stance _stance;

    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedCrouch;
    private Collider[] _uncrouchOverlapResults;

    public void Initialize()
    {
        _stance = Stance.Stand;
        _uncrouchOverlapResults = new Collider[8];

        motor.CharacterController = this;
    }

    public void UpdateInput(CharacterInput characterInput)
    {
        _requestedRotation = characterInput.Rotation;

        _requestedMovement = new Vector3(characterInput.Move.x, 0, characterInput.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        _requestedMovement = characterInput.Rotation * _requestedMovement;

        _requestedJump = _requestedJump || characterInput.Jump;

        _requestedCrouch = characterInput.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None => _requestedCrouch,
            _ => _requestedCrouch,
        };
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        var forward = Vector3.ProjectOnPlane(
            _requestedRotation * Vector3.forward,
            motor.CharacterUp
        );
        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        // Is on ground...
        if (motor.GroundingStatus.IsStableOnGround)
        {
            var groundedMovement = motor.GetDirectionTangentToSurface(
                direction: _requestedMovement,
                surfaceNormal: motor.GroundingStatus.GroundNormal
            );

            var speed = _stance is Stance.Stand ? walkSpeed : crouchSpeed;
            var response = _stance is Stance.Stand ? walkResponse : crouchResponse;

            var targetVelocity = groundedMovement * speed;

            currentVelocity = Vector3.Lerp(
                a: currentVelocity,
                b: targetVelocity,
                t: 1f - Mathf.Exp(-response * deltaTime)
            );

            // Is in air...
        }
        else
        {
			// Move..

			if (_requestedMovement.sqrMagnitude > 0) {
				// Requested movement normalized
				var planarMovement = Vector3.ProjectOnPlane 
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

				var targetPlanarVelocity = currentPlanarMovement + movementForce;

				targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);

				currentVelocity += targetPlanarVelocity - currentPlanarMovement;
			}

            // Gravity
            currentVelocity += gravity * motor.CharacterUp * deltaTime;
        }

        // Jump is requested
        if (_requestedJump)
        {
            _requestedJump = false;

            // Allow character to unstick from ground.
            motor.ForceUnground(0);

            // Calculate minimum vertical speed.
            var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);

            // Add the difference to current velocity.
            currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
        }
    }

    public void UpdateBody(float deltaTime)
    {
        var currentHeight = motor.Capsule.height;
        var normalizedHeight = currentHeight / standHeight;
        var cameraTargetHeight =
            currentHeight
            * (_stance is Stance.Stand ? standCameraTargetHeight : crouchCameraTargetHeight);

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
        if (_requestedCrouch && _stance is Stance.Stand)
        {
            _stance = Stance.Crouch;

            motor.SetCapsuleDimensions(
                radius: motor.Capsule.radius,
                height: crouchHeight,
                yOffset: crouchHeight * 0.5f
            );
        }
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        // Uncrouch..
        if (!_requestedCrouch && _stance is Stance.Crouch)
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
                _stance = Stance.Stand;
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
    ) { }

    public void OnMovementHit(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport
    ) { }

    public void PostGroundingUpdate(float deltaTime) { }

    public void ProcessHitStabilityReport(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        Vector3 atCharacterPosition,
        Quaternion atCharacterRotation,
        ref HitStabilityReport hitStabilityReport
    ) { }

    public Transform GetCameraTarget() => cameraTarget;
}
