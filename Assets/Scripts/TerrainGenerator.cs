using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
using UnityEngine.SocialPlatforms.Impl;
using Newtonsoft.Json;

public partial class TerrainGenerator : MonoBehaviour
{
    // Need to improve later ... works for now... :(
    public static TerrainGenerator instance;

    // How much of the world has been loaded till now
    public static int chunksGenerated;

    // Load data into the generator from scripts or other external sources
    public GameObject goTerrainChunk; // The chunk used to render mesh for the terrain via the attached script
    public Transform tPlayer; // The player mesh model ... so we can get location of the player

    // Chunk render distance (how many chunks to render in every direction from the player)
    public const int iRenderDistance = 2;

    // Number of chunks in every direction (how many chunks are contained in every 1 of the 8 directions from the player)
    //private const int iChunkDirection = uiRenderDistance * (uiRenderDistance+1) / 2;

    // Dictionary that saves render distance chunks (currently rendered on screen chunks)
    public Dictionary<Position, TerrainChunk> dTerrainChunks;// = new Dictionary<Position, TerrainChunk>();
    //private TerrainChunk[,] tcTerrainChunks;

    // Dictionary that saves a pool of previously rendered chunks (to load again if necessary)
    private Dictionary<Position, TerrainChunk> dPooledChunks;// = new Dictionary<Position, TerrainChunk>();

    // Set, containing the positions of half-loaded chunks (to load fully)
    //private HashSet<Position> hsHalfChunks;
    //private HashSet<Position> hsNonRenderedChunks;

    // Save position of player in the world
    private Position pCurPosition, pLastPosition;

    // World seed
    public const int seed = 666;

    private Coroutine AsyncCoroutine;

    //private static TerrainGenerator _instance;

    public FastNoise noise;// = new FastNoise(seed);

    /*public static TerrainGenerator GetInstance()
    {
        if (_instance == null)
            _instance = new TerrainGenerator();
        
        return _instance;
    }*/

    public TerrainGenerator() // Make sure only 1 istance of terrain generator can exist..... i cant even imagine the horror of multiple instances
    {
        dTerrainChunks = new Dictionary<Position, TerrainChunk>();
        dPooledChunks = new Dictionary<Position, TerrainChunk>();
        //hsHalfChunks = new HashSet<Position>();
        //hsNonRenderedChunks = new HashSet<Position>();
        pCurPosition = new Position(0, 0);
        pLastPosition = new Position(0, 0);
        //tcTerrainChunks = new TerrainChunk[8, uiChunkDirection];

        noise = new FastNoise(seed);

        instance = this;
        chunksGenerated = 0;
        AsyncCoroutine = null;
    }

    // Disable self-referencing loop handling in the JsonSerializerSettings
    JsonSerializerSettings settings = new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    //private TerrainGenerator(TerrainGenerator original) {} // no need for a copy constructor, just make it private so it cant be accessed outside

    // Start is called before the first frame update
    void Start()
    {
        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), seed.ToString());
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
        UnityEngine.Random.InitState(seed);
        tPlayer.transform.position = new Vector3(0f,55f,0f);

        // Load all blocks for the chunks inside the render distance + 1
        for (int x = -(iRenderDistance + 1); x <= (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
            for (int z = -(iRenderDistance + 1); z <= (iRenderDistance + 1); z++) // Load all Z chunks at location X
            {
                Position pNew = new Position(x, z); // New chunk X position
                TerrainChunk tc = Instantiate(goTerrainChunk, new Vector3(TerrainChunk.iChunkWidth*pNew.X, 0, TerrainChunk.iChunkLength*pNew.Z), Quaternion.identity).GetComponent<TerrainChunk>(); // instantiate new chunk if memory is available
                tc.gameObject.SetActive(false); // make it invisible ... no need to take gpu power

                try
                {
                    // Try to read file
                    // Read file lines - first line is the object, second line - how it's rendered (fully, not fully)
                    string[] lines = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), seed.ToString(), $"{pNew.X}_{pNew.Z}.txt"));
                    tc.dBlockPillars = JsonConvert.DeserializeObject<Dictionary<Position, OrderedDictionary<int, Block>>>(lines[0]);
                    if (Convert.ToBoolean(lines[1]) == true)
                        tc.isPropsLoaded = true;
                }
                catch (Exception e) // File can't be read for some reason
                {
                    // Let the user know what went wrong
                    Debug.Log("The file could not be read: " + e.Message);
                    tc.BuildBlocks(this, pNew); // load the chunk's blocks
                }

                dTerrainChunks.Add(pNew, tc); // Add current chunk to the rendered screen
            }

        // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
        for (int x = -iRenderDistance; x <= iRenderDistance; x++) // Load only the X chunks, which aren't loaded
            for (int z = -iRenderDistance; z <= iRenderDistance; z++) // Load all Z chunks at location X
            {
                Position pNew = new Position(x, z); // New chunk X position
                RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
            }

        // Build first-time chunks and instantiate them
        //for (int i = -iRenderDistance; i <= iRenderDistance; i++)
        //    for (int j = -iRenderDistance; j <= iRenderDistance; j++)
        //        BuildChunk(i, j, Instantiate(goTerrainChunk, new Vector3(i, 0, j), Quaternion.identity).GetComponent<TerrainChunk>());

    }

    // Update is called once per frame
    void Update()
    {
        pCurPosition = new Position((int)(tPlayer.position.x/TerrainChunk.iChunkWidth), (int)(tPlayer.position.z/TerrainChunk.iChunkLength));

        if ((pCurPosition.X != pLastPosition.X || pCurPosition.Z != pLastPosition.Z) && AsyncCoroutine == null)
        {
            AsyncCoroutine = StartCoroutine(UpdateChunksPartlyAsyncCoroutine(pCurPosition.X, pCurPosition.Z, pLastPosition.X, pLastPosition.Z));
            pLastPosition = pCurPosition;
        }

        //yield return null; // Yield.

        /*switch(UpdateChunksPartly(pCurPosition.X, pCurPosition.Z, pLastPosition.X, pLastPosition.Z))
        {
            case 0: // Cant update partly, because chunks are too far away to optimize by keeping some of them
            {
                UpdateChunksFully(pCurPosition.X, pCurPosition.Z, pLastPosition.X, pLastPosition.Z); // So update fully all chunks
                pLastPosition = pCurPosition;
                break;
            }
            case 1: // Successfully updated partly
            {
                pLastPosition = pCurPosition;
                break;
            }
            case 2: // No update has happened
            {
                break;
            }
            default:
            {
                break;
            }
        }*/
        //UpdateAllChunks();
    }

    IEnumerator UpdateChunksPartlyAsyncCoroutine(int curX, int curZ, int lastX, int lastZ)
    {
        // Perform your asynchronous operations here
    
        yield return StartCoroutine(UpdateChunksPartlyAsync(curX, curZ, lastX, lastZ));
        AsyncCoroutine = null;
    
        // Code to execute after the asynchronous operations complete
    }

    // Obsolete --- slow
    // When user gets teleported or moved abruptly .... more chunks need to load (take advantage of already loaded chunks (if there are any))
    /*private void LoadAllChunks(int x, int z)
    {
        HashSet<Position> pHashSet = new HashSet<Position>();
        List<Position> pList = new List<Position>();
        for (int i = x - iRenderDistance; i <= x + iRenderDistance; i++)
            for (int j = z - iRenderDistance; j <= z + iRenderDistance; j++)
            {
                Position p = new Position(i, j);
                if (dTerrainChunks.ContainsKey(p))
                {
                    pHashSet.Add(p);
                    continue;
                }

                pList.Add(p);
            }

        List<Position> toDelete = new List<Position>();
        foreach (Position p in dTerrainChunks.Keys)
        {
            if (pHashSet.Contains(p))
                continue;

            toDelete.Add(p);
        }

        //foreach(var v in dTerrainChunks.Values)

        for (int i = 0; i < pList.Count; i++)
        {
            //Destroy(kvp.Value);
            //dTerrainChunks.Remove(kvp.Key);
            //kvp.Key.X = pList.Last().X;)
            BuildChunk(pList[i].X, pList[i].Z, dTerrainChunks[toDelete[i]]);
            //pList.Remove(pList.Last());
            dTerrainChunks.Remove(toDelete[i]);
        }
    }*/

    // Load a certain's block chunks ... without rendering the chunk itself
    private void LoadChunkBlocks(Position pNew, Position pOld)
    {
        // If chunk has already been rendered and pooled --- don't load blocks
        if (!dPooledChunks.ContainsKey(pNew))
        {
            TerrainChunk tc; // Terrain chunk to load blocks for
            if (dPooledChunks.Count < iRenderDistance * 35) // Don't take up too much memory, so
            {
                tc = Instantiate(goTerrainChunk, new Vector3(TerrainChunk.iChunkWidth*pNew.X, 0, TerrainChunk.iChunkLength*pNew.Z), Quaternion.identity).GetComponent<TerrainChunk>(); // instantiate new chunk if memory is available
                tc.gameObject.SetActive(false); // make it invisible ... no need to take gpu power

                try
                {
                    // Try to read file
                    // Read file lines - first line is the object, second line - how it's rendered (fully, not fully)
                    string[] lines = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), seed.ToString(), $"{pNew.X}_{pNew.Z}.txt"));
                    tc.dBlockPillars = JsonConvert.DeserializeObject<Dictionary<Position, OrderedDictionary<int, Block>>>(lines[0]);
                    if (bool.TryParse(lines[1], out bool isPropsLoaded))
                        tc.isPropsLoaded = isPropsLoaded;
                }
                catch (Exception e) // File can't be read for some reason
                {
                    // Let the user know what went wrong
                    Debug.Log("The file could not be read: " + e.Message);
                    tc.BuildBlocks(this, pNew); // load the chunk's blocks
                }
            }
            else // Otherwise
            {
                //Position p = dPooledChunks.First().Key;// Reuse random old chunk by teraforming it's blocks .. not perfect, cuz the algorithm is not necessarily taking the furthest from
                Position p = dPooledChunks.Keys.ElementAt(UnityEngine.Random.Range(0, dPooledChunks.Count));
                tc = dPooledChunks[p]; // the player chunk to terraform, but creating such algorithm may prove to be as "heavy"/complex as using a random block... thus making the process unnecessary
                dPooledChunks.Remove(p); // Remove it from the pool,

                int x = (int)tc.gameObject.transform.position.x/16;
                int y = (int)tc.gameObject.transform.position.z/16;
                try // But save it to a file, cuz u need to save changes made to chunk
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), seed.ToString(), $"{pNew.X}_{pNew.Z}.txt");
                    using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            sw.WriteLine(JsonConvert.SerializeObject(tc.dBlockPillars), settings);
                            sw.WriteLine(tc.isPropsLoaded);
                        }
                    }
                }
                catch (Exception e) // File can't be written to for some reason
                {
                    // Let the user know what went wrong
                    Debug.Log("The file could not be written to: " + e.Message);
                }

                tc.transform.position = new Vector3(TerrainChunk.iChunkWidth*pNew.X, 0, TerrainChunk.iChunkLength*pNew.Z); // Move chunk to new position
                tc.isPropsLoaded = false;
                tc.isRendered = false;
                foreach (Transform child in tc.transform) Destroy(child.gameObject);
                tc.ReuseBlocks(this, pNew); // reuse the old chunk's blocks
            }

            //tc.gameObject.SetActive(false); // make it invisible ... no need to take gpu power
            //dTerrainChunks.Add(pNew, tc); // Add chunk to currently rendered on screen chunks
            //dPooledChunks.Add(pNew, tc); // Add chunk to the pool
            dTerrainChunks.Add(pNew, tc); // Add current chunk to the rendered screen
            //hsHalfChunks.Add(pNew); // Safe this chunk as half-rendered one
        }
        else
        {
            dTerrainChunks.Add(pNew, dPooledChunks[pNew]); // Add current chunk to the rendered screen
            dPooledChunks.Remove(pNew); // Remove new chunk from the pool
        }

        chunksGenerated++;
        dPooledChunks.Add(pOld, dTerrainChunks[pOld]); // Pool old chunk
        dPooledChunks[pOld].gameObject.SetActive(false); // Make old chunk invisible
        dTerrainChunks.Remove(pOld); // Remove old chunk from rendered terrain dictionary
    }

    // Update a pair of chunks... basically "display" new chunk and "remove" old (far from player) chunk
    private void RenderChunkBlocks(Position pNew)
    {
        //dPooledChunks.Add(pOld, dTerrainChunks[pOld]); // Pool old chunk
        //dPooledChunks[pOld].gameObject.SetActive(false); // Make old chunk invisible
        //dTerrainChunks.Remove(pOld); // Remove old chunk from rendered terrain dictionary

        //Debug.Log("Chunk: " + pNew.X + " " + pNew.Z + "is inside dictionary: " + dTerrainChunks.ContainsKey(pNew));
        
        //dTerrainChunks.Add(pNew, dPooledChunks[pNew]);
        dTerrainChunks[pNew].gameObject.SetActive(true); // Make chunk visible
        //dPooledChunks.Remove(pNew); // Remove new chunk from pool

        if (!dTerrainChunks[pNew].isPropsLoaded) // Chunk is half-loaded ... load it fully
        {
            //BuildChunk(pNew, dTerrainChunks[pNew]); // use it for biome formation
            //generate trees
            //sHalfChunks.Remove(pNew); // Isn't half-loaded anymore
            dTerrainChunks[pNew].BuildTrees(this, pNew);
            dTerrainChunks[pNew].isPropsLoaded = true;
        }

        if (!dTerrainChunks[pNew].isRendered) // Chunk is half-loaded ... load it fully
        {
            //render chunk
            //hsNonRenderedChunks.Remove(pNew); // Isn't half-loaded anymore
            dTerrainChunks[pNew].BuildMesh(this, pNew);
            dTerrainChunks[pNew].isRendered = true;
        }
    }

    private void UpdateChunksFully(int ToX, int ToZ, int FromX, int FromZ)
    {
        // Load all blocks for the chunks inside the render distance + 1
        for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
            for (int z = ToZ - (iRenderDistance + 1); z <= ToZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
            {
                Position pOld = new Position(FromX + x - ToX, FromZ + z - ToZ); // Old chunk Z position
                Position pNew = new Position(x, z); // New chunk X position
                LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
            }

        // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
        for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load only the X chunks, which aren't loaded
            for (int z = ToZ - iRenderDistance; z <= ToZ + iRenderDistance; z++) // Load all Z chunks at location X
            {
                Position pNew = new Position(x, z); // New chunk X position
                RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
            }
    }

    IEnumerator UpdateChunksPartlyAsync(int ToX, int ToZ, int FromX, int FromZ)
    {
        if (ToX > FromX) // Player has been moved to a different X location, which is > than previous location
        {
            if (ToX - iRenderDistance > FromX + iRenderDistance) // Check to see if some of the X chunks are already loaded
                yield break;

            if (ToZ == FromZ)
            {
                // Load all blocks for the chunks inside the render distance + 1
                for (int x = FromX + (iRenderDistance + 2); x <= ToX + (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - (iRenderDistance + 1); z <= ToZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
                    {
                        Position pOld = new Position(x - (2 * iRenderDistance + 3), z); // Old chunk Z position
                        Position pNew = new Position(x, z); // New chunk X position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                        yield return new WaitForSeconds(0.5f);
                    }

                // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
                for (int x = FromX + (iRenderDistance + 1); x <= ToX + iRenderDistance; x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - iRenderDistance; z <= ToZ + iRenderDistance; z++) // Load all Z chunks at location X
                    {
                        Position pNew = new Position(x, z); // New chunk X position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                        yield return new WaitForSeconds(1.5f);
                    }
            }
            else if (ToZ > FromZ) // Player has also been moved to a different Y location, which is > than previous location
            {
                if (ToZ - iRenderDistance > FromZ + iRenderDistance) // Check to see if some of the Z chunks are already loaded
                    yield break;

                // Load all blocks for the chunks inside the render distance + 1
                for (int x = FromX + (iRenderDistance + 2); x <= ToX + (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - (iRenderDistance + 1); z <= FromZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
                    {
                        Position pOld = new Position(x - (2 * iRenderDistance + 3), z); // Old chunk X position
                        Position pNew = new Position(x, z); // New chunk X position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                        yield return new WaitForSeconds(0.5f);
                    }
                for (int z = FromZ + (iRenderDistance + 2); z <= ToZ + (iRenderDistance + 1); z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load all X chunks at location Z
                    {
                        Position pOld = new Position(x - (ToX - FromX), z - (2 * iRenderDistance + 3)); // Old chunk Z position
                        Position pNew = new Position(x, z); // New chunk Z position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                        yield return new WaitForSeconds(1.5f);
                    }

                // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
                for (int x = FromX + (iRenderDistance + 1); x <= ToX + iRenderDistance; x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - iRenderDistance; z <= FromZ + iRenderDistance; z++) // Load all Z chunks at location X
                    {
                        Position pNew = new Position(x, z); // New chunk X position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                        yield return new WaitForSeconds(0.5f);
                    }
                for (int z = FromZ + (iRenderDistance + 1); z <= ToZ + iRenderDistance; z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load all X chunks at location Z
                    {
                        Position pNew = new Position(x, z); // New chunk Z position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                        yield return new WaitForSeconds(1.5f);
                    }
            }
            else // Player has also been moved to a different Y location, which is < than previous location
            {
                if (ToZ + iRenderDistance < FromZ - iRenderDistance) // Check to see if some of the Z chunks are already loaded
                    yield break;

                // Load all blocks for the chunks inside the render distance + 1
                for (int x = FromX + (iRenderDistance + 2); x <= ToX + (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
                    for (int z = FromZ - (iRenderDistance + 1); z <= ToZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
                    {
                        Position pOld = new Position(x - (2 * iRenderDistance + 3), z); // Old chunk X position
                        Position pNew = new Position(x, z); // New chunk X position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                        yield return new WaitForSeconds(0.5f);
                    }
                for (int z = ToZ - (iRenderDistance + 1); z <= FromZ - (iRenderDistance + 2); z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load all X chunks at location Z
                    {
                        Position pOld = new Position(x - (ToX - FromX), z + (2 * iRenderDistance + 3)); // Old chunk Z position
                        Position pNew = new Position(x, z); // New chunk Z position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                        yield return new WaitForSeconds(0.5f);
                    }

                // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
                for (int x = FromX + (iRenderDistance + 1); x <= ToX + iRenderDistance; x++) // Load only the X chunks, which aren't loaded
                    for (int z = FromZ - iRenderDistance; z <= ToZ + iRenderDistance; z++) // Load all Z chunks at location X
                    {
                        Position pNew = new Position(x, z); // New chunk X position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                        yield return new WaitForSeconds(1.5f);
                    }
                for (int z = ToZ - iRenderDistance; z <= FromZ - (iRenderDistance + 1); z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load all X chunks at location Z
                    {
                        Position pNew = new Position(x, z); // New chunk Z position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                        yield return new WaitForSeconds(1.5f);
                    }
            }
        }
        else if (ToX < FromX) // Player has been moved to a different X location, which is < than previous location
        {
            if (ToX + iRenderDistance < FromX - iRenderDistance) // Check to see if some of the X chunks are already loaded
                yield break;
            
            if (ToZ == FromZ)
            {
                // Load all blocks for the chunks inside the render distance + 1
                for (int x = ToX - (iRenderDistance + 1); x <= FromX - (iRenderDistance + 2); x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - (iRenderDistance + 1); z <= ToZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
                    {
                        Position pOld = new Position(x + (2 * iRenderDistance + 3), z); // Old chunk Z position
                        Position pNew = new Position(x, z); // New chunk X position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                        yield return new WaitForSeconds(0.5f);
                    }

                // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
                for (int x = ToX - iRenderDistance; x <= FromX - (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - iRenderDistance; z <= ToZ + iRenderDistance; z++) // Load all Z chunks at location X
                    {
                        Position pNew = new Position(x, z); // New chunk X position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                        yield return new WaitForSeconds(1.5f);
                    }
            }
            else if (ToZ > FromZ) // Player has also been moved to a different Y location, which is > than previous location
            {
                if (ToZ - iRenderDistance > FromZ + iRenderDistance) // Check to see if some of the Z chunks are already loaded
                    yield break;

                // Load all blocks for the chunks inside the render distance + 1
                for (int x = ToX - (iRenderDistance + 1); x <= FromX - (iRenderDistance + 2); x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - (iRenderDistance + 1); z <= FromZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
                    {
                        Position pOld = new Position(x + (2 * iRenderDistance + 3), z); // Old chunk X position
                        Position pNew = new Position(x, z); // New chunk X position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                        yield return new WaitForSeconds(0.5f);
                    }
                for (int z = FromZ + (iRenderDistance + 2); z <= ToZ + (iRenderDistance + 1); z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load all X chunks at location Z
                    {
                        Position pOld = new Position(x + (FromX - ToX), z - (2 * iRenderDistance + 3)); // Old chunk Z position
                        Position pNew = new Position(x, z); // New chunk Z position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                        yield return new WaitForSeconds(0.5f);
                    }

                // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
                for (int x = ToX - iRenderDistance; x <= FromX - (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - iRenderDistance; z <= FromZ + iRenderDistance; z++) // Load all Z chunks at location X
                    {
                        Position pNew = new Position(x, z); // New chunk X position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                        yield return new WaitForSeconds(1.5f);
                    }
                for (int z = FromZ + (iRenderDistance + 1); z <= ToZ + iRenderDistance; z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load all X chunks at location Z
                    {
                        Position pNew = new Position(x, z); // New chunk Z position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                        yield return new WaitForSeconds(1.5f);
                    }
            }
            else // Player has also been moved to a different Y location, which is < than previous location
            {
                if (ToZ + iRenderDistance < FromZ - iRenderDistance) // Check to see if some of the Z chunks are already loaded
                    yield break;

                // Load all blocks for the chunks inside the render distance + 1
                for (int x = ToX - (iRenderDistance + 1); x <= FromX - (iRenderDistance + 2); x++) // Load only the X chunks, which aren't loaded
                    for (int z = FromZ - (iRenderDistance + 1); z <= ToZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
                    {
                        Position pOld = new Position(x + (2 * iRenderDistance + 3), z); // Old chunk X position
                        Position pNew = new Position(x, z); // New chunk X position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                        yield return new WaitForSeconds(0.5f);
                    }
                for (int z = ToZ - (iRenderDistance + 1); z <= FromZ - (iRenderDistance + 2); z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load all X chunks at location Z
                    {
                        Position pOld = new Position(x + (FromX - ToX), z + (2 * iRenderDistance + 3)); // Old chunk Z position
                        Position pNew = new Position(x, z); // New chunk Z position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                        yield return new WaitForSeconds(0.5f);
                    }

                // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
                for (int x = ToX - iRenderDistance; x <= FromX - (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
                    for (int z = FromZ - iRenderDistance; z <= ToZ + iRenderDistance; z++) // Load all Z chunks at location X
                    {
                        Position pNew = new Position(x, z); // New chunk X position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                        yield return new WaitForSeconds(1.5f);
                    }
                for (int z = ToZ - iRenderDistance; z <= FromZ - (iRenderDistance + 1); z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load all X chunks at location Z
                    {
                        Position pNew = new Position(x, z); // New chunk Z position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                        yield return new WaitForSeconds(1.5f);
                    }
            }
        }
        else if (ToZ > FromZ) // Player has been moved to a different Z location, which is > than previous location
        {
            if (ToZ - iRenderDistance > FromZ + iRenderDistance) // Check to see if some of the Z chunks are already loaded
                yield break;

            for (int z = FromZ + (iRenderDistance + 2); z <= ToZ + (iRenderDistance + 1); z++) // Load only the Z chunks, which aren't loaded
                for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load all X chunks at location Z
                {
                    Position pOld = new Position(x, z - (2 * iRenderDistance + 3)); // Old chunk Z position
                    Position pNew = new Position(x, z); // Old chunk Z position
                    LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                    yield return new WaitForSeconds(0.5f);
                }

            for (int z = FromZ + (iRenderDistance + 1); z <= ToZ + iRenderDistance; z++) // Load only the Z chunks, which aren't loaded
                for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load all X chunks at location Z
                {
                    Position pNew = new Position(x, z); // New chunk X position
                    RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                    yield return new WaitForSeconds(1.5f);
                }
        }
        else if (ToZ < FromZ) // Player has been moved to a different Z location, which is < than previous location
        {
            if (ToZ + iRenderDistance < FromZ - iRenderDistance) // Check to see if some of the Z chunks are already loaded
                yield break;

            for (int z = ToZ - (iRenderDistance + 1); z <= FromZ - (iRenderDistance + 2); z++) // Load only the Z chunks, which aren't loaded
                for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load all X chunks at location Z
                {
                    Position pOld = new Position(x, z + (2 * iRenderDistance + 3)); // Old chunk Z position
                    Position pNew = new Position(x, z); // Old chunk Z position
                    LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                    yield return new WaitForSeconds(0.5f);
                }

            for (int z = ToZ - iRenderDistance; z <= FromZ - (iRenderDistance + 1); z++) // Load only the Z chunks, which aren't loaded
                for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load all X chunks at location Z
                {
                    Position pNew = new Position(x, z); // New chunk X position
                    RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                    yield return new WaitForSeconds(1.5f);
                }
        }

        //yield return null; // Yield the execution briefly
    }

    // Took me a while to make it with O(n) time complexity --- linear execution, looks messy, but highly optimized (cuz checks only newly generated chunks and uses if checks which are faster than value assignment)
    private int UpdateChunksPartly(int ToX, int ToZ, int FromX, int FromZ)
    {
        if (ToX > FromX) // Player has been moved to a different X location, which is > than previous location
        {
            if (ToX - iRenderDistance > FromX + iRenderDistance) // Check to see if some of the X chunks are already loaded
                return 0;

            if (ToZ == FromZ)
            {
                // Load all blocks for the chunks inside the render distance + 1
                for (int x = FromX + (iRenderDistance + 2); x <= ToX + (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - (iRenderDistance + 1); z <= ToZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
                    {
                        Position pOld = new Position(x - (2 * iRenderDistance + 3), z); // Old chunk Z position
                        Position pNew = new Position(x, z); // New chunk X position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                    }

                // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
                for (int x = FromX + (iRenderDistance + 1); x <= ToX + iRenderDistance; x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - iRenderDistance; z <= ToZ + iRenderDistance; z++) // Load all Z chunks at location X
                    {
                        Position pNew = new Position(x, z); // New chunk X position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                    }
                
                return 1;
            }
            else if (ToZ > FromZ) // Player has also been moved to a different Y location, which is > than previous location
            {
                if (ToZ - iRenderDistance > FromZ + iRenderDistance) // Check to see if some of the Z chunks are already loaded
                    return 0;

                // Load all blocks for the chunks inside the render distance + 1
                for (int x = FromX + (iRenderDistance + 2); x <= ToX + (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - (iRenderDistance + 1); z <= FromZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
                    {
                        Position pOld = new Position(x - (2 * iRenderDistance + 3), z); // Old chunk X position
                        Position pNew = new Position(x, z); // New chunk X position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                    }
                for (int z = FromZ + (iRenderDistance + 2); z <= ToZ + (iRenderDistance + 1); z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load all X chunks at location Z
                    {
                        Position pOld = new Position(x - (ToX - FromX), z - (2 * iRenderDistance + 3)); // Old chunk Z position
                        Position pNew = new Position(x, z); // New chunk Z position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                    }

                // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
                for (int x = FromX + (iRenderDistance + 1); x <= ToX + iRenderDistance; x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - iRenderDistance; z <= FromZ + iRenderDistance; z++) // Load all Z chunks at location X
                    {
                        Position pNew = new Position(x, z); // New chunk X position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                    }
                for (int z = FromZ + (iRenderDistance + 1); z <= ToZ + iRenderDistance; z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load all X chunks at location Z
                    {
                        Position pNew = new Position(x, z); // New chunk Z position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                    }
                
                return 1;
            }
            else // Player has also been moved to a different Y location, which is < than previous location
            {
                if (ToZ + iRenderDistance < FromZ - iRenderDistance) // Check to see if some of the Z chunks are already loaded
                    return 0;

                // Load all blocks for the chunks inside the render distance + 1
                for (int x = FromX + (iRenderDistance + 2); x <= ToX + (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
                    for (int z = FromZ - (iRenderDistance + 1); z <= ToZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
                    {
                        Position pOld = new Position(x - (2 * iRenderDistance + 3), z); // Old chunk X position
                        Position pNew = new Position(x, z); // New chunk X position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                    }
                for (int z = ToZ - (iRenderDistance + 1); z <= FromZ - (iRenderDistance + 2); z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load all X chunks at location Z
                    {
                        Position pOld = new Position(x - (ToX - FromX), z + (2 * iRenderDistance + 3)); // Old chunk Z position
                        Position pNew = new Position(x, z); // New chunk Z position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                    }

                // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
                for (int x = FromX + (iRenderDistance + 1); x <= ToX + iRenderDistance; x++) // Load only the X chunks, which aren't loaded
                    for (int z = FromZ - iRenderDistance; z <= ToZ + iRenderDistance; z++) // Load all Z chunks at location X
                    {
                        Position pNew = new Position(x, z); // New chunk X position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                    }
                for (int z = ToZ - iRenderDistance; z <= FromZ - (iRenderDistance + 1); z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load all X chunks at location Z
                    {
                        Position pNew = new Position(x, z); // New chunk Z position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                    }

                return 1;
            }
        }
        else if (ToX < FromX) // Player has been moved to a different X location, which is < than previous location
        {
            if (ToX + iRenderDistance < FromX - iRenderDistance) // Check to see if some of the X chunks are already loaded
                return 0;
            
            if (ToZ == FromZ)
            {
                // Load all blocks for the chunks inside the render distance + 1
                for (int x = ToX - (iRenderDistance + 1); x <= FromX - (iRenderDistance + 2); x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - (iRenderDistance + 1); z <= ToZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
                    {
                        Position pOld = new Position(x + (2 * iRenderDistance + 3), z); // Old chunk Z position
                        Position pNew = new Position(x, z); // New chunk X position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                    }

                // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
                for (int x = ToX - iRenderDistance; x <= FromX - (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - iRenderDistance; z <= ToZ + iRenderDistance; z++) // Load all Z chunks at location X
                    {
                        Position pNew = new Position(x, z); // New chunk X position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                    }

                return 1;
            }
            else if (ToZ > FromZ) // Player has also been moved to a different Y location, which is > than previous location
            {
                if (ToZ - iRenderDistance > FromZ + iRenderDistance) // Check to see if some of the Z chunks are already loaded
                    return 0;

                // Load all blocks for the chunks inside the render distance + 1
                for (int x = ToX - (iRenderDistance + 1); x <= FromX - (iRenderDistance + 2); x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - (iRenderDistance + 1); z <= FromZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
                    {
                        Position pOld = new Position(x + (2 * iRenderDistance + 3), z); // Old chunk X position
                        Position pNew = new Position(x, z); // New chunk X position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                    }
                for (int z = FromZ + (iRenderDistance + 2); z <= ToZ + (iRenderDistance + 1); z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load all X chunks at location Z
                    {
                        Position pOld = new Position(x + (ToX - FromX), z - (2 * iRenderDistance + 3)); // Old chunk Z position
                        Position pNew = new Position(x, z); // New chunk Z position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                    }

                // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
                for (int x = ToX - iRenderDistance; x <= FromX - (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
                    for (int z = ToZ - iRenderDistance; z <= FromZ + iRenderDistance; z++) // Load all Z chunks at location X
                    {
                        Position pNew = new Position(x, z); // New chunk X position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                    }
                for (int z = FromZ + (iRenderDistance + 1); z <= ToZ + iRenderDistance; z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load all X chunks at location Z
                    {
                        Position pNew = new Position(x, z); // New chunk Z position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                    }

                return 1;
            }
            else // Player has also been moved to a different Y location, which is < than previous location
            {
                if (ToZ + iRenderDistance < FromZ - iRenderDistance) // Check to see if some of the Z chunks are already loaded
                    return 0;

                // Load all blocks for the chunks inside the render distance + 1
                for (int x = ToX - (iRenderDistance + 1); x <= FromX - (iRenderDistance + 2); x++) // Load only the X chunks, which aren't loaded
                    for (int z = FromZ - (iRenderDistance + 1); z <= ToZ + (iRenderDistance + 1); z++) // Load all Z chunks at location X
                    {
                        Position pOld = new Position(x + (2 * iRenderDistance + 3), z); // Old chunk X position
                        Position pNew = new Position(x, z); // New chunk X position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                    }
                for (int z = ToZ - (iRenderDistance + 1); z <= FromZ - (iRenderDistance + 2); z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load all X chunks at location Z
                    {
                        Position pOld = new Position(x + (ToX - FromX), z + (2 * iRenderDistance + 3)); // Old chunk Z position
                        Position pNew = new Position(x, z); // New chunk Z position
                        LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                    }

                // Chunk's neighbour's blocks need to be loaded, before we can render the chunk(cuz we need to know what block is next to cur block)... that's why we first load the blocks and then update them (e.g. render them)
                for (int x = ToX - iRenderDistance; x <= FromX - (iRenderDistance + 1); x++) // Load only the X chunks, which aren't loaded
                    for (int z = FromZ - iRenderDistance; z <= ToZ + iRenderDistance; z++) // Load all Z chunks at location X
                    {
                        Position pNew = new Position(x, z); // New chunk X position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                    }
                for (int z = ToZ - iRenderDistance; z <= FromZ - (iRenderDistance + 1); z++) // Load only the Z chunks, which aren't loaded
                    for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load all X chunks at location Z
                    {
                        Position pNew = new Position(x, z); // New chunk Z position
                        RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                    }

                return 1;
            }
        }
        else if (ToZ > FromZ) // Player has been moved to a different Z location, which is > than previous location
        {
            if (ToZ - iRenderDistance > FromZ + iRenderDistance) // Check to see if some of the Z chunks are already loaded
                return 0;

            for (int z = FromZ + (iRenderDistance + 2); z <= ToZ + (iRenderDistance + 1); z++) // Load only the Z chunks, which aren't loaded
                for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load all X chunks at location Z
                {
                    Position pOld = new Position(x, z - (2 * iRenderDistance + 3)); // Old chunk Z position
                    Position pNew = new Position(x, z); // Old chunk Z position
                    LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                }

            for (int z = FromZ + (iRenderDistance + 1); z <= ToZ + iRenderDistance; z++) // Load only the Z chunks, which aren't loaded
                for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load all X chunks at location Z
                {
                    Position pNew = new Position(x, z); // New chunk X position
                    RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                }

            return 1;
        }
        else if (ToZ < FromZ) // Player has been moved to a different Z location, which is < than previous location
        {
            if (ToZ + iRenderDistance < FromZ - iRenderDistance) // Check to see if some of the Z chunks are already loaded
                return 0;

            for (int z = ToZ - (iRenderDistance + 1); z <= FromZ - (iRenderDistance + 2); z++) // Load only the Z chunks, which aren't loaded
                for (int x = ToX - (iRenderDistance + 1); x <= ToX + (iRenderDistance + 1); x++) // Load all X chunks at location Z
                {
                    Position pOld = new Position(x, z + (2 * iRenderDistance + 3)); // Old chunk Z position
                    Position pNew = new Position(x, z); // Old chunk Z position
                    LoadChunkBlocks(pNew, pOld); // Load this chunk's blocks
                }

            for (int z = ToZ - iRenderDistance; z <= FromZ - (iRenderDistance + 1); z++) // Load only the Z chunks, which aren't loaded
                for (int x = ToX - iRenderDistance; x <= ToX + iRenderDistance; x++) // Load all X chunks at location Z
                {
                    Position pNew = new Position(x, z); // New chunk X position
                    RenderChunkBlocks(pNew); // Update chunk pair (pool old chunk, render new chunk)
                }
            
            return 1;
        }

        return 2;
    }
}
