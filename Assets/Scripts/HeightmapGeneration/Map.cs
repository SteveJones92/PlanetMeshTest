using System.IO;
using TinkerWorX.AccidentalNoiseLibrary;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class Map
{
    public int Width { get; set; }
    public int Height { get; set; }
    // make sure color is between black and white
    public Color[] Heightmap { get; set; }
    // actual colors
    public Color[] ColorMap { get; set; }

    private float minValue = float.MaxValue;
    private float maxValue = float.MinValue;

    public Map(int width, int height)
    {
        this.Width = width;
        this.Height = height;
        Heightmap = new Color[width * height];
        ColorMap = new Color[width * height];
    }
    
    public struct ColorJob : IJobParallelFor
    {
        public NativeArray<Color> heightmap;
        [ReadOnly]
        public NativeArray<Color> colors;
        public NativeArray<Color> result;
        public float ratio;

        public void Execute(int index)
        {
            float heightMapValue = heightmap[index].r;
            int idx = (int) (heightMapValue / ratio);
            if (idx >= colors.Length) idx = colors.Length - 1;
            if (idx < 0) idx = 0;
            result[index] = colors[idx];
        }
    }
    
    private float heightMapValue;
    private int idx;
    public void SetColors(Color[] colors)
    {
        //float time = Time.realtimeSinceStartup;
        // NativeArray<Color> nColors = new NativeArray<Color>(colors.Length, Allocator.TempJob);
        // nColors.CopyFrom(colors);
        // NativeArray<Color> nHeightmap = new NativeArray<Color>(Heightmap.Length, Allocator.TempJob);
        // nHeightmap.CopyFrom(Heightmap);
        // NativeArray<Color> result = new NativeArray<Color>(ColorMap.Length, Allocator.TempJob);
        //
        // ColorJob job = new ColorJob();
        // job.result = result;
        // job.ratio = 1f / colors.Length;
        // job.colors = nColors;
        // job.heightmap = nHeightmap;
        //
        // // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
        // JobHandle handle = job.Schedule(result.Length, 64);
        //
        // handle.Complete();
        // ColorMap = result.ToArray();
        // nColors.Dispose();
        // nHeightmap.Dispose();
        // result.Dispose();

        float ratio = 1f / colors.Length;
        
        for (var x = 0; x < ColorMap.Length; x++)
        {
            heightMapValue = Heightmap[x].r;
            idx = (int) (heightMapValue / ratio);
            if (idx >= colors.Length) idx = colors.Length - 1;
            if (idx < 0) idx = 0;
            ColorMap[x] = colors[idx];
        }
        //Debug.Log(Time.realtimeSinceStartup - time);
    }

    public void SetHeightMapNoise(int perlinXScale, int perlinYScale, float frequency, float lacunarity, int octaves, float powerRule,
        float offset, float h, float gain, int seed, FractalType fractalType, BasisType basisType, InterpolationType interpolationType)
    {
        float time = Time.realtimeSinceStartup;
        ImplicitFractal noiseFunction = new ImplicitFractal(fractalType, basisType, interpolationType);
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
                 grayValue = ( grayValue - minValue ) / ( maxValue - minValue );
                 grayValue = Mathf.Pow(grayValue, powerRule);
                 Heightmap[x + y * Width] = new Color(grayValue, grayValue, grayValue);
             }
         }
         Debug.Log(Time.realtimeSinceStartup - time);
    }

    public void Upscale(int x, int y)
    {
        // upscaling should be larger for both
        if (x <= Width || y <= Height) return;
        Debug.Log(x);
        Debug.Log(y);
        
        // how many left and right to check for avg
        int xScale = Mathf.FloorToInt((float)x / Width);
        // how many up or down to check for avg
        int yScale = Mathf.FloorToInt((float)y / Height);
        // heightmap
        Color[] nHeightMap = new Color[x * y];

        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                int oldX = (i * Width) / x;
                int oldY = (j * Height) / y;
                
                nHeightMap[i + j * x] = Average(oldX, oldY, xScale, yScale);
            }
        }

        Width = x;
        Height = y;
        Heightmap = nHeightMap;
        // colormap
        ColorMap = new Color[x * y];
    }

    private Color Average(int x, int y, int xScale, int yScale)
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