using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room
{
    private int id;
    private Rect rect;

    public Room(int i, Rect r) {
        Id = i;
        Rect = r;
    }

    public Room()
    {

    }

    public int Id
    {
        get
        {
            return id;
        }

        set
        {
            id = value;
        }
    }

    public Rect Rect
    {
        get
        {
            return rect;
        }

        set
        {
            rect = value;
        }
    }

    // Use this for initialization
    void Start () {

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}