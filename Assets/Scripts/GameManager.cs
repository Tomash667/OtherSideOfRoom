using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    class Tile
    {
        public GameObject gameObject;
        public int x, y, value;
        public bool known, missing;
    }

    public GameObject baseTile;
    public GameObject[] numbers;
    public int width, height, traps;

    private Tile[] tiles;
    private Vector3 offset;
    private Vector2Int markerPos;

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
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                Tile tile = new()
                {
                    x = x,
                    y = y,
                    gameObject = Instantiate(baseTile, baseTile.transform.position + new Vector3(2.0f * x, 0, 2.0f * y), Quaternion.identity)
                };
                TileInfo tileInfo = tile.gameObject.GetComponent<TileInfo>();
                tileInfo.x = x;
                tileInfo.y = y;
                tiles[x + y * width] = tile;
            }
        }
        offset = baseTile.transform.position - new Vector3(1, 0, 1);
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
                if (tile.value != TRAP)
                    tile.value = GetNearbyTraps(tile);
            }
        }

        Solve();
        for (int i = 0; i < count; ++i)
            tiles[i].known = false;

        // reveal first row
        for (int x = 0; x < width; ++x)
            Reveal(tiles[x]);
    }

    private IEnumerable<Tile> GetNearbyTiles(Tile tile)
    {
        foreach (Vector2Int dir in dirs)
        {
            int x = tile.x + dir.x, y = tile.y + dir.y;
            if (x >= 0 && x < width && y >= 0 && y < height)
                yield return tiles[x + y * width];
        }
    }

    private int GetNearbyTraps(Tile tile)
    {
        return GetNearbyTiles(tile).Count(t => t.value == TRAP);
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
        else if (!tile.known)
            Reveal(tile);
        return false;
    }

    private void Reveal(Tile tile)
    {
        tile.known = true;
        if (tile.value >= 0)
        {
            GameObject number = Instantiate(numbers[tile.value], tile.gameObject.transform);
            number.transform.position = number.transform.position + new Vector3(0, 0.01f, 0);
        }
        else
        {
            Destroy(tile.gameObject);
            tile.missing = true;
        }
    }

    public bool CheckForTile(Vector3 pos, GameObject marker)
    {
        if (pos.x < offset.x || pos.z < offset.z)
            return false;

        markerPos = new(Mathf.FloorToInt((pos.x - offset.x) / 2), Mathf.FloorToInt((pos.z - offset.z) / 2));
        if (markerPos.x < width && markerPos.y < height)
        {
            Tile tile = tiles[markerPos.x + markerPos.y * width];
            if (tile.missing)
                return false;
            else
            {
                marker.transform.position = new Vector3(offset.x + 2.0f * markerPos.x + 1, offset.y + 0.05f, offset.z + 2.0f * markerPos.y);
                return true;
            }
        }
        else
            return false;
    }

    public void DestroyTile()
    {
        Tile tile = tiles[markerPos.x + markerPos.y * width];
        tile.missing = true;
        Destroy(tile.gameObject);
    }

    private void Solve()
    {
        List<Tile> toCheck = new(), toCheckNew = new();
        for (int x = 0; x < width; ++x)
        {
            Tile tile = tiles[x];
            tile.known = true;
            //Reveal(tile);
            if(tile.value != TRAP)
                toCheck.Add(tile);
        }

        int pass = 1, removedTraps = 0;
        while (toCheck.Count > 0)
        {
            bool anything = false;

            foreach (Tile tile in toCheck)
            {
                int knownTraps = 0, unknownTiles = 0;
                foreach (Tile tile2 in GetNearbyTiles(tile))
                {
                    if (tile2.known)
                    {
                        if (tile2.value == TRAP)
                            ++knownTraps;
                    }
                    else
                        ++unknownTiles;
                }

                if (knownTraps == tile.value
                    || tile.value - knownTraps == unknownTiles)
                {
                    // all traps found, reveal unknown tiles
                    // or
                    // all unknown tiles have traps, reveal them
                    foreach (Tile tile2 in GetNearbyTiles(tile).Where(t => !t.known))
                    {
                        tile2.known = true;
                        //Reveal(tile2);
                        if(tile2.value != TRAP)
                            toCheckNew.Add(tile2);
                    }
                    anything = true;
                }
                else
                {
                    // can't guess
                    toCheckNew.Add(tile);
                }
            }

            if (!anything)
            {
                // need better algorithm :P
                // for now juest remove 1 trap and try again
                Tile tile = GetNearbyTiles(toCheckNew[0]).First(t => !t.known && t.value == TRAP);
                tile.value = GetNearbyTraps(tile);
                foreach (Tile tile2 in GetNearbyTiles(tile).Where(t => t.value != TRAP))
                    tile2.value = GetNearbyTraps(tile2);
                ++removedTraps;
            }

            (toCheck, toCheckNew) = (toCheckNew, toCheck);
            toCheckNew.Clear();
            ++pass;
        }

        Debug.Log($"Solved in {pass - 1} passes, removed traps {removedTraps}");
    }
}
