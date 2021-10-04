using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using System.Linq;

//TODO use these to generate perlin noise and shit in compute shader:
//https://github.com/keijiro/NoiseShader
//https://www.youtube.com/watch?v=BrZ4pWwkpto

public class GeneratePlanet : MonoBehaviour
{
    //TODO for now make square but ideally y is higher
    // public Vector2Int mapSize;
    // public Vector2Int buildingsMapSize;

    public bool debugVertices = false;
    public bool debugGen = false;

    [Header("TerrainGen")] public float mapSize, buildingSize;
    public float chunkSize = 10f;
    public float buildingThreshold, baseQuadSize, buildingHeight;
    [Range(0, 1)] public float buildingChance = 0.2f;
    public float noiseScale, worleyScale, distanceScale;

    public int seed = 42;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;


    private MeshData _meshData;
    private worleyPoint[,] worleyMap;
    private float[,] heightMap;
    private int quadsPerChunk;
    private int quadsPerBuilding;
    private int buildingsWidth;


    [Header("SkyGen")] public Transform blockPf;
    public float blockSize = 1f;
    [Range(0, 1)] public float blockChance, heightWeight = 0.1f;
    public float perlinNoiseScale = 0.05f;
    public MeshFilter skyMeshFilter;

    public Material skyMeshMat;

    public ComputeShader computeShader;
    public RenderTexture renderTexture;

    private List<Mesh> meshes = new List<Mesh>();


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
        // meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        // meshRenderer.sharedMaterial
    }

    public void RunShader()
    {
        renderTexture = new RenderTexture(256, 256, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);
    }


    public static void DestroySafe(Object any)
    {
        if (Application.isEditor)
            DestroyImmediate(any);
        else
            Destroy(any);
    }


    public void GenerateTerrainLarge()
    {
        int totalChunks = Mathf.FloorToInt((float) mapSize / chunkSize);
        quadsPerChunk = Mathf.FloorToInt((float) chunkSize / baseQuadSize);

        quadsPerBuilding = Mathf.FloorToInt(buildingSize / baseQuadSize);
        buildingsWidth = Mathf.FloorToInt(mapSize / buildingSize);

        Random.InitState(seed);

        worleyMap = new worleyPoint[buildingsWidth, buildingsWidth];
        heightMap = GenerateNoiseMap((quadsPerChunk + 1) * totalChunks, (quadsPerChunk + 1) * totalChunks, 116, 25, 4,
            0.74f, 2.57f, Vector2.zero);

        List<MeshData> meshChunks = new List<MeshData>();
        Transform container = new GameObject("GroundGen").transform;
        for (int i = 0; i < totalChunks; i++)
        {
            for (int j = 0; j < totalChunks; j++)
            {
                MeshData chunk = ComputeChunk(i * quadsPerChunk, j * quadsPerChunk);

                GameObject g = new GameObject("Meshy"); //create gameobject for the mesh
                g.transform.parent = container; //set parent to the container we just made
                MeshFilter mf = g.AddComponent<MeshFilter>(); //add mesh component
                MeshRenderer mr = g.AddComponent<MeshRenderer>(); //add mesh renderer component
                mr.material = skyMeshMat; //set material to avoid evil pinkness of missing texture
                Mesh mesh = chunk.CreateMesh();
                mf.sharedMesh = mesh;
                // mf.mesh.CombineMeshes(data.ToArray()); //set mesh to the combination of all of the blocks in the list
                meshes.Add(mesh); //keep track of mesh so we can destroy it when it's no longer needed
                g.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                g.transform.position = new Vector3(j * (chunkSize - baseQuadSize), 0, i * (chunkSize - baseQuadSize));
            }
        }
    }

    MeshData ComputeChunk(int xOff, int yOff)
    {
        print($"generating chunk: {xOff}, {yOff}");
        MeshData meshData = new MeshData(quadsPerChunk, quadsPerChunk);
        int vertexIndex = 0;

        for (int i = 0; i <= quadsPerChunk; i++)
        {
            int y = i + yOff;
            for (int j = 0; j <= quadsPerChunk; j++)
            {
                int x = j + xOff;
                // Vector2Int localWorleyPoint = new Vector2Int(x, y) / quadsPerBuilding;
                // Vector2 currPos = new Vector2(x * baseQuadSize, y * baseQuadSize);
                // float minDist = float.MaxValue;
                // Vector2Int closestWorleyPoint = Vector2Int.zero;
                // bool isNearBuilding = false;
                // //Surrounding worley points
                // for (int k = -1; k < 1; k++)
                // for (int l = -1; l < 1; l++)
                // {
                //     //Do some check if at bounds of map
                //     Vector2Int currentWorleyIndex = localWorleyPoint + new Vector2Int(k, l);
                //     if (currentWorleyIndex.x > buildingsWidth - 1) currentWorleyIndex.x = buildingsWidth - 1;
                //     if (currentWorleyIndex.x < 0) currentWorleyIndex.x = 0;
                //
                //     if (currentWorleyIndex.y > buildingsWidth - 1) currentWorleyIndex.y = buildingsWidth - 1;
                //     if (currentWorleyIndex.y < 0) currentWorleyIndex.y = 0;
                //
                //
                //     Vector2 currWorleyPos =
                //         worleyMap[currentWorleyIndex.x, currentWorleyIndex.y].localPos * buildingSize +
                //         (Vector2) currentWorleyIndex * buildingSize;
                //     // Debug.DrawLine(currWorleyPos, Vector3.zero, Color.black, 999999);
                //     float dist = Vector2.Distance(currWorleyPos, currPos);
                //     if (dist < minDist)
                //     {
                //         minDist = dist;
                //         closestWorleyPoint = currentWorleyIndex;
                //     }
                // }

                float currHeight = heightMap[x, y] * noiseScale;
                // if (worleyMap[closestWorleyPoint.x, closestWorleyPoint.y].value < buildingChance &&
                //     minDist > buildingThreshold)
                //     currHeight += buildingHeight * worleyMap[closestWorleyPoint.x, closestWorleyPoint.y].value *
                //                   worleyScale +
                //                   minDist * distanceScale;

                meshData.vertices[vertexIndex] =
                    new Vector3(j * baseQuadSize, currHeight, i * baseQuadSize);
                // meshData.uvs[vertexIndex] = new Vector2(x / (float) quadsPerChunk, y / (float) quadsPerChunk);
                if (j < quadsPerChunk - 1 && i < quadsPerChunk - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + quadsPerChunk + 1, vertexIndex + 1);
                    meshData.AddTriangle(vertexIndex + 1, vertexIndex + quadsPerChunk + 1,
                        vertexIndex + quadsPerChunk + 2);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }


    public void GenerateHighGround()
    {
        DestroySafe(GameObject.Find("Meshys"));
        foreach (Mesh m in
            meshes) //meshes still exist even though they aren't in the scene anymore. destroy them so they don't take up memory.
            DestroySafe(m);

        Random.InitState(seed);
        float randStart = Random.Range(0f, 10000f);

        float startTime = Time.realtimeSinceStartup;

        int blockCount = Mathf.FloorToInt(mapSize / blockSize);
        //create a unit cube and store the mesh from it
        MeshFilter blockMesh = Instantiate(blockPf, Vector3.zero, Quaternion.identity).GetComponent<MeshFilter>();
        blockMesh.transform.localScale = Vector3.one * blockSize;
        List<CombineInstance> blockData = new List<CombineInstance>();

        //For loops to gen
        for (int x = 0; x < blockCount; x++)
        for (int y = 0; y < blockCount; y++)
        for (int z = 0; z < blockCount; z++)
        {
            float noiseVal = Perlin3D(x * perlinNoiseScale + randStart, y * perlinNoiseScale + randStart,
                z * perlinNoiseScale + randStart);

            if (noiseVal <= blockChance)
            {
                blockMesh.transform.position = new Vector3(x, y, z);

                CombineInstance ci = new CombineInstance
                {
                    //copy the data off of the unit cube
                    mesh = blockMesh.sharedMesh,
                    transform = blockMesh.transform.localToWorldMatrix,
                };
                blockData.Add(ci); //add the data to the list
            }
        }

        DestroySafe(blockMesh.gameObject);

        List<List<CombineInstance>>
            blockDataLists =
                new List<List<CombineInstance>>(); //we will store the meshes in a list of lists. each sub-list will contain the data for one mesh. same data as blockData, different format.
        int vertexCount = 0;
        blockDataLists.Add(new List<CombineInstance>()); //initial list of mesh data
        for (int i = 0; i < blockData.Count; i++)
        {
            //go through each element in the previous list and add it to the new list.
            vertexCount += blockData[i].mesh.vertexCount; //keep track of total vertices
            if (vertexCount > 65536)
            {
                //if the list has reached it's capacity. if total vertex count is more then 65536, reset counter and start adding them to a new list.
                vertexCount = 0;
                blockDataLists.Add(new List<CombineInstance>());
                i--;
            }
            else
            {
                //if the list hasn't yet reached it's capacity. safe to add another block data to this list 
                blockDataLists.Last().Add(blockData[i]); //the newest list will always be the last one added
            }
        }


        Transform container = new GameObject("Meshys").transform; //create container object
        foreach (List<CombineInstance> data in blockDataLists)
        {
            //for each list (of block data) in the list (of other lists)
            GameObject g = new GameObject("Meshy"); //create gameobject for the mesh
            g.transform.parent = container; //set parent to the container we just made
            MeshFilter mf = g.AddComponent<MeshFilter>(); //add mesh component
            MeshRenderer mr = g.AddComponent<MeshRenderer>(); //add mesh renderer component
            mr.material = skyMeshMat; //set material to avoid evil pinkness of missing texture
            mf.mesh.CombineMeshes(data.ToArray()); //set mesh to the combination of all of the blocks in the list
            meshes.Add(mf.mesh); //keep track of mesh so we can destroy it when it's no longer needed
            //g.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;//setting colliders takes more time. disabled for testing.
        }


        Debug.Log("Loaded in " + (Time.realtimeSinceStartup - startTime) + " Seconds.");
    }


    MeshData GenerateTerrain(int seed)
    {
        int width = Mathf.FloorToInt((float) mapSize / baseQuadSize);
        //TODO Should be turned to vector
        int quadsPerBuilding = Mathf.FloorToInt(buildingSize / baseQuadSize);
        int buildingsWidth = Mathf.FloorToInt(mapSize / buildingSize);

        Random.InitState(seed);

        float[,] worleyMap = GenerateRandomMap(buildingsWidth, buildingsWidth);
        var worleyPos = GenerateRandomVectors(buildingsWidth, buildingsWidth);
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

                        Vector2 currWorleyPos = worleyPos[currentWorleyIndex.x, currentWorleyIndex.y] * buildingSize +
                                                (Vector2) currentWorleyIndex * buildingSize;
                        // Debug.DrawLine(currWorleyPos, Vector3.zero, Color.black, 999999);
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
                if (worleyMap[closestWorleyPoint.x, closestWorleyPoint.y] < buildingChance &&
                    minDist > buildingThreshold)
                    currHeight += buildingHeight * worleyMap[closestWorleyPoint.x, closestWorleyPoint.y] * worleyScale +
                                  minDist * distanceScale;


                //Add vertex
                // meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, currHeight, topLeftZ - y);
                meshData.vertices[vertexIndex] = new Vector3(x * baseQuadSize, currHeight, y * baseQuadSize);
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


    static Vector2[,] GenerateRandomVectors(int width, int height)
    {
        Vector2[,] randVecs = new Vector2[width, height];
        for (int i = 0; i < width; i++)
        for (int j = 0; j < height; j++)
        {
            randVecs[i, j] = new Vector2(Random.value, Random.value);
        }

        return randVecs;
    }


    static float[,] GenerateRandomMap(int width, int height)
    {
        float[,] randMap = new float[width, height];
        for (int i = 0; i < width; i++)
        for (int j = 0; j < height; j++)
        {
            randMap[i, j] = Random.value;
        }

        return randMap;
    }

    static worleyPoint[,] GenerateRandomPoint(int width, int height)
    {
        worleyPoint[,] map = new worleyPoint[width, height];
        for (int i = 0; i < width; i++)
        for (int j = 0; j < height; j++)
        {
            map[i, j].localPos = new Vector2(Random.value, Random.value);
            map[i, j].value = Random.value;
        }

        return map;
    }

    static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance,
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

    public static float Perlin3D(float x, float y, float z)
    {
        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ac = Mathf.PerlinNoise(x, z);

        float ba = Mathf.PerlinNoise(y, x);
        float cb = Mathf.PerlinNoise(z, y);
        float ca = Mathf.PerlinNoise(z, x);

        float abc = ab + bc + ac + ba + cb + ca;
        return abc / 6f;
    }

    private void OnDrawGizmos()
    {
        if (!debugVertices || _meshData == null) return;
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

public struct worleyPoint
{
    public Vector2 localPos;
    public float value;
}