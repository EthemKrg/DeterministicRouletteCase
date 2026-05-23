using UnityEditor;
using UnityEngine;

public class RouletteTableAreaBuilderWindow : EditorWindow
{
    private Transform parent;
    private GameObject betAreaPrefab;

    private Vector3 startPosition = new Vector3(-7f, 0.1f, -1.2f);
    private Vector2 cellSize = new Vector2(1.2f, 1.2f);
    private Vector3 areaScale = new Vector3(1f, 0.01f, 1f);

    private int betAreaLayer;

    [SerializeField] private Material redMaterial;
    [SerializeField] private Material blackMaterial;
    [SerializeField] private Material greenMaterial;

    private static bool IsRedNumber(int number)
    {
        switch (number)
        {
            case 1:
            case 3:
            case 5:
            case 7:
            case 9:
            case 12:
            case 14:
            case 16:
            case 18:
            case 19:
            case 21:
            case 23:
            case 25:
            case 27:
            case 30:
            case 32:
            case 34:
            case 36:
                return true;

            default:
                return false;
        }
    }

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

        redMaterial = (Material)EditorGUILayout.ObjectField("Red Material", redMaterial, typeof(Material), false);
        blackMaterial = (Material)EditorGUILayout.ObjectField("Black Material", blackMaterial, typeof(Material), false);
        greenMaterial = (Material)EditorGUILayout.ObjectField("Green Material", greenMaterial, typeof(Material), false);

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

        SetLabel(instance, slotId);
        SetStraightAreaMaterial(instance, slotId);

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

    private static void SetLabel(GameObject instance, string text)
    {
        TMPro.TextMeshPro label = instance.GetComponentInChildren<TMPro.TextMeshPro>();

        if (label == null)
            return;

        label.text = text;
    }

    private void SetStraightAreaMaterial(GameObject instance, string slotId)
    {
        Renderer renderer = instance.GetComponentInChildren<Renderer>();

        if (renderer == null)
            return;

        Material material = GetMaterialForSlot(slotId);

        if (material != null)
            renderer.sharedMaterial = material;
    }

    private Material GetMaterialForSlot(string slotId)
    {
        if (slotId == "0" || slotId == "00")
            return greenMaterial;

        if (!int.TryParse(slotId, out int number))
            return null;

        return IsRedNumber(number) ? redMaterial : blackMaterial;
    }
}