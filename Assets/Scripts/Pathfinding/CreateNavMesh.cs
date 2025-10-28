using System.Linq;
using UnityEngine;
using DotRecast.Recast.Geom;
using UnityEditor;
using DotRecast.Recast;
using NaughtyAttributes;
using System.Collections.Generic;

[System.Serializable]
public struct NavMeshConfig
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
	private GameObject geometryParent;

	private List<GameObject> levelGeometry;

	private List<float> vertexPositions;
	private List<int> meshFaces;

	//[Button("Generate Mesh")]
	public RcPolyMeshData GenerateRecastMesh(NavMeshConfig config)
	{
		if (levelGeometry.Count < 1) FetchGeometry();

		(vertexPositions, meshFaces) = FeedMeshToSimpleInputGeom(levelGeometry);

		var geomProvider = new SimpleInputGeomProvider(vertexPositions, meshFaces);

		var bmin = geomProvider.GetMeshBoundsMin();
		var bmax = geomProvider.GetMeshBoundsMax();

		var rcAreaMod = new RcAreaModification(0x5);

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

		var finalPolyMesh = new RcPolyMeshData(result.Mesh);


		return finalPolyMesh;
	}

	[Button("Fetch Geometry")]
	private void FetchGeometry()
	{
		levelGeometry = new();

		void RecursivlyFetch(Transform parent)
		{
			foreach (Transform child in parent)
			{
				levelGeometry.Add(child.gameObject);

				if (child.childCount > 0)
				{
					RecursivlyFetch(child);
				}
			}
		}

		RecursivlyFetch(geometryParent.transform);
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

			allVerts.AddRange(mesh.vertices.SelectMany(v =>
			{
				var wv = go.transform.TransformPoint(v);
				return new float[] { wv.x, wv.y, wv.z };
			}));

			allTris.AddRange(mesh.triangles.Select(t => t + vertOffset));

			vertOffset += mesh.vertexCount;
		}

		return (allVerts, allTris);
	}
}