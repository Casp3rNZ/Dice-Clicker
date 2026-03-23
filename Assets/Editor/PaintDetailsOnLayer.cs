using UnityEngine;
using UnityEditor;

public class PaintDetailsOnLayer : EditorWindow
{
    private Terrain terrain;
    private int textureLayerIndex = 0;
    private float threshold = 0.5f;
    private float targetStrength = 0.4f;
    private int maxDetailDensity = 255;

    [MenuItem("Tools/Paint Details On Texture Layer")]
    static void Open() => GetWindow<PaintDetailsOnLayer>("Paint Details On Layer");

    void OnGUI()
    {
        GUILayout.Label("Auto-Paint Detail Meshes on a Texture Layer", EditorStyles.boldLabel);

        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);

        if (terrain != null)
        {
            var layers = terrain.terrainData.terrainLayers;
            string[] names = new string[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                names[i] = layers[i] != null ? layers[i].name : $"Layer {i}";

            textureLayerIndex = EditorGUILayout.Popup("Grass Texture Layer", textureLayerIndex, names);
        }

        EditorGUILayout.Space();
        GUILayout.Label("Density Settings", EditorStyles.boldLabel);

        threshold        = EditorGUILayout.Slider("Grass Threshold", threshold, 0f, 1f);
        targetStrength   = EditorGUILayout.Slider("Target Strength", targetStrength, 0f, 1f);
        maxDetailDensity = EditorGUILayout.IntSlider("Max Density", maxDetailDensity, 1, 255);

        int computedDensity = Mathf.RoundToInt(targetStrength * maxDetailDensity);
        EditorGUILayout.LabelField("Computed Density Value", computedDensity.ToString());

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This will OVERWRITE all detail layers. It paints every detail mesh at the computed density wherever the chosen texture layer is above the threshold.",
            MessageType.Warning);

        EditorGUI.BeginDisabledGroup(terrain == null);

        if (GUILayout.Button("Paint Details"))
            PaintDetails();

        if (GUILayout.Button("Clear All Details"))
            ClearDetails();

        EditorGUI.EndDisabledGroup();
    }

    void PaintDetails()
    {
        var td = terrain.terrainData;
        int alphaW     = td.alphamapWidth;
        int alphaH     = td.alphamapHeight;
        int detailW    = td.detailWidth;
        int detailH    = td.detailHeight;
        int layerCount = td.detailPrototypes.Length;

        int computedDensity = Mathf.RoundToInt(targetStrength * maxDetailDensity);

        float[,,] alphas = td.GetAlphamaps(0, 0, alphaW, alphaH);

        int[,] densityMap = new int[detailH, detailW];

        for (int y = 0; y < detailH; y++)
        {
            for (int x = 0; x < detailW; x++)
            {
                int ax = Mathf.Clamp(Mathf.RoundToInt((float)x / detailW * alphaW), 0, alphaW - 1);
                int ay = Mathf.Clamp(Mathf.RoundToInt((float)y / detailH * alphaH), 0, alphaH - 1);

                float grassWeight = alphas[ay, ax, textureLayerIndex];
                densityMap[y, x] = grassWeight >= threshold ? computedDensity : 0;
            }
        }

        Undo.RegisterCompleteObjectUndo(td, "Paint Details On Layer");

        for (int i = 0; i < layerCount; i++)
            td.SetDetailLayer(0, 0, i, densityMap);

        Debug.Log($"Painted {layerCount} detail layer(s) on '{td.terrainLayers[textureLayerIndex].name}' at density {computedDensity} (strength {targetStrength} × max {maxDetailDensity}).");
    }

    void ClearDetails()
    {
        var td = terrain.terrainData;
        int[,] empty = new int[td.detailHeight, td.detailWidth];
        Undo.RegisterCompleteObjectUndo(td, "Clear All Details");
        for (int i = 0; i < td.detailPrototypes.Length; i++)
            td.SetDetailLayer(0, 0, i, empty);
        Debug.Log("Cleared all detail layers.");
    }
}