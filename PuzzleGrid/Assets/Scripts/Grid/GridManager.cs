using UnityEngine;

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

    public void HandleTileClick(Tile tile)
    {
        Debug.Log($"Clicked tile at {tile.x}, {tile.y}");
    }
}
