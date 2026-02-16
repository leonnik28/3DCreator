using UnityEngine;
using System;

public class DecalController : MonoBehaviour
{
    [Header("Компоненты")]
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public BoxCollider boxCollider;

    [Header("Настройки обводки")]
    public Color borderColor = Color.yellow;
    [Range(0.001f, 0.1f)]
    public float borderWidth = 0.005f;

    public event Action<DecalController> OnSelected;
    public event Action<DecalController> OnDeleted;

    private Texture2D decalTexture;
    private Material decalMaterial;
    private GameObject outlineObject;
    private bool isSelected = false;
    private float currentSize = 0.2f;

    // Сохраняем оригинальные вершины для обводки
    private Vector3[] baseOutlineVertices;

    public float Size => currentSize;

    void Awake()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();

        // Создаем базовые вершины для обводки (нормализованный квадрат)
        baseOutlineVertices = new Vector3[4];
        baseOutlineVertices[0] = new Vector3(-0.5f, -0.5f, 0);
        baseOutlineVertices[1] = new Vector3(0.5f, -0.5f, 0);
        baseOutlineVertices[2] = new Vector3(-0.5f, 0.5f, 0);
        baseOutlineVertices[3] = new Vector3(0.5f, 0.5f, 0);
    }

    public void Initialize(Texture2D texture, Vector3 surfaceNormal, float size)
    {
        decalTexture = texture;
        currentSize = size;

        // Создаем материал декаля
        decalMaterial = new Material(Shader.Find("Standard"));
        decalMaterial.mainTexture = texture;
        decalMaterial.SetFloat("_Mode", 3);
        decalMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        decalMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        decalMaterial.SetInt("_ZWrite", 0);
        decalMaterial.EnableKeyword("_ALPHABLEND_ON");

        // Основной декаль должен рендериться ПОСЛЕ обводки (чтобы быть сверху)
        decalMaterial.renderQueue = 3001;

        meshRenderer.material = decalMaterial;

        // Создаем Mesh
        CreateMesh();

        // Устанавливаем позицию и поворот
        MoveToPoint(transform.position, surfaceNormal);

        // Устанавливаем размер
        SetSize(size);
    }

    void CreateMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-0.5f, -0.5f, 0);
        vertices[1] = new Vector3(0.5f, -0.5f, 0);
        vertices[2] = new Vector3(-0.5f, 0.5f, 0);
        vertices[3] = new Vector3(0.5f, 0.5f, 0);

        int[] triangles = new int[6];
        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 1;
        triangles[3] = 2;
        triangles[4] = 3;
        triangles[5] = 1;

        Vector2[] uv = new Vector2[4];
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        // Обновляем коллайдер
        boxCollider.center = Vector3.zero;
        boxCollider.size = Vector3.one;
    }

    // Создание обводки снизу
    void CreateOutlineObject()
    {
        if (outlineObject != null) return;

        // Создаем отдельный объект для обводки
        outlineObject = new GameObject("Outline");
        outlineObject.transform.SetParent(transform);

        // Устанавливаем локальные трансформации
        outlineObject.transform.localPosition = new Vector3(0, 0, 0.001f);
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;

        // Копируем компоненты
        MeshFilter outlineMeshFilter = outlineObject.AddComponent<MeshFilter>();
        MeshRenderer outlineMeshRenderer = outlineObject.AddComponent<MeshRenderer>();

        // Создаем меш для обводки на основе базовых вершин
        Mesh outlineMesh = new Mesh();
        outlineMesh.vertices = (Vector3[])baseOutlineVertices.Clone();
        outlineMesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        outlineMesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        outlineMesh.RecalculateNormals();

        outlineMeshFilter.mesh = outlineMesh;

        // Создаем материал обводки
        Material outlineMaterial = new Material(Shader.Find("Standard"));
        outlineMaterial.color = borderColor;
        outlineMaterial.SetFloat("_Mode", 3);
        outlineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        outlineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        outlineMaterial.SetInt("_ZWrite", 0);
        outlineMaterial.EnableKeyword("_ALPHABLEND_ON");

        // ВАЖНО: Обводка должна рендериться РАНЬШЕ (быть снизу)
        outlineMaterial.renderQueue = 2999;

        outlineMeshRenderer.material = outlineMaterial;

        // Отключаем тени
        outlineMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineMeshRenderer.receiveShadows = false;

        // Применяем текущий размер к обводке
        UpdateOutlineMesh();

        // По умолчанию обводка выключена
        outlineObject.SetActive(false);
    }

    public void MoveToPoint(Vector3 position, Vector3 normal)
    {
        transform.position = position + normal * 0.01f;
        Quaternion rotation = Quaternion.LookRotation(-normal, Vector3.up);
        transform.rotation = rotation;
    }

    public void SetSize(float size)
    {
        currentSize = Mathf.Clamp(size, 0.05f, 1f);
        transform.localScale = Vector3.one * currentSize;

        // Если обводка существует, обновляем её
        if (outlineObject != null)
        {
            UpdateOutlineMesh();
        }
    }

    void UpdateOutlineMesh()
    {
        if (outlineObject == null) return;

        MeshFilter outlineMeshFilter = outlineObject.GetComponent<MeshFilter>();
        if (outlineMeshFilter == null || outlineMeshFilter.mesh == null) return;

        Mesh outlineMesh = outlineMeshFilter.mesh;
        Vector3[] vertices = new Vector3[4];

        // Рассчитываем расширение на основе текущего размера
        float expandAmount = borderWidth / currentSize;

        // Создаем увеличенные вершины на основе базовых
        for (int i = 0; i < 4; i++)
        {
            vertices[i] = new Vector3(
                baseOutlineVertices[i].x * (1 + expandAmount),
                baseOutlineVertices[i].y * (1 + expandAmount),
                baseOutlineVertices[i].z
            );
        }

        outlineMesh.vertices = vertices;
        outlineMesh.RecalculateBounds();
        outlineMeshFilter.mesh = outlineMesh;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (isSelected)
        {
            if (outlineObject == null)
            {
                CreateOutlineObject();
            }

            // Всегда обновляем обводку перед показом
            if (outlineObject != null)
            {
                UpdateOutlineMesh();
                outlineObject.SetActive(true);
            }
        }
        else
        {
            if (outlineObject != null)
                outlineObject.SetActive(false);
        }
    }

    void OnMouseDown()
    {
        if (!isSelected)
        {
            OnSelected?.Invoke(this);
        }
    }

    void OnDestroy()
    {
        if (decalMaterial != null)
            Destroy(decalMaterial);
        if (outlineObject != null)
            Destroy(outlineObject);
    }
}