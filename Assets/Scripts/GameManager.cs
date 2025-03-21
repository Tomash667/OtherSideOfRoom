using UnityEngine;

public class GameManager : MonoBehaviour
{
    class Tile
    {
        public GameObject gameObject;
        public int value;
    }

    public GameObject baseTile;
    public GameObject[] numbers;
    public int width, height, traps;

    private Tile[] tiles;

    private const int TRAP = -1;
    private readonly Vector2Int[] dirs = new Vector2Int[]
    {
        new(-1, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, -1),
        new(0, 1),
        new(1, -1),
        new(1, 0),
        new(1, 1)
    };

    private void Start()
    {
        CreateTiles();
        InitLevel();
    }

    private void CreateTiles()
    {
        int count = width * height;
        tiles = new Tile[count];
        for (int i = 0; i < count; ++i)
            tiles[i] = new Tile();
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                Tile tile = new Tile();
                tile.gameObject = Instantiate(baseTile, baseTile.transform.position + new Vector3(2.0f * x, 0, 2.0f * y), Quaternion.identity);
                TileInfo tileInfo = tile.gameObject.GetComponent<TileInfo>();
                tileInfo.x = x;
                tileInfo.y = y;
                tiles[x + y * width] = tile;
            }
        }
        Destroy(baseTile);
    }

    private void InitLevel()
    {
        int count = width * height;

        // place traps
        for (int i = 0; i < traps; ++i)
        {
            while (true)
            {
                int index = Random.Range(0, count);
                if (tiles[index].value != TRAP)
                {
                    tiles[index].value = TRAP;
                    break;
                }
            }
        }

        // calculate values
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                Tile tile = tiles[x + y * width];
                if (tile.value == TRAP)
                    continue;
                int nearTraps = 0;
                foreach (Vector2Int dir in dirs)
                {
                    if (IsTrap(x + dir.x, y + dir.y))
                        ++nearTraps;
                }
                tile.value = nearTraps;
                GameObject number = Instantiate(numbers[nearTraps], tile.gameObject.transform);
                number.transform.position = number.transform.position + new Vector3(0, 0.01f, 0);
            }
        }
    }

    private bool IsTrap(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;
        return tiles[x + y * width].value == TRAP;
    }

    public bool StepOn(GameObject gameObject)
    {
        TileInfo tileInfo = gameObject.GetComponent<TileInfo>();
        Tile tile = tiles[tileInfo.x + tileInfo.y * width];
        if (tile.value == TRAP)
        {
            Destroy(gameObject);
            return true;
        }
        return false;
    }
}
