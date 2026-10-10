using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The battle screen, opened by New Game and Continue on the Home screen. Both sides start with a hand of random blocks. Each turn
/// the player drags one block from their hand onto the grid, the enemy places one
/// of its own (marked with an X), and then every square is activated once, either
/// left to right from a random block or in a random order (the ORDER button). Once the hands are empty the turns keep playing
/// by themselves until one side has no blocks left (the other side wins) or the
/// board has looked exactly the same three times (a draw).
/// Holding the mouse over a block for a moment shows a tooltip with its rules.
/// The item library on the left holds the player's items: a potion that can be used
/// once, and a gem that lets the player's blue blocks be dragged to another square.
/// The battle is saved after every turn (an "AUTOSAVING..." note shows for a moment),
/// so Continue brings it back exactly as it was at the end of the last finished turn.
/// This script only runs the turn and draws the board; the fighting rules are
/// in BattleCombat and the save file is handled by BattleSave.
/// </summary>
public class BattleScreen : MonoBehaviour
{
    private const string HomeScreenSceneName = "HomeScreen";
    private const string YourTurnText = "YOUR TURN: DRAG A BLOCK ONTO THE GRID. HOVER A BLOCK FOR ITS RULES";
    private const string BoardFullText = "GRID IS FULL: PRESS PASS OR RESET";
    private const string OrderLeftToRightText = "ORDER: LEFT TO RIGHT";
    private const string OrderRandomText = "ORDER: RANDOM";

    // The item pictures: StreamingAssets/Content/Items/<name>/<name>.png.
    private const string PotionIconName = "Potion";
    private const string GemIconName = "Gem";
    private const string PotionReadyText = "CLICK TO USE";
    private const string PotionActiveText = "ACTIVE THIS TURN";
    private const string PotionUsedText = "USED UP";

    // The battle is a draw when the board has looked exactly the same this many times.
    private const int RepeatsForDraw = 3;

    // A last safety stop, in case a battle somehow never ends and never repeats.
    private const int MostTurnsPerBattle = 300;
    private const int StartColumns = BattleGridSize.MinimumColumns;
    private const int StartRows = BattleGridSize.MinimumRows;
    private const float GapBetweenCells = 8f;
    private const float PaletteCubeSize = 64f;

    // Pauses that make a turn slow enough to follow.
    private const float EnemyThinkSeconds = 0.6f;
    private const float EmptySquareSeconds = 0.04f;
    private const float BlockSquareSeconds = 0.55f;

    // How long the "AUTOSAVING..." note stays on screen after each save.
    private const float AutosaveLabelSeconds = 1.2f;

    // How long the mouse must rest on a block before its rules appear,
    // and how far from the mouse the tooltip is drawn (in screen pixels).
    private const float HoverSecondsBeforeTooltip = 1f;
    private const float TooltipDistanceFromPointer = 20f;
    private const int TooltipTitleFontSize = 40;

    private static readonly Color CellColor = new Color(0.17f, 0.17f, 0.24f);
    private static readonly Color HighlightColor = new Color(0.55f, 0.55f, 0.70f);

    // Square colors for a hovered block's area of effect.
    private static readonly Color AttackAreaColor = new Color(0.80f, 0.16f, 0.18f);
    private static readonly Color OtherAreaColor = new Color(0.16f, 0.40f, 0.85f);

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
    [SerializeField] private UnityEngine.UI.Text _autosaveLabel;
    [SerializeField] private UnityEngine.UI.Button _fewerColumnsButton;
    [SerializeField] private UnityEngine.UI.Button _moreColumnsButton;
    [SerializeField] private UnityEngine.UI.Button _fewerRowsButton;
    [SerializeField] private UnityEngine.UI.Button _moreRowsButton;
    [SerializeField] private UnityEngine.UI.Button _orderButton;
    [SerializeField] private UnityEngine.UI.Text _orderLabel;
    [SerializeField] private UnityEngine.UI.Button _passButton;
    [SerializeField] private UnityEngine.UI.Button _resetButton;
    [SerializeField] private UnityEngine.UI.Button _backButton;
    [SerializeField] private UnityEngine.UI.Button _potionButton;
    [SerializeField] private UnityEngine.UI.Image _potionIcon;
    [SerializeField] private UnityEngine.UI.Text _potionStateLabel;
    [SerializeField] private UnityEngine.UI.Image _gemIcon;

    private readonly List<BattleCell> _cells = new List<BattleCell>();
    private readonly List<Vector2Int> _blockSquares = new List<Vector2Int>();
    private readonly List<BattleBlock> _destroyedBlocks = new List<BattleBlock>();
    private readonly List<Vector2Int> _attackSquares = new List<Vector2Int>();
    private readonly List<Vector2Int> _hitSquares = new List<Vector2Int>();
    private readonly List<int> _visitOrder = new List<int>();
    private readonly Dictionary<string, int> _timesPositionSeen = new Dictionary<string, int>();
    private readonly StringBuilder _positionText = new StringBuilder();
    private readonly List<Vector2Int> _otherEffectSquares = new List<Vector2Int>();
    private readonly HashSet<BattleBlock> _blocksThatActed = new HashSet<BattleBlock>();
    private readonly System.Random _random = new System.Random();
    private readonly WaitForSeconds _enemyThinkWait = new WaitForSeconds(EnemyThinkSeconds);
    private readonly WaitForSeconds _emptySquareWait = new WaitForSeconds(EmptySquareSeconds);
    private readonly WaitForSeconds _blockSquareWait = new WaitForSeconds(BlockSquareSeconds);
    private readonly WaitForSeconds _autosaveLabelWait = new WaitForSeconds(AutosaveLabelSeconds);

    private BattleGridSize _gridSize;
    private BattleBoard _board;
    private BattleEnemy _enemy;
    private readonly BattleHand _playerHand = new BattleHand();
    private readonly BattleHand _enemyHand = new BattleHand();
    private readonly BattleItems _items = new BattleItems();
    private readonly List<GameObject> _paletteCubes = new List<GameObject>();

    // True once a winner or a draw has been announced. Only RESET and BACK work then.
    private bool _isBattleOver;

    // How squares are visited during activation. False: left to right, row by row,
    // starting from a random block. True: every square once, in a shuffled order.
    private bool _isRandomOrder;
    private string _resultText;
    private float _cellSize;

    // The running "show the autosave note, then hide it" routine, so a new save can restart it.
    private Coroutine _autosaveLabelRoutine;

    // How many hits have landed this turn. Each one plays a higher "hit chain" sound.
    private int _hitsThisTurn;
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
    private BattleCubeKind _hoveredPaletteKind;
    private BattleCell _hoveredCell;

    private void Awake()
    {
        _gridSize = new BattleGridSize(StartColumns, StartRows);
        _board = new BattleBoard(_gridSize.Columns, _gridSize.Rows);
        _enemy = new BattleEnemy(_random);

        _fewerColumnsButton.onClick.AddListener(() => ChangeGridSize(-1, 0));
        _moreColumnsButton.onClick.AddListener(() => ChangeGridSize(1, 0));
        _fewerRowsButton.onClick.AddListener(() => ChangeGridSize(0, -1));
        _moreRowsButton.onClick.AddListener(() => ChangeGridSize(0, 1));
        _orderButton.onClick.AddListener(ToggleActivationOrder);
        _passButton.onClick.AddListener(PassTurn);
        _resetButton.onClick.AddListener(ResetBattle);
        _backButton.onClick.AddListener(ReturnToHomeScreen);
        _potionButton.onClick.AddListener(UsePotion);
    }

    /// <summary>The object dragged blocks are put under, so they are drawn on top of the grid.</summary>
    public RectTransform DragLayer => _dragLayer;

    private void Start()
    {
        HideTooltip();
        ShowItemPicture(_potionIcon, PotionIconName);
        ShowItemPicture(_gemIcon, GemIconName);
        _autosaveLabel.gameObject.SetActive(false);

        // Continue arrives here with a save on disk. New Game deletes the save first,
        // so it arrives with none and a fresh battle is dealt.
        BattleSaveData savedBattle = BattleSave.Load();
        if (savedBattle != null)
        {
            RestoreBattle(savedBattle);
        }
        else
        {
            RebuildCells();
            ShowActivationOrder();
            StartNewBattle();
        }
    }

    /// <summary>
    /// Switches between the two activation orders. It can be pressed at any time;
    /// a turn that is already playing finishes in the order it started with.
    /// </summary>
    private void ToggleActivationOrder()
    {
        _isRandomOrder = !_isRandomOrder;
        ShowActivationOrder();

        // In the middle of a turn the board is half-changed, so the save waits for the turn to end.
        if (!_isTurnRunning)
        {
            Autosave();
        }
    }

    private void ShowActivationOrder()
    {
        _orderLabel.text = _isRandomOrder ? OrderRandomText : OrderLeftToRightText;
    }

    /// <summary>Clears the grid and the counters and deals both sides a new random hand.</summary>
    private void StartNewBattle()
    {
        _board.Clear();
        _playerHand.Deal(_random);
        _enemyHand.Deal(_random);
        _turnNumber = 1;
        _enemyBlocksDestroyed = 0;
        _playerBlocksDestroyed = 0;
        _isBattleOver = false;
        _timesPositionSeen.Clear();
        _items.Reset();

        ShowItems();
        DrawBoard();
        DrawHand();
        ShowStats();
        ShowWhoseTurn();
        GameAudio.Play(GameSound.DealHands);
        Autosave();
    }

    /// <summary>
    /// Writes the battle to the save file and shows the "AUTOSAVING..." note for a moment.
    /// Only called between turns, so a saved battle never holds a half-played turn.
    /// </summary>
    private void Autosave()
    {
        BattleSave.Save(CaptureBattle());

        if (_autosaveLabelRoutine != null)
        {
            StopCoroutine(_autosaveLabelRoutine);
        }

        _autosaveLabelRoutine = StartCoroutine(ShowAutosaveLabel());
    }

    private IEnumerator ShowAutosaveLabel()
    {
        _autosaveLabel.gameObject.SetActive(true);
        yield return _autosaveLabelWait;
        _autosaveLabel.gameObject.SetActive(false);
        _autosaveLabelRoutine = null;
    }

    /// <summary>Copies everything about the battle into save data.</summary>
    private BattleSaveData CaptureBattle()
    {
        BattleSaveData data = new BattleSaveData
        {
            Columns = _board.Columns,
            Rows = _board.Rows,
            TurnNumber = _turnNumber,
            EnemyBlocksDestroyed = _enemyBlocksDestroyed,
            PlayerBlocksDestroyed = _playerBlocksDestroyed,
            IsRandomOrder = _isRandomOrder,
            IsBattleOver = _isBattleOver,
            ResultText = _resultText,
            IsPotionUsed = _items.IsPotionUsed,
            IsPotionActive = _items.IsPotionActive
        };

        _board.CollectPositions(_blockSquares, false);
        foreach (Vector2Int square in _blockSquares)
        {
            BattleBlock block = _board.GetBlock(square);
            data.Blocks.Add(new BattleSavedBlock
            {
                Kind = block.Kind.SkinName,
                IsEnemy = block.Side == BattleSide.Enemy,
                Hp = block.Hp,
                Column = square.x,
                Row = square.y
            });
        }

        CaptureHand(_playerHand, data.PlayerHand);
        CaptureHand(_enemyHand, data.EnemyHand);

        foreach (KeyValuePair<string, int> seen in _timesPositionSeen)
        {
            data.SeenPositions.Add(seen.Key);
            data.SeenPositionCounts.Add(seen.Value);
        }

        return data;
    }

    private static void CaptureHand(BattleHand hand, List<string> kindNames)
    {
        for (int index = 0; index < hand.Count; index++)
        {
            kindNames.Add(hand.GetBlock(index).SkinName);
        }
    }

    /// <summary>
    /// Puts a saved battle back on screen. Anything in the save that no longer
    /// makes sense (an unknown block, a square off the grid) is skipped with a warning.
    /// </summary>
    private void RestoreBattle(BattleSaveData data)
    {
        _gridSize = new BattleGridSize(data.Columns, data.Rows);
        _board.Clear();
        _board.Resize(_gridSize.Columns, _gridSize.Rows);

        foreach (BattleSavedBlock saved in data.Blocks)
        {
            BattleCubeKind kind = BattleCubeKind.FindBySkinName(saved.Kind);
            Vector2Int square = new Vector2Int(saved.Column, saved.Row);
            if (kind == null || !_board.IsInside(square) || _board.GetBlock(square) != null)
            {
                Debug.LogWarning($"[Warn] Saved battle: skipped block '{saved.Kind}' at column {saved.Column}, row {saved.Row}.");
                continue;
            }

            BattleBlock block = new BattleBlock(kind, saved.IsEnemy ? BattleSide.Enemy : BattleSide.Player);
            block.RestoreHp(saved.Hp);
            _board.Place(block, square);
        }

        RestoreHand(_playerHand, data.PlayerHand);
        RestoreHand(_enemyHand, data.EnemyHand);

        _turnNumber = Mathf.Max(1, data.TurnNumber);
        _enemyBlocksDestroyed = data.EnemyBlocksDestroyed;
        _playerBlocksDestroyed = data.PlayerBlocksDestroyed;
        _isRandomOrder = data.IsRandomOrder;
        _isBattleOver = data.IsBattleOver;
        _resultText = data.ResultText;
        _items.Restore(data.IsPotionUsed, data.IsPotionActive);

        _timesPositionSeen.Clear();
        int seenCount = Mathf.Min(data.SeenPositions.Count, data.SeenPositionCounts.Count);
        for (int index = 0; index < seenCount; index++)
        {
            _timesPositionSeen[data.SeenPositions[index]] = data.SeenPositionCounts[index];
        }

        RebuildCells();
        DrawHand();
        ShowItems();
        ShowActivationOrder();
        ShowStats();
        ShowWhoseTurn();
        Debug.Log($"[Info] Continued the saved battle at turn {_turnNumber}.");
    }

    private static void RestoreHand(BattleHand hand, List<string> kindNames)
    {
        hand.Clear();
        foreach (string kindName in kindNames)
        {
            BattleCubeKind kind = BattleCubeKind.FindBySkinName(kindName);
            if (kind == null)
            {
                Debug.LogWarning($"[Warn] Saved battle: skipped unknown block '{kindName}' in a hand.");
                continue;
            }

            hand.Add(kind);
        }
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
    public void StartHover(BattleCubeKind paletteKind, BattleCell cell)
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
    public void EndHover(BattleCubeKind paletteKind, BattleCell cell)
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
        foreach (BattleCell cell in _cells)
        {
            cell.HideAreaTint();
        }

        BattleBlock block = _hoveredCell != null ? _board.GetBlock(_hoveredCell.GridPosition) : null;
        if (block == null)
        {
            return;
        }

        BattleCombat.CollectAreaOfEffect(_board, block, _attackSquares, _otherEffectSquares);
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
    public void TryPlacePlayerBlock(BattleCubeKind kind, Vector2Int position)
    {
        if (_isTurnRunning || _isBattleOver || _board.GetBlock(position) != null || !_playerHand.Remove(kind))
        {
            GameAudio.Play(GameSound.InvalidMove);
            return;
        }

        _board.Place(new BattleBlock(kind, BattleSide.Player), position);
        GameAudio.Play(GameSound.BlockPlace);
        DrawBoard();
        DrawHand();
        StartCoroutine(RunRestOfTurn());
    }

    /// <summary>
    /// Called when the potion in the item library is clicked. From now until the end
    /// of this turn the player's red blocks hit harder. It works once per battle.
    /// </summary>
    private void UsePotion()
    {
        if (_isTurnRunning || _isBattleOver || !_items.UsePotion())
        {
            GameAudio.Play(GameSound.InvalidMove);
            return;
        }

        GameAudio.Play(GameSound.Heal);
        _statusLabel.text = $"POTION USED: YOUR RED BLOCKS HIT +{BattleItems.PotionStrikeBonus} THIS TURN. NOW PLACE A BLOCK";
        ShowItems();
        DrawBoard();
        Autosave();
    }

    /// <summary>
    /// True if the player may drag the block on this square to another square right now.
    /// That is the gem's power: it works on the player's own blue blocks, any number of
    /// times, but only while the player is still deciding where to place a block.
    /// </summary>
    public bool CanMoveBlock(Vector2Int position)
    {
        return !_isTurnRunning && !_isBattleOver && BattleItems.CanBeMoved(_board.GetBlock(position));
    }

    /// <summary>Called when the player drops one of their placed blocks on another square.</summary>
    public void TryMoveBlock(Vector2Int from, Vector2Int to)
    {
        // Dropped back where it started: nothing to do, and not a mistake.
        if (from == to)
        {
            return;
        }

        if (!CanMoveBlock(from) || _board.GetBlock(to) != null)
        {
            GameAudio.Play(GameSound.InvalidMove);
            return;
        }

        _board.Move(_board.GetBlock(from), to);
        GameAudio.Play(GameSound.BlockPlace);
        DrawBoard();
        Autosave();
    }

    private void PassTurn()
    {
        if (!_isTurnRunning && !_isBattleOver)
        {
            StartCoroutine(RunRestOfTurn());
        }
    }

    private void ResetBattle()
    {
        if (!_isTurnRunning)
        {
            StartNewBattle();
        }
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
        Autosave();
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

        // With an empty hand the player has nothing to decide, so turns keep
        // playing by themselves until the battle is settled.
        do
        {
            if (_enemyHand.Count > 0)
            {
                yield return PlaceEnemyBlock();
            }

            yield return ActivateEverySquare();

            // The potion lasts for one turn only.
            _items.EndTurn();
            ShowItems();

            _turnNumber++;
            CheckForEndOfBattle();
            ShowStats();
            Autosave();
        }
        while (!_isBattleOver && _playerHand.Count == 0);

        ShowWhoseTurn();
        _isTurnRunning = false;
    }

    private IEnumerator PlaceEnemyBlock()
    {
        _statusLabel.text = "ENEMY IS CHOOSING...";
        yield return _enemyThinkWait;
        BattleBlock enemyBlock = _enemy.PlaceBlock(_board, _enemyHand, _isRandomOrder, _items.PlayerStrikeBonus);
        _statusLabel.text = enemyBlock != null ? $"ENEMY PLACES {enemyBlock.Kind.Name}" : "ENEMY HAS NO ROOM TO PLACE";
        if (enemyBlock != null)
        {
            GameAudio.Play(GameSound.EnemyPlace);
        }
        DrawBoard();
        ShowStats();
        yield return _enemyThinkWait;
    }

    /// <summary>
    /// Ends the battle if one side is out of blocks, or if the board has now
    /// looked exactly the same three times, which means nothing more will change.
    /// </summary>
    private void CheckForEndOfBattle()
    {
        BattleOutcome outcome = BattleCombat.GetOutcome(_board, _playerHand.Count, _enemyHand.Count);
        if (outcome == BattleOutcome.PlayerWins)
        {
            EndBattle("YOU WIN: THE ENEMY HAS NO BLOCKS LEFT", GameSound.Win);
            return;
        }

        if (outcome == BattleOutcome.EnemyWins)
        {
            EndBattle("ENEMY WINS: YOU HAVE NO BLOCKS LEFT", GameSound.Lose);
            return;
        }

        if (outcome == BattleOutcome.Draw)
        {
            EndBattle("DRAW: NOBODY HAS BLOCKS LEFT", GameSound.Draw);
            return;
        }

        string position = _board.DescribePosition(_positionText);
        _timesPositionSeen.TryGetValue(position, out int timesSeen);
        _timesPositionSeen[position] = timesSeen + 1;
        if (timesSeen + 1 >= RepeatsForDraw)
        {
            EndBattle($"DRAW: THE BOARD WAS THE SAME {RepeatsForDraw} TIMES", GameSound.Draw);
        }
        else if (_turnNumber > MostTurnsPerBattle)
        {
            EndBattle($"DRAW: {MostTurnsPerBattle} TURNS WITHOUT A WINNER", GameSound.Draw);
        }
    }

    private void EndBattle(string result, GameSound sound)
    {
        _isBattleOver = true;
        _resultText = result + ". PRESS RESET";
        GameAudio.Play(sound);
    }

    /// <summary>
    /// Visits every square exactly once, in the order chosen with the ORDER button,
    /// and lets each block it reaches use its ability. A block acts only once per
    /// turn from being visited, even if it gets pushed onto a square that has not
    /// been visited yet. Being triggered again by an orange block is extra and does
    /// not count as its visit.
    /// </summary>
    private IEnumerator ActivateEverySquare()
    {
        _blocksThatActed.Clear();
        _hitsThisTurn = 0;
        BattleActivation.BuildVisitOrder(_board, _random, _isRandomOrder, _visitOrder, _blockSquares);

        for (int step = 0; step < _visitOrder.Count; step++)
        {
            BattleCell cell = _cells[_visitOrder[step]];
            BattleBlock block = _board.GetBlock(cell.GridPosition);
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

    private void ActivateBlock(BattleBlock block)
    {
        // Remember where the block stands now: the slashes are drawn from here.
        Vector2Int attackerSquare = block.Position;
        _destroyedBlocks.Clear();
        bool didSomething = BattleCombat.Activate(_board, block, _destroyedBlocks, _hitSquares, _items.PlayerStrikeBonus);
        foreach (Vector2Int hitSquare in _hitSquares)
        {
            BattleSlashEffect.Play(_gridArea, PositionOfSquare(attackerSquare), PositionOfSquare(hitSquare), _cellSize);
        }

        PlayActivationSounds(block, didSomething);

        foreach (BattleBlock destroyed in _destroyedBlocks)
        {
            if (destroyed.Side == BattleSide.Enemy)
            {
                _enemyBlocksDestroyed++;
            }
            else
            {
                _playerBlocksDestroyed++;
            }
        }

        string owner = block.Side == BattleSide.Enemy ? "ENEMY" : "YOUR";
        _statusLabel.text = $"{owner} {block.Kind.Name} {block.Kind.ActionWord}";
        DrawBoard();
        ShowStats();
    }

    /// <summary>
    /// Plays the block's own sound if its ability did anything, a rising note for
    /// each hit (so a turn full of hits climbs in pitch), and a bang if a block died.
    /// A block whose ability reached nothing stays silent.
    /// </summary>
    private void PlayActivationSounds(BattleBlock block, bool didSomething)
    {
        if (didSomething)
        {
            GameAudio.Play(block.Kind.Sound);
        }

        if (_hitSquares.Count > 0)
        {
            _hitsThisTurn += _hitSquares.Count;
            GameAudio.PlayHitChain(_hitsThisTurn);
        }

        if (_destroyedBlocks.Count > 0)
        {
            GameAudio.Play(GameSound.BlockDestroyed);
        }
    }

    /// <summary>
    /// Squares are created row by row, so the square at (column, row)
    /// is always at this position in the list.
    /// </summary>
    private int IndexOf(Vector2Int position)
    {
        return BattleActivation.IndexOf(_board, position);
    }

    /// <summary>Shows the blocks left in the player's hand under the grid, ready to be dragged.</summary>
    private void DrawHand()
    {
        foreach (GameObject cube in _paletteCubes)
        {
            Destroy(cube);
        }
        _paletteCubes.Clear();

        for (int index = 0; index < _playerHand.Count; index++)
        {
            BattlePaletteCube cube = BattlePaletteCube.Create(_palette, _playerHand.GetBlock(index), this, _dragLayer);
            UnityEngine.UI.LayoutElement size = cube.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            size.preferredWidth = PaletteCubeSize;
            size.preferredHeight = PaletteCubeSize;
            _paletteCubes.Add(cube.gameObject);
        }
    }

    /// <summary>Throws the old squares away and creates new ones at the board's current size.</summary>
    private void RebuildCells()
    {
        // The square under the mouse is about to be destroyed.
        StopHover();
        foreach (BattleCell cell in _cells)
        {
            Destroy(cell.gameObject);
        }
        _cells.Clear();

        _cellSize = CalculateCellSize();
        for (int row = 0; row < _board.Rows; row++)
        {
            for (int column = 0; column < _board.Columns; column++)
            {
                CreateCell(new Vector2Int(column, row));
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

    private void CreateCell(Vector2Int gridPosition)
    {
        BattleCell cell = BattleCell.Create(_gridArea, gridPosition, CellColor, _font, this);
        _cells.Add(cell);

        RectTransform rect = (RectTransform)cell.transform;
        rect.sizeDelta = new Vector2(_cellSize - GapBetweenCells, _cellSize - GapBetweenCells);
        rect.anchoredPosition = PositionOfSquare(gridPosition);
    }

    /// <summary>
    /// Where the middle of a square is inside the grid area. Positions are measured
    /// from the middle of the area, so the grid is centered. Row 0 is the top row,
    /// which is why y is flipped.
    /// </summary>
    private Vector2 PositionOfSquare(Vector2Int gridPosition)
    {
        float x = (gridPosition.x - (_board.Columns - 1) / 2f) * _cellSize;
        float y = ((_board.Rows - 1) / 2f - gridPosition.y) * _cellSize;
        return new Vector2(x, y);
    }

    /// <summary>Makes every square on screen show what the board holds.</summary>
    private void DrawBoard()
    {
        foreach (BattleCell cell in _cells)
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

    /// <summary>Sets the status line: the result if the battle is over, otherwise what the player can do now.</summary>
    private void ShowWhoseTurn()
    {
        if (_isBattleOver)
        {
            _statusLabel.text = _resultText;
            return;
        }

        _board.CollectPositions(_blockSquares, true);
        _statusLabel.text = _blockSquares.Count > 0 ? YourTurnText : BoardFullText;
    }

    /// <summary>
    /// Puts an item's picture on its square in the library. If the PNG is missing the
    /// square keeps the plain color the scene gave it.
    /// </summary>
    private static void ShowItemPicture(UnityEngine.UI.Image image, string itemName)
    {
        Sprite picture = ItemIconLoader.Load(itemName);
        if (picture != null)
        {
            image.sprite = picture;
            image.preserveAspect = true;

            // An image's color is multiplied with its picture, so white shows the picture unchanged.
            image.color = Color.white;
        }
    }

    /// <summary>Makes the item library show whether the potion is ready, active, or used up.</summary>
    private void ShowItems()
    {
        // A button that cannot be pressed is drawn faded, which is how a used potion looks.
        _potionButton.interactable = !_items.IsPotionUsed;
        _potionStateLabel.text = _items.IsPotionActive ? PotionActiveText
            : _items.IsPotionUsed ? PotionUsedText : PotionReadyText;
    }

    private void ShowStats()
    {
        _statsLabel.text = $"TURN {_turnNumber}     HAND: YOU {_playerHand.Count} / ENEMY {_enemyHand.Count}     " +
            $"DESTROYED: ENEMY {_enemyBlocksDestroyed} / YOURS {_playerBlocksDestroyed}";
    }

    /// <summary>
    /// Shows the rules of whatever block the mouse is resting on, next to the mouse.
    /// The text is built from the block list itself, so it can never disagree
    /// with what the blocks really do. An empty square shows nothing.
    /// </summary>
    private void ShowTooltip()
    {
        BattleBlock block = _hoveredCell != null ? _board.GetBlock(_hoveredCell.GridPosition) : null;
        BattleCubeKind kind = block != null ? block.Kind : _hoveredPaletteKind;
        if (kind == null)
        {
            HideTooltip();
            return;
        }

        string owner = block == null ? string.Empty : block.Side == BattleSide.Enemy ? "ENEMY " : "YOUR ";
        string hp = block == null ? $"HP {kind.MaxHp}" : $"HP {block.Hp} / {kind.MaxHp}";
        string colorCode = ColorUtility.ToHtmlStringRGB(kind.Color);
        _tooltipText.text = $"<size={TooltipTitleFontSize}>{owner}<color=#{colorCode}>{kind.Name}</color></size>\n{hp}\n\n{kind.RuleText}"
            + DescribeItemEffects(block);

        bool wasHidden = !_tooltip.gameObject.activeSelf;
        _tooltip.gameObject.SetActive(true);
        if (wasHidden)
        {
            PlaceTooltipNextToPointer();
        }
    }

    /// <summary>Extra tooltip lines for a block that one of the player's items affects. Empty for any other block.</summary>
    private string DescribeItemEffects(BattleBlock block)
    {
        if (_items.IsBoosted(block))
        {
            return $"\n\nPOTION: +{BattleItems.PotionStrikeBonus} HP PER HIT THIS TURN";
        }

        return BattleItems.CanBeMoved(block) ? "\n\nGEM: DRAG IT TO AN EMPTY SQUARE BEFORE YOU PLACE" : string.Empty;
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
