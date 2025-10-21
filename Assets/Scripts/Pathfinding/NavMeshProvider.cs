using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using static PathProcessing;

[System.Serializable]
public struct VisualizationConfig
{
	public bool ShowVisualization;

	public bool ShowNavMesh;
	public Color NavMeshColor;
	public bool ShowNavGraph;
	public Color NavGraphColor;

	public bool ShowRawPath;
	public Color RawPathColor;
	public bool ShowSimplePath;
	public Color SimplePathColor;
	public bool ShowSmoothPath;
	public Color SmoothPathColor;
	public bool ShowDirectPath;
	public Color DirectPathColor;

	public bool ShowEnemyPolygon;
	public bool ShowGoalPolygon;
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
	public Vector3 currentPosition;
	public Vector3 goalPosition;


	public NavMeshNode[] nodePath;

	public Vector3[] rawPath;
	public Vector3[] simplePath;
	public Vector3[] finalPath;
}

public class NavMeshProvider : MonoBehaviour
{
	[SerializeField]
	private VisualizationConfig visualizationConfig = new();
	[SerializeField]
	private PathProcessingConfig config = new();

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

	NavigationPath SmoothPath(List<NavMeshNode> nodePath, Vector3 start, Vector3 goal)
	{
		List<Vector3> path = new();

		Vector3 previousPoint = start;

		foreach (var polygon in nodePath)
		{
		Vector3 closestPointOnEdge = polygon.vertices
			.SelectMany((v, i) =>
			{
				// Get the edge from this vertex to the next (wrap around)
				Vector3 a = v;
				Vector3 b = polygon.vertices[(i + 1) % polygon.vertices.Length];

				// Project player position onto the edge segment
				Vector3 ab = b - a;
				float t = Vector3.Dot(goal - a, ab) / ab.sqrMagnitude;
				t = Mathf.Clamp01(t); // clamp to segment
				Vector3 pointOnEdge = a + ab * t;

				return new[] { pointOnEdge };
			})
			.OrderBy(p => Vector3.Distance(p, goal))
			.First();

			// Offset slightly toward the centroid
			Vector3 directionToCenter = (polygon.Centroid - closestPointOnEdge).normalized;
			Vector3 safePoint = closestPointOnEdge + directionToCenter * config.VertexEdgeOffset;

			path.Add(safePoint);
			previousPoint = safePoint;
		}

		// Finally, add the goal
		path[0] = start;

		// Only add final goal position if goal is close to the same y level as last node.
		if (Mathf.Abs(path[^1].y - goal.y) < 2f)
			path.Add(goal);

		// Simplify path with Ramer–Douglas–Peucker.
		var simplePathPositions = RDP(path, config.RDPTolerance);

		// Simplyfy path with custom shorcut algorithm.
		simplePathPositions = Simplify(simplePathPositions, config.ShortcutLookAhead);

		// Smooth path with Centripetal Catmull–Rom spline.
		var smoothPathPositions = GenerateSpline(simplePathPositions, config.CRPointsPerSegment);

		return new()
		{
			nodePath = nodePath.ToArray(),
			finalPath = smoothPathPositions.ToArray(),
			currentPosition = start,
			goalPosition = goal,

			rawPath = path.ToArray(),
			simplePath = simplePathPositions.ToArray()
		};
	}

	public NavigationPath? GetPath(Vector3 currentPosition, Vector3 goalPosition)
	{
		var nodePath = navMesh.AStar(currentPosition, goalPosition);
		if (nodePath != null && nodePath.Count > 0)
		{
			return SmoothPath(nodePath, currentPosition, goalPosition);
		}

		return null;
	}

	void DrawNavMesh()
	{
		if (visualizationConfig.ShowVisualization == false) return;

		foreach (var node in navMesh.nodes)
		{
			if (visualizationConfig.ShowNavMesh)
			{

				Color regionColor = visualizationConfig.NavMeshColor;
				Gizmos.color = regionColor;

				var vertices = node.vertices;
				int vertexCount = vertices.Length;
				for (int i = 0; i < vertexCount; i++)
				{
					Gizmos.DrawLine(vertices[i], vertices[(i + 1) % vertexCount]);
				}
			}

			if (visualizationConfig.ShowNavGraph)
			{
				Gizmos.color = visualizationConfig.NavGraphColor;
				foreach (var edge in node.edges)
				{
					Gizmos.DrawLine(navMesh.nodes[edge].Centroid, node.Centroid);
				}
			}
		}
	}


	void OnDrawGizmos()
	{
		DrawNavMesh();
	}

	public void DrawVisualization(NavigationPath navPath)
	{
		if (visualizationConfig.ShowVisualization == false) return;

		if (navMesh == null)
			return;

		if (visualizationConfig.ShowDirectPath)
		{
			Gizmos.color = visualizationConfig.DirectPathColor;
			Gizmos.DrawLine(navPath.simplePath[0], navPath.simplePath[^1]);
		}

		if (visualizationConfig.ShowRawPath)
		{
			Gizmos.color = visualizationConfig.RawPathColor;
			for (int i = 0; i < navPath.rawPath.Length - 1; i++)
			{
				Gizmos.DrawLine(navPath.rawPath[i], navPath.rawPath[i + 1]);
			}
		}

		if (navPath.simplePath.Length > 0 && visualizationConfig.ShowSimplePath)
		{
			Gizmos.color = visualizationConfig.SimplePathColor;
			for (int i = 0; i < navPath.simplePath.Length - 1; i++)
			{
				Gizmos.DrawLine(navPath.simplePath[i], navPath.simplePath[i + 1]);
			}
		}

		if (navPath.finalPath.Length > 0 && visualizationConfig.ShowSmoothPath)
		{
			Gizmos.color = visualizationConfig.SmoothPathColor;
			for (int i = 0; i < navPath.finalPath.Length - 1; i++)
			{
				Gizmos.DrawLine(navPath.finalPath[i], navPath.finalPath[i + 1]);
			}
		}

		{
			if (visualizationConfig.ShowGoalPolygon && NavMesh.PositionToNode(navPath.goalPosition) is NavMeshNode goalNode && goalNode != null)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(goalNode.Centroid, 0.2f);
				Handles.Label(goalNode.Centroid + new Vector3(2, 0, 0), $"Node index: {goalNode.polyIndex}");
			}

			if (visualizationConfig.ShowEnemyPolygon && NavMesh.PositionToNode(navPath.currentPosition) is NavMeshNode enemyNode && enemyNode != null)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(enemyNode.Centroid, 0.2f);
				Handles.Label(enemyNode.Centroid + new Vector3(2, 0, 0), $"Node index: {enemyNode.polyIndex}");
			}
		}
	}
}