using UnityEngine;

public class NextPreviewHUD : MonoBehaviour
{
    public Board board;
    public int cellSize = 12;
    public int padding = 6;

    private void Awake()
    {
        if (board == null)
            board = GetComponent<Board>() ?? FindObjectOfType<Board>();
    }

    private void OnGUI()
    {
        if (board == null) return;

        var previews = board.GetNextPreviewPieces();
        if (previews == null || previews.Length == 0) return;

        int startX = Screen.width - (previewAreaWidth(previews.Length)) - 10;
        int startY = 10;

        GUI.Box(new Rect(startX - 6, startY - 6, previewAreaWidth(previews.Length) + 12, 24 + (cellSize * 4 + padding)), "Next");

        for (int i = 0; i < previews.Length; i++)
        {
            DrawPreview(previews[i], startX + i * (cellSize * 4 + padding), startY + 20);
        }
    }

    private int previewAreaWidth(int count)
    {
        return count * (cellSize * 4 + padding);
    }

    private void DrawPreview(TetrominoData data, int x, int y)
    {
        if (data.cells == null) return;

        // We'll draw inside a 4x4 grid
        for (int i = 0; i < data.cells.Length; i++)
        {
            var c = data.cells[i];
            // normalize coordinates to 4x4 for display
            int drawX = x + (c.x + 1) * cellSize; // shift so negative indices fit
            int drawY = y + (2 - c.y) * cellSize; // invert Y for GUI

            GUI.Box(new Rect(drawX, drawY, cellSize - 1, cellSize - 1), GUIContent.none);
        }
    }
}
