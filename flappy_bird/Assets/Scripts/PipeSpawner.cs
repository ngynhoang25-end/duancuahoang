using UnityEngine;

public class PipeSpawner : MonoBehaviour
{
    public GameObject pipePrefab;
    public float spawnRate = 2f;
    private float timer = 0;

    public float minY = -1f;
    public float maxY = 2.5f;

    // 👇 THÊM 2 DÒNG
    public float spawnDecrease = 0.05f;
    public float minSpawnRate = 0.8f;

    void Update()
    {
        // 👇 THÊM ĐOẠN NÀY
        float currentSpawnRate = spawnRate;
        if (GameManager.instance != null)
        {
            currentSpawnRate -= GameManager.instance.score * spawnDecrease;
            currentSpawnRate = Mathf.Max(currentSpawnRate, minSpawnRate);
        }

        timer += Time.deltaTime;

        // ❗ CHỈ SỬA DÒNG NÀY
        if (timer >= currentSpawnRate)
        {
            SpawnPipe();
            timer = 0;
        }
    }

    void SpawnPipe()
    {
        float y = Random.Range(minY, maxY);
        Instantiate(pipePrefab, new Vector3(10f, y, 0), Quaternion.identity);
    }
}