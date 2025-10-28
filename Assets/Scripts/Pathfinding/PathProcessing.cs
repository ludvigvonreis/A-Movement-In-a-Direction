using System.Collections.Generic;
using UnityEngine;

public class PathProcessing
{
	public static List<Vector3> DensifyPath(List<Vector3> path, float maxSegmentLength = 0.5f)
	{
		var newPath = new List<Vector3>();
		if (path == null || path.Count == 0)
			return newPath;

		newPath.Add(path[0]);

		for (int i = 1; i < path.Count; i++)
		{
			Vector3 start = path[i - 1];
			Vector3 end = path[i];
			float dist = Vector3.Distance(start, end);

			if (dist > maxSegmentLength)
			{
				int segments = Mathf.CeilToInt(dist / maxSegmentLength);
				for (int s = 1; s < segments; s++)
				{
					float t = s / (float)segments;
					Vector3 point = Vector3.Lerp(start, end, t);
					newPath.Add(point);
				}
			}

			newPath.Add(end);
		}

		return newPath;
	}

	// public static List<Vector3> GenerateSpline(List<Vector3> controlPoints, int pointsPerSegment = 10)
	// {
	// 	var result = new List<Vector3>();

	// 	if (controlPoints == null || controlPoints.Count < 2)
	// 		return result;

	// 	for (int i = 0; i < controlPoints.Count - 1; i++)
	// 	{
	// 		Vector3 p0 = i == 0 ? controlPoints[i] : controlPoints[i - 1];
	// 		Vector3 p1 = controlPoints[i];
	// 		Vector3 p2 = controlPoints[i + 1];
	// 		Vector3 p3 = (i + 2 < controlPoints.Count) ? controlPoints[i + 2] : controlPoints[i + 1];

	// 		for (int j = 0; j <= pointsPerSegment; j++)
	// 		{
	// 			float t = j / (float)pointsPerSegment;
	// 			Vector3 point = CentripetalCR(p0, p1, p2, p3, t);
	// 			if (result.Count == 0 || point != result[^1])
	// 				result.Add(point);
	// 		}
	// 	}

	// 	return result;
	// }

	// private static Vector3 CentripetalCR(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	// {
	// 	float alpha = 0.5f;

	// 	float t0 = 0f;
	// 	float t1 = t0 + Mathf.Pow(Vector3.Distance(p0, p1), alpha);
	// 	float t2 = t1 + Mathf.Pow(Vector3.Distance(p1, p2), alpha);
	// 	float t3 = t2 + Mathf.Pow(Vector3.Distance(p2, p3), alpha);

	// 	// Prevent zero-length segments
	// 	if (Mathf.Approximately(t1, t0)) t1 += 0.0001f;
	// 	if (Mathf.Approximately(t2, t1)) t2 += 0.0001f;
	// 	if (Mathf.Approximately(t3, t2)) t3 += 0.0001f;

	// 	float t_ = Mathf.Lerp(t1, t2, t);

	// 	Vector3 A1 = Lerp(p0, p1, t0, t1, t_);
	// 	Vector3 A2 = Lerp(p1, p2, t1, t2, t_);
	// 	Vector3 A3 = Lerp(p2, p3, t2, t3, t_);

	// 	Vector3 B1 = Lerp(A1, A2, t0, t2, t_);
	// 	Vector3 B2 = Lerp(A2, A3, t1, t3, t_);

	// 	return Lerp(B1, B2, t1, t2, t_);
	// }

	// private static Vector3 Lerp(Vector3 p0, Vector3 p1, float t0, float t1, float t)
	// {
	// 	if (Mathf.Approximately(t1, t0))
	// 		return p0;
	// 	return (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
	// }

    public static List<Vector3> RDP(List<Vector3> points, float tolerance)
    {
        if (points == null || points.Count < 2)
            return new List<Vector3>(points);

		float maxDist = 0f;
        int index = -1;
        Vector3 start = points[0];
        Vector3 end = points[^1];

        // Use XZ-plane distance only
        for (int i = 1; i < points.Count - 1; i++)
        {
            float dist = DistanceFromLineToPointXZ(start, end, points[i]);
            if (dist > maxDist)
            {
                maxDist = dist;
                index = i;
            }
        }

		float adaptiveTolerance = Mathf.Max(tolerance, Vector3.Distance(start, end) * 0.05f);

		if (maxDist > adaptiveTolerance && index != -1)
		{
			// Recursive calls
			var left = RDP(points.GetRange(0, index + 1), tolerance);
			var right = RDP(points.GetRange(index, points.Count - index), tolerance);

			// Merge without duplicate
			left.RemoveAt(left.Count - 1);
			left.AddRange(right);
			return left;
		}
		else
		{
			return new List<Vector3> { start, end };
		}
    }

	private static float DistanceFromLineToPointXZ(Vector3 start, Vector3 end, Vector3 point)
	{
		Vector2 a = new Vector2(start.x, start.z);
		Vector2 b = new Vector2(end.x, end.z);
		Vector2 p = new Vector2(point.x, point.z);

		Vector2 ab = b - a;
		Vector2 ap = p - a;

		float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
		Vector2 projection = a + ab * t;
		return Vector2.Distance(p, projection);
	}
}
