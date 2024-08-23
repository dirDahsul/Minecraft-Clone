using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;

// Stuct, containing the positions of the blocks and chunks
public struct Position
{
    public readonly int X, Z;

    public Position(int x=0, int z=0)
    {
        this.X = x;
        this.Z = z;
    }
}

public partial class TerrainChunk : MonoBehaviour
{
    // Chunk generation is used for optimizing the performance, as cubes use too much resources FOR an infinite generating world
    // Let's instead of using a bunch of cubes, use chunks of mesh (cube sides), which are rendered only when need to be seen and has the unseen parts
    // concealed until world gets edited and those parts need to be rendered
    public const int iChunkWidth = 16; // Chunk width
    public const int iChunkLength = 16; // Chunk length
    public const int iChunkHeight = 64; // Chunk height, for world generation ... world itself is not limited to 64 blocks in hight

    public const int iWaterHeight = 28; // Water height

    public const int iMinTreeHeight = 4; // Min tree height

    // Let's use a Dictionary to store chunk's info about a cube's type, cuz this way we can use coordinates to get a
    // the cube's type directly without iterating through an array and we can also use coordinates to check if cube exists there
    public Dictionary<Position, OrderedDictionary<int, Block>> dBlockPillars;// = new Dictionary<Position, OrderedDictionary<int, Block>>();

    //private FastNoise noise;

    public bool isRendered;// = false;
    public bool isPropsLoaded;// = false;

    public TerrainChunk()
    {
        dBlockPillars = new Dictionary<Position, OrderedDictionary<int, Block>>();
        //noise = new FastNoise(TerrainGenerator.seed);

        isRendered = false;
        isPropsLoaded = false;
    }

    // Chunk size
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void BuildTrees(TerrainGenerator tg, Position pos)
    {
        float simplex = tg.noise.GetSimplex(pos.X*12.8f, pos.Z*12.8f);

        if(simplex > 0) // has trees
        {
            simplex *= 2f;
            int treeCount = Mathf.FloorToInt(UnityEngine.Random.Range(0f, 5f) * simplex);

            for (int i = 0; i < treeCount; i++)
            {
                Position p = dBlockPillars.Keys.ElementAt(UnityEngine.Random.Range(0, dBlockPillars.Count));
                int y = dBlockPillars[p].LastKey()+1;

                BuildTree(tg, p, y);
            }
        }
    }

    private void BuildTree(TerrainGenerator tg, Position pos, int Y)
    {
        int minHeight = UnityEngine.Random.Range(0, 3) + iMinTreeHeight;
        
        if (dBlockPillars[pos].LastValue() is not Dirt)
            return;

        generateTreeTrunk(tg, pos, Y, minHeight);
        generateTreeLeaves(tg, pos, Y, minHeight);
    }

    private bool isSuitableTreeLocation(TerrainGenerator tg, Position pos, int Y, int minHeight)
    {
        //bool isSuitableLocation = true;
        
        for (int checkY = Y; checkY <= Y + 1 + minHeight; ++checkY)
        {
            // Handle increasing space towards top of tree
            int extraSpaceNeeded;
            // Handle base location
            if (checkY == Y)
            {
                extraSpaceNeeded = 0;
            }             
            // Handle top location
            else if (checkY >= Y + 1 + minHeight - 2)
            {
                extraSpaceNeeded = 2;
            }
            else
            {
                extraSpaceNeeded = 1;
            }

            //BlockPos.MutableBlockPos blockPos = new BlockPos.MutableBlockPos();

            for (int checkX = pos.X - extraSpaceNeeded; checkX <= pos.X + extraSpaceNeeded/* && isSuitableLocation*/; ++checkX)
            {
                for (int checkZ = pos.Z - extraSpaceNeeded; checkZ <= pos.Z + extraSpaceNeeded/* && isSuitableLocation*/; ++checkZ)
                {
                    Position chunkPos = new Position((checkX + (iChunkWidth >> 1) - ((iChunkWidth + 1) & 1)) / iChunkWidth, (checkZ + (iChunkLength >> 1) - ((iChunkLength + 1) & 1)) / iChunkLength);
                    Position blockPos = new Position(checkX, checkZ);
                    var BlockDictionary = tg.dTerrainChunks[chunkPos].dBlockPillars[blockPos];
                    
                    //isSuitableLocation = !BlockDictionary.ContainsKey(checkY) || BlockDictionary[checkY].enBlock == BlockType.Leaves;
                    if (BlockDictionary.ContainsKey(checkY))
                    {
                        if (BlockDictionary[checkY].enBlock != BlockType.Leaves)
                            return false; // cant spawn tree inside blocks, unless those blocks are leaves, cuz leaves are replaceable
                    }
                }
            }
        }
        
        return true;
    }

    private void generateTreeLeaves(TerrainGenerator tg, Position pos, int Y, int height)
    {
        for (int foliageY = Y - 3 + height; foliageY <= Y + height; ++foliageY)
        {
            int foliageLayer = foliageY - (Y + height);
            int foliageLayerRadius = 1 - (foliageLayer >> 1);

            for (int foliageX = pos.X - foliageLayerRadius; foliageX <= pos.X + foliageLayerRadius; ++foliageX)
            {
                int foliageRelativeX = foliageX - pos.X;

                for (int foliageZ = pos.Z - foliageLayerRadius; foliageZ <= pos.Z + foliageLayerRadius; ++foliageZ)
                {
                    int foliageRelativeZ = foliageZ - pos.Z;

                    // Fill in layer with some randomness
                    if (Math.Abs(foliageRelativeX) != foliageLayerRadius || Math.Abs(foliageRelativeZ) != foliageLayerRadius || UnityEngine.Random.Range(0,2) != 0 && foliageLayer != 0)
                    {
                        Position chunkPos = chunkPos = new Position(Mathf.FloorToInt((foliageX + (iChunkWidth >> 1) - ((iChunkWidth + 1) & 1)) / (float)iChunkWidth), Mathf.FloorToInt((foliageZ + (iChunkLength >> 1) - ((iChunkLength + 1) & 1)) / (float)iChunkLength));
                        Position blockPos = new Position(foliageX, foliageZ);
                        var BlockDictionary = tg.dTerrainChunks[chunkPos].dBlockPillars[blockPos];

                        if (BlockDictionary.ContainsKey(foliageY))
                        {
                            continue;
                            //BlockDictionary.Remove(foliageY);
                        }

                        BlockDictionary.Add(foliageY, new Leave());
                    }
                }
            }
        }
    }

    private void generateTreeTrunk(TerrainGenerator tg, Position pos, int Y, int minHeight)
    {
        for (int height = Y; height < Y+minHeight; ++height)
        {
            if (dBlockPillars[pos].ContainsKey(height))
            {
                continue;
                //dBlockPillars[pos].Remove(height);
            }

            dBlockPillars[pos].Add(height, new Log());
        }
    }

    public void BuildBlocks(TerrainGenerator tg, Position chunkPos)
    {
        //for (int x = 0; x < iChunkWidth; x++)
            //for (int z = 0; z < iChunkLength; z++)
        for (int x = -iChunkWidth/2 + ((iChunkWidth+1)%2); x < iChunkWidth/2 + ((iChunkWidth+1)%2); x++)
            for (int z = -iChunkLength/2 + ((iChunkLength+1)%2); z < iChunkLength/2 + ((iChunkLength+1)%2); z++)
            {
                Position pos = new Position(x+(iChunkWidth*chunkPos.X), z+(iChunkLength*chunkPos.Z)); // Absolute position (first chunk has a center block of x=0,z=0, cuz chunk is 16x16)
                FastNoise noise = tg.noise;

                //print(noise.GetSimplex(x, z));
                float simplex1 = noise.GetSimplex(pos.X * .8f, pos.Z * .8f) * 14 + 0.7f;
                float simplex2 = noise.GetSimplex(pos.X * .3f, pos.Z * .3f) * 9;
                float simplex3 = noise.GetSimplex(pos.X * 4f, pos.Z * 4f) * 7;

                float heightMap = simplex1 + simplex2 + simplex3;
                //add the 2d noise to the middle of the terrain chunk
                int baseLandHeight = (int)(iChunkHeight * .5f + heightMap);

                // Create the pillar
                //pos = new Position(x+(iChunkWidth*pos.X), z+(iChunkLength*pos.Z)); // Absolute position (first chunk has a center block of x=0,z=0, cuz chunk is 16x16)
                dBlockPillars.Add(pos, new OrderedDictionary<int, Block>());

                for (int y = 0; y <= baseLandHeight; y++) // Build blocks
                {
                    //3d noise for caves and overhangs and such
                    float caveNoise1 = noise.GetPerlinFractal(pos.X * 5f, (y+1) * 10f, pos.Z * 5f);
                    float caveMask = noise.GetSimplex(pos.X * .3f, pos.Z * .3f) + .3f;

                    if (caveNoise1 > Mathf.Max(caveMask, .2f)) // Don't build block here if there is supposed to be cave
                        continue;

                    //stone layer heightmap
                    float simplexStone1 = noise.GetSimplex(pos.X * 1f, pos.Z * 1f) * 10;
                    float simplexStone2 = (noise.GetSimplex(pos.X * 5f, pos.Z * 5f) + .5f) * 20 * (noise.GetSimplex(pos.X * .3f, pos.Z * .3f) + .5f);

                    float stoneHeightMap = simplexStone1 + simplexStone2;
                    float baseStoneHeight = iChunkHeight * .25f + stoneHeightMap;

                    //float cliff = noise.GetSimplex(x * 1f, z * 1f, y) * 10;
                    //float cliffMask = noise.GetSimplex(x * .4f, z * .4f) + .3f;

                    if (y <= baseStoneHeight) // Add stone block to pillar
                        //dBlockPillars[pos].Insert(y, y, new Stone());
                        dBlockPillars[pos].Add(y, new Stone());
                    else
                        //dBlockPillars[pos].Insert(y, y, new Dirt()); // or dirt block
                        dBlockPillars[pos].Add(y, new Dirt()); // or dirt block

                    /*if(blockType != BlockType.Air)
                        blockType = BlockType.Stone;*/

                    //if(blockType == BlockType.Air && noise.GetSimplex(x * 4f, y * 4f, z*4f) < 0)
                    //  blockType = BlockType.Dirt;

                    //if(Mathf.PerlinNoise(x * .1f, z * .1f) * 10 + y < TerrainChunk.chunkHeight * .5f)
                    //    return BlockType.Grass;
                }

                //Debug.Log("LastKey: " + (dBlockPillars[pos].LastKey()+1));
                //Debug.Log("iWaterHeight: " + iWaterHeight);
                //Debug.Log("baseLandHeight: " + baseLandHeight);

                if (dBlockPillars[pos].LastKey() < iWaterHeight) // Add water if the height is lower than sea level
                {
                    for (int y = dBlockPillars[pos].LastKey() + 1; y <= iWaterHeight; y++)
                    {
                        dBlockPillars[pos].Add(y, new Water());
                        //dBlockPillars[pos].Insert(y, y, new Water());
                    }
                }
                else
                {
                    // Make sure first block is accomodated, cuz the first block may be dirt block with grass
                    if (dBlockPillars[pos].LastValue().enBlock == BlockType.Dirt)
                        if (UnityEngine.Random.Range(0, 33) < 32) // or maybe its just a dirt block... huhuhehe
                            dBlockPillars[pos].LastValue().StartUpdating(true, null, new Position(), new Position(), 0);
                        //else
                            //dBlockPillars[pos].LastValue().StartUpdating(false, null, new Position(), new Position(), 0);
                }
            }
    }

    public void ReuseBlocks(TerrainGenerator tg, Position pos)
    {
        dBlockPillars = new Dictionary<Position, OrderedDictionary<int, Block>>(); // create new memory, let garbage collector free old one .... memory reused successfully, almost
        BuildBlocks(tg, pos);
    }

    public void BuildMesh(TerrainGenerator tg, Position chunkPos)
    {
        float boxWidth = 1.0f;
        float boxHeight = 1.0f;
        float boxDepth = 1.0f;

        foreach(var pillar in dBlockPillars)
            foreach(var block in pillar.Value)
            {
                // Reference empty box
                GameObject boxContainer = null;// = new GameObject("BoxContainer");

                // Render top of the box
                if(!pillar.Value.ContainsKey(block.Key+1) || pillar.Value[block.Key+1].enBlock == BlockType.Water && pillar.Value[block.Key].enBlock != BlockType.Water || pillar.Value[block.Key+1].enBlock == BlockType.Leaves)
                {
                    // Create the box
                    boxContainer = new GameObject("BoxContainer");
                    boxContainer.transform.SetParent(this.transform);

                    // Create the top side
                    if(pillar.Value[block.Key].enBlock != BlockType.Water)
                    {
                        GameObject topSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        topSide.name = "TopSide";
                        topSide.transform.SetParent(boxContainer.transform);
                        topSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                        topSide.transform.localPosition = new Vector3(0f, boxHeight / 2f, 0f);
                        topSide.transform.forward = Vector3.down;

                        topSide.GetComponent<Renderer>().material = GetComponent<MeshRenderer>().material;
                        Vector2[] vectorUV = new Vector2[]
                        {
                            new Vector2(pillar.Value[block.Key].tTop.vUVs[0,0], pillar.Value[block.Key].tTop.vUVs[0,1]),
                            new Vector2(pillar.Value[block.Key].tTop.vUVs[1,0], pillar.Value[block.Key].tTop.vUVs[1,1]),
                            new Vector2(pillar.Value[block.Key].tTop.vUVs[2,0], pillar.Value[block.Key].tTop.vUVs[2,1]),
                            new Vector2(pillar.Value[block.Key].tTop.vUVs[3,0], pillar.Value[block.Key].tTop.vUVs[3,1]),
                        };
                        topSide.GetComponent<MeshFilter>().mesh.SetUVs(0, vectorUV);
                    }
                    else
                    {
                        // Create a new GameObject
                        GameObject topSide = new GameObject("TopSide");

                        // Create a new MeshFilter and MeshRenderer
                        MeshFilter meshFilter = topSide.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = topSide.AddComponent<MeshRenderer>();
                        //MeshCollider meshCollider = topSide.AddComponent<MeshCollider>();

                        // Set parent
                        topSide.transform.SetParent(boxContainer.transform);
                        
                        // Create a new Mesh
                        Mesh mesh = new Mesh();

                        // Define the vertices of the quad
                        Vector3[] vertices = new Vector3[]
                        {
                            new Vector3(0.5f, 0.5f, -0.5f),    // Bottom-right
                            new Vector3(-0.5f, 0.5f, -0.5f),  // Bottom-left
                            new Vector3(0.5f, 0.5f, 0.5f),    // Top-right
                            new Vector3(-0.5f, 0.5f, 0.5f),   // Top-left

                            new Vector3(0.5f, 0.5f, -0.5f),   // Bottom-right (mirrored)
                            new Vector3(-0.5f, 0.5f, -0.5f),  // Bottom-left (mirrored)
                            new Vector3(0.5f, 0.5f, 0.5f),    // Top-right (mirrored)
                            new Vector3(-0.5f, 0.5f, 0.5f)    // Top-left (mirrored)
                        };

                        // Define the triangles
                        int[] triangles = new int[]
                        {
                            //0, 1, 2,     // Top triangle
                            //2, 1, 3,    // Bottom triangle
                            0, 1, 2,     // Top triangle
                            1, 3, 2,    // Bottom triangle

                            6, 5, 4,     // Top triangle (mirrored)
                            7, 5, 6      // Bottom triangle (mirrored)
                        };

                        // Assign the vertices, UV coordinates, and triangles to the mesh
                        mesh.SetVertices(vertices);
                        Vector2[] UVsToApply = new Vector2[8];
                        Vector2[] vectorUV = new Vector2[]
                        {
                            new Vector2(pillar.Value[block.Key].tTop.vUVs[0,0], pillar.Value[block.Key].tTop.vUVs[0,1]),
                            new Vector2(pillar.Value[block.Key].tTop.vUVs[1,0], pillar.Value[block.Key].tTop.vUVs[1,1]),
                            new Vector2(pillar.Value[block.Key].tTop.vUVs[2,0], pillar.Value[block.Key].tTop.vUVs[2,1]),
                            new Vector2(pillar.Value[block.Key].tTop.vUVs[3,0], pillar.Value[block.Key].tTop.vUVs[3,1]),
                        };
                        Vector2[] UVsToTake = vectorUV;
                        for (int i = 0; i < 2; i++) Array.Copy(UVsToTake, 0, UVsToApply, i*UVsToTake.Length, UVsToTake.Length);
                        mesh.SetUVs(0, UVsToApply);
                        mesh.SetTriangles(triangles, 0);
                        
                        // Recalculate the bounds and normals of the mesh
                        mesh.RecalculateBounds();
                        mesh.RecalculateNormals();

                        // Set the rendering mode to "Transparent" or "Fade"
                        Material material = GetComponent<MeshRenderer>().material;

                        /*material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                        material.SetFloat("_Mode", 2); // Set rendering mode to "Fade"
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;*/

                        // Add transparency
                        //Color color = material.color;
                        //color.a = 0.75f;  // Set the alpha value between 0 (fully transparent) and 1 (fully opaque)
                        //material.color = color;

                        // Assign the Material to the MeshRenderer component
                        meshRenderer.material = material;

                        // Assign the mesh to the MeshFilter component
                        meshFilter.mesh = mesh;

                        // Assign the mesh to the MeshCollider component
                        //meshCollider.sharedMesh = mesh;

                        // Enable double-sided global illumination
                        //meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    }
                }

                // Render bottom of the box
                if(block.Key > 0 && (!pillar.Value.ContainsKey(block.Key-1) || pillar.Value[block.Key-1].enBlock == BlockType.Water && pillar.Value[block.Key].enBlock != BlockType.Water || pillar.Value[block.Key-1].enBlock == BlockType.Leaves))
                {
                    // Create the box if it doesnt exist
                    if (boxContainer == null)
                    {
                        boxContainer = new GameObject("BoxContainer");
                        boxContainer.transform.SetParent(this.transform);
                    }

                    // Create the bottom side
                    if(pillar.Value[block.Key].enBlock != BlockType.Water)
                    {
                        // Create the bottom side
                        GameObject bottomSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        bottomSide.name = "BottomSide";
                        bottomSide.transform.SetParent(boxContainer.transform);
                        bottomSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                        bottomSide.transform.localPosition = new Vector3(0f, -boxHeight / 2f, 0f);
                        bottomSide.transform.forward = Vector3.up;
                    
                        bottomSide.GetComponent<Renderer>().material = this.GetComponent<MeshRenderer>().material;
                        Vector2[] vectorUV = new Vector2[]
                        {
                            new Vector2(pillar.Value[block.Key].tBottom.vUVs[0,0], pillar.Value[block.Key].tBottom.vUVs[0,1]),
                            new Vector2(pillar.Value[block.Key].tBottom.vUVs[1,0], pillar.Value[block.Key].tBottom.vUVs[1,1]),
                            new Vector2(pillar.Value[block.Key].tBottom.vUVs[2,0], pillar.Value[block.Key].tBottom.vUVs[2,1]),
                            new Vector2(pillar.Value[block.Key].tBottom.vUVs[3,0], pillar.Value[block.Key].tBottom.vUVs[3,1]),
                        };
                        bottomSide.GetComponent<MeshFilter>().mesh.uv = vectorUV;
                    }
                    else
                    {
                        // Create a new GameObject
                        GameObject bottomSide = new GameObject("BottomSide");

                        // Create a new MeshFilter and MeshRenderer
                        MeshFilter meshFilter = bottomSide.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = bottomSide.AddComponent<MeshRenderer>();
                        //MeshCollider meshCollider = bottomSide.AddComponent<MeshCollider>();

                        // Set parent
                        bottomSide.transform.SetParent(boxContainer.transform);
                        
                        // Create a new Mesh
                        Mesh mesh = new Mesh();

                        // Define the vertices of the quad
                        Vector3[] vertices = new Vector3[]
                        {
                            new Vector3(0.5f, -0.5f, -0.5f),    // Bottom-right
                            new Vector3(-0.5f, -0.5f, -0.5f),  // Bottom-left
                            new Vector3(0.5f, -0.5f, 0.5f),    // Top-right
                            new Vector3(-0.5f, -0.5f, 0.5f),   // Top-left

                            new Vector3(0.5f, -0.5f, -0.5f),   // Bottom-right (mirrored)
                            new Vector3(-0.5f, -0.5f, -0.5f),  // Bottom-left (mirrored)
                            new Vector3(0.5f, -0.5f, 0.5f),    // Top-right (mirrored)
                            new Vector3(-0.5f, -0.5f, 0.5f)    // Top-left (mirrored)
                        };

                        // Define the triangles
                        int[] triangles = new int[]
                        {
                            //0, 1, 2,     // Top triangle
                            //2, 1, 3,    // Bottom triangle
                            0, 1, 2,     // Top triangle
                            1, 3, 2,    // Bottom triangle

                            6, 5, 4,     // Top triangle (mirrored)
                            7, 5, 6      // Bottom triangle (mirrored)
                        };

                        // Assign the vertices, UV coordinates, and triangles to the mesh
                        mesh.SetVertices(vertices);
                        Vector2[] UVsToApply = new Vector2[8];
                        Vector2[] vectorUV = new Vector2[]
                        {
                            new Vector2(pillar.Value[block.Key].tBottom.vUVs[0,0], pillar.Value[block.Key].tBottom.vUVs[0,1]),
                            new Vector2(pillar.Value[block.Key].tBottom.vUVs[1,0], pillar.Value[block.Key].tBottom.vUVs[1,1]),
                            new Vector2(pillar.Value[block.Key].tBottom.vUVs[2,0], pillar.Value[block.Key].tBottom.vUVs[2,1]),
                            new Vector2(pillar.Value[block.Key].tBottom.vUVs[3,0], pillar.Value[block.Key].tBottom.vUVs[3,1]),
                        };
                        Vector2[] UVsToTake = vectorUV;
                        for (int i = 0; i < 2; i++) Array.Copy(UVsToTake, 0, UVsToApply, i*UVsToTake.Length, UVsToTake.Length);
                        mesh.SetUVs(0, UVsToApply);
                        mesh.SetTriangles(triangles, 0);
                        
                        // Recalculate the bounds and normals of the mesh
                        mesh.RecalculateBounds();
                        mesh.RecalculateNormals();

                        // Set the rendering mode to "Transparent" or "Fade"
                        Material material = GetComponent<MeshRenderer>().material;

                        /*material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                        material.SetFloat("_Mode", 2); // Set rendering mode to "Fade"
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;*/

                        // Add transparency
                        //Color color = material.color;
                        //color.a = 0.75f;  // Set the alpha value between 0 (fully transparent) and 1 (fully opaque)
                        //material.color = color;

                        // Assign the Material to the MeshRenderer component
                        meshRenderer.material = material;

                        // Assign the mesh to the MeshFilter component
                        meshFilter.mesh = mesh;

                        // Assign the mesh to the MeshCollider component
                        //meshCollider.sharedMesh = mesh;

                        // Enable double-sided global illumination
                        //meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    }
                }

                //front, but the front one might be in neighbour chunk...
                Position chunkPos2 = new Position(Mathf.FloorToInt((pillar.Key.X + (iChunkWidth >> 1) - ((iChunkWidth + 1) & 1)) / (float)iChunkWidth), Mathf.FloorToInt((pillar.Key.Z-1 + (iChunkLength >> 1) - ((iChunkLength + 1) & 1)) / (float)iChunkLength));
                Position blockPos2 = new Position(pillar.Key.X, pillar.Key.Z-1);

                var BlockDictionary = tg.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];
                if(!BlockDictionary.ContainsKey(block.Key) || BlockDictionary[block.Key].enBlock == BlockType.Water && pillar.Value[block.Key].enBlock != BlockType.Water || BlockDictionary[block.Key].enBlock == BlockType.Leaves)
                {
                    // Create the box if it doesnt exist
                    if (boxContainer == null)
                    {
                        boxContainer = new GameObject("BoxContainer");
                        boxContainer.transform.SetParent(this.transform);
                    }

                    // Create the front side
                    if(pillar.Value[block.Key].enBlock != BlockType.Water)
                    {
                        GameObject frontSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        frontSide.name = "FrontSide";
                        frontSide.transform.SetParent(boxContainer.transform);
                        frontSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                        frontSide.transform.localPosition = new Vector3(0f, 0f, -boxDepth / 2f);
                        frontSide.transform.forward = Vector3.forward;

                        frontSide.GetComponent<Renderer>().material = this.GetComponent<MeshRenderer>().material;
                        Vector2[] vectorUV = new Vector2[]
                        {
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[0,0], pillar.Value[block.Key].tSide.vUVs[0,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[1,0], pillar.Value[block.Key].tSide.vUVs[1,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[2,0], pillar.Value[block.Key].tSide.vUVs[2,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[3,0], pillar.Value[block.Key].tSide.vUVs[3,1]),
                        };
                        frontSide.GetComponent<MeshFilter>().mesh.uv = vectorUV;
                    }
                    else
                    {
                        // Create a new GameObject
                        GameObject frontSide = new GameObject("FrontSide");

                        // Create a new MeshFilter and MeshRenderer
                        MeshFilter meshFilter = frontSide.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = frontSide.AddComponent<MeshRenderer>();
                        //MeshCollider meshCollider = frontSide.AddComponent<MeshCollider>();

                        // Set parent
                        frontSide.transform.SetParent(boxContainer.transform);
                        
                        // Create a new Mesh
                        Mesh mesh = new Mesh();

                        // Define the vertices of the quad
                        Vector3[] vertices = new Vector3[]
                        {
                            new Vector3(0.5f, -0.5f, -0.5f),    // Bottom-right
                            new Vector3(-0.5f, -0.5f, -0.5f),  // Bottom-left
                            new Vector3(0.5f, 0.5f, -0.5f),    // Top-right
                            new Vector3(-0.5f, 0.5f, -0.5f),   // Top-left

                            new Vector3(0.5f, -0.5f, -0.5f),   // Bottom-right (mirrored)
                            new Vector3(-0.5f, -0.5f, -0.5f),  // Bottom-left (mirrored)
                            new Vector3(0.5f, 0.5f, -0.5f),    // Top-right (mirrored)
                            new Vector3(-0.5f, 0.5f, -0.5f)    // Top-left (mirrored)
                        };

                        // Define the triangles
                        int[] triangles = new int[]
                        {
                            //0, 1, 2,     // Top triangle
                            //2, 1, 3,    // Bottom triangle
                            0, 1, 2,     // Top triangle
                            1, 3, 2,    // Bottom triangle

                            6, 5, 4,     // Top triangle (mirrored)
                            7, 5, 6      // Bottom triangle (mirrored)
                        };

                        // Assign the vertices, UV coordinates, and triangles to the mesh
                        mesh.SetVertices(vertices);
                        Vector2[] UVsToApply = new Vector2[8];
                        Vector2[] vectorUV = new Vector2[]
                        {
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[0,0], pillar.Value[block.Key].tSide.vUVs[0,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[1,0], pillar.Value[block.Key].tSide.vUVs[1,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[2,0], pillar.Value[block.Key].tSide.vUVs[2,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[3,0], pillar.Value[block.Key].tSide.vUVs[3,1]),
                        };
                        Vector2[] UVsToTake = vectorUV;
                        for (int i = 0; i < 2; i++) Array.Copy(UVsToTake, 0, UVsToApply, i*UVsToTake.Length, UVsToTake.Length);
                        mesh.SetUVs(0, UVsToApply);
                        mesh.SetTriangles(triangles, 0);
                        
                        // Recalculate the bounds and normals of the mesh
                        mesh.RecalculateBounds();
                        mesh.RecalculateNormals();

                        // Set the rendering mode to "Transparent" or "Fade"
                        Material material = GetComponent<MeshRenderer>().material;

                        /*material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                        material.SetFloat("_Mode", 2); // Set rendering mode to "Fade"
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;*/

                        // Add transparency
                        //Color color = material.color;
                        //color.a = 0.75f;  // Set the alpha value between 0 (fully transparent) and 1 (fully opaque)
                        //material.color = color;

                        // Assign the Material to the MeshRenderer component
                        meshRenderer.material = material;

                        // Assign the mesh to the MeshFilter component
                        meshFilter.mesh = mesh;

                        // Assign the mesh to the MeshCollider component
                        //meshCollider.sharedMesh = mesh;

                        // Enable double-sided global illumination
                        //meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    }
                }

                //right, same as front
                chunkPos2 = new Position(Mathf.FloorToInt((pillar.Key.X+1 + (iChunkWidth >> 1) - ((iChunkWidth + 1) & 1)) / (float)iChunkWidth), Mathf.FloorToInt((pillar.Key.Z + (iChunkLength >> 1) - ((iChunkLength + 1) & 1)) / (float)iChunkLength));
                blockPos2 = new Position(pillar.Key.X+1, pillar.Key.Z);
                BlockDictionary = tg.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];
                if(!BlockDictionary.ContainsKey(block.Key) || BlockDictionary[block.Key].enBlock == BlockType.Water && pillar.Value[block.Key].enBlock != BlockType.Water || BlockDictionary[block.Key].enBlock == BlockType.Leaves)
                {
                    // Create the box if it doesnt exist
                    if (boxContainer == null)
                    {
                        boxContainer = new GameObject("BoxContainer");
                        boxContainer.transform.SetParent(this.transform);
                    }

                    // Create the right side
                    if(pillar.Value[block.Key].enBlock != BlockType.Water)
                    {
                        GameObject rightSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        rightSide.name = "RightSide";
                        rightSide.transform.SetParent(boxContainer.transform);
                        rightSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                        rightSide.transform.localPosition = new Vector3(boxWidth / 2f, 0f, 0f);
                        rightSide.transform.forward = Vector3.left;

                        rightSide.GetComponent<Renderer>().material = this.GetComponent<MeshRenderer>().material;
                        Vector2[] vectorUV = new Vector2[]
                        {
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[0,0], pillar.Value[block.Key].tSide.vUVs[0,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[1,0], pillar.Value[block.Key].tSide.vUVs[1,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[2,0], pillar.Value[block.Key].tSide.vUVs[2,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[3,0], pillar.Value[block.Key].tSide.vUVs[3,1]),
                        };
                        rightSide.GetComponent<MeshFilter>().mesh.uv = vectorUV;
                    }
                    else
                    {
                        // Create a new GameObject
                        GameObject rightSide = new GameObject("RightSide");

                        // Create a new MeshFilter and MeshRenderer
                        MeshFilter meshFilter = rightSide.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = rightSide.AddComponent<MeshRenderer>();
                        //MeshCollider meshCollider = rightSide.AddComponent<MeshCollider>();

                        // Set parent
                        rightSide.transform.SetParent(boxContainer.transform);
                        
                        // Create a new Mesh
                        Mesh mesh = new Mesh();

                        // Define the vertices of the quad
                        Vector3[] vertices = new Vector3[]
                        {
                            new Vector3(0.5f, -0.5f, 0.5f),    // Bottom-right
                            new Vector3(0.5f, -0.5f, -0.5f),  // Bottom-left
                            new Vector3(0.5f, 0.5f, 0.5f),    // Top-right
                            new Vector3(0.5f, 0.5f, -0.5f),   // Top-left

                            new Vector3(0.5f, -0.5f, 0.5f),   // Bottom-right (mirrored)
                            new Vector3(0.5f, -0.5f, -0.5f),  // Bottom-left (mirrored)
                            new Vector3(0.5f, 0.5f, 0.5f),    // Top-right (mirrored)
                            new Vector3(0.5f, 0.5f, -0.5f)    // Top-left (mirrored)
                        };

                        // Define the triangles
                        int[] triangles = new int[]
                        {
                            //0, 1, 2,     // Top triangle
                            //2, 1, 3,    // Bottom triangle
                            0, 2, 1,     // Top triangle
                            2, 3, 1,    // Bottom triangle

                            4, 6, 5,     // Top triangle (mirrored)
                            6, 7, 5      // Bottom triangle (mirrored)
                        };

                        // Assign the vertices, UV coordinates, and triangles to the mesh
                        mesh.SetVertices(vertices);
                        Vector2[] UVsToApply = new Vector2[8];
                        Vector2[] vectorUV = new Vector2[]
                        {
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[0,0], pillar.Value[block.Key].tSide.vUVs[0,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[1,0], pillar.Value[block.Key].tSide.vUVs[1,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[2,0], pillar.Value[block.Key].tSide.vUVs[2,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[3,0], pillar.Value[block.Key].tSide.vUVs[3,1]),
                        };
                        Vector2[] UVsToTake = vectorUV;
                        for (int i = 0; i < 2; i++) Array.Copy(UVsToTake, 0, UVsToApply, i*UVsToTake.Length, UVsToTake.Length);
                        mesh.SetUVs(0, UVsToApply);
                        mesh.SetTriangles(triangles, 0);
                        
                        // Recalculate the bounds and normals of the mesh
                        mesh.RecalculateBounds();
                        mesh.RecalculateNormals();

                        // Set the rendering mode to "Transparent" or "Fade"
                        Material material = GetComponent<MeshRenderer>().material;

                        /*material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                        material.SetFloat("_Mode", 2); // Set rendering mode to "Fade"
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;*/

                        // Add transparency
                        //Color color = material.color;
                        //color.a = 0.75f;  // Set the alpha value between 0 (fully transparent) and 1 (fully opaque)
                        //material.color = color;

                        // Assign the Material to the MeshRenderer component
                        meshRenderer.material = material;

                        // Assign the mesh to the MeshFilter component
                        meshFilter.mesh = mesh;

                        // Assign the mesh to the MeshCollider component
                        //meshCollider.sharedMesh = mesh;

                        // Enable double-sided global illumination
                        //meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    }
                }

                //back, same as right
                chunkPos2 = new Position(Mathf.FloorToInt((pillar.Key.X + (iChunkWidth >> 1) - ((iChunkWidth + 1) & 1)) / (float)iChunkWidth), Mathf.FloorToInt((pillar.Key.Z+1 + (iChunkLength >> 1) - ((iChunkLength + 1) & 1)) / (float)iChunkLength));
                blockPos2 = new Position(pillar.Key.X, pillar.Key.Z+1);
                BlockDictionary = tg.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];
                if(!BlockDictionary.ContainsKey(block.Key) || BlockDictionary[block.Key].enBlock == BlockType.Water && pillar.Value[block.Key].enBlock != BlockType.Water || BlockDictionary[block.Key].enBlock == BlockType.Leaves)
                {
                    // Creathe the box if it doesnt exist
                    if (boxContainer == null)
                    {
                        boxContainer = new GameObject("BoxContainer");
                        boxContainer.transform.SetParent(this.transform);
                    }

                    // Create the back side
                    if(pillar.Value[block.Key].enBlock != BlockType.Water)
                    {
                        GameObject backSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        backSide.name = "BackSide";
                        backSide.transform.SetParent(boxContainer.transform);
                        backSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                        backSide.transform.localPosition = new Vector3(0f, 0f, boxDepth / 2f);
                        backSide.transform.forward = Vector3.back;

                        backSide.GetComponent<Renderer>().material = this.GetComponent<MeshRenderer>().material;
                        Vector2[] vectorUV = new Vector2[]
                        {
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[0,0], pillar.Value[block.Key].tSide.vUVs[0,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[1,0], pillar.Value[block.Key].tSide.vUVs[1,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[2,0], pillar.Value[block.Key].tSide.vUVs[2,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[3,0], pillar.Value[block.Key].tSide.vUVs[3,1]),
                        };
                        backSide.GetComponent<MeshFilter>().mesh.uv = vectorUV;
                    }
                    else
                    {
                        // Create a new GameObject
                        GameObject backSide = new GameObject("BackSide");

                        // Create a new MeshFilter and MeshRenderer
                        MeshFilter meshFilter = backSide.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = backSide.AddComponent<MeshRenderer>();
                        //MeshCollider meshCollider = backSide.AddComponent<MeshCollider>();

                        // Set parent
                        backSide.transform.SetParent(boxContainer.transform);
                        
                        // Create a new Mesh
                        Mesh mesh = new Mesh();

                        // Define the vertices of the quad
                        Vector3[] vertices = new Vector3[]
                        {
                            new Vector3(-0.5f, -0.5f, 0.5f),    // Bottom-right
                            new Vector3(0.5f, -0.5f, 0.5f),  // Bottom-left
                            new Vector3(-0.5f, 0.5f, 0.5f),    // Top-right
                            new Vector3(0.5f, 0.5f, 0.5f),   // Top-left

                            new Vector3(-0.5f, -0.5f, 0.5f),   // Bottom-right (mirrored)
                            new Vector3(0.5f, -0.5f, 0.5f),  // Bottom-left (mirrored)
                            new Vector3(-0.5f, 0.5f, 0.5f),    // Top-right (mirrored)
                            new Vector3(0.5f, 0.5f, 0.5f)    // Top-left (mirrored)
                        };

                        // Define the triangles
                        int[] triangles = new int[]
                        {
                            //0, 1, 2,     // Top triangle
                            //2, 1, 3,    // Bottom triangle
                            0, 1, 2,     // Top triangle
                            1, 3, 2,    // Bottom triangle

                            6, 5, 4,     // Top triangle (mirrored)
                            7, 5, 6      // Bottom triangle (mirrored)
                        };

                        // Assign the vertices, UV coordinates, and triangles to the mesh
                        mesh.SetVertices(vertices);
                        Vector2[] UVsToApply = new Vector2[8];
                        Vector2[] vectorUV = new Vector2[]
                        {
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[0,0], pillar.Value[block.Key].tSide.vUVs[0,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[1,0], pillar.Value[block.Key].tSide.vUVs[1,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[2,0], pillar.Value[block.Key].tSide.vUVs[2,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[3,0], pillar.Value[block.Key].tSide.vUVs[3,1]),
                        };
                        Vector2[] UVsToTake = vectorUV;
                        for (int i = 0; i < 2; i++) Array.Copy(UVsToTake, 0, UVsToApply, i*UVsToTake.Length, UVsToTake.Length);
                        mesh.SetUVs(0, UVsToApply);
                        mesh.SetTriangles(triangles, 0);
                        
                        // Recalculate the bounds and normals of the mesh
                        mesh.RecalculateBounds();
                        mesh.RecalculateNormals();

                        // Set the rendering mode to "Transparent" or "Fade"
                        Material material = GetComponent<MeshRenderer>().material;

                        /*material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                        material.SetFloat("_Mode", 2); // Set rendering mode to "Fade"
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;*/

                        // Add transparency
                        //Color color = material.color;
                        //color.a = 0.75f;  // Set the alpha value between 0 (fully transparent) and 1 (fully opaque)
                        //material.color = color;

                        // Assign the Material to the MeshRenderer component
                        meshRenderer.material = material;

                        // Assign the mesh to the MeshFilter component
                        meshFilter.mesh = mesh;

                        // Assign the mesh to the MeshCollider component
                        //meshCollider.sharedMesh = mesh;

                        // Enable double-sided global illumination
                        //meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    }
                }

                //left, same as back
                chunkPos2 = new Position(Mathf.FloorToInt((pillar.Key.X-1 + (iChunkWidth >> 1) - ((iChunkWidth + 1) & 1)) / (float)iChunkWidth), Mathf.FloorToInt((pillar.Key.Z + (iChunkLength >> 1) - ((iChunkLength + 1) & 1)) / (float)iChunkLength));
                blockPos2 = new Position(pillar.Key.X-1, pillar.Key.Z);
                BlockDictionary = tg.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];
                if(!BlockDictionary.ContainsKey(block.Key) || BlockDictionary[block.Key].enBlock == BlockType.Water && pillar.Value[block.Key].enBlock != BlockType.Water || BlockDictionary[block.Key].enBlock == BlockType.Leaves)
                {
                    // Create the box if it doesnt exist
                    if (boxContainer == null)
                    {
                        boxContainer = new GameObject("BoxContainer");
                        boxContainer.transform.SetParent(this.transform);
                    }

                    // Create the left side
                    if(pillar.Value[block.Key].enBlock != BlockType.Water)
                    {
                        GameObject leftSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        leftSide.name = "LeftSide";
                        leftSide.transform.SetParent(boxContainer.transform);
                        leftSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                        leftSide.transform.localPosition = new Vector3(-boxWidth / 2f, 0f, 0f);
                        leftSide.transform.forward = Vector3.right;

                        leftSide.GetComponent<Renderer>().material = this.GetComponent<MeshRenderer>().material;
                        Vector2[] vectorUV = new Vector2[]
                        {
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[0,0], pillar.Value[block.Key].tSide.vUVs[0,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[1,0], pillar.Value[block.Key].tSide.vUVs[1,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[2,0], pillar.Value[block.Key].tSide.vUVs[2,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[3,0], pillar.Value[block.Key].tSide.vUVs[3,1]),
                        };
                        leftSide.GetComponent<MeshFilter>().mesh.uv = vectorUV;
                    }
                    else
                    {
                        // Create a new GameObject
                        GameObject leftSide = new GameObject("LeftSide");

                        // Create a new MeshFilter and MeshRenderer
                        MeshFilter meshFilter = leftSide.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = leftSide.AddComponent<MeshRenderer>();
                        //MeshCollider meshCollider = leftSide.AddComponent<MeshCollider>();

                        // Set parent
                        leftSide.transform.SetParent(boxContainer.transform);
                        
                        // Create a new Mesh
                        Mesh mesh = new Mesh();

                        // Define the vertices of the quad
                        Vector3[] vertices = new Vector3[]
                        {
                            new Vector3(-0.5f, -0.5f, -0.5f),    // Bottom-right
                            new Vector3(-0.5f, -0.5f, 0.5f),  // Bottom-left
                            new Vector3(-0.5f, 0.5f, -0.5f),    // Top-right
                            new Vector3(-0.5f, 0.5f, 0.5f),   // Top-left

                            new Vector3(-0.5f, -0.5f, -0.5f),   // Bottom-right (mirrored)
                            new Vector3(-0.5f, -0.5f, 0.5f),  // Bottom-left (mirrored)
                            new Vector3(-0.5f, 0.5f, -0.5f),    // Top-right (mirrored)
                            new Vector3(-0.5f, 0.5f, 0.5f)    // Top-left (mirrored)
                        };

                        // Define the triangles
                        int[] triangles = new int[]
                        {
                            //0, 1, 2,     // Top triangle
                            //2, 1, 3,    // Bottom triangle
                            0, 1, 2,     // Top triangle
                            1, 3, 2,    // Bottom triangle

                            6, 5, 4,     // Top triangle (mirrored)
                            7, 5, 6      // Bottom triangle (mirrored)
                        };

                        // Assign the vertices, UV coordinates, and triangles to the mesh
                        mesh.SetVertices(vertices);
                        Vector2[] UVsToApply = new Vector2[8];
                        Vector2[] vectorUV = new Vector2[]
                        {
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[0,0], pillar.Value[block.Key].tSide.vUVs[0,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[1,0], pillar.Value[block.Key].tSide.vUVs[1,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[2,0], pillar.Value[block.Key].tSide.vUVs[2,1]),
                            new Vector2(pillar.Value[block.Key].tSide.vUVs[3,0], pillar.Value[block.Key].tSide.vUVs[3,1]),
                        };
                        Vector2[] UVsToTake = vectorUV;
                        for (int i = 0; i < 2; i++) Array.Copy(UVsToTake, 0, UVsToApply, i*UVsToTake.Length, UVsToTake.Length);
                        mesh.SetUVs(0, UVsToApply);
                        mesh.SetTriangles(triangles, 0);
                        
                        // Recalculate the bounds and normals of the mesh
                        mesh.RecalculateBounds();
                        mesh.RecalculateNormals();

                        // Set the rendering mode to "Transparent" or "Fade"
                        Material material = GetComponent<MeshRenderer>().material;

                        /*material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                        material.SetFloat("_Mode", 2); // Set rendering mode to "Fade"
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;*/

                        // Add transparency
                        //Color color = material.color;
                        //color.a = 0.75f;  // Set the alpha value between 0 (fully transparent) and 1 (fully opaque)
                        //material.color = color;

                        // Assign the Material to the MeshRenderer component
                        meshRenderer.material = material;

                        // Assign the mesh to the MeshFilter component
                        meshFilter.mesh = mesh;

                        // Assign the mesh to the MeshCollider component
                        //meshCollider.sharedMesh = mesh;

                        // Enable double-sided global illumination
                        //meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    }
                }

                if (boxContainer != null)
                {
                    // Place the box in the desired position in the scene
                    Vector3 boxPosition = new Vector3((pillar.Key.X - chunkPos.X*iChunkWidth)*boxWidth, block.Key*boxHeight, (pillar.Key.Z - chunkPos.Z*iChunkLength)*boxDepth); // Position of the box in world space
                    boxContainer.transform.localPosition = boxPosition;
                    pillar.Value[block.Key].SetBlockMesh(boxContainer);
                    //boxContainer.AddComponent<Outline>();
                    //boxContainer.GetComponent<Outline>().enabled = false;
                }
            }
    }

    /*public void ReuseBlocks()
    {
        for (int x = 0; x < iChunkWidth; x++)
            for (int z = 0; z < iChunkLength; z++)
            {
                float simplex1 = noise.GetSimplex(x * .8f, z * .8f) * 14 + 0.7f;
                float simplex2 = noise.GetSimplex(x * .3f, z * .3f) * 9;
                float simplex3 = noise.GetSimplex(x * 4f, z * 4f) * 7;

                float heightMap = simplex1 + simplex2 + simplex3;
                //add the 2d noise to the middle of the terrain chunk
                int baseLandHeight = (int)(iChunkHeight * .5f + heightMap);

                // Create the pillar
                Position pos = new Position(x, z);
                int baseLandHeightPillar = dBlockPillars[pos].baseLandHeight;

                if (baseLandHeightPillar < (baseLandHeight - 1)) // add all non-existent blocks
                {
                    dBlockPillars[pos].baseLandHeight = (baseLandHeight - 1);

                    // Make sure first block is accomodated, cuz the first block may be dirt block with grass
                    //3d noise for caves and overhangs and such
                    float caveNoiseFirst = noise.GetPerlinFractal(x * 5f, (baseLandHeight - 1) * 10f, z * 5f);
                    float caveMaskFirst = noise.GetSimplex(x * .3f, z * .3f) + .3f;

                    if (caveNoiseFirst > Mathf.Max(caveMaskFirst, .2f)) // Don't build block here if there is supposed to be cave
                        dBlockPillars[pos].baseLandHeight--; // part of the baseland has been "clipped", due to cave formation
                    else
                    {
                        //stone layer heightmap
                        float simplexStone1 = noise.GetSimplex(x * 1f, z * 1f) * 10;
                        float simplexStone2 = (noise.GetSimplex(x * 5f, z * 5f) + .5f) * 20 * (noise.GetSimplex(x * .3f, z * .3f) + .5f);

                        float stoneHeightMap = simplexStone1 + simplexStone2;
                        float baseStoneHeight = iChunkHeight * .25f + stoneHeightMap;

                        //float cliffThing = noise.GetSimplex(x * 1f, z * 1f, y) * 10;
                        //float cliffThingMask = noise.GetSimplex(x * .4f, z * .4f) + .3f;

                        if ((baseLandHeight - 1) <= baseStoneHeight) // Add stone block to pillar
                            dBlockPillars[pos].dBlocks.Add((baseLandHeight - 1), new Block(BlockType.Stone, TileType.Cobblestone, 0, 0));
                        else
                            dBlockPillars[pos].dBlocks.Add((baseLandHeight - 1), new Block(BlockType.Grass, TileType.GrassTop, 1, 1, TileType.Dirt, 0, 0, TileType.GrassSide, 1, 0));

                        //if(blockType != BlockType.Air)
                        //    blockType = BlockType.Stone;

                        //if(blockType == BlockType.Air && noise.GetSimplex(x * 4f, y * 4f, z*4f) < 0)
                        //  blockType = BlockType.Dirt;

                        //if(Mathf.PerlinNoise(x * .1f, z * .1f) * 10 + y < TerrainChunk.chunkHeight * .5f)
                        //    return BlockType.Grass;
                    }

                    for (int y = baseLandHeight - 2; y > baseLandHeightPillar; y--) // Do the same for all other blocks
                    {
                        //3d noise for caves and overhangs and such
                        float caveNoise1 = noise.GetPerlinFractal(x * 5f, y * 10f, z * 5f);
                        float caveMask = noise.GetSimplex(x * .3f, z * .3f) + .3f;

                        if (caveNoise1 > Mathf.Max(caveMask, .2f)) // Don't build block here if there is supposed to be cave
                        {
                            if (y == dBlockPillars[pos].baseLandHeight)
                                dBlockPillars[pos].baseLandHeight--; // part of the baseland has been "clipped", due to cave formation

                            continue;
                        }

                        //stone layer heightmap
                        float simplexStone1 = noise.GetSimplex(x * 1f, z * 1f) * 10;
                        float simplexStone2 = (noise.GetSimplex(x * 5f, z * 5f) + .5f) * 20 * (noise.GetSimplex(x * .3f, z * .3f) + .5f);

                        float stoneHeightMap = simplexStone1 + simplexStone2;
                        float baseStoneHeight = iChunkHeight * .25f + stoneHeightMap;

                        //float cliffThing = noise.GetSimplex(x * 1f, z * 1f, y) * 10;
                        //float cliffThingMask = noise.GetSimplex(x * .4f, z * .4f) + .3f;

                        if (y <= baseStoneHeight) // Add stone block to pillar
                            dBlockPillars[pos].dBlocks.Add(y, new Block(BlockType.Stone, TileType.Cobblestone, 0, 0));
                        else
                            dBlockPillars[pos].dBlocks.Add(y, new Block(BlockType.Dirt, TileType.Dirt, 1, 1));

                        //if(blockType != BlockType.Air)
                        //    blockType = BlockType.Stone;

                        //if(blockType == BlockType.Air && noise.GetSimplex(x * 4f, y * 4f, z*4f) < 0)
                        //  blockType = BlockType.Dirt;

                        //if(Mathf.PerlinNoise(x * .1f, z * .1f) * 10 + y < TerrainChunk.chunkHeight * .5f)
                        //    return BlockType.Grass;
                    }

                    for (int y = baseLandHeightPillar; y > -1; y--) // Do the same for all other blocks
                    {
                        //3d noise for caves and overhangs and such
                        float caveNoise1 = noise.GetPerlinFractal(x * 5f, y * 10f, z * 5f);
                        float caveMask = noise.GetSimplex(x * .3f, z * .3f) + .3f;

                        if (caveNoise1 > Mathf.Max(caveMask, .2f)) // Don't build block here if there is supposed to be cave
                        {
                            if (y == dBlockPillars[pos].baseLandHeight)
                                dBlockPillars[pos].baseLandHeight--; // part of the baseland has been "clipped", due to cave formation

                            continue;
                        }

                        //stone layer heightmap
                        float simplexStone1 = noise.GetSimplex(x * 1f, z * 1f) * 10;
                        float simplexStone2 = (noise.GetSimplex(x * 5f, z * 5f) + .5f) * 20 * (noise.GetSimplex(x * .3f, z * .3f) + .5f);

                        float stoneHeightMap = simplexStone1 + simplexStone2;
                        float baseStoneHeight = iChunkHeight * .25f + stoneHeightMap;

                        //float cliffThing = noise.GetSimplex(x * 1f, z * 1f, y) * 10;
                        //float cliffThingMask = noise.GetSimplex(x * .4f, z * .4f) + .3f;

                        if (y <= baseStoneHeight) // Add stone block to pillar
                        {
                            if (dBlockPillars[pos].dBlocks.ContainsKey(y))
                            {
                                if (dBlockPillars[pos].dBlocks[y].enBlock != BlockType.Stone)
                                {
                                    dBlockPillars[pos].dBlocks[y].ChangeBlock(BlockType.Stone);
                                    dBlockPillars[pos].dBlocks[y].tTop.ChangeTile(TileType.Cobblestone, 0, 0);
                                    dBlockPillars[pos].dBlocks[y].tBottom.ChangeTile(TileType.Cobblestone, 0, 0);
                                    dBlockPillars[pos].dBlocks[y].tSide.ChangeTile(TileType.Cobblestone, 0, 0);
                                }
                            }
                            else
                                dBlockPillars[pos].dBlocks.Add(y, new Block(BlockType.Stone, TileType.Cobblestone, 0, 0));
                        }
                        else
                        {
                            if (dBlockPillars[pos].dBlocks.ContainsKey(y))
                            {
                                if (dBlockPillars[pos].dBlocks[y].enBlock != BlockType.Dirt)
                                {
                                    dBlockPillars[pos].dBlocks[y].ChangeBlock(BlockType.Dirt);
                                    dBlockPillars[pos].dBlocks[y].tTop.ChangeTile(TileType.Dirt, 1, 1);
                                    dBlockPillars[pos].dBlocks[y].tBottom.ChangeTile(TileType.Dirt, 1, 1);
                                    dBlockPillars[pos].dBlocks[y].tSide.ChangeTile(TileType.Dirt, 1, 1);
                                }
                            }
                            else
                                dBlockPillars[pos].dBlocks.Add(y, new Block(BlockType.Dirt, TileType.Dirt, 1, 1));
                        }

                        //if(blockType != BlockType.Air)
                        //    blockType = BlockType.Stone;

                        //if(blockType == BlockType.Air && noise.GetSimplex(x * 4f, y * 4f, z*4f) < 0)
                        //  blockType = BlockType.Dirt;

                        //if(Mathf.PerlinNoise(x * .1f, z * .1f) * 10 + y < TerrainChunk.chunkHeight * .5f)
                        //    return BlockType.Grass;
                    }
                }
                else
                {
                    if (baseLandHeightPillar > (baseLandHeight - 1)) // delete all unusable, non-convertible blocks
                    {
                        for (int y = baseLandHeight; y <= baseLandHeightPillar; y++)
                            if (dBlockPillars[pos].dBlocks.ContainsKey(y))
                                dBlockPillars[pos].dBlocks.Remove(y);

                        dBlockPillars[pos].baseLandHeight = (baseLandHeight - 1);
                    }


                }
            }
    }*/
}
