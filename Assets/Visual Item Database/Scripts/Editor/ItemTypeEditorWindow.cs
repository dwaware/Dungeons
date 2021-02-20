using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using ItemSystem;
using ItemSystem.Database;

public class ItemTypeEditorWindow : EditorWindow
{
    enum Mode
    {
        AddNew,
        AddExisting,
        RemoveType,
        AddSubtype,
        RemoveSubtype,
        AddTypeGroup,
        RemoveTypeGroup
    };

    string typeName = "", subtypeName = "", typeClassName = "", typeListName = "", vidListsVar = "autoVidLists";
    string assetsDir = Directory.GetCurrentDirectory().Replace("\\", "/") + "/Assets";
    string editorDir, editorDirToShow, scriptsDir, scriptsDirToShow, prefabsDir, prefabsDirToShow;
    string editorWindowFile = @"/ItemDatabaseEditorWindow.cs", itemBaseFile = @"/ItemBase.cs", itemDatabaseFile = @"/" + ItemDatabaseV3.dbName + ".cs",
        itemUtilityFile = @"/ItemSystemUtility.cs", vidListsFile = @"/" + VIDItemListsV3.itemListsName + ".cs";
    public bool correctEditorDir { get; private set; }
    public bool correctScriptsDir { get; private set; }
    public bool correctPrefabsDir { get; private set; }
    bool removeFromEnum, addFullGroup, removeFullGroup;

    string[] subtypes, typeGroupNames;
    int selectedSubtype, selectedGroup;
    VIDItemListsV3 vidLists;

    Mode mode;
    ItemType selectedType;

    [MenuItem("Assets/Visual Item Database/Open Item Type Control Window %#e", priority = 1)]
    public static ItemTypeEditorWindow OpenItemTypeWindow()
    {
        GetWindow<ItemDatabaseEditorWindow>().Close();  //To prevent any errors, we don't let them be open at the same time

        //Gets the window if there is one, or creates a new one if there is none
        ItemTypeEditorWindow window = GetWindow<ItemTypeEditorWindow>(false, "Item Type Control", true);
        window.minSize = new Vector2(938, 115);
        window.Show();

        return window;
    }

    void OnEnable()
    {
        scriptsDir = assetsDir + "/Visual Item Database/Scripts";
        editorDir = scriptsDir + "/Editor";
        prefabsDir = assetsDir + "/Visual Item Database/Prefabs/Item Prefabs";

        ValidateDirectories();

        ItemDatabaseEditorWindow.CreateItemDatabase();
        vidLists = Resources.Load<VIDItemListsV3>(VIDItemListsV3.itemListsName);
        subtypes = GetSubtypeNames();
        typeGroupNames = GetTypeGroupNames();
    }

    void OnGUI()
    {
        //Select all folders first
        if (!correctEditorDir || !correctScriptsDir || !correctPrefabsDir)
        {
            GUI.color = Color.green;
            if (GUILayout.Button("I Fixed It! Check Again!"))
                ValidateDirectories();

            GUI.color = Color.white;
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("Box");

        mode = (Mode)GUILayout.Toolbar((int)mode, new string[] { "Add New Type", "Add Existing Type", "Remove Type", "Add Subtype", "Remove Subtype",
            "Add Type Group", "Remove Type Group"}, GUILayout.MinWidth(880));

        if (mode == Mode.AddNew)
            AddNewType();
        else if (mode == Mode.AddExisting)
            AddExistingType();
        else if (mode == Mode.RemoveType)
            RemoveType();
        else if (mode == Mode.AddSubtype)
            AddSubtype();
        else if (mode == Mode.RemoveSubtype)
            RemoveSubtype();
        else if (mode == Mode.AddTypeGroup)
            AddTypeGroup();
        else if (mode == Mode.RemoveTypeGroup)
            RemoveTypeGroup();

        GUI.color = Color.white;
        EditorGUILayout.EndVertical();
    }

    void ValidateDirectories()
    {
        //Scripts directory checks
        if (Directory.Exists(scriptsDir))
        {
            if (File.Exists(scriptsDir + itemBaseFile) && File.Exists(scriptsDir + itemDatabaseFile) && File.Exists(scriptsDir + itemUtilityFile) &&
            File.Exists(scriptsDir + vidListsFile))
                correctScriptsDir = true;
            else
                EditorUtility.DisplayDialog("File Not Found", "Please Make Sure That The 'Visual Item Database/Scripts' Folder Contains 'ItemBase.cs', 'ItemDatabaseV3.cs', 'ItemSystemUtility.cs' and 'VIDItemListsV3.cs' Scripts", "OK");
        }

        else
        {
            EditorUtility.DisplayDialog("Folder Not Found", "'Visual Item Database/Scripts' Folder Could NOT be found. Make Sure The 'Visual Item Database' Folder is Directly Inside The Assets folder", "OK");
        }

        //Editor directory checks
        if (Directory.Exists(editorDir))
        {
            if (File.Exists(editorDir + editorWindowFile))
                correctEditorDir = true;
            else
                EditorUtility.DisplayDialog("File Not Found", "Could NOT Find The 'ItemDatabaseEditorWindow.cs' Class in The 'Visual Item Database/Scripts/Editor' Folder, Please Make Sure to Put ALL the VID Editor Scripts There.", "OK");
        }

        else
        {
            EditorUtility.DisplayDialog("Folder Not Found", "'Visual Item Database/Scripts/Editor' Folder Could NOT be found. Make Sure The 'Visual Item Database' Folder is Directly Inside The Assets folder", "OK");
        }

        //Item Prefabs directory checks
        if (prefabsDir.Contains("Item Prefabs"))
            correctPrefabsDir = true;
        else
            EditorUtility.DisplayDialog("Folder Not Found", "Please Make Sure You Have an 'Item Prefabs' Folder in the 'Visual Item Database/Prefabs/' Directory", "OK");
    }

    void AddNewType()
    {
        //New Item Type Name Text Field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name of the New Item Type: ", GUILayout.Width(200));
        typeName = EditorGUILayout.TextField(typeName, GUILayout.MaxWidth(308));
        EditorGUILayout.EndHorizontal();

        //Class name prefix label
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Class To Use for This Type: ", GUILayout.Width(200));

        //Show class name
        EditorGUILayout.BeginHorizontal("Box");
        EditorGUILayout.LabelField(typeClassName, GUILayout.Width(300));
        EditorGUILayout.EndHorizontal();

        //Select class button
        if (GUILayout.Button("Select Class", GUILayout.Width(200)))
            typeClassName = Path.GetFileNameWithoutExtension(EditorUtility.OpenFilePanel("Select Class For New Type", "Assets", "cs"));

        EditorGUILayout.EndHorizontal();

        //Buttons
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(205);

        GUI.color = Color.green;
        if (GUILayout.Button("Add Type", GUILayout.Width(500)))
        {
            EditorGUILayout.EndHorizontal();

            GUI.color = Color.white;
            typeName = typeName.Trim();

            //Make sure everything is filled out
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(typeClassName.Trim()))
            {
                EditorUtility.DisplayDialog("Error", "Please Fill Out the Type and Select a Proper C# Class That Inherits From 'ItemBase'.", "OK");
                return;
            }

            //Accept only letters, numbers and the underscore. Also make sure that the first character isn't a digit
            if (char.IsDigit(typeName[0]) || !System.Text.RegularExpressions.Regex.IsMatch(typeName, @"^[\w]+$"))
            {
                EditorUtility.DisplayDialog("Error", "The Type Name Can ONLY Contain: Letters, Numbers and the 'underscore' Character.\nIt Also Can NOT start with a number", "OK");
                return;
            }

            if (HandleItemBaseCode() && !TypeAlreadyExists())
            {
                typeListName = "auto" + typeName;
                GenerateNewFolder();
                HandleItemUtilityCode();
                HandleVIDItemListsCode();
                HandleItemDatabaseCode();
                HandleItemDatabaseWindowCode();

                //Force a refresh to detect changes
                AssetDatabase.Refresh();
            }
        }
    }

    void AddExistingType()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Item Type to Add:", GUILayout.Width(200));
        selectedType = (ItemType)EditorGUILayout.EnumPopup(selectedType, GUILayout.Width(308));
        EditorGUILayout.EndHorizontal();

        //Class name prefix label
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Class To Use for This Item Type: ", GUILayout.Width(200));

        //Show class name
        EditorGUILayout.BeginHorizontal("Box");
        EditorGUILayout.LabelField(typeClassName, GUILayout.Width(300));
        EditorGUILayout.EndHorizontal();

        //Select class button
        if (GUILayout.Button("Select Class", GUILayout.Width(200)))
            typeClassName = Path.GetFileNameWithoutExtension(EditorUtility.OpenFilePanel("Select Class For This Item Type", "Assets", "cs"));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(205);

        GUI.color = Color.green;
        if (GUILayout.Button("Add Type", GUILayout.MaxWidth(500)))
        {
            typeName = selectedType.ToString();

            //Null and empty checks on type and class names
            if (!string.IsNullOrEmpty(typeName.Trim()) && !string.IsNullOrEmpty(typeClassName.Trim()))
            {
                typeListName = "auto" + typeName;

                if (!TypeAlreadyExists())
                {
                    GenerateNewFolder();
                    HandleItemUtilityCode();
                    HandleVIDItemListsCode();
                    HandleItemDatabaseCode();
                    HandleItemDatabaseWindowCode();

                    //Force a refresh to detect changes
                    AssetDatabase.Refresh();
                }

                else
                {
                    EditorUtility.DisplayDialog("Error", "This Item Type Already Exists", "OK");
                }
            }

            else
            {
                EditorUtility.DisplayDialog("Error", "Please Select the Class to Use", "OK");
            }
        }
    }

    void RemoveType()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Item Type to Remove:", GUILayout.Width(200));
        selectedType = (ItemType)EditorGUILayout.EnumPopup(selectedType, GUILayout.Width(308));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Remove Type From Enum As well: ", GUILayout.Width(200));
        removeFromEnum = EditorGUILayout.Toggle(removeFromEnum, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(205);

        GUI.color = Color.yellow;
        if (GUILayout.Button("Remove Type", GUILayout.MaxWidth(500)) &&
            EditorUtility.DisplayDialog("WARNING!", "Deleting the '" + selectedType.ToString() + "' Category Will Remove It and ALL the Items in it.\nThis Action Can NOT be undone.", "Delete", "Cancel"))
        {
            EditorGUILayout.EndHorizontal();
            GUI.color = Color.white;

            typeName = selectedType.ToString();
            typeListName = "auto" + typeName;

            if (HandleItemBaseCode())
            {
                //If a db exists remove the IDs of the type we are deleting
                if (vidLists)
                {
                    for (int i = vidLists.usedIDs.Count - 1; i >= 0; i--)
                    {
                        if (vidLists.typesOfUsedIDs[i] == selectedType)
                        {
                            vidLists.usedIDs.RemoveAt(i);
                            vidLists.typesOfUsedIDs.RemoveAt(i);
                        }
                    }
                }

                //Remove all subtypes of this type
                vidLists.subtypes.RemoveAll(x => x.type == selectedType);

                //Remove type from all groups it is in
                for (int i = 0; i < vidLists.typeGroups.Count; i++)
                {
                    if (vidLists.typeGroups[i].types.Contains(selectedType))
                        vidLists.typeGroups[i].types.Remove(selectedType);
                }
                vidLists.typeGroups.RemoveAll(x => x.types.Count == 0); //Remove any type groups which have no types in them

                HandleItemUtilityCode();
                HandleVIDItemListsCode();
                HandleItemDatabaseCode();
                HandleItemDatabaseWindowCode();

                //Force a refresh to detect changes
                AssetDatabase.Refresh();
                selectedType = 0;
            }
        }
    }

    void AddSubtype()
    {
        //New subtype name text field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name of the New Subtype: ", GUILayout.Width(200));
        subtypeName = EditorGUILayout.TextField(subtypeName, GUILayout.MaxWidth(308));
        EditorGUILayout.EndHorizontal();

        //Main type selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Main Type:", GUILayout.Width(200));
        selectedType = (ItemType)EditorGUILayout.EnumPopup(selectedType, GUILayout.Width(308));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(205);
        GUI.color = Color.green;
        if (GUILayout.Button("Add Subtype", GUILayout.Width(500)))
        {
            EditorGUILayout.EndHorizontal();

            subtypeName = System.Text.RegularExpressions.Regex.Replace(subtypeName, @"\s+", "");    //Remove all spaces
            subtypeName = subtypeName.Trim();

            //Accept only letters, numbers and the underscore. Also make sure that the first character isn't a digit
            if (string.IsNullOrEmpty(subtypeName) || char.IsDigit(subtypeName[0]) || !System.Text.RegularExpressions.Regex.IsMatch(subtypeName, @"^[\w]+$"))
            {
                EditorUtility.DisplayDialog("Invalid Name!", "Subtype name can only have: Letters, Numbers and Underscore and can NOT start with a digit.", "OK");
                return;
            }

            //If the main type exists in the database
            if (TypeAlreadyExists(selectedType))
            {
                //Subtype already exists for this type
                if (SubtypeExists(selectedType, subtypeName))
                {
                    EditorUtility.DisplayDialog("Error!", "A subtype of this name already exists for this main type.\nPlease Change its name.", "OK");
                    return;
                }

                vidLists.subtypes.Add(new ItemSubtypeV25(subtypeName, selectedType));
                HandleVIDItemListsCode();
                EditorUtility.SetDirty(vidLists);
                EditorUtility.DisplayDialog("Subtype Added!", "Your subtype has been added successfully.", "Thanks!");
                AssetDatabase.Refresh();
                GUIUtility.keyboardControl = 0;
                subtypeName = string.Empty;
                subtypes = GetSubtypeNames();   //Update so it is shown if switched to the other window
            }

            //If the main type isn't in the database
            else
            {
                EditorGUILayout.EndHorizontal();
                EditorUtility.DisplayDialog("Error!", "This main type isn't added to the database. Please add it before adding subtypes to it.", "OK");
            }
        }
    }

    void RemoveSubtype()
    {
        ItemType previousType = selectedType;

        //Main type selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Main Type:", GUILayout.Width(200));
        selectedType = (ItemType)EditorGUILayout.EnumPopup(selectedType, GUILayout.Width(308));
        EditorGUILayout.EndHorizontal();

        //If main type has changed
        if (previousType != selectedType)
        {
            selectedSubtype = 0;
            subtypes = GetSubtypeNames();
        }

        //If no subtypes
        if (subtypes.Length == 0)
        {
            EditorGUILayout.LabelField("This Type Has No Subtypes.");
            return;
        }

        //New subtype name text field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Subtype To Remove: ", GUILayout.Width(200));
        selectedSubtype = EditorGUILayout.Popup(selectedSubtype, subtypes, GUILayout.MaxWidth(308));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(205);
        GUI.color = Color.yellow;
        if (GUILayout.Button("Remove Subtype", GUILayout.Width(500)) && EditorUtility.DisplayDialog("Are you sure?", "Are you sure you want to delete the '" + subtypes[selectedSubtype] + "' subtype?", "Yes", "No"))
        {
            EditorGUILayout.EndHorizontal();

            //Remove subtype
            for (int i = 0; i < vidLists.subtypes.Count; i++)
            {
                if (vidLists.subtypes[i].type == selectedType && vidLists.subtypes[i].name == subtypes[selectedSubtype])
                {
                    vidLists.subtypes.RemoveAt(i);
                    break;
                }
            }
            HandleVIDItemListsCode();

            EditorUtility.DisplayDialog("Subtype Removed!", "The subtype has been successfully removed.", "Thanks!");
            EditorUtility.SetDirty(vidLists);
            AssetDatabase.Refresh();

            //Reset selected and update subtype names
            GUIUtility.keyboardControl = 0;
            selectedSubtype = 0;
            subtypes = GetSubtypeNames();
        }
    }

    void AddTypeGroup()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Add New Group: ", GUILayout.Width(200));
        addFullGroup = EditorGUILayout.Toggle(addFullGroup, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        //Adding a new type group
        if (addFullGroup)
        {
            //New type group name text field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name of the New Type Group: ", GUILayout.Width(200));
            typeName = EditorGUILayout.TextField(typeName, GUILayout.MaxWidth(308));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Default Item Type:", GUILayout.Width(200));
            selectedType = (ItemType)EditorGUILayout.EnumPopup(selectedType, GUILayout.Width(308));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(205);
            GUI.color = Color.green;
            if (GUILayout.Button("Add Type Group", GUILayout.Width(500)))
            {
                EditorGUILayout.EndHorizontal();

                //Make sure everything is filled out
                if (string.IsNullOrEmpty(typeName.Trim()))
                {
                    EditorUtility.DisplayDialog("Error", "Please Fill Out the Group Name.", "OK");
                    return;
                }

                //Make sure the type is added
                if (!TypeAlreadyExists(selectedType))
                {
                    EditorUtility.DisplayDialog("Error", "The Selected Default Item Type is NOT Added To The Database. Please Add It First", "OK");
                    return;
                }

                //Check to see if we already have a group with this name
                for (int i = 0; i < vidLists.typeGroups.Count; i++)
                {
                    if (vidLists.typeGroups[i].name == typeName)
                    {
                        EditorUtility.DisplayDialog("Error", "A Group With This Name Already Exists.", "OK");
                        return;
                    }
                }

                //Add and update names list
                vidLists.typeGroups.Add(new ItemTypeGroup(typeName, selectedType));
                typeGroupNames = GetTypeGroupNames();
                EditorUtility.DisplayDialog("Success", "Item Group Added!", "Nice!");

                //For convenience of the user select the new group and reset selected type
                addFullGroup = false;
                selectedType = 0;
                selectedGroup = vidLists.typeGroups.Count - 1;
            }
        }

        //Adding a new Type
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type Group To Add To:", GUILayout.Width(200));
            selectedGroup = EditorGUILayout.Popup(selectedGroup, typeGroupNames, GUILayout.Width(308));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Item Type To Add:", GUILayout.Width(200));
            selectedType = (ItemType)EditorGUILayout.EnumPopup(selectedType, GUILayout.Width(308));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(205);
            GUI.color = Color.green;
            if (GUILayout.Button("Add Type Group", GUILayout.Width(500)))
            {
                EditorGUILayout.EndHorizontal();

                //Make sure the group doesn't already contain this type
                if (vidLists.typeGroups[selectedGroup].types.Contains(selectedType))
                {
                    EditorUtility.DisplayDialog("Error", "Type Group Already Has This Item Type.", "OK");
                    return;
                }

                //Make sure the type is added
                if (!TypeAlreadyExists(selectedType))
                {
                    EditorUtility.DisplayDialog("Error", "The Selected Default Item Type is NOT Added To The Database. Please Add It First", "OK");
                    return;
                }

                vidLists.typeGroups[selectedGroup].types.Add(selectedType);
                EditorUtility.DisplayDialog("Success", "Item Type Has Been Added to The Type Group", "Cool");
            }
        }
    }

    void RemoveTypeGroup()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Remove Group: ", GUILayout.Width(200));
        removeFullGroup = EditorGUILayout.Toggle(removeFullGroup, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        //Removing entire groups
        if (removeFullGroup)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type Group To Remove:", GUILayout.Width(200));
            selectedGroup = EditorGUILayout.Popup(selectedGroup, typeGroupNames, GUILayout.Width(308));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(205);
            GUI.color = Color.yellow;
            if (GUILayout.Button("Remove Type Group", GUILayout.MaxWidth(500)) &&
                EditorUtility.DisplayDialog("Please Confirm", "Delete The '" + typeGroupNames[selectedGroup] + "' Item Group?", "Delete", "Cancel"))
            {
                EditorGUILayout.EndHorizontal();

                vidLists.typeGroups.RemoveAt(selectedGroup);
                EditorUtility.DisplayDialog("Success", "Type Group Has Been Removed", "Cool");
                selectedGroup = 0;
                typeGroupNames = GetTypeGroupNames();
            }
        }

        //Removing types from a group
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type Group To Remove From:", GUILayout.Width(200));
            selectedGroup = EditorGUILayout.Popup(selectedGroup, typeGroupNames, GUILayout.Width(308));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Item Type To Remove:", GUILayout.Width(200));
            selectedType = (ItemType)EditorGUILayout.EnumPopup(selectedType, GUILayout.Width(308));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(205);
            GUI.color = Color.yellow;
            if (GUILayout.Button("Remove Type From Group", GUILayout.MaxWidth(500)) &&
                EditorUtility.DisplayDialog("Please Confirm", "Remove The '" + selectedType.ToString() + "' Item Type From The Group?", "Remove", "Cancel"))
            {
                EditorGUILayout.EndHorizontal();

                if (vidLists.typeGroups[selectedGroup].types.Count == 1)
                {
                    EditorUtility.DisplayDialog("Error!", "A Type Group Needs At Least ONE Type.", "OK");
                    return;
                }

                //Make sure the type is actually added to the group
                if (!vidLists.typeGroups[selectedGroup].types.Contains(selectedType))
                {
                    EditorUtility.DisplayDialog("Error!", "This Type is NOT Added To This Type Group.", "OK");
                    return;
                }

                vidLists.typeGroups[selectedGroup].types.Remove(selectedType);
                EditorUtility.DisplayDialog("Success", "The Selected Type Has Been Removed From This Type Group.", "Nice");
                selectedType = 0;
            }
        }
    }

    /// <summary>
    /// Gets the subtypes of the currently selected type
    /// </summary>
    /// <returns></returns>
    string[] GetSubtypeNames()
    {
        List<string> names = new List<string>();

        //Get the names of all the subtypes that have the same type as the current type
        for (int i = 0; i < vidLists.subtypes.Count; i++)
            if (vidLists.subtypes[i].type == selectedType)
                names.Add(vidLists.subtypes[i].name);

        return names.ToArray();
    }

    string[] GetTypeGroupNames()
    {
        List<string> names = new List<string>();

        //Get the names of all the subtypes that have the same type as the current type
        for (int i = 0; i < vidLists.typeGroups.Count; i++)
            names.Add(vidLists.typeGroups[i].name);

        return names.ToArray();
    }

    public void GenerateExistingTypeDirectly(string className, ItemType type)
    {
        typeClassName = className;
        typeName = type.ToString();
        typeListName = "auto" + typeName;
        mode = Mode.AddExisting;

        if (!TypeAlreadyExists())
        {
            GenerateNewFolder();
            HandleItemUtilityCode();
            HandleVIDItemListsCode();
            HandleItemDatabaseCode();
            HandleItemDatabaseWindowCode();

            //Force a refresh to detect changes
            AssetDatabase.Refresh();
        }

        else
        {
            EditorUtility.DisplayDialog("Error", "This Item Type Already Exists", "OK");
        }
    }

    public void GenerateSubtypeDirectly(ItemType type, string subName)
    {
        subtypeName = subName;
        selectedType = type;
        vidLists.subtypes.Add(new ItemSubtypeV25(subtypeName, selectedType));
        mode = Mode.AddSubtype;
        HandleVIDItemListsCode();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Returns whether a type already exists by checking if a condition for it exists in the 'ItemSystemUtility' class
    /// </summary>
    /// <returns></returns>
    bool TypeAlreadyExists()
    {
        string[] itemUtility = File.ReadAllLines(scriptsDir + itemUtilityFile);

        int giceIndex = -1, gicbIndex = -1;

        for (int i = 0; i < itemUtility.Length; i++)
        {
            //Wanted indices
            if (itemUtility[i].Contains("//#VID-GICE"))
                giceIndex = i;
            else if (itemUtility[i].Contains("//#VID-GICB"))
                gicbIndex = i;
        }

        for (int i = gicbIndex; i < giceIndex; i++)
        {
            if (itemUtility[i].Contains(typeName + ":"))
                return true;
        }
        return false;
    }

    bool TypeAlreadyExists(ItemType typeToCheck)
    {
        string[] itemUtility = File.ReadAllLines(scriptsDir + itemUtilityFile);
        string typeToCheckName = typeToCheck.ToString();
        int giceIndex = -1, gicbIndex = -1;

        for (int i = 0; i < itemUtility.Length; i++)
        {
            //Wanted indices
            if (itemUtility[i].Contains("//#VID-GICE"))
                giceIndex = i;
            else if (itemUtility[i].Contains("//#VID-GICB"))
                gicbIndex = i;
        }

        for (int i = gicbIndex; i < giceIndex; i++)
        {
            if (itemUtility[i].Contains(typeToCheckName + ":"))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns whether it was successful or not
    /// </summary>
    /// <returns></returns>
    bool HandleItemBaseCode()
    {
        string[] itemBase = File.ReadAllLines(scriptsDir + itemBaseFile);
        List<string> itemBaseNew = new List<string>();

        int itbIndex = -1, iteIndex = -1;
        int typeIndex = -1;
        bool typeExists = false;

        for (int i = 0; i < itemBase.Length; i++)
        {
            itemBaseNew.Add(itemBase[i]);

            //Wanted indices
            if (itemBase[i].Contains("//#VID-ITB"))
                itbIndex = i;
            else if (itemBase[i].Contains("//#VID-ITE"))
                iteIndex = i;
        }

        //Check if the type already exists
        for (int i = itbIndex + 1; i < iteIndex; i++)
        {
            if (itemBaseNew[i].Contains(typeName + ",") || itemBaseNew[i].Contains(typeName + " ") || (itemBaseNew[i].Trim() + ",").Contains(typeName + ","))
            {
                typeExists = true;
                typeIndex = i;
                break;
            }
        }

        if (mode == Mode.AddNew)
        {
            //If the type doesn't exist
            if (!typeExists)
            {
                if (!itemBaseNew[iteIndex - 1].Contains(","))
                    itemBaseNew[iteIndex - 1] += ",";

                itemBaseNew.Insert(iteIndex, "\t\t" + typeName + ",");

                //Write new code
                File.WriteAllLines(scriptsDir + itemBaseFile, itemBaseNew.ToArray());
            }

            else
            {
                EditorUtility.DisplayDialog("Type Already Exists", "The Item Type Already Exists", "OK");
            }

            return !typeExists;
        }

        //Delete
        else if (mode == Mode.RemoveType)
        {
            if (typeExists && TypeAlreadyExists())
            {
                if (removeFromEnum)
                {
                    itemBaseNew.RemoveAt(typeIndex);

                    //Write new code
                    File.WriteAllLines(scriptsDir + itemBaseFile, itemBaseNew.ToArray());
                }
            }

            else
            {
                typeExists = false;
                EditorUtility.DisplayDialog("Type Does Not Exist", "The Item Type Could Not be found", "OK");
            }
            return typeExists;
        }

        return typeExists;
    }

    public bool SubtypeExists(ItemType mainType, string subName)
    {
        //See whether this subtype already exists
        for (int i = 0; i < vidLists.subtypes.Count; i++)
            if (vidLists.subtypes[i].type == mainType && vidLists.subtypes[i].name == subName)
                return true;

        return false;
    }

    void GenerateNewFolder()
    {
        if (!Directory.Exists(prefabsDir + @"/" + typeName))
            Directory.CreateDirectory(prefabsDir + @"/" + typeName);
    }

    /// <summary>
    /// Generates the code for the 'ItemSystemUtility' class
    /// </summary>
    void HandleItemUtilityCode()
    {
        string[] itemUtility = File.ReadAllLines(scriptsDir + itemUtilityFile);
        List<string> itemUtilityNew = new List<string>();

        int giceIndex = -1, gicbIndex = -1, gice2Index = -1, gicb2Index = -1, griceIndex = -1, gricbIndex = -1, gricb2Index = -1, grice2Index = -1,
            gatibIndex = -1, gatieIndex = -1, gasibIndex = -1, gasieIndex = -1;
        int offset = 0;

        for (int i = 0; i < itemUtility.Length; i++)
        {
            itemUtilityNew.Add(itemUtility[i]);

            //Wanted indicies
            if (itemUtility[i].Contains("//#VID-GICE"))
                giceIndex = i;
            else if (itemUtility[i].Contains("//#VID-GICB"))
                gicbIndex = i;
            else if (itemUtility[i].Contains("//#VID-2GICE"))
                gice2Index = i;
            else if (itemUtility[i].Contains("//#VID-2GICB"))
                gicb2Index = i;
            else if (itemUtility[i].Contains("//#VID-GRICE"))
                griceIndex = i;
            else if (itemUtility[i].Contains("//#VID-GRICB"))
                gricbIndex = i;
            else if (itemUtility[i].Contains("//#VID-2GRICB"))
                gricb2Index = i;
            else if (itemUtility[i].Contains("//#VID-2GRICE"))
                grice2Index = i;
            else if (itemUtility[i].Contains("//#VID-GATIB"))
                gatibIndex = i;
            else if (itemUtility[i].Contains("//#VID-GATIE"))
                gatieIndex = i;
            else if (itemUtility[i].Contains("//#VID-GASIB"))
                gasibIndex = i;
            else if (itemUtility[i].Contains("//#VID-GASIE"))
                gasieIndex = i;
        }

        if (mode == Mode.AddNew || mode == Mode.AddExisting)
        {
            /*Generate code*/
            //Bottom to top because of 'Insert' pushes the current line down, so by the end the first line we put becomes the last.
            //In the other indicies we also compensate for the newly added lines above them by incrementing the original index.
            //The indicies are offset depending on how many lines have been added before the index. So if a line is added, the index is incremented by one.
            itemUtilityNew.Insert(giceIndex, "\t\t\t\t\tbreak;");
            itemUtilityNew.Insert(giceIndex, "\t\t\t\t\titem = new " + typeClassName + "();");
            itemUtilityNew.Insert(giceIndex, "\t\t\t\tcase ItemType." + typeName + ":");
            offset += 3;

            itemUtilityNew.Insert(gice2Index + offset, "\t\t\t\t\tbreak;");
            itemUtilityNew.Insert(gice2Index + offset, "\t\t\t\t\titem = new " + typeClassName + "();");
            itemUtilityNew.Insert(gice2Index + offset, "\t\t\t\tcase ItemType." + typeName + ":");
            offset += 3;

            itemUtilityNew.Insert(griceIndex + offset, "\t\t\t\t\tbreak;");
            itemUtilityNew.Insert(griceIndex + offset, "\t\t\t\t\titem = new " + typeClassName + "();");
            itemUtilityNew.Insert(griceIndex + offset, "\t\t\t\tcase ItemType." + typeName + ":");
            offset += 3;

            itemUtilityNew.Insert(grice2Index + offset, "\t\t\t\t\tbreak;");
            itemUtilityNew.Insert(grice2Index + offset, "\t\t\t\t\titem = new " + typeClassName + "();");
            itemUtilityNew.Insert(grice2Index + offset, "\t\t\t\tcase ItemType." + typeName + ":");
            offset += 3;

            //Get all type items method
            itemUtilityNew.Insert(gatieIndex + offset, "\t\t\t\t\tbreak;");
            itemUtilityNew.Insert(gatieIndex + offset, "\t\t\t\t\t}");
            itemUtilityNew.Insert(gatieIndex + offset, "\t\t\t\t\t\titems.Add(instance);");
            itemUtilityNew.Insert(gatieIndex + offset, "\t\t\t\t\t\tinstance.UpdateUniqueProperties(vidLists." + typeListName + "[i]);");
            itemUtilityNew.Insert(gatieIndex + offset, "\t\t\t\t\t\tinstance.UpdateGenericProperties(vidLists." + typeListName + "[i]);");
            itemUtilityNew.Insert(gatieIndex + offset, "\t\t\t\t\t\tinstance = new T();");
            itemUtilityNew.Insert(gatieIndex + offset, "\t\t\t\t\t{");
            itemUtilityNew.Insert(gatieIndex + offset, "\t\t\t\t\tfor (int i = 0; i < vidLists." + typeListName + ".Count; i++)");
            itemUtilityNew.Insert(gatieIndex + offset, "\t\t\t\tcase ItemType." + typeName + ":");
            offset += 9;

            //Get all subtype items method
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\tbreak;");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\t}");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\t\t}");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\t\t\t\treturn items;");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\t\t\tif (items.Count == vidLists.subtypes[subtypeIndex].itemIDs.Count)");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\t\t\t");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\t\t\titems.Add(instance);");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\t\t\tinstance.UpdateUniqueProperties(vidLists." + typeListName + "[i]);");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\t\t\tinstance.UpdateGenericProperties(vidLists." + typeListName + "[i]);");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\t\t\tinstance = new T();");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\t\t{");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\t\tif (vidLists.subtypes[subtypeIndex].itemIDs.Contains(vidLists." + typeListName + "[i].itemID))");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\t{");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\t\tfor (int i = 0; i < vidLists." + typeListName + ".Count; i++)");
            itemUtilityNew.Insert(gasieIndex + offset, "\t\t\t\tcase ItemType." + typeName + ":");

        }

        //Delete Type
        else if (mode == Mode.RemoveType)
        {
            //Delete code from get item copy method
            for (int i = gicbIndex + 1; i < giceIndex; i += 3)
            {
                if (itemUtilityNew[i].Contains(typeName + ":"))
                {
                    itemUtilityNew.RemoveRange(i, 3);
                    break;
                }
            }
            offset += 3;

            for (int i = gicb2Index - (offset - 1); i < gice2Index - offset; i += 3)
            {
                if (itemUtilityNew[i].Contains(typeName + ":"))
                {
                    itemUtilityNew.RemoveRange(i, 3);
                    break;
                }
            }
            offset += 3;

            //Delete code from get random item copy method
            for (int i = gricbIndex - (offset - 1); i < griceIndex - offset; i += 3)
            {
                if (itemUtilityNew[i].Contains(typeName + ":"))
                {
                    itemUtilityNew.RemoveRange(i, 3);
                    break;
                }
            }
            offset += 3;

            for (int i = gricb2Index - (offset - 1); i < grice2Index - offset; i += 3)
            {
                if (itemUtilityNew[i].Contains(typeName + ":"))
                {
                    itemUtilityNew.RemoveRange(i, 3);
                    break;
                }
            }
            offset += 3;

            //Get all type items method
            for (int i = gatibIndex - (offset - 1); i < gatieIndex - offset; i += 9)
            {
                if (itemUtilityNew[i].Contains(typeName + ":"))
                {
                    itemUtilityNew.RemoveRange(i, 9);
                    break;
                }
            }
            offset += 9;

            //Get all subtype items method
            for (int i = gasibIndex - (offset - 1); i < gasieIndex - offset; i += 15)
            {
                if (itemUtilityNew[i].Contains(typeName + ":"))
                {
                    itemUtilityNew.RemoveRange(i, 15);
                    break;
                }
            }
            offset += 9;
        }

        //Write new code
        File.WriteAllLines(scriptsDir + itemUtilityFile, itemUtilityNew.ToArray());
    }

    void HandleVIDItemListsCode()
    {
        string[] vid = File.ReadAllLines(scriptsDir + vidListsFile);
        List<string> vidNew = new List<string>();
        vidNew.AddRange(vid);

        int icbIndex = -1, iceIndex = -1, isnbIndex = -1, isneIndex = -1, isnb2Index = -1, isne2Index = -1;
        int offset = 0;

        for (int i = 0; i < vid.Length; i++)
        {
            //Wanted indices
            if (vid[i].Contains("//#VID-ICB"))
                icbIndex = i;
            else if (vid[i].Contains("//#VID-ICE"))
                iceIndex = i;
            else if (vid[i].Contains("//#VID-ISNB"))
                isnbIndex = i;
            else if (vid[i].Contains("//#VID-ISNE"))
                isneIndex = i;
            else if (vid[i].Contains("//#VID-2ISNB"))
                isnb2Index = i;
            else if (vid[i].Contains("//#VID-2ISNE"))
                isne2Index = i;
        }

        if (mode == Mode.AddNew || mode == Mode.AddExisting)
        {
            vidNew.Insert(iceIndex, string.Format("\t\tpublic List<{0}> {1} = new List<{0}>();", typeClassName, typeListName));
        }

        else if (mode == Mode.AddSubtype || mode == Mode.RemoveSubtype || mode == Mode.RemoveType)
        {
            int subtypeEnumStartIndex = -1, subtypeEnumEndIndex = -1, namesEnumStartIndex = -1, namesEnumEndIndex = -1;
            string subtypeEnumName = "public enum " + selectedType + "Subtypes";   //Enum name to search for
            string itemNamesEnum = "public enum " + selectedType + "Items";

            //Check whether a subtype enum exists for this type
            for (int i = isnbIndex + 1; i < isneIndex; i++)
            {
                //If there is an enum get the index of its closing bracket(the last line of the enum)
                if (vidNew[i].Contains(subtypeEnumName))
                {
                    subtypeEnumStartIndex = i;
                    for (int j = i + 2; j < isneIndex; j++)
                    {
                        if (vidNew[j].Contains("}"))
                        {
                            subtypeEnumEndIndex = j;
                            break;
                        }
                    }
                    break;
                }
            }

            //Check whether a names enum exists for this type
            for (int i = isnb2Index + 1; i < isne2Index; i++)
            {
                //If there is an enum get the index of its closing bracket(the last line of the enum)
                if (vidNew[i].Contains(itemNamesEnum))
                {
                    namesEnumStartIndex = i;
                    for (int j = i + 2; j < isne2Index; j++)
                    {
                        if (vidNew[j].Contains("}"))
                        {
                            namesEnumEndIndex = j;
                            break;
                        }
                    }
                    break;
                }
            }

            if (mode == Mode.AddSubtype)
            {
                //If there is no enum for this type then make one
                if (subtypeEnumEndIndex == -1)
                {
                    vidNew.Insert(isneIndex, "\t}");
                    vidNew.Insert(isneIndex, "\t{");
                    vidNew.Insert(isneIndex, "\t" + subtypeEnumName);

                    isneIndex += 3;
                    subtypeEnumEndIndex = isneIndex - 1;
                }

                vidNew.Insert(subtypeEnumEndIndex, "\t\t" + subtypeName + ",");
            }

            else if (mode == Mode.RemoveSubtype)
            {
                //Find and remove subtype
                for (int i = subtypeEnumStartIndex; i < subtypeEnumEndIndex; i++)
                {
                    if (vidNew[i].Contains(subtypes[selectedSubtype]))
                    {
                        vidNew.RemoveAt(i);
                        break;
                    }
                }
            }

            else if (mode == Mode.RemoveType)
            {
                //Remove all subtypes
                if (subtypeEnumStartIndex != -1)
                {
                    offset = (subtypeEnumEndIndex - subtypeEnumStartIndex) + 1;
                    vidNew.RemoveRange(subtypeEnumStartIndex, offset);
                }

                //Remove names enum
                if (namesEnumStartIndex != -1)
                {
                    namesEnumStartIndex -= offset;
                    namesEnumEndIndex -= offset;

                    vidNew.RemoveRange(namesEnumStartIndex, (namesEnumEndIndex - namesEnumStartIndex) + 1);
                    offset += (namesEnumEndIndex - namesEnumStartIndex) + 1;
                }

                //Remove item type list
                for (int i = (icbIndex - offset) + 1; i < iceIndex - offset; i++)
                {
                    if (vidNew[i].Contains("List") && vidNew[i].Contains(typeListName))
                    {
                        vidNew.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        File.WriteAllLines(scriptsDir + vidListsFile, vidNew.ToArray());
    }

    /// <summary>
    /// Generate code for the 'ItemDatabase' class
    /// </summary>
    void HandleItemDatabaseCode()
    {
        string[] db = File.ReadAllLines(scriptsDir + itemDatabaseFile);
        List<string> dbNew = new List<string>();

        int giibIndex = -1, giieIndex = -1, giib2Index = -1, giie2Index = -1, gibIndex = -1, gieIndex = -1, gib2Index = -1, gie2Index = -1, gribIndex = -1, grieIndex = -1, amidbIndex = -1, amideIndex = -1, reidbIndex = -1, reideIndex = -1;
        int offset = 0;

        if (mode == Mode.AddNew || mode == Mode.AddExisting)
        {
            for (int i = 0; i < db.Length; i++)
            {
                dbNew.Add(db[i]);

                if (db[i].Contains("//#VID-GIIE"))
                    giieIndex = i;
                else if (db[i].Contains("//#VID-2GIIE"))
                    giie2Index = i;
                else if (db[i].Contains("//#VID-GIE"))
                    gieIndex = i;
                else if (db[i].Contains("//#VID-2GIE"))
                    gie2Index = i;
                else if (db[i].Contains("//#VID-GRIE"))
                    grieIndex = i;
                else if (db[i].Contains("//#VID-AMIDE"))
                    amideIndex = i;
                else if (db[i].Contains("//#VID-REIDE"))
                    reideIndex = i;
            }

            //Add Code to 'Get Item Index' method
            dbNew.Insert(giieIndex + offset, "\t\t\t\t\tbreak;");
            dbNew.Insert(giieIndex + offset, "\t\t\t\t\t}");
            dbNew.Insert(giieIndex + offset, "\t\t\t\t\t\t\treturn i;");
            dbNew.Insert(giieIndex + offset, "\t\t\t\t\t\t" + string.Format("if ({0}[i].itemID == item.itemID)", vidListsVar + "." + typeListName));
            dbNew.Insert(giieIndex + offset, "\t\t\t\t\t{");
            dbNew.Insert(giieIndex + offset, "\t\t\t\t\t" + string.Format("for (int i = 0; i < {0}.Count; i++)", vidListsVar + "." + typeListName));
            dbNew.Insert(giieIndex + offset, "\n\t\t\t\t" + string.Format("case ItemType.{0}:", typeName));
            offset += 7;

            //Add Code to 'Get Item Index' method overload
            dbNew.Insert(giie2Index + offset, "\t\t\t\t\tbreak;");
            dbNew.Insert(giie2Index + offset, "\t\t\t\t\t}");
            dbNew.Insert(giie2Index + offset, "\t\t\t\t\t\t\treturn i;");
            dbNew.Insert(giie2Index + offset, "\t\t\t\t\t\t" + string.Format("if ({0}[i].itemID == id)", vidListsVar + "." + typeListName));
            dbNew.Insert(giie2Index + offset, "\t\t\t\t\t{");
            dbNew.Insert(giie2Index + offset, "\t\t\t\t\t" + string.Format("for (int i = 0; i < {0}.Count; i++)", vidListsVar + "." + typeListName));
            dbNew.Insert(giie2Index + offset, "\n\t\t\t\t" + string.Format("case ItemType.{0}:", typeName));
            offset += 7;

            //Add Code to 'Get Item' method
            dbNew.Insert(gieIndex + offset, "\t\t\t\t\tbreak;");
            dbNew.Insert(gieIndex + offset, "\t\t\t\t\t}");
            dbNew.Insert(gieIndex + offset, "\t\t\t\t\t\t\t" + string.Format("return {0}[i];", vidListsVar + "." + typeListName));
            dbNew.Insert(gieIndex + offset, "\t\t\t\t\t\t" + string.Format("if ({0}[i].itemID == id)", vidListsVar + "." + typeListName));
            dbNew.Insert(gieIndex + offset, "\t\t\t\t\t{");
            dbNew.Insert(gieIndex + offset, "\t\t\t\t\t" + string.Format("for (int i = 0; i < {0}.Count; i++)", vidListsVar + "." + typeListName));
            dbNew.Insert(gieIndex + offset, "\n\t\t\t\t" + string.Format("case ItemType.{0}:", typeName));
            offset += 7;

            //Add Code to 'Get Item' method overload
            dbNew.Insert(gie2Index + offset, "\t\t\t\t\tbreak;");
            dbNew.Insert(gie2Index + offset, "\t\t\t\t\t}");
            dbNew.Insert(gie2Index + offset, "\t\t\t\t\t\t\t" + string.Format("return {0}[i];", vidListsVar + "." + typeListName));
            dbNew.Insert(gie2Index + offset, "\t\t\t\t\t\t" + string.Format("if ({0}[i].itemName == itemName)", vidListsVar + "." + typeListName));
            dbNew.Insert(gie2Index + offset, "\t\t\t\t\t{");
            dbNew.Insert(gie2Index + offset, "\t\t\t\t\t" + string.Format("for (int i = 0; i < {0}.Count; i++)", vidListsVar + "." + typeListName));
            dbNew.Insert(gie2Index + offset, "\n\t\t\t\t" + string.Format("case ItemType.{0}:", typeName));
            offset += 7;

            //Add Code to 'Get Random Item' method
            dbNew.Insert(grieIndex + offset, "\t\t\t\t\t" + string.Format("return {0}[Random.Range(0, {0}.Count)];", vidListsVar + "." + typeListName));
            dbNew.Insert(grieIndex + offset, "\t\t\t\t\t}");
            dbNew.Insert(grieIndex + offset, "\t\t\t\t\t\treturn null;");
            dbNew.Insert(grieIndex + offset, "\t\t\t\t\t\tDebug.LogError(type.ToString() + \" has no items in it\");");
            dbNew.Insert(grieIndex + offset, "\t\t\t\t\t{");
            dbNew.Insert(grieIndex + offset, "\t\t\t\t\t" + string.Format("if ({0}.Count == 0)", vidListsVar + "." + typeListName));
            dbNew.Insert(grieIndex + offset, "\n\t\t\t\t" + string.Format("case ItemType.{0}:", typeName));
            offset += 7;

            //Add Code to 'Add missing IDs' method
            dbNew.Insert(amideIndex + offset, "\t\t\t\t\t" + string.Format("AddID({0}[i].itemID, ItemType.{1});", vidListsVar + "." + typeListName, typeName));
            dbNew.Insert(amideIndex + offset, "\t\t\t\t" + string.Format("if (!ItemExists({0}[i].itemID))", vidListsVar + "." + typeListName));
            dbNew.Insert(amideIndex + offset, "\t\t\t" + string.Format("for (int i = 0; i < {0}.Count; i++)", vidListsVar + "." + typeListName));
            dbNew.Insert(amideIndex + offset, "\n\t\t\t" + string.Format("//{0} items", typeName));
            offset += 4;

            //Add Code to 'Remove extra IDs' method
            dbNew.Insert(reideIndex + offset, "\t\t\t\t\t\tbreak;");
            dbNew.Insert(reideIndex + offset, "\t\t\t\t\t\t}");
            dbNew.Insert(reideIndex + offset, "\t\t\t\t\t\t\t}");
            dbNew.Insert(reideIndex + offset, "\t\t\t\t\t\t\t\tbreak;");
            dbNew.Insert(reideIndex + offset, "\t\t\t\t\t\t\t\tremoveKey = false;");
            dbNew.Insert(reideIndex + offset, "\t\t\t\t\t\t\t{");
            dbNew.Insert(reideIndex + offset, "\t\t\t\t\t\t\t" + string.Format("if ({0}[j].itemID == {1}.usedIDs[i])", vidListsVar + "." + typeListName, vidListsVar));
            dbNew.Insert(reideIndex + offset, "\t\t\t\t\t\t{");
            dbNew.Insert(reideIndex + offset, "\t\t\t\t\t\t" + string.Format("for (int j = 0; j < {0}.Count; j++)", vidListsVar + "." + typeListName));
            dbNew.Insert(reideIndex + offset, "\t\t\t\t\t" + string.Format("case ItemType.{0}:", typeName));
            dbNew.Insert(reideIndex + offset, "\n\t\t\t\t\t" + string.Format("//{0}", typeName));
        }

        //Remove item type code
        else if (mode == Mode.RemoveType)
        {
            for (int i = 0; i < db.Length; i++)
            {
                dbNew.Add(db[i]);

                if (db[i].Contains("//#VID-GIIB"))
                    giibIndex = i;
                else if (db[i].Contains("//#VID-GIIE"))
                    giieIndex = i;
                else if (db[i].Contains("//#VID-2GIIB"))
                    giib2Index = i;
                else if (db[i].Contains("//#VID-2GIIE"))
                    giie2Index = i;
                else if (db[i].Contains("//#VID-GIB"))
                    gibIndex = i;
                else if (db[i].Contains("//#VID-GIE"))
                    gieIndex = i;
                else if (db[i].Contains("//#VID-2GIB"))
                    gib2Index = i;
                else if (db[i].Contains("//#VID-2GIE"))
                    gie2Index = i;
                else if (db[i].Contains("//#VID-GRIB"))
                    gribIndex = i;
                else if (db[i].Contains("//#VID-GRIE"))
                    grieIndex = i;
                else if (db[i].Contains("//#VID-AMIDB"))
                    amidbIndex = i;
                else if (db[i].Contains("//#VID-AMIDE"))
                    amideIndex = i;
                else if (db[i].Contains("//#VID-REIDB"))
                    reidbIndex = i;
                else if (db[i].Contains("//#VID-REIDE"))
                    reideIndex = i;
            }

            //Get Item Index method
            for (int i = giibIndex; i < giieIndex - offset; i++)
            {
                if (dbNew[i].Contains(typeName + ":"))
                {
                    dbNew.RemoveRange(i, 7);
                    offset += 7;

                    //If this line is empty remove it, otherwise remove the line before it.(This is to handle cases where we are removing the bottom 'case')
                    if (dbNew[i].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i);
                        offset++;
                    }

                    else if (dbNew[i - 1].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i - 1);
                        offset++;
                    }
                    break;
                }
            }

            //Get Item Index Method Overload
            for (int i = giib2Index - offset - 1; i < giie2Index - offset; i++)
            {
                if (dbNew[i].Contains(typeName + ":"))
                {
                    dbNew.RemoveRange(i, 7);
                    offset += 7;

                    if (dbNew[i].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i);
                        offset++;
                    }

                    else if (dbNew[i - 1].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i - 1);
                        offset++;
                    }
                    break;
                }
            }

            //Get Item method
            for (int i = gibIndex - offset - 1; i < gieIndex - offset; i++)
            {
                if (dbNew[i].Contains(typeName + ":"))
                {
                    dbNew.RemoveRange(i, 7);
                    offset += 7;

                    if (dbNew[i].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i);
                        offset++;
                    }

                    else if (dbNew[i - 1].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i - 1);
                        offset++;
                    }
                    break;
                }
            }

            //Get Item method overload
            for (int i = gib2Index - offset - 1; i < gie2Index - offset; i++)
            {
                if (dbNew[i].Contains(typeName + ":"))
                {
                    dbNew.RemoveRange(i, 7);
                    offset += 7;

                    if (dbNew[i].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i);
                        offset++;
                    }

                    else if (dbNew[i - 1].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i - 1);
                        offset++;
                    }
                    break;
                }
            }

            //Get Random Item method
            for (int i = gribIndex - offset - 1; i < grieIndex - offset; i++)
            {
                if (dbNew[i].Contains(typeName + ":"))
                {
                    dbNew.RemoveRange(i, 7);
                    offset += 7;

                    if (dbNew[i].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i);
                        offset++;
                    }

                    else if (dbNew[i - 1].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i - 1);
                        offset++;
                    }
                    break;
                }
            }

            //Add Missing IDs method
            for (int i = amidbIndex - offset - 1; i < amideIndex - offset; i++)
            {
                if (dbNew[i].Contains(typeListName + "."))
                {
                    dbNew.RemoveRange(i, 3);
                    dbNew.RemoveAt(i - 1);
                    offset += 4;

                    if (dbNew[i - 1].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i - 1);
                        offset++;
                    }

                    else if (dbNew[i - 2].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i - 2);
                        offset++;
                    }
                    break;
                }
            }

            //Remove Extra IDs method
            for (int i = reidbIndex - offset - 1; i < reideIndex - offset; i++)
            {
                if (dbNew[i].Contains(typeName + ":"))
                {
                    dbNew.RemoveRange(i, 10);
                    dbNew.RemoveAt(i - 1);

                    if (dbNew[i - 1].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i - 1);
                    }

                    else if (dbNew[i - 2].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i - 2);
                    }
                    break;
                }
            }
        }

        //Write new code
        File.WriteAllLines(scriptsDir + itemDatabaseFile, dbNew.ToArray());
    }

    /// <summary>
    /// Generate code for the 'ItemDatabaseEditorWindow' class
    /// </summary>
    void HandleItemDatabaseWindowCode()
    {
        string[] db = File.ReadAllLines(scriptsDir + @"/Editor" + editorWindowFile);
        List<string> dbNew = new List<string>();

        int spIndex = -1, aspeIndex = -1, splaeIndex = -1, msbIndex = -1, msbIndex2 = -1, aieIndex = -1, giaieIndex = -1, gfnlbIndex = -1, gfnleIndex = -1;
        int offset = 0;

        for (int i = 0; i < db.Length; i++)
        {
            dbNew.Add(db[i]);

            //The '-1' checks in some places is so for codes that have similar starting letters
            if (spIndex == -1 && db[i].Contains("//#VID-SP"))
                spIndex = i;
            else if (db[i].Contains("//#VID-ASPE"))
                aspeIndex = i;
            else if (splaeIndex == -1 && db[i].Contains("//#VID-SPLAE"))
                splaeIndex = i;
            else if (db[i].Contains("//#VID-MSB"))
                msbIndex = i;
            else if (db[i].Contains("//#VID-2MSB"))
                msbIndex2 = i;
            else if (db[i].Contains("//#VID-AIE"))
                aieIndex = i;
            else if (db[i].Contains("//#VID-GIAIE"))
                giaieIndex = i;
            else if (db[i].Contains("//#VID-GFNLB"))
                gfnlbIndex = i;
            else if (db[i].Contains("//#VID-GFNLE"))
                gfnleIndex = i;
        }

        //int numberOfTypes = int.Parse(dbNew[lspIndex + offset].Split(new char[] { '[', ']' })[3]);

        if (mode == Mode.AddNew || mode == Mode.AddExisting)
        {
            //Add serialized property variable
            dbNew.Insert(spIndex, "\tSerializedProperty " + typeListName + ";");
            offset++;

            //dbNew[lspIndex + offset] = string.Format("\tSerializedProperty[] itemLists = new SerializedProperty[{0}];//#VID-LSP", numberOfTypes + 1);

            //Assign serialized property variable
            dbNew.Insert(aspeIndex, string.Format("\t\t{0} = vidListsSerialized.FindProperty(\"{0}\");", typeListName));
            offset++;

            //Assigne SP vars to SP array
            dbNew.Insert(splaeIndex + offset, string.Format("\t\titemLists[(int)ItemType.{0}] = {1};", typeName, typeListName));
            offset++;

            //Add code to first check in the mini side buttons method
            dbNew.Insert(msbIndex + offset, string.Format("\t\t\t\t\t\tbreak;"));
            dbNew.Insert(msbIndex + offset, string.Format("\t\t\t\t\t\t" + string.Format("{0}.{1}.RemoveAt(index);", vidListsVar, typeListName)));
            dbNew.Insert(msbIndex + offset, string.Format("\t\t\t\t\t" + string.Format("case (int)ItemType.{0}:", typeName)));
            offset += 3;

            //Add code to second check in the mini side buttons method
            dbNew.Insert(msbIndex2 + offset, "\t\t\t\t\tbreak;");
            dbNew.Insert(msbIndex2 + offset, "\t\t\t\t\t" + string.Format("{0}.{1}.Insert(index, ({2})itemCont.item);", vidListsVar, typeListName, typeClassName));
            dbNew.Insert(msbIndex2 + offset, "\t\t\t\t" + string.Format("case ItemType.{0}:", typeName));
            offset += 3;

            //Add code to 'Add Item' method
            dbNew.Insert(aieIndex + offset, "\t\t\t\tbreak;");
            dbNew.Insert(aieIndex + offset, "\t\t\t\t" + string.Format("itemToShow.itemToShowIndex = {0}.{1}.Count - 1;", vidListsVar, typeListName));
            dbNew.Insert(aieIndex + offset, "\t\t\t\t" + string.Format("{0}.{1}.Add({2});", vidListsVar, typeListName, typeListName + "VAR"));
            dbNew.Insert(aieIndex + offset, "\t\t\t\t");
            dbNew.Insert(aieIndex + offset, "\t\t\t\t" + string.Format("{0}.itemType = ItemType.{1};", typeListName + "VAR", typeName));
            dbNew.Insert(aieIndex + offset, "\t\t\t\t" + string.Format("{0}.itemID = database.GetNewID(ItemType.{1});", typeListName + "VAR", typeName));
            dbNew.Insert(aieIndex + offset, "\t\t\t\t" + string.Format("{0} {1} = new {0}();", typeClassName, typeListName + "VAR"));
            dbNew.Insert(aieIndex + offset, "\n\t\t\t" + string.Format("case ItemType.{0}:", typeName));
            offset += 8;

            //Add code to 'Get Item At Index' method
            dbNew.Insert(giaieIndex + offset, "\t\t\t\t" + string.Format("return {0}.{1}[index];", vidListsVar, typeListName));
            dbNew.Insert(giaieIndex + offset, "\t\t\t" + string.Format("case (int)ItemType.{0}:", typeName));
            offset += 2;

            //Add code to 'Get Formatted Names List' method
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\tbreak;");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t}");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t\t\t" +
                "itemNames.Add(" + "\"\\t\\t\"" + " + itemName.Replace(\" \", \"\") + \" = \" + autoVidLists." + typeListName + "[i].itemID + \",\");");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t\t");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t\t}");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t\t\tcontinue;");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t\t\tinvalidList.Add(itemName);");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t\t{");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t\tif (!hash.Add(itemName) || !char.IsLetter(itemName[0]))");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t\t");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t\t\tcontinue;");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t\tif (itemName == string.Empty)");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t\t");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t\titemName = autoVidLists." + typeListName + "[i].itemName.Trim();");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\t{");
            dbNew.Insert(gfnleIndex + offset, "\t\t\t\tfor (int i = 0; i < autoVidLists." + typeListName + ".Count; i++)");
            dbNew.Insert(gfnleIndex + offset, "\t\t\tcase ItemType." + typeName + ":");
        }

        //Delete
        else
        {
            //Remove Serialized property(SP) variable
            for (int i = spIndex - 1; i > 0; i--)
            {
                if (dbNew[i].Contains(typeListName + ";"))
                {
                    dbNew.RemoveAt(i);
                    offset++;
                    break;
                }
            }

            //Decrement the SP array size
            //dbNew[lspIndex - offset] = string.Format("\tSerializedProperty[] itemLists = new SerializedProperty[{0}];//#VID-LSP", numberOfTypes - 1);

            //Remove SP assignment
            for (int i = aspeIndex - offset - 1; i > 0; i--)
            {
                //Handle different formatting
                if (dbNew[i].Contains(typeListName + " =") || dbNew[i].Contains(typeListName + "="))
                {
                    dbNew.RemoveAt(i);
                    offset++;
                    break;
                }
            }

            //Remove SP array assignment
            for (int i = splaeIndex - offset - 1; i > 0; i--)
            {
                if (dbNew[i].Contains(typeListName + ";"))
                {
                    dbNew.RemoveAt(i);
                    offset++;
                    break;
                }
            }

            //Remove code from mini side buttons method
            for (int i = msbIndex - offset - 1; i > 0; i--)
            {
                if (dbNew[i].Contains("ItemType." + typeName + ":"))
                {
                    dbNew.RemoveRange(i, 3);
                    offset += 3;
                    break;
                }
            }

            //Remove second code from mini side buttons method
            for (int i = msbIndex2 - offset - 1; i > 0; i--)
            {
                if (dbNew[i].Contains("ItemType." + typeName + ":"))
                {
                    dbNew.RemoveRange(i, 3);
                    offset += 3;
                    break;
                }
            }

            //Remove code from add item method
            for (int i = aieIndex - offset - 1; i > 0; i--)
            {
                if (dbNew[i].Contains("case") && dbNew[i].Contains(typeName + ":"))
                {
                    dbNew.RemoveRange(i, 8);
                    offset += 8;

                    if (dbNew[i].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i);
                        offset++;
                    }

                    else if (dbNew[i - 1].Trim() == string.Empty)
                    {
                        dbNew.RemoveAt(i - 1);
                        offset++;
                    }
                    break;
                }
            }

            //Remove code from 'Get Item At Index' method
            for (int i = giaieIndex - offset - 1; i > 0; i--)
            {
                //Extra check to handle differnt formatting
                if (dbNew[i].Contains("ItemType." + typeName + ":"))
                {
                    dbNew.RemoveRange(i, 2);
                    offset += 2;
                    break;
                }
            }

            //Remove code from 'Get Formatted Names List' method
            for (int i = gfnlbIndex - offset + 1; i < gfnleIndex - offset; i++)
            {
                if (dbNew[i].Contains("ItemType." + typeName + ":"))
                {
                    dbNew.RemoveRange(i, 17);
                    break;
                }
            }
        }

        //Write new code
        File.WriteAllLines(scriptsDir + @"/Editor" + editorWindowFile, dbNew.ToArray());
    }
}