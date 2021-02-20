using UnityEngine;
using UnityEditor;
using ItemSystem.Database;

[CustomEditor(typeof(ItemDatabaseV3))]
public class ItemDatabaseInspectorEditor : Editor
{
    //This is just so that the database can't be edited in the inspector
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        //Open the window
        if (GUILayout.Button("Open ItemDatabase Editor Window"))
            ItemDatabaseEditorWindow.OpenDatabaseWindow();
    }
}