using UnityEditor;
using UnityEngine;

public class RouletteTableAreaBuilderWindow : EditorWindow
{
    [SerializeField] private Transform parent;
    [SerializeField] private GameObject betAreaPrefab;

    [SerializeField] private Vector3 startPosition = new Vector3(-7f, 0.1f, -1.2f);
    [SerializeField] private Vector2 cellSize = new Vector2(1.2f, 1.2f);
    [SerializeField] private Vector3 areaScale = new Vector3(1f, 0.01f, 1f);

    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material redMaterial;
    [SerializeField] private Material blackMaterial;
    [SerializeField] private Material greenMaterial;

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

        defaultMaterial = (Material)EditorGUILayout.ObjectField("Default Material", defaultMaterial, typeof(Material), false);
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

        if (GUILayout.Button("Generate Outside Areas"))
            GenerateOutsideAreas();

        if (GUILayout.Button("Generate Street and Six Line Areas"))
            GenerateStreetAndSixLineAreas();

        if (GUILayout.Button("Generate Split Areas"))
            GenerateSplitAreas();

        if (GUILayout.Button("Generate Corner Areas"))
            GenerateCornerAreas();
    }

    private void GenerateStraightAreas()
    {
        if (!CanGenerate())
            return;

        Transform root = GetOrCreateChild(parent, "Straight");
        ClearChildren(root);

        CreateStraightArea(
            root,
            "0",
            0,
            new Vector3(startPosition.x - cellSize.x, startPosition.y, startPosition.z + cellSize.y));

        for (int number = 1; number <= 36; number++)
        {
            int row = (number - 1) % RouletteTableLayout.RowCount;
            int column = (number - 1) / RouletteTableLayout.RowCount;

            Vector3 position = new Vector3(
                startPosition.x + column * cellSize.x,
                startPosition.y,
                startPosition.z + row * cellSize.y);

            CreateStraightArea(root, number.ToString(), number, position);
        }

        Debug.Log("Generated straight bet areas 0-36.");
    }

    private void GenerateOutsideAreas()
    {
        if (!CanGenerate())
            return;

        Transform outsideRoot = GetOrCreateChild(parent, "Outside");
        Transform dozenRoot = GetOrCreateChild(parent, "Dozens");
        Transform columnRoot = GetOrCreateChild(parent, "Columns");

        ClearChildren(outsideRoot);
        ClearChildren(dozenRoot);
        ClearChildren(columnRoot);

        float tableWidth = cellSize.x * RouletteTableLayout.ColumnCount;
        float centerX = startPosition.x + tableWidth * 0.5f - cellSize.x * 0.5f;

        float dozenZ = startPosition.z - cellSize.y * 1.15f;
        float outsideZ = startPosition.z - cellSize.y * 2.15f;
        float columnX = startPosition.x + cellSize.x * 12.15f;

        Vector3 dozenScale = new Vector3(cellSize.x * 3.8f, areaScale.y, areaScale.z);
        Vector3 outsideScale = new Vector3(cellSize.x * 1.8f, areaScale.y, areaScale.z);
        Vector3 columnScale = new Vector3(areaScale.x, areaScale.y, areaScale.z);

        CreateArea(
            dozenRoot,
            "BetArea_Dozen_1",
            BetType.Dozen,
            "1st 12",
            new Vector3(startPosition.x + cellSize.x * 1.5f, startPosition.y, dozenZ),
            dozenScale,
            index: 1);

        CreateArea(
            dozenRoot,
            "BetArea_Dozen_2",
            BetType.Dozen,
            "2nd 12",
            new Vector3(startPosition.x + cellSize.x * 5.5f, startPosition.y, dozenZ),
            dozenScale,
            index: 2);

        CreateArea(
            dozenRoot,
            "BetArea_Dozen_3",
            BetType.Dozen,
            "3rd 12",
            new Vector3(startPosition.x + cellSize.x * 9.5f, startPosition.y, dozenZ),
            dozenScale,
            index: 3);

        CreateArea(
            outsideRoot,
            "BetArea_Low",
            BetType.Low,
            "1-18",
            new Vector3(centerX - cellSize.x * 5f, startPosition.y, outsideZ),
            outsideScale);

        CreateArea(
            outsideRoot,
            "BetArea_Even",
            BetType.Even,
            "Even",
            new Vector3(centerX - cellSize.x * 3f, startPosition.y, outsideZ),
            outsideScale);

        GameObject redArea = CreateArea(
            outsideRoot,
            "BetArea_Red",
            BetType.Red,
            "Red",
            new Vector3(centerX - cellSize.x, startPosition.y, outsideZ),
            outsideScale);

        SetAreaMaterial(redArea, redMaterial);

        GameObject blackArea = CreateArea(
            outsideRoot,
            "BetArea_Black",
            BetType.Black,
            "Black",
            new Vector3(centerX + cellSize.x, startPosition.y, outsideZ),
            outsideScale);

        SetAreaMaterial(blackArea, blackMaterial);

        CreateArea(
            outsideRoot,
            "BetArea_Odd",
            BetType.Odd,
            "Odd",
            new Vector3(centerX + cellSize.x * 3f, startPosition.y, outsideZ),
            outsideScale);

        CreateArea(
            outsideRoot,
            "BetArea_High",
            BetType.High,
            "19-36",
            new Vector3(centerX + cellSize.x * 5f, startPosition.y, outsideZ),
            outsideScale);

        CreateArea(
            columnRoot,
            "BetArea_Column_1",
            BetType.Column,
            "Column 1",
            new Vector3(columnX, startPosition.y, startPosition.z),
            columnScale,
            index: 1);

        CreateArea(
            columnRoot,
            "BetArea_Column_2",
            BetType.Column,
            "Column 2",
            new Vector3(columnX, startPosition.y, startPosition.z + cellSize.y),
            columnScale,
            index: 2);

        CreateArea(
            columnRoot,
            "BetArea_Column_3",
            BetType.Column,
            "Column 3",
            new Vector3(columnX, startPosition.y, startPosition.z + cellSize.y * 2f),
            columnScale,
            index: 3);

        Debug.Log("Generated outside bet areas.");
    }

    private void GenerateStreetAndSixLineAreas()
    {
        if (!CanGenerate())
            return;

        Transform streetRoot = GetOrCreateChild(parent, "Street");
        Transform sixLineRoot = GetOrCreateChild(parent, "SixLine");

        ClearChildren(streetRoot);
        ClearChildren(sixLineRoot);

        float lineZ = startPosition.z - cellSize.y * 0.55f;

        Vector3 streetScale = new Vector3(cellSize.x * 0.65f, areaScale.y, cellSize.y * 0.22f);
        Vector3 sixLineScale = new Vector3(cellSize.x * 0.25f, areaScale.y, cellSize.y * 0.22f);

        for (int column = 0; column < RouletteTableLayout.ColumnCount; column++)
        {
            int startNumber = RouletteTableLayout.GetNumberAt(0, column);

            Vector3 position = new Vector3(
                startPosition.x + column * cellSize.x,
                startPosition.y,
                lineZ);

            CreateArea(
                streetRoot,
                $"BetArea_Street_{startNumber}",
                BetType.Street,
                GetStreetLabel(startNumber),
                position,
                streetScale,
                primaryNumber: startNumber);
        }

        for (int column = 0; column < RouletteTableLayout.ColumnCount - 1; column++)
        {
            int startNumber = RouletteTableLayout.GetNumberAt(0, column);

            Vector3 position = new Vector3(
                startPosition.x + (column + 0.5f) * cellSize.x,
                startPosition.y,
                lineZ);

            CreateArea(
                sixLineRoot,
                $"BetArea_SixLine_{startNumber}",
                BetType.SixLine,
                GetSixLineLabel(startNumber),
                position,
                sixLineScale,
                primaryNumber: startNumber);
        }

        Debug.Log("Generated street and six line bet areas.");
    }

    private void GenerateSplitAreas()
    {
        if (!CanGenerate())
            return;

        Transform splitRoot = GetOrCreateChild(parent, "Split");
        ClearChildren(splitRoot);

        Vector3 horizontalSplitScale = new Vector3(cellSize.x * 0.16f, areaScale.y, cellSize.y * 0.65f);
        Vector3 verticalSplitScale = new Vector3(cellSize.x * 0.65f, areaScale.y, cellSize.y * 0.16f);

        float splitY = startPosition.y + 0.01f;

        GenerateHorizontalSplits(splitRoot, horizontalSplitScale, splitY);
        GenerateVerticalSplits(splitRoot, verticalSplitScale, splitY);

        Debug.Log("Generated split bet areas.");
    }

    private void GenerateCornerAreas()
    {
        if (!CanGenerate())
            return;

        Transform cornerRoot = GetOrCreateChild(parent, "Corner");
        ClearChildren(cornerRoot);

        Vector3 cornerScale = new Vector3(cellSize.x * 0.22f, areaScale.y, cellSize.y * 0.22f);
        float cornerY = startPosition.y + 0.03f;

        for (int column = 0; column < RouletteTableLayout.ColumnCount - 1; column++)
        {
            for (int row = 0; row < RouletteTableLayout.RowCount - 1; row++)
            {
                int bottomLeftNumber = RouletteTableLayout.GetNumberAt(row, column);

                Vector3 position = new Vector3(
                    startPosition.x + (column + 0.5f) * cellSize.x,
                    cornerY,
                    startPosition.z + (row + 0.5f) * cellSize.y);

                CreateArea(
                    cornerRoot,
                    $"BetArea_Corner_{bottomLeftNumber}",
                    BetType.Corner,
                    string.Empty,
                    position,
                    cornerScale,
                    primaryNumber: bottomLeftNumber);
            }
        }

        Debug.Log("Generated corner bet areas.");
    }

    private void GenerateHorizontalSplits(Transform splitRoot, Vector3 splitScale, float splitY)
    {
        for (int column = 0; column < RouletteTableLayout.ColumnCount - 1; column++)
        {
            for (int row = 0; row < RouletteTableLayout.RowCount; row++)
            {
                int firstNumber = RouletteTableLayout.GetNumberAt(row, column);
                int secondNumber = RouletteTableLayout.GetNumberAt(row, column + 1);

                Vector3 position = new Vector3(
                    startPosition.x + (column + 0.5f) * cellSize.x,
                    splitY,
                    startPosition.z + row * cellSize.y);

                CreateArea(
                    splitRoot,
                    $"BetArea_Split_{firstNumber}_{secondNumber}",
                    BetType.Split,
                    $"{firstNumber}/{secondNumber}",
                    position,
                    splitScale,
                    primaryNumber: firstNumber,
                    secondaryNumber: secondNumber);
            }
        }
    }

    private void GenerateVerticalSplits(Transform splitRoot, Vector3 splitScale, float splitY)
    {
        for (int column = 0; column < RouletteTableLayout.ColumnCount; column++)
        {
            for (int row = 0; row < RouletteTableLayout.RowCount - 1; row++)
            {
                int firstNumber = RouletteTableLayout.GetNumberAt(row, column);
                int secondNumber = RouletteTableLayout.GetNumberAt(row + 1, column);

                Vector3 position = new Vector3(
                    startPosition.x + column * cellSize.x,
                    splitY,
                    startPosition.z + (row + 0.5f) * cellSize.y);

                CreateArea(
                    splitRoot,
                    $"BetArea_Split_{firstNumber}_{secondNumber}",
                    BetType.Split,
                    $"{firstNumber}/{secondNumber}",
                    position,
                    splitScale,
                    primaryNumber: firstNumber,
                    secondaryNumber: secondNumber);
            }
        }
    }

    private bool CanGenerate()
    {
        if (parent == null)
        {
            Debug.LogWarning("Parent is missing.");
            return false;
        }

        if (betAreaPrefab == null)
        {
            Debug.LogWarning("Bet area prefab is missing.");
            return false;
        }

        return true;
    }

    private void CreateStraightArea(Transform root, string slotId, int primaryNumber, Vector3 position)
    {
        GameObject instance = CreateArea(
            root,
            $"BetArea_Straight_{slotId}",
            BetType.Straight,
            slotId,
            position,
            areaScale,
            primaryNumber);

        RouletteBetArea betArea = instance.GetComponent<RouletteBetArea>();

        if (betArea == null)
            return;

        SerializedObject serializedObject = new SerializedObject(betArea);
        serializedObject.FindProperty("straightSlotId").stringValue = slotId;
        serializedObject.ApplyModifiedProperties();

        SetStraightAreaMaterial(instance, slotId);
        SetNumberArea(instance, slotId);
    }

    private static void SetNumberArea(GameObject instance, string slotId)
    {
        RouletteNumberArea numberArea = instance.GetComponent<RouletteNumberArea>();

        if (numberArea == null)
            numberArea = instance.AddComponent<RouletteNumberArea>();

        SerializedObject serializedObject = new SerializedObject(numberArea);
        serializedObject.FindProperty("slotId").stringValue = slotId;
        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(numberArea);
    }

    private GameObject CreateArea(
        Transform root,
        string objectName,
        BetType betType,
        string label,
        Vector3 localPosition,
        Vector3 localScale,
        int primaryNumber = 0,
        int secondaryNumber = 0,
        int index = 0)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(betAreaPrefab, root);

        instance.name = objectName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = localScale;
        instance.layer = betAreaLayer;

        RouletteBetArea betArea = instance.GetComponent<RouletteBetArea>();

        if (betArea == null)
        {
            Debug.LogWarning($"{instance.name} has no RouletteBetArea component.");
            return instance;
        }

        SerializedObject serializedObject = new SerializedObject(betArea);

        serializedObject.FindProperty("betType").enumValueIndex = (int)betType;
        serializedObject.FindProperty("primaryNumber").intValue = primaryNumber;
        serializedObject.FindProperty("secondaryNumber").intValue = secondaryNumber;
        serializedObject.FindProperty("index").intValue = index;

        SerializedProperty straightSlotIdProperty = serializedObject.FindProperty("straightSlotId");

        if (straightSlotIdProperty != null)
            straightSlotIdProperty.stringValue = string.Empty;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(betArea);

        SetLabel(instance, label);
        SetAreaMaterial(instance, defaultMaterial);

        return instance;
    }

    private void SetStraightAreaMaterial(GameObject instance, string slotId)
    {
        SetAreaMaterial(instance, GetMaterialForSlot(slotId));
    }

    private Material GetMaterialForSlot(string slotId)
    {
        RouletteSlot slot = RouletteWheelData.GetSlotById(slotId, RouletteWheelType.European);

        if (slot == null)
            return null;

        switch (slot.Color)
        {
            case RouletteColor.Green:
                return greenMaterial;

            case RouletteColor.Red:
                return redMaterial;

            case RouletteColor.Black:
                return blackMaterial;

            default:
                return null;
        }
    }

    private static void SetAreaMaterial(GameObject instance, Material material)
    {
        if (material == null)
            return;

        Renderer renderer = instance.GetComponentInChildren<Renderer>();

        if (renderer == null)
            return;

        renderer.sharedMaterial = material;
    }

    private static void SetLabel(GameObject instance, string text)
    {
        TMPro.TextMeshPro label = instance.GetComponentInChildren<TMPro.TextMeshPro>();

        if (label == null)
            return;

        label.text = text;
    }

    private static string GetStreetLabel(int startNumber)
    {
        return $"{startNumber}-{startNumber + 2}";
    }

    private static string GetSixLineLabel(int startNumber)
    {
        return $"{startNumber}-{startNumber + 5}";
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
