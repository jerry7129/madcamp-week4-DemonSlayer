using UnityEngine;

public class SimpleMapGenerator : MonoBehaviour
{
    public GameObject wallPrefab;
    public Vector2 roomSize = new Vector2(20, 10);

    void Start()
    {
        GenerateRoom();
    }

    void GenerateRoom()
    {
        if (wallPrefab == null) return;

        // Floor
        CreateWall(new Vector2(0, -roomSize.y / 2), new Vector2(roomSize.x + 1, 1));
        // Ceiling
        CreateWall(new Vector2(0, roomSize.y / 2), new Vector2(roomSize.x + 1, 1));
        // Left Wall
        CreateWall(new Vector2(-roomSize.x / 2, 0), new Vector2(1, roomSize.y));
        // Right Wall
        CreateWall(new Vector2(roomSize.x / 2, 0), new Vector2(1, roomSize.y));
    }

    void CreateWall(Vector2 pos, Vector2 scale)
    {
        GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity);
        wall.transform.localScale = scale;
        wall.transform.SetParent(this.transform);
    }
}