using System.Collections.Generic;
using NaughtyAttributes;

//using NaughtyAttributes;
using UnityEngine;

//[ExecuteInEditMode]
public class NavMeshProvider : MonoBehaviour
{
	[SerializeField]
	private Transform goal;
	[SerializeField]
	private List<Vector3> path = new();

	[SerializeField]
	private Transform enemy;
	public float speed = 3f;
	public float reachThreshold = 0.1f;
	public float pathUpdateRate = 0.2f; // how often to recalc path in seconds

	private int pathIndex = 0;
	private float pathTimer = 0f;

	[SerializeField]
	private bool Visualize = true;

	[SerializeField]
	private NavMesh navMesh;
	public NavMesh NavMesh => navMesh;

	public static NavMeshProvider instance;

	public static NavMeshProvider Instance
	{
		get
		{
			if (instance == null)
				instance = FindFirstObjectByType<NavMeshProvider>();

			return instance;
		}
	}

	public void CreateFromRecastData(RcPolyMeshData _polyMesh, RecastConfig _recastConfig)
	{
		navMesh = new(_polyMesh, _recastConfig);
	}

	[Button("Update Path")]
	public void Update()
	{
		var newPath = navMesh.AStar(enemy.position, goal.position);
		print(newPath);

		if (newPath != null && newPath.Count > 0)
		{
			path = newPath;
			pathIndex = 0;
		}

		FollowPath();
	}

	void FollowPath()
	{
		if (path == null || path.Count == 0 || pathIndex >= path.Count)
			return; // no valid path yet, keep trying next update

		Vector3 target = path[pathIndex];
		Vector3 dir = target - enemy.position;

		if (dir.magnitude < reachThreshold)
		{
			pathIndex++;
		}
		else
		{
			enemy.position += dir.normalized * speed * Time.deltaTime;

			if (dir != Vector3.zero)
			{
				Quaternion targetRot = Quaternion.LookRotation(dir);
				enemy.rotation = Quaternion.Slerp(enemy.rotation, targetRot, 10f * Time.deltaTime);
			}
		}
	}

	void OnDrawGizmos()
	{
		if (navMesh == null || Visualize == false)
			return;

		Color[] colors = { Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan };

		foreach (var node in navMesh.nodes)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(node.Centroid, 0.2f);

			Color regionColor = colors[node.regionId % colors.Length];
			Gizmos.color = regionColor;

			var vertices = node.vertices;
			int vertexCount = vertices.Length;
			for (int i = 0; i < vertexCount; i++)
			{
				Gizmos.DrawLine(vertices[i], vertices[(i + 1) % vertexCount]);
			}

			Gizmos.color = Color.white;
			foreach (var edge in node.edges)
			{
				Gizmos.DrawLine(navMesh.nodes[edge].Centroid, node.Centroid);
			}
		}

		if (path.Count > 0)
		{
			Gizmos.color = Color.black;
			for (int i = 0; i < path.Count - 1; i++)
			{
				Gizmos.DrawLine(path[i], path[i + 1]);
			}
		}
	}
}