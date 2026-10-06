using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// A test page with a grid whose size can be changed, a palette of cubes that can
/// be dragged into it, and a scoring run that visits every square.
/// See docs/GameState.md for how to remove it.
/// </summary>
public class SandboxScreen : MonoBehaviour
{
    private const string HomeScreenSceneName = "HomeScreen";
    private const int StartColumns = SandboxGridSize.MinimumColumns;
    private const int StartRows = SandboxGridSize.MinimumRows;
    private const float GapBetweenCells = 8f;
    private const float PaletteCubeSize = 96f;

    // How long scoring stays on a square. Squares with a cube get longer so the change can be read.
    private const float EmptySquareSeconds = 0.05f;
    private const float CubeSquareSeconds = 0.45f;

    // Scores at or above this are shown in short scientific form, for example 1.23e12.
    private const double LargeScore = 1e9;

    private static readonly Color CellColor = new Color(0.17f, 0.17f, 0.24f);
    private static readonly Color HighlightColor = new Color(0.55f, 0.55f, 0.70f);
    private static readonly Vector2Int[] NeighbourDirections =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    [SerializeField] private RectTransform _gridArea;
    [SerializeField] private RectTransform _dragLayer;
    [SerializeField] private Transform _palette;
    [SerializeField] private UnityEngine.UI.Text _columnsLabel;
    [SerializeField] private UnityEngine.UI.Text _rowsLabel;
    [SerializeField] private UnityEngine.UI.Text _scoreLabel;
    [SerializeField] private UnityEngine.UI.Text _rulesLabel;
    [SerializeField] private UnityEngine.UI.Button _fewerColumnsButton;
    [SerializeField] private UnityEngine.UI.Button _moreColumnsButton;
    [SerializeField] private UnityEngine.UI.Button _fewerRowsButton;
    [SerializeField] private UnityEngine.UI.Button _moreRowsButton;
    [SerializeField] private UnityEngine.UI.Button _scoreButton;
    [SerializeField] private UnityEngine.UI.Button _backButton;

    private readonly List<SandboxCell> _cells = new List<SandboxCell>();
    private readonly List<int> _cubeCellIndexes = new List<int>();
    private readonly Dictionary<Vector2Int, SandboxCubeKind> _rememberedCubes = new Dictionary<Vector2Int, SandboxCubeKind>();
    private readonly System.Random _random = new System.Random();
    private readonly WaitForSeconds _emptySquareWait = new WaitForSeconds(EmptySquareSeconds);
    private readonly WaitForSeconds _cubeSquareWait = new WaitForSeconds(CubeSquareSeconds);
    private SandboxGridSize _gridSize;
    private bool _isScoring;

    private void Awake()
    {
        _gridSize = new SandboxGridSize(StartColumns, StartRows);

        _fewerColumnsButton.onClick.AddListener(() => ChangeColumns(-1));
        _moreColumnsButton.onClick.AddListener(() => ChangeColumns(1));
        _fewerRowsButton.onClick.AddListener(() => ChangeRows(-1));
        _moreRowsButton.onClick.AddListener(() => ChangeRows(1));
        _scoreButton.onClick.AddListener(StartScoring);
        _backButton.onClick.AddListener(ReturnToHomeScreen);
    }

    private void Start()
    {
        BuildPalette();
        RebuildGrid();
        ShowRules();
        _scoreLabel.text = "SCORE: 0";
    }

    private void ChangeColumns(int amount)
    {
        // The grid cannot be rebuilt while scoring is walking through its squares.
        if (_isScoring)
        {
            return;
        }

        _gridSize.ChangeColumns(amount);
        RebuildGrid();
    }

    private void ChangeRows(int amount)
    {
        if (_isScoring)
        {
            return;
        }

        _gridSize.ChangeRows(amount);
        RebuildGrid();
    }

    private void ReturnToHomeScreen()
    {
        SceneManager.LoadScene(HomeScreenSceneName);
    }

    /// <summary>Creates one cube of each kind for the player to drag copies from.</summary>
    private void BuildPalette()
    {
        foreach (SandboxCubeKind kind in SandboxCubeKind.All)
        {
            SandboxCube cube = SandboxCube.Create(_palette, kind, _dragLayer, true);
            UnityEngine.UI.LayoutElement size = cube.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            size.preferredWidth = PaletteCubeSize;
            size.preferredHeight = PaletteCubeSize;
        }
    }

    /// <summary>
    /// Writes the side note from the cube list itself, so the note can never
    /// disagree with what the cubes really do.
    /// </summary>
    private void ShowRules()
    {
        StringBuilder rules = new StringBuilder("SCORING RULES\n\n");
        foreach (SandboxCubeKind kind in SandboxCubeKind.All)
        {
            string colorCode = ColorUtility.ToHtmlStringRGB(kind.Color);
            rules.Append($"<color=#{colorCode}>{kind.Name}</color>\n{kind.RuleText}\n\n");
        }

        rules.Append("SCORING STARTS AT A RANDOM CUBE, THEN VISITS EVERY SQUARE IN ORDER.");
        _rulesLabel.text = rules.ToString();
    }

    /// <summary>
    /// Throws the old grid away and builds a new one at the current size.
    /// Cubes that still fit in the new grid are put back where they were.
    /// </summary>
    private void RebuildGrid()
    {
        RememberCubes();
        foreach (SandboxCell cell in _cells)
        {
            Destroy(cell.gameObject);
        }
        _cells.Clear();

        // Cells are created row by row, so the cell at (column, row)
        // is always at position row * Columns + column in the list.
        float cellSize = CalculateCellSize();
        for (int row = 0; row < _gridSize.Rows; row++)
        {
            for (int column = 0; column < _gridSize.Columns; column++)
            {
                CreateCell(new Vector2Int(column, row), cellSize);
            }
        }

        _columnsLabel.text = _gridSize.Columns.ToString();
        _rowsLabel.text = _gridSize.Rows.ToString();
    }

    private void RememberCubes()
    {
        _rememberedCubes.Clear();
        foreach (SandboxCell cell in _cells)
        {
            if (cell.Occupant != null)
            {
                _rememberedCubes[cell.GridPosition] = cell.Occupant.Kind;
            }
        }
    }

    /// <summary>The largest square cell that lets the whole grid fit in its area.</summary>
    private float CalculateCellSize()
    {
        Vector2 area = _gridArea.rect.size;
        return Mathf.Floor(Mathf.Min(area.x / _gridSize.Columns, area.y / _gridSize.Rows));
    }

    private void CreateCell(Vector2Int gridPosition, float cellSize)
    {
        SandboxCell cell = SandboxCell.Create(_gridArea, gridPosition, CellColor);
        _cells.Add(cell);

        // Positions are measured from the middle of the grid area, so the grid is centered.
        // Row 0 is the top row, which is why y is flipped.
        RectTransform rect = (RectTransform)cell.transform;
        float x = (gridPosition.x - (_gridSize.Columns - 1) / 2f) * cellSize;
        float y = ((_gridSize.Rows - 1) / 2f - gridPosition.y) * cellSize;
        rect.sizeDelta = new Vector2(cellSize - GapBetweenCells, cellSize - GapBetweenCells);
        rect.anchoredPosition = new Vector2(x, y);

        if (_rememberedCubes.TryGetValue(gridPosition, out SandboxCubeKind kind))
        {
            cell.Place(SandboxCube.Create(cell.transform, kind, _dragLayer, false));
        }
    }

    private void StartScoring()
    {
        if (!_isScoring)
        {
            StartCoroutine(RunScoring());
        }
    }

    /// <summary>
    /// One scoring run. It starts at a random cube, then visits every square of the
    /// grid once, in reading order, wrapping from the last square back to the first.
    /// Each cube it reaches changes the score using its own rule.
    /// A coroutine lets the run pause on each square so the player can watch it.
    /// </summary>
    private IEnumerator RunScoring()
    {
        _isScoring = true;
        int startIndex = PickStartIndex();
        int cubesOnGrid = _cubeCellIndexes.Count;
        double score = 0;
        _scoreLabel.text = "SCORE: 0";

        for (int step = 0; step < _cells.Count; step++)
        {
            SandboxCell cell = _cells[(startIndex + step) % _cells.Count];
            cell.SetHighlight(true, HighlightColor);

            // Read the cube now: the player may drag it away during the pause below.
            SandboxCube cube = cell.Occupant;
            if (cube != null)
            {
                double scoreBefore = score;
                score = SandboxScoring.Apply(cube.Kind.Rule, score, CountNeighbourCubes(cell), cubesOnGrid);
                _scoreLabel.text = $"SCORE: {FormatScore(score)}  ({cube.Kind.Name} +{FormatScore(score - scoreBefore)})";
                yield return _cubeSquareWait;
            }
            else
            {
                yield return _emptySquareWait;
            }

            cell.SetHighlight(false, HighlightColor);
        }

        _scoreLabel.text = $"FINAL SCORE: {FormatScore(score)}";
        _isScoring = false;
    }

    /// <summary>
    /// Lists the squares that hold a cube and picks one at random to start from.
    /// With no cubes at all, any square can be the start.
    /// </summary>
    private int PickStartIndex()
    {
        _cubeCellIndexes.Clear();
        for (int index = 0; index < _cells.Count; index++)
        {
            if (_cells[index].Occupant != null)
            {
                _cubeCellIndexes.Add(index);
            }
        }

        if (_cubeCellIndexes.Count == 0)
        {
            return _random.Next(_cells.Count);
        }

        return _cubeCellIndexes[_random.Next(_cubeCellIndexes.Count)];
    }

    /// <summary>Counts the cubes directly above, below, left, and right of a cell.</summary>
    private int CountNeighbourCubes(SandboxCell cell)
    {
        int count = 0;
        foreach (Vector2Int direction in NeighbourDirections)
        {
            Vector2Int position = cell.GridPosition + direction;
            bool isInsideGrid = position.x >= 0 && position.x < _gridSize.Columns
                && position.y >= 0 && position.y < _gridSize.Rows;
            if (isInsideGrid && _cells[position.y * _gridSize.Columns + position.x].Occupant != null)
            {
                count++;
            }
        }

        return count;
    }

    private static string FormatScore(double score)
    {
        string format = score < LargeScore ? "N0" : "0.00e0";
        return score.ToString(format, CultureInfo.InvariantCulture);
    }
}
