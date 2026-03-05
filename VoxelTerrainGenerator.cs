using UnityEngine;
using System.Collections.Generic;

public class VoxelTerrainGenerator : MonoBehaviour
{
    [Header("World Settings")]
    public int chunkSize = 16;
    public int renderDistance = 3;

    [Header("Height & Noise")]
    public int maxHeight = 20;
    public float terrainScale = 20f;
    public float biomeScale = 50f;
    public float caveScale = 0.1f;
    public float caveThreshold = 0.6f;
    public float heightMultiplier = 10f;
    public int seed = 0;

    [Header("Block Prefabs")]
    public GameObject grassBlock, dirtBlock, stoneBlock, bedrockBlock, sandBlock, waterBlock;

    [Header("Ore Prefabs")]
    public GameObject coalOre, ironOre, copperOre, goldOre, diamondOre;

    [Header("Tree")]
    public GameObject treePrefab;
    [Range(0, 1)] public float treeSpawnChance = 0.05f;

    [Header("Village Prefabs")]
    public GameObject housePrefab;
    public GameObject smithPrefab;
    public GameObject wellPrefab;
    [Range(0, 1)] public float villageSpawnChance = 0.005f;

    [Header("Ore Chances")]
    [Range(0, 1)] public float coalChance = 0.08f;
    [Range(0, 1)] public float ironChance = 0.06f;
    [Range(0, 1)] public float copperChance = 0.05f;
    [Range(0, 1)] public float goldChance = 0.03f;
    [Range(0, 1)] public float diamondChance = 0.01f;

    [Header("Water Level")]
    public int waterLevel = 5;

    private Transform player;
    private Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        if (seed == 0) seed = Random.Range(1, 99999);
        player = Camera.main.transform;
        InvokeRepeating(nameof(UpdateChunks), 0, 1f);
    }

    void UpdateChunks()
    {
        Vector2Int currentChunk = new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );

        HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                Vector2Int chunkCoord = new Vector2Int(currentChunk.x + x, currentChunk.y + z);
                activeChunks.Add(chunkCoord);

                if (!chunks.ContainsKey(chunkCoord))
                {
                    GameObject chunk = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
                    chunk.transform.parent = transform;
                    chunk.transform.position = new Vector3(chunkCoord.x * chunkSize, 0, chunkCoord.y * chunkSize);
                    GenerateChunk(chunk, chunkCoord);
                    chunks[chunkCoord] = chunk;
                }
            }
        }

        // Unload distant chunks
        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var pair in chunks)
        {
            if (!activeChunks.Contains(pair.Key))
            {
                Destroy(pair.Value);
                toRemove.Add(pair.Key);
            }
        }
        foreach (var key in toRemove) chunks.Remove(key);
    }

    void GenerateChunk(GameObject parent, Vector2Int coord)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int worldX = coord.x * chunkSize + x;
                int worldZ = coord.y * chunkSize + z;

                float biomeNoise = Mathf.PerlinNoise(worldX / biomeScale + seed, worldZ / biomeScale + seed);
                bool isDesert = biomeNoise > 0.6f;

                float terrainNoise = Mathf.PerlinNoise(worldX / terrainScale + seed, worldZ / terrainScale + seed);
                int surfaceHeight = Mathf.FloorToInt(terrainNoise * heightMultiplier);

                for (int y = 0; y <= surfaceHeight; y++)
                {
                    Vector3 pos = new Vector3(worldX, y, worldZ);

                    // Caves
                    if (y > 0 && y < surfaceHeight - 1)
                    {
                        float caveNoise = Mathf.PerlinNoise((worldX + seed) * caveScale, (y + seed) * caveScale) +
                                          Mathf.PerlinNoise((worldZ + seed) * caveScale, (y + seed) * caveScale);
                        if (caveNoise > caveThreshold) continue;
                    }

                    GameObject block = ChooseBlock(y, surfaceHeight, isDesert);

                    if (y == 0) block = bedrockBlock;
                    if (y < surfaceHeight - 2 && block == stoneBlock)
                        block = ChooseOre(y);

                    if (block != null)
                        Instantiate(block, pos, Quaternion.identity, parent.transform);
                }

                // Water
                for (int y = surfaceHeight + 1; y <= waterLevel; y++)
                {
                    Instantiate(waterBlock, new Vector3(worldX, y, worldZ), Quaternion.identity, parent.transform);
                }

                // Tree
                if (!isDesert && treePrefab && Random.value < treeSpawnChance && surfaceHeight >= waterLevel)
                {
                    Instantiate(treePrefab, new Vector3(worldX, surfaceHeight + 1, worldZ), Quaternion.identity, parent.transform);
                }

                // Village
                if (!isDesert && Random.value < villageSpawnChance && surfaceHeight >= waterLevel + 1)
                {
                    SpawnVillage(new Vector3(worldX, surfaceHeight + 1, worldZ), parent.transform);
                }
            }
        }
    }

    GameObject ChooseBlock(int y, int surfaceHeight, bool isDesert)
    {
        if (y == surfaceHeight)
            return isDesert ? sandBlock : grassBlock;
        else if (y > surfaceHeight - 3)
            return dirtBlock;
        else
            return stoneBlock;
    }

    GameObject ChooseOre(int y)
    {
        float r = Random.value;
        if (r < diamondChance && y <= 5)
            return diamondOre;
        if (r < goldChance && y <= 10)
            return goldOre;
        if (r < ironChance && y <= 15)
            return ironOre;
        if (r < copperChance && y <= 18)
            return copperOre;
        if (r < coalChance)
            return coalOre;
        return stoneBlock;
    }

    void SpawnVillage(Vector3 origin, Transform parent)
    {
        Vector3 pos = origin;
        Instantiate(housePrefab, pos, Quaternion.identity, parent);
        Instantiate(smithPrefab, pos + new Vector3(3, 0, 0), Quaternion.identity, parent);
        Instantiate(wellPrefab, pos + new Vector3(6, 0, 0), Quaternion.identity, parent);
    }
}
