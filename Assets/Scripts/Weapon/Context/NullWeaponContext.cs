using UnityEngine;

public class NullWeaponContext : IWeaponContext
{
	public void AddCameraShake(float intensity) {}

	public void ChangeCameraFov(float value, bool animated = false, FovAnimationParams? animParams = null) {}

	public Collider GetOwnerCollider()
	{
		return null;
	}

	public void ResetCameraFov(bool animated = false, FovAnimationParams? animParams = null) {}
}