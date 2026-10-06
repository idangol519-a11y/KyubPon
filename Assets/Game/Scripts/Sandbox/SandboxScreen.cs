using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// A small battle test. Each turn the player drags one block onto the grid,
/// the enemy places one of its own (marked with an X), and then every square
/// is activated in order, starting from a random block.
/// This script only runs the turn and draws the board; the fighting rules are
/// in SandboxCombat. See docs/GameState.md for how to remove the sandbox.
/// </summary>
public class SandboxScreen : MonoBehaviour
{
    private const string HomeScreenSceneName = "HomeScreen";
    private const string YourTurnText = "YOUR TURN: DRAG A BLOCK ONTO THE GRID";
    private const string BoardFullText = "GRID IS FULL: PRESS PASS OR RESET";
    private const int StartColumns = SandboxGridSize.MinimumColumns;
    private const int StartRows = SandboxGridSize.MinimumRows;
    private const float GapBetweenCells = 8f;
    private const float PaletteCubeSize = 96f;

    // Pauses that make a turn slow enough to follow.
    private const float EnemyThinkSeconds = 0.6f;
    private const float EmptySquareSeconds = 0.04f;
    private const float BlockSquareSeconds = 0.55f;

    private static readonly Color CellColor = new Color(0.17f, 0.17f, 0.24f);
    private static readonly Color HighlightColor = new Color(0.55f, 0.55f, 0.70f);

    [SerializeField] private RectTransform _gridArea;
    [SerializeField] private RectTransform _dragLayer;
    [SerializeField] private Transform _palette;
    [SerializeField] private Font _font;
    [SerializeField] private UnityEngine.UI.Text _columnsLabel;
    [SerializeField] private UnityEngine.UI.Text _rowsLabel;
    [SerializeField] private UnityEngine.UI.Text _statusLabel;
    [SerializeField] private UnityEngine.UI.Text _rulesLabel;
    [SerializeField] private UnityEngine.UI.Text _statsLabel;
    [SerializeField] private UnityEngine.UI.Button _fewerColumnsButton;
    [SerializeField] private UnityEngine.UI.Button _moreColumnsButton;
    [SerializeField] private UnityEngine.UI.Button _fewerRowsButton;
    [SerializeField] private UnityEngine.UI.Button _moreRowsButton;
    [SerializeField] private UnityEngine.UI.Button _passButton;
    [SerializeField] private UnityEngine.UI.Button _resetButton;
    [SerializeField] private UnityEngine.UI.Button _backButton;

    private readonly List<SandboxCell> _cells = new List<SandboxCell>();
    private readonly List<Vector2Int> _blockSquares = new List<Vector2Int>();
    private readonly List<SandboxBlock> _destroyedBlocks = new List<SandboxBlock>();
    private readonly HashSet<SandboxBlock> _blocksThatActed = new HashSet<SandboxBlock>();
    private readonly System.Random _random = new System.Random();
    private readonly WaitForSeconds _enemyThinkWait = new WaitForSeconds(EnemyThinkSeconds);
    private readonly WaitForSeconds _emptySquareWait = new WaitForSeconds(EmptySquareSeconds);
    private readonly WaitForSeconds _blockSquareWait = new WaitForSeconds(BlockSquareSeconds);

    private SandboxGridSize _gridSize;
    private SandboxBoard _board;
    private SandboxEnemy _enemy;
    private int _turnNumber = 1;
    private int _enemyBlocksDestroyed;
    private int _playerBlocksDestroyed;

    // True while the enemy is placing and the squares are activating.
    // The player cannot place, pass, reset, or resize until it is over.
    private bool _isTurnRunning;

    private void Awake()
    {
        _gridSize = new SandboxGridSize(StartColumns, StartRows);
        _board = new SandboxBoard(_gridSize.Columns, _gridSize.Rows);
        _enemy = new SandboxEnemy(_random);

        _fewerColumnsButton.onClick.AddListener(() => ChangeGridSize(-1, 0));
        _moreColumnsButton.onClick.AddListener(() => ChangeGridSize(1, 0));
        _fewerRowsButton.onClick.AddListener(() => ChangeGridSize(0, -1));
        _moreRowsButton.onClick.AddListener(() => ChangeGridSize(0, 1));
        _passButton.onClick.AddListener(PassTurn);
        _resetButton.onClick.AddListener(ResetBattle);
        _backButton.onClick.AddListener(ReturnToHomeScreen);
    }

    private void Start()
    {
        BuildPalette();
        RebuildCells();
        ShowRules();
        ShowStats();
        ShowWhoseTurn();
    }

    /// <summary>
    /// Called when the player drops a palette block on a square.
    /// Places the block if it is the player's turn and the square is empty,
    /// then lets the rest of the turn play out.
    /// </summary>
    public void TryPlacePlayerBlock(SandboxCubeKind kind, Vector2Int position)
    {
        if (_isTurnRunning || _board.GetBlock(position) != null)
        {
            return;
        }

        _board.Place(new SandboxBlock(kind, SandboxSide.Player), position);
        DrawBoard();
        StartCoroutine(RunRestOfTurn());
    }

    private void PassTurn()
    {
        if (!_isTurnRunning)
        {
            StartCoroutine(RunRestOfTurn());
        }
    }

    private void ResetBattle()
    {
        if (_isTurnRunning)
        {
            return;
        }

        _board.Clear();
        _turnNumber = 1;
        _enemyBlocksDestroyed = 0;
        _playerBlocksDestroyed = 0;
        DrawBoard();
        ShowStats();
        ShowWhoseTurn();
    }

    private void ChangeGridSize(int columnChange, int rowChange)
    {
        if (_isTurnRunning)
        {
            return;
        }

        _gridSize.ChangeColumns(columnChange);
        _gridSize.ChangeRows(rowChange);
        _board.Resize(_gridSize.Columns, _gridSize.Rows);
        RebuildCells();
        ShowStats();
        ShowWhoseTurn();
    }

    private void ReturnToHomeScreen()
    {
        SceneManager.LoadScene(HomeScreenSceneName);
    }

    /// <summary>
    /// The part of a turn that follows the player's move: the enemy places a block,
    /// then every square is activated. A coroutine lets it pause between steps
    /// so the player can watch what happens.
    /// </summary>
    private IEnumerator RunRestOfTurn()
    {
        _isTurnRunning = true;

        _statusLabel.text = "ENEMY IS CHOOSING...";
        yield return _enemyThinkWait;
        SandboxBlock enemyBlock = _enemy.PlaceBlock(_board);
        _statusLabel.text = enemyBlock != null ? $"ENEMY PLACES {enemyBlock.Kind.Name}" : "ENEMY HAS NO ROOM";
        DrawBoard();
        ShowStats();
        yield return _enemyThinkWait;

        yield return ActivateEverySquare();

        _turnNumber++;
        ShowStats();
        ShowWhoseTurn();
        _isTurnRunning = false;
    }

    /// <summary>
    /// Starts at a random block, then visits every square once, in reading order,
    /// wrapping from the last square back to the first. Each block that is reached
    /// uses its ability. A block acts only once per turn, even if it gets pushed
    /// onto a square that has not been visited yet.
    /// </summary>
    private IEnumerator ActivateEverySquare()
    {
        _blocksThatActed.Clear();
        int startIndex = PickStartIndex();

        for (int step = 0; step < _cells.Count; step++)
        {
            SandboxCell cell = _cells[(startIndex + step) % _cells.Count];
            SandboxBlock block = _board.GetBlock(cell.GridPosition);
            cell.SetHighlight(true, HighlightColor);

            if (block != null && _blocksThatActed.Add(block))
            {
                ActivateBlock(block);
                yield return _blockSquareWait;
            }
            else
            {
                yield return _emptySquareWait;
            }

            cell.SetHighlight(false, HighlightColor);
        }
    }

    private void ActivateBlock(SandboxBlock block)
    {
        _destroyedBlocks.Clear();
        SandboxCombat.Activate(_board, block, _destroyedBlocks);

        foreach (SandboxBlock destroyed in _destroyedBlocks)
        {
            if (destroyed.Side == SandboxSide.Enemy)
            {
                _enemyBlocksDestroyed++;
            }
            else
            {
                _playerBlocksDestroyed++;
            }
        }

        string owner = block.Side == SandboxSide.Enemy ? "ENEMY" : "YOUR";
        _statusLabel.text = $"{owner} {block.Kind.Name} {block.Kind.ActionWord}";
        DrawBoard();
        ShowStats();
    }

    /// <summary>
    /// Picks the square of a random block to start from.
    /// With no blocks at all, any square can be the start.
    /// </summary>
    private int PickStartIndex()
    {
        _board.CollectPositions(_blockSquares, false);
        if (_blockSquares.Count == 0)
        {
            return _random.Next(_cells.Count);
        }

        return IndexOf(_blockSquares[_random.Next(_blockSquares.Count)]);
    }

    /// <summary>
    /// Squares are created row by row, so the square at (column, row)
    /// is always at this position in the list.
    /// </summary>
    private int IndexOf(Vector2Int position)
    {
        return position.y * _board.Columns + position.x;
    }

    /// <summary>Creates one block of each kind for the player to drag from.</summary>
    private void BuildPalette()
    {
        foreach (SandboxCubeKind kind in SandboxCubeKind.All)
        {
            SandboxPaletteCube cube = SandboxPaletteCube.Create(_palette, kind, this, _dragLayer);
            UnityEngine.UI.LayoutElement size = cube.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            size.preferredWidth = PaletteCubeSize;
            size.preferredHeight = PaletteCubeSize;
        }
    }

    /// <summary>Throws the old squares away and creates new ones at the board's current size.</summary>
    private void RebuildCells()
    {
        foreach (SandboxCell cell in _cells)
        {
            Destroy(cell.gameObject);
        }
        _cells.Clear();

        float cellSize = CalculateCellSize();
        for (int row = 0; row < _board.Rows; row++)
        {
            for (int column = 0; column < _board.Columns; column++)
            {
                CreateCell(new Vector2Int(column, row), cellSize);
            }
        }

        _columnsLabel.text = _board.Columns.ToString();
        _rowsLabel.text = _board.Rows.ToString();
        DrawBoard();
    }

    /// <summary>The largest square that lets the whole grid fit in its area.</summary>
    private float CalculateCellSize()
    {
        Vector2 area = _gridArea.rect.size;
        return Mathf.Floor(Mathf.Min(area.x / _board.Columns, area.y / _board.Rows));
    }

    private void CreateCell(Vector2Int gridPosition, float cellSize)
    {
        SandboxCell cell = SandboxCell.Create(_gridArea, gridPosition, CellColor, _font);
        _cells.Add(cell);

        // Positions are measured from the middle of the grid area, so the grid is centered.
        // Row 0 is the top row, which is why y is flipped.
        RectTransform rect = (RectTransform)cell.transform;
        float x = (gridPosition.x - (_board.Columns - 1) / 2f) * cellSize;
        float y = ((_board.Rows - 1) / 2f - gridPosition.y) * cellSize;
        rect.sizeDelta = new Vector2(cellSize - GapBetweenCells, cellSize - GapBetweenCells);
        rect.anchoredPosition = new Vector2(x, y);
    }

    /// <summary>Makes every square on screen show what the board holds.</summary>
    private void DrawBoard()
    {
        foreach (SandboxCell cell in _cells)
        {
            cell.Show(_board.GetBlock(cell.GridPosition));
        }
    }

    private void ShowWhoseTurn()
    {
        _board.CollectPositions(_blockSquares, true);
        _statusLabel.text = _blockSquares.Count > 0 ? YourTurnText : BoardFullText;
    }

    private void ShowStats()
    {
        _statsLabel.text =
            "BATTLE\n\n" +
            $"TURN: {_turnNumber}\n\n" +
            $"ENEMY BLOCKS DESTROYED: {_enemyBlocksDestroyed}\n\n" +
            $"YOUR BLOCKS DESTROYED: {_playerBlocksDestroyed}\n\n" +
            $"YOUR BLOCKS ON GRID: {_board.CountBlocks(SandboxSide.Player)}\n\n" +
            $"ENEMY BLOCKS ON GRID: {_board.CountBlocks(SandboxSide.Enemy)}";
    }

    /// <summary>
    /// Writes the rules note from the block list itself, so the note can never
    /// disagree with what the blocks really do.
    /// </summary>
    private void ShowRules()
    {
        StringBuilder rules = new StringBuilder("BLOCKS\n\n");
        foreach (SandboxCubeKind kind in SandboxCubeKind.All)
        {
            string colorCode = ColorUtility.ToHtmlStringRGB(kind.Color);
            rules.Append($"<color=#{colorCode}>{kind.Name}</color>  {kind.MaxHp} HP\n{kind.RuleText}\n\n");
        }

        rules.Append("X = ENEMY BLOCK. THE NUMBER IS ITS HP.\n\n");
        rules.Append("EACH TURN: YOU PLACE, THE ENEMY PLACES, THEN EVERY SQUARE ACTIVATES, STARTING FROM A RANDOM BLOCK.");
        _rulesLabel.text = rules.ToString();
    }
}
