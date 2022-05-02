using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Noise/NoiseValuesGroup")]
public class PlanetNoiseGroup : ScriptableObject
{
    public List<PlanetNoiseList> list;


    public PlanetNoiseList GetRandomNoiseList()
    {
        return list[Random.Range(0, list.Count)];
    }
}