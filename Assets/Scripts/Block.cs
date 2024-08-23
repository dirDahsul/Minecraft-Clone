using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

// Lets register all types of tiles via enum, so we can call or check a tile when needed
public enum TileType { Dirt, GrassTop, GrassSide, Cobblestone, TreeTB, TreeSide, Leaves, Water };

// Lets register all types of blocks via enum, so we can call or check a block when needed
public enum BlockType { Dirt, Grass, Stone, Trunk, Leaves, Water };

// The world is build from blocks, which are a bunch of tiles, which are used to create meshes
public class Block// : MonoBehaviour
{
    // Since we use 1 texture for all tile types and every tile is the size 16x16 pixels (from the texture),
    // let's use this class to identify the tile we need to get
    public class Tile
    {
        // Texture size in pixels
        public const uint uiTextureSize = 1024;

        public const uint uiTextureUVSize = 64;

        // This stores the tile's positions in the texture (to make sure we get the part of the texture we need)
        public float[,] vUVs { get; private set; }
        // The tile's type ........... not necessary for this build of the game, but usable in future development
        public TileType enTile { get; private set; }

        internal Tile(TileType t, uint x, uint y)
        {
            ChangeTile(t, x, y);
        }

        public TileType ChangeTile(TileType t, uint x, uint y)
        {
            enTile = t;
            float fTextureSize = Convert.ToSingle(uiTextureSize);
            vUVs = new float[,] // 9, 20
            {
                // Move by uiTextureSize, cuz the tile sizes in the texture are uiTextureSizexuiTextureSize pixels wide
                {(x+1)*uiTextureUVSize/fTextureSize - .001f, y*uiTextureUVSize/fTextureSize + .001f}, // 4
                {x*uiTextureUVSize/fTextureSize + .001f, y*uiTextureUVSize/fTextureSize + .001f}, // 1
                {(x+1)*uiTextureUVSize/fTextureSize - .001f, (y+1)*uiTextureUVSize/fTextureSize - .001f}, // 3
                {x*uiTextureUVSize/fTextureSize + .001f, (y+1)*uiTextureUVSize/fTextureSize - .001f}, // 2

                //new Vector2((x+1)*uiTextureUVSize/fTextureSize - .001f, y*uiTextureUVSize/fTextureSize + .001f), // 4
                //new Vector2(x*uiTextureUVSize/fTextureSize + .001f, y*uiTextureUVSize/fTextureSize + .001f), // 1
                //new Vector2((x+1)*uiTextureUVSize/fTextureSize - .001f, (y+1)*uiTextureUVSize/fTextureSize - .001f), // 3
                //new Vector2(x*uiTextureUVSize/fTextureSize + .001f, (y+1)*uiTextureUVSize/fTextureSize - .001f), // 2
            };

            return enTile;
        }
    }

    public virtual bool StartUpdating(bool instant, TerrainGenerator tg, Position chunkPos, Position pillarPos, int height) { return false; }
    public virtual bool StopUpdating() { return false; }

    // Store all tiles that make up a block
    public Tile tTop { get; } // only get, cuz we want it to be readonly
    public Tile tBottom { get; }
    public Tile tSide { get; }
    // Store block type ....... not usable in current build if the game, but usable in fugure development
    public BlockType enBlock { get; protected set; }

    public GameObject goBlockMesh { get; private set; }

    public Block(BlockType b, TileType t, uint x, uint y)
    {
        goBlockMesh = null;
        enBlock = b;
        tTop = new Tile(t, x, y);
        tBottom = new Tile(t, x, y);
        tSide = new Tile(t, x, y);
    }

    public Block(BlockType b, TileType t1, uint x1, uint y1, TileType t2, uint x2, uint y2)
    {
        goBlockMesh = null;
        enBlock = b;
        tTop = new Tile(t1, x1, y1);
        tBottom = new Tile(t1, x1, y1);
        tSide = new Tile(t2, x2, y2);
    }

    public Block(BlockType b, TileType t1, uint x1, uint y1, TileType t2, uint x2, uint y2, TileType t3, uint x3, uint y3)
    {
        goBlockMesh = null;
        enBlock = b;
        tTop = new Tile(t1, x1, y1);
        tBottom = new Tile(t2, x2, y2);
        tSide = new Tile(t3, x3, y3);
    }

    public BlockType ChangeBlock(BlockType b)
    {
        enBlock = b;
        return enBlock;
    }

    public void SetBlockMesh(GameObject bm)
    {
        goBlockMesh = bm;
    }
}
