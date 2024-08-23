using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayNumberOfBlocksTaken : MonoBehaviour
{
    private static int blocksDestroyed = -666;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (TerrainModifier.blocksDestroyed != blocksDestroyed && TerrainModifier.blocksDestroyed < 50)
        {
            blocksDestroyed = TerrainModifier.blocksDestroyed;
            var editor = GetComponent<Text>();
            editor.text = "Blocks destroyed: " + blocksDestroyed + "/50";
        }
    }
}
