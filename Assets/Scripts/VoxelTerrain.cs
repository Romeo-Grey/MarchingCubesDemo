using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelTerrain : MonoBehaviour
{
    public int chunkSize = 16; // Size of each chunk
    public float voxelSize = 0.5f; // Smaller size increases resolution
    public float[,,] voxelData; // 3D array to store voxel data
    public MeshFilter meshFilter; // Reference to the MeshFilter component
    public bool useMarchingCubes = true; // Enable marching cubes visualization
    public float isoLevel = 0.5f; // Iso level for marching cubes
    public bool useMarchDelay; // Whether to use marching delay
    public float marchSpeedInSeconds = 0.5f; // Speed of marching
    public bool drawNoiseGizmos = true; // Toggle to visualize the noise
    public Color groundColor = Color.green; // Color for ground gizmos
    public Color airColor = Color.blue; // Color for air gizmos
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

        // Simple flat terrain generation with multiple noise octaves
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float noiseValue = 0;
                    float amplitude = 1f;
                    float frequency = 0.1f;

                    // Generate noise with multiple octaves
                    for (int octave = 0; octave < 4; octave++)
                    {
                        noiseValue += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
                        amplitude *= 0.5f;  // Decrease amplitude for finer details
                        frequency *= 2f;    // Increase frequency for finer details
                    }

                    // Calculate height based on the noise value
                    float height = noiseValue * 10f;

                    // Fill voxel data based on height
                    voxelData[x, y, z] = y < height ? 1 : 0; // Solid ground below the noise height, air above
                }
            }
        }
    }

    // Gizmo visualization of the noise
    void OnDrawGizmos()
    {
        if (!drawNoiseGizmos || voxelData == null) return;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Vector3 worldPos = new Vector3(x, y, z) * voxelSize;
                    if (voxelData[x, y, z] > 0) // Ground voxel
                    {
                        Gizmos.color = groundColor;
                    }
                    else // Air voxel
                    {
                        Gizmos.color = airColor;
                    }

                    Gizmos.DrawSphere(worldPos, voxelSize * 0.25f); // Draw spheres for visualization
                }
            }
        }
    }

    // Marching cubes algorithm
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
                    // Set values at the corners of the cube
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

                    // Find the triangulation index
                    int cubeIndex = 0;
                    if (cubeValues[0] < isoLevel) cubeIndex |= 1;
                    if (cubeValues[1] < isoLevel) cubeIndex |= 2;
                    if (cubeValues[2] < isoLevel) cubeIndex |= 4;
                    if (cubeValues[3] < isoLevel) cubeIndex |= 8;
                    if (cubeValues[4] < isoLevel) cubeIndex |= 16;
                    if (cubeValues[5] < isoLevel) cubeIndex |= 32;
                    if (cubeValues[6] < isoLevel) cubeIndex |= 64;
                    if (cubeValues[7] < isoLevel) cubeIndex |= 128;

                    // Get the intersecting edges
                    int[] edges = MarchingCubesTables.triTable[cubeIndex];

                    Vector3 worldPos = new Vector3(x, y, z) * voxelSize;

                    // Triangulate
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

        // Update mesh with vertices and triangles
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        if (useMarchDelay)
        {
            yield return new WaitForSeconds(marchSpeedInSeconds);
        }
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
