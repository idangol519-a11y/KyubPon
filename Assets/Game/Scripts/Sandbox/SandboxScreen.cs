using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// A test page with a grid whose size can be changed and a palette of cubes
/// that can be dragged into it. See docs/GameState.md for how to remove it.
/// </summary>
public class SandboxScreen : MonoBehaviour
{
    private const string HomeScreenSceneName = "HomeScreen";
    private const int StartColumns = SandboxGridSize.MinimumColumns;
    private const int StartRows = SandboxGridSize.MinimumRows;
    private const float GapBetweenCells = 8f;
    private const float PaletteCubeSize = 96f;

    private static readonly Color CellColor = new Color(0.17f, 0.17f, 0.24f);
    private static readonly Color[] CubeColors =
    {
        new Color(0.90f, 0.30f, 0.25f),
        new Color(0.25f, 0.60f, 0.95f),
        new Color(0.35f, 0.80f, 0.40f),
        new Color(1.00f, 0.82f, 0.20f)
    };

    [SerializeField] private RectTransform _gridArea;
    [SerializeField] private RectTransform _dragLayer;
    [SerializeField] private Transform _palette;
    [SerializeField] private UnityEngine.UI.Text _columnsLabel;
    [SerializeField] private UnityEngine.UI.Text _rowsLabel;
    [SerializeField] private UnityEngine.UI.Button _fewerColumnsButton;
    [SerializeField] private UnityEngine.UI.Button _moreColumnsButton;
    [SerializeField] private UnityEngine.UI.Button _fewerRowsButton;
    [SerializeField] private UnityEngine.UI.Button _moreRowsButton;
    [SerializeField] private UnityEngine.UI.Button _backButton;

    private readonly List<SandboxCell> _cells = new List<SandboxCell>();
    private readonly Dictionary<Vector2Int, Color> _rememberedCubes = new Dictionary<Vector2Int, Color>();
    private SandboxGridSize _gridSize;

    private void Awake()
    {
        _gridSize = new SandboxGridSize(StartColumns, StartRows);

        _fewerColumnsButton.onClick.AddListener(() => ChangeColumns(-1));
        _moreColumnsButton.onClick.AddListener(() => ChangeColumns(1));
        _fewerRowsButton.onClick.AddListener(() => ChangeRows(-1));
        _moreRowsButton.onClick.AddListener(() => ChangeRows(1));
        _backButton.onClick.AddListener(ReturnToHomeScreen);
    }

    private void Start()
    {
        BuildPalette();
        RebuildGrid();
    }

    private void ChangeColumns(int amount)
    {
        _gridSize.ChangeColumns(amount);
        RebuildGrid();
    }

    private void ChangeRows(int amount)
    {
        _gridSize.ChangeRows(amount);
        RebuildGrid();
    }

    private void ReturnToHomeScreen()
    {
        SceneManager.LoadScene(HomeScreenSceneName);
    }

    /// <summary>Creates one cube of each color for the player to drag copies from.</summary>
    private void BuildPalette()
    {
        foreach (Color color in CubeColors)
        {
            SandboxCube cube = SandboxCube.Create(_palette, color, _dragLayer, true);
            UnityEngine.UI.LayoutElement size = cube.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            size.preferredWidth = PaletteCubeSize;
            size.preferredHeight = PaletteCubeSize;
        }
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
                _rememberedCubes[cell.GridPosition] = cell.Occupant.Color;
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

        if (_rememberedCubes.TryGetValue(gridPosition, out Color color))
        {
            cell.Place(SandboxCube.Create(cell.transform, color, _dragLayer, false));
        }
    }
}
