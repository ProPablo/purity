using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GeneratePlanet))]
public class TerrainInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GeneratePlanet gen = (GeneratePlanet) target;
        if (gen.debugVerteces)
        {
            if (GUILayout.Button("Generate Mesh"))
            {
                gen.DrawMesh();
            }
        }
        else
        {
            gen.DrawMesh();
        }
    }
}