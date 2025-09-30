using UnityEngine;

public interface IWeaponContext
{
	public void AddCameraShake(Vector3 vector);
	public void ChangeCameraFov(float value, bool animated = false, FovAnimationParams? animParams = null);
	public void ResetCameraFov(bool animated = false, FovAnimationParams? animParams = null);
	public float GetCameraFov();

	public Collider GetOwnerCollider();
}