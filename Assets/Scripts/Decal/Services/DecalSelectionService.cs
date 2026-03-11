using UnityEngine;
using System;

public class DecalSelectionService
{
    private DecalController _selectedDecal;

    public event Action<DecalController> OnSelectionChanged;
    public event Action<DecalController> OnDecalDeleted;

    public DecalController SelectedDecal => _selectedDecal;

    public void Select(DecalController decal)
    {
        if (_selectedDecal != null)
        {
            _selectedDecal.SetSelected(false);
        }

        _selectedDecal = decal;

        if (_selectedDecal != null)
        {
            _selectedDecal.SetSelected(true);
        }

        OnSelectionChanged?.Invoke(_selectedDecal);
    }

    public void Deselect()
    {
        if (_selectedDecal != null)
        {
            _selectedDecal.SetSelected(false);
            _selectedDecal = null;
            OnSelectionChanged?.Invoke(null);
        }
    }

    public void NotifyDeleted(DecalController decal)
    {
        if (_selectedDecal == decal)
        {
            Deselect();
        }
        OnDecalDeleted?.Invoke(decal);
    }
}