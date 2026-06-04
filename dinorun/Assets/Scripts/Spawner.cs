using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public static Spawner Instance { get; private set; }

    [System.Serializable]
    public struct SpawnableObject
    {
        public GameObject prefab;
        [Range(0f, 1f)]
        public float spawnChance;
    }

    public SpawnableObject[] objects;
    public float minSpawnRate = 1f;
    public float maxSpawnRate = 2f;

    private readonly Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();
    private readonly List<Obstacle> activeObstacles = new List<Obstacle>();
    private Coroutine spawnRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.Playing)
        {
            StartSpawning();
        }
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    public void ResetSpawner()
    {
        StopSpawning();

        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            if (activeObstacles[i] != null)
            {
                Recycle(activeObstacles[i]);
            }
        }

        activeObstacles.Clear();
    }

    public void StartSpawning()
    {
        if (spawnRoutine == null)
        {
            spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    public void Recycle(Obstacle obstacle)
    {
        if (obstacle == null)
        {
            return;
        }

        int prefabIndex = obstacle.PrefabIndex;
        obstacle.gameObject.SetActive(false);
        activeObstacles.Remove(obstacle);

        if (!pools.TryGetValue(prefabIndex, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools.Add(prefabIndex, pool);
        }

        pool.Enqueue(obstacle.gameObject);
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.Playing)
            {
                yield return null;
                continue;
            }

            SpawnPattern();

            yield return new WaitForSeconds(GetSpawnDelay());
        }
    }

    private float GetSpawnDelay()
    {
        float difficulty = Mathf.InverseLerp(5f, 20f, GameManager.Instance.gameSpeed);
        float scaledMin = Mathf.Lerp(minSpawnRate, minSpawnRate * 0.55f, difficulty);
        float scaledMax = Mathf.Lerp(maxSpawnRate, maxSpawnRate * 0.65f, difficulty);

        return Random.Range(scaledMin, scaledMax);
    }

    private void SpawnPattern()
    {
        int obstacleCount = Random.value < Mathf.InverseLerp(5f, 18f, GameManager.Instance.gameSpeed) ? Random.Range(1, 4) : 1;
        float spacing = Random.Range(1.2f, 2.1f);

        for (int i = 0; i < obstacleCount; i++)
        {
            SpawnSingle(transform.position + Vector3.right * spacing * i);
        }
    }

    private void SpawnSingle(Vector3 positionOffset)
    {
        if (objects == null || objects.Length == 0)
        {
            return;
        }

        float spawnChance = Random.value;
        int selectedIndex = 0;

        for (int i = 0; i < objects.Length; i++)
        {
            if (spawnChance < objects[i].spawnChance)
            {
                selectedIndex = i;
                break;
            }

            spawnChance -= objects[i].spawnChance;
        }

        SpawnFromPool(selectedIndex, positionOffset);
    }

    private void SpawnFromPool(int prefabIndex, Vector3 positionOffset)
    {
        GameObject prefab = objects[prefabIndex].prefab;
        if (prefab == null)
        {
            return;
        }

        if (!pools.TryGetValue(prefabIndex, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools.Add(prefabIndex, pool);
        }

        GameObject obstacleObject = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab);
        obstacleObject.transform.position = prefab.transform.position + positionOffset;
        obstacleObject.transform.rotation = prefab.transform.rotation;
        obstacleObject.SetActive(true);

        Obstacle obstacle = obstacleObject.GetComponent<Obstacle>();
        if (obstacle != null)
        {
            obstacle.Initialize(this, prefabIndex);
            activeObstacles.Add(obstacle);
        }
    }

}
