using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class FlatDiscMeshReplacementTool : EditorWindow
{
    private const string DiscMeshPath = "Assets/_Project/Art/Models/UnitDisc_32.asset";
    private const int DiscSegments = 32;

    [SerializeField] private Transform root;
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool skipChipStacks = true;
    [SerializeField] private bool skipChipVisuals = true;
    [SerializeField] private bool replaceBuiltInSphere = true;
    [SerializeField] private bool replaceHighVertexMeshes = true;
    [SerializeField] private int highVertexThreshold = 100;

    private Vector2 scroll;
    private readonly List<Candidate> lastCandidates = new List<Candidate>();
    private string lastScanSummary = string.Empty;

    [MenuItem("Tools/Roulette/Flat Disc Mesh Replacement")]
    private static void Open()
    {
        GetWindow<FlatDiscMeshReplacementTool>("Flat Disc Replace");
    }

    [MenuItem("Tools/Roulette/Create UnitDisc_32 Mesh")]
    public static void CreateDiscMeshMenu()
    {
        Mesh disc = GetOrCreateDiscMesh();
        Selection.activeObject = disc;
        EditorGUIUtility.PingObject(disc);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scope", EditorStyles.boldLabel);
        root = (Transform)EditorGUILayout.ObjectField("Selected Root", root, typeof(Transform), true);
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Safety", EditorStyles.boldLabel);
        skipChipStacks = EditorGUILayout.Toggle("Skip ChipStackView", skipChipStacks);
        skipChipVisuals = EditorGUILayout.Toggle("Skip ChipVisual", skipChipVisuals);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Replacement Criteria", EditorStyles.boldLabel);
        replaceBuiltInSphere = EditorGUILayout.Toggle("Built-in Sphere Mesh", replaceBuiltInSphere);
        replaceHighVertexMeshes = EditorGUILayout.Toggle("High Vertex Meshes", replaceHighVertexMeshes);
        highVertexThreshold = EditorGUILayout.IntField("High Vertex Threshold", Mathf.Max(3, highVertexThreshold));

        EditorGUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(root == null))
            {
                if (GUILayout.Button("Scan Selected Root"))
                    Scan();

                if (GUILayout.Button("Replace Scanned Meshes"))
                    ReplaceScannedMeshes();
            }
        }

        if (GUILayout.Button("Create/Select UnitDisc_32 Mesh"))
            CreateDiscMeshMenu();

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "This tool only swaps MeshFilter.sharedMesh to UnitDisc_32. It does not change transforms, materials, renderers, colliders, TMP, layers, or gameplay components.",
            MessageType.Info);

        if (!string.IsNullOrEmpty(lastScanSummary))
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(lastScanSummary, EditorStyles.boldLabel);
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (Candidate candidate in lastCandidates)
        {
            if (candidate.Filter == null)
                continue;

            EditorGUILayout.LabelField(
                candidate.Filter.name,
                $"{candidate.Reason} | verts: {candidate.OriginalVertexCount} | mesh: {candidate.OriginalMeshName}");
        }
        EditorGUILayout.EndScrollView();
    }

    private void OnEnable()
    {
        if (root == null && Selection.activeTransform != null)
            root = Selection.activeTransform;
    }

    private void OnSelectionChange()
    {
        if (Selection.activeTransform != null)
        {
            root = Selection.activeTransform;
            Repaint();
        }
    }

    private void Scan()
    {
        lastCandidates.Clear();

        if (root == null)
        {
            lastScanSummary = "No root selected.";
            return;
        }

        Mesh discMesh = AssetDatabase.LoadAssetAtPath<Mesh>(DiscMeshPath);
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(includeInactive);

        foreach (MeshFilter filter in filters)
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            if (discMesh != null && filter.sharedMesh == discMesh)
                continue;

            if (ShouldSkip(filter))
                continue;

            if (!TryGetReplacementReason(filter, out string reason))
                continue;

            lastCandidates.Add(new Candidate(filter, filter.sharedMesh.name, filter.sharedMesh.vertexCount, reason));
        }

        lastScanSummary = $"Found {lastCandidates.Count} candidate mesh filter(s) under {root.name}.";
    }

    private void ReplaceScannedMeshes()
    {
        if (root == null)
            return;

        if (lastCandidates.Count == 0)
            Scan();

        if (lastCandidates.Count == 0)
        {
            Debug.Log("No flat disc replacement candidates found.");
            return;
        }

        Mesh discMesh = GetOrCreateDiscMesh();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        int replaced = 0;

        foreach (Candidate candidate in lastCandidates)
        {
            MeshFilter filter = candidate.Filter;
            if (filter == null || filter.sharedMesh == null)
                continue;

            Undo.RecordObject(filter, "Replace Shape Mesh With UnitDisc_32");
            filter.sharedMesh = discMesh;
            EditorUtility.SetDirty(filter);
            replaced++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();

        Debug.Log($"Replaced {replaced} mesh filter(s) with {DiscMeshPath}.");
        Scan();
    }

    private bool ShouldSkip(MeshFilter filter)
    {
        if (filter.GetComponent<Renderer>() == null)
            return true;

        if (skipChipStacks && filter.GetComponentInParent<ChipStackView>(true) != null)
            return true;

        if (skipChipVisuals && filter.GetComponentInParent<ChipVisual>(true) != null)
            return true;

        return false;
    }

    private bool TryGetReplacementReason(MeshFilter filter, out string reason)
    {
        reason = string.Empty;
        Mesh mesh = filter.sharedMesh;

        if (!LooksLikeFlattenedCircleOrEllipse(filter.transform))
            return false;

        if (replaceBuiltInSphere && IsBuiltInSphere(mesh))
        {
            reason = "Flattened built-in sphere";
            return true;
        }

        if (replaceHighVertexMeshes && mesh.vertexCount >= highVertexThreshold)
        {
            reason = "Flattened high vertex mesh";
            return true;
        }

        return false;
    }

    private static bool LooksLikeFlattenedCircleOrEllipse(Transform transform)
    {
        Vector3 scale = transform.lossyScale;
        float x = Mathf.Abs(scale.x);
        float y = Mathf.Abs(scale.y);
        float z = Mathf.Abs(scale.z);

        float max = Mathf.Max(x, Mathf.Max(y, z));
        float min = Mathf.Min(x, Mathf.Min(y, z));
        float middle = x + y + z - max - min;

        if (max <= 0.0001f)
            return false;

        return min <= max * 0.25f && middle >= max * 0.2f;
    }

    private static bool IsBuiltInSphere(Mesh mesh)
    {
        if (mesh == null)
            return false;

        return string.Equals(mesh.name, "Sphere", System.StringComparison.OrdinalIgnoreCase)
            && AssetDatabase.GetAssetPath(mesh) == "Library/unity default resources";
    }

    private static Mesh GetOrCreateDiscMesh()
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(DiscMeshPath);
        if (existing != null)
            return existing;

        Directory.CreateDirectory(Path.GetDirectoryName(DiscMeshPath));

        Mesh mesh = BuildDiscMesh();
        AssetDatabase.CreateAsset(mesh, DiscMeshPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return mesh;
    }

    private static Mesh BuildDiscMesh()
    {
        var vertices = new List<Vector3>(DiscSegments + 1);
        var normals = new List<Vector3>(DiscSegments + 1);
        var uvs = new List<Vector2>(DiscSegments + 1);
        var triangles = new List<int>(DiscSegments * 3);

        vertices.Add(Vector3.zero);
        normals.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (int i = 0; i < DiscSegments; i++)
        {
            float angle = Mathf.PI * 2f * i / DiscSegments;
            float x = Mathf.Cos(angle) * 0.5f;
            float z = Mathf.Sin(angle) * 0.5f;

            vertices.Add(new Vector3(x, 0f, z));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(x + 0.5f, z + 0.5f));
        }

        for (int i = 0; i < DiscSegments; i++)
        {
            int current = i + 1;
            int next = i == DiscSegments - 1 ? 1 : current + 1;

            triangles.Add(0);
            triangles.Add(next);
            triangles.Add(current);
        }

        var mesh = new Mesh
        {
            name = "UnitDisc_32",
            indexFormat = IndexFormat.UInt16
        };

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        return mesh;
    }

    private sealed class Candidate
    {
        public readonly MeshFilter Filter;
        public readonly string OriginalMeshName;
        public readonly int OriginalVertexCount;
        public readonly string Reason;

        public Candidate(MeshFilter filter, string originalMeshName, int originalVertexCount, string reason)
        {
            Filter = filter;
            OriginalMeshName = originalMeshName;
            OriginalVertexCount = originalVertexCount;
            Reason = reason;
        }
    }
}
