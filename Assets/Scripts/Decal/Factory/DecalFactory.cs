using UnityEngine;

/// <summary>
/// Фабрика для создания и уничтожения декалей.
/// </summary>
public class DecalFactory : MonoBehaviour
{
    [SerializeField] private GameObject _decalPrefab;
    [SerializeField] private Transform _decalParent;

    public DecalController CreateDecal(Texture2D texture, Vector3 position, Vector3 normal, float size, LayerMask targetLayers)
    {
        if (_decalPrefab == null)
        {
            Debug.LogError("DecalFactory: decal prefab is not assigned.");
            return null;
        }

        if (texture == null)
        {
            Debug.LogError("DecalFactory: texture is null.");
            return null;
        }

        var decalObj = Instantiate(_decalPrefab, position, Quaternion.identity, _decalParent);
        var decal = decalObj.GetComponent<DecalController>();

        if (decal == null)
        {
            Debug.LogError("DecalFactory: prefab must contain DecalController.");
            Destroy(decalObj);
            return null;
        }

        decal.Initialize(texture, position, normal, size, targetLayers);
        return decal;
    }

    public void DestroyDecal(DecalController decal)
    {
        if (decal != null)
            Destroy(decal.gameObject);
    }
}