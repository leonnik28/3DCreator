using UnityEngine;

public interface IDecalEditor
{
    void SetActiveDecal(DecalController decal);
    DecalController GetActiveDecal();
    void OnTransformChanged();
    RectTransform GetPreviewRect();
}