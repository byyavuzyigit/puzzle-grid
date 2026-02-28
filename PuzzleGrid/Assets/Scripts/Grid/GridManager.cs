using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public int width = 6;
    public int height = 6;
    public float tileSize = 1f;

    public GameObject tilePrefab;

    private Tile[,] grid;

    private Color[] colors =
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta
    };

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        grid = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SpawnTile(x, y);
            }
        }
    }

    void SpawnTile(int x, int y)
    {
        GameObject obj = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
        obj.transform.parent = transform;

        // OnMouseDown requires a physics collider on the clicked object.
        if (obj.GetComponent<Collider2D>() == null && obj.GetComponent<Collider>() == null)
        {
            obj.AddComponent<BoxCollider2D>();
        }

        Tile tile = obj.GetComponent<Tile>();
        tile.x = x;
        tile.y = y;

        int randomType = Random.Range(0, colors.Length);
        tile.type = randomType;
        tile.SetColor(colors[randomType]);

        grid[x, y] = tile;
    }

    // flood fill to find connected tiles of the same type - flood fill using bfs
    private List<Tile> GetConnectedGroup(Tile start)
    {
    var result = new List<Tile>();
    var visited = new bool[width, height];
    var queue = new Queue<Tile>();

    // put the clicked tile in the queue to start
    queue.Enqueue(start);
    visited[start.x, start.y] = true;

    int targetType = start.type;

    while (queue.Count > 0)
    {
        Tile current = queue.Dequeue();
        result.Add(current);

        // 4 direction neighbors
        TryEnqueueNeighbor(current.x + 1, current.y);
        TryEnqueueNeighbor(current.x - 1, current.y);
        TryEnqueueNeighbor(current.x, current.y + 1);
        TryEnqueueNeighbor(current.x, current.y - 1);
    }

    return result;

    void TryEnqueueNeighbor(int nx, int ny)
    {
        if (nx < 0 || nx >= width || ny < 0 || ny >= height) return;
        if (visited[nx, ny]) return;

        Tile neighbor = grid[nx, ny];
        if (neighbor == null) return;
        if (neighbor.type != targetType) return;

        // valid neighbor - mark visited and enqueue
        visited[nx, ny] = true;
        queue.Enqueue(neighbor);
    }

    }

    // when clicking a tile, find all neighboring tiles that have the same type and connected to it
    public void HandleTileClick(Tile tile)
    {
    if (tile == null) return;

    var group = GetConnectedGroup(tile);

    Debug.Log($"Group size: {group.Count} (type {tile.type})");

    // temporary visual feedback for the group - will replace with proper animation later.
    // we scale the group tiles up slightly then back down on next click.
    ResetAllScales();
    foreach (var t in group)
        t.transform.localScale = Vector3.one * 1.15f;

    // valid move rule - will update later
    if (group.Count < 2)
    {
        Debug.Log("Not enough tiles to clear (need at least 2).");
        return;
    }

    // for now just use a move and score a bit to test:
    GameManager.Instance.UseMove();
    GameManager.Instance.AddScore(group.Count * 10);
    }

    private void ResetAllScales()
    {
        if (grid == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                    grid[x, y].transform.localScale = Vector3.one;
            }
        }
    }
}
