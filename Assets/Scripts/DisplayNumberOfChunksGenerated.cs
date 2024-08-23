using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayNumberOfChunksGenerated : MonoBehaviour
{
    private static int chunksGenerated = -666;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (TerrainGenerator.chunksGenerated != chunksGenerated && TerrainGenerator.chunksGenerated < 100)
        {
            chunksGenerated = TerrainGenerator.chunksGenerated;
            var editor = GetComponent<Text>();
            editor.text = "Explore the world: " + chunksGenerated + "/100";
        }
    }
}
