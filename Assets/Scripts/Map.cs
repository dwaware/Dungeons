using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

// The type of tile that will be laid in a specific position.
public enum TileType
{
    Wall = 0, Floor, Door, Hall, Connector, DoorSecret, SpecialEnd, SpecialMid, Fog, Player, Mob, Edge
}

// Directions
public enum Direction
{
    S = 0, SE, E, NE, N, NW, W, SW
}

public class Map : MonoBehaviour
{
    [SerializeField]
    private int width;                                     // The number of rows on the map (how tall it will be).                                 
    [SerializeField]
    private int height;                                    // The number of columns on the map (how wide it will be.)
    [SerializeField]
    private int maxRoomsPerLevel;
    [SerializeField]
    private int maxRoomAttempts;
    [SerializeField]
    private float percentSecretDoors;
    [SerializeField]
    private bool godMode;
    [SerializeField]
    private bool verboseRoomStats;
    [SerializeField]
    private bool verboseHallStats;
    [SerializeField]
    private bool verboseConnStats;

    private int seed;
    private int[][] cells;
    private bool[][] explored;

    // odd only
    [SerializeField]
    private int maxRoomWidth;
    [SerializeField]
    private int minRoomWidth;
    [SerializeField]
    private int maxRoomHeight;
    [SerializeField]
    private int minRoomHeight;
    [SerializeField]
    private int minRoomGap;
    [SerializeField]
    private int mapBorder;
    private int ci = 0;
    private int cj = 0;
    private int cid = 0;

    //private GameObject map;

    private List<Room> rooms = new List<Room>();
    private List<Hall> halls = new List<Hall>();
    private List<Connector> connectors = new List<Connector>();

    public IList<Room> Rooms
    {
        get { return rooms.AsReadOnly(); }
    }

    public IList<Hall> Halls
    {
        get { return halls.AsReadOnly(); }
    }

    public IList<Connector> Connectors
    {
        get { return connectors.AsReadOnly(); }
    }

    public int Width
    {
        get
        {
            return width;
        }

        set
        {
            width = value;
        }
    }

    public int Height
    {
        get
        {
            return height;
        }

        set
        {
            height = value;
        }
    }

    public int Seed
    {
        get
        {
            return seed;
        }

        set
        {
            seed = value;
        }
    }

    public bool GodMode
    {
        get
        {
            return godMode;
        }

        set
        {
            godMode = value;
        }
    }

    void Start()
    {
        //map = new GameObject("Map");
    }

    void Update()
    {

    }

    public int[][] GetCells()
    {
        return cells;
    }

    public void SetCells(int[][] c)
    {
        cells = c;
    }

    public bool[][] GetExplored ()
    {
        return explored;
    }

    public void SetExplored(bool[][] e)
    {
        explored = e;
    }

    public int GetData(int i, int j)
    {
        return cells[i][j];
    }

    public int SetData(int i, int j, int val)
    {
        return cells[i][j] = val;
    }

    public bool GetExploredCell(int i, int j)
    {
        if (i >= 0 && i < width && j >= 0 && j < height)
        {
            return explored[i][j];
        }
        else
        {
            return false;
        }
    }

    public void SetExploredCell(int i, int j, bool isExplored)
    {
        if (i >= 0 && i < width && j >= 0 && j < height)
        {
            explored[i][j] = isExplored;
        }
        else
        {
            Debug.Log("Explored out of bounds at x,y:  " + i + " " + j);
        }
    }

    public void GetNewSeed()
    {
        Seed = Random.Range(0, 999999);
        //Debug.Log("Map returns seed as:  " + seed);
        UpdateSeedText();
    }

    public void UpdateSeedText()
    {
        Text[] textGOs = GameObject.FindObjectOfType<Canvas>().GetComponentsInChildren<Text>();
        foreach (Text go in textGOs)
        {
            if (go.name == "Text_Seed")
            {
                go.GetComponent<Text>().text = seed.ToString();
            }
        }
    }

    public void InitMap()
    {
        Random.seed = Seed;

        // Set the tiles jagged array to the correct width.
        cells = new int[width][];
        explored = new bool[width][];

        // Go through all the tile arrays...
        for (int i = 0; i < cells.Length; i++)
        {
            // ... and set each tile array is the correct height.
            cells[i] = new int[height];
            explored[i] = new bool[height];
        }

        // Go through all the tiles in the jagged array...
        for (int i = 0; i < cells.Length; i++)
        {
            for (int j = 0; j < cells[i].Length; j++)
            {
                cells[i][j] = 0;
                explored[i][j] = godMode;
            }
        }

        //Debug.Log("ADDING ROOMS");
        AddRooms();       
        //Debug.Log("ADDING HALLS");
        AddHalls();
        //Debug.Log("ADDING CONNECTORS");
        AddConnectors();
        if (connectors.Count > 0)
        {
            PromoteConnectorsToDoors();
            PromoteConnectorsToSecretDoors(percentSecretDoors);
            SetOrientationAndRemoveUnusedConnectors();
        }
        ClearHallCrumbsAndSetHallPaths();
        if (verboseRoomStats) { PrintRoomStats(); }
        if (verboseHallStats) { PrintHallStats(); }
        if (verboseConnStats) { PrintConnStats(); }
    }

    public void AddRooms()
    {
        rooms.Clear();
        int r = 0;
        int randomW, randomH, randomX, randomY;
        while (r < maxRoomAttempts)
        {
            randomW = Random.Range(minRoomWidth, maxRoomWidth);
            if (randomW % 2 == 0) { randomW++; }
            randomH = Random.Range(minRoomHeight, maxRoomHeight);
            if (randomH % 2 == 0) { randomH++; }
            randomX = Random.Range(mapBorder, width - randomW - mapBorder);
            if (randomX % 2 == 0) { randomX++; }
            randomY = Random.Range(mapBorder, height - randomH - mapBorder);
            if (randomY % 2 == 0) { randomY++; }

            //Debug.Log("BEGIN VALIDATION");

            if (rooms.Count < maxRoomsPerLevel && ValidateNewRoom(randomX, randomY, randomW, randomH) == true)
            {
                rooms.Add(new Room(rooms.Count + 1, new Rect(randomX, randomY, randomW, randomH)));
                //Debug.Log("Added a room.  Number of rooms now: " + rooms.Count);
                //Debug.Log("X:" + randomX + " Y:" + randomY + " W:" + randomW + " H:" + randomH + " with id:" + rooms.Count);
                for (int i = randomX; i < randomX + randomW; i++)
                {
                    for (int j = randomY; j < randomY + randomH; j++)
                    {
                        cells[i][j] = rooms.Count;
                        //Debug.Log("Room cell @ " + i + " " + j + " labeled as: " + -rooms.Count);
                    }
                }
            }
            r++;
        }
        Debug.Log("Room Count:  " + rooms.Count);
    }

    public bool ValidateNewRoom(int x, int y, int w, int h)
    {
        //Debug.Log("VALIDATE x, y, w, h: " + x + " " + y + " " + w + " " + h);
        bool newRoomIsValid = true;

        Rect rRect = new Rect(x-minRoomGap-1, y-minRoomGap-1, w+2*minRoomGap+1, h+2*minRoomGap+1);

        for (int r = 0; r < rooms.Count; r++)
        {
            //Debug.Log("new room:  "+rRect.x + " " + rRect.y + " " + rRect.width + " " + rRect.height);
            //Debug.Log("from arr:  "+rooms[r].Rect.x + " " + rooms[r].Rect.y + " " + rooms[r].Rect.width + " " + rooms[r].Rect.height);
            //Debug.Log("Overlaps?  "+rRect.Overlaps(rooms[r].Rect));
            if (rRect.Overlaps(rooms[r].Rect))
            {
                newRoomIsValid = false;
            }
        }

        return newRoomIsValid;
    }

    public void AddHalls()
    {
        halls.Clear();
        while (SeedHall() == true)
        {
            CarveHall();
            cid = halls.Count;
            while (halls[cid - 1].Crumbs.Count > 0)
            {
                ReverseCarveHall();
            }
        }
    }

    public bool SeedHall()
    {
        bool foundSeed = false;
        for (int i = mapBorder; i < width - mapBorder; i += 2)
        {
            for (int j = mapBorder; j < height - mapBorder; j += 2)
            {
                if (cells[i][j] == 0 && foundSeed == false)
                {
                    cid = halls.Count + 1;
                    halls.Add(new Hall(cid));
                    cells[i][j] = -cid;
                    ci = i;
                    cj = j;
                    foundSeed = true;
                }
            }
        }
        return foundSeed;
    }

    public void CarveHall()
    {
        bool keepCarving = true;
        while (keepCarving == true)
        {
            int i = ci;
            int j = cj;
            List<char> validDirs = new List<char>();
            int n = j - 2;
            int s = j + 2;
            int w = i - 2;
            int e = i + 2;
            if (n >= mapBorder && n < height - mapBorder && cells[i][n] == 0) { validDirs.Add((char)('n')); }
            if (s >= mapBorder && s < height - mapBorder && cells[i][s] == 0) { validDirs.Add((char)('s')); }
            if (w >= mapBorder && w < width - mapBorder && cells[w][j] == 0) { validDirs.Add((char)('w')); }
            if (e >= mapBorder && e < width - mapBorder && cells[e][j] == 0) { validDirs.Add((char)('e')); }
            if (validDirs.Count == 0)
            {
                keepCarving = false;
                cid = halls.Count;
                halls[cid - 1].Crumbs.Add(new Vector2(ci, cj));
                halls[cid - 1].Paths.Add(new Vector2(ci, cj));
            }
            else
            {
                char dir = validDirs[Random.Range(0, validDirs.Count)];
                cid = halls.Count;
                halls[cid - 1].Crumbs.Add(new Vector2(ci, cj));
                halls[cid - 1].Paths.Add(new Vector2(ci, cj));
                cid = halls.Count;
                if (dir.Equals('n'))
                {
                    cells[i][j - 1] = -cid;
                    cells[i][j - 2] = -cid;
                    ci = i;
                    cj = j - 2;
                }
                if (dir.Equals('s'))
                {
                    cells[i][j + 1] = -cid;
                    cells[i][j + 2] = -cid;
                    ci = i;
                    cj = j + 2;
                }
                if (dir.Equals('w'))
                {
                    cells[i - 1][j] = -cid;
                    cells[i - 2][j] = -cid;
                    ci = i - 2;
                    cj = j;
                }
                if (dir.Equals('e'))
                {
                    cells[i + 1][j] = -cid;
                    cells[i + 2][j] = -cid;
                    ci = i + 2;
                    cj = j;
                }
            }
        }
        cid = halls.Count;
        halls[cid - 1].Crumbs.RemoveAt(halls[cid - 1].Crumbs.Count - 1);
        int lc = halls[cid - 1].Crumbs.Count;
        if (lc > 0)
        {
            Vector2 lcv = halls[cid - 1].Crumbs[lc - 1];
            ci = (int)lcv.x;
            cj = (int)lcv.y;
        }
    }

    public void ReverseCarveHall()
    {
        bool keepCarving = true;
        while (keepCarving == true)
        {
            int i = ci;
            int j = cj;
            List<char> validDirs = new List<char>();
            int n = j - 2;
            int s = j + 2;
            int w = i - 2;
            int e = i + 2;
            if (n >= mapBorder && n < height - mapBorder && cells[i][n] == 0) { validDirs.Add((char)('n')); }
            if (s >= mapBorder && s < height - mapBorder && cells[i][s] == 0) { validDirs.Add((char)('s')); }
            if (w >= mapBorder && w < width - mapBorder && cells[w][j] == 0) { validDirs.Add((char)('w')); }
            if (e >= mapBorder && e < width - mapBorder && cells[e][j] == 0) { validDirs.Add((char)('e')); }
            if (validDirs.Count == 0)
            {
                keepCarving = false;
            }
            else
            {
                char dir = validDirs[Random.Range(0, validDirs.Count)];
                cid = halls.Count;
                halls[cid - 1].Crumbs.Add(new Vector2(ci, cj));

                cid = halls.Count;
                if (dir.Equals('n'))
                {
                    cells[i][j - 1] = -cid;
                    cells[i][j - 2] = -cid;
                    ci = i;
                    cj = j - 2;
                    halls[cid - 1].Paths.Add(new Vector2(ci, cj));
                }
                if (dir.Equals('s'))
                {
                    cells[i][j + 1] = -cid;
                    cells[i][j + 2] = -cid;
                    ci = i;
                    cj = j + 2;
                    halls[cid - 1].Paths.Add(new Vector2(ci, cj));
                }
                if (dir.Equals('w'))
                {
                    cells[i - 1][j] = -cid;
                    cells[i - 2][j] = -cid;
                    ci = i - 2;
                    cj = j;
                    halls[cid - 1].Paths.Add(new Vector2(ci, cj));
                }
                if (dir.Equals('e'))
                {
                    cells[i + 1][j] = -cid;
                    cells[i + 2][j] = -cid;
                    ci = i + 2;
                    cj = j;
                    halls[cid - 1].Paths.Add(new Vector2(ci, cj));
                }
            }
        }
        cid = halls.Count;
        halls[cid - 1].Crumbs.RemoveAt(halls[cid - 1].Crumbs.Count - 1);
        int lc = halls[cid - 1].Crumbs.Count;
        if (lc > 0)
        {
            Vector2 lcv = halls[cid - 1].Crumbs[lc - 1];
            ci = (int)lcv.x;
            cj = (int)lcv.y;
        }
    }

    public void AddConnectors()
    {
        connectors.Clear();
        for (int i = mapBorder; i < width - mapBorder; i++)
        {
            for (int j = mapBorder; j < height - mapBorder; j++)
            {
                for (int k = -1; k < 2; k += 2)
                {
                    if (cells[i - 1][j] != cells[i + 1][j] &&
                        cells[i][j] == 0 &&
                        cells[i - 1][j] != 0 &&
                        cells[i + 1][j] != 0 &&
                        cells[i - 1][j] != 1000 &&
                        cells[i + 1][j] != 1000)
                    {
                        cells[i][j] = 1000;
                        connectors.Add(new Connector(connectors.Count + 1, i, j, cells[i - 1][j], cells[i + 1][j], 0, 0));
                        //Debug.Log(connectors[connectors.Count - 1].reg1);
                        //Debug.Log(connectors[connectors.Count - 1].reg2);
                    }
                    if (cells[i][j - 1] != cells[i][j + 1] &&
                        cells[i][j] == 0 &&
                        cells[i][j - 1] != 0 &&
                        cells[i][j + 1] != 0 &&
                        cells[i][j - 1] != 1000 &&
                        cells[i][j + 1] != 1000)
                    {
                        cells[i][j] = 1000;
                        connectors.Add(new Connector(connectors.Count + 1, i, j, cells[i][j + 1], cells[i][j - 1], 0, 0));
                        //Debug.Log(connectors[connectors.Count - 1].reg1);
                        //Debug.Log(connectors[connectors.Count - 1].reg2);
                    }
                }
            }
        }
        //Debug.Log("Connectors:  " + connectors.Count);
    }

    public void PromoteConnectorsToDoors()
    {
        List<int> unConnectedRegions = new List<int>();
        List<int> connectedRegions = new List<int>();
        List<Connector> possibleConnectors = new List<Connector>();
        //Debug.Log("Number of Rooms:  " + rooms.Count);
        //Debug.Log("Number of Halls:  " + halls.Count);
        for (int ur = 0; ur < rooms.Count; ur++)
        {
            unConnectedRegions.Add(rooms[ur].Id);
            //Debug.Log("Room id:  " + rooms[ur].id);
        }
        for (int uh = 0; uh < halls.Count; uh++)
        {
            unConnectedRegions.Add(-rooms[uh].Id);
            //Debug.Log("Hall id  " + -halls[uh].id);
        }
        for (int uc = 0; uc < connectors.Count; uc++)
        {
            //Debug.Log("Connector:  " + connectors[uc].id + "    " + connectors[uc].reg1 + " " + connectors[uc].reg2);
        }
        //Debug.Log("Unconnected Regions:  " + unConnectedRegions.Count);
        //Debug.Log("Connected Regions:  " + connectedRegions.Count);
        int randomConnectorID = Random.Range(0, connectors.Count);
        Connector randomConnector = connectors[randomConnectorID];
        unConnectedRegions.Remove(randomConnector.Reg1);
        unConnectedRegions.Remove(randomConnector.Reg2);
        connectedRegions.Add(randomConnector.Reg1);
        connectedRegions.Add(randomConnector.Reg2);
        connectors[randomConnectorID].Type = 1;
        cells[randomConnector.X][randomConnector.Y] = 1001;
        //Debug.Log("Unconnected Regions:  " + unConnectedRegions.Count);
        //Debug.Log("Connected Regions:  " + connectedRegions.Count);
        //Debug.Log("Connected Region #1:  " + connectedRegions[0]);
        //Debug.Log("Connected Region #2:  " + connectedRegions[1]);

        while (unConnectedRegions.Count > 0)
        {
            possibleConnectors.Clear();
            int selectedRegion = unConnectedRegions[Random.Range(0, unConnectedRegions.Count)];
            //Debug.Log("Selected Region:  " + selectedRegion);
            for (int oc = 0; oc < connectors.Count; oc++)
            {
                if (
                   (connectors[oc].Reg1 == selectedRegion) && (connectedRegions.Contains(connectors[oc].Reg2)) ||
                   (connectors[oc].Reg2 == selectedRegion) && (connectedRegions.Contains(connectors[oc].Reg1))
                   )
                {
                    possibleConnectors.Add(connectors[oc]);
                }
            }
            for (int pc = 0; pc < possibleConnectors.Count; pc++)
            {
                //Debug.Log("Region Pair:     " + possibleConnectors[pc].reg1 + " " + possibleConnectors[pc].reg2);
            }
            int spcID = Random.Range(0, possibleConnectors.Count);
            if (possibleConnectors.Count != 0)
            {
                Connector spc = possibleConnectors[spcID];
                //Debug.Log("Selected connector xy12:  " + spc.x + " " + spc.y + " " + spc.reg1 + " " + spc.reg2);
                connectors[spc.Id - 1].Type = 1;
                cells[spc.X][spc.Y] = 1001;
                //Debug.Log("Unconnected Regions:  " + unConnectedRegions.Count);
                //Debug.Log("Connected Regions:  " + connectedRegions.Count);
                //Debug.Log("Connected Region #1:  " + connectedRegions[0]);
                //Debug.Log("Connected Region #2:  " + connectedRegions[1]);
                unConnectedRegions.Remove(selectedRegion);
                connectedRegions.Add(selectedRegion);
                //Debug.Log("Unconnected Regions:  " + unConnectedRegions.Count);
                //Debug.Log("Connected Regions:  " + connectedRegions.Count);
                //for (int oc = 0; oc < connectors.Count; oc++)
                //{
                //    if (connectors[oc].type == 1) { Debug.Log("Door at:  " + connectors[oc].x + " " + connectors[oc].y); }
                //}
            }
        }
    }

    public void PromoteConnectorsToSecretDoors(float psd)
    {
        for (int r = 0; r < connectors.Count; r++)
        {
            float chance = Random.value;
            if (connectors[r].Type == 0 &&
                chance < psd &&
                cells[connectors[r].X + 1][connectors[r].Y] < 1001 &&
                cells[connectors[r].X - 1][connectors[r].Y] < 1001 &&
                cells[connectors[r].X][connectors[r].Y + 1] < 1001 &&
                cells[connectors[r].X][connectors[r].Y - 1] < 1001)
            {
                {
                    connectors[r].Type = 2;
                    cells[connectors[r].X][connectors[r].Y] = 1002;
                }
            }
        }
    }

    public void SetOrientationAndRemoveUnusedConnectors()
    {
        for (int c = 0; c < connectors.Count; c++)
        {
            Connector pc = connectors[c];
            if (pc.Type != 0)
            {
                SetConnectorOrientation(c);
            }
            else 
            {
                //Debug.Log("Connector number:  " + c);
                pc.Type = -1;
                cells[pc.X][pc.Y] = 0;
                pc.Orient = 0;
                //Debug.Log("Invalid connector:  " + c + "    " + pc.type + "    " + pc.x + " " + pc.y);
            }
        }
    }

    public void ClearHallCrumbsAndSetHallPaths()
    {
        for (int h = 0; h < halls.Count; h++)
        {
            //Debug.Log("###########   SCANNING FOR HALL INDEX " + h + "   #######################################");
            halls[h].Crumbs.Clear();
            halls[h].Paths.Clear();
            for (int i = mapBorder; i < width - mapBorder; i++)
            {
                for (int j = mapBorder; j < height - mapBorder; j++)
                {
                    if (cells[i][j] < 0)
                    {
                        if (!halls[-cells[i][j] - 1].Paths.Contains(new Vector2(i, j)))
                        {
                            halls[-cells[i][j] - 1].Paths.Add(new Vector2(i, j));
                        }
                    }
                }
            }
            //Debug.Log("PATH LENGTH FOR HALL INDEX:  " + h + ":   " + halls[h].paths.Count);
        }
    }

    public void SetConnectorOrientation(int index)
    {
        int orientation = -1;

        int x = connectors[index].X;
        int y = connectors[index].Y;

        int n = y + 1;
        int e = x + 1;
        int s = y - 1;
        int w = x - 1;

        int nVal = cells[x][n];
        int eVal = cells[e][y];
        int sVal = cells[x][s];
        int wVal = cells[w][y];

        if (nVal > 0 && sVal < 0) { orientation = 0; }
        if (eVal > 0 && wVal < 0) { orientation = 1; }
        if (sVal > 0 && nVal < 0) { orientation = 2; }
        if (wVal > 0 && eVal < 0) { orientation = 3; }
        if (wVal > 0 && eVal > 0) { orientation = 4; }
        if (nVal > 0 && sVal > 0) { orientation = 5; }
        if (wVal < 0 && eVal < 0) { orientation = 6; }
        if (nVal < 0 && sVal < 0) { orientation = 7; }

        if (orientation == -1 ) {
            Debug.Log("BAD ORIENTATION at " + x + " " + y);
            orientation = 8;
        }
        connectors[index].Orient = orientation;
    }

    public int GetConnOrientByPosition(int i, int j)
    {
        int orientIndex = 0;

        for (int c = 0; c < connectors.Count; c++)
        {
            if (i == connectors[c].X && j == connectors[c].Y)
            {
                orientIndex = connectors[c].Orient;
            }
        }

        return orientIndex;
    }

    void PrintRoomStats()
    {
        Debug.Log("ROOMS");
        Debug.Log("Number of Rooms:  " + rooms.Count);
        for (int r = 0; r < rooms.Count; r++)
        {
            Debug.Log("Room index " + r + " ID " + rooms[r].Id + " x " + rooms[r].Rect.x + "  y " + rooms[r].Rect.y + " w " + rooms[r].Rect.width + " h " + rooms[r].Rect.height);

        }
    }

    void PrintHallStats()
    {
        Debug.Log("HALLS");
        Debug.Log("Number of Halls:  " + halls.Count);
        for (int h = 0; h < halls.Count; h++) {
            Debug.Log("Hall " + h + " count:  " + halls[h].Paths.Count);
            for (int p = 0; p < halls[h].Paths.Count; p++) {
                Debug.Log("Hall index" + h + " ID " + halls[h].Id + " path " + p + "    " + halls[h].Paths[p].x + " " + halls[h].Paths[p].y);
            }
        }
    }

    void PrintConnStats()
    {
        Map map = GetComponent<Map>();
        Debug.Log("CONNECTORS");
        Debug.Log("Number of Connectors:  " + connectors.Count);
        for (int c = 0; c < connectors.Count; c++)
        {
            if (connectors[c].Type != -1) {
                Debug.Log("Connector index " + c + " ID " + connectors[c].Id + " x " + connectors[c].X + "  y " + connectors[c].Y + " type " + connectors[c].Type + " reg1 " + connectors[c].Reg1 + " reg2 " + connectors[c].Reg2 + " orient " + connectors[c].Orient);
                Debug.Log(map.GetData(connectors[c].X-1, connectors[c].Y+1) + " " + map.GetData(connectors[c].X, connectors[c].Y+1) + " " + map.GetData(connectors[c].X+1, connectors[c].Y+1));
                Debug.Log(map.GetData(connectors[c].X-1, connectors[c].Y  ) + " " + map.GetData(connectors[c].X, connectors[c].Y  ) + " " + map.GetData(connectors[c].X+1, connectors[c].Y  ));
                Debug.Log(map.GetData(connectors[c].X-1, connectors[c].Y-1) + " " + map.GetData(connectors[c].X, connectors[c].Y-1) + " " + map.GetData(connectors[c].X+1, connectors[c].Y-1));
            }
        }
    }

    public void SaveGame()
    {
        Board board = GetComponent<Board>();
        Player plr = GetComponent<Player>();

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/map.dat");
        bf.Serialize(file, seed);
        bf.Serialize(file, cells);
        bf.Serialize(file, plr.X);
        bf.Serialize(file, plr.Y);
        bf.Serialize(file, GetExplored());
        bf.Serialize(file, board.GetTorch());

        // serialize room data
        bf.Serialize(file, rooms.Count);
        for (int r = 0; r < rooms.Count; r++)
        {
            bf.Serialize(file, rooms[r].Id);
            Rect rRect = rooms[r].Rect;

            bf.Serialize(file, rRect.x);
            bf.Serialize(file, rRect.y);
            bf.Serialize(file, rRect.width);
            bf.Serialize(file, rRect.height);
        }

        // serialize hall data
        bf.Serialize(file, halls.Count);
        for (int h = 0; h < halls.Count; h++)
        {
            bf.Serialize(file, halls[h].Id);
        }

        // serialize conn data
        bf.Serialize(file, connectors.Count);
        for (int c = 0; c < connectors.Count; c++)
        {
            bf.Serialize(file, connectors[c].Id);
            bf.Serialize(file, connectors[c].X);
            bf.Serialize(file, connectors[c].Y);
            bf.Serialize(file, connectors[c].Reg1);
            bf.Serialize(file, connectors[c].Reg2);
            bf.Serialize(file, connectors[c].Type);
            bf.Serialize(file, connectors[c].Orient);
        }

        //Debug.Log("Saved map...");
        file.Close();
    }

    public void LoadGame()
    {
        Board board = GetComponent<Board>();
        MiniBoard miniBoard = GetComponent<MiniBoard>();
        Player plr = GetComponent<Player>();

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(Application.persistentDataPath + "/map.dat", FileMode.Open);
        int fSeed = (int)bf.Deserialize(file);
        int[][] fCells = (int[][])bf.Deserialize(file);
        int fX = (int)bf.Deserialize(file);
        int fY = (int)bf.Deserialize(file);
        bool[][] fExplored = (bool[][])bf.Deserialize(file);
        bool fT = (bool)bf.Deserialize(file);

        // deserialize room data
        rooms.Clear();
        int sr = (int)bf.Deserialize(file);
        for (int r = 0; r < sr; r++)
        {
            int sID = (int)bf.Deserialize(file);
            float sRectX = (float)bf.Deserialize(file);
            float sRectY = (float)bf.Deserialize(file);
            float sRectW = (float)bf.Deserialize(file);
            float sRectH = (float)bf.Deserialize(file);
            Rect sRect = new Rect(sRectX, sRectY, sRectW, sRectH);
            rooms.Add(new Room(sID, sRect));
        }

        // deserialize hall data
        halls.Clear();
        int sh = (int)bf.Deserialize(file);
        for (int h = 0; h < sh; h++)
        {
            int hID = (int)bf.Deserialize(file);
            halls.Add(new Hall(hID));
        }

        // deserialize conn data
        int sc = (int)bf.Deserialize(file);
        for (int c = 0; c < sc; c++)
        {
            int cID = (int)bf.Deserialize(file);
            int cX = (int)bf.Deserialize(file);
            int cY = (int)bf.Deserialize(file);
            int cReg1 = (int)bf.Deserialize(file);
            int cReg2 = (int)bf.Deserialize(file);
            int cType = (int)bf.Deserialize(file);
            int cOrient = (int)bf.Deserialize(file);
            connectors.Add(new Connector(cID, cX, cY, cReg1, cReg2, cType, cOrient));
        }

        //Debug.Log("Loaded map...");
        file.Close();

        seed = fSeed;
        cells = fCells;
        ClearHallCrumbsAndSetHallPaths();
        plr.X = fX;
        plr.Y = fY;
        explored = fExplored;
        board.SetTorch(fT);

        board.ResetBoard();
        miniBoard.UpdateMiniBoard();
    }
}