using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ProceduralColors/MultiColorPaletteList")]
public class MultiColorPaletteList : ScriptableObject
{
    public List<MultiColorPalette> list;


    public MultiColorPalette GetRandomPalette()
    {
        return list[Random.Range(0, list.Count)];
    }
}
