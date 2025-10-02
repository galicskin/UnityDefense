using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Iron : BlockBase
{
    private void Awake()
    {
        var IronData = MapBlockData.Instance.BlockProperties[1];
        CreatedBlock(IronData);
    }
}