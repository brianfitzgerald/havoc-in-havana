using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

public enum TileType
{
    EMPTY,
    BUILDING,
    PICKUP,
    SPAWN,
    // used to determine orientation
    PAVEMENT,
    JUMP_PAD
}

public enum Sides
{
    FRONT,
    FRONT_BACK,
    FRONT_RIGHT,
}

public struct PieceToSpawn
{
    public GameObject prefab;
    public Vector3 position;
    public Quaternion rotation;
    public PieceToSpawn(GameObject go, Vector3 pos, Quaternion r)
    {
        this.prefab = go;
        this.position = pos;
        this.rotation = r;
    }
}

public struct GenerationPiece
{
    public string name;
    public GameObject prefab;
    public Vector2Int dimensions;
    public TileType tileType;
    public Sides sides;
    public GenerationPiece(string name, int x, int y, TileType tileType, Sides sides = Sides.FRONT)
    {
        this.name = name;
        this.prefab = Resources.Load<GameObject>($"Cubes/{name}");
        var filter = this.prefab.GetComponentInChildren<MeshFilter>();
        if (filter == null)
        {
            filter = this.prefab.GetComponent<MeshFilter>();
        }
        var bounds = filter.sharedMesh.bounds;
        this.dimensions = new Vector2Int(x, y);
        this.tileType = tileType;
        this.sides = sides;
    }
    // used for pickup and spawn
    public GenerationPiece(TileType tileType, GameObject prefab)
    {
        this.sides = Sides.FRONT;
        this.dimensions = Vector2Int.one;
        this.prefab = prefab;
        this.name = prefab.name;
        this.tileType = tileType;
    }
}


public class MapGenerator : MonoBehaviourPunCallbacks
{
    public static MapGenerator Instance;

    private List<GenerationPiece> piecesArray;
    public GameObject pickupPrefab;
    public GameObject jumpPadPrefab;
    public GameObject spawnPrefab;
    public Dictionary<TileType, List<GenerationPiece>> piecesDict = new Dictionary<TileType, List<GenerationPiece>>();

    public void Initialize()
    {
        Instance = this;
        this.piecesArray = new List<GenerationPiece>(new GenerationPiece[] {
            new GenerationPiece("1x1_red", 1, 1, TileType.BUILDING, Sides.FRONT),
            new GenerationPiece("1x1_tall", 1, 1, TileType.BUILDING, Sides.FRONT),
            new GenerationPiece("1x1_yellow", 1, 1, TileType.BUILDING, Sides.FRONT),
            new GenerationPiece("1x2_awning", 1, 2, TileType.BUILDING),
            new GenerationPiece("1x2_blue", 1, 2, TileType.BUILDING, Sides.FRONT),
            new GenerationPiece("1x1_blue_porch", 1, 1, TileType.BUILDING, Sides.FRONT),
            new GenerationPiece("1x2_yellow_awning", 1, 2, TileType.BUILDING, Sides.FRONT),
        });
        foreach (var piece in this.piecesArray)
        {
            if (!this.piecesDict.ContainsKey(piece.tileType))
            {
                this.piecesDict[piece.tileType] = new List<GenerationPiece>();
            }
            this.piecesDict[piece.tileType].Add(piece);
        }
        this.piecesDict[TileType.PICKUP] = new List<GenerationPiece>();
        this.piecesDict[TileType.SPAWN] = new List<GenerationPiece>();
        this.piecesDict[TileType.JUMP_PAD] = new List<GenerationPiece>();
        this.piecesDict[TileType.PICKUP].Add(new GenerationPiece(TileType.PICKUP, pickupPrefab));
        this.piecesDict[TileType.JUMP_PAD].Add(new GenerationPiece(TileType.JUMP_PAD, jumpPadPrefab));
        this.piecesDict[TileType.SPAWN].Add(new GenerationPiece(TileType.SPAWN, spawnPrefab));
    }

    // blocks that are part of a double-piece that should not be spawned
    [HideInInspector]
    public List<Vector2Int> overrideBlocks = new List<Vector2Int>();

    // assumed to be a plane

    private List<GameObject> generatedObjects = new List<GameObject>();
    // first val is never used
    public GameObject floor;

    public float pieceY = 1;
    public Vector2 tileScale = Vector2.one;


    private Hash128 textureHash;

    public string mapName;


    public void GenerateObjectsFromTileTypes(TileType[,] tiles)
    {
        var startCorner = floor.transform.position;
        int height = tiles.GetLength(0);
        int width = tiles.GetLength(1);
        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < width; y++)
            {
                var type = tiles[x, y];
                var position = new Vector3(startCorner.x + x * tileScale.x, pieceY, startCorner.y + y * tileScale.y);
                if (!piecesDict.ContainsKey(type) || type == TileType.EMPTY)
                {
                    continue;
                }
                if (type == TileType.SPAWN)
                {
                    position.y = 15f;
                    var newSpawn = Instantiate(spawnPrefab, position, Quaternion.identity);
                    newSpawn.transform.parent = GameController.Instance.spawnsParent.transform;
                    GameController.Instance.spawnPositions.Add(newSpawn.transform.position);
                    generatedObjects.Add(newSpawn);
                    continue;
                }
                else if (type == TileType.PICKUP)
                {
                    position.y = 3.5f;
                    var pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
                    pickup.transform.parent = GameController.Instance.pickupsParent.transform;
                    generatedObjects.Add(pickup);
                    continue;
                }
                else if (type == TileType.JUMP_PAD)
                {
                    position.y = 3f;
                    var jumpPad = Instantiate(jumpPadPrefab, position, Quaternion.identity);
                    generatedObjects.Add(jumpPad);
                    continue;
                }
                if (MapGenerator.Instance.overrideBlocks.Contains(new Vector2Int(x, y)))
                {
                    continue;
                }
                // get whether we can grow the tile
                var size = GetSize(tiles, x, y, width, height);
                // var size = Vector2Int.one;
                var facingSides = new List<int>();
                facingSides.Add(0);
                if (type == TileType.BUILDING)
                {
                    facingSides = GetFacingSides(tiles, x, y);
                }
                var rotations = GetSidesForRotation(facingSides);
                var piece = GetPiece(type, size);
                var prefab = piece.prefab;
                var r = facingSides.ToArray()[0];
                if (size.x > 1)
                {
                    r = 0;
                }
                if (size.y > 1)
                {
                    r = 90;
                    position.z += 4;
                }
                var rotation = Quaternion.Euler(new Vector3(0, r + rotationOffset, 0));
                var placedPiece = Instantiate(prefab, position, rotation);
                placedPiece.transform.localScale = Vector3.one;
                if (type == TileType.PICKUP)
                {
                    placedPiece.transform.parent = GameController.Instance.pickupsParent.transform;
                }
                else if (type == TileType.SPAWN)
                {
                    placedPiece.transform.parent = GameController.Instance.spawnsParent.transform;
                }
                generatedObjects.Add(placedPiece);
            }
        }
    }

    public static Dictionary<Color, TileType> colorMap = new Dictionary<Color, TileType>{
        {new Color32(255, 235, 4, 255), TileType.BUILDING},
        {new Color32(204, 204, 204, 255), TileType.PAVEMENT},
        {new Color32(255, 255, 255, 255), TileType.EMPTY},
        {new Color32(0, 255, 0, 255), TileType.SPAWN},
        {new Color32(0, 255, 255, 255), TileType.PICKUP},
        {new Color32(0, 153, 255, 255), TileType.JUMP_PAD},
    };

    // check if any surrounding blocks are 
    private static Vector2Int GetSize(TileType[,] tiles, int x, int y, int width, int height)
    {
        var type = tiles[x, y];
        var pieceSize = new Vector2Int(1, 1);
        if (RandomCheck(pieceSize)) return pieceSize;
        if (x + 1 < width - 1 && tiles[x + 1, y] == type)
        {
            pieceSize.x += 1;
            MapGenerator.Instance.overrideBlocks.Add(new Vector2Int(x + 1, y));
        }
        if (RandomCheck(pieceSize)) return pieceSize;
        if (y < height - 1 && tiles[x, y + 1] == type)
        {
            pieceSize.y += 1;
            MapGenerator.Instance.overrideBlocks.Add(new Vector2Int(x, y + 1));
        }
        return pieceSize;
    }

    public float rotationOffset = -90;

    // directions are 0, 90, 180, 270
    // up is 0, right is 90, down is 180, left is 270
    private static List<int> GetFacingSides(TileType[,] tiles, int x, int y)
    {
        List<int> sides = new List<int>();
        var p = TileType.PAVEMENT;
        var pieceSize = new Vector2Int(1, 1);
        var width = tiles.GetLength(0);
        var height = tiles.GetLength(1);
        if (x + 1 < width && tiles[x + 1, y] == p)
        {
            sides.Add(90);
        }
        if (x > 0 && tiles[x - 1, y] == p)
        {
            sides.Add(-90);
        }
        if (y > 0 && tiles[x, y - 1] == p)
        {
            sides.Add(180);
        }
        if (y + 1 < height && tiles[x, y + 1] == p)
        {
            sides.Add(0);
        }
        if (sides.Count == 0)
        {
            sides.Add(0);
        }
        return sides;
    }

    private Sides GetSidesForRotation(List<int> facingSides)
    {
        Sides sides = Sides.FRONT;
        if ((facingSides.Contains(90) && facingSides.Contains(270)) || (facingSides.Contains(0) && facingSides.Contains(180)))
        {
            sides = Sides.FRONT_BACK;
        }
        // TODO get sides too
        var initialSide = facingSides.ToList()[0];
        if (facingSides.Contains(initialSide + 90))
        {
            sides = Sides.FRONT_RIGHT;
        }
        return sides;
    }

    private static bool RandomCheck(Vector2Int pieceSize)
    {
        // if we already extended once then return
        if (pieceSize.x > 1 || pieceSize.y > 1)
        {
            return true;
        }
        if (UnityEngine.Random.Range(0, 5) > 1)
        {
            return true;
        }
        return false;
    }

    private int previousPieceIndex = -1;

    public GenerationPiece GetPiece(TileType tileType, Vector2Int size)
    {
        var pieceOptions = MapGenerator.Instance.piecesDict[tileType];
        var piece = pieceOptions[UnityEngine.Random.Range(0, pieceOptions.Count)];
        for (int i = 0; i < 100; i++)
        {
            var idx = UnityEngine.Random.Range(0, pieceOptions.Count);
            piece = pieceOptions[idx];
            var invertedSize = new Vector2Int(size.y, size.x);
            if ((piece.dimensions == size || piece.dimensions == invertedSize) && idx != previousPieceIndex)
            {
                previousPieceIndex = idx;
                return piece;
            }
        }
        return piece;
    }

    // generates a tileset for all pixels in the input texture
    // no map gen in here, just parsing
    public static TileType[,] GenerateMapFromTexture(Texture2D tex)
    {
        TileType[,] result = new TileType[tex.height, tex.width];
        var colorsSeen = new HashSet<Color32>();
        for (int x = 0; x < tex.height; x++)
        {
            for (int y = 0; y < tex.width; y++)
            {
                Color32 pix = tex.GetPixel(x, y);
                var type = TileType.EMPTY;
                if (!colorsSeen.Contains(pix))
                {
                    colorsSeen.Add(pix);
                }
                foreach (var item in colorMap)
                {
                    if (item.Key == pix)
                    {
                        type = item.Value;
                    }
                }
                result[x, y] = type;
            }
        }
        return result;
    }

    public void GenerateFromTexture(Texture2D mapTex)
    {
        for (int i = 0; i < generatedObjects.Count; i++)
        {
            foreach (Transform child in generatedObjects[i].transform)
            {
                GameObject.Destroy(child.gameObject);
            }
            Destroy(generatedObjects[i]);
        }
        generatedObjects.Clear();
        overrideBlocks.Clear();
        var tiles = GenerateMapFromTexture(mapTex);
        GenerateObjectsFromTileTypes(tiles);
    }


    void Update()
    {
        var prevHash = textureHash;
    }
    // TODO spawn from multiple textures
    // TODO merge building nodes to create 2x types
    // TODO multi story buildings - chance of taller, or different color orange?
    // TODO check if texture changed

}