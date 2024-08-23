using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirtUpdater : MonoBehaviour
{
    private static DirtUpdater instance;

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
    public Coroutine StartUpdating(Dirt dirt, float minWaitTime, float maxWaitTime)
    {
        Debug.Log("Dirt coroutine started");
        return StartCoroutine(WaitAndTurnToGrass(dirt, minWaitTime, maxWaitTime));
    }

    public void StopUpdating(Coroutine coroutine)
    {
        StopCoroutine(coroutine);
    }

    // The coroutine
    private IEnumerator WaitAndTurnToGrass(Dirt dirt, float minWaitTime, float maxWaitTime)
    {
        float waitTime = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(waitTime);
        dirt.TurnToGrass();
        dirt.coroutine = null;
        Debug.Log("Dirt coroutine finished");
    }
}

public class Dirt : Block
{
    private DirtUpdater dirtUpdater;
    public Coroutine coroutine;

    public Dirt() : base(BlockType.Dirt, TileType.Dirt, 2, 15)
    {
        coroutine = null;
        dirtUpdater = new GameObject("DirtUpdater").AddComponent<DirtUpdater>();
        //dirtUpdater.StartUpdating(this, 45f, 250f);
    }

    ~Dirt()
    {
        StopUpdating();
    }

    public override bool StartUpdating(bool instant, TerrainGenerator tg, Position chunkPos, Position pillarPos, int height)
    {
        if (instant)
        {
            return TurnToGrass();
        }
        else if (coroutine == null)
        {
            if (this.enBlock == BlockType.Dirt)
            {
                //myCoroutine = StartCoroutine(WaitAndTurnToGrass(45f, 250f));
                coroutine = dirtUpdater.StartUpdating(this, 15f, 25f);
                return true;
            }
            else
            {
                Debug.Log("Block can't be changed to Grass, because block is a type which can't do that");
                return false;
            }
        }
        
        Debug.Log("Can't start coroutine, as coroutine is already running for this block");
        return false;
    }

    public override bool StopUpdating()
    {
        if (coroutine != null)
        {
            dirtUpdater.StopUpdating(coroutine);
            coroutine = null;
            return true;
        }

        return false;
    }

    public bool TurnToGrass()
    {
        if (this.enBlock != BlockType.Dirt)
            return false;
        
        this.enBlock = BlockType.Grass;
        this.tTop.ChangeTile(TileType.GrassTop, 8, 13);
        this.tSide.ChangeTile(TileType.GrassSide, 3, 15);

        if (goBlockMesh != null)
        {
            Debug.Log("goBlockMesh");
            Transform side = goBlockMesh.transform.Find("TopSide");
            if(side != null)
            {
                Debug.Log("TopSide");
                Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(this.tTop.vUVs[0,0], this.tTop.vUVs[0,1]),
                    new Vector2(this.tTop.vUVs[1,0], this.tTop.vUVs[1,1]),
                    new Vector2(this.tTop.vUVs[2,0], this.tTop.vUVs[2,1]),
                    new Vector2(this.tTop.vUVs[3,0], this.tTop.vUVs[3,1]),
                };
                side.gameObject.GetComponent<MeshFilter>().mesh.uv = vectorUV;
            }

            side = goBlockMesh.transform.Find("BackSide");
            if(side != null)
            {
                Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(this.tTop.vUVs[0,0], this.tTop.vUVs[0,1]),
                    new Vector2(this.tTop.vUVs[1,0], this.tTop.vUVs[1,1]),
                    new Vector2(this.tTop.vUVs[2,0], this.tTop.vUVs[2,1]),
                    new Vector2(this.tTop.vUVs[3,0], this.tTop.vUVs[3,1]),
                };
                side.gameObject.GetComponent<MeshFilter>().mesh.uv = vectorUV;
            }

            side = goBlockMesh.transform.Find("FrontSide");
            if(side != null)
            {
                Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(this.tTop.vUVs[0,0], this.tTop.vUVs[0,1]),
                    new Vector2(this.tTop.vUVs[1,0], this.tTop.vUVs[1,1]),
                    new Vector2(this.tTop.vUVs[2,0], this.tTop.vUVs[2,1]),
                    new Vector2(this.tTop.vUVs[3,0], this.tTop.vUVs[3,1]),
                };
                side.gameObject.GetComponent<MeshFilter>().mesh.uv = vectorUV;
            }

            side = goBlockMesh.transform.Find("RightSide");
            if(side != null)
            {
                Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(this.tTop.vUVs[0,0], this.tTop.vUVs[0,1]),
                    new Vector2(this.tTop.vUVs[1,0], this.tTop.vUVs[1,1]),
                    new Vector2(this.tTop.vUVs[2,0], this.tTop.vUVs[2,1]),
                    new Vector2(this.tTop.vUVs[3,0], this.tTop.vUVs[3,1]),
                };
                side.gameObject.GetComponent<MeshFilter>().mesh.uv = vectorUV;
            }

            side = goBlockMesh.transform.Find("LeftSide");
            if(side != null)
            {
                Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(this.tTop.vUVs[0,0], this.tTop.vUVs[0,1]),
                    new Vector2(this.tTop.vUVs[1,0], this.tTop.vUVs[1,1]),
                    new Vector2(this.tTop.vUVs[2,0], this.tTop.vUVs[2,1]),
                    new Vector2(this.tTop.vUVs[3,0], this.tTop.vUVs[3,1]),
                };
                side.gameObject.GetComponent<MeshFilter>().mesh.uv = vectorUV;
            }
        }

        return true;
    }
}
