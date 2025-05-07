using UnityEngine;

[System.Serializable]
public class Biome
{
    public string name;
    public float noiseScale = 0.1f;   
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float heightMult = 20f;
    public float heightOffset = 0f;
    public Color tint;
}
