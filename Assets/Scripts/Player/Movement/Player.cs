using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private PlayerCharacter playerCharacter;

    [SerializeField]
    private PlayerCamera playerCamera;

	[SerializeField]
	private PlayerWeaponHandler playerWeaponHandler;

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
        CameraInput cameraInput = new() { Look = input.Mouse.ReadValue<Vector2>() };
        playerCamera.UpdateRotation(cameraInput);

        CharacterInput characterInput = new()
		{
            Rotation    = playerCamera.transform.rotation,
            Move        = input.Move.ReadValue<Vector2>(),
            Jump        = input.Jump.WasPressedThisFrame(),
			JumpSustain = input.Jump.IsPressed(),
			Sprint      = input.Sprint.IsPressed(),
            Crouch      = input.Crouch.WasPressedThisFrame() ? CrouchInput.Toggle : CrouchInput.None,
        };
        playerCharacter.UpdateInput(characterInput);
        playerCharacter.UpdateBody(deltaTime);

		PlayerWeaponInput playerWeaponInput = new()
		{
			PrimaryAction          = input.Primary.WasPressedThisFrame(),
			PrimaryActionSustain   = input.Primary.IsPressed(),
			SecondaryAction        = input.Secondary.WasPressedThisFrame(),
			SecondaryActionSustain = input.Secondary.IsPressed(),
			Scroll                 = input.Scroll.ReadValue<Vector2>(),
			Reload                 = input.Reload.WasPressedThisFrame(),
		};

		playerWeaponHandler.UpdateInput(playerWeaponInput);
    }

    void LateUpdate()
    {
        playerCamera.UpdatePosition(playerCharacter.GetCameraTarget());
    }
}
