using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine;
using static MapGenerator;

public class RandomGeneration
{
    private List<GenerationPiece> pieces;

    public void Init(List<GenerationPiece> pieces)
    {
        this.pieces = pieces;
    }
    public int numBuildings = 5;

    public Dictionary<Vector2, GameObject> placedbuildings = new Dictionary<Vector2, GameObject>();

    public Vector2 minBuildingSize = Vector2.zero;
    public Vector2 maxBuildingSize = Vector2.zero;
    public int openingChance = 10;
    public GameObject floor;


    private static int[] rotations = new int[] { 0, 270, 0, 90, 180 };
    private int getRotationForRandomWall(int x, int y, Vector2 blockSize, Vector2 buildingSize)
    {
        int idx = -1;
        if (x == -1) return 1;
        if (y == -1) return 2;
        if (y + blockSize.y >= buildingSize.y) return 2;
        if (x + blockSize.x >= buildingSize.x) return 3;
        return idx;
    }
    void GenerateRandomMap()
    {

        var groundBounds = floor.GetComponent<Collider>().bounds;
        for (int i = 0; i < numBuildings; i++)
        {
            // building size
            var buildingSize = new Vector2((int)Random.Range(minBuildingSize.x, maxBuildingSize.x), (int)Random.Range(minBuildingSize.y, maxBuildingSize.y));
            // make sure center isn't inside a generated building
            var newBounds = getRandomBounds(buildingSize, groundBounds);
            var extendedBounds = new Bounds(newBounds.center, newBounds.size * 1.5f);
            var foundSolution = false;
            for (int b = 0; b < 1000; b++)
            {
                var intersecting = false;
                // foreach (var bound in generatedBounds)
                // {
                //     if (bound.Intersects(extendedBounds))
                //     {
                //         intersecting = true;
                //     }
                // }

                if (intersecting)
                {
                    newBounds = getRandomBounds(buildingSize, groundBounds);
                }
                else
                {
                    foundSolution = true;
                    break;
                }
                Debug.Log(b);
            }
            if (!foundSolution)
            {
                continue;
            }
            var parent = new GameObject("Generated Building");
            for (int x = 0; x < buildingSize.x;)
            {
                var building = pieces[Random.Range(0, pieces.Count)];
                for (int y = 0; y < buildingSize.y;)
                {
                    building = pieces[Random.Range(0, pieces.Count)];
                    // if the first or last row then spawn
                    // building dimensions int
                    var blockSize = new Vector2((int)building.dimensions.x, (int)building.dimensions.y);
                    var wallIdx = getRotationForRandomWall(x, y, blockSize, buildingSize);
                    if (wallIdx > 0)
                    {
                        // remove random pieces for exits
                        if (Random.Range(0, 100) < openingChance && x != 1 && y != 0)
                        {
                            continue;
                        }
                        var rot = rotations[wallIdx];
                        // var b = Instantiate(building.prefab, new Vector3(newBounds.center.x + (blockSize.x + x), height, newBounds.center.z + (blockSize.y + y)), Quaternion.Euler(0, rot, 0), parent.transform);
                    }
                    y += (int)building.dimensions.y;

                }
                x += (int)building.dimensions.x;
            }
            // generatedBounds.Add(newBounds);
            // generatedBuildings.Add(parent);
        }


        // Generate the outer perimeter.
        // Generate the individual lines.
        // Delete passageway buildings.
        // Add pickups and spawns.

    }


    private Bounds getRandomBounds(Vector2 bs, Bounds floorBounds)
    {
        var center = new Vector3(Random.Range(floorBounds.min.x + bs.x, floorBounds.max.x - bs.x), 0, Random.Range(floorBounds.min.z + bs.y, floorBounds.max.z - bs.y));
        var bv3 = new Vector3(bs.x, 0, bs.y);
        var newBounds = new Bounds(center, bv3);
        return newBounds;
    }
}