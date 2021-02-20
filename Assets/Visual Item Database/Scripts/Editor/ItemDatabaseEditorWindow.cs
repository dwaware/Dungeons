using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using ItemSystem;
using ItemSystem.Database;

public partial class ItemDatabaseEditorWindow : EditorWindow
{
    SerializedObject databaseSerialized, vidListsSerialized;

	SerializedProperty autoMeleeWeapon;
    //#VID-SP

    SerializedProperty[] itemLists = new SerializedProperty[System.Enum.GetNames(typeof(ItemType)).Length];//#VID-LSP
    ItemDatabaseV3 database;
    VIDItemListsV3 autoVidLists;
    Vector2 buttonAreaScrollPos = Vector2.zero;
    Vector2 itemAreaScrollPos = Vector2.zero;
    ItemToShow itemToShow = new ItemToShow();
    GameObject objectToAddTo = null;

    List<int> categoryNamesTypeIndex = new List<int>();
    string subtypesOfCurrentItem;
    string[] categoryNamesArray, subtypeNames, typeGroupNames;

    string objectToFind = string.Empty;
    static readonly string databasePath = @"Assets/Visual Item Database/Resources/" + ItemDatabaseV3.dbName + ".asset";   //Path of the item database
    readonly string itemPrefabsPath = @"Assets/Visual Item Database/Prefabs/Item Prefabs/"; //Path that contains the category folders
    static readonly string vidListsPath = @"Assets/Visual Item Database/Resources/" + VIDItemListsV3.itemListsName + ".asset"; //Path that contains the category folders
    readonly string vidListsScriptPath = @"Assets/Visual Item Database/Scripts/" + VIDItemListsV3.itemListsName + ".cs"; //Path that contains the category folders
    int itemsPerPage = 100; //Don't go too high, otherwise it might become laggy
    int currentPage = 1, selectedSubtype = 0, selectedTypeGroup = 0;
    /// <summary>The subtype to use when adding/removing an item from a subtype</summary>
    int subtypeToUse = 0;

    bool addedSubtype = false, removedSubtype = false, failedToAddSubtype = false, failedToRemoveSubtype = false;

    bool confirmBeforeDeleting = true;
    bool displayLogMessages = true;
    bool showItemPositions = true;
    bool deletePrefabWithItem = false;
    bool updateNamesOnExit = true;
    bool itemWasDeleted = false;    //Whether the current item being drawn(in the side window) was deleted
    bool noCategories = true;
    public bool reloadRequested = false;

    /// <summary>
    /// Creates the VIDItemLists and ItemDatabase assets in the resources folder if they do not exist
    /// </summary>
    public static void CreateItemDatabase()
    {
        //Create vid lists if it doesn't exist
        if (!AssetDatabase.LoadAssetAtPath(vidListsPath, typeof(VIDItemListsV3)))
        {
            AssetDatabase.CreateAsset(CreateInstance<VIDItemListsV3>(), vidListsPath);
            AssetDatabase.DeleteAsset(databasePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("'VIDItemListsV3' asset created in Resources folder");
        }

        //If an ItemDatabse doesn't exists then create one(We don't use the generic function to support older unity versions)
        if (!AssetDatabase.LoadAssetAtPath(databasePath, typeof(ItemDatabaseV3)))
        {
            //Clear those keys
            EditorPrefs.DeleteKey("ItemToShowIndex");
            EditorPrefs.DeleteKey("ListIndex");

            ScriptableObject database = CreateInstance<ItemDatabaseV3>();
            AssetDatabase.CreateAsset(database, databasePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("'" + ItemDatabaseV3.dbName + "' created in Resources folder");
        }
    }

    [MenuItem("Assets/Visual Item Database/Open ItemDatabase window %#d", priority = 0)]  //Control-Shift-D as hotkey
    public static ItemDatabaseEditorWindow OpenDatabaseWindow()
    {
        //If itemDatabse doesn't exist then create it
        if (!AssetDatabase.LoadAssetAtPath(databasePath, typeof(ItemDatabaseV3)) || !AssetDatabase.LoadAssetAtPath(vidListsPath, typeof(VIDItemListsV3)))
            CreateItemDatabase();

        //If the user database script doesn't exist create it
        string editorPath = @"Assets/Visual Item Database/Scripts/Editor/";
        if (!File.Exists(editorPath + "ItemDatabaseEditorWindowUser.cs"))
        {
            File.Copy(editorPath + "ItemDatabaseEditorWindowUser.txt", editorPath + "ItemDatabaseEditorWindowUser.cs", false);
            AssetDatabase.Refresh();
        }

        if (!ItemSystemUtility.itemDatabase)
            ItemSystemUtility.LoadItemDatabase();

        //Close this window
        GetWindow<ItemTypeEditorWindow>().Close();

        //Gets the window if there is one, or creates a new one if there is none
        ItemDatabaseEditorWindow window = GetWindow<ItemDatabaseEditorWindow>(false, "Visual Item Database", true);
        window.minSize = new Vector2(900, 600);
        window.Show();

        return window;
    }

    [MenuItem("Assets/Visual Item Database/About", priority = 3)]
    public static void About()
    {
        EditorUtility.DisplayDialog("About", "The Visual Item Database\nAn Asset by: bloeys\nVersion: 3.0", "Close");
    }

    void OnEnable()
    {
        //Load database
        autoVidLists = (VIDItemListsV3)AssetDatabase.LoadAssetAtPath(vidListsPath, typeof(VIDItemListsV3));
        database = (ItemDatabaseV3)AssetDatabase.LoadAssetAtPath(databasePath, typeof(ItemDatabaseV3));

        //Get a serialized object for the database
        databaseSerialized = new SerializedObject(database);
        vidListsSerialized = new SerializedObject(autoVidLists);

		autoMeleeWeapon = vidListsSerialized.FindProperty("autoMeleeWeapon");
        //Get a serialized object and a property for the items list
        //#VID-ASPE

        //#VID-SPLAB
		itemLists[(int)ItemType.MeleeWeapon] = autoMeleeWeapon;
        //#VID-SPLAE

        UpdateGategoryData();

        //Get group names
        typeGroupNames = new string[autoVidLists.typeGroups.Count + 1];
        typeGroupNames[0] = "None";
        for (int i = 1; i < typeGroupNames.Length; i++)
            typeGroupNames[i] = autoVidLists.typeGroups[i - 1].name;

        //Create item prefabs folder if it doesn't exist
        if (!Directory.Exists(itemPrefabsPath))
        {
            Directory.CreateDirectory(itemPrefabsPath);
            AssetDatabase.Refresh();
        }

        //Load settings
        LoadEditorSettings();

        //Handle deleting categories(the loaded index might be bigger since it points to a deleted category)
        if (itemToShow.listIndex >= itemLists.Length)
            itemToShow.listIndex = 0;

        if (itemToShow.categoryIndex >= categoryNamesArray.Length)
            itemToShow.categoryIndex = 0;

        //Load subtypes
        subtypeNames = GetSubtypeNames();

        //Validate the database
        database.ValidateDatabase();

        //This makes sure we validate the database everytime a redo or undo is done
        Undo.undoRedoPerformed += database.ValidateDatabase;


        if (database.ItemExists(itemToShow.itemToShowIndex))
            UpdateCurrentItemSubtypes(GetItemAtIndex(itemToShow.itemToShowIndex).itemID);
    }

    void OnGUI()
    {
        if (noCategories)
        {
            GUILayout.BeginVertical("Box");
            GUILayout.Label("There are no item types to show, please add item types from the 'Item Type Control' window.\nIf you have downloaded this as an update and have items in your old database, then please use the 'Version Updater' window by clicking on\n'Update Database Version' to move your items.\n\nMore details on the update process are available in the 'Transfer' section of the documentation.");
            GUILayout.EndVertical();
            return;
        }

        databaseSerialized.UpdateIfDirtyOrScript();
        vidListsSerialized.UpdateIfDirtyOrScript();

        //Options category buttons area
        EditorGUILayout.BeginVertical();

        DrawOptionsArea();
        DrawItemTypeButtons();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        //Side and item areas
        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

        DrawSideArea();
        DrawItemArea();
        EditorGUILayout.EndHorizontal();

        //We do a reload at the end of OnGUI to prevent any errors that might happen if we do a reload midway through some operation
        if (reloadRequested)
        {
            LoadEditorSettings();
            reloadRequested = false;
        }
        databaseSerialized.ApplyModifiedProperties();
        vidListsSerialized.ApplyModifiedProperties();
    }

    #region Draw methods
    void DrawSideArea()
    {
        int listSize = itemLists[itemToShow.listIndex].arraySize;

        EditorGUILayout.BeginVertical("Box", GUILayout.Width(325)); //Make vertical area for side view

        //Category and item count
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total items: " + listSize, EditorStyles.label);
        EditorGUILayout.EndHorizontal();

        //Subtype selection
        int oldSubtype = selectedSubtype;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Subtype:", GUILayout.Width(130));
        selectedSubtype = EditorGUILayout.Popup(selectedSubtype, subtypeNames);
        EditorGUILayout.EndHorizontal();

        //If the subtype was changed reset some things
        if (oldSubtype != selectedSubtype)
        {
            currentPage = 1;
            itemToShow.itemToShowIndex = -1;
        }

        //If the subtype is not zero then the list size is the number of items in that subtype
        if (selectedSubtype != 0)
            listSize = autoVidLists.subtypes[GetSubtypeIndex(subtypeNames[selectedSubtype])].itemIDs.Count;

        //Search field
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search Category For:", GUILayout.Width(130));
        objectToFind = GUILayout.TextField(objectToFind);
        GUILayout.EndHorizontal();

        //Make it scrollable
        buttonAreaScrollPos = EditorGUILayout.BeginScrollView(buttonAreaScrollPos, false, false, GUILayout.ExpandWidth(true));

        int oldItemsPerPage = itemsPerPage;
        int numberOfPages = Mathf.CeilToInt((float)listSize / itemsPerPage);
        if (numberOfPages == 0)
            numberOfPages = 1;

        //Make sure we don't show a page that isn't there(might happen when deleting items causes page count to decrease)
        if (currentPage > numberOfPages)
            currentPage = numberOfPages;

        int oldCurrentPage = currentPage;

        bool searching = false;
        //If list has something
        if (listSize > 0)
        {
            //If we are searching, then we change these values to get a dirty way of searching the whole category
            if (objectToFind.Trim() != string.Empty && objectToFind.Length >= 3)
            {
                searching = true;
                currentPage = 1;
                itemsPerPage = 1000000;
            }

            EditorGUILayout.Space();

            //Loop and show all buttons in page
            for (int elementIndex = (currentPage - 1) * itemsPerPage;
                elementIndex < Mathf.Clamp(((currentPage - 1) * itemsPerPage) + itemsPerPage, 0, listSize);
                elementIndex++)
            {
                ItemBase item = GetItemAtIndex(elementIndex);

                //If we want to search for an object then don't show a button unless it contains what we want
                if (searching && !item.itemName.ToLower().Contains(objectToFind.ToLower()))
                    continue;

                EditorGUILayout.BeginHorizontal("Box");

                //Show element position
                if (showItemPositions)
                    GUILayout.Label((elementIndex + 1).ToString(), GUILayout.MaxWidth(32));

                //Show icon
                GUILayout.Label(AssetPreview.GetAssetPreview(item.itemIcon), GUILayout.Width(32), GUILayout.Height(32));

                //Draw mini buttons near item names
                MiniSideButtonsControl(elementIndex);

                //If the item was deleted then stop here
                if (!itemWasDeleted)
                {
                    //Make selected button a different color
                    if (elementIndex == itemToShow.itemToShowIndex)
                        GUI.color = Color.yellow;

                    //Side item button
                    if (GUILayout.Button(item.itemName == string.Empty ? "Item Not Named" : item.itemName, GUILayout.Width(170)))
                    {
                        itemToShow.itemToShowIndex = elementIndex;
                        UpdateCurrentItemSubtypes(item.itemID); //Show subtypes for this item

                        //To fix bug where values wont update if field is selected
                        GUIUtility.keyboardControl = 0;

                        //Save to store which item is being looked at
                        SaveEditorSettings();
                    }
                    GUI.color = Color.white;    //Reset color
                }

                //This is to fix a bug where an error happens if we delete an item on the last page
                else
                {
                    EditorGUILayout.EndHorizontal();    //Ofcourse we don't forget to end the horizontal ;)
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.EndScrollView();    //End here so the page buttons always stay where they are

        itemsPerPage = EditorGUILayout.IntSlider("Items Per Page:", itemsPerPage, 10, 200);
        numberOfPages = Mathf.CeilToInt((float)listSize / itemsPerPage);    //Update number of pages since we might have added/removed an item
        if (numberOfPages == 0)
            numberOfPages = 1;

        currentPage = EditorGUILayout.IntSlider("Current Page: ", currentPage, 1, numberOfPages);
        EditorGUILayout.BeginHorizontal();

        //Previous page button
        if (GUILayout.Button("\u25C0", EditorStyles.miniButtonLeft))
            currentPage = Mathf.Clamp(--currentPage, 1, numberOfPages);

        //Next page button
        if (GUILayout.Button("\u25B6", EditorStyles.miniButtonRight))
            currentPage = Mathf.Clamp(++currentPage, 1, numberOfPages);

        EditorGUILayout.EndHorizontal();

        //Add Item Button
        GUI.color = Color.green;
        if (GUILayout.Button("Add New Item"))
            AddItem();

        //Update item name enum buttons
        GUI.color = Color.yellow;
        if (GUILayout.Button("Update Names Enum For This Type"))
            UpdateItemNamesEnum(false);
        if (GUILayout.Button("Update Names Enum For All Types"))
            UpdateItemNamesEnum(true);
        GUI.color = Color.white;

        //Show page count
        EditorGUILayout.LabelField("Page: " + currentPage + "/" + numberOfPages);
        EditorGUILayout.EndVertical();

        //If we were searching make sure we return these values to what they were
        if (searching)
        {
            itemsPerPage = oldItemsPerPage;
            currentPage = oldCurrentPage;
        }

        //If something changed(like items per page) then save
        if (GUI.changed)
        {
            SaveEditorSettings();
            ResetSubtypeStates();
        }
    }

    void DrawItemArea()
    {
        EditorGUILayout.BeginVertical("Box", GUILayout.ExpandWidth(true));
        itemAreaScrollPos = EditorGUILayout.BeginScrollView(itemAreaScrollPos, false, false);
        SerializedProperty item = null;   //Get selected item

        //Draw item
        if (itemToShow.itemToShowIndex != -1 && itemToShow.itemToShowIndex < itemLists[itemToShow.listIndex].arraySize)
        {
            ItemBase i = GetItemAtIndex(itemToShow.itemToShowIndex);

            //If this is a subtype then the itemToShowIndex is used as an index of a subtype list, not of a type list
            if (selectedSubtype == 0)
                item = itemLists[itemToShow.listIndex].GetArrayElementAtIndex(itemToShow.itemToShowIndex);
            else
                item = itemLists[itemToShow.listIndex].GetArrayElementAtIndex(database.GetItemIndex(i));

            item.isExpanded = true;
            EditorGUILayout.LabelField("Item ID: " + item.FindPropertyRelative("itemID").intValue.ToString());
            EditorGUILayout.PropertyField(item, true);
            AfterItemDrawing(i);    //Custom user stuff after the item data

            //Make sure the item is expanded
            item.isExpanded = true;

            //Update window
            if (focusedWindow)
                focusedWindow.Repaint();

            GUILayout.Space(20);
            DrawItemAreaButtons(); //Draw all other buttons
            AfterSubtypeButtonsDrawing(i);  //Custom user stuff under all the buttons
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Handles drawing the toggle buttons at the top of the window
    /// </summary>
    void DrawOptionsArea()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("Options");

        confirmBeforeDeleting = GUILayout.Toggle(confirmBeforeDeleting, "Confirm Before Deleting");
        deletePrefabWithItem = GUILayout.Toggle(deletePrefabWithItem, new GUIContent("Delete Prefab With Item", "Whether the Prefab will be Deleted When its Item is Deleted"));
        displayLogMessages = GUILayout.Toggle(displayLogMessages, new GUIContent("Show log messages", "Warning Messages Still Show"));
        showItemPositions = GUILayout.Toggle(showItemPositions, new GUIContent("Show Item Positions", "Whether to Show the Position no. of Items or not"));
        updateNamesOnExit = GUILayout.Toggle(updateNamesOnExit, new GUIContent("Update Name Enums On Window Close", "Whether to Update the Name Enums of All Types When the Window is Closed"));
        GUILayout.EndHorizontal();

        //If a toggle was changed then update settings
        if (GUI.changed)
            SaveEditorSettings();
    }

    void DrawItemTypeButtons()
    {
        int oldListIndex = itemToShow.listIndex;

        EditorGUILayout.BeginHorizontal("Box");
        EditorGUILayout.BeginVertical();
        //if (GUILayout.Button("Test"))
        //    Test();

        itemToShow.categoryIndex = GUILayout.Toolbar(itemToShow.categoryIndex, categoryNamesArray); //Show category selection
        itemToShow.listIndex = categoryNamesTypeIndex[itemToShow.categoryIndex];    //Get a list index based on the selected category

        //Type Group Section
        int oldGroup = selectedTypeGroup;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Type Group:", GUILayout.Width(129));
        selectedTypeGroup = EditorGUILayout.Popup(selectedTypeGroup, typeGroupNames, GUILayout.Width(185));
        EditorGUILayout.EndHorizontal();

        if (oldGroup != selectedTypeGroup)
        {
            UpdateGategoryData();
            oldListIndex = -100;    //To make sure the next if statement becomes true
        }

        //If we changed lists (item type) then reset some things
        if (oldListIndex != itemToShow.listIndex)
        {
            currentPage = 1;
            selectedSubtype = subtypeToUse = 0;
            subtypeNames = GetSubtypeNames();
            GUIUtility.keyboardControl = 0;

            //If list isn't empty then select the first item
            itemToShow.itemToShowIndex = itemLists[itemToShow.listIndex].arraySize == 0 ? -1 : 0;

            if (itemToShow.itemToShowIndex != -1)
                UpdateCurrentItemSubtypes(GetItemAtIndex(itemToShow.itemToShowIndex).itemID);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    void DrawItemAreaButtons()
    {
        int itemID = GetItemAtIndex(itemToShow.itemToShowIndex).itemID;

        GUILayout.BeginHorizontal();

        //Up arrow
        if (GUILayout.Button(new GUIContent("\u25b2", "Moves this entry up"), EditorStyles.miniButtonLeft, GUILayout.Width(100)))
        {
            //If it is already in top position then return
            if (itemToShow.itemToShowIndex == 0)
                return;

            //Move element
            if (selectedSubtype > 0)
            {
                int selectedSubtypeIndex = GetSubtypeIndex(subtypeNames[selectedSubtype]);  //Store this so we can reuse it
                int topElement = autoVidLists.subtypes[selectedSubtypeIndex].itemIDs[itemToShow.itemToShowIndex - 1]; //The element above us

                //Make this element take the position of the element on top of it and then make the top element take the current index(Swap)
                autoVidLists.subtypes[selectedSubtypeIndex].itemIDs[itemToShow.itemToShowIndex - 1] =
                    autoVidLists.subtypes[selectedSubtypeIndex].itemIDs[itemToShow.itemToShowIndex];
                autoVidLists.subtypes[selectedSubtypeIndex].itemIDs[itemToShow.itemToShowIndex] = topElement;
            }
            else
            {
                itemLists[itemToShow.listIndex].MoveArrayElement(itemToShow.itemToShowIndex, itemToShow.itemToShowIndex - 1);
            }

            itemToShow.itemToShowIndex--;
            return;
        }

        //Down arrow
        else if (GUILayout.Button(new GUIContent("\u25bc", "Moves this entry down"), EditorStyles.miniButtonRight, GUILayout.Width(100)))
        {
            if (selectedSubtype > 0)
            {
                int selectedSubtypeIndex = GetSubtypeIndex(subtypeNames[selectedSubtype]);

                //Make sure we are not already bottom
                if (itemToShow.itemToShowIndex == autoVidLists.subtypes[selectedSubtypeIndex].itemIDs.Count - 1)
                    return;

                int botElement = autoVidLists.subtypes[selectedSubtypeIndex].itemIDs[itemToShow.itemToShowIndex + 1]; //Store the element below us

                autoVidLists.subtypes[selectedSubtypeIndex].itemIDs[itemToShow.itemToShowIndex + 1] = autoVidLists.subtypes[selectedSubtypeIndex].itemIDs[itemToShow.itemToShowIndex];
                autoVidLists.subtypes[selectedSubtypeIndex].itemIDs[itemToShow.itemToShowIndex] = botElement;
            }

            else
            {
                //If already at bottom position then return
                if (itemToShow.itemToShowIndex == itemLists[itemToShow.listIndex].arraySize - 1)
                    return;

                //Move element
                itemLists[itemToShow.listIndex].MoveArrayElement(itemToShow.itemToShowIndex, itemToShow.itemToShowIndex + 1);
            }

            itemToShow.itemToShowIndex++;
            return;
        }

        GUILayout.Space(100);

        GUI.color = Color.cyan;    //Make update prefab buttons cyan
        GUILayout.BeginVertical();

        //Update prefab button
        if (GUILayout.Button(new GUIContent("Update Prefab", "Updates item prefab with new values, or creates a new one if none exist"),
            GUILayout.Width(120)))
            PrefabButtonClicked();

        //Update category button
        if (GUILayout.Button(new GUIContent("Update Type Prefabs", "Updates the prefabs of the entire selected type/subtype"),
            GUILayout.Width(158)))
            UpdateCategoryClicked();

        GUILayout.BeginHorizontal();

        //Add item to object button
        if (GUILayout.Button(new GUIContent("Update Object", "Adds item to object as a component. Update the item if its already there"), GUILayout.Width(120)) &&
            objectToAddTo != null)
        {
            ItemBase currentItem = GetItemAtIndex(itemToShow.itemToShowIndex);
            ItemContainer[] containers = objectToAddTo.GetComponents<ItemContainer>();  //Get list of containers
            bool found = false;

            //If the item exists update it
            for (int i = 0; i < containers.Length; i++)
            {
                if (currentItem.itemID == containers[i].item.itemID)
                {
                    found = true;
                    containers[i].item = ItemSystemUtility.GetItemCopy(currentItem.itemID, currentItem.itemType);
                    break;
                }
            }

            //Otherwise add it
            if (!found)
                objectToAddTo.AddComponent<ItemContainer>().item = ItemSystemUtility.GetItemCopy(currentItem.itemID, currentItem.itemType);
        }

        GUI.color = Color.white;
        objectToAddTo = (GameObject)EditorGUILayout.ObjectField(objectToAddTo, typeof(GameObject), true, GUILayout.Width(200)); //Object field

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.EndHorizontal();

        //Align jump button with the update object button
        GUILayout.Space(-20);

        GUILayout.BeginVertical();
        GUI.color = Color.yellow;
        if (GUILayout.Button("Jump To Item Page", GUILayout.Width(150)))
        {
            //Stop searching
            objectToFind = string.Empty;

            currentPage = Mathf.CeilToInt((float)(itemToShow.itemToShowIndex + 1) / itemsPerPage);  //Jump to page
            SaveEditorSettings();   //Save changes(new page)
        }

        GUILayout.Space(10);
        GUI.color = Color.white;

        //Subtype selection
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Subtype To Use:", GUILayout.Width(100));
        subtypeToUse = EditorGUILayout.Popup(subtypeToUse, subtypeNames, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        //Get subtype index of the subtype we want to act on
        int subtypeIndex = GetSubtypeIndex(subtypeNames[subtypeToUse]);

        //Remove from subtype button
        GUILayout.BeginHorizontal();
        GUI.color = Color.yellow;
        if (GUILayout.Button("Remove From Subtype", GUILayout.Width(150)) && subtypeToUse > 0)
        {
            bool success = false;
            for (int i = 0; i < autoVidLists.subtypes[subtypeIndex].itemIDs.Count; i++)
            {
                if (autoVidLists.subtypes[subtypeIndex].itemIDs[i] == itemID)
                {
                    autoVidLists.subtypes[subtypeIndex].itemIDs.RemoveAt(i);
                    success = true;

                    //This is so we don't get out of range errors
                    if (selectedSubtype != 0)
                        itemToShow.itemToShowIndex--;
                    break;
                }
            }

            ResetSubtypeStates();
            if (success)
                removedSubtype = true;
            else
                failedToRemoveSubtype = true;
        }

        //Add to subtype button
        GUI.color = Color.green;
        if (GUILayout.Button("Add To Subtype", GUILayout.Width(150)) && subtypeToUse > 0)
        {
            bool hasItem = false;
            ResetSubtypeStates();

            for (int i = 0; i < autoVidLists.subtypes[subtypeIndex].itemIDs.Count; i++)
            {
                if (autoVidLists.subtypes[subtypeIndex].itemIDs[i] == itemID)
                {
                    hasItem = true;
                    failedToAddSubtype = true;
                    break;
                }
            }

            if (!hasItem)
            {
                autoVidLists.subtypes[subtypeIndex].itemIDs.Add(itemID);
                addedSubtype = true;
            }
        }

        GUILayout.EndHorizontal();

        if (addedSubtype || removedSubtype)
            UpdateCurrentItemSubtypes(itemID);

        GUILayout.Label(subtypesOfCurrentItem);
        //if (addedSubtype)
        //    GUILayout.Label("Successfully Added Item To Subtype");
        //else if (failedToAddSubtype)
        //    GUILayout.Label("Failed To Add Item ToSubtype. Item Already Added To Subtype");
        //else if (removedSubtype)
        //    GUILayout.Label("Successfully Removed Item From Subtype");
        //else if (failedToRemoveSubtype)
        //    GUILayout.Label("Failed To Remove Item From Subtype. Item Wasn't Added To Subtype");

        GUILayout.EndVertical();
        GUI.color = Color.white;    //Reset gui color
    }

    void MiniSideButtonsControl(int index)
    {
        itemWasDeleted = false;
        //Remove button
        if (GUILayout.Button(new GUIContent("-", "Delete Item"), EditorStyles.miniButtonLeft, GUILayout.Width(20)))
        {
            //If delete is confirmed then remove item entry and clear id
            if (!confirmBeforeDeleting || ItemRemovalConfirmed())
            {
                Undo.RecordObject(autoVidLists, "Delete Object");
                ItemBase item = GetItemAtIndex(index);

                if (selectedSubtype > 0)
                {
                    //If last item in subtype then item to show is -1(none)
                    if (autoVidLists.subtypes[GetSubtypeIndex(subtypeNames[selectedSubtype])].itemIDs.Count == 1)
                        itemToShow.itemToShowIndex = -1;

                    //In this case the index is subtype relative, so we need the actual item index in the main type list
                    index = database.GetItemIndex(item);
                }

                //Remove item from all subtypes
                int subtypeIndex = 0;
                for (int i = 1; i < subtypeNames.Length; i++)
                {
                    subtypeIndex = GetSubtypeIndex(subtypeNames[i]);

                    if (autoVidLists.subtypes[subtypeIndex].itemIDs.Contains(item.itemID))
                        autoVidLists.subtypes[subtypeIndex].itemIDs.Remove(item.itemID);
                }

                //If wanted, delete the prefab along with the item
                if (deletePrefabWithItem && !string.IsNullOrEmpty(item.itemName.Trim()))
                {
                    AssetDatabase.DeleteAsset(itemPrefabsPath +
                        ((ItemType)itemToShow.listIndex).ToString() + "/" + item.itemName + ".prefab");
                }

                database.DeleteID(item.itemID);    //Delete ID
                GUIUtility.keyboardControl = 0;   //Remove control in case anything was selected

                //According to the list index choose item list to remove from (Such things can be replaced by a one liner with serialized objects, 
                //but I didn't use them in a few critical places because with large databases they are so slow they basically hang the editor!)
                switch (itemToShow.listIndex)
                {
					case (int)ItemType.MeleeWeapon:
						autoVidLists.autoMeleeWeapon.RemoveAt(index);
						break;
                }//#VID-MSB

                itemWasDeleted = true;
                vidListsSerialized.Update();    //Update the serialized object to show changes

#if !UNITY_5_3_OR_NEWER
                EditorUtility.SetDirty(autoVidLists);
#endif
            }
        }

        //Duplicate button
        else if (GUILayout.Button(new GUIContent("+", "Duplicate Item"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
        {
            Undo.RecordObject(autoVidLists, "Duplicate Object");

            //Make a temp object and give it the item container script
            ItemContainer itemCont = new GameObject("tempGO").AddComponent<ItemContainer>();
            ItemBase itemToDuplicate = GetItemAtIndex(index);   //Get item to duplicate

            itemCont.item = ItemSystemUtility.GetItemCopy(itemToDuplicate.itemID, itemToDuplicate.itemType);  //Duplicate
            itemCont.item.itemName = "DUPLICATE " + itemCont.item.itemName; //Change name of duplicate item
            itemCont.item.itemID = database.GetNewID(itemToDuplicate.itemType); //Give duplicate item a new ID

            //Add to current subtype
            if (selectedSubtype > 0)
            {
                autoVidLists.subtypes[GetSubtypeIndex(subtypeNames[selectedSubtype])].itemIDs.Insert(index, itemCont.item.itemID);
                index = database.GetItemIndex(itemToDuplicate); //Convert index to a main type index
            }

            //Finally insert the item into the correct item list(category)
            switch (itemToDuplicate.itemType)
            {
				case ItemType.MeleeWeapon:
					autoVidLists.autoMeleeWeapon.Insert(index, (ItemMelee)itemCont.item);
					break;
            }//#VID-2MSB

            DestroyImmediate(itemCont.gameObject);   //Destroy the temp object
            vidListsSerialized.Update();    //Update the serialized object to show these changes

#if !UNITY_5_3_OR_NEWER
            EditorUtility.SetDirty(autoVidLists);
#endif
        }

        if (GUI.changed)
            ResetSubtypeStates();
    }
    #endregion

    #region Prefab Button Methods
    /// <summary>
    /// Called when the update prefab button is clicked
    /// </summary>
    /// <param name="i">Index of current item</param>
    void PrefabButtonClicked()
    {
        //Get item
        ItemBase element = GetItemAtIndex(itemToShow.itemToShowIndex);

        //If for some reason we have an error and the item doesn't exist in the database then make sure its added
        if (!database.ItemExists(element.itemID))
            database.ValidateDatabase();

        //If there is no name stop
        if (string.IsNullOrEmpty(element.itemName) || string.IsNullOrEmpty(element.itemName.Trim()))
        {
            EditorUtility.DisplayDialog("Error", "Name can not be empty!", "OK");
            return;
        }

        string folder = ((ItemType)itemToShow.listIndex).ToString();

        //If a prefab for this item doesn't exist
        if (!(GameObject)AssetDatabase.LoadAssetAtPath(itemPrefabsPath + folder + "/" + element.itemName + ".prefab", typeof(GameObject)))
        {
            CreateItemPrefab(element);  //Create a prefab of the item

            if (displayLogMessages)
                Debug.Log(element.itemName + " Prefab Created");
        }

        //If a prefab with this name already exists
        else
        {
            UpdateItemPrefab(element, folder);  //Update prefab

            if (displayLogMessages)
                Debug.Log(element.itemName + " Prefab Updated");
        }
    }

    /// <summary>
    /// Updates the prefabs of the entire selected category
    /// </summary>
    void UpdateCategoryClicked()
    {
        if (!EditorUtility.DisplayDialog("Are You Sure?", "Are you sure you want to create a prefab of all the items in this type/subtype?", "Yes", "No"))
            return;

        //If in a subtype only update that subtype
        int count = selectedSubtype == 0 ? itemLists[itemToShow.listIndex].arraySize :
            autoVidLists.subtypes[GetSubtypeIndex(subtypeNames[selectedSubtype])].itemIDs.Count;

        string folder = ((ItemType)itemToShow.listIndex).ToString();
        bool someLeft = false;

        GameObject tempGo = new GameObject("tempGO");
        ItemContainer container = tempGo.AddComponent<ItemContainer>();
        ItemContainer loadedPrefabContainer;

        //Slightly different versions of 'CreateItemPrefab' and 'UpdateItemPrefab' are used here instead of simply calling the two methods because this place is
        //performance critical, as we can going through over a few thousand items, and you don't want to wait for a too long ;P
        for (int i = 0; i < count; i++)
        {
            //Get item
            ItemBase item = GetItemAtIndex(i);

            //If there is no name move on
            if (string.IsNullOrEmpty(item.itemName.Trim()))
            {
                someLeft = true;
                continue;
            }

            //Try to load the prefab
            loadedPrefabContainer = (ItemContainer)AssetDatabase.LoadAssetAtPath(itemPrefabsPath + folder + "/" + item.itemName + ".prefab", typeof(ItemContainer));

            //If a prefab for this item doesn't exist
            if (!loadedPrefabContainer)
            {
                container.item = ItemSystemUtility.GetItemCopy(item.itemID, (ItemType)itemToShow.listIndex);   //Make our temp object contain the item

                //Create the prefab and destroy temporary gameobject
                PrefabUtility.CreatePrefab(itemPrefabsPath + folder + "/" + item.itemName + ".prefab", tempGo);
            }

            //If a prefab with this name already exists
            else
            {
                loadedPrefabContainer.item = ItemSystemUtility.GetItemCopy(item.itemID, (ItemType)itemToShow.listIndex);  //Make it become this item, basically updating all its values
                EditorUtility.SetDirty(loadedPrefabContainer.gameObject);   //Do this to make the prefab recognize the update
            }
        }
        DestroyImmediate(tempGo);

        if (displayLogMessages)
            Debug.Log("Done Updating Category");

        if (someLeft)
            Debug.LogWarning("Some items were not updated because their names were empty");
    }

    /// <summary>
    /// Creates an item prefab with settings from passed element
    /// </summary>
    /// <param name="element">Element in items array</param>
    void CreateItemPrefab(ItemBase element)
    {
        //Create a temporary gameobject and add a container
        GameObject tempGo = new GameObject("tempGO");
        ItemContainer item = tempGo.AddComponent<ItemContainer>();

        item.item = ItemSystemUtility.GetItemCopy(element.itemID, (ItemType)itemToShow.listIndex);   //Create item

        tempGo.AddComponent<SpriteRenderer>().sprite = item.item.itemIcon;
        tempGo.GetComponent<SpriteRenderer>().sortingLayerName = "Item";

        //Create the prefab and destroy temporary gameobject
        PrefabUtility.CreatePrefab(itemPrefabsPath + ((ItemType)itemToShow.listIndex).ToString() + "/" + element.itemName + ".prefab", tempGo);

        GameObject.DestroyImmediate(tempGo);
    }

    /// <summary>
    /// Updates prefab properties using settings from passed element
    /// </summary>
    /// <param name="element">Element in items array</param>
    void UpdateItemPrefab(ItemBase element, string folder)
    {
        ItemContainer i = (ItemContainer)AssetDatabase.LoadAssetAtPath(itemPrefabsPath + folder + "/" + element.itemName + ".prefab", typeof(ItemContainer));   //Get the prefab item component
        i.item = ItemSystemUtility.GetItemCopy(element.itemID, (ItemType)itemToShow.listIndex);  //Make it become this item, basically updating all its values

        SpriteRenderer sr = i.GetComponent<SpriteRenderer>();
        if (sr)
        {
            sr.sprite = i.item.itemIcon;
            sr.sortingLayerName = "Item";
        }

        EditorUtility.SetDirty(i.gameObject);   //Do this to make it recognize the update
    }
    #endregion

    /// <summary>
    /// Adds item and switches view to it
    /// </summary>
    void AddItem()
    {
        Undo.RecordObject(autoVidLists, "Add Item");
        switch (((ItemType)itemToShow.listIndex))
        {

			case ItemType.MeleeWeapon:
				ItemMelee autoMeleeWeaponVAR = new ItemMelee();
				autoMeleeWeaponVAR.itemID = database.GetNewID(ItemType.MeleeWeapon);
				autoMeleeWeaponVAR.itemType = ItemType.MeleeWeapon;
				
				autoVidLists.autoMeleeWeapon.Add(autoMeleeWeaponVAR);
				itemToShow.itemToShowIndex = autoVidLists.autoMeleeWeapon.Count - 1;
				break;
        }//#VID-AIE

        //First add the new item to the subtype(works since show index is now the new item and we are forcing main type) and then since we are showing a subtype
        //the show index is reverted to being relative to the subtype item id list, not the main list. If this is not done you will get errors
        if (selectedSubtype > 0)
        {
            autoVidLists.subtypes[GetSubtypeIndex(subtypeNames[selectedSubtype])].itemIDs.Add(GetItemAtIndex(itemToShow.itemToShowIndex, true).itemID);
            itemToShow.itemToShowIndex = autoVidLists.subtypes[GetSubtypeIndex(subtypeNames[selectedSubtype])].itemIDs.Count - 1;
        }

        GUIUtility.keyboardControl = 0;
        itemWasDeleted = true;
        vidListsSerialized.Update();

#if !UNITY_5_3_OR_NEWER
        EditorUtility.SetDirty(autoVidLists);
#endif
    }

    bool ItemRemovalConfirmed()
    {
        return EditorUtility.DisplayDialog("Delete?", "Are you sure you want to remove this item?", "Yes", "No");
    }

    /// <summary>
    /// Returns the item at the current index from the currently selected type or subtype if one is selected
    /// </summary>
    /// <param name="index"></param>
    /// <param name="mainOnly">Whether to only use the passed indexs on main types</param>
    /// <returns></returns>
    ItemBase GetItemAtIndex(int index, bool mainOnly = false)
    {
        //If no subtype is selected
        if (selectedSubtype == 0 || mainOnly)
        {
            switch (itemToShow.listIndex)
            {
			case (int)ItemType.MeleeWeapon:
				return autoVidLists.autoMeleeWeapon[index];
            }//#VID-GIAIE
        }

        //If a subtype is selected then return the item at the give index in the subtype's list, not the main one
        else
        {
            return database.GetItem(autoVidLists.subtypes[GetSubtypeIndex(subtypeNames[selectedSubtype])].itemIDs[index], (ItemType)itemToShow.listIndex);
        }

        return null;
    }

    /// <summary>
    /// Loads settings for option bools. Creates keys for them if no keys are saved.
    /// </summary>
    void LoadEditorSettings()
    {
        if (EditorPrefs.HasKey("ItemToShowIndex"))
        {
            confirmBeforeDeleting = EditorPrefs.GetBool("ConfirmBeforeDeleting");
            displayLogMessages = EditorPrefs.GetBool("DisplayLogMessages");
            showItemPositions = EditorPrefs.GetBool("ShowItemPositions");
            deletePrefabWithItem = EditorPrefs.GetBool("DeletePrefabWithItem");
            updateNamesOnExit = EditorPrefs.GetBool("UpdateNamesOnExit", updateNamesOnExit);
            itemToShow.itemToShowIndex = EditorPrefs.GetInt("ItemToShowIndex");
            itemToShow.listIndex = EditorPrefs.GetInt("ListIndex");
            itemToShow.categoryIndex = EditorPrefs.GetInt("CategoryIndex");
            itemsPerPage = EditorPrefs.GetInt("ItemsPerPage");
            currentPage = EditorPrefs.GetInt("CurrentPage");

            //This is to match the list index with the correct category index when an item wants to be shown
            for (int i = 0; i < categoryNamesTypeIndex.Count; i++)
            {
                if (categoryNamesTypeIndex[i] == itemToShow.listIndex)
                {
                    itemToShow.categoryIndex = i;
                    break;
                }
            }
        }

        else
        {
            SaveEditorSettings();
        }
    }

    /// <summary>
    /// Saves the current states of the option bools
    /// </summary>
    void SaveEditorSettings()
    {
        EditorPrefs.SetBool("ConfirmBeforeDeleting", confirmBeforeDeleting);
        EditorPrefs.SetBool("DisplayLogMessages", displayLogMessages);
        EditorPrefs.SetBool("ShowItemPositions", showItemPositions);
        EditorPrefs.SetBool("DeletePrefabWithItem", deletePrefabWithItem);
        EditorPrefs.SetBool("UpdateNamesOnExit", updateNamesOnExit);
        EditorPrefs.SetInt("ItemToShowIndex", itemToShow.itemToShowIndex);
        EditorPrefs.SetInt("ListIndex", itemToShow.listIndex);
        EditorPrefs.SetInt("CategoryIndex", itemToShow.categoryIndex);
        EditorPrefs.SetInt("ItemsPerPage", itemsPerPage);
        EditorPrefs.SetInt("CurrentPage", currentPage);
    }

    /// <summary>
    /// Gets the subtypes of the currently selected type
    /// </summary>
    /// <returns></returns>
    string[] GetSubtypeNames()
    {
        List<string> names = new List<string>();
        names.Add("None");

        //Get the names of all the subtypes that have the same type as the current type
        for (int i = 0; i < autoVidLists.subtypes.Count; i++)
            if ((int)autoVidLists.subtypes[i].type == itemToShow.listIndex)
                names.Add(autoVidLists.subtypes[i].name);

        return names.ToArray();
    }

    int GetSubtypeIndex(string subtypeName)
    {
        for (int i = 0; i < autoVidLists.subtypes.Count; i++)
            if ((int)autoVidLists.subtypes[i].type == itemToShow.listIndex && autoVidLists.subtypes[i].name == subtypeName)
                return i;

        return -1;
    }

    /// <summary>
    /// Updates the string to contain a formatted sentence detailing what subtypes the currently shown item is in(if at all)
    /// </summary>
    /// <param name="ID">ID of the currently shown item</param>
    void UpdateCurrentItemSubtypes(int ID)
    {
        subtypesOfCurrentItem = "";
        for (int i = 0; i < autoVidLists.subtypes.Count; i++)
            if ((int)autoVidLists.subtypes[i].type == itemToShow.listIndex && autoVidLists.subtypes[i].itemIDs.Contains(ID))
                subtypesOfCurrentItem += autoVidLists.subtypes[i].name + ", ";

        //We will have an extra comma and space at the end, so remove them and add a period
        if (subtypesOfCurrentItem != string.Empty)
            subtypesOfCurrentItem = "Item is in these Subtypes: " + subtypesOfCurrentItem.Remove(subtypesOfCurrentItem.Length - 2, 2) + ".";
        else
            subtypesOfCurrentItem = "Item is in NO Subtypes.";
    }

    void UpdateGategoryData()
    {
        //Reset some things
        itemToShow.categoryIndex = 0;
        List<string> categoryNames = new List<string>();
        categoryNamesTypeIndex.Clear();

        for (int i = 0; i < itemLists.Length; i++)
        {
            if (itemLists[i] != null)
            {
                //If a group is now selected and it doesn't include this type then skip it
                if (selectedTypeGroup > 0 && !autoVidLists.typeGroups[selectedTypeGroup - 1].types.Contains((ItemType)i))
                    continue;

                categoryNames.Add(((ItemType)i).ToString());    //Add category name
                categoryNamesTypeIndex.Add(i);  //Add category name index
                noCategories = false;
            }
        }

        if (noCategories)
        {
            categoryNamesArray = new string[0];
            return;
        }

        //Convert and store an array to use so we don't convert every OnGUI call
        categoryNamesArray = categoryNames.ToArray();
        itemToShow.listIndex = categoryNamesTypeIndex[0];
    }

    void ResetSubtypeStates()
    {
        addedSubtype = removedSubtype = failedToAddSubtype = failedToRemoveSubtype = false;
    }

    /// <summary>
    /// Updates the item name enums
    /// </summary>
    /// <param name="allTypes">Whether to update the name enums for all types or only the currently selected one</param>
    void UpdateItemNamesEnum(bool allTypes)
    {
        //Read and setup vars
        string[] lines = File.ReadAllLines(vidListsScriptPath);
        List<string> newLines = new List<string>();
        newLines.AddRange(lines);

        int isnb2Index = -1, isne2Index = -1, wantedTypeStart = -1, wantedTypeEnd = -1;
        string wantedType = ((ItemType)itemToShow.listIndex).ToString();

        //Get wanted indices
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("//#VID-2ISNB"))
                isnb2Index = i;

            //If we want to update a single type only
            else if (!allTypes && lines[i].Contains("public enum " + wantedType + "Items"))
            {
                wantedTypeStart = i;

                //Get start and end of the item enum we want to update
                for (int j = i + 2; j < lines.Length; j++)
                    if (lines[j].Contains("}"))
                    {
                        wantedTypeEnd = j;
                        break;
                    }
            }

            else if (lines[i].Contains("//#VID-2ISNE"))
            {
                isne2Index = i;
                break;
            }
        }

        List<string> formattedList = null;

        //Update this type
        if (!allTypes)
        {
            //If the enum exists remove it
            if (wantedTypeStart != -1)
            {
                newLines.RemoveRange(wantedTypeStart, (wantedTypeEnd - wantedTypeStart) + 1);
                isne2Index -= (wantedTypeEnd - wantedTypeStart) + 1;    //Update isne index to reflect its new position
            }

            List<string> invalidNames = new List<string>();
            formattedList = GetFormattedNamesList((ItemType)itemToShow.listIndex, ref invalidNames);

            //Check for invalid names
            if (invalidNames.Count > 0)
            {
                string d = string.Empty;
                for (int i = 0; i < invalidNames.Count; i++)
                {
                    d += invalidNames[i] + ", ";

                    //New line every five names
                    if (i != invalidNames.Count - 1 && (i + 1) % 5 == 0)
                        d += "\n";
                }

                d = d.Remove(d.Length - 2, 1);  //Remove the last comma
                EditorUtility.DisplayDialog("Warning: Invalid Names, Some Item Names Weren't Updated", "These Items of the Current Type Were NOT Updated: \n\n" + d
                    + "\n\nThis is Because of a Name Collision With Another Item OR That the Name Doesn't Start With a Letter.\nPlease Rename Them and Update Again."
                    , "OK");
            }

            //Generate the code
            newLines.Insert(isne2Index, "\t}");
            newLines.InsertRange(isne2Index, formattedList);
            newLines.Insert(isne2Index, "\t\tNone = 0,");
            newLines.Insert(isne2Index, "\t{");
            newLines.Insert(isne2Index, "\tpublic enum " + ((ItemType)itemToShow.listIndex).ToString() + "Items");
        }

        //Update all the types
        else
        {
            //Remove all the name enums and update index
            newLines.RemoveRange(isnb2Index + 1, (isne2Index - isnb2Index) - 1);
            isne2Index = isnb2Index + 1;

            List<InvalidItemNames> invalidNames = new List<InvalidItemNames>();

            //Create item name enums for all VID types
            for (int i = 0; i < itemLists.Length; i++)
            {
                if (itemLists[i] != null)
                {
                    invalidNames.Add(new InvalidItemNames((ItemType)i));
                    formattedList = GetFormattedNamesList((ItemType)i, ref invalidNames[invalidNames.Count - 1].invalidNames);

                    newLines.Insert(isne2Index, "\t}");
                    newLines.InsertRange(isne2Index, formattedList);
                    newLines.Insert(isne2Index, "\t\tNone = 0,");
                    newLines.Insert(isne2Index, "\t{");
                    newLines.Insert(isne2Index, "\tpublic enum " + ((ItemType)i).ToString() + "Items");
                }
            }

            //Show duplication message
            for (int i = 0; i < invalidNames.Count; i++)
            {
                //If no invalid names for this type skip
                if (invalidNames[i].invalidNames.Count == 0)
                    continue;

                string d = string.Empty;

                //Get duplicate names
                for (int j = 0; j < invalidNames[i].invalidNames.Count; j++)
                {
                    d += invalidNames[i].invalidNames[j] + ", ";

                    //New line every five names
                    if (j != invalidNames[i].invalidNames.Count - 1 && (j + 1) % 5 == 0)
                        d += "\n";
                }

                d = d.Remove(d.Length - 2, 1);  //Remove the last comma
                EditorUtility.DisplayDialog("Warning: Invalid Names, Some Item Names Weren't Updated", "These Items of the '" + invalidNames[i].type
                    + "' Type Were NOT Updated: \n" + d
                    + "\n\nThis is Because of a Name Collision With Another Item OR That the Name Doesn't Start With a Letter.\nPlease Rename Them and Update Again.",
                    "OK");
            }
        }

        //Write to file
        File.WriteAllLines(vidListsScriptPath, newLines.ToArray());
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Returns a list of formatted entries for an item name enum, EXCLUDING duplicate values and anything that doesn't start with a letter
    ///(Includes the first occurence of a duplicate name)
    /// </summary>
    /// <param name="t">Type to get names list for</param>
    /// <param name="invalidList">List which will be updated with invalid names</param>
    /// <returns></returns>
    List<string> GetFormattedNamesList(ItemType t, ref List<string> invalidList)
    {
        List<string> itemNames = new List<string>();
        HashSet<string> hash = new HashSet<string>();
        string itemName;

        switch (t)
        {//#VID-GFNLB
			case ItemType.MeleeWeapon:
				for (int i = 0; i < autoVidLists.autoMeleeWeapon.Count; i++)
				{
					itemName = autoVidLists.autoMeleeWeapon[i].itemName.Trim();
					
					if (itemName == string.Empty)
						continue;
					
					if (!hash.Add(itemName) || !char.IsLetter(itemName[0]))
					{
						invalidList.Add(itemName);
						continue;
					}
					
						itemNames.Add("\t\t" + itemName.Replace(" ", "") + " = " + autoVidLists.autoMeleeWeapon[i].itemID + ",");
				}
				break;
        }//#VID-GFNLE

        return itemNames;
    }

    void OnDestroy()
    {
        if (!noCategories && updateNamesOnExit)
            UpdateItemNamesEnum(true);
    }

    partial void AfterItemDrawing(ItemBase shownItem);
    partial void AfterSubtypeButtonsDrawing(ItemBase shownItem);

    //public void Test()
    //{
    //    //What are you doing here?!
    //    for (int i = 0; i < 1000; i++)
    //    {
    //        AddItem();
    //    }
    //}
}

class ItemToShow
{
    public int itemToShowIndex = -1;    //Index of selected item
    /// <summary> Index of item list in 'itemLists' array.(Can be directly converted into an item type)</summary>
    public int listIndex = 0;
    /// <summary>Index of selected item type used in 'DrawItemTypeButtons' method</summary>
    public int categoryIndex = 0;
}

class InvalidItemNames
{
    public ItemType type;
    public List<string> invalidNames = new List<string>();

    public InvalidItemNames(ItemType typeOfItems)
    {
        type = typeOfItems;
    }
}
