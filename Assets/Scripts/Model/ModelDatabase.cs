using UnityEngine;

/// <summary>
/// База данных моделей для выбора. ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "ModelDatabase", menuName = "Fotocentr/Model Database")]
public class ModelDatabase : ScriptableObject
{
    [System.Serializable]
    public struct ModelEntry
    {
        public string DisplayName;
        public GameObject Prefab;
        public Sprite Thumbnail;
    }

    [SerializeField] private ModelEntry[] _models = new ModelEntry[0];

    public int Count => _models?.Length ?? 0;
    public ModelEntry this[int index] => _models[index];
    public ModelEntry[] Models => _models;
}
