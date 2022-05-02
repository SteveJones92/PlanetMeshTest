using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Noise/NoiseValuesList")]
public class PlanetNoiseList : ScriptableObject
{
    public List<PlanetNoise> list;

    public PlanetNoise GetRandomNoise()
    {
        return list[Random.Range(0, list.Count)];
    }
}
