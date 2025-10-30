using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct NeighbourEntry
{
	public int neighborIndex;
	public Vector3 a;
	public Vector3 b;
}

[System.Serializable]
public class NavMeshNode
{
	// Index from recast. also this nodes index in the mesh.
	public int polyIndex;

	public Vector3[] vertices;
	//public List<int> edges = new();
	public List<OffMeshLink> offMeshLinks = new();
	public List<NeighbourEntry> neighbours = new();

	// Data from recast.
	public int flags;
	public int regionId;


	[SerializeField]
	private Vector3 centroid;
	[SerializeField]
	private Vector3 normal;
	[SerializeField]
	private float surfaceArea;

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

	public float SurfaceArea
	{
		get
		{
			if (surfaceArea > 0f) return surfaceArea;

			float area = 0f;
			if (vertices.Length < 3) return 0f;

			Vector3 v0 = vertices[0];
			for (int i = 1; i < vertices.Length - 1; i++)
			{
				Vector3 v1 = vertices[i];
				Vector3 v2 = vertices[i + 1];
				area += Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
			}

			surfaceArea = area;
			return surfaceArea;
		}
	}

	public bool TryGetSharedEdge(NavMeshNode other, out (Vector3 a, Vector3 b) edge)
	{
		foreach (var entry in neighbours)
		{
			if (entry.neighborIndex == other.polyIndex)
			{
				edge = (entry.a, entry.b);
				return true;
			}
		}

		edge = default;
		return false;
	}
}

[System.Serializable]
public struct OffMeshLink
{
	public int startPolyIndex;   // polygon the link starts from
	public int endPolyIndex;     // polygon the link goes to
	public Vector3 startPos;     // optional exact start point
	public Vector3 endPos;       // optional exact end point
	public bool bidirectional;   // true = two-way, false = one-way
	public float cost;           // optional cost override for A*
}

[System.Serializable]
public class NavMesh
{
	const int RC_MESH_NULL_IDX = 65535;
	const int FLAG_RAMP = 1 << 0;

	[SerializeField]
	private float jumpLinkMinSurfaceArea = 5f;
	[SerializeField]
	private float jumpLinkMinDistance = 0.5f;
	[SerializeField]
	private float jumpLinkMaxDistance = 15f;

	[Space, Space]
	public List<NavMeshNode> nodes = new();

	[SerializeField]
	private RcPolyMeshData polyMesh;
	private NavMeshConfig recastConfig;

	public NavMesh(RcPolyMeshData _polyMesh, NavMeshConfig _recastConfig)
	{
		polyMesh = _polyMesh;
		recastConfig = _recastConfig;

		GenerateGraph();
	}

	public void GenerateGraph()
	{
		var mesh = polyMesh;
		int nvp = mesh.nvp;
		var minBounds = mesh.bmin;

		nodes = new();

		// First pass: create nodes
		for (int i = 0; i < mesh.npolys; i++)
		{
			NavMeshNode node = new()
			{
				polyIndex = i,
				regionId = mesh.regs[i],
				flags = 0
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

			// Generate flags for polgons.

			// Mark polygon as ramp.
			if (Vector3.Dot(node.Normal, Vector3.up) < 1)
			{
				node.flags |= FLAG_RAMP;
			}


			nodes.Add(node);
		}

		// Second pass: connect neighbors
		for (int i = 0; i < mesh.npolys; i++)
		{
			NavMeshNode node = nodes[i];

			// Determine actual vertex count of this polygon
			int vertexCount = 0;
			for (; vertexCount < nvp; vertexCount++)
				if (mesh.polys[i * nvp * 2 + vertexCount] == RC_MESH_NULL_IDX)
					break;

			for (int j = 0; j < vertexCount; j++)
			{
				int neighborIndex = mesh.polys[i * nvp * 2 + nvp + j];
				if (neighborIndex == RC_MESH_NULL_IDX)
					continue;

				int v0 = mesh.polys[i * nvp * 2 + j];
				int v1 = mesh.polys[i * nvp * 2 + ((j + 1) % vertexCount)];

				// Determine actual vertex count of neighbor
				int neighborVertexCount = 0;
				for (; neighborVertexCount < nvp; neighborVertexCount++)
					if (mesh.polys[neighborIndex * nvp * 2 + neighborVertexCount] == RC_MESH_NULL_IDX)
						break;

				bool shared = false;
				for (int k = 0; k < neighborVertexCount; k++)
				{
					int nv0 = mesh.polys[neighborIndex * nvp * 2 + k];
					int nv1 = mesh.polys[neighborIndex * nvp * 2 + ((k + 1) % neighborVertexCount)];

					if ((nv0 == v1 && nv1 == v0) || (nv0 == v0 && nv1 == v1))
					{
						shared = true;
						break;
					}
				}

				if (shared)
				{
					Vector3 vpos0 = new Vector3(
						mesh.verts[v0 * 3] * recastConfig.cellSize,
						mesh.verts[v0 * 3 + 1] * recastConfig.cellHeight,
						mesh.verts[v0 * 3 + 2] * recastConfig.cellSize
					) + minBounds;

					Vector3 vpos1 = new Vector3(
						mesh.verts[v1 * 3] * recastConfig.cellSize,
						mesh.verts[v1 * 3 + 1] * recastConfig.cellHeight,
						mesh.verts[v1 * 3 + 2] * recastConfig.cellSize
					) + minBounds;

					node.neighbours.Add(new() { neighborIndex = neighborIndex, a = vpos0, b = vpos1 });
				}
			}
		}


		// Third pass: find off-mesh links
		for (int i = 0; i < mesh.npolys; i++)
		{
			NavMeshNode node = nodes[i];
			if (node.SurfaceArea < jumpLinkMinSurfaceArea) continue;

			for (int j = 0; j < node.vertices.Length - 1; j++)
			{
				var midPoint = (node.vertices[j] + node.vertices[j + 1]) / 2;
				var direction = (midPoint - node.Centroid).normalized;

				if (Physics.Raycast(midPoint + direction * 1f, Vector3.down, out RaycastHit hit, jumpLinkMaxDistance, 1 << 3))
				{
					if (hit.distance < jumpLinkMinDistance) continue;

					if (PositionToNode(hit.point) is NavMeshNode other && node != other)
					{
						//Debug.DrawRay(midPoint + direction, Vector3.down * jumpLinkMaxDistance, Color.green, 1);
						bool linkExists = node.offMeshLinks.Any(l => l.endPolyIndex == other.polyIndex);
						if (!linkExists)
						{
							OffMeshLink link = new()
							{
								startPolyIndex = i,
								endPolyIndex = other.polyIndex,
								startPos = midPoint,
								endPos = hit.point,
								bidirectional = false,
								cost = Mathf.Max(hit.distance * 4, 30)
							};

							node.offMeshLinks.Add(link);
						}
					}
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

	public bool Raycast(Vector3 start, Vector3 end, out Vector3 hitPosition, float verticalThreshold = 0.5f)
	{
		hitPosition = end;
		Vector3 dir = end - start;
		float maxDist = dir.magnitude;
		if (maxDist < 1e-6f) return false;
		dir /= maxDist;

		bool blocked = false;
		float closestT = maxDist;

		if (Mathf.Abs(start.y - end.y) > 1f) return true;

		foreach (var node in nodes)
		{
			for (int j = 0; j < node.vertices.Length - 1; j++)
			{
				var v0 = node.vertices[j];
				var v1 = node.vertices[j + 1];

				// Edge line in XZ plane
				Vector2 p0 = new Vector2(v0.x, v0.z);
				Vector2 p1 = new Vector2(v1.x, v1.z);
				Vector2 rayStartXZ = new Vector2(start.x, start.z);
				Vector2 rayEndXZ = new Vector2(end.x, end.z);

				if (GeometryLib.LineSegmentIntersection(rayStartXZ, rayEndXZ, p0, p1, out var intersectionXZ))
				{
					// Compute Y at intersection along ray
					float t = ((intersectionXZ.x - rayStartXZ.x) / (rayEndXZ.x - rayStartXZ.x + 1e-6f));
					float yAtIntersection = Mathf.Lerp(start.y, end.y, t);

					// Check if intersection is within vertical bounds of edge + threshold
					float minY = Mathf.Min(v0.y, v1.y) - verticalThreshold;
					float maxY = Mathf.Max(v0.y, v1.y) + verticalThreshold;

					if (yAtIntersection < minY || yAtIntersection > maxY)
						continue; // edge is too high/low, ignore

					// Check if edge is a shared/passable edge
					bool isShared = node.neighbours.Any(e =>
						(e.a == v0 && e.b == v1) ||
						(e.a == v1 && e.b == v0)
					);

					if (!isShared)
					{
						// Blocked by non-shared edge
						blocked = true;

						// Optional: store hit position along ray
						hitPosition = new Vector3(intersectionXZ.x, yAtIntersection, intersectionXZ.y);
						closestT = Vector3.Distance(start, hitPosition);
					}
				}
			}
		}

		return blocked;
	}

	public List<NavMeshNode> AStar(Vector3 origin, Vector3 goal)
	{
		var originNode = PositionToNode(origin);
		var goalNode = PositionToNode(goal);
		if (originNode == null || goalNode == null) return null;

		// Links between nodes used for path reconstruction.
		var cameFrom = new Dictionary<NavMeshNode, NavMeshNode>();

		// Nodes currently in queue to be processed.
		var open = new PriorityQueue<NavMeshNode>();

		// Nodes we have processed
		var closed = new HashSet<NavMeshNode>();

		// For node{key} this is the cost from start to node.
		var g = new Dictionary<NavMeshNode, float>
		{
			[originNode] = 0f
		};

		// For node{key} value is the best guess for how much it would 
		// cost to find the goal.
		// ----
		// Start is initialized as the euclidian distance to goal from 
		// center of polygon.
		var f = new Dictionary<NavMeshNode, float>
		{
			[originNode] = Vector3.Distance(originNode.Centroid, goal)
		};

		open.Enqueue(originNode, f[originNode]);

		while (open.TryDequeue(out var current))
		{
			// We have reached our goal.
			if (current == goalNode)
				return RetracePath(cameFrom, originNode, goalNode);

			closed.Add(current);

			// Process neighbours of current node.
			foreach (var neighbourEntry in current.neighbours)
			{
				var neighbor = nodes[neighbourEntry.neighborIndex];
				if (closed.Contains(neighbor)) continue;

				// Calculate cost addition of this neighbour.
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

			// Process offmesh links of current node.
			foreach (var link in current.offMeshLinks)
			{
				var linkEnd = GetNodeFromIndex(link.endPolyIndex);
				if (closed.Contains(linkEnd)) continue;

				float tentativeG = g[current] + link.cost;

				if (!g.TryGetValue(linkEnd, out float oldG) || tentativeG < oldG)
				{
					cameFrom[linkEnd] = current;
					g[linkEnd] = tentativeG;
					float h = Vector3.Distance(linkEnd.Centroid, goal);
					f[linkEnd] = tentativeG + h;
					open.Enqueue(linkEnd, f[linkEnd]);
				}
			}
		}

		return null;
	}

	static float CalculateCost(NavMeshNode a, NavMeshNode b, Vector3 goal)
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

	private NavMeshNode FindContainingNode(Vector3 point)
	{
		foreach (var n in nodes)
			if (GeometryLib.PointInPolygon(point, n.vertices))
				return n;
		return null;
	}
}
