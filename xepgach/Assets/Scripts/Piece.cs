using UnityEngine;

public class Piece : MonoBehaviour
{
    public Board board { get; private set; }
    public TetrominoData data { get; private set; }
    public Vector3Int[] cells { get; private set; }
    public Vector3Int position { get; private set; }
    public int rotationIndex { get; private set; }
    public bool RotatedThisTurn { get; private set; }
    public Vector2Int LastRotationKick { get; private set; }

    public float stepDelay = 1f;
    public float moveDelay = 0.1f;
    public float lockDelay = 0.5f;
    public float swipeThreshold = 40f;
    public float swipeSoftDropDuration = 0.25f;

    private float stepTime;
    private float moveTime;
    private float lockTime;
    private bool softDropHeld;
    private int trackedTouchId = -1;
    private Vector2 touchStartPosition;
    private float softDropUntilTime;

    public void Initialize(Board board, Vector3Int position, TetrominoData data)
    {
        this.data = data;
        this.board = board;
        this.position = position;

        rotationIndex = 0;
        RotatedThisTurn = false;
        LastRotationKick = Vector2Int.zero;
        stepTime = Time.time + stepDelay;
        moveTime = Time.time + moveDelay;
        lockTime = 0f;
        softDropHeld = false;
        trackedTouchId = -1;
        softDropUntilTime = 0f;

        if (cells == null) {
            cells = new Vector3Int[data.cells.Length];
        }

        for (int i = 0; i < cells.Length; i++) {
            cells[i] = (Vector3Int)data.cells[i];
        }
    }

    private void Update()
    {
        board.Clear(this);

        lockTime += Time.deltaTime;

        UpdateTouchSoftDropState();

        if (HandleMobileInput())
        {
            board.Set(this);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q)) {
            Rotate(-1);
        } else if (Input.GetKeyDown(KeyCode.E)) {
            Rotate(1);
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            HardDrop();
            board.Set(this);
            return;
        }

        if (Time.time > moveTime) {
            HandleMoveInputs();
        }

        if (Time.time > stepTime) {
            Step();
        }

        board.Set(this);
    }

    public bool RequestHold()
    {
        return board.TryHold(this);
    }

    public bool RequestHardDrop()
    {
        HardDrop();
        return true;
    }

    public void MoveLeft()
    {
        Move(Vector2Int.left);
    }

    public void MoveRight()
    {
        Move(Vector2Int.right);
    }

    public void RotateLeft()
    {
        Rotate(-1);
    }

    public void RotateRight()
    {
        Rotate(1);
    }

    public void SetSoftDropHeld(bool held)
    {
        softDropHeld = held;
    }

    private void HandleMoveInputs()
    {
        if (Input.GetKey(KeyCode.S) || softDropHeld)
        {
            if (Move(Vector2Int.down, true)) {
                stepTime = Time.time + stepDelay;
            }
        }

        if (Input.GetKey(KeyCode.A)) {
            Move(Vector2Int.left);
        } else if (Input.GetKey(KeyCode.D)) {
            Move(Vector2Int.right);
        }
    }

    private void Step()
    {
        stepTime = Time.time + stepDelay;
        Move(Vector2Int.down);

        if (lockTime >= lockDelay) {
            Lock();
        }
    }

    private void HardDrop()
    {
        int droppedDistance = 0;

        while (Move(Vector2Int.down)) {
            droppedDistance++;
        }

        board.AddHardDropScore(droppedDistance);
        Lock();
    }

    private void Lock()
    {
        board.LockPiece(this);
    }

    private bool Move(Vector2Int translation, bool softDrop = false)
    {
        Vector3Int newPosition = position;
        newPosition.x += translation.x;
        newPosition.y += translation.y;

        bool valid = board.IsValidPosition(this, newPosition);

        if (valid)
        {
            position = newPosition;
            moveTime = Time.time + moveDelay;
            lockTime = 0f;

            if (softDrop) {
                board.AddSoftDropScore(1);
            }
        }

        return valid;
    }

    private void Rotate(int direction)
    {
        int originalRotation = rotationIndex;

        rotationIndex = Wrap(rotationIndex + direction, 0, 4);
        ApplyRotationMatrix(direction);

        if (!TestWallKicks(rotationIndex, direction))
        {
            rotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
            RotatedThisTurn = false;
            LastRotationKick = Vector2Int.zero;
        }
        else
        {
            RotatedThisTurn = true;
        }
    }

    private void ApplyRotationMatrix(int direction)
    {
        float[] matrix = Data.RotationMatrix;

        for (int i = 0; i < cells.Length; i++)
        {
            Vector3 cell = cells[i];

            int x, y;

            switch (data.tetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                    cell.x -= 0.5f;
                    cell.y -= 0.5f;
                    x = Mathf.CeilToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.CeilToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;

                default:
                    x = Mathf.RoundToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.RoundToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;
            }

            cells[i] = new Vector3Int(x, y, 0);
        }
    }

    private bool TestWallKicks(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = GetWallKickIndex(rotationIndex, rotationDirection);

        for (int i = 0; i < data.wallKicks.GetLength(1); i++)
        {
            Vector2Int translation = data.wallKicks[wallKickIndex, i];

            if (Move(translation)) {
                LastRotationKick = translation;
                return true;
            }
        }

        LastRotationKick = Vector2Int.zero;
        return false;
    }

    private int GetWallKickIndex(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = rotationIndex * 2;

        if (rotationDirection < 0) {
            wallKickIndex--;
        }

        return Wrap(wallKickIndex, 0, data.wallKicks.GetLength(0));
    }

    private int Wrap(int input, int min, int max)
    {
        if (input < min) {
            return max - (min - input) % (max - min);
        } else {
            return min + (input - min) % (max - min);
        }
    }

    private void UpdateTouchSoftDropState()
    {
        softDropHeld = Time.time < softDropUntilTime;
    }

    private bool HandleMobileInput()
    {
        if (Input.touchCount <= 0) {
            return false;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase == TouchPhase.Began)
            {
                trackedTouchId = touch.fingerId;
                touchStartPosition = touch.position;
            }

            if (touch.fingerId != trackedTouchId) {
                continue;
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                Vector2 delta = touch.position - touchStartPosition;

                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x) && Mathf.Abs(delta.y) > swipeThreshold)
                {
                    if (delta.y > 0f)
                    {
                        HardDrop();
                        return true;
                    }

                    softDropUntilTime = Time.time + swipeSoftDropDuration;
                    return true;
                }

                if (Mathf.Abs(delta.x) > swipeThreshold)
                {
                    if (delta.x < 0f) {
                        Move(Vector2Int.left);
                    } else {
                        Move(Vector2Int.right);
                    }

                    return true;
                }

                if (touch.position.x < Screen.width / 3f)
                {
                    Move(Vector2Int.left);
                    return true;
                }

                if (touch.position.x > (Screen.width * 2f) / 3f)
                {
                    Move(Vector2Int.right);
                    return true;
                }

                Rotate(1);
                return true;
            }
        }

        return false;
    }
}
