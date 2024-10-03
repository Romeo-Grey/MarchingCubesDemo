using UnityEngine;

public class PerlinTerrainGenerator : MonoBehaviour
{
    public int width = 256; // Width of the terrain
    public int height = 256; // Height of the terrain
    public float scale = 20f; // Scale of the noise

    // Additional noise settings
    public int octaves = 4; // Number of noise layers
    public float persistence = 0.5f; // Decrease in amplitude of each octave
    public float lacunarity = 2.0f; // Increase in frequency of each octave
    public float heightMultiplier = 10f; // Multiplies the height of the terrain
    public float xOffset = 0f; // Offset to move terrain in x direction
    public float zOffset = 0f; // Offset to move terrain in z direction

    // Regeneration settings
    public bool autoRegenerate = false; // Toggle for automatic regeneration
    public float regenerateInterval = 5f; // Time in seconds between regenerations

    private Terrain terrain;
    private float timer = 0f;

    private void Start()
    {
        terrain = GetComponent<Terrain>();
        GenerateTerrain();
        
        if (autoRegenerate)
        {
            timer = regenerateInterval;
        }
    }

    private void Update()
    {
        if (autoRegenerate)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                GenerateTerrain();
                timer = regenerateInterval;
            }
        }
    }

    public void GenerateTerrain()
    {
        terrain.terrainData = CreateTerrain(terrain.terrainData);
    }

    private TerrainData CreateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, heightMultiplier, height); // Set terrain size
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }

    private float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                // Calculate the Perlin noise value at the specific (x, z) point using multiple octaves
                float noiseValue = GeneratePerlinNoise(x, z);
                heights[x, z] = noiseValue;
            }
        }
        return heights;
    }

    private float GeneratePerlinNoise(float x, float z)
    {
        float noiseHeight = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue = 0f; // Normalization value

        // Apply octaves, persistence, and lacunarity for more detailed terrain
        for (int i = 0; i < octaves; i++)
        {
            // Scale coordinates before passing to Perlin noise function
            float sampleX = (x + xOffset) / scale * frequency;
            float sampleZ = (z + zOffset) / scale * frequency;

            // Use Unity's built-in Mathf.PerlinNoise function to get the noise value at (sampleX, sampleZ)
            float noiseValue = Mathf.PerlinNoise(sampleX, sampleZ);

            noiseHeight += noiseValue * amplitude;

            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        // Normalize the final noise value
        noiseHeight /= maxValue;

        return noiseHeight;
    }
}