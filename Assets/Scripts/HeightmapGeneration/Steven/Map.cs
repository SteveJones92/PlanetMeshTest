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
                color = Color.Lerp(Color.blue*.65f, Color.blue*.45f, (heightMapValue)/-1.0f);
            }
            else if (heightMapValue < .2f)
            {
                color = Color.blue;
            }
            else if (heightMapValue < .45f)
            {
                color = Color.Lerp(Color.green*.71f, Color.green *.30f, (heightMapValue - .2f)/.30f);
            }
            else if (heightMapValue < .8f)
            {
                color = Color.Lerp(Color.gray*.75f, Color.gray, (heightMapValue - .45f)/1.1f);
            }
            else
            {
                color = Color.white; // snow
            }

            ColorMap[x] = color;
        }
    }
    
    public void SetHeightMapNoise(int perlinXScale, int perlinYScale, float frequency, float lacunarity, int octaves,
        float offset, float h, float gain, bool wrapped, int seed, bool stretched, float stretchPower, FractalType fractalType, BasisType basisType, InterpolationType interpolationType)
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

/*

        for (var y = 0; y < Height; y++)
        {
            float stretchedPercentage = 1f;
            
            // at this y, how much of the x should be extra sampled
            if (stretched)
            {
                // percentage away from middle
                stretchedPercentage = 1 - Mathf.Abs(Height / 2f - y) / (Height / 2f);
                stretchedPercentage = Mathf.Pow(stretchedPercentage, stretchPower);
                //stretchedPercentage = Mathf.Sin(stretchedPercentage*1.5708f);
            }
            int newWidth = (int)(Width * stretchedPercentage);
            float stretchOffset = (Width - newWidth) / 2f;
            
            for (var x = 0; x < Width; x++)
            {
                //float n = Mathf.InverseLerp(0, Width, x);    
                float r = Mathf.Cos (Mathf.Deg2Rad * lon);
                float newX = r * Mathf.Cos (Mathf.Deg2Rad * lat);
                float newy = Mathf.Sin (Mathf.Deg2Rad * lon);
                float newZ = r * Mathf.Sin (Mathf.Deg2Rad * lat);
                //float newX = _newX[x];// * Mathf.Lerp(stretchOffset, newWidth + stretchOffset, n);
                //float newZ = _newZ[x];// * Mathf.Lerp(stretchOffset, newWidth + stretchOffset, n);
                float grayValue = (float) noiseFunction.Get(newX / (float)perlinXScale, y / (float)perlinYScale, newZ);

                if (wrapped)
                {
                    int otherX = Width - x;
                    float nn = Mathf.InverseLerp(0, Width, otherX);
                    int otherNewX = (int) Mathf.Lerp(stretchOffset, newWidth + stretchOffset, nn);
                    
                    float otherGrayValue = (float) noiseFunction.Get(otherNewX / (float)perlinXScale, y / (float)perlinYScale);
                    // percentage away from middle
                    float percentage = 1 - Mathf.Abs(newWidth / 2f - newX) / (newWidth / 2f);
                    if (!stretched)
                        percentage = Mathf.Pow(percentage, stretchPower);
                    
                    // make farthest away from middle be .5 instead of 0
                    float normal = Mathf.InverseLerp(0f, 1f, percentage);
                    percentage = Mathf.Lerp(0.5f, 1f, normal);
                    
                    // mix the 2, center is no averaging, edges are equal percentages averaging
                    grayValue = (grayValue * percentage + otherGrayValue * (1f - percentage));
                }
                
                Heightmap[x + y * Width] = new Color(grayValue, grayValue, grayValue);
            }
        }

*/