using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CellularAutomataTilePlacer))]
public class CellularAutomataTilePlacerEditor : Editor
{ 
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Generate Preview"))
        {
            CellularAutomataTilePlacer generator = (CellularAutomataTilePlacer)target;
            generator.GeneratePreview();
            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("Place Tiles"))
        {
            CellularAutomataTilePlacer generator = (CellularAutomataTilePlacer)target;
            generator.PlaceTiles();
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(generator.targetTilemap);
        }

    }

}
