using System.Collections;
using System.Collections.Generic;
using TinkerWorX.AccidentalNoiseLibrary;
using UnityEngine;

[CreateAssetMenu(menuName = "Noise/NoiseValues")]
public class PlanetNoise : ScriptableObject
{
    public Vector2Int dimensionsForGeneration;
    public Vector2Int perlinScale;
    public float lacunarity;
    public float frequency;
    public float offset;
    public int octaves;
    public float h;
    public float gain;
    public bool wrapped;
    public bool stretched;
    public int seed;
    public float stretchPower;
    public FractalType fractalType;
    public BasisType basisType;
    public InterpolationType interpolationType;
}
