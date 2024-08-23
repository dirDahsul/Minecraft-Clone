using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
// Lets register all types of tiles via enum, so we can call or check a tyle when needed
public enum TileType { Dirt, GrassTop, GrassSide, Cobblestone, TreeTB, TreeSide, Leaves };

// Since we use 1 texture for all tyle types and every tile is the size 16x16 pixels (from the texture),
// let's use this class to identify the tyle we need to get
public class Tile
{
    // This stores the tyle's positions in the texture (to make sure we get the part of the texture we need)
    private readonly Vector2[] vUVs;
    private readonly TileType enTyle;

    public Tile(TileType t, int x, int y)
    {
        enTyle = t;
        vUVs = new Vector2[]
        {
            // Move by 16f, cuz the tyle sizes in the texture are 16x16 pixels wide
            new Vector2(x/16f + .001f, y/16f + .001f),
            new Vector2(x/16f+ .001f, (y+1)/16f - .001f),
            new Vector2((x+1)/16f - .001f, (y+1)/16f - .001f),
            new Vector2((x+1)/16f - .001f, y/16f+ .001f),
        };
    }

    public Vector2[] getUVs()
    {
        return vUVs;
    }

    public TileType getTyle()
    {
        return enTyle;
    }
}
*/