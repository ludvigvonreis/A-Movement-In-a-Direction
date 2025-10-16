using UnityEngine;

public class NavMeshProvider : MonoBehaviour
{
	[SerializeField]
	private bool Visualize = true;

	[SerializeField, HideInInspector]
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
	}
}