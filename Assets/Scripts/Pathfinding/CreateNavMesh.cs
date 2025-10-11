using System.Linq;
using UnityEngine;
using DotRecast.Recast.Geom;
using DotRecast.Core;

using DotRecast.Recast;
using NaughtyAttributes;
using System.Collections.Generic;

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

public class CreateNavMesh : MonoBehaviour
{
	[SerializeField]
	private RecastConfig config = new RecastConfig();

	[SerializeField]
	private List<GameObject> levelGeometry;

	private RcPolyMesh finalPolyMesh;

	[SerializeField]
	private List<float> vertexPositions;
	[SerializeField]
	private List<int> meshFaces;

	[SerializeField]
	private int[] navMeshVerticies;

	[SerializeField]
	private int[] navMeshPolygons;

	[Button("Generate Mesh")]
	private void GenerateRecastMesh()
	{
		(vertexPositions, meshFaces) = FeedMeshToSimpleInputGeom(levelGeometry);
		Debug.Log($"Vertices: {vertexPositions?.Count}, Faces: {meshFaces?.Count}");

		var geomProvider = new SimpleInputGeomProvider(vertexPositions, meshFaces);

		var bmin = geomProvider.GetMeshBoundsMin();
		var bmax = geomProvider.GetMeshBoundsMax();

		var m_ctx = new RcContext();

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

		// RcHeightfield m_solid = new RcHeightfield(bcfg.width, bcfg.height, bcfg.bmin, bcfg.bmax, cfg.Cs, cfg.Ch, cfg.BorderSize);

		// foreach (RcTriMesh geom in geomProvider.Meshes())
		// {
		// 	float[] verts = geom.GetVerts();
		// 	int[] tris = geom.GetTris();
		// 	int ntris = tris.Length / 3;

		// 	int[] m_triareas = RcRecast.MarkWalkableTriangles(m_ctx, cfg.WalkableSlopeAngle, verts, tris, ntris, cfg.WalkableAreaMod);

		// 	RcRasterizations.RasterizeTriangles(m_ctx, verts, tris, m_triareas, ntris, m_solid, cfg.WalkableClimb);
		// }

		// RcFilters.FilterLowHangingWalkableObstacles(m_ctx, cfg.WalkableClimb, m_solid);
		// RcFilters.FilterLedgeSpans(m_ctx, cfg.WalkableHeight, cfg.WalkableClimb, m_solid);
		// RcFilters.FilterWalkableLowHeightSpans(m_ctx, cfg.WalkableHeight, m_solid);

		// RcCompactHeightfield m_chf = RcCompacts.BuildCompactHeightfield(m_ctx, cfg.WalkableHeight, cfg.WalkableClimb, m_solid);
		// RcRegions.BuildDistanceField(m_ctx, m_chf);
		// RcRegions.BuildRegions(m_ctx, m_chf, cfg.MinRegionArea, cfg.MergeRegionArea);

		// RcContourSet m_cset = RcContours.BuildContours(m_ctx, m_chf, cfg.MaxSimplificationError, cfg.MaxEdgeLen, RcBuildContoursFlags.RC_CONTOUR_TESS_WALL_EDGES);

		// RcPolyMesh m_pmesh = RcMeshs.BuildPolyMesh(m_ctx, m_cset, cfg.MaxVertsPerPoly);
		// Debug.Log($"PolyMesh: {m_pmesh != null} Polys: {m_pmesh.npolys} Verts: {m_pmesh.nverts}");

		// RcPolyMeshDetail m_dmesh = RcMeshDetails.BuildPolyMeshDetail(
		// 	m_ctx,
		// 	m_pmesh,
		// 	m_chf,
		// 	cfg.DetailSampleDist,
		// 	cfg.DetailSampleMaxError
		// );

		//finalPolyMesh = m_pmesh;
		//navMeshVerticies = m_pmesh.verts;


		RcBuilder rcBuilder = new RcBuilder();
		var navMesh = rcBuilder.Build(geomProvider, bcfg, true);
		print(navMesh.Mesh.verts);

		finalPolyMesh = navMesh.Mesh;
		navMeshVerticies = navMesh.Mesh.verts;
		navMeshPolygons = navMesh.Mesh.polys;
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

	const int RC_MESH_NULL_IDX = 65535;

	void OnDrawGizmos()
	{
		if (finalPolyMesh == null) return;

		var mesh = finalPolyMesh;
		var minBounds = new Vector3(mesh.bmin.X, mesh.bmin.Y, mesh.bmin.Z);

		print("Polys: " + mesh.npolys);

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

				Gizmos.color = neighborIndex != RC_MESH_NULL_IDX ? Color.blue : Color.red;

				var vert = new Vector3(
					mesh.verts[vertexIndex * 3],
					mesh.verts[vertexIndex * 3 + 1],
					mesh.verts[vertexIndex * 3 + 2]
				);

				vert *= config.cellSize;
				vert += minBounds;

				Gizmos.DrawSphere(vert, 0.2f);

				polyVerts[vertCount] = vert;
				vertCount++;
			}

			Gizmos.color = Color.green;
			// Draw edges
			for (int j = 0; j < vertCount; j++)
			{
				int neighborIdx = mesh.polys[i * mesh.nvp * 2 + mesh.nvp + j];
				Gizmos.color = neighborIdx != RC_MESH_NULL_IDX ? Color.blue : Color.red;

				Vector3 start = polyVerts[j];
				Vector3 end = polyVerts[(j + 1) % vertCount]; // wrap around to form closed polygon
				Gizmos.DrawLine(start, end);
			}
		}
	}
}
