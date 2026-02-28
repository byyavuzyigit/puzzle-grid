using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public int width = 6;
    public int height = 6;
    public float tileSize = 1f;
    public float refillSpawnOffset = 2f; // how high above the grid new tiles spawn when refilling

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
        GameObject obj = Instantiate(tilePrefab, new Vector3(x * tileSize, y * tileSize, 0), Quaternion.identity);
        obj.transform.SetParent(transform);

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

    // clear the group of tiles by destroying their game objects and setting their grid positions to null
    private void ClearGroup(List<Tile> group)
    {
        foreach (var t in group)
        {
            grid[t.x, t.y] = null;
            Destroy(t.gameObject);
        }
    }

    // collapse the grid by moving tiles down into empty spaces
    private void CollapseColumns()
    {
        // for each column, track the next avaiable lowest slot and compact non null tiles down into it
        for (int x = 0; x < width; x++)
        {
            int writeY = 0; // next empty spot from bottom

            for (int y = 0; y < height; y++)
            {
                var tile = grid[x, y];
                // if there's no tile, just skip it and keep looking for the next one to pull down
                if (tile == null) continue;

                if (y != writeY)
                {
                    // move tile down in array
                    grid[x, writeY] = tile;
                    grid[x, y] = null;

                    // update tile coords
                    tile.y = writeY;

                    // move tile transform (instant)
                    tile.transform.position = new Vector3(x * tileSize, writeY * tileSize, 0);
                }
                writeY++;
            }
        }
    }

    // refill the grid with new tiles at the top
    private void RefillGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null) continue;

                // spawn above and place into the hole (instant placement for now)
                var spawnPos = new Vector3(x * tileSize, (y + refillSpawnOffset) * tileSize, 0);
                GameObject obj = Instantiate(tilePrefab, spawnPos, Quaternion.identity);
                obj.transform.SetParent(transform);

                Tile tile = obj.GetComponent<Tile>();
                tile.x = x;
                tile.y = y;

                int randomType = Random.Range(0, colors.Length);
                tile.type = randomType;
                tile.SetColor(colors[randomType]);

                grid[x, y] = tile;

                // snap into place (instant)
                tile.transform.position = new Vector3(x * tileSize, y * tileSize, 0);
            }
        }
    }


    // when clicking a tile, find all neighboring tiles that have the same type and connected to it and highlight them by scaling them up a bit. if there are 2 or more, clear them and apply gravity to collapse the grid down and to the left then refill the grid with new tiles at the top.
    public void HandleTileClick(Tile tile)
    {
        if (tile == null) return;

        ResetAllScales();

        var group = GetConnectedGroup(tile);
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

        // first clear the group
        ClearGroup(group);

        // then collapse the grid down and to the left
        CollapseColumns();

        // finally refill the grid with new tiles
        RefillGrid();
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
