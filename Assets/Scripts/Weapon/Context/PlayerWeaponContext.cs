using UnityEngine;

public class PlayerWeaponContext : IWeaponContext
{
	private readonly PlayerCamera playerCamera;

    public PlayerWeaponContext(PlayerCamera _playerCamera)
    {
		playerCamera = _playerCamera;
    }

	public void AddCameraShake(float intensity)
	{
		playerCamera.Shake(intensity);
	}
}
