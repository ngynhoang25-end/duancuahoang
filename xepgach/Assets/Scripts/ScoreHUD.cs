using UnityEngine;

public class ScoreHUD : MonoBehaviour
{
    public Board board;

    private void Awake()
    {
        if (board == null)
            board = GetComponent<Board>() ?? FindObjectOfType<Board>();
    }

    private void OnGUI()
    {
        if (board == null)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 220, 120), GUI.skin.box);
        GUILayout.Label("Xếp Gạch");
        GUILayout.Label($"Score: {board.Score}");
        GUILayout.Label($"Level: {board.Level}");
        GUILayout.Label($"Lines: {board.TotalLinesCleared}");
        GUILayout.EndArea();
    }
}
