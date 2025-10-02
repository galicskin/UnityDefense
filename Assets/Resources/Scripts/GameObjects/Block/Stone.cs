using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stone : BlockBase
{
    private void Awake()
    {
        var StonData = MapBlockData.Instance.BlockProperties[0];
        CreatedBlock(StonData);
    }
}
