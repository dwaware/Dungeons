using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField]
    private int radius;
    private int height;                         // The number of rows on the board (how tall it will be).
    private int width;                          // The number of columns on the board (how wide it will be).
    public GameObject[] wallTiles;
    public GameObject[] floorTiles;             // An array of floor tile prefabs.
    public GameObject[] doorTiles;
    public GameObject[] hallTiles;
    public GameObject[] connectorTiles;
    public GameObject[] doorSecretTiles;
    public GameObject[] specialEndTiles;
    public GameObject[] specialMidTiles;
    public GameObject[] fogTiles;
    public GameObject[] playerTiles;
    public GameObject[] mobTiles;
    public GameObject[] edgeTiles;
    public Sprite[] playerSprites;

    [SerializeField]
    private bool radialTacFog; // leave this as true at all times for now
    [SerializeField]
    private int lowLightRadius;
    [SerializeField]
    private int torchLightRadius;
    private bool torchOn;
    private float scaleFactor = 64.0f;
    private float scale;
    private float mapOffsetX;
    private float mapOffsetY;

    private TileType[][] tiles;                 // A jagged array of tile types representing the board, like a grid.
    private GameObject boardHolder;             // GameObject that acts as a container for all other tiles.
    private TileType[][] borderTiles;
    private GameObject borderHolder;

    private GameObject tileInstance;
    private GameObject playerInstance;
    private Direction playerDirection;
    private Direction playerFacing;

    private bool[][] tacFog;

    private bool isMoving = false;

    private Map map;
    private Player plr;
    private MiniBoard miniBoard;

    public int Radius
    {
        get
        {
            return radius;
        }

        set
        {
            radius = value;
        }
    }

    public bool IsMoving
    {
        get
        {
            return isMoving;
        }

        set
        {
            isMoving = value;
        }
    }

    // Use this for initialization
    void Start()
    {
        playerSprites = Resources.LoadAll<Sprite>("playerSprites");
        //Debug.Log("Number of sprites:  "+playerSprites.Length);

        map = gameObject.GetComponent<Map>();
        plr = gameObject.GetComponent<Player>();
        miniBoard = gameObject.GetComponent<MiniBoard>();
    }

    // Update is called once per frame
    void Update()
    {
        int x = plr.X;
        int y = plr.Y;
        //Debug.Log("Player x,y:  " + x + " " + y);

        Transform targetTransform = boardHolder.transform;
        Vector3 targetPos = Vector3.zero;
        if (isMoving == false)
        {
            if ((Input.GetKey(KeyCode.A) || (Input.GetKey(KeyCode.LeftArrow))) && x > 0 && map.GetData(x - 1, y) != 0)
            {
                plr.X--;
                isMoving = true;
                playerDirection = Direction.W;
                targetPos = targetTransform.position + Vector3.right * scale;
            }
            if ((Input.GetKey(KeyCode.W) || (Input.GetKey(KeyCode.UpArrow))) && y < map.Height - 1 && map.GetData(x, y + 1) != 0)
            {
                plr.Y++;
                isMoving = true;
                playerDirection = Direction.N;
                targetPos = targetTransform.position + Vector3.down * scale;
            }
            if ((Input.GetKey(KeyCode.S) || (Input.GetKey(KeyCode.DownArrow))) && y > 0 && map.GetData(x, y - 1) != 0)
            {
                plr.Y--;
                isMoving = true;
                playerDirection = Direction.S;
                targetPos = targetTransform.position + Vector3.up * scale;
            }
            if ((Input.GetKey(KeyCode.D) || (Input.GetKey(KeyCode.RightArrow))) && x < map.Width - 1 && map.GetData(x + 1, y) != 0)
            {
                plr.X++;
                isMoving = true;
                playerDirection = Direction.E;
                targetPos = targetTransform.position + Vector3.left * scale;
            }
            if ((Input.GetKey(KeyCode.Q) || (Input.GetKey(KeyCode.Home))) && x > 0 && y < map.Height - 1 && map.GetData(x - 1, y + 1) != 0)
            {
                plr.X--;
                plr.Y++;
                isMoving = true;
                playerDirection = Direction.NW;
                targetPos = targetTransform.position + (Vector3.down + Vector3.right) * scale;
            }
            if ((Input.GetKey(KeyCode.E) || (Input.GetKey(KeyCode.PageUp))) && x < map.Width - 1 && y < map.Height && map.GetData(x + 1, y + 1) != 0)
            {
                plr.X++;
                plr.Y++;
                isMoving = true;
                playerDirection = Direction.NE;
                targetPos = targetTransform.position + (Vector3.down + Vector3.left) * scale;
            }
            if ((Input.GetKey(KeyCode.Z) || (Input.GetKey(KeyCode.End))) && x > 0 && y > 0 && map.GetData(x - 1, y - 1) != 0)
            {
                plr.X--;
                plr.Y--;
                isMoving = true;
                playerDirection = Direction.SW;
                targetPos = targetTransform.position + (Vector3.up + Vector3.right) * scale;
            }
            if ((Input.GetKey(KeyCode.C) || (Input.GetKey(KeyCode.PageDown))) && x < map.Width - 1 && y > 0 && map.GetData(x + 1, y - 1) != 0)
            {
                plr.X++;
                plr.Y--;
                isMoving = true;
                playerDirection = Direction.SE;
                targetPos = targetTransform.position + (Vector3.up + Vector3.left) * scale;
            }

            if (isMoving)
            {
                StartCoroutine(TranslateBoard(targetPos));

                //Debug.Log("MOVED TO X,Y:  " + x +" "+ y);
                if (map.GetData(plr.X, plr.Y) == 1002)
                {
                    map.SetData(plr.X, plr.Y, 1001);
                }

                //ResetBoard();
                UpdatePlayerMap();
                miniBoard.UpdateMiniBoard();
            }
        }
    }

    public void UpdatePlayerMap()
    {
        Map map = gameObject.GetComponent<Map>();
        Player plr = gameObject.GetComponent<Player>();

        int x = plr.X;
        int y = plr.Y;

        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                map.SetExploredCell(x + i, y + j, true);
            }
        }
        int pRgn = map.GetData(x, y);
        if (pRgn > 0 && pRgn < 1000)
        {
            for (int rx = (int)map.Rooms[pRgn - 1].Rect.x - 1; rx < (int)map.Rooms[pRgn - 1].Rect.x + (int)map.Rooms[pRgn - 1].Rect.width + 1; rx++)
            {
                for (int ry = (int)map.Rooms[pRgn - 1].Rect.y - 1; ry < (int)map.Rooms[pRgn - 1].Rect.y + (int)map.Rooms[pRgn - 1].Rect.height + 1; ry++)
                {
                    map.SetExploredCell(rx, ry, true);
                }
            }
        }
    }

    public void SetupScene()
    {
        height = radius * 2 + 1;
        width = radius * 2 + 1;

        scale = Screen.height / (scaleFactor * height);
        mapOffsetX = -height * scale / 2 + 0.5f * scale;
        mapOffsetY = mapOffsetX;

        // Create the board holder.
        boardHolder = new GameObject("BoardHolder");
        borderHolder = new GameObject("BorderHolder");

        SetupTilesArray();
        CreateBorder();
        InstantiateTiles(); 

        SetTorch(false);
    }

    public void NewMap()
    {
        Destroy(boardHolder);
        boardHolder = new GameObject("BoardHolder");
        SetupTilesArray();
        InstantiateTiles();
    }

    public void CreateBorder() {
        // Set the tiles jagged array to the correct width.
        borderTiles = new TileType[width+2][];

        // Go through all the tile arrays...
        for (int i = 0; i < borderTiles.Length; i++)
        {
            // ... and set each tile array is the correct height.
            borderTiles[i] = new TileType[height+2];
        }
        for (int i = 0; i < borderTiles.Length; i++)
        {
            for (int j = 0; j < borderTiles[i].Length; j++)
            {
                {
                    if (i <= 1 || i >= 2 * radius + 1 || j <= 1 || j >= 2 * radius + 1)
                    {
                        // ... otherwise a special mid hall tile
                        float xCoord = (i - 1) * scale;
                        float yCoord = (j - 1) * scale;

                        // The position to be instantiated at is based on the coordinates.
                        Vector3 position = new Vector3(xCoord + mapOffsetX, yCoord + mapOffsetY, 0f);

                        tileInstance = Instantiate(edgeTiles[0], position, Quaternion.identity) as GameObject;
                        tileInstance.transform.localScale = new Vector3(scale, scale, scale);

                        // Set the tile's parent to the border holder.
                        tileInstance.transform.parent = borderHolder.transform;
                    }
                    if (i == radius && j == radius)
                    {
                        float xCoord = i * scale;
                        float yCoord = j * scale;

                        // The position to be instantiated at is based on the coordinates.
                        Vector3 position = new Vector3(xCoord + mapOffsetX, yCoord + mapOffsetY, 0f);

                        playerInstance = Instantiate(playerTiles[0], position, Quaternion.identity) as GameObject;
                        playerInstance.transform.localScale = new Vector3(scale, scale, scale);

                        playerInstance.GetComponent<SpriteRenderer>().sprite = playerSprites[0];
                        //Debug.Log("Number of sprites, later...:  " + playerSprites.Length);

                        // Set the tile's parent to the border holder.
                        playerInstance.transform.parent = borderHolder.transform;
                    }
                }
            }
        }
    }

    public void SetupTilesArray()
    {
        // Set the tiles jagged array to the correct width.
        tiles = new TileType[width][];
        tacFog = new bool[width][];

        // Go through all the tile arrays...
        for (int i = 0; i < tiles.Length; i++)
        {
            // ... and set each tile array is the correct height.
            tiles[i] = new TileType[height];
            tacFog[i] = new bool[height];
        }
    }

    public void InstantiateTiles()
    {
        Map map = gameObject.GetComponent<Map>();
        Player plr = gameObject.GetComponent<Player>();
        //Debug.Log("Player is at x,y:  " + plr.X + " " + plr.Y);

        DetermineTacFog();

        // Go through all the tiles in the jagged array...
        for (int i = 0; i < tiles.Length; i++)
        {
            for (int j = 0; j < tiles[i].Length; j++)
            {
                int randomTile = 0;
                int gridX = i + plr.X - radius;
                int gridY = j + plr.Y - radius;
                int cellData = 0;
                if (gridX >= 0 && gridX < map.Width && gridY >= 0 && gridY < map.Height)
                {
                    cellData = map.GetData(gridX, gridY);
                }
                if (cellData < 0)
                {
                    randomTile = 3;
                }
                else
                {
                    if (cellData > 0 && cellData < 1000)
                    {
                        randomTile = 1;
                    }
                    if (cellData == 1001)
                    {
                        randomTile = 2;
                    }
                    if (cellData == 1002)
                    {
                        randomTile = 0;
                    }
                    if (cellData == 9998)
                    {
                        randomTile = 6;
                    }
                    if (cellData == 9999)
                    {
                        randomTile = 7;
                    }
                }
                if (tacFog[i][j])
                {
                    randomTile = 8;
                }
                tiles[i][j] = (TileType)randomTile;

                // If the tile type is wall...
                if (tiles[i][j] == TileType.Wall)
                {
                    // ... instantiate a wall
                    InstantiateFromArray(wallTiles, 0, i, j);
                }
                if (tiles[i][j] == TileType.Floor)
                {
                    // ... otherwise a floor
                    InstantiateFromArray(floorTiles, 0, i, j);
                }
                if (tiles[i][j] == TileType.Door)
                {
                    // ... otherwise a door
                    InstantiateFromArray(doorTiles, map.GetConnOrientByPosition(gridX, gridY), i, j);
                }
                if (tiles[i][j] == TileType.Hall)
                {
                    // ... otherwise a hall
                    InstantiateFromArray(hallTiles, 0, i, j);
                }
                if (tiles[i][j] == TileType.Connector)
                {
                    // ... otherwise a hall
                    InstantiateFromArray(connectorTiles, 0, i, j);
                }
                if (tiles[i][j] == TileType.DoorSecret)
                {
                    // ... otherwise a secret door
                    InstantiateFromArray(doorSecretTiles, 0, i, j);
                }
                if (tiles[i][j] == TileType.SpecialEnd)
                {
                    // ... otherwise a special end hall tile
                    InstantiateFromArray(specialEndTiles, 0, i, j);
                }
                if (tiles[i][j] == TileType.SpecialMid)
                {
                    // ... otherwise a special mid hall tile
                    InstantiateFromArray(specialMidTiles, 0, i, j);
                }
                if (tiles[i][j] == TileType.Fog)
                {
                    // ... otherwise a special mid hall tile
                    InstantiateFromArray(fogTiles, 0, i, j);
                }
            }
        }
    }

    public void InstantiateFromArray(GameObject[] prefabs, int pIndex, int i, int j)
    {
        // Create a random index for the array
        //int randomIndex = Random.Range(0, prefabs.Length);
        int preferredIndex = pIndex;

        /* eventually allow for multiple variants
        if (prefabs.Length > 1)
        {
            randomIndex = (modOffsetX + modOffsetY) % prefabs.Length;
            //CHECK MAP BOUNDS (ESP LOWER LEFT) BEFORE ADDING THIS BACK...
        }
        */

        float xCoord = i * scale;
        float yCoord = j * scale;

        // The position to be instantiated at is based on the coordinates.
        Vector3 position = new Vector3(xCoord + mapOffsetX, yCoord + mapOffsetY, 0f);

        // Create an instance of the prefab from the random index of the array.

        tileInstance = Instantiate(prefabs[preferredIndex], position, Quaternion.identity) as GameObject;
        tileInstance.transform.localScale = new Vector3(scale, scale, scale);

        // Set the tile's parent to the board holder.
        tileInstance.transform.parent = boardHolder.transform;
    }

    public IEnumerator TranslateBoard(Vector3 targetPos)
    {
        float startTime = Time.time;
        int iteration = 1;
        int spriteIndex = 1;
        int iterMultiplier = 1;
        while (Vector3.Distance(boardHolder.transform.position, targetPos) > 0.1*scale)
        {
            iterMultiplier = 1;
            if (Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)) { iterMultiplier = 3; }
            boardHolder.transform.position = Vector3.Lerp(boardHolder.transform.position, targetPos, 1.0f * Time.deltaTime*(iteration*iterMultiplier));
            iteration++;
            spriteIndex++;
            if (spriteIndex > 5) { spriteIndex = 0 ; }
            //Debug.Log("######################################");
            //Debug.Log("Direction:  "+(int)playerDirection);
            //Debug.Log("Index:  "+spriteIndex);
            int finalIndex = 6 * (int)playerDirection + spriteIndex;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || iteration %5==0)
            {
                playerInstance.GetComponent<SpriteRenderer>().sprite = playerSprites[finalIndex];
                //Debug.Log(finalIndex);
            }
            float yieldSeconds = 0.015f;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) { yieldSeconds = 0.01f; }
            yield return new WaitForSeconds(yieldSeconds);
        }
        boardHolder.transform.position = targetPos;

        //float deltaTime = Time.time - startTime;
        //Debug.Log(deltaTime+"Done moving.");
        //Debug.Log("Iteration"+iteration);
        ResetBoard();
        isMoving = false;
    }

public void ResetBoard()
    {
        for (int c = 0; c < boardHolder.transform.childCount; c++)
        {
            Destroy(boardHolder.transform.GetChild(c).gameObject);
        }
        InstantiateTiles();
    }

    public void DetermineTacFog()
    {
        Map map = GetComponent<Map>();
        Player plr = GetComponent<Player>();

        int currLightRadius = torchOn ? torchLightRadius : lowLightRadius;

        for (int i = 0; i < radius*2+1; i++)
        {
            for (int j = 0; j < radius*2+1; j++)
            {
                tacFog[i][j] = true;
            }
        }

        bool playerInRoom = false;
        int mapData = map.GetData(plr.X, plr.Y);
        //Debug.Log("MAP AT:  " + plr.X + " " + plr.Y + " IS:  " + mapData);
        if (mapData > 0 && mapData < 1000)
        {
            playerInRoom = true;
        }
        if (playerInRoom)
        {
            Rect r = map.Rooms[mapData - 1].Rect;
            int fX = (int)r.x;
            int fY = (int)r.y;
            int fW = (int)r.width;
            int fH = (int)r.height;
            //Debug.Log(plr.X + " " + plr.Y + " " + fX + " " + fY + " " + fW + " " + fH);
            int i = fX - plr.X + radius;
            int j = fY - plr.Y + radius;
            //Debug.Log(i + " " + j);
            for (int x = i - 1; x < i + fW + 1; x++)
            {
                for (int y = j - 1; y < j + fH + 1; y++)
                {
                    if (x >= 0 && x < 2 * radius + 1 && y >= 0 && y < 2 * radius + 1)
                    tacFog[x][y] = false;
                    //Debug.Log(x + " " + y);
                }
            }
        }
        else
        {
            int bound;

            bound = 1;
            while (plr.Y + bound < map.Height && map.GetData(plr.X, plr.Y + bound) < 0 && bound < radius && bound < currLightRadius)
            {
                bound++;
            }
            int nBound = radius + bound;

            bound = 1;
            while (plr.Y - bound > 0 && map.GetData(plr.X, plr.Y - bound) < 0 && bound < radius && bound < currLightRadius)
            {
                bound++;
            }
            int sBound = radius - bound;

            bound = 1;
            while (plr.X + bound < map.Width && map.GetData(plr.X + bound, plr.Y) < 0 && bound < radius && bound < currLightRadius)
            {
                bound++;
            }
            int eBound = radius + bound;

            bound = 1;
            while (plr.X - bound > 0 && map.GetData(plr.X - bound, plr.Y) < 0 && bound < radius && bound < currLightRadius)
            {
                bound++;
            }
            int wBound = radius - bound;



            for (int x = wBound; x <= eBound; x++)
            {
                for (int y = sBound; y <= nBound; y++)
                {
                    tacFog[x][y] = false;
                    if (Mathf.Abs(radius - x) > 1 && Mathf.Abs(radius - y) > 1)
                    {
                        tacFog[x][y] = true;
                    }
                }
            }
        }
        if (radialTacFog)
        {
            for (int x = 0; x < radius * 2 + 1; x++)
            {
                for (int y = 0; y < radius * 2 + 1; y++)
                {
                    //"square circle" fog
                    //if (x < radius - currLightRadius || x > radius + currLightRadius ||  y < radius - currLightRadius || y > radius + currLightRadius)

                    //"more circular looking" fog
                    float distance = Mathf.Sqrt((radius-x) * (radius-x) + (radius-y) * (radius-y));
                    if (distance + 0.5f > currLightRadius)
                    {
                        tacFog[x][y] = true;
                    }
                }
            }
        }
    }

    public void ToggleTorch ()
    {
        if (torchOn)
        {
            torchOn = false;
            SetTorch(torchOn);
        }
        else
        {
            torchOn = true;
            SetTorch(torchOn);
        }
        ResetBoard();
    }

    public bool GetTorch ()
    {
        return torchOn;
    }

    public void SetTorch ( bool t)
    {
        torchOn = t;
    }
}