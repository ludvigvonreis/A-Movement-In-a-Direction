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
		foreach (var node in nodes)
		{
			bool inside = true;
			for (int i = 0; i < node.vertices.Length; i++)
			{
				Vector3 a = node.vertices[i];
				Vector3 b = node.vertices[(i + 1) % node.vertices.Length];

				Vector3 edge = b - a;
				Vector3 toPoint = position - a;
				Vector3 cross = Vector3.Cross(edge, toPoint);

				if (Vector3.Dot(cross, node.Normal) < -1e-3f)
				{
					inside = false;
					break; // early exit
				}
			}

			if (inside)
				return node;
		}

		return null;
	}

	public NavMeshNode GetNodeFromIndex(int nodeIndex)
	{
		return nodes[nodeIndex];
	}

	public List<NavMeshNode> AStar(Vector3 origin, Vector3 goal)
	{
		NavMeshNode originNode = PositionToNode(origin);
		NavMeshNode goalNode = PositionToNode(goal);

		//Debug.Log(originNode + " :: " + goalNode);

		if (originNode == null || goalNode == null) return null;

		List<NavMeshNode> openSet = new() { originNode };
		HashSet<NavMeshNode> closed = new();

		var gScores = new Dictionary<int, float>();
		var fScores = new Dictionary<int, float>();
		var cameFrom = new Dictionary<NavMeshNode, NavMeshNode>();

		gScores[originNode.polyIndex] = 0f;
		fScores[originNode.polyIndex] = Vector3.Distance(origin, goal);

		while (openSet.Count > 0)
		{
			NavMeshNode current = openSet[0];
			foreach (var node in openSet)
			{
				if (fScores.TryGetValue(node.polyIndex, out float f) && f < fScores[current.polyIndex])
					current = node;
			}

			if (current.polyIndex == goalNode.polyIndex)
				return RetracePath(cameFrom, originNode, goalNode);

			openSet.Remove(current);
			closed.Add(current);

			foreach (var edgeIndex in current.edges)
			{
				var neighbor = GetNodeFromIndex(edgeIndex);
				if (closed.Contains(neighbor))
					continue;

				float currentG = gScores.TryGetValue(current.polyIndex, out float g) ? g : float.PositiveInfinity;
				float tentativeG = currentG + Vector3.Distance(current.Centroid, neighbor.Centroid);

				if (!gScores.ContainsKey(neighbor.polyIndex) || tentativeG < gScores[neighbor.polyIndex])
				{
					cameFrom[neighbor] = current;
					gScores[neighbor.polyIndex] = tentativeG;
					fScores[neighbor.polyIndex] = tentativeG + Vector3.Distance(neighbor.Centroid, goal);

					if (!openSet.Contains(neighbor))
						openSet.Add(neighbor);
				}
			}
		}

		return null;
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
