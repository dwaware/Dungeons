using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    [SerializeField]
    private int x;
    [SerializeField]
    private int y;

    //private GameObject player;
    
    public void InitPlayer()
    {
        //player = new GameObject("Player");

        Map map = gameObject.GetComponent<Map>();
        Board board = gameObject.GetComponent<Board>();

        int randomRoom = Random.Range(0, map.Rooms.Count);
        //Debug.Log(randomRoom);

        x = (int)map.Rooms[randomRoom].Rect.x + Random.Range(0,(int)map.Rooms[randomRoom].Rect.width);
        y = (int)map.Rooms[randomRoom].Rect.y + Random.Range(0,(int)map.Rooms[randomRoom].Rect.height);

        board.UpdatePlayerMap();
    }

    public int X
    {
        get
        {
            return x;
        }

        set
        {
            x = value;
        }
    }

    public int Y
    {
        get
        {
            return y;
        }

        set
        {
            y = value;
        }
    }

    // Use this for initialization
    void Start ()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}