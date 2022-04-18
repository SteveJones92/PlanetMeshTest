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

    public Map(int width, int height)
    {
        this.Width = width;
        this.Height = height;
        Heightmap = new Color[width * height];
        ColorMap = new Color[width * height];
    }

    public void SetColors()
    {
        for (var x = 0; x < Width * Height; x++)
        {
            //Debug.Log(Heightmap[x].r);
            float heightMapValue = Heightmap[x].r;
            Color color;
            
            if (heightMapValue < 0f) {
                color = Color.blue * .5f;
            }
            else if (heightMapValue < .2f)
            {
                color = Color.blue;
            }
            else if (heightMapValue < .4f)
            {
                color = Color.green * .7f;
            }
            else if (heightMapValue < .8f)
            {
                color = Color.gray;
            }
            else
            {
                color = Color.white; // snow
            }

            ColorMap[x] = color;
        }
    }
    
    public void SetHeightMapNoise(int perlinXScale, int perlinYScale, float frequency, float lacunarity, int octaves, float offset, float h, float gain)
    {
        var noiseFunction = new ImplicitFractal(FractalType.FractionalBrownianMotion, BasisType.Simplex, InterpolationType.Cubic);
        noiseFunction.Frequency = frequency;
        noiseFunction.Lacunarity = lacunarity;
        noiseFunction.Octaves = octaves;
        noiseFunction.Offset = offset;
        noiseFunction.H = h;
        noiseFunction.Gain = gain;
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                float grayValue = (float) noiseFunction.Get(x / (float)perlinXScale, y / (float)perlinYScale);
                //float grayValue = Mathf.PerlinNoise(x / (float) perlinXScale, y / (float) perlinYScale);
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
