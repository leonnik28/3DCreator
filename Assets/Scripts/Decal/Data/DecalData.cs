using UnityEngine;
using System;

[Serializable]
public class DecalData
{
    public string Id { get; private set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Scale { get; set; }
    public Texture2D Texture { get; set; }
    public float Size { get; set; }

    public DecalData(Texture2D texture, Vector3 position, Quaternion rotation, float size)
    {
        Id = Guid.NewGuid().ToString();
        Texture = texture;
        Position = position;
        Rotation = rotation;
        Size = size;
        Scale = Vector3.one * size;
    }
}