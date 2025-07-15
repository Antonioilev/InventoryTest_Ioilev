using UnityEngine;

[System.Serializable]
public class CellPos
{
    public int x;
    public int y;

    public CellPos(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2Int ToVector2Int() => new Vector2Int(x, y);
}
