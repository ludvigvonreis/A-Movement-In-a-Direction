using UnityEngine;

public class NullWeaponContext : IWeaponContext
{
	public void AddCameraShake(float intensity) {}

	public void ChangeCameraFov(float value, bool animated = false, FovAnimationParams? animParams = null) {}

	public float GetCameraFov()
	{
		return 90;
	}

	public Collider GetOwnerCollider()
	{
		return null;
	}

	public void ResetCameraFov(bool animated = false, FovAnimationParams? animParams = null) {}
}