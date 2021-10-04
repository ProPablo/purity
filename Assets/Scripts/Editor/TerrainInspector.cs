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
        if (gen.debugGen)
        {
            gen.DrawMesh();
        }
        else
        {
            if (GUILayout.Button("Generate Mesh"))
            {
                gen.DrawMesh();
            }
            
        }

        if (GUILayout.Button("ComputeShader"))
        {
            gen.RunShader();
        }
        
        if (GUILayout.Button("GenSky"))
        {
            gen.GenerateHighGround();
        }

        if (GUILayout.Button("Gen Large Terrain"))
        {
            gen.GenerateTerrainLarge();
        }
        
    }
}