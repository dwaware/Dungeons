using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hall
{

    private int id;
    private List<Vector2> crumbs;
    private List<Vector2> paths;


    public Hall(int i)
    {
        Id = i;
        Crumbs = new List<Vector2>();
        Paths = new List<Vector2>();
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

    public List<Vector2> Crumbs
    {
        get
        {
            return crumbs;
        }

        set
        {
            crumbs = value;
        }
    }

    public List<Vector2> Paths
    {
        get
        {
            return paths;
        }

        set
        {
            paths = value;
        }
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}