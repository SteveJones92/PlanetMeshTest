using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TinkerWorX.AccidentalNoiseLibrary;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class Planet : MonoBehaviour
{
    private Map _map;
    private Texture2D heightmap;
    private Texture2D colormap;
    
    [OnValueChanged(nameof(UpdateGeneration))]
    public Vector2Int dimensionsForGeneration;
    [OnValueChanged(nameof(UpdateGeneration))]
    public Vector2Int perlinScale;
    [OnValueChanged(nameof(UpdateGeneration)), MinValue(0.001f)]
    public float lacunarity;
    [OnValueChanged(nameof(UpdateGeneration)), MinValue(0.001f)]
    public float frequency;
    [OnValueChanged(nameof(UpdateGeneration))]
    public float offset;
    [OnValueChanged(nameof(UpdateGeneration)), MinValue(1)]
    public int octaves;
    [OnValueChanged(nameof(UpdateGeneration))]
    public float h;
    [OnValueChanged(nameof(UpdateGeneration))]
    public float gain;
    [OnValueChanged(nameof(UpdateGeneration))]
    public bool wrapped;
    [OnValueChanged(nameof(UpdateGeneration))]
    public bool stretched;
    [OnValueChanged(nameof(UpdateGeneration))]
    public int seed;
    [OnValueChanged(nameof(UpdateGeneration)), MinValue(0.001f)]
    public float stretchPower;
    [OnValueChanged(nameof(UpdateGeneration))]
    public FractalType fractalType;
    [OnValueChanged(nameof(UpdateGeneration))]
    public BasisType basisType;
    [OnValueChanged(nameof(UpdateGeneration))]
    public InterpolationType interpolationType;

    private static SavedMeshes _meshes;

    private struct SavedMeshes
    {
        public int Generations;
        public List<Mesh> MeshList;

        public void AddGeneration(Mesh mesh)
        {
            // if null, initialize
            MeshList ??= new List<Mesh>();
            MeshList.Add(mesh);
            Generations++;
        }
    }
    
    [SerializeField, OnValueChanged("UpdateGeneration"), CustomValueDrawer("MinMaxGenerations")]
    private int currentGeneration;

    [SerializeField]
    private int maxGenerations = 8;

    
    [UsedImplicitly]
    private int MinMaxGenerations(int value, GUIContent label)
    {
        return EditorGUILayout.IntSlider(label, value, 0, maxGenerations);
    }

    [ShowInInspector] private string outputName = "Assets/Resources/Generation";
    
    [ShowInInspector, OnValueChanged(nameof(LoadValues))]
    private GameObject _loadMap;
    
    private void CreateMeshFiles()
    {
        // make sure the files are saved
        IcosphereMesh.CreateMeshGenerations(maxGenerations, outputName);
        // populate savedmeshes
        LoadMeshFiles();
    }
    
    private void LoadMeshFiles()
    {
        for (int i = 0; i <= maxGenerations; i++)
        {
            if (i < _meshes.Generations) continue;
            var meshSaved = File.ReadAllBytes(outputName + i);
            Mesh mesh = SerializationUtility.DeserializeValue<MeshSerializable>(meshSaved, DataFormat.Binary).GetMesh();

            // uvs
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];
        
            for(int j = 0; j < uvs.Length; ++j)
            {
                uvs[j] = new Vector2(
                    0.5f + Mathf.Atan2(vertices[j].x, vertices[j].z)/ (2*Mathf.PI)
                    , 0.5f + Mathf.Asin(vertices[j].y)/(Mathf.PI)
                );
            }
            mesh.uv = uvs;
            
            _meshes.AddGeneration(mesh);
        }
    }

    private void SetHeightColor()
    {
        Mesh mesh = new Mesh();
        Mesh meshBase = _meshes.MeshList[currentGeneration];
        
        mesh.indexFormat = meshBase.indexFormat;
        mesh.vertices = meshBase.vertices.ToArray();
        mesh.triangles = meshBase.triangles.ToArray();
        mesh.uv = meshBase.uv.ToArray();

        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        // colors and height
        Color[] vertexColors = new Color[vertices.Length];
        
        for (int i = 0; i < vertices.Length; i++)  //Applying heightmap to the sphere using uv coords
        {
            Color heightClr = heightmap.GetPixel((int)((uvs[i].x) * heightmap.width), (int)((1 - uvs[i].y) * heightmap.height));
            Color actualClr = colormap.GetPixel((int)((uvs[i].x) * colormap.width), (int)((1 - uvs[i].y) * colormap.height));
            float he = heightClr.r * 0.025f;
            Vector3 v = vertices[i].normalized;
            vertices[i] = new Vector3(vertices[i].x + v.x* he, vertices[i].y + v.y* he, vertices[i].z + + v.z* he );
            vertexColors[i] = actualClr;
        }

        mesh.colors = vertexColors;
        mesh.vertices = vertices;
        
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
    }
    
    // Start is called before the first frame update
    [ShowInInspector]
    private void Setup()
    {
        // will only create if it doesnt exist already at the output name
        CreateMeshFiles();
        // create the map from the values to load heightmap and colormap
        CreateMap();
        // set the color and values
        SetHeightColor();
    }
    
    [ShowInInspector]
    private void PrintOutMap()
    {
        _map.CreateTextureImages(name);
    }

    private void UpdateGeneration()
    {
        CreateMap();
        SetHeightColor();
    }

    private void CreateMap()
    {
        _map = MapGen.CreateMap(dimensionsForGeneration.x, dimensionsForGeneration.y, perlinScale.x, perlinScale.y, frequency, lacunarity, octaves, offset,
            h, gain, wrapped, seed, stretched, stretchPower, fractalType, basisType, interpolationType);
        Texture2D[] texs = _map.GetTextures();
        heightmap = texs[0];
        colormap = texs[1];
        //heightmap = (Texture2D) Resources.Load("earth_heightmap");
        //colormap = (Texture2D) Resources.Load("earth_color");
        // comment
    }
    
    void LoadValues()
    {
        TexturePrint t = _loadMap.GetComponent<TexturePrint>();
        lacunarity = t.lacunarity;
        perlinScale = t.perlinScale;
        dimensionsForGeneration.x = t.width;
        dimensionsForGeneration.y = t.height;
        frequency = t.frequency;
        offset = t.offset;
        octaves = t.octaves;
        h = t.h;
        gain = t.gain;
        wrapped = t.wrapped;
        stretched = t.stretched;
        seed = t.seed;
        stretchPower = t.stretchPower;
        fractalType = t.fractalType;
        basisType = t.basisType;
        interpolationType = t.interpolationType;
        UpdateGeneration();
        _loadMap = null;
    }
}
