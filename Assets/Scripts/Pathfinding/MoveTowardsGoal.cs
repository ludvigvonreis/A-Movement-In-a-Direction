using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Rigidbody))]
public class MoveTowardsGoal : MonoBehaviour
{
	private NavMeshProvider navMeshProvider;

	[SerializeField]
	private Transform goal;
	[SerializeField]
	private NavigationPath NavPath;
	private bool hasPath = false;
	Rigidbody rb;

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

	void Awake()
	{
		rb = GetComponent<Rigidbody>();
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		rb.constraints = RigidbodyConstraints.FreezeRotation;
		rb.useGravity = false;
	}

	void Update()
	{
		if (!Application.isPlaying) return;

		if (Vector3.Distance(goal.position, lastGoal) > 1f)
		{
			FetchPath();
			lastGoal = goal.position;
		}
	}

	void FixedUpdate()
	{
		var path = NavPath;
		if (path.simplePath == null || currentPathIndex >= path.simplePath.Length) return;

		Vector3 target = path.simplePath[currentPathIndex];

		Vector3 desired = target - rb.position;
		float distance = desired.magnitude;
		desired.Normalize();
		desired *= maxSpeed;

		Vector3 steering = desired - velocity;
		velocity = Vector3.ClampMagnitude(velocity + steering * Time.fixedDeltaTime * acceleration, maxSpeed);

		rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
		//rb.linearVelocity = velocity;

		if (distance < 0.5f)
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
			navMeshProvider.DrawNodeInfo(goalNode, "Node");
		}
	}
}