using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TerrainModifier : MonoBehaviour
{
    public LayerMask groundLayer;

    public Inventory inventory;

    public static int blocksDestroyed;
    public static int blocksPlaced;

    float maxDist = 4;

    // Start is called before the first frame update
    void Start()
    {
        blocksDestroyed = 0;
        blocksPlaced = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Mouse mouse = Mouse.current;
        bool leftClick = mouse.leftButton.wasPressedThisFrame;
        bool rightClick = mouse.rightButton.wasPressedThisFrame;
        if (leftClick || rightClick)
        {
            RaycastHit hitInfo;
            if(Physics.Raycast(transform.position, transform.forward, out hitInfo, maxDist, groundLayer))
            {
                Vector3 chunk = hitInfo.collider.transform.parent.parent.position; // Get the chunk's position
                Position chunkPos = new Position((int)chunk.x/TerrainChunk.iChunkWidth, (int)chunk.z/TerrainChunk.iChunkLength); // Convert the chunk's position to the position that gets stored in the dictionary
                Vector3 block = hitInfo.collider.transform.parent.position; // Get the block position (-7, 22, 5)
                Position blockPos = new Position((int)block.x, (int)block.z); // Convert to the block's position that is stored in the dictionary
                Vector3 blockLocal = hitInfo.collider.transform.parent.localPosition;

                var pillar = TerrainGenerator.instance.dTerrainChunks[chunkPos].dBlockPillars[blockPos];
                int blockY = (int)block.y;

                float boxWidth = 1.0f;
                float boxHeight = 1.0f;
                float boxDepth = 1.0f;

                if (leftClick)
                {
                    GameObject boxContainer; // Get boxContainer for every neighbour that needs to have it's side created

                    // Render bottom of top neighbour box, if it exists
                    if(pillar.ContainsKey(blockY+1) && pillar[blockY].enBlock != BlockType.Leaves)
                    {
                        // Get the box
                        boxContainer = pillar[blockY+1].goBlockMesh;
                        if (boxContainer == null)
                        {
                            boxContainer = new GameObject("BoxContainer");
                            boxContainer.transform.SetParent(hitInfo.collider.transform.parent.parent);
                        }

                        // Create the bottom side
                        if(pillar[blockY+1].enBlock != BlockType.Water)
                        {
                            // Create the bottom side
                            GameObject bottomSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                            bottomSide.name = "BottomSide";
                            bottomSide.transform.SetParent(boxContainer.transform);
                            bottomSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                            bottomSide.transform.localPosition = new Vector3(0f, -boxHeight / 2f, 0f);
                            bottomSide.transform.forward = Vector3.up;
                    
                            bottomSide.GetComponent<Renderer>().material = Resources.Load<Material>("TerrainMaterial");
                                                                                            Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(pillar[blockY+1].tBottom.vUVs[0,0], pillar[blockY+1].tBottom.vUVs[0,1]),
                    new Vector2(pillar[blockY+1].tBottom.vUVs[1,0], pillar[blockY+1].tBottom.vUVs[1,1]),
                    new Vector2(pillar[blockY+1].tBottom.vUVs[2,0], pillar[blockY+1].tBottom.vUVs[2,1]),
                    new Vector2(pillar[blockY+1].tBottom.vUVs[3,0], pillar[blockY+1].tBottom.vUVs[3,1]),
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
                            bottomSide.transform.localPosition = new Vector3(0f,0f,0f);
                        
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
                    new Vector2(pillar[blockY+1].tBottom.vUVs[0,0], pillar[blockY+1].tBottom.vUVs[0,1]),
                    new Vector2(pillar[blockY+1].tBottom.vUVs[1,0], pillar[blockY+1].tBottom.vUVs[1,1]),
                    new Vector2(pillar[blockY+1].tBottom.vUVs[2,0], pillar[blockY+1].tBottom.vUVs[2,1]),
                    new Vector2(pillar[blockY+1].tBottom.vUVs[3,0], pillar[blockY+1].tBottom.vUVs[3,1]),
                };
                            Vector2[] UVsToTake = vectorUV;
                            for (int i = 0; i < 2; i++) Array.Copy(UVsToTake, 0, UVsToApply, i*UVsToTake.Length, UVsToTake.Length);
                            mesh.SetUVs(0, UVsToApply);
                            mesh.SetTriangles(triangles, 0);
                        
                            // Recalculate the bounds and normals of the mesh
                            mesh.RecalculateBounds();
                            mesh.RecalculateNormals();

                            // Set the rendering mode to "Transparent" or "Fade"
                            Material material = Resources.Load<Material>("TerrainMaterial");

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

                        if (pillar[blockY+1].goBlockMesh == null)
                        {
                            // Place the box in the desired position in the scene
                            Vector3 boxPosition = new Vector3(blockLocal.x, blockLocal.y+boxHeight, blockLocal.z); // Position of the box in world space
                            boxContainer.transform.localPosition = boxPosition;
                            pillar[blockY+1].SetBlockMesh(boxContainer);
                        }
                    }

                    // Render top of bottom neighbour box, if it exists
                    if(pillar.ContainsKey(blockY-1) && pillar[blockY].enBlock != BlockType.Leaves)
                    {
                        // Get the box
                        boxContainer = pillar[blockY-1].goBlockMesh;
                        if (boxContainer == null)
                        {
                            boxContainer = new GameObject("BoxContainer");
                            boxContainer.transform.SetParent(hitInfo.collider.transform.parent.parent);
                        }

                        // Create the top side
                        if(pillar[blockY-1].enBlock != BlockType.Water)
                        {
                            GameObject topSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                            topSide.name = "TopSide";
                            topSide.transform.SetParent(boxContainer.transform);
                            topSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                            topSide.transform.localPosition = new Vector3(0f, boxHeight / 2f, 0f);
                            topSide.transform.forward = Vector3.down;

                            topSide.GetComponent<Renderer>().material = Resources.Load<Material>("TerrainMaterial");
                                                                                            Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(pillar[blockY-1].tTop.vUVs[0,0], pillar[blockY-1].tTop.vUVs[0,1]),
                    new Vector2(pillar[blockY-1].tTop.vUVs[1,0], pillar[blockY-1].tTop.vUVs[1,1]),
                    new Vector2(pillar[blockY-1].tTop.vUVs[2,0], pillar[blockY-1].tTop.vUVs[2,1]),
                    new Vector2(pillar[blockY-1].tTop.vUVs[3,0], pillar[blockY-1].tTop.vUVs[3,1]),
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
                            topSide.transform.localPosition = new Vector3(0f,0f,0f);
                        
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
                    new Vector2(pillar[blockY-1].tTop.vUVs[0,0], pillar[blockY-1].tTop.vUVs[0,1]),
                    new Vector2(pillar[blockY-1].tTop.vUVs[1,0], pillar[blockY-1].tTop.vUVs[1,1]),
                    new Vector2(pillar[blockY-1].tTop.vUVs[2,0], pillar[blockY-1].tTop.vUVs[2,1]),
                    new Vector2(pillar[blockY-1].tTop.vUVs[3,0], pillar[blockY-1].tTop.vUVs[3,1]),
                };
                            Vector2[] UVsToTake = vectorUV;
                            for (int i = 0; i < 2; i++) Array.Copy(UVsToTake, 0, UVsToApply, i*UVsToTake.Length, UVsToTake.Length);
                            mesh.SetUVs(0, UVsToApply);
                            mesh.SetTriangles(triangles, 0);
                        
                            // Recalculate the bounds and normals of the mesh
                            mesh.RecalculateBounds();
                            mesh.RecalculateNormals();

                            // Set the rendering mode to "Transparent" or "Fade"
                            Material material = Resources.Load<Material>("TerrainMaterial");

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

                        if (pillar[blockY-1].goBlockMesh == null)
                        {
                            // Place the box in the desired position in the scene
                            Vector3 boxPosition = new Vector3(blockLocal.x, blockLocal.y-boxHeight, blockLocal.z); // Position of the box in world space
                            boxContainer.transform.localPosition = boxPosition;
                            pillar[blockY-1].SetBlockMesh(boxContainer);
                        }
                    }

                    // front box might be in neighbour chunk
                    Position chunkPos2 = new Position(chunkPos.X, blockLocal.z-1 == -8 ? chunkPos.Z-1 : chunkPos.Z);
                    Position blockPos2 = new Position(blockPos.X, blockPos.Z-1);

                    var BlockDictionary = TerrainGenerator.instance.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];
                    // Render back of front neighbour box, if it exists
                    if(BlockDictionary.ContainsKey(blockY) && pillar[blockY].enBlock != BlockType.Leaves)
                    {
                        // Get the box
                        boxContainer = BlockDictionary[blockY].goBlockMesh;
                        if (boxContainer == null)
                        {
                            boxContainer = new GameObject("BoxContainer");
                            boxContainer.transform.SetParent(TerrainGenerator.instance.dTerrainChunks[chunkPos2].transform);
                        }

                        // Create the back side
                        if(BlockDictionary[blockY].enBlock != BlockType.Water)
                        {
                            GameObject backSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                            backSide.name = "BackSide";
                            backSide.transform.SetParent(boxContainer.transform);
                            backSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                            backSide.transform.localPosition = new Vector3(0f, 0f, boxDepth / 2f);
                            backSide.transform.forward = Vector3.back;

                            backSide.GetComponent<Renderer>().material = Resources.Load<Material>("TerrainMaterial");
                                                                                            Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[0,0], BlockDictionary[blockY].tSide.vUVs[0,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[1,0], BlockDictionary[blockY].tSide.vUVs[1,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[2,0], BlockDictionary[blockY].tSide.vUVs[2,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[3,0], BlockDictionary[blockY].tSide.vUVs[3,1]),
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
                            backSide.transform.localPosition = new Vector3(0f,0f,0f);
                        
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
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[0,0], BlockDictionary[blockY].tSide.vUVs[0,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[1,0], BlockDictionary[blockY].tSide.vUVs[1,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[2,0], BlockDictionary[blockY].tSide.vUVs[2,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[3,0], BlockDictionary[blockY].tSide.vUVs[3,1]),
                };
                            Vector2[] UVsToTake = vectorUV;
                            for (int i = 0; i < 2; i++) Array.Copy(UVsToTake, 0, UVsToApply, i*UVsToTake.Length, UVsToTake.Length);
                            mesh.SetUVs(0, UVsToApply);
                            mesh.SetTriangles(triangles, 0);
                        
                            // Recalculate the bounds and normals of the mesh
                            mesh.RecalculateBounds();
                            mesh.RecalculateNormals();

                            // Set the rendering mode to "Transparent" or "Fade"
                            Material material = Resources.Load<Material>("TerrainMaterial");

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

                        if (BlockDictionary[blockY].goBlockMesh == null)
                        {
                            // Place the box in the desired position in the scene
                            Vector3 boxPosition = new Vector3(blockPos2.X, blockY, blockPos2.Z); // Position of the box in world space
                            boxContainer.transform.position = boxPosition;
                            BlockDictionary[blockY].SetBlockMesh(boxContainer);
                        }
                    }

                    // right box might be in neighbour chunk
                    chunkPos2 = new Position(blockLocal.x+1 == 9 ? chunkPos.X+1 : chunkPos.X, chunkPos.Z);
                    blockPos2 = new Position(blockPos.X+1, blockPos.Z);

                    BlockDictionary = TerrainGenerator.instance.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];
                    // Render left of right neighbour box, if it exists
                    if(BlockDictionary.ContainsKey(blockY) && pillar[blockY].enBlock != BlockType.Leaves)
                    {
                        // Get the box
                        boxContainer = BlockDictionary[blockY].goBlockMesh;
                        if (boxContainer == null)
                        {
                            boxContainer = new GameObject("BoxContainer");
                            boxContainer.transform.SetParent(TerrainGenerator.instance.dTerrainChunks[chunkPos2].transform);
                        }

                        // Create the left side
                        if(BlockDictionary[blockY].enBlock != BlockType.Water)
                        {
                            GameObject leftSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                            leftSide.name = "LeftSide";
                            leftSide.transform.SetParent(boxContainer.transform);
                            leftSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                            leftSide.transform.localPosition = new Vector3(-boxWidth / 2f, 0f, 0f);
                            leftSide.transform.forward = Vector3.right;

                            leftSide.GetComponent<Renderer>().material = Resources.Load<Material>("TerrainMaterial");
                                                Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[0,0], BlockDictionary[blockY].tSide.vUVs[0,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[1,0], BlockDictionary[blockY].tSide.vUVs[1,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[2,0], BlockDictionary[blockY].tSide.vUVs[2,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[3,0], BlockDictionary[blockY].tSide.vUVs[3,1]),
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
                            leftSide.transform.localPosition = new Vector3(0f,0f,0f);
                        
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
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[0,0], BlockDictionary[blockY].tSide.vUVs[0,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[1,0], BlockDictionary[blockY].tSide.vUVs[1,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[2,0], BlockDictionary[blockY].tSide.vUVs[2,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[3,0], BlockDictionary[blockY].tSide.vUVs[3,1]),
                };
                            Vector2[] UVsToTake = vectorUV;
                            for (int i = 0; i < 2; i++) Array.Copy(UVsToTake, 0, UVsToApply, i*UVsToTake.Length, UVsToTake.Length);
                            mesh.SetUVs(0, UVsToApply);
                            mesh.SetTriangles(triangles, 0);
                        
                            // Recalculate the bounds and normals of the mesh
                            mesh.RecalculateBounds();
                            mesh.RecalculateNormals();

                            // Set the rendering mode to "Transparent" or "Fade"
                            Material material = Resources.Load<Material>("TerrainMaterial");

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

                        if (BlockDictionary[blockY].goBlockMesh == null)
                        {
                            // Place the box in the desired position in the scene
                            Vector3 boxPosition = new Vector3(blockPos2.X, blockY, blockPos2.Z); // Position of the box in world space
                            boxContainer.transform.position = boxPosition;
                            BlockDictionary[blockY].SetBlockMesh(boxContainer);
                        }
                    }

                    // back box might be in neighbour chunk
                    chunkPos2 = new Position(chunkPos.X, blockLocal.z+1 == 9 ? chunkPos.Z+1 : chunkPos.Z);
                    blockPos2 = new Position(blockPos.X, blockPos.Z+1);

                    BlockDictionary = TerrainGenerator.instance.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];
                    // Render front of back neighbour box, if it exists
                    if(BlockDictionary.ContainsKey(blockY) && pillar[blockY].enBlock != BlockType.Leaves)
                    {
                        // Get the box
                        boxContainer = BlockDictionary[blockY].goBlockMesh;
                        if (boxContainer == null)
                        {
                            boxContainer = new GameObject("BoxContainer");
                            boxContainer.transform.SetParent(TerrainGenerator.instance.dTerrainChunks[chunkPos2].transform);
                        }

                        // Create the front side
                        if(BlockDictionary[blockY].enBlock != BlockType.Water)
                        {
                            GameObject frontSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                            frontSide.name = "FrontSide";
                            frontSide.transform.SetParent(boxContainer.transform);
                            frontSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                            frontSide.transform.localPosition = new Vector3(0f, 0f, -boxDepth / 2f);
                            frontSide.transform.forward = Vector3.forward;

                            frontSide.GetComponent<Renderer>().material = Resources.Load<Material>("TerrainMaterial");
                                                                            Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[0,0], BlockDictionary[blockY].tSide.vUVs[0,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[1,0], BlockDictionary[blockY].tSide.vUVs[1,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[2,0], BlockDictionary[blockY].tSide.vUVs[2,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[3,0], BlockDictionary[blockY].tSide.vUVs[3,1]),
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
                            frontSide.transform.localPosition = new Vector3(0f,0f,0f);
                        
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
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[0,0], BlockDictionary[blockY].tSide.vUVs[0,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[1,0], BlockDictionary[blockY].tSide.vUVs[1,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[2,0], BlockDictionary[blockY].tSide.vUVs[2,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[3,0], BlockDictionary[blockY].tSide.vUVs[3,1]),
                };
                            Vector2[] UVsToTake = vectorUV;
                            for (int i = 0; i < 2; i++) Array.Copy(UVsToTake, 0, UVsToApply, i*UVsToTake.Length, UVsToTake.Length);
                            mesh.SetUVs(0, UVsToApply);
                            mesh.SetTriangles(triangles, 0);
                        
                            // Recalculate the bounds and normals of the mesh
                            mesh.RecalculateBounds();
                            mesh.RecalculateNormals();

                            // Set the rendering mode to "Transparent" or "Fade"
                            Material material = Resources.Load<Material>("TerrainMaterial");

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

                        if (BlockDictionary[blockY].goBlockMesh == null)
                        {
                            // Place the box in the desired position in the scene
                            Vector3 boxPosition = new Vector3(blockPos2.X, blockY, blockPos2.Z); // Position of the box in world space
                            boxContainer.transform.position = boxPosition;
                            BlockDictionary[blockY].SetBlockMesh(boxContainer);
                        }
                    }

                    // left box might be in neighbour chunk
                    chunkPos2 = new Position(blockLocal.x-1 == -8 ? chunkPos.X-1 : chunkPos.X, chunkPos.Z);
                    blockPos2 = new Position(blockPos.X-1, blockPos.Z);

                    BlockDictionary = TerrainGenerator.instance.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];
                    // Render right of left neighbour box, if it exists
                    if(BlockDictionary.ContainsKey(blockY) && pillar[blockY].enBlock != BlockType.Leaves)
                    {
                        // Get the box
                        boxContainer = BlockDictionary[blockY].goBlockMesh;
                        if (boxContainer == null)
                        {
                            boxContainer = new GameObject("BoxContainer");
                            boxContainer.transform.SetParent(TerrainGenerator.instance.dTerrainChunks[chunkPos2].transform);
                        }

                        // Create the right side
                        if(BlockDictionary[blockY].enBlock != BlockType.Water)
                        {
                            GameObject rightSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                            rightSide.name = "RightSide";
                            rightSide.transform.SetParent(boxContainer.transform);
                            rightSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                            rightSide.transform.localPosition = new Vector3(boxWidth / 2f, 0f, 0f);
                            rightSide.transform.forward = Vector3.left;

                            rightSide.GetComponent<Renderer>().material = Resources.Load<Material>("TerrainMaterial");
                                            Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[0,0], BlockDictionary[blockY].tSide.vUVs[0,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[1,0], BlockDictionary[blockY].tSide.vUVs[1,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[2,0], BlockDictionary[blockY].tSide.vUVs[2,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[3,0], BlockDictionary[blockY].tSide.vUVs[3,1]),
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
                            rightSide.transform.localPosition = new Vector3(0f,0f,0f);
                        
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
                                0, 2, 1,     // Top triangle (visible from the right side)
                                2, 3, 1,     // Bottom triangle (visible from the right side)

                                5, 6, 4,     // Top triangle (visible from the left side)
                                7, 6, 5      // Bottom triangle (visible from the left side)
                            };

                            // Assign the vertices, UV coordinates, and triangles to the mesh
                            mesh.SetVertices(vertices);
                            Vector2[] UVsToApply = new Vector2[8];
                                                                        Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[0,0], BlockDictionary[blockY].tSide.vUVs[0,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[1,0], BlockDictionary[blockY].tSide.vUVs[1,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[2,0], BlockDictionary[blockY].tSide.vUVs[2,1]),
                    new Vector2(BlockDictionary[blockY].tSide.vUVs[3,0], BlockDictionary[blockY].tSide.vUVs[3,1]),
                };
                            Vector2[] UVsToTake = vectorUV;
                            for (int i = 0; i < 2; i++) Array.Copy(UVsToTake, 0, UVsToApply, i*UVsToTake.Length, UVsToTake.Length);
                            mesh.SetUVs(0, UVsToApply);
                            mesh.SetTriangles(triangles, 0);
                        
                            // Recalculate the bounds and normals of the mesh
                            mesh.RecalculateBounds();
                            mesh.RecalculateNormals();

                            // Set the rendering mode to "Transparent" or "Fade"
                            Material material = Resources.Load<Material>("TerrainMaterial");

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

                        if (BlockDictionary[blockY].goBlockMesh == null)
                        {
                            // Place the box in the desired position in the scene
                            Vector3 boxPosition = new Vector3(blockPos2.X, blockY, blockPos2.Z); // Position of the box in world space
                            boxContainer.transform.position = boxPosition;
                            BlockDictionary[blockY].SetBlockMesh(boxContainer);
                        }
                    }

                    inventory.AddToInventory(pillar[blockY].enBlock);
                    Destroy(hitInfo.collider.transform.parent.gameObject); // Remove object from scene... the mesh
                    pillar.Remove(blockY); // Remove object from dictionary
                    blocksDestroyed++;
                }
                else if(rightClick && inventory.CanPlaceCur())
                {
                    // Reference position of where box should be put IFFF its in neighbour chunk
                    Vector3 pointInTargetBlock = hitInfo.point - transform.forward * .02f;
                    Position chunkPos2 = new Position(Mathf.RoundToInt(pointInTargetBlock.x / 16f), Mathf.RoundToInt(pointInTargetBlock.z / 16f));
                    Position blockPos2 = new Position(Mathf.RoundToInt(pointInTargetBlock.x), Mathf.RoundToInt(pointInTargetBlock.z));
                    int y = Mathf.RoundToInt(pointInTargetBlock.y);

                    Debug.Log("ChunkPos: x " + chunkPos2.X + " z " + chunkPos2.Z);
                    Debug.Log("BlockPos: x " + blockPos2.X + " z " + blockPos2.Z + " y " + y);
                    //Debug.Log("pointInTargetBlock: x " + pointInTargetBlock.x + " y " + pointInTargetBlock.y + " z " + pointInTargetBlock.z);

                    var BlockDictionary = TerrainGenerator.instance.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];

                    // Reference new box
                    GameObject boxContainer = new GameObject("BoxContainer");
                    boxContainer.transform.SetParent(TerrainGenerator.instance.dTerrainChunks[chunkPos2].transform);

                    // Create new block at position in ordered dictionary
                    int i;
                    for (i = BlockDictionary.Count-1; BlockDictionary.GetKey(i)>y; i--){}
                    BlockDictionary.Insert(i+1, y, inventory.GetCurBlock());

                    // Get which side the box should be place on
                    /*if (hitInfo.collider.name == "TopSide")
                        blockY += 1;
                    else if (hitInfo.collider.name == "BottomSide")
                        blockY -= 1;
                    else if (hitInfo.collider.name == "FrontSide")
                    {
                        chunkPos2 = new Position(chunkPos.X, blockLocal.z-1 == -8 ? chunkPos.Z-1 : chunkPos.Z);
                        blockPos2 = new Position(blockPos.X, blockPos.Z-1);
                        BlockDictionary = TerrainGenerator.instance.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];
                    }
                    else if (hitInfo.collider.name == "BackSide")
                    {
                        chunkPos2 = new Position(chunkPos.X, blockLocal.z+1 == 9 ? chunkPos.Z+1 : chunkPos.Z);
                        blockPos2 = new Position(blockPos.X, blockPos.Z+1);
                        BlockDictionary = TerrainGenerator.instance.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];
                    }
                    else if (hitInfo.collider.name == "LeftSide")
                    {
                        chunkPos2 = new Position(blockLocal.x-1 == -8 ? chunkPos.X-1 : chunkPos.X, chunkPos.Z);
                        blockPos2 = new Position(blockPos.X-1, blockPos.Z);
                        BlockDictionary = TerrainGenerator.instance.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];
                    }
                    else if (hitInfo.collider.name == "RightSide")
                    {
                        chunkPos2 = new Position(blockLocal.x+1 == 9 ? chunkPos.X+1 : chunkPos.X, chunkPos.Z);
                        blockPos2 = new Position(blockPos.X+1, blockPos.Z);
                        BlockDictionary = TerrainGenerator.instance.dTerrainChunks[chunkPos2].dBlockPillars[blockPos2];
                    }*/

                    // Render top of the box
                    if(!BlockDictionary.ContainsKey(y+1) || BlockDictionary[y].enBlock == BlockType.Leaves)
                    {
                        GameObject topSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        topSide.name = "TopSide";
                        topSide.transform.SetParent(boxContainer.transform);
                        topSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                        topSide.transform.localPosition = new Vector3(0f, boxHeight / 2f, 0f);
                        topSide.transform.forward = Vector3.down;

                        topSide.GetComponent<Renderer>().material = Resources.Load<Material>("TerrainMaterial");
                                                                                                Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(BlockDictionary[y].tTop.vUVs[0,0], BlockDictionary[y].tTop.vUVs[0,1]),
                    new Vector2(BlockDictionary[y].tTop.vUVs[1,0], BlockDictionary[y].tTop.vUVs[1,1]),
                    new Vector2(BlockDictionary[y].tTop.vUVs[2,0], BlockDictionary[y].tTop.vUVs[2,1]),
                    new Vector2(BlockDictionary[y].tTop.vUVs[3,0], BlockDictionary[y].tTop.vUVs[3,1]),
                };
                        topSide.GetComponent<MeshFilter>().mesh.SetUVs(0, vectorUV);
                    }
                    else Destroy(BlockDictionary[y+1].goBlockMesh.transform.Find("BottomSide").gameObject);

                    // Render bottom of the box
                    if(!BlockDictionary.ContainsKey(y-1) || BlockDictionary[y].enBlock == BlockType.Leaves)
                    {
                        // Create the bottom side
                        GameObject bottomSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        bottomSide.name = "BottomSide";
                        bottomSide.transform.SetParent(boxContainer.transform);
                        bottomSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                        bottomSide.transform.localPosition = new Vector3(0f, -boxHeight / 2f, 0f);
                        bottomSide.transform.forward = Vector3.up;
                    
                        bottomSide.GetComponent<Renderer>().material = Resources.Load<Material>("TerrainMaterial");
                        Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(BlockDictionary[y].tBottom.vUVs[0,0], BlockDictionary[y].tBottom.vUVs[0,1]),
                    new Vector2(BlockDictionary[y].tBottom.vUVs[1,0], BlockDictionary[y].tBottom.vUVs[1,1]),
                    new Vector2(BlockDictionary[y].tBottom.vUVs[2,0], BlockDictionary[y].tBottom.vUVs[2,1]),
                    new Vector2(BlockDictionary[y].tBottom.vUVs[3,0], BlockDictionary[y].tBottom.vUVs[3,1]),
                };
                        bottomSide.GetComponent<MeshFilter>().mesh.uv = vectorUV;
                    }
                    else Destroy(BlockDictionary[y-1].goBlockMesh.transform.Find("TopSide").gameObject);

                    //front, but the front one might be in neighbour chunk...
                    Position chunkPos3 = new Position(chunkPos2.X, Mathf.FloorToInt((blockPos2.Z-1 + (TerrainChunk.iChunkLength >> 1) - ((TerrainChunk.iChunkLength + 1) & 1)) / (float)TerrainChunk.iChunkLength));
                    Position blockPos3 = new Position(blockPos2.X, blockPos2.Z-1);
                    var BlockDictionary2 = TerrainGenerator.instance.dTerrainChunks[chunkPos3].dBlockPillars[blockPos3];

                    if(!BlockDictionary2.ContainsKey(y) || BlockDictionary[y].enBlock == BlockType.Leaves)
                    {
                        // Create the front side
                        GameObject frontSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        frontSide.name = "FrontSide";
                        frontSide.transform.SetParent(boxContainer.transform);
                        frontSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                        frontSide.transform.localPosition = new Vector3(0f, 0f, -boxDepth / 2f);
                        frontSide.transform.forward = Vector3.forward;

                        frontSide.GetComponent<Renderer>().material = Resources.Load<Material>("TerrainMaterial");
                                                Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(BlockDictionary[y].tSide.vUVs[0,0], BlockDictionary[y].tSide.vUVs[0,1]),
                    new Vector2(BlockDictionary[y].tSide.vUVs[1,0], BlockDictionary[y].tSide.vUVs[1,1]),
                    new Vector2(BlockDictionary[y].tSide.vUVs[2,0], BlockDictionary[y].tSide.vUVs[2,1]),
                    new Vector2(BlockDictionary[y].tSide.vUVs[3,0], BlockDictionary[y].tSide.vUVs[3,1]),
                };
                        frontSide.GetComponent<MeshFilter>().mesh.uv = vectorUV;
                    }
                    else Destroy(BlockDictionary2[y].goBlockMesh.transform.Find("BackSide").gameObject);

                    //right, same as front
                    chunkPos3 = new Position(Mathf.FloorToInt((blockPos2.X+1 + (TerrainChunk.iChunkWidth >> 1) - ((TerrainChunk.iChunkWidth + 1) & 1)) / (float)TerrainChunk.iChunkWidth), chunkPos2.Z);
                    blockPos3 = new Position(blockPos2.X+1, blockPos2.Z);
                    BlockDictionary2 = TerrainGenerator.instance.dTerrainChunks[chunkPos3].dBlockPillars[blockPos3];
                
                    if(!BlockDictionary2.ContainsKey(y) || BlockDictionary[y].enBlock == BlockType.Leaves)
                    {
                        // Create the right side
                        GameObject rightSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        rightSide.name = "RightSide";
                        rightSide.transform.SetParent(boxContainer.transform);
                        rightSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                        rightSide.transform.localPosition = new Vector3(boxWidth / 2f, 0f, 0f);
                        rightSide.transform.forward = Vector3.left;

                        rightSide.GetComponent<Renderer>().material = Resources.Load<Material>("TerrainMaterial");
                                                Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(BlockDictionary[y].tSide.vUVs[0,0], BlockDictionary[y].tSide.vUVs[0,1]),
                    new Vector2(BlockDictionary[y].tSide.vUVs[1,0], BlockDictionary[y].tSide.vUVs[1,1]),
                    new Vector2(BlockDictionary[y].tSide.vUVs[2,0], BlockDictionary[y].tSide.vUVs[2,1]),
                    new Vector2(BlockDictionary[y].tSide.vUVs[3,0], BlockDictionary[y].tSide.vUVs[3,1]),
                };
                        rightSide.GetComponent<MeshFilter>().mesh.uv = vectorUV;
                    }
                    else Destroy(BlockDictionary2[y].goBlockMesh.transform.Find("LeftSide").gameObject);

                    //back, same as right
                    chunkPos3 = new Position(chunkPos2.X, Mathf.FloorToInt((blockPos2.Z+1 + (TerrainChunk.iChunkLength >> 1) - ((TerrainChunk.iChunkLength + 1) & 1)) / (float)TerrainChunk.iChunkLength));
                    blockPos3 = new Position(blockPos2.X, blockPos2.Z+1);
                    BlockDictionary2 = TerrainGenerator.instance.dTerrainChunks[chunkPos3].dBlockPillars[blockPos3];
                
                    if(!BlockDictionary2.ContainsKey(y) || BlockDictionary[y].enBlock == BlockType.Leaves)
                    {
                        // Create the back side
                        GameObject backSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        backSide.name = "BackSide";
                        backSide.transform.SetParent(boxContainer.transform);
                        backSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                        backSide.transform.localPosition = new Vector3(0f, 0f, boxDepth / 2f);
                        backSide.transform.forward = Vector3.back;

                        backSide.GetComponent<Renderer>().material = Resources.Load<Material>("TerrainMaterial");
                                                                        Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(BlockDictionary[y].tSide.vUVs[0,0], BlockDictionary[y].tSide.vUVs[0,1]),
                    new Vector2(BlockDictionary[y].tSide.vUVs[1,0], BlockDictionary[y].tSide.vUVs[1,1]),
                    new Vector2(BlockDictionary[y].tSide.vUVs[2,0], BlockDictionary[y].tSide.vUVs[2,1]),
                    new Vector2(BlockDictionary[y].tSide.vUVs[3,0], BlockDictionary[y].tSide.vUVs[3,1]),
                };
                        backSide.GetComponent<MeshFilter>().mesh.uv = vectorUV;
                    }
                    else Destroy(BlockDictionary2[y].goBlockMesh.transform.Find("FrontSide").gameObject);

                    //left, same as back
                    chunkPos3 = new Position(Mathf.FloorToInt((blockPos2.X-1 + (TerrainChunk.iChunkWidth >> 1) - ((TerrainChunk.iChunkWidth + 1) & 1)) / (float)TerrainChunk.iChunkWidth), chunkPos2.Z);
                    blockPos3 = new Position(blockPos2.X-1, blockPos2.Z);
                    BlockDictionary2 = TerrainGenerator.instance.dTerrainChunks[chunkPos3].dBlockPillars[blockPos3];
                
                    if(!BlockDictionary2.ContainsKey(y) || BlockDictionary[y].enBlock == BlockType.Leaves)
                    {
                        // Create the left side
                        GameObject leftSide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        leftSide.name = "LeftSide";
                        leftSide.transform.SetParent(boxContainer.transform);
                        leftSide.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);
                        leftSide.transform.localPosition = new Vector3(-boxWidth / 2f, 0f, 0f);
                        leftSide.transform.forward = Vector3.right;

                        leftSide.GetComponent<Renderer>().material = Resources.Load<Material>("TerrainMaterial");
                                                                                                Vector2[] vectorUV = new Vector2[]
                {
                    new Vector2(BlockDictionary[y].tSide.vUVs[0,0], BlockDictionary[y].tSide.vUVs[0,1]),
                    new Vector2(BlockDictionary[y].tSide.vUVs[1,0], BlockDictionary[y].tSide.vUVs[1,1]),
                    new Vector2(BlockDictionary[y].tSide.vUVs[2,0], BlockDictionary[y].tSide.vUVs[2,1]),
                    new Vector2(BlockDictionary[y].tSide.vUVs[3,0], BlockDictionary[y].tSide.vUVs[3,1]),
                };
                        leftSide.GetComponent<MeshFilter>().mesh.uv = vectorUV;
                    }
                    else Destroy(BlockDictionary2[y].goBlockMesh.transform.Find("RightSide").gameObject);

                    // Place the box in the desired position in the scene
                    Vector3 boxPosition = new Vector3(blockPos2.X*boxWidth, y*boxHeight, blockPos2.Z*boxDepth); // Position of the box in world space
                    boxContainer.transform.position = boxPosition;
                    BlockDictionary[y].SetBlockMesh(boxContainer);
                    inventory.ReduceCur();
                    blocksPlaced++;
                    //boxContainer.AddComponent<Outline>();
                    //boxContainer.GetComponent<Outline>().enabled = false;*/
                }
            }
        }
    }
}