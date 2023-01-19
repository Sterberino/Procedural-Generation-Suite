using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralIslandGenerator))]
public class ProceduralIslandGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Generate Island Texture"))
        {
            ProceduralIslandGenerator generator = (ProceduralIslandGenerator)target;
            generator.GenerateIslandTexture();
            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("Place Island"))
        {
            ProceduralIslandGenerator generator = (ProceduralIslandGenerator)target;
            generator.PlaceIsland();
            EditorUtility.SetDirty(target);

        }

        if (GUILayout.Button("Save Island Texture as PNG"))
        {
            ProceduralIslandGenerator generator = (ProceduralIslandGenerator)target;
            generator.TrySaveTexture();
        }

    }

}