using UnityEngine;
using UnityEditor;

public class AssignMaterialToChildren : EditorWindow
{
    private Material targetMaterial;

    [MenuItem("Tools/Assign Material To Children")]
    public static void ShowWindow()
    {
        GetWindow<AssignMaterialToChildren>("Assign Material");
    }

    private void OnGUI()
    {
        targetMaterial = (Material)EditorGUILayout.ObjectField(
            "Target Material", targetMaterial, typeof(Material), false);

        if (GUILayout.Button("Assign to Selected Groups"))
        {
            if (targetMaterial == null)
            {
                Debug.LogWarning("请先指定材质");
                return;
            }

            foreach (GameObject go in Selection.gameObjects)
            {
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);

                foreach (Renderer r in renderers)
                {
                    Undo.RecordObject(r, "Assign Material To Children");

                    Material[] oldMats = r.sharedMaterials;
                    int count = Mathf.Max(1, oldMats.Length);
                    Material[] newMats = new Material[count];

                    for (int i = 0; i < count; i++)
                    {
                        newMats[i] = targetMaterial;
                    }

                    r.sharedMaterials = newMats;
                    EditorUtility.SetDirty(r);
                }
            }

            Debug.Log("材质已赋给选中组下所有 Renderer。");
        }
    }
}
