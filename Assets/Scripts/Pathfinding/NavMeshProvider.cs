using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using static PathProcessing;

[System.Serializable]
public struct VisualizationConfig
{
	public bool ShowVisualization;

	public bool ShowNavMesh;
	public Color NavMeshColor;
	public bool ShowNavGraph;
	public Color NavGraphColor;

	public bool ShowPath;
	public Color PathColor;

	public bool ShowDirectPath;
	public Color DirectPathColor;
}

[System.Serializable]
public struct PathProcessingConfig
{
	public float VertexEdgeOffset;

	public float RDPTolerance;

	public int ShortcutLookAhead;

	public int CRPointsPerSegment;
}

[System.Serializable]
public struct NavigationPath
{
	public Vector3 goalPosition;
	public Vector3 startPosition;

	// Path of nodes.
	public int[] nodePath;
	// Path of positions.
	public Vector3[] path;
}

[ExecuteAlways]
public class NavMeshProvider : MonoBehaviour
{
	[SerializeField]
	private NavMeshConfig navMeshConfig = new();
	[SerializeField]
	public VisualizationConfig visualizationConfig = new();
	[SerializeField]
	private PathProcessingConfig pathProcessingConfig = new();

	public Transform A;
	public Transform B;

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

	[Button("Generate Mesh")]
	public void GenerateMesh()
	{
		var _polyMesh = GetComponent<CreateNavMesh>().GenerateRecastMesh(navMeshConfig);
		navMesh = new(_polyMesh, navMeshConfig);
	}

	Vector3[] SmoothPath(List<NavMeshNode> nodePath, Vector3 start, Vector3 goal)
	{
		// What flags to keep, ie ramps.
		int mask = ~(1 << 0);
		var flags = nodePath.Select(x => x.flags).ToList();

		List<Vector3> path = new()
		{
			start
		};

		for (int i = 0; i < nodePath.Count; i++)
		{
			var node = nodePath[i];

			for (int j = 0; j < nodePath.Count; j++)
			{
				if (node.TryGetSharedEdge(nodePath[j], out var edge))
				{
					Vector3 AB = edge.a + edge.b;
					path.Add(AB / 2f);
				}
			}
		}

		path.Add(goal);
		path = Shortcutting(path, mask, navMesh);
		path = DensifyPath(path);
		for (int i = 0; i < path.Count; i++)
		{
			Vector3 p = path[i];
			// Cast down from 2 units above
			if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 100f, 1 << 3))
			{
				path[i] = hit.point; // snap point to ground
			}
		}
		path = StringPulling(path, pathProcessingConfig.RDPTolerance, flags, mask);

		return path.ToArray();
	}

	public NavigationPath? GetPath(Vector3 currentPosition, Vector3 goalPosition)
	{
		var nodePath = navMesh.AStar(currentPosition, goalPosition);
		if (nodePath != null && nodePath.Count > 0)
		{
			var path = SmoothPath(nodePath, currentPosition, goalPosition);

			return new NavigationPath
			{
				startPosition = currentPosition,
				goalPosition = goalPosition,

				path = path,
				nodePath = nodePath.Select(x => x.polyIndex).ToArray(),
			};
		}

		return null;
	}

	[Button("Regen Graph")]
	void RegenGraph()
	{
		navMesh.GenerateGraph();
	}

	void OnDrawGizmos()
	{
		DrawNavMesh();
		// var APos = A.position;
		// var BPos = B.position;

		// APos.y = BPos.y;

		// if (navMesh.Raycast(APos, BPos, out var hit))
		// {
		// 	Gizmos.color = Color.red;
		// 	// Gizmos.DrawSphere(hit, 0.05f);
		// }
		// else
		// {
		// 	Gizmos.color = Color.blue;
		// }


		// Gizmos.DrawLine(APos, BPos);

		// //new Vector3(5, 4.5f, -18),

		// var pathsss = GetPath(A.position, B.position);
		// if (pathsss.HasValue) DrawPath(pathsss.Value.finalPath, Color.yellow);
	}

	public void DrawVisualization(NavigationPath navPath)
	{
		if (visualizationConfig.ShowVisualization == false) return;

		if (navMesh == null)
			return;

		if (visualizationConfig.ShowDirectPath)
			DrawPath(new[] { navPath.path[0], navPath.path[^1] }, visualizationConfig.DirectPathColor);

		if (navPath.path.Length > 0 && visualizationConfig.ShowPath)
			DrawPath(navPath.path, visualizationConfig.PathColor);
	}

	public void DrawPath(Vector3[] pathPoints, Color color)
	{
		if (pathPoints == null || pathPoints.Length < 2)
			return;

		for (int i = 0; i < pathPoints.Length - 1; i++)
		{
			Gizmos.color = color;
			Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
			Gizmos.color = Color.black;
			Gizmos.DrawSphere(pathPoints[i], 0.05f);
		}
	}

	public void DrawNodeInfo(NavMeshNode node, string labelPrefix)
	{
		if (node == null)
			return;

		Gizmos.color = Color.red;
		Gizmos.DrawSphere(node.Centroid, 0.2f);

		Handles.Label(node.Centroid + new Vector3(2, 0, 0), $"{labelPrefix} index: {node.polyIndex}");
	}

	void DrawNavMesh()
	{
		if (visualizationConfig.ShowVisualization == false) return;

		// Enable depth test (draw only when visible)
		Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

		foreach (var node in navMesh.nodes)
		{
			if (visualizationConfig.ShowNavMesh)
			{
				Handles.color = visualizationConfig.NavMeshColor;

				var vertices = node.vertices;
				int vertexCount = vertices.Length;

				// Draw polygon outline
				for (int i = 0; i < vertexCount; i++)
				{
					Vector3 v0 = vertices[i];
					Vector3 v1 = vertices[(i + 1) % vertexCount];
					Handles.DrawAAPolyLine(2f, v0, v1);
				}
			}

			// --- NAVGRAPH CONNECTIONS ---
			if (visualizationConfig.ShowNavGraph)
			{
				Handles.color = visualizationConfig.NavGraphColor;

				foreach (var neighbourEntry in node.neighbours)
				{
					if (neighbourEntry.neighborIndex >= 0 && neighbourEntry.neighborIndex < navMesh.nodes.Count)
					{
						Vector3 a = node.Centroid;
						Vector3 b = navMesh.nodes[neighbourEntry.neighborIndex].Centroid;
						Handles.DrawAAPolyLine(2f, a, b);
					}
				}
			}
		}

		// Reset zTest
		Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
	}
}