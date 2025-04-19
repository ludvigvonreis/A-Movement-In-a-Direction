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

	public void ChangeCameraFov(float value, bool animated = false, FovAnimationParams? animParams = null)
	{
		playerCamera.ChangeCameraFov(value, animated, animParams);
	}


	public void ResetCameraFov(bool animated = false, FovAnimationParams? animParams = null)
	{
		playerCamera.ResetCameraFov(animated, animParams);
	}
}
