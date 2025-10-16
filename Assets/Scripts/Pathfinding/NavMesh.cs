using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NavMeshNode
{
	public int polyIndex;
	public int regionId;
	public Vector3[] vertices;
	public List<int> edges = new();

	private Vector3 centroid;

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
}

[System.Serializable]
public class NavMesh
{
	const int RC_MESH_NULL_IDX = 65535;

	[HideInInspector]
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
}
