using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RoundedTableLineMesh : MonoBehaviour
{
    [SerializeField] private Transform[] points;
    [SerializeField] private float lineWidth = 0.035f;
    [SerializeField] private float cornerRadius = 0.18f;
    [SerializeField] private int cornerSegments = 12;
    [SerializeField] private float yOffset = 0.003f;
    [SerializeField] private bool closedLoop = true;
    [SerializeField] private bool rebuildInEditMode = true;

    private const float MinDistance = 0.001f;

    private Mesh generatedMesh;

    private void Awake()
    {
        Build();
    }

    private void OnValidate()
    {
        lineWidth = Mathf.Max(0.001f, lineWidth);
        cornerRadius = Mathf.Max(0f, cornerRadius);
        cornerSegments = Mathf.Max(1, cornerSegments);

        if (!Application.isPlaying && rebuildInEditMode)
            Build();
    }

    [ContextMenu("Rebuild Mesh")]
    public void Build()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null)
            return;

        List<Vector3> sourcePoints = GetLocalPoints();

        if (sourcePoints.Count < 2)
        {
            ClearMesh(meshFilter);
            return;
        }

        List<Vector3> linePoints = BuildLinePoints(sourcePoints);

        if (linePoints.Count < 2)
        {
            ClearMesh(meshFilter);
            return;
        }

        Mesh mesh = GetOrCreateMesh();
        FillMesh(mesh, linePoints);
        meshFilter.sharedMesh = mesh;
    }

    public void Configure(Transform[] newPoints, Material material, float newLineWidth, float newCornerRadius, int newCornerSegments, float newYOffset, bool newClosedLoop)
    {
        points = newPoints;
        lineWidth = Mathf.Max(0.001f, newLineWidth);
        cornerRadius = Mathf.Max(0f, newCornerRadius);
        cornerSegments = Mathf.Max(1, newCornerSegments);
        yOffset = newYOffset;
        closedLoop = newClosedLoop;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
            meshRenderer.sharedMaterial = material;

        Build();
    }

    private List<Vector3> GetLocalPoints()
    {
        List<Vector3> localPoints = new List<Vector3>();

        if (points == null)
            return localPoints;

        foreach (Transform point in points)
        {
            if (point == null)
                continue;

            Vector3 localPoint = transform.InverseTransformPoint(point.position);
            localPoint.y = yOffset;
            localPoints.Add(localPoint);
        }

        return localPoints;
    }

    private List<Vector3> BuildLinePoints(List<Vector3> sourcePoints)
    {
        List<Vector3> linePoints = new List<Vector3>();
        int pointCount = sourcePoints.Count;

        for (int i = 0; i < pointCount; i++)
        {
            bool isOpenEnd = !closedLoop && (i == 0 || i == pointCount - 1);

            if (isOpenEnd)
            {
                AddPoint(linePoints, sourcePoints[i]);
                continue;
            }

            Vector3 current = sourcePoints[i];
            Vector3 previous = sourcePoints[GetPreviousIndex(i, pointCount)];
            Vector3 next = sourcePoints[GetNextIndex(i, pointCount)];

            Vector3 fromPrevious = current - previous;
            Vector3 toNext = next - current;
            float previousLength = GetFlatMagnitude(fromPrevious);
            float nextLength = GetFlatMagnitude(toNext);

            if (previousLength < MinDistance || nextLength < MinDistance || cornerRadius <= 0f)
            {
                AddPoint(linePoints, current);
                continue;
            }

            float radius = Mathf.Min(cornerRadius, previousLength * 0.5f, nextLength * 0.5f);
            Vector3 beforeCorner = current - GetFlatDirection(fromPrevious) * radius;
            Vector3 afterCorner = current + GetFlatDirection(toNext) * radius;

            AddPoint(linePoints, beforeCorner);

            for (int segment = 1; segment <= cornerSegments; segment++)
            {
                float t = segment / (float)cornerSegments;
                AddPoint(linePoints, GetCornerPoint(beforeCorner, current, afterCorner, t));
            }
        }

        return linePoints;
    }

    private void FillMesh(Mesh mesh, List<Vector3> linePoints)
    {
        int pointCount = linePoints.Count;
        bool loop = closedLoop && pointCount > 2;
        int segmentCount = loop ? pointCount : pointCount - 1;

        Vector3[] vertices = new Vector3[pointCount * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segmentCount * 6];
        float halfWidth = lineWidth * 0.5f;

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 direction = GetPointDirection(linePoints, i, loop);
            Vector3 side = new Vector3(-direction.z, 0f, direction.x) * halfWidth;

            vertices[i * 2] = linePoints[i] + side;
            vertices[i * 2 + 1] = linePoints[i] - side;
            uvs[i * 2] = new Vector2(0f, i);
            uvs[i * 2 + 1] = new Vector2(1f, i);
        }

        int triangleIndex = 0;

        for (int i = 0; i < segmentCount; i++)
        {
            int nextIndex = (i + 1) % pointCount;
            int leftA = i * 2;
            int rightA = leftA + 1;
            int leftB = nextIndex * 2;
            int rightB = leftB + 1;

            triangles[triangleIndex++] = leftA;
            triangles[triangleIndex++] = leftB;
            triangles[triangleIndex++] = rightA;

            triangles[triangleIndex++] = rightA;
            triangles[triangleIndex++] = leftB;
            triangles[triangleIndex++] = rightB;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private Mesh GetOrCreateMesh()
    {
        if (generatedMesh != null)
            return generatedMesh;

        generatedMesh = new Mesh
        {
            name = "Rounded Table Line Mesh"
        };

        return generatedMesh;
    }

    private void ClearMesh(MeshFilter meshFilter)
    {
        if (generatedMesh != null)
            generatedMesh.Clear();

        meshFilter.sharedMesh = generatedMesh;
    }

    private static int GetPreviousIndex(int index, int count)
    {
        return index == 0 ? count - 1 : index - 1;
    }

    private static int GetNextIndex(int index, int count)
    {
        return index == count - 1 ? 0 : index + 1;
    }

    private static Vector3 GetCornerPoint(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float inverseT = 1f - t;
        return inverseT * inverseT * start + 2f * inverseT * t * control + t * t * end;
    }

    private static Vector3 GetPointDirection(List<Vector3> linePoints, int index, bool loop)
    {
        int count = linePoints.Count;

        if (!loop)
        {
            if (index == 0)
                return GetFlatDirection(linePoints[1] - linePoints[0]);

            if (index == count - 1)
                return GetFlatDirection(linePoints[count - 1] - linePoints[count - 2]);
        }

        Vector3 previous = linePoints[loop ? GetPreviousIndex(index, count) : index - 1];
        Vector3 next = linePoints[loop ? GetNextIndex(index, count) : index + 1];
        return GetFlatDirection(next - previous);
    }

    private static Vector3 GetFlatDirection(Vector3 value)
    {
        value.y = 0f;

        if (value.sqrMagnitude < MinDistance * MinDistance)
            return Vector3.forward;

        return value.normalized;
    }

    private static float GetFlatMagnitude(Vector3 value)
    {
        value.y = 0f;
        return value.magnitude;
    }

    private static void AddPoint(List<Vector3> list, Vector3 point)
    {
        if (list.Count > 0 && Vector3.Distance(list[list.Count - 1], point) < MinDistance)
            return;

        list.Add(point);
    }
}
