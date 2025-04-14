using System.Collections;
using UnityEngine;

public class ProjectileWeaponBehaviour : WeaponBehaviour<WeaponStats>
{
	private bool _requestPrimaryAction;
	private bool _requestSecondaryAction;

	public override void PrimaryAction()
	{
		var projectileStats = WeaponStats.projectile;
		var projectilePrefab = projectileStats.projectilePrefab;

		// Spawn the required amount of projectiles for this weapon cycle.
		for (int i = 0; i < WeaponStats.projectileCount; i++)
		{

			// Spread.
			var rotation = Quaternion.AngleAxis(
					Random.Range(0f, WeaponStats.spreadAngle), Random.onUnitSphere
				) * projectileFirePoint.forward;

			// Summon projectile
			var spawnedProjectile = Instantiate
			(
				original: projectilePrefab,
				position: projectileFirePoint.position,
				rotation: Quaternion.Euler(rotation)
			);

			var projectileObject = spawnedProjectile.GetComponent<ProjectileObject>();
			projectileObject.Initialize(
				rotation, 
				projectileFirePoint.position, 
				projectileStats, 
				WeaponStats
			);
		}
	}

	public override void SecondaryAction()
	{
		Debug.Log("Hello im secondary action");
	}

	void Update()
	{
		_requestPrimaryAction = WeaponStats.fireMode is FireMode.Single ?
			Input.GetKeyDown(KeyCode.G) :
			Input.GetKey(KeyCode.G);

		// TODO: Secondary action is often aiming, but can be another thing. 
		// implement that for both stats and stuff.
		_requestSecondaryAction = Input.GetKeyDown(KeyCode.H);
	}

	void Start()
	{
		StartCoroutine(EventHandler());
	}

	IEnumerator EventHandler()
	{
		while (true)
		{
			yield return new WaitForSeconds(WeaponStats.Firerate);

			if (_requestPrimaryAction)
			{
				PrimaryAction();
			}

			if (_requestSecondaryAction)
			{
				SecondaryAction();
			}
		}
	}
}