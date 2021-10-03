using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;



public class GeneratePlanet : MonoBehaviour
{
    //TODO for now make square but ideally y is higher
    // public Vector2Int mapSize;
    // public Vector2Int buildingsMapSize;

    public bool debugVerteces = false;
    
    public float mapSize, buildingSize;

    public float buildingThreshold, buildingChance, baseQuadSize, buildingHeight;
    public float noiseScale, worleyScale, distanceScale;

    public int seed = 42;
    
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    private MeshData _meshData;

    // Start is called before the first frame update
    void Start()
    {
        GenerateTerrain(seed);
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void DrawMesh()
    {
        MeshData mesh = GenerateTerrain(seed);
        _meshData = mesh;
        meshFilter.sharedMesh = null;
        meshFilter.sharedMesh = mesh.CreateMesh();
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        // meshRenderer.sharedMaterial
    }

    MeshData GenerateTerrain(int seed)
    {
        int width = Mathf.FloorToInt((float) mapSize / baseQuadSize);
        //TODO Should be turned to vector
        int quadsPerBuilding = Mathf.FloorToInt(buildingSize / baseQuadSize);
        int buildingsWidth = Mathf.FloorToInt( mapSize / buildingSize);

        Random.InitState(seed);        
        

        float[,] worleyMap = GenerateRandomMap(buildingsWidth, buildingsWidth);
        Vector2[,] worleyPos = GenerateRandomVectors(buildingsWidth, buildingsWidth);
        float[,] heightMap = GenerateNoiseMap(width + 1, width + 1, 116, 25, 4, 0.74f, 2.57f, Vector2.zero);


        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (width - 1) / 2f;

        MeshData meshData = new MeshData(width, width);
        int vertexIndex = 0;
        //
        for (int y = 0; y <= width; y++)
        {
            for (int x = 0; x <= width; x++)
            {
                Vector2Int localWorleyPoint = new Vector2Int(x, y) / quadsPerBuilding;
                Vector2 currPos = new Vector2(x * baseQuadSize, y * baseQuadSize);
                float minDist = float.MaxValue;
                Vector2Int closestWorleyPoint = Vector2Int.zero;
                bool isNearBuilding = false;
                //Surrounding worley points
                for (int k = -1; k < 1; k++)
                for (int l = -1; l < 1; l++)
                {
                    //Do some check if at bounds of map
                    Vector2Int currentWorleyIndex = localWorleyPoint + new Vector2Int(k, l);
                    if (currentWorleyIndex.x > buildingsWidth - 1) currentWorleyIndex.x = buildingsWidth - 1;
                    if (currentWorleyIndex.x < 0) currentWorleyIndex.x = 0;

                    if (currentWorleyIndex.y > buildingsWidth - 1) currentWorleyIndex.y = buildingsWidth - 1;
                    if (currentWorleyIndex.y < 0) currentWorleyIndex.y = 0;

                    // if (worleyMap[currentWorleyIndex.x, currentWorleyIndex.y] < buildingChance)
                    {
                        // isNearBuilding = true;
                        
                        Vector2 currWorleyPos = worleyPos[currentWorleyIndex.x, currentWorleyIndex.y] + (Vector2) currentWorleyIndex * buildingSize;
                        float dist = Vector2.Distance(currWorleyPos, currPos);
                        if (dist < minDist)
                        {

                            minDist = dist;
                            // print(minDist);
                            closestWorleyPoint = currentWorleyIndex;
                        }
                    }
                }
                
                //
                // if (worleyMap[closestWorleyPoint.x, closestWorleyPoint.y] < buildingChance)
                //     isNearBuilding = true;
                
                float currHeight = heightMap[x, y] * noiseScale;
                if (worleyMap[closestWorleyPoint.x, closestWorleyPoint.y] < buildingChance && minDist > buildingThreshold)
                    currHeight += buildingHeight * worleyMap[closestWorleyPoint.x, closestWorleyPoint.y] * worleyScale + minDist * distanceScale;


                //Add vertex
                // meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, currHeight, topLeftZ - y);
                meshData.vertices[vertexIndex] = new Vector3(x, currHeight, y);
                //Add uv
                meshData.uvs[vertexIndex] = new Vector2(x / (float) width, y / (float) width);

                if (x < width - 1 && y < width - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + width + 1, vertexIndex + 1);
                    meshData.AddTriangle(vertexIndex + 1, vertexIndex + width + 1, vertexIndex + width + 2);
                }

                vertexIndex++;
            }
        }
        //
        // for (int y = 0; y <= width; y++)
        // {
        //     for (int x = 0; x <= width; x++)
        //     {
        //         //using middling inverts the mesh
        //         meshData.vertices[vertexIndex] =new Vector3 (topLeftX + x, heightMap [x, y], topLeftZ - y);
        //         meshData.uvs[vertexIndex] = new Vector2(x / (float) width, y / (float) width);
        //
        //         if (x < width - 1 && y < width - 1)
        //         {
        //             meshData.AddTriangle(vertexIndex, vertexIndex + width + 1, vertexIndex + 1);
        //             meshData.AddTriangle(vertexIndex + 1, vertexIndex + width + 1, vertexIndex + width + 2);
        //         }
        //
        //         vertexIndex++;
        //     }
        // }

        //plug height value into cellular noise multiplication,

        //Layer more perlin noise map for authenticity

        return meshData;
    }

    Vector2[,] GenerateRandomVectors(int width, int height)
    {
        Vector2[,] randVecs = new Vector2[width, height];
        for (int i = 0; i < width; i++)
        for (int j = 0; j < height; j++)
        {
            randVecs[i, j] = new Vector2(Random.value, Random.value);
        }

        return randVecs;
    }


    float[,] GenerateRandomMap(int width, int height)
    {
        
        float[,] randMap = new float[width, height];
        for (int i = 0; i < width; i++)
        for (int j = 0; j < height; j++)
        {
            randMap[i, j] = Random.value;
        }

        return randMap;
    }

    float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance,
        float lacunarity, Vector2 offset)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        System.Random prng = new System.Random(seed);

        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;


        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }


        return noiseMap;
    }

    private void OnDrawGizmos()
    {
        if (!debugVerteces || _meshData == null) return;
        for (int i = 0; i < _meshData.vertices.Length; i++)
        {
            Gizmos.DrawSphere(_meshData.vertices[i], .1f);
        }
    }
}


public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[(meshWidth + 1) * (meshWidth + 1)];
        uvs = new Vector2[(meshWidth + 1) * (meshHeight + 1)];
        triangles = new int[(meshWidth) * (meshHeight) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}