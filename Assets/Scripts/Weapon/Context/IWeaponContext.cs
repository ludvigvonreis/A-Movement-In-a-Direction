using UnityEngine;

public interface IWeaponContext 
{
	public void AddCameraShake(float intensity);
	public void ChangeCameraFov(float value, bool animated = false, FovAnimationParams? animParams = null);
	public void ResetCameraFov(bool animated = false, FovAnimationParams? animParams = null);
}