using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using ItemSystem.Database;

public partial class DatabaseVersionUpdater : EditorWindow
{
    static readonly string resourcesFolderPath = @"Assets/Visual Item Database/Resources/";
    static readonly string scriptsFolderPath = @"Assets/Visual Item Database/Scripts/";
    static readonly string editorFolderPath = @"Assets/Visual Item Database/Scripts/Editor/";
    static readonly string partialClassName = "DatabaseVersionUpdater2";
    static readonly string currentDir = CorrectPathSeparators(Directory.GetCurrentDirectory()) + "/";
    string tempSettingsFullPath;

    bool itemTypeWindowisGood = false;

    [MenuItem("Assets/Visual Item Database/Update Database Version", priority = 2)]
    public static void IsUpdatingDatabase()
    {
        //If the user needs to update, copy the .txt and make the copy a .cs script file
        if (EditorUtility.DisplayDialog("Are You Updating?", "If You have Just Downloaded a New Version Of The Visual Item Database and You Have an Old Database With Items In It, Then Please Press 'Update' So we Can Move Your Items to The New Version.", "Update", "I Don't Have an Old Database"))
        {
            if (!File.Exists(currentDir + editorFolderPath + partialClassName + ".cs"))
            {
                File.Copy(currentDir + editorFolderPath + partialClassName + ".txt",
                    currentDir + editorFolderPath + partialClassName + ".cs", false);
                AssetDatabase.Refresh();    //Detect changes
            }
            ShowWindow();
        }
    }

    public static void ShowWindow()
    {
        //Gets the window if there is one, or creates a new one if there is none
        DatabaseVersionUpdater window = GetWindow<DatabaseVersionUpdater>(false, "Version Updater", true);
        window.minSize = new Vector2(350, 30);
        window.Show();
    }

    void OnEnable()
    {
        itemTypeWindowisGood = TypeWindowIsReady();
        tempSettingsFullPath = currentDir + editorFolderPath + "TempTransferData.txt";
    }

    /// <summary>
    /// Checks whether the 'Item Type Control' window has all the correct paths for generation
    /// </summary>
    /// <returns></returns>
    bool TypeWindowIsReady()
    {
        ItemTypeEditorWindow typeWindow = ItemTypeEditorWindow.OpenItemTypeWindow();
        if (typeWindow.correctEditorDir && typeWindow.correctScriptsDir && typeWindow.correctPrefabsDir)
        {
            typeWindow.Close();
            return true;
        }
        return false;
    }

    void FinishedUpdating()
    {
        //Delete the no longer needed class, close the window and refresh
        File.Delete(currentDir + editorFolderPath + partialClassName + ".cs");
        Close();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Returns a new path that has '\' separators replaced with '/'
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static string CorrectPathSeparators(string path)
    {
        return path.Replace("\\", "/");
    }
}