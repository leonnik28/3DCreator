using UnityEngine;
using UnityEditor;

public static class FotocentrSetupEditor
{
    [MenuItem("Fotocentr/Setup Model Database")]
    public static void SetupModelDatabase()
    {
        EnsureFolder("Assets", "Models");
        string basePath = "Assets/Models";

        var spherePrefab = CreatePrimitivePrefab(PrimitiveType.Sphere, basePath + "/Sphere.prefab");
        var cubePrefab = CreatePrimitivePrefab(PrimitiveType.Cube, basePath + "/Cube.prefab");
        var cylinderPrefab = CreatePrimitivePrefab(PrimitiveType.Cylinder, basePath + "/Cylinder.prefab");

        var db = ScriptableObject.CreateInstance<ModelDatabase>();
        var so = new SerializedObject(db);
        var arr = so.FindProperty("_models");
        arr.arraySize = 3;
        arr.GetArrayElementAtIndex(0).FindPropertyRelative("DisplayName").stringValue = "Sphere";
        arr.GetArrayElementAtIndex(0).FindPropertyRelative("Prefab").objectReferenceValue = spherePrefab;
        arr.GetArrayElementAtIndex(1).FindPropertyRelative("DisplayName").stringValue = "Cube";
        arr.GetArrayElementAtIndex(1).FindPropertyRelative("Prefab").objectReferenceValue = cubePrefab;
        arr.GetArrayElementAtIndex(2).FindPropertyRelative("DisplayName").stringValue = "Cylinder";
        arr.GetArrayElementAtIndex(2).FindPropertyRelative("Prefab").objectReferenceValue = cylinderPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(db, basePath + "/ModelDatabase.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("ModelDatabase created at Assets/Models/ModelDatabase.asset");
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent)) return;
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    static GameObject CreatePrimitivePrefab(PrimitiveType type, string path)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = type.ToString();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
