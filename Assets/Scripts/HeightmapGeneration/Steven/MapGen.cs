using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapGen : MonoBehaviour
{
    public GameObject planePrefab;
    public GameObject mapPrefab;
    public int width;
    public int height;
    // Start is called before the first frame update
    
    [ShowInInspector]
    void Start()
    {
        var map = new Map(width, height);
        
        var gameObj = Instantiate(mapPrefab, transform);
        gameObj.name = "Map" + transform.childCount;
        
        var hmPlane = Instantiate(planePrefab, gameObj.transform);
        var cmPlane = Instantiate(planePrefab, new Vector3(15, 0, 0), Quaternion.identity, gameObj.transform);

        var hmPlaneRenderer = hmPlane.GetComponent<MeshRenderer>();
        //var hmPlaneMeshFilter = hmPlane.GetComponent<MeshFilter>();
        var cmPlaneRenderer = cmPlane.GetComponent<MeshRenderer>();
        //var cmPlaneMeshFilter = cmPlane.GetComponent<MeshFilter>();

        map.SetRandomColors();
        var texs = GetTextures(map);
        hmPlaneRenderer.material.mainTexture = texs[0];
        cmPlaneRenderer.material.mainTexture = texs[1];
        gameObj.GetComponent<TexturePrint>().SetHeightMap(texs[0]);
        gameObj.GetComponent<TexturePrint>().SetColorMap(texs[1]);
    }

    [ShowInInspector]
    void ClearChildren()
    {
        while (transform.childCount > 0) DestroyImmediate(transform.GetChild(0).gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public struct Map
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

        public Color GetPixel(int x, int y)
        {
            return Color.black;
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
    }

    public Texture2D[] GetTextures(Map map)
    {
        var textureHeight = new Texture2D( map.Width,  map.Height);
        var textureColor = new Texture2D(map.Width, map.Height);
        textureHeight.SetPixels(map.Heightmap);
        textureColor.SetPixels(map.ColorMap);
        textureColor.wrapMode = TextureWrapMode.Clamp;
        textureHeight.wrapMode = TextureWrapMode.Clamp;
        textureHeight.Apply();
        textureColor.Apply();
        return new[] {textureHeight, textureColor};
    }
}
