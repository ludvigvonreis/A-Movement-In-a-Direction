using UnityEngine;

public static class GeometryLib
{
	public static bool PointInPolygon(Vector3 point, Vector3[] verts)
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

	public static bool LineSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
	{
		intersection = new Vector2();

		Vector2 s1 = p2 - p1;
		Vector2 s2 = p4 - p3;

		float s, t;
		float denominator = -s2.x * s1.y + s1.x * s2.y;
		if (denominator == 0) return true; // Parallel

		s = (-s1.y * (p1.x - p3.x) + s1.x * (p1.y - p3.y)) / denominator;
		t = (s2.x * (p1.y - p3.y) - s2.y * (p1.x - p3.x)) / denominator;

		if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
		{
			intersection = p1 + (t * s1);
			return true;
		}

		return false; // No intersection within the segments
	}

	public static bool RayIntersectsTriangle(Vector3 rayOrigin, Vector3 rayDir, Vector3 v0, Vector3 v1, Vector3 v2, out float t)
	{
		t = 0f;
		Vector3 edge1 = v1 - v0;
		Vector3 edge2 = v2 - v0;
		Vector3 h = Vector3.Cross(rayDir, edge2);
		float a = Vector3.Dot(edge1, h);
		if (Mathf.Abs(a) < 1e-6f) return false; // parallel

		float f = 1f / a;
		Vector3 s = rayOrigin - v0;
		float u = f * Vector3.Dot(s, h);
		if (u < 0f || u > 1f) return false;

		Vector3 q = Vector3.Cross(s, edge1);
		float v = f * Vector3.Dot(rayDir, q);
		if (v < 0f || u + v > 1f) return false;

		t = f * Vector3.Dot(edge2, q);
		return t > 0f;
	}
}