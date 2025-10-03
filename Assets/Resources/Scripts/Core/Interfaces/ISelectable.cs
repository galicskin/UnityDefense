using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Flags]
public enum SelectionType
{
    None = 0,
    Worker = 1 << 0,
    Building = 1 << 1,
    Special = 1 << 2,
}
public interface ISelectable
{
    Transform Transform { get; }
    SelectionType SelectionType { get; } // Unit/Building/Resource
    Bounds SelectionBounds { get; }
    Renderer SelectionRenderer { get; }
    void OnSelected(); void OnDeselected();
}