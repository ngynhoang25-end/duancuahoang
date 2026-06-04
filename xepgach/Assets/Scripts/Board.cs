using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-1)]
public class Board : MonoBehaviour
{
    public enum TSpinType { None, Mini, Full }

    public Tilemap tilemap { get; private set; }
    public Piece activePiece { get; private set; }

    public TetrominoData[] tetrominoes;
    public Vector2Int boardSize = new Vector2Int(10, 20);
    public Vector3Int spawnPosition = new Vector3Int(-1, 8, 0);
    [SerializeField] private int previewCount = 3;

    public int Score { get; private set; }
    public int Level { get; private set; } = 1;
    public int TotalLinesCleared { get; private set; }
    public int Combo { get; private set; } = -1;

    public bool HasHoldPiece => hasHoldPiece;

    private readonly Queue<TetrominoData> nextQueue = new Queue<TetrominoData>();
    private bool canHold = true;
    private bool hasHoldPiece;
    private TetrominoData heldPiece;

    public RectInt Bounds
    {
        get
        {
            Vector2Int position = new Vector2Int(-boardSize.x / 2, -boardSize.y / 2);
            return new RectInt(position, boardSize);
        }
    }

    private void Awake()
    {
        tilemap = GetComponentInChildren<Tilemap>();
        activePiece = GetComponentInChildren<Piece>();

        for (int i = 0; i < tetrominoes.Length; i++) {
            tetrominoes[i].Initialize();
        }

        EnsureNextQueue(previewCount + 1);
        // Ensure ghost references if a Ghost exists in the scene
        Ghost ghost = FindObjectOfType<Ghost>();
        if (ghost != null)
        {
            ghost.mainBoard = this;
            if (activePiece != null)
                ghost.trackingPiece = activePiece;
        }
    }

    private void Start()
    {
        SpawnPiece();
    }

    public void SpawnPiece()
    {
        SpawnNextPiece(true);
    }

    private void SpawnNextPiece(bool resetHold)
    {
        EnsureNextQueue(previewCount + 1);

        TetrominoData data = nextQueue.Dequeue();

        activePiece.Initialize(this, spawnPosition, data);
        canHold = resetHold;

        if (IsValidPosition(activePiece, spawnPosition)) {
            Set(activePiece);
        } else {
            GameOver();
        }
        // Update ghost tracking whenever a new piece spawns
        Ghost ghost = FindObjectOfType<Ghost>();
        if (ghost != null)
        {
            ghost.mainBoard = this;
            ghost.trackingPiece = activePiece;
        }
    }

    public void GameOver()
    {
        tilemap.ClearAllTiles();
    }

    public TetrominoData[] GetNextPreviewPieces()
    {
        EnsureNextQueue(previewCount);

        TetrominoData[] preview = new TetrominoData[Mathf.Min(previewCount, nextQueue.Count)];
        int index = 0;

        foreach (TetrominoData piece in nextQueue)
        {
            if (index >= preview.Length) {
                break;
            }

            preview[index] = piece;
            index++;
        }

        return preview;
    }

    public bool TryGetHeldPiece(out TetrominoData piece)
    {
        piece = heldPiece;
        return hasHoldPiece;
    }

    public bool TryHold(Piece piece)
    {
        if (!canHold || piece != activePiece) {
            return false;
        }

        Clear(piece);

        TetrominoData currentPiece = piece.data;

        if (hasHoldPiece)
        {
            TetrominoData swapPiece = heldPiece;
            heldPiece = currentPiece;
            piece.Initialize(this, spawnPosition, swapPiece);

            if (IsValidPosition(piece, spawnPosition)) {
                Set(piece);
            } else {
                GameOver();
            }
        }
        else
        {
            heldPiece = currentPiece;
            hasHoldPiece = true;
            SpawnNextPiece(false);
        }

        canHold = false;
        return true;
    }

    public void LockPiece(Piece piece)
    {
        Set(piece);
        TSpinType tSpin = GetTSpinType(piece);
        int linesCleared = ClearLines();

        UpdateScore(linesCleared, tSpin);
        SpawnPiece();
    }

    public void AddSoftDropScore(int amount)
    {
        if (amount > 0) {
            Score += amount;
        }
    }

    public void AddHardDropScore(int amount)
    {
        if (amount > 0) {
            Score += amount * 2;
        }
    }

    public void Set(Piece piece)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = piece.cells[i] + piece.position;
            tilemap.SetTile(tilePosition, piece.data.tile);
        }
    }

    public void Clear(Piece piece)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = piece.cells[i] + piece.position;
            tilemap.SetTile(tilePosition, null);
        }
    }

    public bool IsValidPosition(Piece piece, Vector3Int position)
    {
        RectInt bounds = Bounds;

        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = piece.cells[i] + position;

            if (!bounds.Contains((Vector2Int)tilePosition)) {
                return false;
            }

            if (tilemap.HasTile(tilePosition)) {
                return false;
            }
        }

        return true;
    }

    public int ClearLines()
    {
        RectInt bounds = Bounds;
        int row = bounds.yMin;
        int linesCleared = 0;

        while (row < bounds.yMax)
        {
            if (IsLineFull(row)) {
                LineClear(row);
                linesCleared++;
            } else {
                row++;
            }
        }

        if (linesCleared > 0) {
            TotalLinesCleared += linesCleared;
            Level = 1 + (TotalLinesCleared / 10);
            Combo++;
        } else {
            Combo = -1;
        }

        return linesCleared;
    }

    public bool IsLineFull(int row)
    {
        RectInt bounds = Bounds;

        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);

            if (!tilemap.HasTile(position)) {
                return false;
            }
        }

        return true;
    }

    public void LineClear(int row)
    {
        RectInt bounds = Bounds;

        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);
            tilemap.SetTile(position, null);
        }

        while (row < bounds.yMax)
        {
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row + 1, 0);
                TileBase above = tilemap.GetTile(position);

                position = new Vector3Int(col, row, 0);
                tilemap.SetTile(position, above);
            }

            row++;
        }
    }

    private void EnsureNextQueue(int minimumCount)
    {
        while (nextQueue.Count < minimumCount)
        {
            EnqueueRandomBag();
        }
    }

    private void EnqueueRandomBag()
    {
        List<int> indices = new List<int>(tetrominoes.Length);

        for (int i = 0; i < tetrominoes.Length; i++) {
            indices.Add(i);
        }

        for (int i = indices.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[swapIndex];
            indices[swapIndex] = temp;
        }

        for (int i = 0; i < indices.Count; i++) {
            nextQueue.Enqueue(tetrominoes[indices[i]]);
        }
    }

    private void UpdateScore(int linesCleared, TSpinType tSpin)
    {
        int lineScore = 0;

        if (tSpin != TSpinType.None)
        {
            switch (linesCleared)
            {
                case 1:
                    lineScore = (tSpin == TSpinType.Full) ? 800 : 200; // full T-Spin single vs mini
                    break;
                case 2:
                    lineScore = 1200;
                    break;
                case 3:
                    lineScore = 1600;
                    break;
                default:
                    lineScore = 400;
                    break;
            }
        }
        else
        {
            switch (linesCleared)
            {
                case 1:
                    lineScore = 100;
                    break;
                case 2:
                    lineScore = 300;
                    break;
                case 3:
                    lineScore = 500;
                    break;
                case 4:
                    lineScore = 800;
                    break;
                default:
                    lineScore = 0;
                    break;
            }
        }

        if (linesCleared > 0)
        {
            Score += lineScore * Level;

            if (Combo > 0) {
                Score += Combo * 50 * Level;
            }
        }
    }

    private TSpinType GetTSpinType(Piece piece)
    {
        if (piece.data.tetromino != Tetromino.T || !piece.RotatedThisTurn) {
            return TSpinType.None;
        }

        Vector3Int center = piece.position;
        int blockedCorners = 0;

        if (IsCellBlocked(new Vector3Int(center.x - 1, center.y - 1, 0))) blockedCorners++;
        if (IsCellBlocked(new Vector3Int(center.x + 1, center.y - 1, 0))) blockedCorners++;
        if (IsCellBlocked(new Vector3Int(center.x - 1, center.y + 1, 0))) blockedCorners++;
        if (IsCellBlocked(new Vector3Int(center.x + 1, center.y + 1, 0))) blockedCorners++;

        if (blockedCorners < 3) return TSpinType.None;

        // Simple heuristic: if a rotation used a non-zero wall-kick, treat as full T-Spin
        if (piece.LastRotationKick != Vector2Int.zero) return TSpinType.Full;

        // Otherwise classify as a Mini
        return TSpinType.Mini;
    }

    private bool IsCellBlocked(Vector3Int position)
    {
        RectInt bounds = Bounds;

        if (!bounds.Contains((Vector2Int)position)) {
            return true;
        }

        return tilemap.HasTile(position);
    }
}
