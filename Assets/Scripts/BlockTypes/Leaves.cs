using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaveUpdater : MonoBehaviour
{
    private static LeaveUpdater instance;

    private void Awake()
    {
        // Ensure only one instance of DirtUpdater exists
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    // Method to start the coroutine
    public Coroutine StartUpdating(Leave leave, TerrainGenerator tg, Position chunkPos, Position pillarPos, int height, float minWaitTime, float maxWaitTime)
    {
        return StartCoroutine(WaitAndRemoveLeave(leave, tg, chunkPos, pillarPos, height, minWaitTime, maxWaitTime));
    }

    public void StopUpdating(Coroutine coroutine)
    {
        StopCoroutine(coroutine);
    }

    // The coroutine
    private IEnumerator WaitAndRemoveLeave(Leave leave, TerrainGenerator tg, Position chunkPos, Position pillarPos, int height, float minWaitTime, float maxWaitTime)
    {
        float waitTime = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(waitTime);
        if(leave.RemoveLeave(tg, chunkPos, pillarPos, height))
            Destroy(tg.dTerrainChunks[chunkPos].dBlockPillars[pillarPos][height].goBlockMesh);
        leave.coroutine = null;
    }
}

public class Leave : Block
{
    private LeaveUpdater leaveUpdater;
    public Coroutine coroutine;

    public override bool StartUpdating(bool instant, TerrainGenerator tg, Position chunkPos, Position pillarPos, int height)
    {
        if (instant)
        {
            return RemoveLeave(tg, chunkPos, pillarPos, height);
        }
        else if (coroutine == null)
        {
            //myCoroutine = StartCoroutine(WaitAndTurnToGrass(45f, 250f));
            coroutine = leaveUpdater.StartUpdating(this, tg, chunkPos, pillarPos, height, 45f, 250f);
            return true;
        }
        
        Debug.Log("Can't start coroutine, as coroutine is already running for this block");
        return false;
    }

    public override bool StopUpdating()
    {
        if (coroutine != null)
        {
            leaveUpdater.StopUpdating(coroutine);
            coroutine = null;
            return true;
        }

        return false;
    }

    public bool RemoveLeave(TerrainGenerator tg, Position chunkPos, Position pillarPos, int height)
    {
        if (!tg.dTerrainChunks.ContainsKey(chunkPos))
            return false;
        
        var BlockDictionary = tg.dTerrainChunks[chunkPos].dBlockPillars[pillarPos];
        if (!BlockDictionary.ContainsKey(height))
            return false;
        
        //Destroy(BlockDictionary[height].goBlockMesh);
        BlockDictionary.Remove(height);
        return true;
    }

    public Leave() : base(BlockType.Leaves, TileType.Leaves, 4, 12)
    {
    }

    ~Leave()
    {
        StopUpdating();
    }
}