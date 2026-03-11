using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DecalRepository
{
    private List<DecalController> _activeDecals = new List<DecalController>();

    public IReadOnlyList<DecalController> ActiveDecals => _activeDecals;

    public void Add(DecalController decal)
    {
        if (decal != null && !_activeDecals.Contains(decal))
        {
            _activeDecals.Add(decal);
        }
    }

    public void Remove(DecalController decal)
    {
        if (decal != null)
        {
            _activeDecals.Remove(decal);
        }
    }

    public void Clear()
    {
        _activeDecals.Clear();
    }
}