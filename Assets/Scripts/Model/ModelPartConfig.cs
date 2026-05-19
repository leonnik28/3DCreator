using UnityEngine;
using System;

[Serializable]
public class ModelPartEntry
{
    public string PartId;
    public string DisplayName;
    public Renderer TargetRenderer;
    [Tooltip("-1 = все материалы рендерера")]
    public int MaterialIndex = -1;
}

public class ModelPartConfig : MonoBehaviour
{
    [SerializeField] private ModelPartEntry[] _parts = new ModelPartEntry[0];

    public ModelPartEntry[] Parts => _parts;
    public int PartCount => _parts?.Length ?? 0;

    public ModelPartEntry GetPart(int index)
    {
        if (_parts == null || index < 0 || index >= _parts.Length)
            return null;
        return _parts[index];
    }

    public ModelPartEntry GetPartById(string partId)
    {
        if (_parts == null) return null;
        foreach (var p in _parts)
            if (p != null && p.PartId == partId) return p;
        return null;
    }

    public void SetupDefaultPart(Renderer renderer)
    {
        _parts = new ModelPartEntry[]
        {
            new ModelPartEntry { PartId = "Default", DisplayName = "Model", TargetRenderer = renderer, MaterialIndex = -1 }
        };
    }
}
