using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Copper : BlockBase
{
    private void Awake()
    {
        var CopperData = MapBlockData.Instance.BlockProperties[2];
        CreatedBlock(CopperData);
    }
}