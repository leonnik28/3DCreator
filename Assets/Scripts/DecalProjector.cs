using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class DecalProjector : MonoBehaviour
{
    [Header("Decal Settings")]
    [SerializeField] private float _size = 1f;
    [SerializeField] private float _offset = 0.01f; // Небольшое смещение чтобы избежать z-fighting

    private Mesh _decalMesh;
    private Material _decalMaterial;
    private Texture2D _decalTexture;

    public void Initialize(Texture2D texture, Vector3 hitPoint, Vector3 hitNormal, GameObject targetObject)
    {
        _decalTexture = texture;

        // Создаем материал
        _decalMaterial = new Material(Shader.Find("Standard"));
        _decalMaterial.mainTexture = texture;
        _decalMaterial.SetFloat("_Mode", 3); // Transparent mode
        _decalMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _decalMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _decalMaterial.SetInt("_ZWrite", 0);
        _decalMaterial.EnableKeyword("_ALPHABLEND_ON");

        GetComponent<MeshRenderer>().material = _decalMaterial;

        // Позиционируем проектор
        transform.position = hitPoint + hitNormal * _offset;
        transform.rotation = Quaternion.LookRotation(-hitNormal, Vector3.up);

        // Создаем декаль-меш на основе целевого объекта
        CreateDecalMesh(targetObject, hitPoint, hitNormal);
    }

    private void CreateDecalMesh(GameObject targetObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        // Получаем меш целевого объекта
        MeshFilter targetMeshFilter = targetObject.GetComponent<MeshFilter>();
        if (targetMeshFilter == null || targetMeshFilter.sharedMesh == null)
        {
            Debug.LogError("Target object has no MeshFilter!");
            return;
        }

        Mesh targetMesh = targetMeshFilter.sharedMesh;
        Vector3[] targetVertices = targetMesh.vertices;
        Vector3[] targetNormals = targetMesh.normals;

        // Преобразуем вершины в мировые координаты
        Matrix4x4 localToWorld = targetObject.transform.localToWorldMatrix;

        // Создаем bounding box для проекции
        Bounds bounds = new Bounds(hitPoint, Vector3.one * _size);

        // Собираем вершины, попадающие в область проекции
        System.Collections.Generic.List<Vector3> projectedVerts = new System.Collections.Generic.List<Vector3>();
        System.Collections.Generic.List<Vector3> projectedNormals = new System.Collections.Generic.List<Vector3>();
        System.Collections.Generic.List<Vector2> projectedUVs = new System.Collections.Generic.List<Vector2>();

        // Матрица для преобразования в локальное пространство проектора
        Matrix4x4 worldToProjector = transform.worldToLocalMatrix;

        for (int i = 0; i < targetVertices.Length; i++)
        {
            // Вершина в мировых координатах
            Vector3 worldVert = localToWorld.MultiplyPoint(targetVertices[i]);

            // Проверяем, попадает ли вершина в bounds проектора
            if (bounds.Contains(worldVert))
            {
                // Нормаль в мировых координатах
                Vector3 worldNormal = localToWorld.MultiplyVector(targetNormals[i]).normalized;

                // Проверяем, смотрит ли нормаль в сторону проектора
                float dot = Vector3.Dot(worldNormal, hitNormal);
                if (dot > 0.5f) // Полусфера в направлении проектора
                {
                    // Конвертируем в локальное пространство проектора
                    Vector3 localVert = worldToProjector.MultiplyPoint(worldVert);

                    // Создаем UV на основе проекции
                    Vector2 uv = new Vector2(
                        (localVert.x / _size) + 0.5f,
                        (localVert.y / _size) + 0.5f
                    );

                    projectedVerts.Add(localVert);
                    projectedNormals.Add(worldToProjector.MultiplyVector(worldNormal));
                    projectedUVs.Add(uv);
                }
            }
        }

        if (projectedVerts.Count < 3)
        {
            Debug.LogWarning("Not enough vertices to create decal mesh!");
            return;
        }

        // Создаем меш декали
        _decalMesh = new Mesh();
        _decalMesh.vertices = projectedVerts.ToArray();
        _decalMesh.normals = projectedNormals.ToArray();
        _decalMesh.uv = projectedUVs.ToArray();

        // Генерируем треугольники (простая триангуляция - для сложных случаев нужно более продвинутое решение)
        GenerateTriangles(projectedVerts.Count);

        _decalMesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = _decalMesh;
    }

    private void GenerateTriangles(int vertexCount)
    {
        // Простая триангуляция - создает сетку из квадов
        // Для production нужно использовать более сложный алгоритм
        int quadCount = (vertexCount / 4) * 4;
        int[] triangles = new int[quadCount * 3 / 2]; // 6 индексов на квад

        for (int i = 0; i < quadCount; i += 4)
        {
            int idx = i * 3 / 2;
            triangles[idx] = i;
            triangles[idx + 1] = i + 1;
            triangles[idx + 2] = i + 2;
            triangles[idx + 3] = i + 2;
            triangles[idx + 4] = i + 3;
            triangles[idx + 5] = i;
        }

        GetComponent<MeshFilter>().mesh.triangles = triangles;
    }

    public void SetSize(float size)
    {
        _size = size;
        transform.localScale = Vector3.one * size;
    }
}