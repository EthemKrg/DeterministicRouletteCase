using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class RouletteLabelTMPGenerator : EditorWindow
{
    TMP_FontAsset fontAsset;
    Material materialPreset;
    float fontSize = 0.12f;
    float characterSpacing = 0f;
    Vector3 localOffset = new Vector3(0f, 0.002f, 0f);
    bool disableSourceRenderer = true;
    bool overwriteExisting = true;

    static readonly Regex LabelRegex = new Regex(
        @"(?:DoubleZero|00|(?<!\d)(?:0|[1-9]|[12]\d|3[0-6])(?!\d))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    [MenuItem("Tools/Roulette/Add TMP Children To Selected Labels")]
    static void Open()
    {
        GetWindow<RouletteLabelTMPGenerator>("Roulette TMP Labels");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Target Text", EditorStyles.boldLabel);
        fontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField("TMP Font Asset", fontAsset, typeof(TMP_FontAsset), false);
        materialPreset = (Material)EditorGUILayout.ObjectField("Material Preset", materialPreset, typeof(Material), false);
        fontSize = EditorGUILayout.FloatField("Font Size", fontSize);
        characterSpacing = EditorGUILayout.FloatField("Character Spacing", characterSpacing);
        localOffset = EditorGUILayout.Vector3Field("Local Offset", localOffset);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Source Mesh Handling", EditorStyles.boldLabel);
        disableSourceRenderer = EditorGUILayout.Toggle("Disable Source Renderer", disableSourceRenderer);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing TMP Child", overwriteExisting);

        EditorGUILayout.Space(10);
        using (new EditorGUI.DisabledScope(fontAsset == null || Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button($"Create TMP Children For {Selection.gameObjects.Length} Selected Labels"))
            {
                CreateForSelection();
            }
        }

        EditorGUILayout.HelpBox(
            "Select imported label GameObjects such as Wheel_EU_Label_01_0, Wheel_US_Label_20_DoubleZero, EU_Number_32_Label, then run this tool. The text is parsed from the object name.",
            MessageType.Info
        );
    }

    void CreateForSelection()
    {
        var created = 0;
        var skipped = new List<string>();

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();

        foreach (var source in Selection.gameObjects)
        {
            var label = ExtractLabel(source.name);
            if (string.IsNullOrEmpty(label))
            {
                skipped.Add(source.name);
                continue;
            }

            var existing = source.transform.Find("TMP_Label");
            if (existing != null)
            {
                if (!overwriteExisting)
                {
                    skipped.Add(source.name);
                    continue;
                }

                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var child = new GameObject("TMP_Label");
            Undo.RegisterCreatedObjectUndo(child, "Create TMP Label");
            child.transform.SetParent(source.transform, false);
            child.transform.localPosition = localOffset;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            var tmp = child.AddComponent<TextMeshPro>();
            tmp.text = label;
            tmp.font = fontAsset;
            if (materialPreset != null)
                tmp.fontSharedMaterial = materialPreset;
            tmp.fontSize = fontSize;
            tmp.characterSpacing = characterSpacing;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.richText = false;
            tmp.extraPadding = false;
            tmp.UpdateMeshPadding();
            tmp.ForceMeshUpdate();

            if (disableSourceRenderer)
            {
                var renderer = source.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Undo.RecordObject(renderer, "Disable Source Label Renderer");
                    renderer.enabled = false;
                }
            }

            EditorUtility.SetDirty(source);
            created++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"Roulette TMP labels created: {created}. Skipped: {skipped.Count}");
        if (skipped.Count > 0)
            Debug.LogWarning("Skipped labels: " + string.Join(", ", skipped));
    }

    static string ExtractLabel(string objectName)
    {
        var name = objectName.Replace("DoubleZero", "00");

        if (name.Contains("_00") || name.EndsWith("00"))
            return "00";

        var matches = LabelRegex.Matches(name);
        if (matches.Count == 0)
            return string.Empty;

        var value = matches[matches.Count - 1].Value;
        return value.Equals("DoubleZero", System.StringComparison.OrdinalIgnoreCase) ? "00" : value;
    }
}
