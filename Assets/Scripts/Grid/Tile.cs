using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x;
    public int y;
    public int type; // color/type id

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetColor(Color color)
    {
        spriteRenderer.color = color;
    }

    // going to switch to the new input system later, but for now this is fine for quick prototyping
    private void OnMouseDown()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("GridManager.Instance is null, cannot handle tile click.");
            return;
        }

        GridManager.Instance.HandleTileClick(this);
    }
}
