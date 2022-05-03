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
        // TODO can post process the noise map here, for example adding craters or volcanos
        // create the color map from the palette, after any noise modifications have happened
        SetColors();
        // TODO can post process color map here for better looks (like using the heatmap and other options)
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
            
            // go through and remap the values to 0-1 range
            for (var x = 0; x < Width; x++)
            {
                 float grayValue = Heightmap[x + y * Width].r;
                 grayValue = ( grayValue - minValue ) / ( maxValue - minValue );
                 grayValue = Mathf.Pow(grayValue, _noiseSettings.powerRule);
                 Heightmap[x + y * Width] = new Color(grayValue, grayValue, grayValue);
            }
        }
         //Debug.Log(Time.realtimeSinceStartup - time);
    }

    // upscale image into a larger resolution
    public void Upscale()
    {
        int newX = _noiseSettings.upscaleTo.x;
        int newY = _noiseSettings.upscaleTo.y;
        // upscaling should be larger for both
        if (newX <= Width || newY <= Height) return;

        // larger upscales sample more points?
        int xScale = _noiseSettings.smoothDistance;
        int yScale = _noiseSettings.smoothDistance;

        if (_noiseSettings.autoSmoothDistance)
        {
            xScale = Mathf.FloorToInt((float) newX / Width);                    // TODO
            yScale = Mathf.FloorToInt((float) newY / Height);
        }
        
        // heightmap
        Color[] nHeightMap = new Color[newX * newY];

        for (int i = 0; i < newX; i++)
        {
            for (int j = 0; j < newY; j++)
            {
                // remap new coordinates to old for stretched sampling
                int oldX = (i * Width) / newX;
                int oldY = (j * Height) / newY;
                
                // if we only use the point at that position, we get the same image at a larger resolution
                // averaging smooths
                if (_noiseSettings.smoothUpscale)
                {
                    nHeightMap[i + j * newX] = Smooth(oldX, oldY, xScale, yScale);
                }
                else
                {
                    nHeightMap[i + j * newX] = Heightmap[oldX + oldY * Width];
                }
            }
        }

        // reset width and height to new, and Heightmap to fixed resolution
        Width = newX;
        Height = newY;
        Heightmap = nHeightMap;
    }

    private Color Smooth(int x, int y, int xScale, int yScale)
    {
        float avg = 0;
        int count = 0;
        for (int i = x - xScale; i <= x + xScale; i++)
        {
            for (int j = y - yScale; j <= y + yScale; j++)
            {
                if (i < 0 || j < 0 || i >= Width || j >= Height) continue;

                avg += Heightmap[i + j * Width].r;
                count++;
            }
        }

        avg /= count;
        return new Color(avg, avg, avg);
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