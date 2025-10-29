using System;
using System.Collections.Generic;
using UnityEngine;

public class PathProcessing
{
	public static List<Vector3> StringPulling(List<Vector3> points, float epsilon, List<int> flags, int keepMask)
	{
		if (points == null) return null;
		int count = points.Count;
		if (count <= 2 || epsilon <= 0f) return new List<Vector3>(points);

		if (flags == null || flags.Count != count)
		{
			// If no flags provided or mismatched length, treat as zero
			flags = new List<int>(new int[count]);
		}

		var keep = new bool[count];
		keep[0] = true;
		keep[count - 1] = true;

		// Pre-mark points that must be kept because of flags
		for (int i = 0; i < count; i++)
		{
			if ((flags[i] & keepMask) != 0)
			{
				keep[i] = true;
			}
		}

		SimplifyRecursive(points, 0, count - 1, epsilon, keep);

		var result = new List<Vector3>();
		for (int i = 0; i < count; i++)
		{
			if (keep[i]) result.Add(points[i]);
		}
		return result;
	}

	private static void SimplifyRecursive(List<Vector3> points, int startIndex, int endIndex, float epsilon, bool[] keep)
	{
		if (endIndex <= startIndex + 1) return; // nothing to simplify

		float maxDistance = -1f;
		int indexFarthest = -1;

		Vector3 a = points[startIndex];
		Vector3 b = points[endIndex];

		for (int i = startIndex + 1; i < endIndex; i++)
		{
			if (keep[i]) continue; // skip pre-marked points

			float dist = PerpendicularDistance(points[i], a, b);
			if (dist > maxDistance)
			{
				maxDistance = dist;
				indexFarthest = i;
			}
		}

		if (maxDistance > epsilon)
		{
			// Keep farthest point and recurse
			keep[indexFarthest] = true;
			SimplifyRecursive(points, startIndex, indexFarthest, epsilon, keep);
			SimplifyRecursive(points, indexFarthest, endIndex, epsilon, keep);
		}
	}

	private static float PerpendicularDistance(Vector3 p, Vector3 a, Vector3 b)
	{
		Vector3 ab = b - a;
		Vector3 ap = p - a;
		float abLenSq = ab.sqrMagnitude;

		if (abLenSq == 0f) return ap.magnitude;

		float t = Vector3.Dot(ap, ab) / abLenSq;
		t = Mathf.Clamp01(t);

		Vector3 projection = a + ab * t;
		return (p - projection).magnitude;
	}

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

	public static List<Vector3> Shortcutting(List<Vector3> path, int mask, NavMesh navMesh, float maxShortcutDistance = 15f)
	{
		if (path == null || path.Count < 2)
			return new List<Vector3>(path);

		List<Vector3> simplified = new List<Vector3>();
		int currentIndex = 0;
		simplified.Add(path[currentIndex]);

		while (currentIndex < path.Count - 1)
		{
			int nextIndex = currentIndex + 1; // default to next point
			float maxDist = 0f;

			// Try to find the farthest reachable point within maxShortcutDistance
			for (int i = currentIndex + 1; i < path.Count; i++)
			{
				float dist = Vector3.Distance(path[currentIndex], path[i]);
				if (dist > maxShortcutDistance)
					break; // do not consider points farther than max distance

				if (IsWalkable(currentIndex, i, path, mask, navMesh))
				{
					nextIndex = i;
					maxDist = dist;
				}
			}

			simplified.Add(path[nextIndex]);
			currentIndex = nextIndex;
		}

		return simplified;
	}

    // Checks if you can move straight from startIndex to endIndex along the path inside NavMesh
    private static bool IsWalkable(int startIndex, int endIndex, List<Vector3> path, int mask, NavMesh navMesh)
    {
        Vector3 start = path[startIndex];
        Vector3 end = path[endIndex];

        // NavMesh.Raycast returns true if something blocks the direct path
		bool blocked = navMesh.Raycast(start, end, out Vector3 _);
		
		// Gizmos.color = !blocked ? Color.blue : Color.red;
		// Gizmos.DrawSphere(start, 0.25f);
		// Gizmos.DrawSphere(end, 0.25f);
        return !blocked;
    }
}
