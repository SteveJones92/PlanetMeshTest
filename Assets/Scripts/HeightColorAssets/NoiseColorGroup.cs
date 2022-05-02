using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[CreateAssetMenu(menuName = "Planet/NoiseColorGroups")]
public class NoiseColorGroup : SerializedScriptableObject
{
    public Dictionary<PlanetNoiseList, MultiColorPaletteList> List = new Dictionary<PlanetNoiseList, MultiColorPaletteList>();

    public KeyValuePair<PlanetNoiseList, MultiColorPaletteList> GetRandomMap()
    {
        return List.ElementAt(Random.Range(0, List.Count));
    }
}
