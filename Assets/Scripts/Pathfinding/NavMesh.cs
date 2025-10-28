using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class NavMeshNode
{
	public int polyIndex;
	public int regionId;
	public Vector3[] vertices;
	public List<int> edges = new();
	public List<OffMeshLink> offMeshLinks = new List<OffMeshLink>();
	public int flags;

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

			for (int j = 0; j < nvp; j++)
			{
				int neighborIndex = mesh.polys[i * nvp * 2 + nvp + j];
				if (neighborIndex != RC_MESH_NULL_IDX)
				{
					node.edges.Add(neighborIndex);
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

	public bool Raycast(Vector3 start, Vector3 end, out Vector3 hitPosition)
	{
		hitPosition = end;
		var current = FindContainingNode(start);
		if (current == null)
			return true; // start outside mesh

		Vector3 dir = end - start;
		float remainingDist = dir.magnitude;
		dir.Normalize();

		Vector3 pos = start;

		// Safety loop to prevent infinite recursion
		for (int steps = 0; steps < 64; steps++)
		{
			if (current == null)
				return true;

			// Check if end point lies inside current polygon
			if (PointInPolygon(end, current.vertices))
				return false;

			// Find which edge is crossed first
			if (TryFindCrossedEdge(pos, end, current, out (Vector3 a, Vector3 b) crossedEdge, out float t))
			{
				Vector3 intersection = pos + dir * (remainingDist * t);

				// Find neighboring polygon across that edge
				NavMeshNode neighbor = null;
				foreach (var n in nodes)
				{
					if (n == current) continue;
					if (current.TryGetSharedEdge(n, out var shared, 0.001f) &&
						NearlyEqualEdge(shared, crossedEdge))
					{
						neighbor = n;
						break;
					}
				}

				if (neighbor == null)
				{
					// Edge has no neighbor → ray exits mesh
					hitPosition = intersection;
					return true;
				}

				// Continue from intersection into neighbor
				current = neighbor;
				pos = intersection;
				remainingDist = Vector3.Distance(pos, end);
				dir = (end - pos).normalized;
			}
			else
			{
				// Didn't cross any edge, must be inside polygon
				return false;
			}
		}

		// Safety exit
		hitPosition = pos;
		return true;
	}
	private NavMeshNode FindContainingNode(Vector3 point)
	{
		foreach (var n in nodes)
			if (PointInPolygon(point, n.vertices))
				return n;
		return null;
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
			foreach (int edgeIndex in current.edges)
			{
				var neighbor = GetNodeFromIndex(edgeIndex);
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

	private static bool NearlyEqualEdge((Vector3 a, Vector3 b) e1, (Vector3 a, Vector3 b) e2, float tol = 0.01f)
	{
		return (Vector3.Distance(e1.a, e2.a) < tol && Vector3.Distance(e1.b, e2.b) < tol) ||
			   (Vector3.Distance(e1.a, e2.b) < tol && Vector3.Distance(e1.b, e2.a) < tol);
	}
	
	private static bool PointInPolygon(Vector3 point, Vector3[] verts)
	{
		Vector2 p = new(point.x, point.z);
		bool inside = false;

		for (int i = 0, j = verts.Length - 1; i < verts.Length; j = i++)
		{
			Vector2 a = new(verts[i].x, verts[i].z);
			Vector2 b = new(verts[j].x, verts[j].z);

			if (((a.y > p.y) != (b.y > p.y)) &&
				(p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y + float.Epsilon) + a.x))
				inside = !inside;
		}

		return inside;
	}

	private static bool TryFindCrossedEdge(Vector3 start, Vector3 end, NavMeshNode node,
		out (Vector3 a, Vector3 b) crossed, out float t)
	{
		crossed = default;
		t = 1f;
		bool found = false;

		for (int i = 0; i < node.vertices.Length; i++)
		{
			Vector3 a = node.vertices[i];
			Vector3 b = node.vertices[(i + 1) % node.vertices.Length];

			if (LineSegmentsIntersectXZ(start, end, a, b, out float u))
			{
				crossed = (a, b);
				t = u;
				found = true;
				break;
			}
		}
		return found;
	}

	private static bool LineSegmentsIntersectXZ(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out float t)
	{
		Vector2 a = new(p1.x, p1.z);
		Vector2 b = new(p2.x, p2.z);
		Vector2 c = new(p3.x, p3.z);
		Vector2 d = new(p4.x, p4.z);

		Vector2 r = b - a;
		Vector2 s = d - c;
		float denom = r.x * s.y - r.y * s.x;

		if (Mathf.Abs(denom) < 1e-5f)
		{
			t = 0f;
			return false; // parallel
		}

		Vector2 cma = c - a;
		float u = (cma.x * r.y - cma.y * r.x) / denom;
		float tVal = (cma.x * s.y - cma.y * s.x) / denom;

		if (tVal >= 0f && tVal <= 1f && u >= 0f && u <= 1f)
		{
			t = tVal;
			return true;
		}

		t = 0f;
		return false;
	}
}
