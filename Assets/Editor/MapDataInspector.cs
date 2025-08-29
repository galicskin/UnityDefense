using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapData))]
public class MapDataInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(8);
        if (GUILayout.Button("Edit Grid¡¦", GUILayout.Height(28)))
        {
            MapGridEditorWindow.Open((MapData)target);
        }
    }
}
