using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    int[] matCounts = new int[] { 0, 0, 0, 0 };

    public BlockType[] matTypes;
    public Image[] invImgs;
    public Image[] matImgs;

    int curMat;

    // Start is called before the first frame update
    void Start()
    {
        foreach(Image img in matImgs)
        {
            img.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SetCur(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SetCur(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SetCur(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            SetCur(3);
    }

    void SetCur(int i)
    {
        invImgs[curMat].color = new Color(0, 0, 0, 43/255f);

        curMat = i;
        invImgs[i].color = new Color(0, 0, 0, 80/255f);
    }

    public bool CanPlaceCur()
    {
        return matCounts[curMat] > 0;
    }

    public Block GetCurBlock()
    {
        //return matTypes[curMat];
        switch (matTypes[curMat])
        {
            case BlockType.Dirt:
            {
                return new Dirt();
            }
            case BlockType.Stone:
            {
                return new Stone();
            }
            case BlockType.Trunk:
            {
                return new Log();
            }
            case BlockType.Leaves:
            {
                return new Leave();
            }
            default:
            {
                return new Dirt();
            }
        }
    }

    public void ReduceCur()
    {
        matCounts[curMat]--;

        if(matCounts[curMat] == 0)
            matImgs[curMat].gameObject.SetActive(false);
    }

    public void AddToInventory(BlockType block)
    {
        int i = 0;
        if(block == BlockType.Stone)
            i = 1;
        else if(block == BlockType.Trunk)
            i = 2;
        else if(block == BlockType.Leaves)
            i = 3;

        matCounts[i]++;
        if(matCounts[i] == 1)
            matImgs[i].gameObject.SetActive(true);

    }
}