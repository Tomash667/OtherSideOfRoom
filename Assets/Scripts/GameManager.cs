using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    enum TileDir
    {
        LeftBottom,
        Left,
        LeftTop,
        Bottom,
        Top,
        RightBottom,
        Right,
        RightTop
    }

    class Tile
    {
        public GameObject gameObject;
        public int x, y, value;
        public bool known, missing;
    }

    class TileSolver
    {
        public Tile[] tiles;

        public Tile GetTile(TileDir dir)
        {
            return tiles[(int)dir];
        }

        public bool IsUnknown(TileDir dir)
        {
            Tile tile = tiles[(int)dir];
            return tile != null && !tile.known;
        }

        public bool IsKnown(TileDir dir)
        {
            Tile tile = tiles[(int)dir];
            return tile != null && tile.known;
        }

        public bool IsKnownOrOutside(TileDir dir)
        {
            Tile tile = tiles[(int)dir];
            return tile == null || tile.known;
        }
    }

    public GameObject baseTile, collapsingTile, collapsingTrap, dustParticle;
    public GameObject[] numbers;
    public GameObject winCamera;
    public GameObject winText;
    public ParticleSystem winParticle;
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

    private void Update()
    {
        if (Input.GetKeyDown(Application.isEditor ? KeyCode.Q : KeyCode.Escape))
        {
            SceneManager.LoadScene(0);
            return;
        }
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

        while (true)
        {
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

            // solve and if failed regenerate
            if (!Solve())
            {
                for (int i = 0; i < count; ++i)
                {
                    Tile tile = tiles[i];
                    tile.known = false;
                    tile.value = 0;
                }
                continue;
            }

            // revert state after solving
            for (int i = 0; i < count; ++i)
                tiles[i].known = false;

            // reveal first row
            for (int x = 0; x < width; ++x)
                Reveal(tiles[x]);
            break;
        }
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

    private TileSolver GetNearbyTilesAll(Tile tile)
    {
        Tile[] result = new Tile[8];
        if (tile.x > 0)
        {
            if (tile.y > 0)
                result[(int)TileDir.LeftBottom] = tiles[tile.x - 1 + (tile.y - 1) * width];
            result[(int)TileDir.Left] = tiles[tile.x - 1 + tile.y * width];
            if (tile.y < height - 1)
                result[(int)TileDir.LeftTop] = tiles[tile.x - 1 + (tile.y + 1) * width];
        }
        if (tile.y > 0)
            result[(int)TileDir.Bottom] = tiles[tile.x + (tile.y - 1) * width];
        if (tile.y < height - 1)
            result[(int)TileDir.Top] = tiles[tile.x + (tile.y + 1) * width];
        if (tile.x < width - 1)
        {
            if (tile.y > 0)
                result[(int)TileDir.RightBottom] = tiles[tile.x + 1 + (tile.y - 1) * width];
            result[(int)TileDir.Right] = tiles[tile.x + 1 + tile.y * width];
            if (tile.y < height - 1)
                result[(int)TileDir.RightTop] = tiles[tile.x + 1 + (tile.y + 1) * width];
        }
        return new TileSolver { tiles = result };
    }

    private int GetNearbyTraps(Tile tile)
    {
        return GetNearbyTiles(tile).Count(t => t.value == TRAP);
    }

    private int GetNearbyUnknownTraps(Tile tile)
    {
        if (tile.value == TRAP)
            return -1;
        return GetNearbyTiles(tile).Count(t => !t.known && t.value == TRAP);
    }

    public bool StepOn(GameObject gameObject)
    {
        TileInfo tileInfo = gameObject.GetComponent<TileInfo>();
        Tile tile = tiles[tileInfo.x + tileInfo.y * width];
        if (tile.value == TRAP)
        {
            // step on trap, collapse
            DestroyTile(tile);
            return true;
        }
        else if (!tile.known)
        {
            if (!GetNearbyTiles(tile).Any(t => t.known))
            {
                // jumped on unknown tile with no known tiles next to it, always collapse
                DestroyTile(tile);
                return true;
            }
            Reveal(tile);
        }
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

    private void SolveReveal(Tile tile)
    {
        tile.known = true;
        //Reveal(tile);
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

    public void DestroyMarkedTile()
    {
        DestroyTile(tiles[markerPos.x + markerPos.y * width]);
    }

    private void DestroyTile(Tile tile)
    {
        tile.missing = true;
        Instantiate(tile.value == TRAP ? collapsingTrap : collapsingTile, tile.gameObject.transform.position, tile.gameObject.transform.rotation);
        Instantiate(dustParticle, tile.gameObject.transform.position, dustParticle.transform.rotation);
        Destroy(tile.gameObject);
    }

    private bool Solve()
    {
        List<Tile> toCheck = new(), toCheckNew = new();
        for (int x = 0; x < width; ++x)
        {
            Tile tile = tiles[x];
            SolveReveal(tile);
            if (tile.value != TRAP)
                toCheck.Add(tile);
        }

        int pass = 0, removedTraps = 0;
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
                        SolveReveal(tile2);
                        if (tile2.value != TRAP)
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
                (toCheck, toCheckNew) = (toCheckNew, toCheck);
                toCheckNew.Clear();
                foreach (Tile tile in toCheck)
                {
                    if (GetNearbyUnknownTraps(tile) == 2)
                    {
                        TileSolver s = GetNearbyTilesAll(tile);
                        int doneSomething = 0; // 1-partial, 2-fully solved

                        // ???
                        // k2k
                        // xxx
                        if (s.IsUnknown(TileDir.LeftTop) && s.IsUnknown(TileDir.Top) && s.IsUnknown(TileDir.RightTop)
                            && s.IsKnown(TileDir.Left) && s.IsKnown(TileDir.Right)
                            && s.IsKnownOrOutside(TileDir.LeftBottom) && s.IsKnownOrOutside(TileDir.Bottom) && s.IsKnownOrOutside(TileDir.RightBottom))
                        {
                            int leftUnknownTraps = GetNearbyUnknownTraps(s.GetTile(TileDir.Left));
                            int rightUnknownTraps = GetNearbyUnknownTraps(s.GetTile(TileDir.Right));
                            if (leftUnknownTraps == 1 && rightUnknownTraps == 1)
                            {
                                // safe tile between two traps
                                // TST
                                // 121
                                Tile t = s.GetTile(TileDir.Top);
                                SolveReveal(t);
                                toCheckNew.Add(t);
                                SolveReveal(s.GetTile(TileDir.LeftTop));
                                SolveReveal(s.GetTile(TileDir.RightTop));
                                doneSomething = 2;
                            }
                            else if (leftUnknownTraps == 1)
                            {
                                // trap on right
                                // ??T
                                // 12k
                                SolveReveal(s.GetTile(TileDir.RightTop));
                                doneSomething = 1;
                            }
                            else if (rightUnknownTraps == 1)
                            {
                                // trap on left
                                // T??
                                // k21
                                SolveReveal(s.GetTile(TileDir.LeftTop));
                                doneSomething = 1;
                            }
                        }

                        // xxx
                        // k2k
                        // ???
                        if (doneSomething == 0
                            && s.IsUnknown(TileDir.LeftBottom) && s.IsUnknown(TileDir.Bottom) && s.IsUnknown(TileDir.RightBottom)
                            && s.IsKnown(TileDir.Left) && s.IsKnown(TileDir.Right)
                            && s.IsKnownOrOutside(TileDir.LeftTop) && s.IsKnownOrOutside(TileDir.Top) && s.IsKnownOrOutside(TileDir.RightTop))
                        {
                            int leftUnknownTraps = GetNearbyUnknownTraps(s.GetTile(TileDir.Left));
                            int rightUnknownTraps = GetNearbyUnknownTraps(s.GetTile(TileDir.Right));
                            if (leftUnknownTraps == 1 && rightUnknownTraps == 1)
                            {
                                // safe tile between two traps
                                // 121
                                // STS
                                Tile t = s.GetTile(TileDir.Bottom);
                                SolveReveal(t);
                                toCheckNew.Add(t);
                                SolveReveal(s.GetTile(TileDir.LeftBottom));
                                SolveReveal(s.GetTile(TileDir.RightBottom));
                                doneSomething = 2;
                            }
                            else if (leftUnknownTraps == 1)
                            {
                                // trap on right
                                // 12k
                                // ??T
                                SolveReveal(s.GetTile(TileDir.RightBottom));
                                doneSomething = 1;
                            }
                            else if (rightUnknownTraps == 1)
                            {
                                // trap on left
                                // k21
                                // T??
                                SolveReveal(s.GetTile(TileDir.LeftBottom));
                                doneSomething = 1;
                            }
                        }

                        // ?kx
                        // ?2x
                        // ?kx
                        if (doneSomething == 0
                            && s.IsUnknown(TileDir.LeftTop) && s.IsUnknown(TileDir.Left) && s.IsUnknown(TileDir.LeftBottom)
                            && s.IsKnown(TileDir.Top) && s.IsKnown(TileDir.Bottom)
                            && s.IsKnownOrOutside(TileDir.RightTop) && s.IsKnownOrOutside(TileDir.Right) && s.IsKnownOrOutside(TileDir.RightBottom))
                        {
                            int topUnknownTraps = GetNearbyUnknownTraps(s.GetTile(TileDir.Top));
                            int bottomUnknownTraps = GetNearbyUnknownTraps(s.GetTile(TileDir.Bottom));
                            if (topUnknownTraps == 1 && bottomUnknownTraps == 1)
                            {
                                // safe tile between two traps
                                // T1
                                // S2
                                // T1
                                Tile t = s.GetTile(TileDir.Left);
                                SolveReveal(t);
                                toCheckNew.Add(t);
                                SolveReveal(s.GetTile(TileDir.LeftTop));
                                SolveReveal(s.GetTile(TileDir.LeftBottom));
                                doneSomething = 2;
                            }
                            else if (topUnknownTraps == 1)
                            {
                                // trap on bottom
                                // ?1
                                // ?2
                                // Tk
                                SolveReveal(s.GetTile(TileDir.LeftBottom));
                                doneSomething = 1;
                            }
                            else if (bottomUnknownTraps == 1)
                            {
                                // trap on top
                                // Tk
                                // ?2
                                // ?1
                                SolveReveal(s.GetTile(TileDir.LeftTop));
                                doneSomething = 1;
                            }
                        }

                        // xk?
                        // x2?
                        // xk?
                        if (doneSomething == 0
                            && s.IsUnknown(TileDir.RightTop) && s.IsUnknown(TileDir.Right) && s.IsUnknown(TileDir.RightBottom)
                            && s.IsKnown(TileDir.Top) && s.IsKnown(TileDir.Bottom)
                            && s.IsKnownOrOutside(TileDir.LeftTop) && s.IsKnownOrOutside(TileDir.Left) && s.IsKnownOrOutside(TileDir.LeftBottom))
                        {
                            int topUnknownTraps = GetNearbyUnknownTraps(s.GetTile(TileDir.Top));
                            int bottomUnknownTraps = GetNearbyUnknownTraps(s.GetTile(TileDir.Bottom));
                            if (topUnknownTraps == 1 && bottomUnknownTraps == 1)
                            {
                                // safe tile between two traps
                                // 1T
                                // 2S
                                // 1T
                                Tile t = s.GetTile(TileDir.Right);
                                SolveReveal(t);
                                toCheckNew.Add(t);
                                SolveReveal(s.GetTile(TileDir.RightTop));
                                SolveReveal(s.GetTile(TileDir.RightBottom));
                                doneSomething = 2;
                            }
                            else if (topUnknownTraps == 1)
                            {
                                // trap on bottom
                                // 1?
                                // 2?
                                // kT
                                SolveReveal(s.GetTile(TileDir.RightBottom));
                                doneSomething = 1;
                            }
                            else if (bottomUnknownTraps == 1)
                            {
                                // trap on top
                                // kT
                                // 2?
                                // 1?
                                SolveReveal(s.GetTile(TileDir.RightTop));
                                doneSomething = 1;
                            }
                        }

                        if (doneSomething > 0)
                        {
                            Debug.Log($"Algo2 {doneSomething} ({tile.x},{tile.y}) pass:{pass}");
                            anything = true;
                        }
                        if (doneSomething < 2)
                            toCheckNew.Add(tile);
                    }
                    else
                    {
                        // can't guess
                        toCheckNew.Add(tile);
                    }
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

        if (tiles.All(t => t.known))
        {
            Debug.Log($"Solved in {pass} passes, removed traps {removedTraps}");
            return true;
        }
        else
        {
            Debug.LogWarning($"Failed to solve after {pass} passes, removed traps {removedTraps}");
            return false;
        }
    }

    public void Win()
    {
        // play fanfare & particle
        GetComponent<AudioSource>().Play();
        winParticle.Play();
        // set camera to look at reward
        Camera.main.gameObject.SetActive(false);
        winCamera.SetActive(true);
        // show win ui
        winText.SetActive(true);
        // unlock next level
        int level = SceneManager.GetActiveScene().buildIndex;
        if (level != 3)
        {
            GameData gameData = SaveLoadManager.Load();
            if (level == 1 && !gameData.level2Unlocked)
            {
                gameData.level2Unlocked = true;
                SaveLoadManager.Save(gameData);
            }
            else if (level == 2 && !gameData.level3Unlocked)
            {
                gameData.level3Unlocked = true;
                SaveLoadManager.Save(gameData);
            }
        }
    }
}
