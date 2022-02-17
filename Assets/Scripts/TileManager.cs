using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public enum ActionState
{
    PlacingWalls,
    PlacingStartPoint,
    EvaluatingPaths
}

[RequireComponent(typeof(Tilemap))]
public class TileManager : MonoBehaviour
{
    [Tooltip("Tile Object that represents an empty tile.")]
    public TileBase EmptyTile;
    [Tooltip("Tile Object that represents a wall tile.")]
    public TileBase WallTile;
    [Tooltip("Tile Object that represents the starting tile and selected tiles.")]
    public TileBase StartTile;
    [Tooltip("Tile Object that represents all tiles that are accessible after generating paths.")]
    public TileBase AccessibleTile;
    [Tooltip("Tile Object that represents a tile on the path between the starting tile, and the selected tile.")]
    public TileBase PathTile;

    [Tooltip("Container gameobject to hold the generated grid labels.")]
    public Transform LabelContainer;
    [Tooltip("Prefab for a gameobject that will be instantiated that represents a label.")]
    public GameObject LabelPrefab;
    [Tooltip("Text object that will be used to display either relevant instructions, or the current path information.")]
    public Text infoText;

    // Dictionary containing the distances from the starting node to every other accesssible node after generating paths.
    // Uses the tile position as a key.
    private Dictionary<Vector3Int, int> _distanceMap = new Dictionary<Vector3Int, int>();
    // Dictionary containing the distances from the starting node to every other accesssible node after generating paths.
    // Uses the tile position as a key.
    private Dictionary<Vector3Int, Vector3Int?> _previousNodeMap = new Dictionary<Vector3Int, Vector3Int?>();

    // Position of the currently selected starting tile. If no starting tile is selected, it will be null.
    private Vector3Int? _startingTile = null;
    // Position of the currently selected target tile after generating paths. If no tile is selected, it will be null.
    private Vector3Int? _selectedTile = null;

    // Tilemap object that holds the tiles used in this program.
    private Tilemap _tilemap;

    // This property controls what the user is currently able to do. i.e. placing walls, setting the start tile, at looking at paths.
    private ActionState _currentActionState;

    // Size that reprents both the width and height of the grid.
    private int _gridSize = 10;
    // Cost that reprents how many steps can be made in a single turn.
    private int _movementCost = 6;


    /// <summary>
    /// This function sets up the initial state of the grid. It sets up the tilemap with empty tiles for all needed cells. It also sets up the grid labels that show
    /// on the bottom and left side of the grid, and it sets the initial Action state to be drawing walls on the map.
    /// </summary>
    public void Init()
    {
        _tilemap = GetComponent<Tilemap>();

        for (int i = 0; i < _gridSize; i++)
        {
            for (int j = 0; j < _gridSize; j++)
            {
                _tilemap.SetTile(new Vector3Int(i, j, 0), EmptyTile);
            }
        }

        SetupGridLabels();

        // We need to resize and position the grid based on the size of the grid. This takes care of that.
        float unitSize = 10.0f / (_gridSize + 1);
        transform.localScale = new Vector3(unitSize, unitSize);
        transform.position = new Vector3(unitSize, unitSize);

        SetActionState(0);
    }

    /// <summary>
    /// This function will generate headers for each row and column. This is used so the user can better keep track of the current coordinates of any tile.
    /// If LabelContainer or LabelPrefab is not set, nothing will happen. 
    /// </summary>
    private void SetupGridLabels()
    {
        if (LabelContainer != null && LabelPrefab != null)
        {
            var size = Screen.width * 0.56f / (_gridSize + 1);
            for (int i = 0; i < _gridSize; i++)
            {
                var label = GameObject.Instantiate(LabelPrefab, LabelContainer);
                label.GetComponent<Text>().text = i.ToString();
                label.transform.position = new Vector3(size * 1.5f + i * size, size / 2, 0);
                label.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size) * 0.75f;
            }

            for (int i = 0; i < _gridSize; i++)
            {
                var label = GameObject.Instantiate(LabelPrefab, LabelContainer);
                label.GetComponent<Text>().text = i.ToString();
                label.transform.position = new Vector3(size / 2, size * 1.5f + i * size, 0);
                label.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size) * 0.75f;
            }
        }
    }

    /// <summary>
    /// Sets the current state. This will determine what will happen when the user clicks on the grid.
    /// Also updates the Info Text string to give the user instructions related to their new state.
    /// </summary>
    /// <param name="state"></param>
    public void SetActionState(int state)
    {
        _currentActionState = (ActionState)state;
        switch (_currentActionState)
        {
            case ActionState.PlacingWalls:
                SetInfoText("Hold the Left Mouse Button and drag your mouse on the grid to draw walls your map. Hold the Right Mouse Button to erase.");
                break;
            case ActionState.PlacingStartPoint:
                SetInfoText("Click on the grid to place your starting position. Hold the Right Mouse Button to erase.");
                break;
            case ActionState.EvaluatingPaths:
                if (_startingTile != null)
                {
                    SetInfoText("Click on a blue tile to see the generated path and path information.");
                }
                else
                {
                    _currentActionState = ActionState.PlacingStartPoint;
                    SetInfoText("Please select a starting point before generating paths.");
                }
                break;
            default:
                SetInfoText("");
                break;
        }
    }

    /// <summary>
    /// Sets the info text string if info text object is not null.
    /// </summary>
    /// <param name="newText"></param>
    public void SetInfoText(string newText)
    {
        if(infoText != null)
        {
            infoText.text = newText;
        }
    }

    /// <summary>
    /// Sets the current grid size. This is used for both the width and height of the grid.
    /// Value will be clamped between 2 and 50.
    /// If value passed is not a number, it will be set to the default of 10
    /// </summary>
    /// <param name="value"></param>
    public void SetGridSize(string value)
    {
        int val;
        if (int.TryParse(value, out val))
        {
            _gridSize = Mathf.Clamp(val, 2, 50);
        }
        else
        {
            _gridSize = 10;
        }
    }

    /// <summary>
    /// Sets the max movement cost.
    /// Value will be clamped between 1 and 500.
    /// If value passed is not a number, it will be set to the default of 6
    /// </summary>
    /// <param name="value"></param>
    public void SetMovementCost(string value)
    {
        int val;
        if (int.TryParse(value, out val))
        {
            _movementCost = Mathf.Clamp(val, 1, 500);
        }
        else
        {
            _movementCost = 6;
        }
    }

    /// <summary>
    /// Restarts the current level.
    /// </summary>
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Update()
    {
        Vector3Int tilemapPos = _tilemap.WorldToCell(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        tilemapPos = new Vector3Int(tilemapPos.x, tilemapPos.y, 0);

        switch (_currentActionState)
        {
            case ActionState.PlacingWalls:
                if (Input.GetMouseButton(0))
                {
                    PlaceTile(tilemapPos, WallTile);
                }
                else if (Input.GetMouseButton(1))
                {
                    PlaceTile(tilemapPos, EmptyTile);
                }
                break;
            case ActionState.PlacingStartPoint:
                if (Input.GetMouseButton(0))
                {
                    PlaceStartTile(tilemapPos, StartTile);
                }
                else if (Input.GetMouseButton(1))
                {
                    PlaceTile(tilemapPos, EmptyTile);
                }
                break;
            case ActionState.EvaluatingPaths:
                if (Input.GetMouseButton(0))
                {
                    UpdateCurrentPath(tilemapPos);
                }
                break;
            default:
                break;
        }
    }

    
    /// <summary>
    /// Place any type of tile. If we are placing a tile on top of the starting point, reset the starting point.
    /// </summary>
    /// <param name="tilemapPos"></param>
    /// <param name="tile"></param>
    private void PlaceTile(Vector3Int tilemapPos, TileBase tile)
    {
        if (_tilemap.GetTile(tilemapPos) != null)
        {
            if (_startingTile == tilemapPos)
            {
                _startingTile = null;
            }
            _tilemap.SetTile(tilemapPos, tile);
        }
    }

    /// <summary>
    /// Place The starting point tile. Reset old starting point to an empty tile.
    /// </summary>
    /// <param name="tilemapPos"></param>
    /// <param name="tile"></param>
    private void PlaceStartTile(Vector3Int tilemapPos, TileBase tile)
    {
        if (_tilemap.GetTile(tilemapPos) == EmptyTile)
        {
            if (_startingTile != null)
            {
                _tilemap.SetTile(_startingTile.Value, EmptyTile);
            }

            _startingTile = tilemapPos;
            _tilemap.SetTile(tilemapPos, tile);
        }
    }


    private void UpdateCurrentPath(Vector3Int tilemapPos)
    {
        if (_previousNodeMap.ContainsKey(tilemapPos) && _selectedTile != tilemapPos)
        {
            if (_selectedTile != null)
            {
                while (_previousNodeMap[_selectedTile.Value] != null)
                {
                    _tilemap.SetTile(_selectedTile.Value, AccessibleTile);
                    _selectedTile = _previousNodeMap[_selectedTile.Value].Value;
                }
            }

            string infotext = "";
            _selectedTile = tilemapPos;

            _tilemap.SetTile(tilemapPos, StartTile);
            while (_previousNodeMap[tilemapPos] != null)
            {
                infotext = string.Format("({0}, {1})\n{2}", tilemapPos.x, tilemapPos.y, infotext);
                tilemapPos = _previousNodeMap[tilemapPos].Value;
                if (tilemapPos != _startingTile)
                {
                    _tilemap.SetTile(tilemapPos, PathTile);
                }
            }
            infotext = string.Format("({0}, {1})\n{2}", tilemapPos.x, tilemapPos.y, infotext);
            infotext = string.Format("Cost from ({0}, {1}) to ({2}, {3}): {4}\nPath:\n{5}", _startingTile?.x, _startingTile?.y, _selectedTile?.x, _selectedTile?.y, _distanceMap[_selectedTile.Value], infotext);
            SetInfoText(infotext);
        }
    }

    /// <summary>
    /// Generates the shortest distance and shortest path from the starting tile to every other accessible tile within the boundaries of our cost value.
    /// Uses a modified version of Dijkstra's algorithm built specifically to work with Unity's tilemap system.
    /// It follows the same steps that Dijkstra's normally does, but it's optimized to ignore some values that will always be out of range based on our cost.
    /// </summary>
    public void GeneratePaths()
    {
        if(_startingTile != null)
        {
            _distanceMap.Clear();
            _previousNodeMap.Clear();
            List<Vector3Int> queue = new List<Vector3Int>();

            // Setup collections of all non-wall nodes that are within range of our starting position
            for (int i = 0; i < _gridSize; i++)
            {
                for (int j = 0; j < _gridSize; j++)
                {
                    Vector3Int position = new Vector3Int(i, j, 0);
                    if(_tilemap.GetTile(position) != null && _tilemap.GetTile(position) != WallTile)
                    {
                        if (Mathf.Abs(_startingTile.Value.x - i) + Mathf.Abs(_startingTile.Value.y - j) <= _movementCost)
                        {
                            _distanceMap[new Vector3Int(i, j, 0)] = int.MaxValue;
                            _previousNodeMap[new Vector3Int(i, j, 0)] = null;
                            queue.Add(new Vector3Int(i, j, 0));
                        }
                    }
                }
            }

            _distanceMap[_startingTile.Value] = 0;

            // Setup offsets to make it easier to work with neighboring nodes.
            var neighborOffsets = new List<Vector3Int>
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, -1, 0)
            };

            // Break when we get to the point where there are no more tiles without max values.
            // Normally, you would break when the queue is empty, but since we can have cells that are completely blocked off by walls, we need to break when there are
            // no more accessible tiles instead.
            while(queue.Any(q => _distanceMap[q] != int.MaxValue))
            {
                var node = queue.OrderBy(x => _distanceMap[x]).First();
                queue.Remove(node);

                // Look through each of the neighbors and see if the path to the neighbor is less than the currently known path for that neighbor.
                // If it is, update it.
                foreach(Vector3Int offset in neighborOffsets)
                {
                    Vector3Int neighbor = node + offset;
                    if (queue.Contains(neighbor))
                    {
                        var totaldist = _distanceMap[node] + 1;
                        if (totaldist < _distanceMap[neighbor])
                        {
                            _distanceMap[neighbor] = totaldist;
                            _previousNodeMap[neighbor] = node;
                        }
                    }
                }
            }

            // Filter lists down to only items that are within range of our movement cost.
            _previousNodeMap = _previousNodeMap.Where(kv => _distanceMap[kv.Key] <= _movementCost).ToDictionary(kv => kv.Key, kv => kv.Value);
            _distanceMap = _distanceMap.Where(kv => kv.Value <= _movementCost).ToDictionary(kv => kv.Key, kv => kv.Value);

            // Mark all tiles within range as Accessible. This lets the user know which tiles are able to be traversed to.
            foreach(var key in _distanceMap.Keys)
            {
                if (key != _startingTile)
                {
                    _tilemap.SetTile(key, AccessibleTile);
                }
            }
        }
    }

    /// <summary>
    /// Resets all accessible tiles and path tiles back to default and clears path lists. Needed because we don't want to show path tiles while we are updating the map.
    /// </summary>
    public void ResetPaths()
    {
        foreach (var key in _distanceMap.Keys)
        {
            if (key != _startingTile)
            {
                _tilemap.SetTile(key, EmptyTile);
            }
        }
        _selectedTile = null;
        _distanceMap.Clear();
        _previousNodeMap.Clear();
    }
}
