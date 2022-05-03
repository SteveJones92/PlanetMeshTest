using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TinkerWorX.AccidentalNoiseLibrary;
using UnityEngine;
[CreateAssetMenu(menuName = "Noise/NoiseValues")]
public class PlanetNoise : ScriptableObject
{
    // resolution to create heightmap (and associated other maps like colormap)
    public Vector2Int dimensionsForGeneration;
    // use upscaling to get larger resolutions without sampling noise
    public bool upscale;
    // new resolution to scale up to
    [ShowIf("upscale")]
    public Vector2Int upscaleTo;
    // scale multiplier to sample noise
    public int perlinScale;
    // higher value means more detail around a point (breaks up smooth surfaces)
    public float lacunarity;
    // reverse of perlinScale
    public float frequency;
    public float offset;
    // higher octaves costs more but allows more detail, used in conjunction with lacunarity
    public int octaves;
    public float h;
    public float gain;
    // random seed value
    public int seed;
    public FractalType fractalType;
    // simplex is the fastest and therefore preferable
    public BasisType basisType;
    public InterpolationType interpolationType;
    // used to rescale noise values, 2 means lower lows while preserving highs, < 1 has opposite effect
    public float powerRule = 1;
    // scale out the height of the map on the sphere surface
    [Min(1)]
    public float heightScale = 1;
    // slightly different than power rule, applies a power directly to output height, which scales out heights, whereas powerRule is on 0-1 decimals
    [Min(0)]
    public float heightPower = 1;
    [ToggleLeft]
    // used to toggle whether to use smoothing, autosmoothing is based on amount upscaled
    // otherwise it means how many layers around to check for averaging
    public bool smooth = true;
    [ShowIf("smooth"), Min(1)]
    public int smoothDistance = 1;
    [ShowIf("upscale"), Min(0f), MaxValue(1f)]
    public float resampleRatio = 0.0f;
}
