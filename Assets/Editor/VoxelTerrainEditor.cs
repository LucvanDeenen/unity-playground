#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelTerrain))]
public class VoxelTerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VoxelTerrain voxelTerrain = (VoxelTerrain)target;

        if (voxelTerrain.noiseMapTexture != null)
        {
            GUILayout.Label("Noise Map Preview", EditorStyles.boldLabel);
            GUILayout.Space(5);

            float aspect = (float)voxelTerrain.noiseMapTexture.width / voxelTerrain.noiseMapTexture.height;
            Rect rect = GUILayoutUtility.GetAspectRect(aspect);
            EditorGUI.DrawPreviewTexture(rect, voxelTerrain.noiseMapTexture);
        }
        else
        {
            GUILayout.Label("Noise Map not generated yet.");
        }
    }
}
#endif
