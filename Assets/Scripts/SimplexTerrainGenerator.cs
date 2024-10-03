using UnityEngine;
using SimplexNoise;
public class SimplexTerrainGenerator : MonoBehaviour
{
   public int width = 256;
   public int height = 256;
   public float scale = 20.0f;
   public float heightMultiplier = 10.0f;


   private void Start()
   {
        GenerateTerrain();
   }

    private void GenerateTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = CreateTerrain(terrain.terrainData);
    }

    private TerrainData CreateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, heightMultiplier, height);
        terrainData.SetHeights(0,0, GenerateHeights());
        return terrainData;
    }

    private float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];
        
    }
}
