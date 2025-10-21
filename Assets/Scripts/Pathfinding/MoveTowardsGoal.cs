using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class MoveTowardsGoal : MonoBehaviour
{
	private NavMeshProvider navMeshProvider;

	[SerializeField]
	private Transform goal;
	[SerializeField]
	private NavigationPath NavPath;
	private bool hasPath = false;

	[SerializeField]
	int currentPathIndex = 0;
	[SerializeField]
	Vector3 velocity;
	[SerializeField]
	float maxSpeed = 5f;
	[SerializeField]
	float acceleration = 10f;
	private Vector3 lastGoal;

	void Start()
	{
		navMeshProvider = NavMeshProvider.Instance;

		//FetchPath();
	}

	void FetchPath()
	{
		if (navMeshProvider.GetPath(transform.position, goal.position) is NavigationPath path)
		{

			if (NavPath.rawPath.Length < 1)
			{
				// First path request
				NavPath = path;
				currentPathIndex = 0;
				return;
			}

			// Only reset path if the goal moved significantly
			if (Vector3.Distance(path.goalPosition, NavPath.goalPosition) > 0.1f)
			{
				NavPath = path;

				// Find the closest point on the new path to our current position
				float closestDistance = float.MaxValue;
				int closestIndex = 0;
				for (int i = 0; i < NavPath.simplePath.Length; i++)
				{
					float dist = Vector3.Distance(transform.position, NavPath.simplePath[i]);
					if (dist < closestDistance)
					{
						closestDistance = dist;
						closestIndex = i;
					}
				}

				currentPathIndex = closestIndex;

				hasPath = true;
				return;
			}
		}

		hasPath = false;
	}

	void Update()
	{
		if (!Application.isPlaying) return;

		// Recalculate path if goal moved
		if (Vector3.Distance(goal.position, lastGoal) > 1f)
		{
			FetchPath();
			lastGoal = goal.position;
		}

		var path = NavPath;
		if (currentPathIndex >= path.simplePath.Length) return;

		Vector3 target = path.simplePath[currentPathIndex];

		// Seek behavior
		Vector3 desired = (target - transform.position);
		float distance = desired.magnitude;
		desired.Normalize();
		desired *= maxSpeed;

		Vector3 steering = desired - velocity;
		velocity = Vector3.ClampMagnitude(velocity + steering * Time.deltaTime * acceleration, maxSpeed);

		transform.position += velocity * Time.deltaTime;

		// Advance to next waypoint if close enough
		float arriveRadius = 0.5f; // tolerance radius for "close enough"
		if (distance < arriveRadius)
			currentPathIndex++;
	}

	void OnDrawGizmos()
	{
		if (!NavPath.Equals(default(NavigationPath)))
		{
			navMeshProvider.DrawVisualization(NavPath);
		}

		if (navMeshProvider.NavMesh.PositionToNode(goal.position) is NavMeshNode goalNode && goalNode != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(goalNode.Centroid, 0.2f);
			Handles.Label(goalNode.Centroid + new Vector3(2, 0, 0), $"Node index: {goalNode.polyIndex}");
		}
	}
}