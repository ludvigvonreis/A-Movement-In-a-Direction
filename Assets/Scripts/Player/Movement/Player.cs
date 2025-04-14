using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private PlayerCharacter playerCharacter;

    [SerializeField]
    private PlayerCamera playerCamera;

    private PlayerInputActions playerInputActions;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();

        playerCharacter.Initialize();
        playerCamera.Initialize(playerCharacter.GetCameraTarget());
    }

    void OnDestroy()
    {
        playerInputActions.Dispose();
    }

    void Update()
    {
        var input = playerInputActions.Gameplay;
        var deltaTime = Time.deltaTime;

        // Get mouse input and move camera.
        CameraInput cameraInput = new CameraInput { Look = input.Mouse.ReadValue<Vector2>() };
        playerCamera.UpdateRotation(cameraInput);

        CharacterInput characterInput = new CharacterInput
        {
            Rotation    = playerCamera.transform.rotation,
            Move        = input.Move.ReadValue<Vector2>(),
            Jump        = input.Jump.WasPressedThisFrame(),
			JumpSustain = input.Jump.IsPressed(),
            Crouch      = input.Crouch.WasPressedThisFrame() ? CrouchInput.Toggle : CrouchInput.None,
        };
        playerCharacter.UpdateInput(characterInput);
        playerCharacter.UpdateBody(deltaTime);
    }

    void LateUpdate()
    {
        playerCamera.UpdatePosition(playerCharacter.GetCameraTarget());
    }
}
