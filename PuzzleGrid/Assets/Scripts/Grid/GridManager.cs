using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public int width = 6;
    public int height = 6;
    public float tileSize = 1f;
    public float refillSpawnOffset = 2f; // how high above the grid new tiles spawn when refilling
    public float fallDuration = 0.30f;
    public float refillDuration = 0.35f;
    private bool isAnimating = false;

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

    // process the move by clearing the group, collapsing columns and refilling the grid. using a coroutine allows us to wait for animations to finish before proceeding to the next step.
    private System.Collections.IEnumerator ProcessMove(List<Tile> group)
    {
        isAnimating = true; // locks input until the move is fully processed

        GameManager.Instance.UseMove();
        GameManager.Instance.AddScore(group.Count * 10);

        ClearGroup(group); // clear tiles instantly - can add later animation here 

        // animate the collapse of columns - find all tiles that need to fall down and their target positions, then animate them together (yield return -> wait until animation is done)
        yield return StartCoroutine(CollapseColumnsAnimated(fallDuration));

        // refill the grid with new tiles and animate them falling in
        yield return StartCoroutine(RefillGridAnimated(refillDuration));

        ResetAllScales();
        isAnimating = false;
    }

    // instead of collapsing one tile at a time, collect all the tiles that need to move and their start/end positions then animate them together for a smoother effect.
    private System.Collections.IEnumerator CollapseColumnsAnimated(float duration)
    {
        // collect moves (tile -> target position)
        var moves = new List<(Tile tile, Vector3 start, Vector3 end)>();

        for (int x = 0; x < width; x++)
        {
            int writeY = 0;

            for (int y = 0; y < height; y++)
            {
                var tile = grid[x, y];
                if (tile == null) continue;

                if (y != writeY)
                {
                    grid[x, writeY] = tile;
                    grid[x, y] = null;

                    tile.y = writeY;

                    Vector3 start = tile.transform.position;
                    Vector3 end = new Vector3(x * tileSize, writeY * tileSize, 0);
                    // collect moves to animate together later
                    moves.Add((tile, start, end));
                }
                writeY++;
            }
        }

        // animate all moves together
        if (moves.Count > 0)
            yield return StartCoroutine(AnimateMoves(moves, duration));
    }

    // when refilling, spawn new tiles above the grid and animate them falling into place. collect all new tiles and their target positions to animate together for a smoother effect.
    private System.Collections.IEnumerator RefillGridAnimated(float duration)
    {
        var moves = new List<(Tile tile, Vector3 start, Vector3 end)>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null) continue;

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

                Vector3 endPos = new Vector3(x * tileSize, y * tileSize, 0);
                // same logic - collect all moves to animate together later
                moves.Add((tile, spawnPos, endPos));
            }
        }

        if (moves.Count > 0)
            yield return StartCoroutine(AnimateMoves(moves, duration));
    }

    // shared animation engine for moving tiles from start to end positions over a duration with easing. by using a single coroutine to animate all tile movements together, we get smoother animations and can easily wait for all animations to finish before proceeding to the next step in the move processing.
    private System.Collections.IEnumerator AnimateMoves(List<(Tile tile, Vector3 start, Vector3 end)> moves, float duration)
    {
        float t = 0f;

        // ensure all start positions are applied
        foreach (var m in moves)
            if (m.tile != null)
                m.tile.transform.position = m.start;

        while (t < duration)
        {
            float a = t / duration;
            // smoothstep for nicer easing - easing function
            a = a * a * (3f - 2f * a);

            foreach (var m in moves)
            {
                if (m.tile == null) continue;
                m.tile.transform.position = Vector3.Lerp(m.start, m.end, a);
            }

            t += Time.deltaTime;
            yield return null; // wait for next frame
        }

        // snap to exact end
        foreach (var m in moves)
            if (m.tile != null)
                m.tile.transform.position = m.end;
    }


    // when clicking a tile, find all neighboring tiles that have the same type and connected to it and highlight them by scaling them up a bit. if there are 2 or more, clear them and apply gravity to collapse the grid down and to the left then refill the grid with new tiles at the top.
    public void HandleTileClick(Tile tile)
    {
        if (tile == null) return;

        // to ensure one move completes before another one starts
        if (isAnimating) return;

        ResetAllScales();
        var group = GetConnectedGroup(tile);

        foreach (var t in group)
            t.transform.localScale = Vector3.one * 1.15f;

        if (group.Count < 2) return;

        // coroutine lets us pause logic across multiple frames to allow animations to play out while keeping the main thread responsive. we can yield until animations are done before proceeding to the next step of collapsing and refilling the grid.
        StartCoroutine(ProcessMove(group));
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
