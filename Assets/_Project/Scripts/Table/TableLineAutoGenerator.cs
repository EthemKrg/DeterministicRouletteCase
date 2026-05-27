using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TableLineAutoGenerator : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Transform sourceRoot;
    [SerializeField] private Material lineMaterial;

    [Header("Line")]
    [SerializeField] private float lineWidth = 0.025f;
    [SerializeField] private float cornerRadius = 0.03f;
    [SerializeField] private int cornerSegments = 4;
    [SerializeField] private float yOffset = 0.006f;
    [SerializeField] private float boundsPadding = 0.01f;
    [SerializeField] private float coordinateMergeTolerance = 0.02f;
    [SerializeField] private float parallelLineMergeDistance = 0.12f;
    [SerializeField] private float segmentEndOverlap = 0.02f;
    [SerializeField] private bool includeInactiveSources = true;

    private const string GeneratedRootName = "Generated Table Lines";
    private const float MinSegmentLength = 0.001f;

    [ContextMenu("Generate Lines From Source")]
    public void GenerateLinesFromSource()
    {
        ClearGeneratedLines();

        Transform root = sourceRoot != null ? sourceRoot : transform;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        List<LineSegment> verticalSegments = new List<LineSegment>();
        List<LineSegment> horizontalSegments = new List<LineSegment>();
        Bounds totalBounds = new Bounds();
        bool hasBounds = false;
        Material fallbackMaterial = lineMaterial;
        int usedRendererCount = 0;

        foreach (Renderer sourceRenderer in renderers)
        {
            if (!ShouldUseRenderer(sourceRenderer, root))
                continue;

            if (fallbackMaterial == null)
                fallbackMaterial = sourceRenderer.sharedMaterial;

            Bounds bounds = sourceRenderer.bounds;
            bounds.Expand(boundsPadding * 2f);

            AddBoundsSegments(bounds, verticalSegments, horizontalSegments);
            usedRendererCount++;

            if (!hasBounds)
            {
                totalBounds = bounds;
                hasBounds = true;
            }
            else
            {
                totalBounds.Encapsulate(bounds);
            }
        }

        if (!hasBounds)
        {
            Debug.LogWarning($"{nameof(TableLineAutoGenerator)} could not find table cell renderers under {root.name}. Check sourceRoot and object names.", this);
            return;
        }

        RemoveOuterBoundarySegments(verticalSegments, horizontalSegments, totalBounds);
        MergeParallelLineAxes(verticalSegments);
        MergeParallelLineAxes(horizontalSegments);
        MergeSegments(verticalSegments);
        MergeSegments(horizontalSegments);

        Transform generatedRoot = CreateGeneratedRoot();
        int lineIndex = 0;

        foreach (LineSegment segment in verticalSegments)
            CreateOpenLine(generatedRoot, $"Vertical_{lineIndex++}", segment, fallbackMaterial);

        foreach (LineSegment segment in horizontalSegments)
            CreateOpenLine(generatedRoot, $"Horizontal_{lineIndex++}", segment, fallbackMaterial);

        CreateBorderLine(generatedRoot, totalBounds, fallbackMaterial);

        Debug.Log($"{nameof(TableLineAutoGenerator)} generated {lineIndex + 1} table lines from {usedRendererCount} renderers.", this);
    }

    [ContextMenu("Clear Generated Lines")]
    public void ClearGeneratedLines()
    {
        Transform generatedRoot = transform.Find(GeneratedRootName);

        if (generatedRoot == null)
            return;

        if (Application.isPlaying)
            Destroy(generatedRoot.gameObject);
        else
            DestroyImmediate(generatedRoot.gameObject);
    }

    private Transform CreateGeneratedRoot()
    {
        GameObject rootObject = new GameObject(GeneratedRootName);
        rootObject.transform.SetParent(transform, false);
        return rootObject.transform;
    }

    private void AddBoundsSegments(Bounds bounds, List<LineSegment> verticalSegments, List<LineSegment> horizontalSegments)
    {
        float minX = Snap(bounds.min.x);
        float maxX = Snap(bounds.max.x);
        float minZ = Snap(bounds.min.z);
        float maxZ = Snap(bounds.max.z);

        verticalSegments.Add(LineSegment.Vertical(minX, minZ, maxZ));
        verticalSegments.Add(LineSegment.Vertical(maxX, minZ, maxZ));
        horizontalSegments.Add(LineSegment.Horizontal(minZ, minX, maxX));
        horizontalSegments.Add(LineSegment.Horizontal(maxZ, minX, maxX));
    }

    private void MergeSegments(List<LineSegment> segments)
    {
        segments.Sort(CompareSegments);

        for (int i = segments.Count - 1; i > 0; i--)
        {
            LineSegment current = segments[i];
            LineSegment previous = segments[i - 1];

            if (!CanMerge(previous, current))
                continue;

            previous.End = Mathf.Max(previous.End, current.End);
            segments[i - 1] = previous;
            segments.RemoveAt(i);
        }
    }

    private void RemoveOuterBoundarySegments(List<LineSegment> verticalSegments, List<LineSegment> horizontalSegments, Bounds totalBounds)
    {
        float minX = Snap(totalBounds.min.x);
        float maxX = Snap(totalBounds.max.x);
        float minZ = Snap(totalBounds.min.z);
        float maxZ = Snap(totalBounds.max.z);

        verticalSegments.RemoveAll(segment => Mathf.Abs(segment.Axis - minX) <= coordinateMergeTolerance || Mathf.Abs(segment.Axis - maxX) <= coordinateMergeTolerance);
        horizontalSegments.RemoveAll(segment => Mathf.Abs(segment.Axis - minZ) <= coordinateMergeTolerance || Mathf.Abs(segment.Axis - maxZ) <= coordinateMergeTolerance);
    }

    private void MergeParallelLineAxes(List<LineSegment> segments)
    {
        if (parallelLineMergeDistance <= 0f || segments.Count <= 1)
            return;

        segments.Sort(CompareSegments);

        int groupStart = 0;

        for (int i = 1; i <= segments.Count; i++)
        {
            bool endOfGroup = i == segments.Count || Mathf.Abs(segments[i].Axis - segments[i - 1].Axis) > parallelLineMergeDistance;

            if (!endOfGroup)
                continue;

            SetGroupAxisToAverage(segments, groupStart, i);
            groupStart = i;
        }
    }

    private void SetGroupAxisToAverage(List<LineSegment> segments, int startIndex, int endIndex)
    {
        if (endIndex - startIndex <= 1)
            return;

        float axisTotal = 0f;

        for (int i = startIndex; i < endIndex; i++)
            axisTotal += segments[i].Axis;

        float axisAverage = Snap(axisTotal / (endIndex - startIndex));

        for (int i = startIndex; i < endIndex; i++)
        {
            LineSegment segment = segments[i];
            segment.Axis = axisAverage;
            segments[i] = segment;
        }
    }

    private bool CanMerge(LineSegment first, LineSegment second)
    {
        return first.IsVertical == second.IsVertical
            && Mathf.Abs(first.Axis - second.Axis) <= coordinateMergeTolerance
            && second.Start <= first.End + coordinateMergeTolerance;
    }

    private int CompareSegments(LineSegment first, LineSegment second)
    {
        int axisCompare = first.Axis.CompareTo(second.Axis);

        if (axisCompare != 0)
            return axisCompare;

        return first.Start.CompareTo(second.Start);
    }

    private void CreateOpenLine(Transform parent, string lineName, LineSegment segment, Material material)
    {
        if (segment.End - segment.Start < MinSegmentLength)
            return;

        Vector3 start = segment.IsVertical
            ? new Vector3(segment.Axis, yOffset, segment.Start)
            : new Vector3(segment.Start, yOffset, segment.Axis);

        Vector3 end = segment.IsVertical
            ? new Vector3(segment.Axis, yOffset, segment.End)
            : new Vector3(segment.End, yOffset, segment.Axis);

        Vector3 direction = (end - start).normalized;
        start -= direction * segmentEndOverlap;
        end += direction * segmentEndOverlap;

        CreateLine(parent, lineName, new[] { start, end }, false, 0f, material);
    }

    private void CreateBorderLine(Transform parent, Bounds bounds, Material material)
    {
        float minX = Snap(bounds.min.x);
        float maxX = Snap(bounds.max.x);
        float minZ = Snap(bounds.min.z);
        float maxZ = Snap(bounds.max.z);

        Vector3[] points =
        {
            new Vector3(minX, yOffset, minZ),
            new Vector3(minX, yOffset, maxZ),
            new Vector3(maxX, yOffset, maxZ),
            new Vector3(maxX, yOffset, minZ)
        };

        CreateLine(parent, "Outer_Border", points, true, cornerRadius, material);
    }

    private void CreateLine(Transform parent, string lineName, Vector3[] worldPoints, bool closedLoop, float radius, Material material)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(parent, false);

        Transform[] pointTransforms = new Transform[worldPoints.Length];

        for (int i = 0; i < worldPoints.Length; i++)
        {
            GameObject pointObject = new GameObject($"Point_{i}");
            pointObject.transform.SetParent(lineObject.transform, false);
            pointObject.transform.position = worldPoints[i];
            pointTransforms[i] = pointObject.transform;
        }

        RoundedTableLineMesh lineMesh = lineObject.AddComponent<RoundedTableLineMesh>();
        lineMesh.Configure(pointTransforms, material, lineWidth, radius, cornerSegments, yOffset, closedLoop);
    }

    private bool ShouldUseRenderer(Renderer sourceRenderer, Transform root)
    {
        if (sourceRenderer == null || !sourceRenderer.enabled)
            return false;

        if (!includeInactiveSources && !sourceRenderer.gameObject.activeInHierarchy)
            return false;

        Transform current = sourceRenderer.transform;
        bool matchedCell = false;

        while (current != null)
        {
            string objectName = current.name;

            if (objectName.Contains("Label"))
                return false;

            RouletteBetArea betArea = current.GetComponent<RouletteBetArea>();

            if (IsTableCellName(objectName) || IsDrawableBetArea(betArea))
                matchedCell = true;

            if (current == root)
                break;

            current = current.parent;
        }

        return matchedCell;
    }

    private static bool IsTableCellName(string objectName)
    {
        return objectName.StartsWith("EU_Number_")
            || objectName.StartsWith("US_Number_")
            || objectName.StartsWith("EU_Zero")
            || objectName.StartsWith("US_Zero")
            || objectName.StartsWith("EU_Dozen_")
            || objectName.StartsWith("US_Dozen_")
            || objectName.StartsWith("EU_Outside_")
            || objectName.StartsWith("US_Outside_")
            || objectName.StartsWith("EU_TwoToOne_")
            || objectName.StartsWith("US_TwoToOne_")
            || objectName.StartsWith("BetArea_Straight_")
            || objectName.StartsWith("BetArea_Dozen_")
            || objectName.StartsWith("BetArea_Column_")
            || objectName.StartsWith("BetArea_Outside_");
    }

    private static bool IsDrawableBetArea(RouletteBetArea betArea)
    {
        if (betArea == null)
            return false;

        return betArea.BetType == BetType.Straight
            || betArea.BetType == BetType.Dozen
            || betArea.BetType == BetType.Column
            || betArea.BetType == BetType.Red
            || betArea.BetType == BetType.Black
            || betArea.BetType == BetType.Even
            || betArea.BetType == BetType.Odd
            || betArea.BetType == BetType.Low
            || betArea.BetType == BetType.High;
    }

    private float Snap(float value)
    {
        if (coordinateMergeTolerance <= 0f)
            return value;

        return Mathf.Round(value / coordinateMergeTolerance) * coordinateMergeTolerance;
    }

    private struct LineSegment
    {
        public bool IsVertical;
        public float Axis;
        public float Start;
        public float End;

        public static LineSegment Vertical(float x, float startZ, float endZ)
        {
            return Create(true, x, startZ, endZ);
        }

        public static LineSegment Horizontal(float z, float startX, float endX)
        {
            return Create(false, z, startX, endX);
        }

        private static LineSegment Create(bool isVertical, float axis, float start, float end)
        {
            if (start > end)
            {
                float temp = start;
                start = end;
                end = temp;
            }

            return new LineSegment
            {
                IsVertical = isVertical,
                Axis = axis,
                Start = start,
                End = end
            };
        }
    }
}
