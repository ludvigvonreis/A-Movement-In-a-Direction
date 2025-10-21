using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NavMeshNode
{
	public int polyIndex;
	public int regionId;
	public Vector3[] vertices;
	public List<int> edges = new();

	[SerializeField]
	private Vector3 centroid;
	[SerializeField]
	private Vector3 normal;

	public Vector3 Centroid
	{
		get
		{
			if (centroid != Vector3.zero) return centroid;

			Vector3 c = Vector3.zero;
			foreach (var v in vertices)
				c += v;
			centroid = c / vertices.Length;

			return centroid;
		}
	}

	public Vector3 Normal
	{
		get
		{
			if (normal != Vector3.zero) return normal;

			normal = Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[1]).normalized;

			return normal;
		}
	}

	public bool TryGetSharedEdge(NavMeshNode other, out (Vector3 a, Vector3 b) edge, float tolerance = 0.001f)
	{
		for (int i = 0; i < vertices.Length; i++)
		{
			Vector3 a1 = vertices[i];
			Vector3 a2 = vertices[(i + 1) % vertices.Length];

			for (int j = 0; j < other.vertices.Length; j++)
			{
				Vector3 b1 = other.vertices[j];
				Vector3 b2 = other.vertices[(j + 1) % other.vertices.Length];

				// check if the edge matches in either direction
				bool sameDir =
					Vector3.Distance(a1, b2) < tolerance &&
					Vector3.Distance(a2, b1) < tolerance;
				bool oppositeDir =
					Vector3.Distance(a1, b1) < tolerance &&
					Vector3.Distance(a2, b2) < tolerance;

				if (sameDir || oppositeDir)
				{
					edge = (a1, a2);
					return true;
				}
			}
		}

		edge = default;
		return false;
	}

}

[System.Serializable]
public class NavMesh
{
	const int RC_MESH_NULL_IDX = 65535;

	//[HideInInspector]
	public List<NavMeshNode> nodes = new();

	private RcPolyMeshData polyMesh;

	private RecastConfig recastConfig;

	public NavMesh(RcPolyMeshData _polyMesh, RecastConfig _recastConfig)
	{
		polyMesh = _polyMesh;
		recastConfig = _recastConfig;

		GenerateGraph();
	}

	void GenerateGraph()
	{
		var mesh = polyMesh;
		int nvp = mesh.nvp;
		var minBounds = mesh.bmin;

		// First pass: create nodes
		for (int i = 0; i < mesh.npolys; i++)
		{
			NavMeshNode node = new()
			{
				polyIndex = i,
				regionId = mesh.regs[i]
			};

			// Collect polygon vertices
			List<Vector3> verts = new List<Vector3>();
			for (int j = 0; j < nvp; j++)
			{
				int idx = mesh.polys[i * nvp * 2 + j];
				if (idx == RC_MESH_NULL_IDX) break;

				Vector3 vert = new Vector3(
					mesh.verts[idx * 3] * recastConfig.cellSize,
					mesh.verts[idx * 3 + 1] * recastConfig.cellHeight,
					mesh.verts[idx * 3 + 2] * recastConfig.cellSize
				) + minBounds;

				verts.Add(vert);
			}

			node.vertices = verts.ToArray();
			nodes.Add(node);
		}

		// Second pass: connect neighbors
		for (int i = 0; i < mesh.npolys; i++)
		{
			NavMeshNode node = nodes[i];

			for (int j = 0; j < nvp; j++)
			{
				int neighborIndex = mesh.polys[i * nvp * 2 + nvp + j];
				if (neighborIndex != RC_MESH_NULL_IDX)
				{
					node.edges.Add(neighborIndex);
				}
			}
		}
	}

	public NavMeshNode PositionToNode(Vector3 position)
	{
		NavMeshNode closest = null;
		float closestDist = float.PositiveInfinity;

		foreach (var node in nodes)
		{
			Plane plane = new(node.Normal, node.vertices[0]);

			// Project the position onto this node's plane
			float distanceToPlane = plane.GetDistanceToPoint(position);
			Vector3 projected = position - node.Normal * distanceToPlane;

			// Check if projected point lies inside polygon
			bool inside = true;
			for (int i = 0; i < node.vertices.Length; i++)
			{
				Vector3 a = node.vertices[i];
				Vector3 b = node.vertices[(i + 1) % node.vertices.Length];
				Vector3 edge = b - a;
				Vector3 toPoint = projected - a;
				Vector3 cross = Vector3.Cross(edge, toPoint);

				if (Vector3.Dot(cross, node.Normal) < -1e-3f)
				{
					inside = false;
					break;
				}
			}

			// Compute distance from position to the plane
			float absDist = Mathf.Abs(distanceToPlane);

			// If inside polygon or closest plane, update
			if (inside && absDist < closestDist)
			{
				closest = node;
				closestDist = absDist;
			}
		}

		return closest;
	}

	public NavMeshNode GetNodeFromIndex(int nodeIndex)
	{
		return nodes[nodeIndex];
	}

	public List<NavMeshNode> AStar(Vector3 origin, Vector3 goal)
	{
		var originNode = PositionToNode(origin);
		var goalNode = PositionToNode(goal);
		if (originNode == null || goalNode == null) return null;

		var open = new PriorityQueue<NavMeshNode>();
		var closed = new HashSet<NavMeshNode>();
		var g = new Dictionary<NavMeshNode, float>();
		var f = new Dictionary<NavMeshNode, float>();
		var cameFrom = new Dictionary<NavMeshNode, NavMeshNode>();

		g[originNode] = 0f;
		f[originNode] = Vector3.Distance(originNode.Centroid, goal);
		open.Enqueue(originNode, f[originNode]);

		while (open.TryDequeue(out var current))
		{
			if (current == goalNode)
				return RetracePath(cameFrom, originNode, goalNode);

			closed.Add(current);

			foreach (int edgeIndex in current.edges)
			{
				var neighbor = GetNodeFromIndex(edgeIndex);
				if (closed.Contains(neighbor)) continue;

				float tentativeG = g[current] + CalculateCost(current, neighbor, goal);

				if (!g.TryGetValue(neighbor, out float oldG) || tentativeG < oldG)
				{
					cameFrom[neighbor] = current;
					g[neighbor] = tentativeG;
					float h = Vector3.Distance(neighbor.Centroid, goal);
					f[neighbor] = tentativeG + h;
					open.Enqueue(neighbor, f[neighbor]);
				}
			}
		}

		return null;
	}

	float CalculateCost(NavMeshNode a, NavMeshNode b, Vector3 goal)
	{
		Vector3 portalMid;

		// Try to use the shared edge if it exists
		if (a.TryGetSharedEdge(b, out var edge))
		{
			portalMid = (edge.a + edge.b) * 0.5f;
		}
		else
		{
			// Fallback: use closest vertices between polygons
			float minDist = float.MaxValue;
			Vector3 bestA = a.Centroid;
			Vector3 bestB = b.Centroid;

			foreach (var va in a.vertices)
			foreach (var vb in b.vertices)
			{
				float d = Vector3.SqrMagnitude(va - vb);
				if (d < minDist)
				{
					minDist = d;
					bestA = va;
					bestB = vb;
				}
			}

			portalMid = (bestA + bestB) * 0.5f;
		}

		float dist = Vector3.Distance(a.Centroid, portalMid);

		Vector3 dirToNext = Vector3.Normalize(portalMid - a.Centroid);
		Vector3 dirToGoal = Vector3.Normalize(goal - a.Centroid);
		float alignment = Mathf.Clamp01(Vector3.Dot(dirToNext, dirToGoal));

		// Penalize turns away from goal direction
		float turnPenalty = 1f - alignment;

		return dist * (1f + 0.5f * turnPenalty);
	}

	static List<NavMeshNode> RetracePath(Dictionary<NavMeshNode, NavMeshNode> cameFrom, NavMeshNode start, NavMeshNode end)
	{
		List<NavMeshNode> path = new();
		NavMeshNode current = end;

		// Include goal node
		path.Add(current);

		while (current != start)
		{
			current = cameFrom[current];
			path.Add(current);
		}

		path.Reverse();

		return path;
	}
}
