using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ItemSystem;
using UnityEditor;

public class ItemList : MonoBehaviour
{
    private List <Item> itemList;
    public GameObject itemTile;
    private GameObject itemGOs;
    private GameObject itemTileInstance;
    readonly string itemPrefabsPath = @"Assets/Visual Item Database/Prefabs/Item Prefabs/"; //Path that contains the category folders

    private int height;
    private int width;
    private float scaleFactor = 64;
    private float scale;
    private float mapOffsetX;
    private float mapOffsetY;

    void Start()
    {

    }

    void Update()
    {

    }

    public void Init()
    {
        itemList = new List<Item>();
        ItemBase item;
        for (int i = 0; i < 8; i++)
        {
            int id = i;
            int x = 1;
            int y = 1;
            int pid = -1;
            item = ItemSystemUtility.GetRandomItemCopy(ItemType.MeleeWeapon);
            itemList.Add(new Item(id, x, y, pid, item));
        }
        Debug.Log("Item count:  " + itemList.Count);
        SetupScene();
    }

    public void SetupScene()
    {
        Board board = gameObject.GetComponent<Board>();
        height = board.Radius * 2 + 1;
        width = board.Radius * 2 + 1;

        scale = Screen.height / (scaleFactor * height);
        mapOffsetX = -height * scale / 2 + 0.5f * scale;
        mapOffsetY = mapOffsetX;

        itemGOs = new GameObject("ItemGOs");

        for (int i = 0; i < itemList.Count;i++)
        {
            InstantiateFromArray(itemTile, i, (i+1)*scale, 0*scale);
        }
    }

    public void InstantiateFromArray(GameObject prefab, int i, float xCoord, float yCoord)
    {

        // The position to be instantiated at is based on the coordinates.
        Vector3 position = new Vector3(xCoord, yCoord, 0f);

        
        // Create an instance of the prefab from the random index of the array.
        itemTileInstance = Instantiate(prefab, position, Quaternion.identity) as GameObject;
        itemTileInstance.transform.localScale = new Vector3(scale, scale, scale);

        itemTileInstance.GetComponent<SpriteRenderer>().sprite = itemList[i].GetItemSprite();
        itemTileInstance.name = itemList[i].GetItemName();

        // Set the tile's parent to the mini board holder.
        itemTileInstance.transform.parent = itemGOs.transform;
    }
}