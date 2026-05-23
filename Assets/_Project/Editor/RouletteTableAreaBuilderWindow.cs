using UnityEditor;
using UnityEngine;

public class RouletteTableAreaBuilderWindow : EditorWindow
{
    private Transform parent;
    private GameObject betAreaPrefab;

    private Vector3 startPosition = new Vector3(-2.75f, 0.15f, -1.2f);
    private Vector2 cellSize = new Vector2(0.45f, 0.45f);
    private Vector3 areaScale = new Vector3(0.42f, 0.05f, 0.42f);

    private int betAreaLayer;

    [MenuItem("Tools/Roulette/Table Area Builder")]
    private static void Open()
    {
        GetWindow<RouletteTableAreaBuilderWindow>("Roulette Table Builder");
    }

    private void OnEnable()
    {
        betAreaLayer = LayerMask.NameToLayer("BetArea");
    }

    private void OnGUI()
    {
        parent = (Transform)EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true);
        betAreaPrefab = (GameObject)EditorGUILayout.ObjectField("Bet Area Prefab", betAreaPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();

        startPosition = EditorGUILayout.Vector3Field("Start Position", startPosition);
        cellSize = EditorGUILayout.Vector2Field("Cell Size", cellSize);
        areaScale = EditorGUILayout.Vector3Field("Area Scale", areaScale);

        EditorGUILayout.Space();

        if (betAreaLayer == -1)
        {
            EditorGUILayout.HelpBox("BetArea layer was not found. Create it before generating table areas.", MessageType.Error);
            return;
        }

        if (GUILayout.Button("Generate Straight Areas 0-36"))
            GenerateStraightAreas();
    }

    private void GenerateStraightAreas()
    {
        if (parent == null)
        {
            Debug.LogWarning("Parent is missing.");
            return;
        }

        if (betAreaPrefab == null)
        {
            Debug.LogWarning("Bet area prefab is missing.");
            return;
        }

        Transform root = GetOrCreateChild(parent, "Straight");

        ClearChildren(root);

        CreateStraightArea(root, "0", 0, new Vector3(startPosition.x - cellSize.x, startPosition.y, startPosition.z + cellSize.y));

        for (int number = 1; number <= 36; number++)
        {
            int row = (number - 1) % 3;
            int column = (number - 1) / 3;

            Vector3 position = new Vector3(
                startPosition.x + column * cellSize.x,
                startPosition.y,
                startPosition.z + row * cellSize.y
            );

            CreateStraightArea(root, number.ToString(), number, position);
        }

        Debug.Log("Generated straight bet areas 0-36.");
    }

    private void CreateStraightArea(Transform root, string slotId, int primaryNumber, Vector3 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(betAreaPrefab, root);

        instance.name = $"BetArea_Straight_{slotId}";
        instance.transform.localPosition = position;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = areaScale;
        instance.layer = betAreaLayer;

        RouletteBetArea betArea = instance.GetComponent<RouletteBetArea>();

        if (betArea == null)
        {
            Debug.LogWarning($"{instance.name} has no RouletteBetArea component.");
            return;
        }

        SerializedObject serializedObject = new SerializedObject(betArea);

        serializedObject.FindProperty("betType").enumValueIndex = (int)BetType.Straight;
        serializedObject.FindProperty("primaryNumber").intValue = primaryNumber;
        serializedObject.FindProperty("secondaryNumber").intValue = 0;
        serializedObject.FindProperty("index").intValue = 0;
        serializedObject.FindProperty("straightSlotId").stringValue = slotId;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(betArea);
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);

        if (existing != null)
            return existing;

        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);

        return child.transform;
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            DestroyImmediate(root.GetChild(i).gameObject);
    }
}