using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayNumberOfBlocksPlaced : MonoBehaviour
{
    private static int blocksPlaced = -666;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (TerrainModifier.blocksPlaced != blocksPlaced && TerrainModifier.blocksPlaced < 50)
        {
            blocksPlaced = TerrainModifier.blocksPlaced;
            var editor = GetComponent<Text>();
            editor.text = "Blocks placed: " + blocksPlaced + "/50";
        }
    }
}
