using UnityEngine;

public class ScoreZone : MonoBehaviour
{
    private bool scored = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!scored && collision.transform.root.CompareTag("Player"))
        {
            scored = true;
            GameManager.instance.AddScore();
        }
    }
}