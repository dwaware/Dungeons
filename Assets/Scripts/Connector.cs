using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connector
{

    private int id;
    private int x;
    private int y;
    private int reg1;
    private int reg2;
    private int type;
    private int orient;

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

    public int Reg1
    {
        get
        {
            return reg1;
        }

        set
        {
            reg1 = value;
        }
    }

    public int Reg2
    {
        get
        {
            return reg2;
        }

        set
        {
            reg2 = value;
        }
    }

    public int Type
    {
        get
        {
            return type;
        }

        set
        {
            type = value;
        }
    }

    public int Orient
    {
        get
        {
            return orient;
        }

        set
        {
            orient = value;
        }
    }

    public Connector(int i, int cx, int cy, int creg1, int creg2, int ctype, int orient)
    {
        Id = i;
        X = cx;
        Y = cy;
        Reg1 = creg1;
        Reg2 = creg2;
        Type = ctype;
        Orient = orient;
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