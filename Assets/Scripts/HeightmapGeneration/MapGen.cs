using TinkerWorX.AccidentalNoiseLibrary;
using UnityEngine;

public class MapGen : MonoBehaviour
{
    public static Map CreateMap(int width, int height, int perlinX, int perlinY, float frequency, float lacunarity, int octaves,
        float offset, float h, float gain, int seed, FractalType fractalType, BasisType basisType, InterpolationType interpolationType, Color[] colors)
    {
        Map map = new Map(width, height);
        map.SetHeightMapNoise(perlinX, perlinY, frequency, lacunarity, octaves, offset, h, gain, seed, fractalType, basisType, interpolationType);
        map.SetColors(colors);
        return map;
    }
}
