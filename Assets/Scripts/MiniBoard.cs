using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniBoard : MonoBehaviour
{

    private int height;                 // The number of rows on the mini board (how tall it will be).
    private int width;                  // The number of columns on the mini board (how wide it will be).
    public GameObject floorTile;        // An array of floor tile prefabs.
    public GameObject wallTile;         // An array of wall tile prefabs.
    public GameObject doorTile;
    public GameObject fogTile;
    public GameObject playerTile;

    private float scale;
    [SerializeField]
    private float scaleFactor;
    private float mapOffsetX;
    private float mapOffsetY;

    private TileType[][] tiles;         // A jagged array of tile types representing the mini board, like a grid.
    private GameObject miniBoardHolder; // GameObject that acts as a container for all other tiles.
    private GameObject tileInstance;

    void Start()
    {

    }

    void Update()
    {

    }

    public void SetupScene()
    {
        Map map = gameObject.GetComponent<Map>();
        Board board = gameObject.GetComponent<Board>();
        height = map.Height;
        width = map.Width;
        scale = 5.0f / height;
        mapOffsetX = board.Radius + 1 + 1 + 2.5f;
        mapOffsetY = -2.5f;

        // Create the mini board holder.
        miniBoardHolder = new GameObject("MiniBoardHolder");

        SetupTilesArray();
        InstantiateTiles();
    }

    public void NewMap()
    {
        Destroy(miniBoardHolder);
        miniBoardHolder = new GameObject("MiniBoardHolder");
        InstantiateTiles();
    }

    public void SetupTilesArray()
    {
        // Set the tiles jagged array to the correct width.
        tiles = new TileType[width][];

        // Go through all the tile arrays...
        for (int i = 0; i < tiles.Length; i++)
        {
            // ... and set each tile array is the correct height.
            tiles[i] = new TileType[height];
        }
    }

    public void InstantiateTiles()
    {
        Map map = gameObject.GetComponent<Map>();
        Player plr = gameObject.GetComponent<Player>();
        // Go through all the tiles in the jagged array...
        for (int i = 0; i < tiles.Length; i++)
        {
            for (int j = 0; j < tiles[i].Length; j++)
            {
                int tile = 0;
                int cellData = map.GetData(i, j);
                if (cellData == 0 || cellData == 1002)
                {
                    tile = 0;
                }
                else
                {
                    tile = 1;
                }
                if (cellData == 1001 || (map.GodMode==true && cellData == 1002))
                {
                    tile = 2;
                }
                if (map.GetExploredCell(i,j) == false )
                {
                    tile = 8;
                }
                if (plr.X == i && plr.Y == j)
                {
                    tile = 9;
                }

                tiles[i][j] = (TileType)tile;

                // If the tile type is wall...
                if (tiles[i][j] == TileType.Wall)
                {
                    // ... instantiate a wall
                    InstantiateFromArray(wallTile, i * scale, j * scale);
                }
                if (tiles[i][j] == TileType.Floor)
                {
                    // ... otherwise a floor
                    InstantiateFromArray(floorTile, i * scale, j * scale);
                }
                if (tiles[i][j] == TileType.Door)
                {
                    // ... otherwise a door
                    InstantiateFromArray(doorTile, i * scale, j * scale);
                }
                if (tiles[i][j] == TileType.Fog)
                {
                    // ... otherwise fog
                    InstantiateFromArray(fogTile, i * scale, j * scale);
                }
            }
        }
        // ...  the player
        InstantiateFromArray(playerTile, plr.X * scale, plr.Y * scale);
    }

    public void InstantiateFromArray(GameObject prefab, float xCoord, float yCoord)
    {    
        // Create a random index for the array

        // The position to be instantiated at is based on the coordinates.
        Vector3 position = new Vector3(xCoord+mapOffsetX, yCoord+mapOffsetY, 0f);

        // Create an instance of the prefab from the random index of the array.
        tileInstance = Instantiate(prefab, position, Quaternion.identity) as GameObject;
        tileInstance.transform.localScale = new Vector3(scale, scale, scale);

        // Set the tile's parent to the mini board holder.
        tileInstance.transform.parent = miniBoardHolder.transform;
    }

    public void UpdateMiniBoard()
    {
        for (int c = 0; c < miniBoardHolder.transform.childCount; c++)
        {
            Destroy(miniBoardHolder.transform.GetChild(c).gameObject);
        }
        InstantiateTiles();
    }
}