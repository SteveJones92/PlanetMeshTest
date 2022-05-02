using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using TinkerWorX.AccidentalNoiseLibrary;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using Random = UnityEngine.Random;

public class Map
{
    public int Width { get; }
    public int Height { get; }
    // make sure color is between black and white
    public Color[] Heightmap { get; }
    // actual colors
    public Color[] ColorMap { get; }

    private float minValue = float.MaxValue;
    private float maxValue = float.MinValue;

    public Map(int width, int height)
    {
        this.Width = width;
        this.Height = height;
        Heightmap = new Color[width * height];
        ColorMap = new Color[width * height];
    }

    private float heightMapValue;
    private int idx;
    public void SetColors(Color[] colors)
    {
        float ratio = 1f / colors.Length;
        
        for (var x = 0; x < Width * Height; x++)
        {
            heightMapValue = Heightmap[x].r;
            idx = (int) (heightMapValue / ratio);
            if (idx >= colors.Length) idx = colors.Length - 1;
            if (idx < 0) idx = 0;
            ColorMap[x] = colors[idx];;
        }
    }
    
    public void SetHeightMapNoise(int perlinXScale, int perlinYScale, float frequency, float lacunarity, int octaves,
        float offset, float h, float gain, int seed, FractalType fractalType, BasisType basisType, InterpolationType interpolationType)
    {
        var noiseFunction = new ImplicitFractal(fractalType, basisType, interpolationType);
        noiseFunction.Frequency = frequency;
        noiseFunction.Lacunarity = lacunarity;
        noiseFunction.Octaves = octaves;
        noiseFunction.Offset = offset;
        noiseFunction.H = h;
        noiseFunction.Gain = gain;
        noiseFunction.Seed = seed;

        int centerx = Width/2;
        int centery = Height/2;
        float r = Width/ (2f*Mathf.PI);
        float longitude_r, newX, newY, newZ;
        for (var y = 0; y < Height; y++)
        {
            float latitude = (float)(y - centery) / (float)Height;
            longitude_r = r*Mathf.Cos(latitude * Mathf.PI);
            newY = Mathf.Sin(latitude*Mathf.PI)*r;
            for (var x = 0; x < Width; x++)
            {
                float longitude = (float)(x - centerx) / (float)centerx;
                newX = Mathf.Cos(longitude * Mathf.PI) * longitude_r + offset;
                newZ = Mathf.Sin(longitude * Mathf.PI) * longitude_r + offset;


                float grayValue = (float) noiseFunction.Get((newX + offset) / (float)perlinXScale, (newY + offset) / (float)perlinXScale, (newZ + offset) / (float)perlinXScale);
                if (grayValue < minValue) minValue = grayValue;
                if (grayValue > maxValue) maxValue = grayValue;
                Heightmap[x + y * Width] = new Color(grayValue, grayValue, grayValue);
            }
            for (var x = 0; x < Width; x++)
            {
                float grayValue = Heightmap[x + y * Width].r;
                //grayValue = 0.01f + ( grayValue - minValue ) * ( 0.99f - 0.01f ) / ( maxValue - minValue );
                grayValue = 0f + ( grayValue - minValue ) * ( 1f - 0f ) / ( maxValue - minValue );
                //grayValue = grayValue * grayValue;
                Heightmap[x + y * Width] = new Color(grayValue, grayValue, grayValue);
            }
        }
    }

    // use the current map to create a texture for the heightmap and colormap
    public Texture2D[] GetTextures()
    {
        var textureHeight = new Texture2D( Width,  Height);
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
    
    // output
    public void CreateTextureImages(string name)
    {
        var texs = GetTextures();
        File.WriteAllBytes("Assets/Maps/height" + name + ".png", texs[0].EncodeToPNG());
        File.WriteAllBytes("Assets/Maps/color" + name + ".png", texs[1].EncodeToPNG());
    }
}