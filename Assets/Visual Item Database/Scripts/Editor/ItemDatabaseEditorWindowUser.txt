using UnityEngine;
using UnityEditor;
using ItemSystem;
using ItemSystem.Database;

public partial class ItemDatabaseEditorWindow : EditorWindow
{
    /// <summary>
    /// Anything added to the editor window here will show directly under the data of the selected item. This can be used to customize the editor window for 
    /// one or all the types.
    /// </summary>
    /// <param name="shownItem">The item currently being shown</param>
    partial void AfterItemDrawing(ItemBase shownItem)
    {
    }

    /// <summary>
    /// Anything added to the editor window here will show under all the buttons in the item area. This can be used to customize the editor window for 
    /// one or all the types.
    /// </summary>
    /// <param name="shownItem">The item currently being shown</param>
    partial void AfterSubtypeButtonsDrawing(ItemBase shownItem)
    {
    }
}