using System.IO;
using System.Runtime.CompilerServices;
using TinkerWorX.AccidentalNoiseLibrary;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class Map
{
    // current width - either reg or upscaled resolution
    private int Width;
    // current height - either reg or upscaled resolution
    private int Height;
    
    private PlanetNoise _noiseSettings;
    private MultiColorPalette _colorSettings;
    
    // make sure color is between black and white
    public Color[] Heightmap { get; set; }
    // actual colors
    public Color[] ColorMap { get; set; }
    
    private float minValue = float.MaxValue;
    private float maxValue = float.MinValue;

    // save the fractal setup for resampling
    private ImplicitFractal _noiseFunction;

    public Map(PlanetNoise noiseSettings, MultiColorPalette colorSettings)
    {
        this._noiseSettings = noiseSettings;
        this._colorSettings = colorSettings;
        
        // generate the initial non-upscaled heightmap
        SetHeightMapNoise();
        // upscale based on upscale dimensions
        if (noiseSettings.upscale) Upscale();
        
        // TODO can post process the noise map here, for example adding craters or volcanos, smoothing
        if (noiseSettings.smooth) Smooth(); // warning, expensive
        
        // before moving on, remap the result to 0-1 scale
        Remap();
        
        // create the color map from the palette, after any noise modifications have happened
        SetColors();
        // TODO can post process color map here for better looks (like using the heatmap and other options)
        // followup smoothing to replace current smoothing as it was a nice option
    }

    private float heightMapValue;
    private int idx;
    public void SetColors()
    {
        // colormap should match width and height
        ColorMap = new Color[Width * Height];
        // ensure the palette output colors are generated
        _colorSettings.Generate();
        // get the output color list from the generation
        Color[] colors = _colorSettings.GetColors();
        // set up a ratio for use in indexing based on the heightmap values
        float ratio = 1f / colors.Length;
        
        // go through each item in the colormap and set it based on heightmap value
        for (var x = 0; x < ColorMap.Length; x++)
        {
            // get one of the gray values
            heightMapValue = Heightmap[x].r;
            // index based on value 0-1, to where that would be in the color list, using ratio
            idx = (int) (heightMapValue / ratio);
            // ensure not out of bounds, in case heightmap isnt exactly 0-1
            if (idx >= colors.Length) idx = colors.Length - 1;
            if (idx < 0) idx = 0;
            ColorMap[x] = colors[idx];
        }
    }

    // create the heightmap based on given dimensions
    private void SetHeightMapNoise()
    {
        //float time = Time.realtimeSinceStartup;
        
        // create the noise function based on settings
        _noiseFunction = new ImplicitFractal(_noiseSettings.fractalType, _noiseSettings.basisType, _noiseSettings.interpolationType);
        _noiseFunction.Frequency = _noiseSettings.frequency;
        _noiseFunction.Lacunarity = _noiseSettings.lacunarity;
        _noiseFunction.Octaves = _noiseSettings.octaves;
        _noiseFunction.Offset = _noiseSettings.offset;
        _noiseFunction.H = _noiseSettings.h;
        _noiseFunction.Gain = _noiseSettings.gain;
        _noiseFunction.Seed = _noiseSettings.seed;

        // initialize heightmap for dimensions
        Heightmap = new Color[_noiseSettings.dimensionsForGeneration.x * _noiseSettings.dimensionsForGeneration.y];
        Width = _noiseSettings.dimensionsForGeneration.x;
        Height = _noiseSettings.dimensionsForGeneration.y;

        int centerX = Width / 2;
        int centerY = Height / 2;
        float r = Width / (2f * Mathf.PI);
        float longitudeR, newX, newY, newZ;
        for (var y = 0; y < Height; y++)
        {
            float latitude = (float)(y - centerY) / (float)Height;
            longitudeR = r * Mathf.Cos(latitude * Mathf.PI);
            newY = Mathf.Sin(latitude*Mathf.PI)*r;
            for (var x = 0; x < Width; x++)
            {
                 float longitude = (float)(x - centerX) / (float)centerX;
                 newX = Mathf.Cos(longitude * Mathf.PI) * longitudeR + _noiseSettings.offset;
                 newZ = Mathf.Sin(longitude * Mathf.PI) * longitudeR + _noiseSettings.offset;


                 // get the noise at each point, all the calculations were for 3D noise sampling on a sphere
                 float grayValue = (float) _noiseFunction.Get(
                     (newX + _noiseSettings.offset) / (float)_noiseSettings.perlinScale, 
                     (newY + _noiseSettings.offset) / (float)_noiseSettings.perlinScale, 
                     (newZ + _noiseSettings.offset) / (float)_noiseSettings.perlinScale);
                 
                 // make sure to save the min and max values for remapping later
                 if (grayValue < minValue) minValue = grayValue;
                 if (grayValue > maxValue) maxValue = grayValue;
                 
                 // set the heightmap color
                 Heightmap[x + y * Width] = new Color(grayValue, grayValue, grayValue);
            }
        }
         //Debug.Log(Time.realtimeSinceStartup - time);
    }

    private void Remap()
    {
        for (var y = 0; y < Height; y++)
        {
            // go through and remap the values to 0-1 range
            for (var x = 0; x < Width; x++)
            {
                float grayValue = Heightmap[x + y * Width].r;
                grayValue = (grayValue - minValue) / (maxValue - minValue);
                grayValue = Mathf.Pow(grayValue, _noiseSettings.powerRule);
                Heightmap[x + y * Width] = new Color(grayValue, grayValue, grayValue);
            }
        }
    }

    // upscale image into a larger resolution
    public void Upscale()
    {
        int newX = _noiseSettings.upscaleTo.x;
        int newY = _noiseSettings.upscaleTo.y;
        // upscaling should be larger for both
        if (newX <= Width || newY <= Height) return;

        // heightmap
        Color[] nHeightMap = new Color[newX * newY];

        float ratioOldNewX = (float)Width / newX;
        float ratioOldNewY = (float)Height / newY;
        float ratio = Mathf.Sqrt((1 / ratioOldNewX) * (1 / ratioOldNewY));
        
        for (int i = 0; i < newX; i++)
        {
            for (int j = 0; j < newY; j++)
            {
                // remap new coordinates to old for stretched sampling
                float oldXf = i * ratioOldNewX;
                float oldYf = j * ratioOldNewY;
                int oldX = (int)oldXf;
                int oldY = (int)oldYf;

                // old point, use same value
                if (oldXf == oldX && oldYf == oldY)
                {
                    nHeightMap[i + j * newX] = Heightmap[oldX + oldY * Width];
                }
                else if (Random.Range(0f, 1f) < _noiseSettings.resampleRatio)
                {
                    nHeightMap[i + j * newX] = ResampleNoise(i, j, ratio);
                }
                // if not, get the proper average of the points it would be between
                else
                {
                    nHeightMap[i + j * newX] = UpscaleGetAverage(oldX, oldY);
                }
            }
        }

        // reset width and height to new, and Heightmap to fixed resolution
        Width = newX;
        Height = newY;
        Heightmap = nHeightMap;
    }

    private Color ResampleNoise(int x, int y, float ratio)
    {
        int width = _noiseSettings.upscaleTo.x;
        int height = _noiseSettings.upscaleTo.y;
        int centerX = width / 2;
        int centerY = height / 2;
        float r = width / (2f * Mathf.PI);
        float longitudeR, newX, newY, newZ;
        float latitude = (float)(y - centerY) / (float)height;
        longitudeR = r * Mathf.Cos(latitude * Mathf.PI);
        newY = Mathf.Sin(latitude*Mathf.PI)*r;
        float longitude = (float)(x - centerX) / (float)centerX;
        newX = Mathf.Cos(longitude * Mathf.PI) * longitudeR + _noiseSettings.offset;
        newZ = Mathf.Sin(longitude * Mathf.PI) * longitudeR + _noiseSettings.offset;
        float grayValue = (float) _noiseFunction.Get(
            (newX + _noiseSettings.offset) / (float)(_noiseSettings.perlinScale * ratio), 
            (newY + _noiseSettings.offset) / (float)(_noiseSettings.perlinScale * ratio), 
            (newZ + _noiseSettings.offset) / (float)(_noiseSettings.perlinScale * ratio));

        
        // make sure to save the min and max values for remapping later
        if (grayValue < minValue) minValue = grayValue;
        if (grayValue > maxValue) maxValue = grayValue;
        
        return new Color(grayValue, grayValue, grayValue);
    }
    

    // upscale averages around where the point would be between
    // no diagonals are counted, but could improve the output
    private Color UpscaleGetAverage(int oldX, int oldY)
    {
        int point = oldX + oldY * Width;
        // cover edge case of last item
        if (point == Heightmap.Length - 1) return Heightmap[point];
        int pointNext = point + 1;
        int pointAbove = oldX + (oldY - 1) * Width;
        int pointAboveNext = pointAbove + 1;
        int pointBelow = oldX + (oldY + 1) * Width;
        int pointBelowNext = pointBelow + 1;
        
        int count = 2;
        float avg = (Heightmap[point].r + Heightmap[pointNext].r);

        if (pointAbove > 0)
        {
            avg += (Heightmap[pointAbove].r + Heightmap[pointAboveNext].r) / 2f;
            count++;
        }

        if (pointBelow < Heightmap.Length - 1)
        {
            avg += (Heightmap[pointBelow].r + Heightmap[pointBelowNext].r) / 2f;
            count++;
        }

        avg /= count;
        return new Color(avg, avg, avg);
    }
    
    private void Smooth()
    {
        //larger upscales sample more points?
        int smoothAmount = _noiseSettings.smoothDistance;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                float avg = 0;
                int count = 0;
                
                for (int i = x - smoothAmount; i <= x + smoothAmount; i++)
                {
                    for (int j = y - smoothAmount; j <= y + smoothAmount; j++)
                    {
                        if (i < 0 || j < 0 || i >= Width || j >= Height) continue;

                        avg += Heightmap[i + j * Width].r;
                        count++;
                    }
                }
    
                avg /= count;
                Heightmap[x + y * Width] = new Color(avg, avg, avg);
            }
        }
    }

    // use the current map to create a texture for the heightmap and colormap
    public Texture2D[] GetTextures()
    {
        var textureHeight = new Texture2D(Width,  Height);
        var textureColor = new Texture2D(Width, Height);
        textureHeight.SetPixels(Heightmap);
        textureColor.SetPixels(ColorMap);
        textureColor.wrapMode = TextureWrapMode.Clamp;
        textureHeight.wrapMode = TextureWrapMode.Clamp;
        textureHeight.filterMode = FilterMode.Point;
        textureColor.filterMode = FilterMode.Point;
        textureHeight.Apply();
        textureColor.Apply();
        return new[] {textureHeight, textureColor};
    }
    
    // output the heightmap and colormap as png images
    public void CreateTextureImages(string name)
    {
        var texs = GetTextures();
        File.WriteAllBytes("Assets/Maps/height" + name + ".png", texs[0].EncodeToPNG());
        File.WriteAllBytes("Assets/Maps/color" + name + ".png", texs[1].EncodeToPNG());
    }
}