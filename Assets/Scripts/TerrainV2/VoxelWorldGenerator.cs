using UnityEngine;

public class VoxelWorldGenerator : MonoBehaviour
{
    [Header("World Settings")]
    public int chunkSizeXZ = 16;   // X & Z dimensions
    public int chunkHeight = 32;   // Y dimension (taller!)
    public int chunksPerAxis = 4;
    public float noiseScale = 0.1f;
    public float heightMult = 20f;  // bump this up for even taller mountains

    [Header("Optional Material (Built-in RP)")]
    public Material voxelMaterial;

    void Start()
    {
        for (int x = 0; x < chunksPerAxis; x++)
            for (int z = 0; z < chunksPerAxis; z++)
            {
                Vector3 pos = new Vector3(x * chunkSizeXZ, 0, z * chunkSizeXZ);
                var go = new GameObject($"Chunk_{x}_{z}");
                go.transform.parent = transform;
                go.transform.localPosition = pos;

                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                if (voxelMaterial != null)
                    mr.material = voxelMaterial;
                else
                {
                    mr.material = new Material(Shader.Find("Standard"));
                    mr.material.color = Color.green;
                }

                var chunk = go.AddComponent<VoxelChunk>();
                chunk.sizeXZ = chunkSizeXZ;
                chunk.sizeY = chunkHeight;
                chunk.noiseScale = noiseScale;
                chunk.heightMult = heightMult;
                chunk.Initialize();    // new setup step
                chunk.GenerateMesh();
            }
    }
}
