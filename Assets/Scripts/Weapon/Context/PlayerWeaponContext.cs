using UnityEngine;

public class PlayerWeaponContext : IWeaponContext
{
	private readonly PlayerCamera playerCamera;
	private readonly PlayerCharacter playerCharacter;

	public PlayerWeaponContext(PlayerCamera _playerCamera, PlayerCharacter _playerCharacter)
	{
		playerCamera = _playerCamera;
		playerCharacter = _playerCharacter;
	}

	public void AddCameraShake(float intensity)
	{
		playerCamera.Shake(intensity);
	}

	public void ChangeCameraFov(float value, bool animated = false, FovAnimationParams? animParams = null)
	{
		playerCamera.ChangeCameraFov(value, animated, animParams);
	}

	public Collider GetOwnerCollider()
	{
		return playerCharacter.GetComponent<Collider>();
	}

	public void ResetCameraFov(bool animated = false, FovAnimationParams? animParams = null)
	{
		playerCamera.ResetCameraFov(animated, animParams);
	}
}
