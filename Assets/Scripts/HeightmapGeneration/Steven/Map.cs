using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

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

    public void SetRandomColors()
    {
        for (var x = 0; x < Width * Height; x++)
        {
            float grayValue = Random.Range(0, 255) / 255f;
            Heightmap[x] = new Color(grayValue, grayValue, grayValue);
            ColorMap[x] = new Color(Random.Range(0, 255) / 255f, Random.Range(0, 255) / 255f, Random.Range(0, 255) / 255f);
        }
    }
    
    public void SetHeightMapNoise(int perlinXScale, int perlinYScale)
    {
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                float grayValue = Mathf.PerlinNoise(x / (float) perlinXScale, y / (float) perlinYScale);
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
