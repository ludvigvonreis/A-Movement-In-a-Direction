using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathProcessing
{
	public static List<Vector3> Simplify(List<Vector3> points, int maxLookAhead = 3)
	{

		if (points == null || points.Count < 3)
			return new List<Vector3>(points);

		List<Vector3> simplified = new()
		{
			points[0]
		};

		int currentIndex = 0;

		while (currentIndex < points.Count - 1)
		{
			Vector3 currentPos = simplified[^1];
			bool foundShortcut = false;

			// Look ahead through segments to find the farthest reachable point
			for (int lookAhead = Mathf.Min(maxLookAhead, points.Count - currentIndex - 2); lookAhead >= 1; lookAhead--)
			{
				int segmentEndIndex = currentIndex + lookAhead + 1;

				// Test direct connection to the segment endpoint first
				if (!Physics.Linecast(currentPos, points[segmentEndIndex]))
				{
					simplified.Add(points[segmentEndIndex]);
					currentIndex = segmentEndIndex;
					foundShortcut = true;
					break;
				}

				// If direct to endpoint is blocked, try points along the segment
				int segmentStartIndex = currentIndex + lookAhead;
				Vector3 segmentStart = points[segmentStartIndex];
				Vector3 segmentEnd = points[segmentEndIndex];
				Vector3 segmentDir = (segmentEnd - segmentStart).normalized;
				float segmentLength = Vector3.Distance(segmentStart, segmentEnd);

				// Test multiple points along the segment (including midpoint)
				float[] testPoints = { 0.5f, 0.25f, 0.75f, 0.1f, 0.9f }; // Midpoint first, then others

				foreach (float t in testPoints)
				{
					Vector3 testPoint = segmentStart + segmentDir * (segmentLength * t);

					if (!Physics.Linecast(currentPos, testPoint))
					{
						// Found a valid shortcut point along the segment
						simplified.Add(testPoint);
						// Move current index to the start of this segment
						currentIndex = segmentStartIndex;
						foundShortcut = true;
						break;
					}
				}

				if (foundShortcut) break;
			}

			// If no shortcut found, move to next point
			if (!foundShortcut)
			{
				currentIndex++;
				if (currentIndex < points.Count)
					simplified.Add(points[currentIndex]);
			}
		}

		return simplified;
	}

	public static List<Vector3> GenerateSpline(List<Vector3> controlPoints, int pointsPerSegment = 10)
	{
		var result = new List<Vector3>();

		if (controlPoints == null || controlPoints.Count < 2)
			return result;

		for (int i = 0; i < controlPoints.Count - 1; i++)
		{
			Vector3 p0 = i == 0 ? controlPoints[i] : controlPoints[i - 1];
			Vector3 p1 = controlPoints[i];
			Vector3 p2 = controlPoints[i + 1];
			Vector3 p3 = (i + 2 < controlPoints.Count) ? controlPoints[i + 2] : controlPoints[i + 1];

			for (int j = 0; j <= pointsPerSegment; j++)
			{
				float t = j / (float)pointsPerSegment;
				Vector3 point = CentripetalCR(p0, p1, p2, p3, t);
				if (result.Count == 0 || point != result[^1])
					result.Add(point);
			}
		}

		return result;
	}

	private static Vector3 CentripetalCR(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		float alpha = 0.5f;

		float t0 = 0f;
		float t1 = t0 + Mathf.Pow(Vector3.Distance(p0, p1), alpha);
		float t2 = t1 + Mathf.Pow(Vector3.Distance(p1, p2), alpha);
		float t3 = t2 + Mathf.Pow(Vector3.Distance(p2, p3), alpha);

		// Prevent zero-length segments
		if (Mathf.Approximately(t1, t0)) t1 += 0.0001f;
		if (Mathf.Approximately(t2, t1)) t2 += 0.0001f;
		if (Mathf.Approximately(t3, t2)) t3 += 0.0001f;

		float t_ = Mathf.Lerp(t1, t2, t);

		Vector3 A1 = Lerp(p0, p1, t0, t1, t_);
		Vector3 A2 = Lerp(p1, p2, t1, t2, t_);
		Vector3 A3 = Lerp(p2, p3, t2, t3, t_);

		Vector3 B1 = Lerp(A1, A2, t0, t2, t_);
		Vector3 B2 = Lerp(A2, A3, t1, t3, t_);

		return Lerp(B1, B2, t1, t2, t_);
	}

	private static Vector3 Lerp(Vector3 p0, Vector3 p1, float t0, float t1, float t)
	{
		if (Mathf.Approximately(t1, t0))
			return p0;
		return (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
	}

	public static List<Vector3> RDP(List<Vector3> points, float tolerance)
	{
		if (points == null || points.Count < 2)
			return new List<Vector3>(points); // nothing to simplify

		// Find the point with the maximum distance from the line
		float maxDist = 0f;
		int index = -1;
		Vector3 start = points[0];
		Vector3 end = points[^1];

		for (int i = 1; i < points.Count - 1; i++) // skip first and last point
		{
			float dist = DistanceFromLineToPoint(start, end, points[i]);
			if (dist > maxDist)
			{
				maxDist = dist;
				index = i;
			}
		}

		if (maxDist > tolerance && index != -1)
		{
			// Recursive call
			var left = RDP(points.Take(index + 1).ToList(), tolerance);
			var right = RDP(points.Skip(index).ToList(), tolerance);

			// Combine results, avoid duplicate at split
			left.RemoveAt(left.Count - 1);
			left.AddRange(right);

			return left;
		}
		else
		{
			// Just keep start and end points
			return new List<Vector3> { start, end };
		}
	}

	private static float DistanceFromLineToPoint(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
	{
		Vector3 lineDirection = lineEnd - lineStart;
		Vector3 pointDirection = point - lineStart;

		float lineSqrLength = lineDirection.sqrMagnitude;

		if (lineSqrLength < Mathf.Epsilon)
			return pointDirection.magnitude; // line is effectively a point

		// Distance from point to line: |(point - lineStart) x lineDirection| / |lineDirection|
		Vector3 cross = Vector3.Cross(pointDirection, lineDirection);
		return cross.magnitude / Mathf.Sqrt(lineSqrLength);
	}
}
