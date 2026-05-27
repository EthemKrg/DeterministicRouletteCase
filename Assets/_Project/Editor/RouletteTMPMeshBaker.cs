using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class RouletteTMPMeshBaker : EditorWindow
{
    string outputName = "Roulette_TMP_Combined";
    string assetFolder = "Assets/GeneratedMeshes";
    bool includeInactive = true;
    bool disableSourceTMP = true;
    bool includeOnlyNamedTMPLabel = true;

    [MenuItem("Tools/Roulette/Bake Selected TMP Labels To One Mesh")]
    static void Open()
    {
        GetWindow<RouletteTMPMeshBaker>("Roulette TMP Baker");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Bake Scope", EditorStyles.boldLabel);
        outputName = EditorGUILayout.TextField("Output Name", outputName);
        assetFolder = EditorGUILayout.TextField("Asset Folder", assetFolder);
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        includeOnlyNamedTMPLabel = EditorGUILayout.Toggle("Only TMP_Label Children", includeOnlyNamedTMPLabel);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("After Bake", EditorStyles.boldLabel);
        disableSourceTMP = EditorGUILayout.Toggle("Disable Source TMP", disableSourceTMP);

        EditorGUILayout.Space(10);
        using (new EditorGUI.DisabledScope(Selection.activeTransform == null))
        {
            if (GUILayout.Button("Bake TMP Labels Under Selected Root"))
            {
                Bake(Selection.activeTransform);
            }
        }

        EditorGUILayout.HelpBox(
            "Select the parent/root that should own the combined mesh. For wheel numbers, select Wheel_EU_SpinRoot or Wheel_US_SpinRoot so the baked text follows the spin animation.",
            MessageType.Info
        );
    }

    void Bake(Transform root)
    {
        var tmps = root.GetComponentsInChildren<TextMeshPro>(includeInactive);
        var byMaterial = new Dictionary<Material, List<CombineInstance>>();
        var sourceObjects = new List<GameObject>();
        var bakedCount = 0;

        foreach (var tmp in tmps)
        {
            if (includeOnlyNamedTMPLabel && tmp.gameObject.name != "TMP_Label")
                continue;

            tmp.ForceMeshUpdate(true, true);

            var mesh = tmp.mesh;
            if (mesh == null || mesh.vertexCount == 0)
                continue;

            var renderer = tmp.GetComponent<MeshRenderer>();
            var material = renderer != null ? renderer.sharedMaterial : tmp.fontSharedMaterial;
            if (material == null)
                continue;

            if (!byMaterial.TryGetValue(material, out var list))
            {
                list = new List<CombineInstance>();
                byMaterial.Add(material, list);
            }

            list.Add(new CombineInstance
            {
                mesh = mesh,
                transform = root.worldToLocalMatrix * tmp.transform.localToWorldMatrix
            });

            sourceObjects.Add(tmp.gameObject);
            bakedCount++;
        }

        if (bakedCount == 0)
        {
            Debug.LogWarning($"No TMP labels found under {root.name}.");
            return;
        }

        Directory.CreateDirectory(assetFolder);

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();

        var combinedRoot = new GameObject(outputName);
        Undo.RegisterCreatedObjectUndo(combinedRoot, "Create Combined TMP Mesh Root");
        combinedRoot.transform.SetParent(root, false);
        combinedRoot.transform.localPosition = Vector3.zero;
        combinedRoot.transform.localRotation = Quaternion.identity;
        combinedRoot.transform.localScale = Vector3.one;

        var materialIndex = 0;
        var totalCombinedMeshes = 0;

        foreach (var pair in byMaterial)
        {
            var material = pair.Key;
            var combine = pair.Value;

            var mesh = new Mesh
            {
                name = byMaterial.Count == 1 ? outputName : $"{outputName}_{materialIndex}",
                indexFormat = IndexFormat.UInt32
            };
            mesh.CombineMeshes(combine.ToArray(), true, true);
            mesh.RecalculateBounds();

            var safeName = SanitizeAssetName(mesh.name);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{assetFolder}/{safeName}.asset");
            AssetDatabase.CreateAsset(mesh, path);

            var child = byMaterial.Count == 1 ? combinedRoot : new GameObject(mesh.name);
            if (child != combinedRoot)
            {
                Undo.RegisterCreatedObjectUndo(child, "Create Combined TMP Mesh");
                child.transform.SetParent(combinedRoot.transform, false);
            }

            var filter = child.AddComponent<MeshFilter>();
            var meshRenderer = child.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = material;

            materialIndex++;
            totalCombinedMeshes++;
        }

        if (disableSourceTMP)
        {
            foreach (var source in sourceObjects)
            {
                Undo.RecordObject(source, "Disable Source TMP Label");
                source.SetActive(false);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Baked {bakedCount} TMP labels under {root.name} into {totalCombinedMeshes} mesh renderer(s).");
    }

    static string SanitizeAssetName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');

        return value;
    }
}
