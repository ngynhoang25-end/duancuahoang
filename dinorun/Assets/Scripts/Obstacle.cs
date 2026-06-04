using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public int PrefabIndex { get; private set; }

    private Spawner ownerSpawner;
    private float leftEdge;
    private bool recycled;

    private void Start()
    {
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 2f;
    }

    public void Initialize(Spawner ownerSpawner, int prefabIndex)
    {
        this.ownerSpawner = ownerSpawner;
        PrefabIndex = prefabIndex;
        recycled = false;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.Playing)
        {
            return;
        }

        transform.position += GameManager.Instance.gameSpeed * Time.deltaTime * Vector3.left;

        if (!recycled && transform.position.x < leftEdge)
        {
            recycled = true;

            if (ownerSpawner != null)
            {
                ownerSpawner.Recycle(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

}
