using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ProceduralColors/MultiColorPaletteGroup")]
public class MultiColorPaletteGroup : ScriptableObject
{
    public List<MultiColorPaletteList> list;


    public MultiColorPaletteList GetRandomPaletteList()
    {
        return list[Random.Range(0, list.Count)];
    }
}
