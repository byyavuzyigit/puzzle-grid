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
    public Transform uiAnchorTopLeft;
    public Transform uiAnchorTopRight;
    public float uiAnchorPadding = 0.4f; // world units above the grid

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

    public void RecreateGrid()
    {
        StopAllCoroutines();
        isAnimating = false;
        ClearExistingTiles();
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
        UpdateUIAnchors();
    }

    private void ClearExistingTiles()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }

        grid = null;
    }

    void SpawnTile(int x, int y)
    {
        GameObject obj = Instantiate(tilePrefab, GetWorldPosition(x, y), Quaternion.identity);
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

    // clear the group of tiles by destroying their game objects and setting their grid positions to null. animate the clearing by scaling down and fading out the tiles over a short duration before destroying them, to give visual feedback to the player.
    private System.Collections.IEnumerator ClearGroupAnimated(List<Tile> group, float duration)
    {
        float t = 0f;

        var sprites = new List<SpriteRenderer>();

        foreach (var tile in group)
        {
            // remove each tile from the grid immediately so they won't interfere with collapse/refill logic, but keep their game objects around for animation until the end of the duration.
            grid[tile.x, tile.y] = null;
            sprites.Add(tile.GetComponent<SpriteRenderer>());
        }

        while (t < duration)
        {
            float a = t / duration;

            foreach (var tile in group)
            {
                if (tile == null) continue;

                // scale down - tiles shrink down smoothly
                tile.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, a);

                // fade out
                var sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // only fade transparancy, keep color hue intact - fade out smoothly
                    Color c = sr.color;
                    c.a = Mathf.Lerp(1f, 0f, a);
                    sr.color = c;
                }
            }

            t += Time.deltaTime;
            yield return null;
        }

        // Final cleanup
        foreach (var tile in group)
        {
            if (tile != null)
                Destroy(tile.gameObject);
        }
    }

    // process the move by clearing the group, collapsing columns and refilling the grid. using a coroutine allows us to wait for animations to finish before proceeding to the next step.
    private System.Collections.IEnumerator ProcessMove(List<Tile> group)
    {
        isAnimating = true; // locks input until the move is fully processed

        GameManager.Instance.UseMove();
        GameManager.Instance.AddScore(group.Count * 10);

        yield return StartCoroutine(ClearGroupAnimated(group, 0.15f)); // clear tiles with animation

        // animate the collapse of columns - find all tiles that need to fall down and their target positions, then animate them together (yield return -> wait until animation is done)
        yield return StartCoroutine(CollapseColumnsAnimated(fallDuration));

        // refill the grid with new tiles and animate them falling in
        yield return StartCoroutine(RefillGridAnimated(refillDuration));

        ResetAllScales();
        isAnimating = false;

        // defer Game Over UI until animations are finished
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            UIManager.Instance?.ShowHUD(false);
            UIManager.Instance?.ShowGameOver(true);
        }
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
                    Vector3 end = GetWorldPosition(x, writeY);
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

                var spawnPos = GetWorldPosition(x, y + refillSpawnOffset);
                GameObject obj = Instantiate(tilePrefab, spawnPos, Quaternion.identity);
                obj.transform.SetParent(transform);

                Tile tile = obj.GetComponent<Tile>();
                tile.x = x;
                tile.y = y;

                int randomType = Random.Range(0, colors.Length);
                tile.type = randomType;
                tile.SetColor(colors[randomType]);

                grid[x, y] = tile;

                Vector3 endPos = GetWorldPosition(x, y);
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
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsGameOver) return;
        if (UIManager.Instance != null && UIManager.Instance.IsInMenu) return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

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
    Vector3 GetWorldPosition(int x, int y)
    {
        return GetWorldPosition(x, (float)y);
    }

    Vector3 GetWorldPosition(int x, float y)
    {
        return new Vector3(
            x * tileSize - (width * tileSize) / 2f + tileSize / 2f,
            y * tileSize - (height * tileSize) / 2f + tileSize / 2f,
            0
        );
    }

    public void UpdateUIAnchors()
    {
        float gridWidth = width * tileSize;
        float gridHeight = height * tileSize;

        // local positions relative to GridRoot (0,0) center
        Vector3 localTopLeft = new Vector3(
            -gridWidth / 2f + tileSize / 2f,
            gridHeight / 2f + uiAnchorPadding,
            0f
        );

        Vector3 localTopRight = new Vector3(
            gridWidth / 2f - tileSize / 2f,
            gridHeight / 2f + uiAnchorPadding,
            0f
        );

        if (uiAnchorTopLeft != null) uiAnchorTopLeft.position = transform.TransformPoint(localTopLeft);
        if (uiAnchorTopRight != null) uiAnchorTopRight.position = transform.TransformPoint(localTopRight);
    }
}
