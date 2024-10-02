using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelTerrain : MonoBehaviour
{
    public int chunkSize = 32; // Increased chunk size for more detail
    public float[,,] voxelData; // 3D array to store voxel data
    public MeshFilter meshFilter; // Reference to the MeshFilter component
    public bool useMarchingCubes = true; // Enable marching cubes visualization
    public float isoLevel = 0.5f; // Iso level for marching cubes

    // Perlin noise parameters
    public float noiseScale = 5f; // Decrease scale for finer detail
    public int octaves = 4;        // Number of noise layers
    public float persistence = 0.5f; // Controls the amplitude of each octave
    public float lacunarity = 2f;  // Controls the frequency of each octave
    public Vector2 offset;         // Offset for random terrain variation

    private Mesh mesh; // Mesh for the marching cubes

    void Start()
    {
        GenerateVoxels(); // Generate voxel data
        if (useMarchingCubes)
        {
            mesh = new Mesh(); // Create a new mesh
            StartCoroutine("March"); // Start the marching cubes coroutine
        }
    }

    void GenerateVoxels()
    {
        // Initialize the voxel data array
        voxelData = new float[chunkSize, chunkSize, chunkSize];

        // Generate voxel data with configurable Perlin noise
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                // Generate height based on configurable Perlin noise
                float height = GeneratePerlinNoise(x, z) * 10f; // Adjust amplitude for height
                for (int y = 0; y < chunkSize; y++)
                {
                    // Use a smoother transition based on height
                    voxelData[x, y, z] = (y < height) ? 1f : 0f;
                }
            }
        }

        // Optional: Apply a smoothing algorithm here to smooth the terrain
        SmoothTerrain();
    }

    float GeneratePerlinNoise(float x, float z)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f; // Used to normalize result to 0-1

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x + offset.x) / noiseScale * frequency;
            float sampleZ = (z + offset.y) / noiseScale * frequency;

            float noiseValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1; // Map from [0,1] to [-1,1]
            total += noiseValue * amplitude;

            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return (total / maxValue + 1) / 2; // Normalize to [0,1]
    }

    // Smooth the terrain by averaging values of adjacent voxels
    void SmoothTerrain()
    {
        float[,,] smoothedData = new float[chunkSize, chunkSize, chunkSize];

        for (int x = 1; x < chunkSize - 1; x++)
        {
            for (int y = 1; y < chunkSize - 1; y++)
            {
                for (int z = 1; z < chunkSize - 1; z++)
                {
                    // Average the values of the surrounding voxels
                    float average = (
                        voxelData[x - 1, y, z] +
                        voxelData[x + 1, y, z] +
                        voxelData[x, y - 1, z] +
                        voxelData[x, y + 1, z] +
                        voxelData[x, y, z - 1] +
                        voxelData[x, y, z + 1]) / 6f;

                    smoothedData[x, y, z] = average;
                }
            }
        }

        voxelData = smoothedData; // Replace original voxel data with smoothed data
    }

    IEnumerator March()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int x = 0; x < chunkSize - 1; x++)
        {
            for (int y = 0; y < chunkSize - 1; y++)
            {
                for (int z = 0; z < chunkSize - 1; z++)
                {
                    float[] cubeValues = new float[]
                    {
                        voxelData[x, y, z + 1],
                        voxelData[x + 1, y, z + 1],
                        voxelData[x + 1, y, z],
                        voxelData[x, y, z],
                        voxelData[x, y + 1, z + 1],
                        voxelData[x + 1, y + 1, z + 1],
                        voxelData[x + 1, y + 1, z],
                        voxelData[x, y + 1, z]
                    };

                    int cubeIndex = 0;
                    if (cubeValues[0] < isoLevel) cubeIndex |= 1;
                    if (cubeValues[1] < isoLevel) cubeIndex |= 2;
                    if (cubeValues[2] < isoLevel) cubeIndex |= 4;
                    if (cubeValues[3] < isoLevel) cubeIndex |= 8;
                    if (cubeValues[4] < isoLevel) cubeIndex |= 16;
                    if (cubeValues[5] < isoLevel) cubeIndex |= 32;
                    if (cubeValues[6] < isoLevel) cubeIndex |= 64;
                    if (cubeValues[7] < isoLevel) cubeIndex |= 128;

                    int[] edges = MarchingCubesTables.triTable[cubeIndex];

                    Vector3 worldPos = new Vector3(x, y, z);

                    for (int i = 0; edges[i] != -1; i += 3)
                    {
                        int e00 = MarchingCubesTables.edgeConnections[edges[i]][0];
                        int e01 = MarchingCubesTables.edgeConnections[edges[i]][1];
                        int e10 = MarchingCubesTables.edgeConnections[edges[i + 1]][0];
                        int e11 = MarchingCubesTables.edgeConnections[edges[i + 1]][1];
                        int e20 = MarchingCubesTables.edgeConnections[edges[i + 2]][0];
                        int e21 = MarchingCubesTables.edgeConnections[edges[i + 2]][1];

                        Vector3 a = Interp(MarchingCubesTables.cubeCorners[e00], cubeValues[e00], MarchingCubesTables.cubeCorners[e01], cubeValues[e01]) + worldPos;
                        Vector3 b = Interp(MarchingCubesTables.cubeCorners[e10], cubeValues[e10], MarchingCubesTables.cubeCorners[e11], cubeValues[e11]) + worldPos;
                        Vector3 c = Interp(MarchingCubesTables.cubeCorners[e20], cubeValues[e20], MarchingCubesTables.cubeCorners[e21], cubeValues[e21]) + worldPos;

                        AddTriangle(vertices, triangles, a, b, c);
                    }
                }
            }
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        yield break; // Ends the coroutine without returning a value
}


    void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
    {
        int triIndex = triangles.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        triangles.Add(triIndex);
        triangles.Add(triIndex + 1);
        triangles.Add(triIndex + 2);
    }

    Vector3 Interp(Vector3 edgeVertex1, float valueAtVertex1, Vector3 edgeVertex2, float valueAtVertex2)
    {
        return (edgeVertex1 + (isoLevel - valueAtVertex1) * (edgeVertex2 - edgeVertex1) / (valueAtVertex2 - valueAtVertex1));
    }
}
