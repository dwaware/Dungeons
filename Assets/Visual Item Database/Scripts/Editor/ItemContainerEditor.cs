using UnityEngine;
using UnityEditor;
using ItemSystem;

[CustomEditor(typeof(ItemContainer))]
public class ItemContainerEditor : Editor
{
    ItemContainer container;

    void OnEnable()
    {
        container = (ItemContainer)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        GUI.color = Color.yellow;
        if (GUILayout.Button("Show in Item Database"))
            OpenItemDatabase();
        GUI.color = Color.white;
    }

    void OpenItemDatabase()
    {
        //Load database if needed
        if (!ItemSystemUtility.itemDatabase)
            ItemSystemUtility.LoadItemDatabase();

        //If itemDatabase is loaded and item exists then show it
        if (!ItemSystemUtility.itemDatabase.ItemExists(container.item.itemID))
        {
            Debug.LogError("Couldn't find item in database");
            return;
        }

        int itemIndex = ItemSystemUtility.itemDatabase.GetItemIndex(container.item);
        EditorPrefs.SetInt("ItemToShowIndex", itemIndex);   //Set item to show
        EditorPrefs.SetInt("ListIndex", (int)container.item.itemType);  //Set category(categories must be the same order in the enum as well)
        EditorPrefs.SetInt("CurrentPage", Mathf.CeilToInt((float)(itemIndex + 1) / EditorPrefs.GetInt("ItemsPerPage")));    //Calculate and set correct page

        ItemDatabaseEditorWindow window = ItemDatabaseEditorWindow.OpenDatabaseWindow();
        window.reloadRequested = true;  //Make a request to reload settings so that this item shows
    }
}