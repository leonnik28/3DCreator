using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class DecalManager : MonoBehaviour
{
    [Header("Настройки декалей")]
    public GameObject decalPrefab; // Префаб с Quad и материалом
    public LayerMask modelLayer = 1 << 0; // Слой модели
    public float defaultDecalSize = 0.2f;

    [Header("UI References")]
    public TextureLoader textureLoader;
    public Transform decalParent; // Родительский объект для декалей

    private List<DecalController> activeDecals = new List<DecalController>();
    private DecalController selectedDecal;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        if (textureLoader != null)
        {
            textureLoader.OnTextureLoaded += CreateDecalWithTexture;
        }
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0)) // Левая кнопка мыши
        {
            if (!EventSystem.current.IsPointerOverGameObject()) // Не над UI
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100f, modelLayer))
                {
                    // Проверяем, кликнули ли по существующему декалю
                    DecalController decal = hit.collider.GetComponentInParent<DecalController>();

                    if (decal != null)
                    {
                        SelectDecal(decal);
                    }
                    else
                    {
                        // Если у нас есть загруженная текстура, создаем новый декаль
                        if (textureLoader != null && textureLoader.lastLoadedTexture != null)
                        {
                            CreateDecalAtPoint(hit.point, hit.normal, hit.transform);
                        }
                    }
                }
            }
        }

        // Управление выбранным декалем
        if (selectedDecal != null)
        {
            // Масштабирование колесиком мыши
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                float newSize = selectedDecal.Size * (1 + scroll * 2f);
                selectedDecal.SetSize(newSize);
            }

            // Перемещение с зажатой клавишей Ctrl
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButton(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100f, modelLayer))
                {
                    selectedDecal.MoveToPoint(hit.point, hit.normal);
                }
            }

            // Удаление выделенного декаля
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            {
                DeleteDecal(selectedDecal);
            }
        }
    }

    void CreateDecalWithTexture(Texture2D texture)
    {
        // Текстура загружена, ждем клика по модели
        Debug.Log("Текстура загружена, кликните по модели для размещения");
    }

    void CreateDecalAtPoint(Vector3 position, Vector3 normal, Transform parentTransform)
    {
        if (decalPrefab == null || textureLoader.lastLoadedTexture == null)
        {
            Debug.LogError("Нет префаба декаля или текстуры");
            return;
        }

        // Создаем новый декаль
        GameObject decalObj = Instantiate(decalPrefab, position, Quaternion.identity, decalParent);
        DecalController decal = decalObj.GetComponent<DecalController>();

        if (decal != null)
        {
            // Настраиваем декаль
            decal.Initialize(textureLoader.lastLoadedTexture, normal, defaultDecalSize);
            decal.OnSelected += OnDecalSelected;
            decal.OnDeleted += OnDecalDeleted;

            activeDecals.Add(decal);
            SelectDecal(decal);
        }
    }

    void SelectDecal(DecalController decal)
    {
        if (selectedDecal != null)
        {
            selectedDecal.SetSelected(false);
        }

        selectedDecal = decal;
        if (selectedDecal != null)
        {
            selectedDecal.SetSelected(true);
        }
    }

    void OnDecalSelected(DecalController decal)
    {
        SelectDecal(decal);
    }

    void OnDecalDeleted(DecalController decal)
    {
        activeDecals.Remove(decal);
        if (selectedDecal == decal)
        {
            selectedDecal = null;
        }
    }

    void DeleteDecal(DecalController decal)
    {
        if (decal != null)
        {
            activeDecals.Remove(decal);
            if (selectedDecal == decal)
            {
                selectedDecal = null;
            }
            Destroy(decal.gameObject);
        }
    }

    public void ClearAllDecals()
    {
        foreach (var decal in activeDecals)
        {
            if (decal != null)
                Destroy(decal.gameObject);
        }
        activeDecals.Clear();
        selectedDecal = null;
    }
}