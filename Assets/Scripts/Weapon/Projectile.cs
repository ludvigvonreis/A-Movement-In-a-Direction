using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile", menuName = "Weapon/Projectile")]
public class Projectile : ScriptableObject 
{
	public float speed = 100f;
	public bool useGravity = true;

	[Space]
	[Header("Damage")]
	public float damage = 1f;
	public AnimationCurve damageFalloff;

	public GameObject projectilePrefab;
	public GameObject projectileHitVFX;
}