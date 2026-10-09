using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// A small battle test. Each turn the player drags one block onto the grid,
/// the enemy places one of its own (marked with an X), and then every square
/// is activated in order, starting from a random block.
/// Holding the mouse over a block for a moment shows a tooltip with its rules.
/// This script only runs the turn and draws the board; the fighting rules are
/// in SandboxCombat. See docs/GameState.md for how to remove the sandbox.
/// </summary>
public class SandboxScreen : MonoBehaviour
{
    private const string HomeScreenSceneName = "HomeScreen";
    private const string YourTurnText = "YOUR TURN: DRAG A BLOCK ONTO THE GRID. HOVER A BLOCK FOR ITS RULES";
    private const string BoardFullText = "GRID IS FULL: PRESS PASS OR RESET";
    private const int StartColumns = SandboxGridSize.MinimumColumns;
    private const int StartRows = SandboxGridSize.MinimumRows;
    private const float GapBetweenCells = 8f;
    private const float PaletteCubeSize = 96f;

    // Pauses that make a turn slow enough to follow.
    private const float EnemyThinkSeconds = 0.6f;
    private const float EmptySquareSeconds = 0.04f;
    private const float BlockSquareSeconds = 0.55f;

    // How long the mouse must rest on a block before its rules appear,
    // and how far from the mouse the tooltip is drawn (in screen pixels).
    private const float HoverSecondsBeforeTooltip = 1f;
    private const float TooltipDistanceFromPointer = 20f;

    private static readonly Color CellColor = new Color(0.17f, 0.17f, 0.24f);
    private static readonly Color HighlightColor = new Color(0.55f, 0.55f, 0.70f);

    // See-through tints for a hovered block's area of effect.
    private static readonly Color AttackAreaColor = new Color(0.95f, 0.15f, 0.15f, 0.45f);
    private static readonly Color OtherAreaColor = new Color(0.20f, 0.50f, 1.00f, 0.45f);

    [SerializeField] private RectTransform _gridArea;
    [SerializeField] private RectTransform _dragLayer;
    [SerializeField] private Transform _palette;
    [SerializeField] private Font _font;
    [SerializeField] private RectTransform _tooltip;
    [SerializeField] private UnityEngine.UI.Text _tooltipText;
    [SerializeField] private UnityEngine.UI.Text _columnsLabel;
    [SerializeField] private UnityEngine.UI.Text _rowsLabel;
    [SerializeField] private UnityEngine.UI.Text _statusLabel;
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
    private readonly List<Vector2Int> _attackSquares = new List<Vector2Int>();
    private readonly List<Vector2Int> _otherEffectSquares = new List<Vector2Int>();
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

    // What the mouse is resting on. A palette block sets the kind; a grid square sets the cell.
    private bool _isHovering;
    private bool _isDraggingBlock;
    private float _hoverStartTime;
    private SandboxCubeKind _hoveredPaletteKind;
    private SandboxCell _hoveredCell;

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
        HideTooltip();
        BuildPalette();
        RebuildCells();
        ShowStats();
        ShowWhoseTurn();
    }

    private void Update()
    {
        bool hasWaitedLongEnough = Time.unscaledTime - _hoverStartTime >= HoverSecondsBeforeTooltip;
        if (_isHovering && hasWaitedLongEnough && !_tooltip.gameObject.activeSelf)
        {
            ShowTooltip();
        }
    }

    /// <summary>
    /// Called when the mouse moves onto a palette block (pass its kind) or a
    /// grid square (pass the cell). The tooltip appears if the mouse stays there.
    /// </summary>
    public void StartHover(SandboxCubeKind paletteKind, SandboxCell cell)
    {
        // No tooltip while a block is being dragged across the grid.
        if (_isDraggingBlock)
        {
            return;
        }

        HideTooltip();
        _hoveredPaletteKind = paletteKind;
        _hoveredCell = cell;
        _hoverStartTime = Time.unscaledTime;
        _isHovering = true;

        // The area of effect shows at once; only the rules tooltip waits.
        ShowAreaOfEffect();
    }

    /// <summary>
    /// Called when the mouse leaves a palette block or a grid square. Hides the tooltip,
    /// but only if that is still the thing being hovered. Unity can report "left the
    /// old one" after "entered the new one", and that late report must not cancel the new hover.
    /// </summary>
    public void EndHover(SandboxCubeKind paletteKind, SandboxCell cell)
    {
        if (_hoveredPaletteKind == paletteKind && _hoveredCell == cell)
        {
            StopHover();
        }
    }

    /// <summary>Called when the player starts or stops dragging a palette block.</summary>
    public void SetDraggingBlock(bool isDragging)
    {
        _isDraggingBlock = isDragging;
        if (isDragging)
        {
            StopHover();
        }
    }

    private void StopHover()
    {
        _isHovering = false;
        _hoveredPaletteKind = null;
        _hoveredCell = null;
        HideTooltip();
        ShowAreaOfEffect();
    }

    /// <summary>
    /// Tints the squares that the block under the mouse can affect: red where it
    /// attacks, blue for any other effect. With no placed block under the mouse,
    /// all tints are simply removed.
    /// </summary>
    private void ShowAreaOfEffect()
    {
        foreach (SandboxCell cell in _cells)
        {
            cell.HideAreaTint();
        }

        SandboxBlock block = _hoveredCell != null ? _board.GetBlock(_hoveredCell.GridPosition) : null;
        if (block == null)
        {
            return;
        }

        SandboxCombat.CollectAreaOfEffect(_board, block, _attackSquares, _otherEffectSquares);
        foreach (Vector2Int square in _attackSquares)
        {
            _cells[IndexOf(square)].ShowAreaTint(AttackAreaColor);
        }

        foreach (Vector2Int square in _otherEffectSquares)
        {
            _cells[IndexOf(square)].ShowAreaTint(OtherAreaColor);
        }
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
        // The square under the mouse is about to be destroyed.
        StopHover();
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
        SandboxCell cell = SandboxCell.Create(_gridArea, gridPosition, CellColor, _font, this);
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

        // The block under the mouse may have lost HP, moved, or been destroyed.
        ShowAreaOfEffect();
        if (_tooltip.gameObject.activeSelf)
        {
            ShowTooltip();
        }
    }

    private void ShowWhoseTurn()
    {
        _board.CollectPositions(_blockSquares, true);
        _statusLabel.text = _blockSquares.Count > 0 ? YourTurnText : BoardFullText;
    }

    private void ShowStats()
    {
        _statsLabel.text = $"TURN {_turnNumber}     ENEMY BLOCKS DESTROYED: {_enemyBlocksDestroyed}     " +
            $"YOUR BLOCKS DESTROYED: {_playerBlocksDestroyed}";
    }

    /// <summary>
    /// Shows the rules of whatever block the mouse is resting on, next to the mouse.
    /// The text is built from the block list itself, so it can never disagree
    /// with what the blocks really do. An empty square shows nothing.
    /// </summary>
    private void ShowTooltip()
    {
        SandboxBlock block = _hoveredCell != null ? _board.GetBlock(_hoveredCell.GridPosition) : null;
        SandboxCubeKind kind = block != null ? block.Kind : _hoveredPaletteKind;
        if (kind == null)
        {
            HideTooltip();
            return;
        }

        string owner = block == null ? string.Empty : block.Side == SandboxSide.Enemy ? "ENEMY " : "YOUR ";
        string hp = block == null ? $"HP {kind.MaxHp}" : $"HP {block.Hp} / {kind.MaxHp}";
        string colorCode = ColorUtility.ToHtmlStringRGB(kind.Color);
        _tooltipText.text = $"{owner}<color=#{colorCode}>{kind.Name}</color>\n{hp}\n\n{kind.RuleText}";

        bool wasHidden = !_tooltip.gameObject.activeSelf;
        _tooltip.gameObject.SetActive(true);
        if (wasHidden)
        {
            PlaceTooltipNextToPointer();
        }
    }

    private void HideTooltip()
    {
        _tooltip.gameObject.SetActive(false);
    }

    /// <summary>
    /// Puts the tooltip below and to the right of the mouse. If it would run off
    /// the screen there, it flips to the other side so it is always fully visible.
    /// </summary>
    private void PlaceTooltipNextToPointer()
    {
        if (Pointer.current == null)
        {
            return;
        }

        // The tooltip's height depends on its text, so its layout must be worked out before measuring it.
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltip);
        Vector2 pointer = Pointer.current.position.ReadValue();
        Vector2 sizeOnScreen = _tooltip.rect.size * _tooltip.lossyScale.x;

        bool fitsOnRight = pointer.x + TooltipDistanceFromPointer + sizeOnScreen.x <= Screen.width;
        bool fitsBelow = pointer.y - TooltipDistanceFromPointer - sizeOnScreen.y >= 0f;

        // The pivot is the corner of the tooltip that sits next to the mouse.
        _tooltip.pivot = new Vector2(fitsOnRight ? 0f : 1f, fitsBelow ? 1f : 0f);
        _tooltip.position = pointer + new Vector2(
            fitsOnRight ? TooltipDistanceFromPointer : -TooltipDistanceFromPointer,
            fitsBelow ? -TooltipDistanceFromPointer : TooltipDistanceFromPointer);
    }
}
