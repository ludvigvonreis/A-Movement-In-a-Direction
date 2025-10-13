using System.Linq;
using UnityEngine;
using DotRecast.Recast.Geom;
using DotRecast.Core;
using UnityEditor;
using DotRecast.Recast;
using NaughtyAttributes;
using System.Collections.Generic;
using DotRecast.Detour;
using DotRecast.Core.Numerics;

[System.Serializable]
public struct RecastConfig
{
	public float cellSize;
	public float cellHeight;
	public float agentMaxSlope;
	public float agentHeight;
	public float agentRadius;
	public float agentMaxClimb;
	public int regionMinSize;
	public int regionMergeSize;
	public float edgeMaxLen;
	public float edgeMaxError;
	public int vertsPerPoly;
	public float detailSampleDist;
	public float detailSampleMaxError;
	public bool filterLowHangingObstacles;
	public bool filterLedgeSpans;
	public bool filterWalkableLowHeightSpans;
	public bool buildMeshDetail;
}

[System.Serializable]
public class RcPolyMeshData
{
	public int[] verts;
	public int[] polys;
	public int[] regs;
	public int[] areas;
	public int nverts;
	public int npolys;
	public int nvp;
	public int maxpolys;
	public int[] flags;
	public Vector3 bmin;
	public Vector3 bmax;
	public float cs;
	public float ch;
	public int borderSize;
	public float maxEdgeError;

	public RcPolyMeshData() { }

	// Create surrogate from library object
	public RcPolyMeshData(RcPolyMesh mesh)
	{
		verts = mesh.verts;
		polys = mesh.polys;
		regs = mesh.regs;
		areas = mesh.areas;
		nverts = mesh.nverts;
		npolys = mesh.npolys;
		nvp = mesh.nvp;
		maxpolys = mesh.maxpolys;
		flags = mesh.flags;
		bmin = new Vector3(mesh.bmin.X, mesh.bmin.Y, mesh.bmin.Z);
		bmax = new Vector3(mesh.bmax.X, mesh.bmax.Y, mesh.bmax.Z);
		cs = mesh.cs;
		ch = mesh.ch;
		borderSize = mesh.borderSize;
		maxEdgeError = mesh.maxEdgeError;
	}

}

public class CreateNavMesh : MonoBehaviour
{
	[SerializeField]
	private RecastConfig config = new RecastConfig();
	[SerializeField]
	private GameObject geometryParent;

	private List<GameObject> levelGeometry;

	[SerializeField]
	private RcPolyMeshData finalPolyMesh;

	//[SerializeField]
	private List<float> vertexPositions;
	//[SerializeField]
	private List<int> meshFaces;

	[Button("Generate Mesh")]
	private void GenerateRecastMesh()
	{
		(vertexPositions, meshFaces) = FeedMeshToSimpleInputGeom(levelGeometry);
		Debug.Log($"Vertices: {vertexPositions?.Count}, Faces: {meshFaces?.Count}");

		var geomProvider = new SimpleInputGeomProvider(vertexPositions, meshFaces);

		var bmin = geomProvider.GetMeshBoundsMin();
		var bmax = geomProvider.GetMeshBoundsMax();

		//var m_ctx = new RcContext();

		var rcAreaMod = new RcAreaModification(0x1);
		var cfg = new RcConfig(
			RcPartition.MONOTONE,
			config.cellSize,
			config.cellHeight,
			config.agentMaxSlope,
			config.agentHeight,
			config.agentRadius,
			config.agentMaxClimb,
			config.regionMinSize,
			config.regionMergeSize,
			config.edgeMaxLen,
			config.edgeMaxError,
			config.vertsPerPoly,
			config.detailSampleDist,
			config.detailSampleMaxError,
			config.filterLowHangingObstacles,
			config.filterLedgeSpans,
			config.filterWalkableLowHeightSpans,
			rcAreaMod,
			config.buildMeshDetail
		);

		var bcfg = new RcBuilderConfig(cfg, bmin, bmax);

		RcBuilder rcBuilder = new RcBuilder();
		var result = rcBuilder.Build(geomProvider, bcfg, true);

		finalPolyMesh = new RcPolyMeshData(result.Mesh);

		// // Create navmesh data
		// DtNavMeshCreateParams param = new DtNavMeshCreateParams();
		// param.verts = polyMesh.verts;
		// param.vertCount = polyMesh.nverts;
		// param.polys = polyMesh.polys;
		// param.polyAreas = polyMesh.areas;
		// param.polyFlags = polyMesh.flags;
		// param.polyCount = polyMesh.npolys;
		// param.nvp = polyMesh.nvp;
		// param.detailMeshes = detailMesh.meshes;
		// param.detailVerts = detailMesh.verts;
		// param.detailVertsCount = detailMesh.nverts;
		// param.detailTris = detailMesh.tris;
		// param.detailTriCount = detailMesh.ntris;
		// param.walkableHeight = config.agentHeight;
		// param.walkableRadius = config.agentHeight;
		// param.walkableClimb = config.agentMaxClimb;
		// param.bmin = bmin;
		// param.bmax = bmax;
		// param.cs = config.cellSize;
		// param.ch = config.cellHeight;
		// param.buildBvTree = true;

		// var data = DtNavMeshBuilder.CreateNavMeshData(param);
		// DtNavMesh navMesh = new DtNavMesh();
		// navMesh.Init(data, config.vertsPerPoly, 0);

		// var navMeshQuery = new DtNavMeshQuery(navMesh);
		// var center = new RcVec3f(0f, 0f, -5.5f);      // The point to check
		// var halfExtents = new RcVec3f(0.5f, 1f, 0.5f); // Search box size
		// IDtQueryFilter filter = new DtQueryDefaultFilter();  // Default filter

		// long nearestRef;
		// RcVec3f nearestPt;
		// bool isOverPoly;

		// DtStatus status = navMeshQuery.FindNearestPoly(
		// 	center,
		// 	halfExtents,
		// 	filter,
		// 	out nearestRef,
		// 	out nearestPt,
		// 	out isOverPoly
		// );

		// print(nearestRef);

		// if (status.Succeeded())
		// {
		// 	var polyCenter = navMesh.GetPolyCenter(nearestRef);
		// 	print($"{nearestRef} : {nearestPt} : {isOverPoly} : {polyCenter}");
		// }
	}
	(List<float> vertexPositions, List<int> meshFaces)
	FeedMeshToSimpleInputGeom(List<GameObject> objects)
	{
		var allVerts = new List<float>();
		var allTris = new List<int>();
		int vertOffset = 0;

		foreach (var go in objects)
		{
			Mesh mesh = null;

			var mf = go.GetComponent<MeshFilter>();
			if (mf != null) mesh = mf.sharedMesh;
			else
			{
				var smr = go.GetComponent<SkinnedMeshRenderer>();
				if (smr != null)
				{
					mesh = new Mesh();
					smr.BakeMesh(mesh);
				}
			}

			if (mesh == null) continue;

			// Flatten vertices into world space
			allVerts.AddRange(mesh.vertices.SelectMany(v =>
			{
				var wv = go.transform.TransformPoint(v);
				return new float[] { wv.x, wv.y, wv.z };
			}));

			// Add triangles with vertex offset
			allTris.AddRange(mesh.triangles.Select(t => t + vertOffset));

			vertOffset += mesh.vertexCount;
		}

		return (allVerts, allTris);
	}

	[Button("Fetch Geometry")]
	private void FetchGeometry()
	{
		levelGeometry = new();

		foreach (Transform child in geometryParent.transform)
		{
			levelGeometry.Add(child.gameObject);
		}
	}

	const int RC_MESH_NULL_IDX = 65535;

	void OnDrawGizmos()
	{
		if (finalPolyMesh == null) return;

		var mesh = finalPolyMesh;
		var minBounds = mesh.bmin;

		Color[] colors = { Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan};

		for (int i = 0; i < mesh.npolys; i++)
		{
			Vector3[] polyVerts = new Vector3[mesh.nvp];
			int vertCount = 0;

			for (int j = 0; j < mesh.nvp; j++)
			{
				int idx = i * mesh.nvp * 2 + j;
				int vertexIndex = mesh.polys[idx];

				if (vertexIndex == RC_MESH_NULL_IDX)
					break;

				int neighborIndex = mesh.polys[i * mesh.nvp * 2 + mesh.nvp + j];

				Gizmos.color = Color.red;

				var vert = new Vector3(
					mesh.verts[vertexIndex * 3] * config.cellSize,
					mesh.verts[vertexIndex * 3 + 1] * config.cellHeight,
					mesh.verts[vertexIndex * 3 + 2] * config.cellSize
				);

				vert += minBounds;

				Gizmos.DrawSphere(vert, 0.2f);

				polyVerts[vertCount] = vert;
				vertCount++;
			}

			Gizmos.color = colors[mesh.regs[i] % 4];
			//Handles.color = colors[mesh.regs[i] % 4];
			//Handles.color = new Color(0, 1, 0, 0.5f);

			Vector3 centroid = new();

			// Draw edges
			for (int j = 0; j < vertCount; j++)
			{
				Vector3 start = polyVerts[j];
				Vector3 end = polyVerts[(j + 1) % vertCount]; // wrap around to form closed polygon

				Gizmos.DrawLine(start, end);

				centroid += start;
			}

			centroid /= vertCount;
			Gizmos.DrawSphere(centroid, 0.4f);
			//Handles.DrawAAConvexPolygon(polyVerts.Where(v => v != Vector3.zero).ToArray());
		}
	}
}
  