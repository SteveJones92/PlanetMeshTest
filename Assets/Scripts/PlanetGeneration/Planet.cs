using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class Planet : MonoBehaviour
{
    private Map _map;
    private Texture2D heightmap;
    private Texture2D colormap;

    [InlineEditor(InlineEditorModes.FullEditor)]
    public NoiseColorGroup mapLists;
    [InlineEditor(InlineEditorModes.FullEditor)]
    public PlanetNoise noise;
    [InlineEditor(InlineEditorModes.FullEditor)]
    public MultiColorPalette palette;

    public bool seedChange;

    // this exists across planets as loaded meshes from the files, don't want to redo a lot
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
    
    [SerializeField]
    private int currentGeneration = 6;

    [SerializeField]
    private int maxGenerations = 8;
    
    private float scale;

    [ShowInInspector] private string outputName = "Assets/Resources/Generation";

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
    
    [ShowInInspector]
    private void LoadRandomPalette()
    {
        palette = mapLists.GetRandomPaletteList().GetRandomPalette();
        palette = Instantiate(palette);
    }

    [ShowInInspector]
    private void LoadRandomNoise()
    {
        noise = mapLists.GetRandomNoiseList().GetRandomNoise();
        noise = Instantiate(noise);
        scale = noise.heightScale;
        noise.seed = Random.Range(1000, 10000);
    }

    [ShowInInspector]
    private void LoadRandomMap()
    {
        var mapPair = mapLists.GetRandomMap();
        noise = mapPair.Key.GetRandomNoise();
        noise = Instantiate(noise);
        noise.seed = Random.Range(1000, 10000);
        scale = noise.heightScale;
        palette = mapPair.Value.GetRandomPalette();
        palette = Instantiate(palette);
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
            float he = heightClr.r * (0.025f * Mathf.Pow(noise.heightScale, noise.heightPower));
            Vector3 v = vertices[i].normalized;
            vertices[i] = new Vector3(vertices[i].x + v.x * he, vertices[i].y + v.y * he, vertices[i].z + + v.z * he );
            vertexColors[i] = actualClr;
        }

        mesh.colors = vertexColors;
        mesh.vertices = vertices;
        
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
    
    // Start is called before the first frame update
    [ShowInInspector]
    private void Setup()
    {
        // will only create if it doesnt exist already at the output name
        CreateMeshFiles();
        if (seedChange) noise.seed = Random.Range(1000, 10000);
        // create the map from the values to load heightmap and colormap
        CreateMap();
        // set the color and values
        SetHeightColor();
    }

    [ShowInInspector]
    private void Initialize()
    {
        LoadRandomMap();
        Setup();
    }

    private void Start()
    {
        Initialize();
    }

    [ShowInInspector]
    public void PrintOutMap()
    {
        _map.CreateTextureImages(name);
    }

    private void CreateMap()
    {
        _map = new Map(noise, palette);
        Texture2D[] texs = _map.GetTextures();
        heightmap = texs[0];
        colormap = texs[1];
        //heightmap = (Texture2D) Resources.Load("earth_heightmap");
        //colormap = (Texture2D) Resources.Load("earth_color");
    }
}
