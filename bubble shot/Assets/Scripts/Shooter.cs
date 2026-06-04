using System.Collections.Generic;
using UnityEngine;

public class Shooter : MonoBehaviour
{
	public bool canShoot;
	public float speed = 25f;

	public Transform nextBubblePosition;
	public GameObject currentBubble;
	public GameObject nextBubble;
	public GameObject bottomShootPoint;

	private Vector2 lookDirection;
	private float lookAngle;
	private GameObject line;
	private GameObject limit;
	private LineRenderer lineRenderer;
	private Vector2 gizmosPoint;
	private const float trajectoryRayDistance = 300f;
	private const int maxTrajectoryBounces = 4;

	public void Awake()
	{
		line = GameObject.FindGameObjectWithTag("Line");
		limit = GameObject.FindGameObjectWithTag("Limit");
		if (line != null)
			lineRenderer = line.GetComponent<LineRenderer>();
	}

	private Vector2 GetPointerScreenPosition()
	{
		if (Input.touchCount > 0)
			return Input.GetTouch(0).position;

		return Input.mousePosition;
	}

	private bool IsPointerDown()
	{
		if (Input.touchCount > 0)
		{
			Touch touch = Input.GetTouch(0);
			return touch.phase == TouchPhase.Began
				|| touch.phase == TouchPhase.Moved
				|| touch.phase == TouchPhase.Stationary;
		}

		return Input.GetMouseButton(0);
	}

	private bool IsPointerReleased()
	{
		if (Input.touchCount > 0)
		{
			Touch touch = Input.GetTouch(0);
			return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
		}

		return Input.GetMouseButtonUp(0);
	}

	private bool IsPointerInShootRange(Vector2 pointerWorld)
	{
		return pointerWorld.y > bottomShootPoint.transform.position.y
			&& pointerWorld.y < limit.transform.position.y;
	}

	private void UpdateTrajectoryPreview(Vector2 targetWorld)
	{
		if (line == null || lineRenderer == null)
			return;

		line.transform.position = transform.position;
		line.transform.rotation = Quaternion.Euler(0f, 0f, lookAngle - 90f);

		if (LevelManager.instance == null || LevelManager.instance.GetBubbleAreaChildCount() <= 0)
		{
			lineRenderer.positionCount = 0;
			line.SetActive(false);
			return;
		}

		Vector2 origin = transform.position;
		Vector2 direction = (targetWorld - origin).normalized;
		List<Vector3> points = new List<Vector3> { origin };

		for (int bounce = 0; bounce < maxTrajectoryBounces; bounce++)
		{
			RaycastHit2D hit = Physics2D.Raycast(origin, direction, trajectoryRayDistance);

			if (hit.collider == null)
			{
				points.Add(origin + direction * 20f);
				break;
			}

			points.Add(hit.point);

			if (hit.transform.CompareTag("Wall"))
			{
				direction = Vector2.Reflect(direction, hit.normal).normalized;
				origin = hit.point + direction * 0.01f;
				continue;
			}

			break;
		}

		lineRenderer.positionCount = points.Count;
		lineRenderer.SetPositions(points.ToArray());
		line.SetActive(true);
	}

	public void Update()
	{
		if (GameManager.instance.gameState == "play")
		{
			Vector2 pointerWorld = Camera.main.ScreenToWorldPoint(GetPointerScreenPosition());
			gizmosPoint = pointerWorld;
			lookDirection = pointerWorld - (Vector2)transform.position;
			lookAngle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

			if (IsPointerDown() && IsPointerInShootRange(pointerWorld))
			{
				UpdateTrajectoryPreview(pointerWorld);
			}
			else
			{
				if (line != null)
					line.SetActive(false);
				if (lineRenderer != null)
					lineRenderer.positionCount = 0;
			}

			if (canShoot && IsPointerReleased() && IsPointerInShootRange(pointerWorld))
			{
				canShoot = false;
				Shoot();
			}
		}
	}

	public void Shoot()
	{
		if (currentBubble == null) CreateNextBubble();
		ScoreManager.GetInstance().AddThrows();
		AudioManager.instance.PlaySound("shoot");
		transform.rotation = Quaternion.Euler(0f, 0f, lookAngle - 90f);
		currentBubble.transform.rotation = transform.rotation;
		currentBubble.GetComponent<CircleCollider2D>().enabled = true;
		Rigidbody2D rb = currentBubble.GetComponent<Rigidbody2D>();
		rb.AddForce(currentBubble.transform.up * speed, ForceMode2D.Impulse);
		rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		rb.gravityScale = 0;
		currentBubble = null;
	}

	public void SwapBubbles()
	{
		List<GameObject> bubblesInScene = LevelManager.instance.bubblesInScene;
		if (bubblesInScene.Count < 1) return;

		currentBubble.transform.position = nextBubblePosition.position;
		nextBubble.transform.position = transform.position;
		GameObject temp = currentBubble;
		currentBubble = nextBubble;
		nextBubble = temp;
	}

	public void CreateNewBubbles()
	{
		if (nextBubble != null)
			Destroy(nextBubble);

		if (currentBubble != null)
			Destroy(currentBubble);

		nextBubble = null;
		currentBubble = null;
		CreateNextBubble();
		canShoot = true;
	}

	public void CreateNextBubble()
	{
		List<GameObject> bubblesInScene = LevelManager.instance.bubblesInScene;
		List<string> colors = LevelManager.instance.colorsInScene;

		if (bubblesInScene.Count < 1) return;

		if (nextBubble == null)
		{
			nextBubble = InstantiateNewBubble(bubblesInScene);
		}
		else
		{
			// if (!colors.Contains(nextBubble.GetComponent<Bubble>().bubbleColor.ToString()))
			// {
			// 	Destroy(nextBubble);
			// 	nextBubble = InstantiateNewBubble(bubblesInScene);
			// }
		}

		if (currentBubble == null)
		{
			currentBubble = nextBubble;
			currentBubble.transform.position = transform.position;
			nextBubble = InstantiateNewBubble(bubblesInScene);
		}
	}

	private GameObject InstantiateNewBubble(List<GameObject> bubblesInScene)
	{
		if (bubblesInScene.Count > 0)
		{
			GameObject newBubble = Instantiate(bubblesInScene[Random.Range(0, bubblesInScene.Count)]);
			newBubble.transform.position = nextBubblePosition.position;
			newBubble.GetComponent<Bubble>().isFixed = false;
			newBubble.GetComponent<CircleCollider2D>().enabled = false;
			Rigidbody2D rb2d = newBubble.AddComponent(typeof(Rigidbody2D)) as Rigidbody2D;
			rb2d.gravityScale = 0f;
			return newBubble;
		}
		else
		{
			return null;
		}

	}
}
