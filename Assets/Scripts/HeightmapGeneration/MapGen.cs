using TinkerWorX.AccidentalNoiseLibrary;
using UnityEngine;

public class MapGen : MonoBehaviour
{
    public static Map CreateMap(int width, int height, int upX, int upY, int perlinX, int perlinY, float frequency, float lacunarity, int octaves, float powerRule,
        float offset, float h, float gain, int seed, FractalType fractalType, BasisType basisType, InterpolationType interpolationType, Color[] colors)
    {
        Map map = new Map(width, height);
        map.SetHeightMapNoise(perlinX, perlinY, frequency, lacunarity, octaves, powerRule, offset, h, gain, seed, fractalType, basisType, interpolationType);
        map.Upscale(upX, upY);
        map.SetColors(colors);
        return map;
    }
}
