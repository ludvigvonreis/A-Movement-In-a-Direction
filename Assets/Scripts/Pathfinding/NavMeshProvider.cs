using System.Collections.Generic;
using System.Linq;
using System.Collections;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public struct VisualizationConfig
{
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

public class NavMeshProvider : MonoBehaviour
{
	[SerializeField]
	private VisualizationConfig visualizationConfig = new();

	[Space, SerializeField]
	private Transform goal;
	[SerializeField]
	private Transform enemy;

	[SerializeField]
	private List<NavMeshNode> path = new();
	[SerializeField]
	private List<Vector3> smoothPathPositions = new();

	[SerializeField]
	private List<Vector3> simplePathPositions = new();

	private List<Vector3> rawPathPositions = new();

	private int pathIndex = 0;
	private Vector3 lastGoal;
	private bool reachedGoal = false;

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

	void Update()
	{
		if (Application.isPlaying)
			UpdatePath();
	}

	void Start()
	{
		if (Application.isPlaying)
			StartCoroutine(MoveCoroutine());
	}


	[Button("Start moving")]
	void Move()
	{
		if (simplePathPositions == null || simplePathPositions.Count == 0 || reachedGoal)
			return;

		enemy.position = simplePathPositions[pathIndex];
		pathIndex++;

		if (pathIndex >= simplePathPositions.Count)
		{
			reachedGoal = true;
			pathIndex = 0;
		}
	}

	IEnumerator MoveCoroutine()
	{
		while (true)
		{
			Move();

			yield return new WaitForSeconds(0.5f);
		}
	}

	[Button("Update Path")]
	void UpdatePath()
	{
		if (Application.isPlaying && goal.position == lastGoal)
			return;

		path = navMesh.AStar(enemy.position, goal.position);
		if (path.Count > 0)
		{
			smoothPathPositions = SmoothPath(path, enemy.position, goal.position);

			pathIndex = 0;
			lastGoal = goal.position;
			reachedGoal = false;
		}
	}

	List<Vector3> SmoothPath(List<NavMeshNode> nodePath, Vector3 start, Vector3 goal)
	{
		List<Vector3> path = new();
		float tolerance = 1.5f;
		float offsetDistance = 1f;

		Vector3 previousPoint = start;

		foreach (var polygon in nodePath)
		{
			Vector3 closestPointOnEdge = polygon.vertices
				.SelectMany((v, i) =>
				{
					// Get the edge from this vertex to the next (wrap around)
					Vector3 a = v;
					Vector3 b = polygon.vertices[(i + 1) % polygon.vertices.Count()];

					// Project previousPoint onto the edge segment
					Vector3 ab = b - a;
					float t = Vector3.Dot(previousPoint - a, ab) / ab.sqrMagnitude;
					t = Mathf.Clamp01(t); // clamp to segment
					Vector3 pointOnEdge = a + ab * t;

					return new[] { pointOnEdge };
				})
				.OrderBy(p => Vector3.Distance(p, previousPoint))
				.First();

			// Offset slightly toward the centroid
			Vector3 directionToCenter = (polygon.Centroid - closestPointOnEdge).normalized;
			Vector3 safePoint = closestPointOnEdge + directionToCenter * offsetDistance;

			path.Add(safePoint);
			previousPoint = safePoint;
		}

		// Finally, add the goal
		path.Add(goal);
		path[0] = start;

		rawPathPositions = path;

		// Simplify path with Ramer–Douglas–Peucker.
		simplePathPositions = RDP(path, tolerance);
		simplePathPositions = Simplify(simplePathPositions, 2);

		smoothPathPositions = GenerateSpline(simplePathPositions);

		return smoothPathPositions;
	}

	public static List<Vector3> Simplify(List<Vector3> points, int maxLookAhead = 3)
	{

		if (points == null || points.Count < 3)
			return new List<Vector3>(points);

		List<Vector3> simplified = new()
		{
			points[0]
		};

		int currentIndex = 0;

		while (currentIndex < points.Count - 1)
		{
			Vector3 currentPos = simplified[^1];
			bool foundShortcut = false;

			// Look ahead through segments to find the farthest reachable point
			for (int lookAhead = Mathf.Min(maxLookAhead, points.Count - currentIndex - 2); lookAhead >= 1; lookAhead--)
			{
				int segmentEndIndex = currentIndex + lookAhead + 1;

				// Test direct connection to the segment endpoint first
				if (!Physics.Linecast(currentPos, points[segmentEndIndex]))
				{
					simplified.Add(points[segmentEndIndex]);
					currentIndex = segmentEndIndex;
					foundShortcut = true;
					break;
				}

				// If direct to endpoint is blocked, try points along the segment
				int segmentStartIndex = currentIndex + lookAhead;
				Vector3 segmentStart = points[segmentStartIndex];
				Vector3 segmentEnd = points[segmentEndIndex];
				Vector3 segmentDir = (segmentEnd - segmentStart).normalized;
				float segmentLength = Vector3.Distance(segmentStart, segmentEnd);

				// Test multiple points along the segment (including midpoint)
				float[] testPoints = { 0.5f, 0.25f, 0.75f, 0.1f, 0.9f }; // Midpoint first, then others

				foreach (float t in testPoints)
				{
					Vector3 testPoint = segmentStart + segmentDir * (segmentLength * t);

					if (!Physics.Linecast(currentPos, testPoint))
					{
						// Found a valid shortcut point along the segment
						simplified.Add(testPoint);
						// Move current index to the start of this segment
						currentIndex = segmentStartIndex;
						foundShortcut = true;
						break;
					}
				}

				if (foundShortcut) break;
			}

			// If no shortcut found, move to next point
			if (!foundShortcut)
			{
				currentIndex++;
				if (currentIndex < points.Count)
					simplified.Add(points[currentIndex]);
			}
		}

		return simplified;
	}

	public static List<Vector3> GenerateSpline(List<Vector3> controlPoints, int pointsPerSegment = 10)
	{
		var result = new List<Vector3>();

		if (controlPoints == null || controlPoints.Count < 2)
			return result;

		for (int i = 0; i < controlPoints.Count - 1; i++)
		{
			Vector3 p0 = i == 0 ? controlPoints[i] : controlPoints[i - 1];
			Vector3 p1 = controlPoints[i];
			Vector3 p2 = controlPoints[i + 1];
			Vector3 p3 = (i + 2 < controlPoints.Count) ? controlPoints[i + 2] : controlPoints[i + 1];

			for (int j = 0; j <= pointsPerSegment; j++)
			{
				float t = j / (float)pointsPerSegment;
				Vector3 point = CentripetalCR(p0, p1, p2, p3, t);
				if (result.Count == 0 || point != result[^1])
					result.Add(point);
			}
		}

		return result;
	}

	private static Vector3 CentripetalCR(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		float alpha = 0.5f;

		float t0 = 0f;
		float t1 = t0 + Mathf.Pow(Vector3.Distance(p0, p1), alpha);
		float t2 = t1 + Mathf.Pow(Vector3.Distance(p1, p2), alpha);
		float t3 = t2 + Mathf.Pow(Vector3.Distance(p2, p3), alpha);

		// Prevent zero-length segments
		if (Mathf.Approximately(t1, t0)) t1 += 0.0001f;
		if (Mathf.Approximately(t2, t1)) t2 += 0.0001f;
		if (Mathf.Approximately(t3, t2)) t3 += 0.0001f;

		float t_ = Mathf.Lerp(t1, t2, t);

		Vector3 A1 = Lerp(p0, p1, t0, t1, t_);
		Vector3 A2 = Lerp(p1, p2, t1, t2, t_);
		Vector3 A3 = Lerp(p2, p3, t2, t3, t_);

		Vector3 B1 = Lerp(A1, A2, t0, t2, t_);
		Vector3 B2 = Lerp(A2, A3, t1, t3, t_);

		return Lerp(B1, B2, t1, t2, t_);
	}

	private static Vector3 Lerp(Vector3 p0, Vector3 p1, float t0, float t1, float t)
	{
		if (Mathf.Approximately(t1, t0))
			return p0;
		return (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
	}

	static List<Vector3> RDP(List<Vector3> points, float tolerance)
	{
		if (points == null || points.Count < 2)
			return new List<Vector3>(points); // nothing to simplify

		// Find the point with the maximum distance from the line
		float maxDist = 0f;
		int index = -1;
		Vector3 start = points[0];
		Vector3 end = points[^1];

		for (int i = 1; i < points.Count - 1; i++) // skip first and last point
		{
			float dist = DistanceFromLineToPoint(start, end, points[i]);
			if (dist > maxDist)
			{
				maxDist = dist;
				index = i;
			}
		}

		if (maxDist > tolerance && index != -1)
		{
			// Recursive call
			var left = RDP(points.Take(index + 1).ToList(), tolerance);
			var right = RDP(points.Skip(index).ToList(), tolerance);

			// Combine results, avoid duplicate at split
			left.RemoveAt(left.Count - 1);
			left.AddRange(right);

			return left;
		}
		else
		{
			// Just keep start and end points
			return new List<Vector3> { start, end };
		}
	}

	public static float DistanceFromLineToPoint(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
	{
		Vector3 lineDirection = lineEnd - lineStart;
		Vector3 pointDirection = point - lineStart;

		float lineSqrLength = lineDirection.sqrMagnitude;

		if (lineSqrLength < Mathf.Epsilon)
			return pointDirection.magnitude; // line is effectively a point

		// Distance from point to line: |(point - lineStart) x lineDirection| / |lineDirection|
		Vector3 cross = Vector3.Cross(pointDirection, lineDirection);
		return cross.magnitude / Mathf.Sqrt(lineSqrLength);
	}


	void OnDrawGizmos()
	{
		if (navMesh == null)
			return;

		path = navMesh.AStar(enemy.position, goal.position);
		if (path != null && path.Count > 0)
		{
			smoothPathPositions = SmoothPath(path, enemy.position, goal.position);
		}

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

		if (path.Count > 0)
		{
			if (visualizationConfig.ShowDirectPath)
			{
				Gizmos.color = visualizationConfig.DirectPathColor;
				Gizmos.DrawLine(simplePathPositions[0], simplePathPositions[^1]);
			}

			if (visualizationConfig.ShowRawPath)
			{
				Gizmos.color = visualizationConfig.RawPathColor;
				for (int i = 0; i < rawPathPositions.Count - 1; i++)
				{
					Gizmos.DrawLine(rawPathPositions[i], rawPathPositions[i + 1]);
				}
			}
		}

		if (simplePathPositions.Count > 0 && visualizationConfig.ShowSimplePath)
		{

			Gizmos.color = visualizationConfig.SimplePathColor;
			for (int i = 0; i < simplePathPositions.Count - 1; i++)
			{
				Gizmos.DrawLine(simplePathPositions[i], simplePathPositions[i + 1]);
			}
		}

		if (smoothPathPositions.Count > 0 && visualizationConfig.ShowSmoothPath)
		{
			Gizmos.color = visualizationConfig.SmoothPathColor;
			for (int i = 0; i < smoothPathPositions.Count - 1; i++)
			{
				Gizmos.DrawLine(smoothPathPositions[i], smoothPathPositions[i + 1]);
			}
		}

		{
			if (visualizationConfig.ShowGoalPolygon && NavMesh.PositionToNode(goal.position) is NavMeshNode goalNode && goalNode != null)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(goalNode.Centroid, 0.2f);
				Handles.Label(goalNode.Centroid + new Vector3(2, 0, 0), $"Node index: {goalNode.polyIndex}");
			}

			if (visualizationConfig.ShowEnemyPolygon && NavMesh.PositionToNode(enemy.position) is NavMeshNode enemyNode && enemyNode != null)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(enemyNode.Centroid, 0.2f);
				Handles.Label(enemyNode.Centroid + new Vector3(2, 0, 0), $"Node index: {enemyNode.polyIndex}");
			}
		}
	}
}