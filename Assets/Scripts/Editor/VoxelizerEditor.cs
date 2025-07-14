using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Voxelizer))]
public class VoxelizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("⚙️ Control Tools", EditorStyles.boldLabel);

        Voxelizer voxelizer = (Voxelizer)target;

        if (GUILayout.Button("🔷 Generate Voxels"))
        {
            if (!Application.isPlaying)
            {
                voxelizer.GenerateVoxels();
            }
            else
            {
                Debug.LogWarning("⚠️ Cannot generate voxels during Play Mode!");
            }
        }

        VoxelMassSpringBuilder builder = voxelizer.GetComponent<VoxelMassSpringBuilder>();
        if (builder != null)
        {
            if (GUILayout.Button("🪡 Build Mass-Spring"))
            {
                if (!Application.isPlaying)
                {
                    builder.BuildMassSpring();
                }
                else
                {
                    Debug.LogWarning("⚠️ Cannot build Mass-Spring during Play Mode!");
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("VoxelMassSpringBuilder component is missing on this GameObject.", MessageType.Info);
        }

        if (voxelizer.GetComponent<MeshFilter>()?.sharedMesh == null)
        {
            EditorGUILayout.HelpBox("⚠️ No Mesh found on the object to voxelize.", MessageType.Warning);
        }
    }
}
