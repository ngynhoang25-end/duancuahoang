using UnityEngine;

public class PipeMove : MonoBehaviour
{
    public float speed = 2f;

    // 👇 THÊM 2 DÒNG NÀY
    public float speedIncrease = 0.15f;
    public float maxSpeed = 6f;

    void Update()
    {
        // 👇 THÊM ĐOẠN NÀY
        float currentSpeed = speed;
        if (GameManager.instance != null)
        {
            currentSpeed += GameManager.instance.score * speedIncrease;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed); // giới hạn tốc độ
        }

        // ❗ CHỈ SỬA DÒNG NÀY (dùng currentSpeed)
        transform.position += Vector3.left * currentSpeed * Time.deltaTime;

        if (transform.position.x < -10f)
        {
            Destroy(gameObject);
        }
    }
}