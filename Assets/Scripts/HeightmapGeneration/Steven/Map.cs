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
    
    public void SetHeightMapNoise(int perlinXScale, int perlinYScale, float frequency, float lacunarity, int octaves, float offset, float h, float gain, bool wrapped, int seed, bool stretched)
    {
        var noiseFunction = new ImplicitFractal(FractalType.FractionalBrownianMotion, BasisType.Simplex, InterpolationType.Cubic);
        noiseFunction.Frequency = frequency;
        noiseFunction.Lacunarity = lacunarity;
        noiseFunction.Octaves = octaves;
        noiseFunction.Offset = offset;
        noiseFunction.H = h;
        noiseFunction.Gain = gain;
        noiseFunction.Seed = seed;

        int offsetStretched = 0;

        for (var y = 0; y < Height; y++)
        {
            float stretchedPercentage = 1f;
            
            // at this y, how much of the x should be extra sampled
            if (stretched)
            {
                // percentage away from middle
                stretchedPercentage = 1 - Mathf.Abs(Height / 2f - y) / (Height / 2f);
            }
            int newWidth = (int)(Width * stretchedPercentage);
            float stretchOffset = (Width - newWidth) / 2f;
            
            for (var x = 0; x < Width; x++)
            {
                float n = Mathf.InverseLerp(0, Width, x);
                int newX = (int) Mathf.Lerp(stretchOffset, newWidth + stretchOffset, n);
                
                float grayValue = (float) noiseFunction.Get(newX / (float)perlinXScale, y / (float)perlinYScale);

                if (wrapped)
                {
                    int otherX = Width - x;
                    float nn = Mathf.InverseLerp(0, Width, otherX);
                    int otherNewX = (int) Mathf.Lerp(stretchOffset, newWidth + stretchOffset, nn);
                    
                    float otherGrayValue = (float) noiseFunction.Get(otherNewX / (float)perlinXScale, y / (float)perlinYScale);
                    // percentage away from middle
                    float percentage = 1 - Mathf.Abs(newWidth / 2f - newX) / (newWidth / 2f);
                    
                    // make farthest away from middle be .5 instead of 0
                    float normal = Mathf.InverseLerp(0f, 1f, percentage);
                    percentage = Mathf.Lerp(0.5f, 1f, normal);
                    
                    // mix the 2, center is no averaging, edges are equal percentages averaging
                    grayValue = (grayValue * percentage + otherGrayValue * (1f - percentage));
                }
                
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
