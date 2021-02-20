using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ItemSystem;

public class Item
{
    private int iID;
    public int x;
    private int y;
    private int pID;
    private ItemBase item;

    public Item(int myiID, int myx, int myy, int mypID, ItemBase myitem)
    {
        iID = myiID;
        x = myx;
        y = myy;
        pID = mypID;
        item = myitem;
    }

    public Item()
    {

    }

    public Sprite GetItemSprite()
    {
        return item.itemSprite;
    }

    public string GetItemName()
    {
        return item.itemName;
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