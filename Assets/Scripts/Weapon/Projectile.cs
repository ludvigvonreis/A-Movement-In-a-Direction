using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile", menuName = "Weapon/Projectile")]
public class Projectile : ScriptableObject 
{
	public float speed = 100f;
	public bool useGravity = true;
	public float mass = 1f;
	public float radiusOfEffect = 1f;

	[Space]
	[Header("Damage")]
	public float damage = 1f;
	public AnimationCurve damageFalloff;

	public GameObject projectilePrefab;
	public GameObject projectileHitVFX;
}