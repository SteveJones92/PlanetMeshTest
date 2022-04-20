using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using JetBrains.Annotations;
using TinkerWorX.AccidentalNoiseLibrary;


[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class IcosphereMesh : MonoBehaviour
{
    // All globals, most are static calculations
    #region Globals

    
    // if creating more IcosphereMeshes, dont recalculate everything based on constants
    private static bool _constantsLoaded;
    
    [Title("Length, Height, Z")]
    [ShowInInspector, ReadOnly, FoldoutGroup("L, H, Z"), HideLabel, SuffixLabel("L")]
    private static float _l;
    [ShowInInspector, ReadOnly, FoldoutGroup("L, H, Z"), HideLabel, SuffixLabel("H")]
    private static float _h;
    [ShowInInspector, ReadOnly, FoldoutGroup("L, H, Z"), HideLabel, SuffixLabel("Z")]
    private static float _z;
    
    [Title("L^2, L/2, (L/2)^2, h/3")]
    // common use
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Calculations"), HideLabel, SuffixLabel("L^2")]
    private static float _lSq;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Calculations"), HideLabel, SuffixLabel("L/2")]
    private static float _lHalf;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Calculations"), HideLabel, SuffixLabel("(L/2)^2")]
    private static float _lHalfSq;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Calculations"), HideLabel, SuffixLabel("h/3")]
    private static float _hDiv3;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Calculations"), HideLabel, SuffixLabel("sqrt(3/4)")]
    private static float _sqrt3On4;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Calculations"), HideLabel, SuffixLabel("cos(PI/5)")]
    private static float _s;
    
    [Title("vertices of the triangle")]
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("a")]
    // a = (ax, ay, az) = (-2h/3, 0, z) page5
    private static Vector3 _a;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("b")]
    // b = (bx, by, bz) = (h/3, l/2, z) page5
    private static Vector3 _b;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("bb")]
    // bb = (bx, b.-y, bz)
    private static Vector3 _bb;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("p")]
    // p = (px, py, pz) = (ax, 0, -2az) page5
    private static Vector3 _p;
    [Title("midpoints for the spherical icosahedron triangle ribs")]
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("magBxBz")]
    private static float _magBxBz;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("c")]
    // c = (cx, cy, cz) = (bx / |bx, bz|, 0, bz / |bx, bz|)
    private static Vector3 _c;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("m")]
    // m = (mx, my, mz) = (-cx / 2, sqrt(3 / 4) cx, cz)
    private static Vector3 _m;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("fv90Cw_a")]
    // FrontViewRotation90Cw of _a
    private static Vector3 _ffa;
    //[ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("mow")]
    // outward scaled m
    //private static Vector3 _mow;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("pow")]
    // outward scaled p
    private static Vector3 _pow;

    [SerializeField, OnValueChanged("UpdateGeneration"), CustomValueDrawer("MinMaxGenerations")]
    private int _generations;
    
    [UsedImplicitly]
    private static int MinMaxGenerations(int value, GUIContent label)
    {
        return EditorGUILayout.IntSlider(label, value, 0, 7);
    }

    private List<Axes> _AllAxes = new List<Axes>();
    private List<Vector3> geodesic_grid_base = new List<Vector3>();
    private List<Vpair> duplicated_points = new List<Vpair>();
    private List<Vector3> _geodesicGrid = new List<Vector3>();     // Result array
    private List<Vector3> _allcutPoints = new List<Vector3>();      // Array that stores all the generated cutpoints including one of the apex point

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

    private Map _map;

    private static SavedMeshes _meshes;

    private struct SavedMeshes
    {
        public int Generations;
        public List<Mesh> MeshList;
        public List<Vector3[]> VerticesOriginalList;

        public SavedMeshes(int generations)
        {
            Generations = generations;
            this.MeshList = new List<Mesh>();
            this.VerticesOriginalList = new List<Vector3[]>();
        }

        public void AddGeneration(Mesh mesh, Vector3[] verticesOriginal)
        {
            MeshList.Add(mesh);
            VerticesOriginalList.Add(verticesOriginal);
            Generations++;
        }
    }
    
    #endregion

    // Start, OnDrawGizmos
    #region Unity Functions
    
    [Title("Functions")] 
    [ShowInInspector]
    private void Start()
    {
        _map = MapGen.CreateMap(dimensionsForGeneration.x, dimensionsForGeneration.y, perlinScale.x, perlinScale.y, frequency, lacunarity, octaves, offset,
            h, gain, wrapped, seed, stretched, stretchPower, fractalType, basisType, interpolationType);
        Texture2D[] texs = _map.GetTextures();
        heightmap = texs[0];
        colormap = texs[1];
        //_map.CreateTextureImages(name);

        if (!_constantsLoaded)
        {
            _l = 4 / Mathf.Sqrt(10 + Mathf.Sqrt(20)); // l = 4 / sqrt(10 + sqrt(20)) page4
            _lHalf = _l / 2f;
            _lSq = Mathf.Pow(_l, 2);
            _lHalfSq = Mathf.Pow(_lHalf, 2);
            _h = Mathf.Sqrt(_lSq - _lHalfSq); // h = sqrt(l^2 - (l/2)^2) page5
            _hDiv3 = _h / 3f;
            _z = Mathf.Sqrt(1 - Mathf.Pow(2 * _h / 3, 2)); // z = sqrt(1 - (2h/3)^2 page5
            _sqrt3On4 = Mathf.Sqrt(3f / 4f);
            _s = Mathf.Cos(Mathf.PI / 5f);
            _a = new Vector3(-2 * _hDiv3, 0, _z);
            _b = new Vector3(_hDiv3, _lHalf, _z);
            _bb = new Vector3(_b.x, -_b.y, _b.z);
            _p = new Vector3(_a.x, 0, -2 * _a.z);
            _magBxBz = Mathf.Sqrt(Mathf.Pow(_b.x, 2) + Mathf.Pow(_b.z, 2));
            _c = new Vector3(_b.x / _magBxBz, 0, _b.z / _magBxBz);
            _m = new Vector3(-_c.x / 2, _sqrt3On4 * _c.x, _c.z);
            _ffa = FrontViewRotation90Cw(_a);
            //_mow = EllipseScaleOutward(_m);
            _pow = EllipseScaleOutward(_p);
            calculateAxes();
            _meshes = new SavedMeshes(-1);
            CreateMeshes(7);
            _constantsLoaded = true;
        }
        GetComponent<MeshFilter>().mesh = _meshes.MeshList[_generations]; // highest detail mesh
    }
    
    private void CreateMeshes(int generations)
    {
        _generations = 0;
        for (int i = 0; i <= generations; i++)
        {
            Debug.Log("Creating generation: " + _generations);
            CreateMesh();
            _generations++;
        }

        _generations = generations;
    }

    private void UpdateGeneration()
    {
        _map = MapGen.CreateMap(dimensionsForGeneration.x, dimensionsForGeneration.y, perlinScale.x, perlinScale.y, frequency, lacunarity, octaves, offset,
            h, gain, wrapped, seed, stretched, stretchPower, fractalType, basisType, interpolationType);
        Texture2D[] texs = _map.GetTextures();
        heightmap = texs[0];
        colormap = texs[1];
        //map.CreateTextureImages(name);
        
        Mesh mesh = _meshes.MeshList[_generations];
        Vector3[] vertices = new Vector3[mesh.vertices.Length]; 
        Array.Copy(_meshes.VerticesOriginalList[_generations], vertices, mesh.vertices.Length);
        Vector2[] uvs = mesh.uv;
        Color[] vertexColors = mesh.colors;
        
        for (int i = 0; i < vertices.Length; i++)  //Applying heightmap to the sphere using uv coords
        {
            Color heightClr = heightmap.GetPixel((int)((uvs[i].x) * heightmap.width), (int)((1 - uvs[i].y) * heightmap.height));
            Color actualClr = colormap.GetPixel((int)((uvs[i].x) * colormap.width), (int)((1 - uvs[i].y) * colormap.height));
            float he = heightClr.r * 0.025f;
            Vector3 v = vertices[i].normalized;
            vertices[i] = new Vector3(vertices[i].x + v.x* he, vertices[i].y + v.y* he, vertices[i].z + + v.z* he );
            vertexColors[i] = actualClr;
        }

        mesh.vertices = vertices;
        mesh.colors = vertexColors;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    [ShowInInspector]
    private void PrintOutMap()
    {
        _map.CreateTextureImages(name);
    }

    void calculateAxes()
    {
        _AllAxes.Clear();
        Axes cardinal = new Axes(new Vector3(1,0,0), new Vector3(0,1,0), new Vector3(0,0,1));
        float triangleRot1 = 60f;
        float triangleRot2 = 41.811f;
        float triangleRot3 = 30f;
        Matrix4x4 m;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 1st rotation to top right ------------------------------------------------
        Matrix4x4 mtr1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref mtr1, ref cardinal);
        Matrix4x4 mtr2 = fromRotation(Mathf.Deg2Rad * -triangleRot2, cardinal.x);
        ApplyMat4toAxis(ref mtr2, ref cardinal);
        mtr2*= mtr1;
        mtr1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref mtr1, ref cardinal);
        mtr1*= mtr2;
        m = mtr1;

        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));

        // 2nd rotation to top right ------------------------------------------------
        mtr1 = fromRotation(Mathf.Deg2Rad * triangleRot2, cardinal.y);
        ApplyMat4toAxis(ref mtr1, ref cardinal);
        mtr1 *= m;
        mtr2 = fromRotation(Mathf.Deg2Rad * triangleRot1, cardinal.z);
        ApplyMat4toAxis(ref mtr2, ref cardinal);
        mtr2*= mtr1;
        m = mtr2;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 3rd rotation to top right ------------------------------------------------
        mtr1 = fromRotation(Mathf.Deg2Rad * triangleRot2, cardinal.y);
        ApplyMat4toAxis(ref mtr1, ref cardinal);
        mtr1 *= m;
        mtr2 = fromRotation(Mathf.Deg2Rad * triangleRot1, cardinal.z);
        ApplyMat4toAxis(ref mtr2, ref cardinal);
        mtr2*= mtr1;
        m = mtr2;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 4th rotation to top right ------------------------------------------------
        mtr1 = fromRotation(Mathf.Deg2Rad * triangleRot2, cardinal.y);
        ApplyMat4toAxis(ref mtr1, ref cardinal);
        mtr1 *= m;
        mtr2 = fromRotation(Mathf.Deg2Rad * triangleRot1, cardinal.z);
        ApplyMat4toAxis(ref mtr2, ref cardinal);
        mtr2*= mtr1;
        m = mtr2;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 5th rotation to top right ------------------------------------------------
        mtr1 = fromRotation(Mathf.Deg2Rad * triangleRot2, cardinal.y);
        ApplyMat4toAxis(ref mtr1, ref cardinal);
        mtr1 *= m;
        mtr2 = fromRotation(Mathf.Deg2Rad * triangleRot1, cardinal.z);
        ApplyMat4toAxis(ref mtr2, ref cardinal);
        mtr2*= mtr1;
        m = mtr2;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));

        
        // 1st rotation to left ------------------------------------------------
        cardinal = new Axes(new Vector3(1,0,0), new Vector3(0,1,0), new Vector3(0,0,1));
        Matrix4x4 ml1 = fromRotation(Mathf.Deg2Rad * triangleRot2, cardinal.y);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        Matrix4x4 ml2 = fromRotation(Mathf.Deg2Rad * -triangleRot1, cardinal.z);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        m = ml2;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 2nd rotation to left ------------------------------------------------
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= m;
        ml2 = fromRotation(Mathf.Deg2Rad * -triangleRot2, cardinal.x);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= ml2;
        m = ml1;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 3rd rotation to left ------------------------------------------------
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot2, cardinal.y);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= m;
        ml2 = fromRotation(Mathf.Deg2Rad * triangleRot1, cardinal.z);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        m = ml2;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 4th rotation to left ------------------------------------------------
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot2, cardinal.y);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= m;
        ml2 = fromRotation(Mathf.Deg2Rad * triangleRot1, cardinal.z);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        m = ml2;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 5th rotation to left ------------------------------------------------
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot1, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= m;
        ml2 = fromRotation(Mathf.Deg2Rad * -triangleRot2, cardinal.y);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        m = ml2;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 6th rotation to left ------------------------------------------------
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= m;
        ml2 = fromRotation(Mathf.Deg2Rad * -triangleRot2, cardinal.x);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= ml2;
        m = ml1;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 7th rotation to left ------------------------------------------------
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot2, cardinal.y);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= m;
        ml2 = fromRotation(Mathf.Deg2Rad * triangleRot1, cardinal.z);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        m = ml2;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 8th rotation to left ------------------------------------------------
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot2, cardinal.y);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= m;
        ml2 = fromRotation(Mathf.Deg2Rad * -triangleRot1, cardinal.z);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        m = ml2;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));

        // 1st rotation to bottom right ------------------------------------------------
        cardinal = new Axes(new Vector3(1,0,0), new Vector3(0,1,0), new Vector3(0,0,1));
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot1, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml2 = fromRotation(Mathf.Deg2Rad * -triangleRot2, cardinal.y);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        m = ml2;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 2nd rotation to bottom right ------------------------------------------------
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= m;
        ml2 = fromRotation(Mathf.Deg2Rad * -triangleRot2, cardinal.x);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= ml2;
        m = ml1;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 3rd rotation to bottom right ------------------------------------------------
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= m;
        ml2 = fromRotation(Mathf.Deg2Rad * -triangleRot2, cardinal.x);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= ml2;
        m = ml1;
        Axes cardinal_temp = new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        );
        Matrix4x4 mtemp = m;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // Side Step
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot2, cardinal_temp.y);
        ApplyMat4toAxis(ref ml1, ref cardinal_temp);
        ml1 *= mtemp;
        ml2 = fromRotation(Mathf.Deg2Rad * triangleRot1, cardinal_temp.z);
        ApplyMat4toAxis(ref ml2, ref cardinal_temp);
        ml2*= ml1;
        mtemp = ml2;
        _AllAxes.Add(cardinal_temp);
        // 4th rotation to bottom right ------------------------------------------------
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= m;
        ml2 = fromRotation(Mathf.Deg2Rad * -triangleRot2, cardinal.x);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= ml2;
        m = ml1;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
        // 5nd rotation to bottom right ------------------------------------------------
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= m;
        ml2 = fromRotation(Mathf.Deg2Rad * -triangleRot2, cardinal.x);
        ApplyMat4toAxis(ref ml2, ref cardinal);
        ml2*= ml1;
        ml1 = fromRotation(Mathf.Deg2Rad * triangleRot3, cardinal.z);
        ApplyMat4toAxis(ref ml1, ref cardinal);
        ml1 *= ml2;
        m = ml1;
        _AllAxes.Add(new Axes(
            new Vector3(cardinal.x.x, cardinal.x.y, cardinal.x.z), 
            new Vector3(cardinal.y.x,cardinal.y.y,cardinal.y.z), 
            new Vector3(cardinal.z.x,cardinal.z.y,cardinal.z.z)
        ));
    }

    #endregion

    // Paper implementations
    // RotateClockwise (frcw), RotateCounterClockwise (frccw), FrontViewRotation90Cw (ff), FrontView2D (fxz),
    // GetUnitVectorComponentY (fy), GetUnitVectorComponentZ, ConstructLine (fL), FindIntersectionPoint (fu)
    // FrontViewMirrorPointAlongLine (fm), EllipseScaleOutward (fow), CircleScaleInward (fiw)
    // CutPointOne (c0), CutPointTwo3DMethod, AddSlicingPlanes, CalculateGridPoints
    #region Utility Functions

    private static Vector3 RotateClockwise(Vector3 point)
    {
        // frcw (Qxyz) = (qy * sqrt(3/4) - qx / 2, -qx * sqrt(3/4) - qy / 2, qz)
        return new Vector3(point.y * _sqrt3On4 - point.x / 2, -point.x * _sqrt3On4 - point.y / 2, point.z);
    }
    
    private static Vector3 RotateCounterClockwise(Vector3 point)
    {
        // frccw (Qxyz) = (-qy * sqrt(3/4) - qx / 2, qx * sqrt(3/4) - qy / 2, qz)
        return new Vector3(-point.y * _sqrt3On4 - point.x / 2, point.x * _sqrt3On4 - point.y / 2, point.z);
    }
    
    private static Vector3 FrontViewRotation90Cw(Vector3 point)
    {
        // ff(Qxyz) = (qz, qy, -qx)
        return new Vector3(point.z, point.y, -point.x);
    }
    
    private static Vector3 FrontView2D(Vector3 point)
    {
        // fxz(Qxyz) = (qx, 0, qz)
        return new Vector3(point.x, 0, point.z);
    }
    
    private static Vector3 GetUnitVectorComponentY(Vector3 point)
    {
        // fy(Qxyz) = (qx, sqrt(1 - qx^2 - qz^2), qz) -- qx^2 + qz^2 must be smaller than or equal to 1
        float qxSq = Mathf.Pow(point.x, 2);
        float qzSq = Mathf.Pow(point.z, 2);
        if (qxSq + qzSq > 1) throw new InvalidOperationException("qx^2 + qz^2 must be smaller than or equal to 1");
        return new Vector3(point.x, Mathf.Sqrt(1 - qxSq - qzSq), point.z);
    }
    
    private static Vector3 GetUnitVectorComponentZ(Vector3 point)
    {
        float qxSq = Mathf.Pow(point.x, 2);
        float qySq = Mathf.Pow(point.y, 2);
        if (qxSq + qySq > 1) throw new InvalidOperationException("qx^2 + qz^2 must be smaller than or equal to 1");
        return new Vector3(point.x, point.y, Mathf.Sqrt(1 - qxSq - qySq));
    }
    
    // fL page10
    // To construct a line L with vector Lˆv and point Lp where the point Lp is the point on
    // the line that is the nearest to the origin, based on 2 vectors q and r, which are points on that line
    // fL (Qxyz, Rxyz) = (L^v, Lp) = ( ^(r-q), q - ^(r-q) * (q . ^(r-q) ) = (L^v, q - L^v * (q . L^v))
    private static Line ConstructLine(Vector3 q, Vector3 r)
    {
        Vector3 lv = (r - q).normalized;
        return new Line(lv, q - lv * (Vector3.Dot(q, lv)));
    }
    
    // fu page11
    // To find the intersection points of a line intersecting the unit sphere
    private static Vector3 FindIntersectionPoint(Line l)
    {
        float lpMag = l.Point.magnitude;
        //if (lpMag >= 1) throw new InvalidOperationException("Lp magnitude must be smaller than 1");
        if (lpMag > 1) return new Vector3(0,0,-100f);
        Vector3 val = l.Normal * Mathf.Sqrt(1 - Mathf.Pow(lpMag, 2));
        Vector3 point = l.Point + val;
        if (point.z < 0)
        {
            point = l.Point - val;
        }
        
        return point;
    }
    
    // fm page 11
    // 2D front view mirror point q along line L
    // fm(q, L) = fxz(q) + ff(^(fxz(L^v)) * (2 * (ff(^fxz(L^v)) . (fxz(Lp) - fxz(q))))
    private static Vector3 FrontViewMirrorPointAlongLine(Vector3 q, Line l)
    {
        Vector3 fxzq = FrontView2D(q);
        Vector3 fxzlp = FrontView2D(l.Point);
        Vector3 fxzlv = FrontView2D(l.Normal);
        Vector3 fxzlvUnitV = fxzlv.normalized;
        return fxzq + FrontViewRotation90Cw(fxzlvUnitV) * Vector3.Dot((2 * FrontViewRotation90Cw(fxzlvUnitV)), fxzlp - fxzq);
    }
    
    // fow
    // scale outward ellipse to circle
    // fow (Qxyz) = a ∗ (a · q) + ff(a) ∗ ((ff(a) · q)/s)
    private static Vector3 EllipseScaleOutward(Vector3 q)
    {
        float aDotq = Vector3.Dot(_a, q);
        return _a * aDotq + _ffa * (Vector3.Dot(_ffa, q) / _s);
    }
    
    // fiw
    // scale inward circle to ellipse
    // fiw (Qxyz) = a ∗ (a · q) + ff(a) ∗ ((ff(a) · q)/s)
    private static Vector3 CircleScaleInward(Vector3 q)
    {
        float aDotq = Vector3.Dot(_a, q);
        return _a * aDotq + _ffa * (Vector3.Dot(_ffa, q) * _s);
    }
    
     private static Vector3 CutPointOne(Vector3 seedPoint)
    {
        /*
        Vector3 sxz = FrontView2D(seedPoint); // fxz
        Line lps = ConstructLine(_p, sxz); // fL
        Vector3 t = FindIntersectionPoint(lps); // fu
        Vector3 u = FrontView2D(RotateCounterClockwise(t)); // fxz(frccw)
        Vector3 uow = EllipseScaleOutward(u); // fow
        Line lpowuow = ConstructLine(uow, _pow); // fL
        Vector3 c0ow = FindIntersectionPoint(lpowuow); // fu
        Vector3 c0xz = CircleScaleInward(c0ow); // fiw
        Vector3 c0 = GetUnitVectorComponent(c0xz); // fy
        return c0;
        */

        return GetUnitVectorComponentY(
            CircleScaleInward(
                FindIntersectionPoint(
                    ConstructLine(
                        EllipseScaleOutward(
                            FrontView2D(
                                RotateCounterClockwise(
                                    FindIntersectionPoint(
                                        ConstructLine(_p, FrontView2D(seedPoint)))))), 
                        _pow))));
    }

    private static Vector3 CutPointTwo3DMethod(Vector3 c0)
    {
        c0 = RotateCounterClockwise(c0);
        c0[1] *= -1;
        return c0;
    }

    private static void AddSlicingPlanes(Vector3 cutPoint, ref List<Plane> paramPlanes)
    {
        Vector3 n = new Vector3(_p.z - cutPoint.z, 0, cutPoint.x - _p.x).normalized;
        Plane c = new Plane(n, Vector3.Dot(n, _p));
        paramPlanes.Add(c);
        paramPlanes.Add(new Plane(RotateClockwise(c.UnitVector), c.DistToOrigin));     // Clockwise + 1
        paramPlanes.Add(new Plane(RotateCounterClockwise(c.UnitVector), c.DistToOrigin));   // anticlockwise + 2
    }

    private static Vector3 CalculateGridPoints(Plane a, Plane b)
    {
        Vector3 l = Vector3.Cross(a.UnitVector, b.UnitVector).normalized;
        Line f = new Line(Vector3.Cross(a.UnitVector, l).normalized, a.UnitVector * a.DistToOrigin);
        Line g = new Line(Vector3.Cross(b.UnitVector, l).normalized, b.UnitVector * b.DistToOrigin);
        Vector3 lp = g.Point + g.Normal * (Vector3.Dot(f.Point - g.Point, a.UnitVector) / Vector3.Dot(a.UnitVector, g.Normal));

        Line ll = new Line(l, lp);
        Vector3 t = FindIntersectionPoint(ll);
        return t;
    }

    #endregion

    // Line, Plane
    #region Struct Types

    // a line L is defined by its normal vector v and the point p on the line where it is nearest to the origin.
    // A line can be constructed as: L = (L^v, Lp)
    private struct Line
    {
        public Vector3 Normal { get; }
        public Vector3 Point { get; }

        public Line(Vector3 normal, Vector3 point)
        {
            this.Normal = normal;
            this.Point = point;
        }
    }
    
    // a plane P is defined by its normal vector in unit vector ˆv, and distance d to the origin (0, 0, 0)
    // Hence the point on the plane nearest to the Cartesian origin can be found by using Pˆv∗ Pd
    // To refer to a planes component we use the following:
    //      Pˆv: the unit vector component of the plane, containing an x, y and z component.
    //      Pd: the minimal distance from the origin to the plane.
    // A plane can be constructed as P = (Pˆv,Pd).
    private struct Plane
    {
        public Vector3 UnitVector { get; }
        public float DistToOrigin { get; }

        public Plane(Vector3 v, float distToOrigin)
        {
            this.UnitVector = v;
            this.DistToOrigin = distToOrigin;
        }
    }

    class Vpair
    {
        public Vector3 vec;
        public int count;
        public int index;

        public Vpair(Vector3 a, int b, int c)
        {
            this.vec = a;
            this.count = b;
            this.index = c;
        }
    }
    class Axes
    {
        public Vector3 x;
        public Vector3 y;
        public Vector3 z;

        public Axes(Vector3 a, Vector3 b, Vector3 c)
        {
            this.x = a;
            this.y = b;
            this.z = c;
        }
    }
    #endregion

    /*  Adapted From glMatrix Library
 * Creates a matrix from a given angle around a given axis
 * This is equivalent to (but much faster than):
 * @param {rad} rad the angle to rotate the matrix by
 * @param {axis} axis the axis to rotate around
 * @returns {mat4} ret
 */
    private Matrix4x4 fromRotation(float rad, Vector3 axis) 
    {
        float x = axis[0];
        float y = axis[1];
        float z = axis[2];
        float len = Mathf.Sqrt(x*x + y*y + z*z);
        float s, c, t;
        Matrix4x4 ret = new Matrix4x4();
        ret[0] = 0;
        if (len < 1e-7) {
            return ret;
        }
        len = 1 / len;
        x *= len;
        y *= len;
        z *= len;
        s = Mathf.Sin(rad);
        c = Mathf.Cos(rad);
        t = 1 - c;
        // Perform rotation-specific matrix multiplication
        ret[0] = x * x * t + c;
        ret[1] = y * x * t + z * s;
        ret[2] = z * x * t - y * s;
        ret[3] = 0;
        ret[4] = x * y * t - z * s;
        ret[5] = y * y * t + c;
        ret[6] = z * y * t + x * s;
        ret[7] = 0;
        ret[8] = x * z * t + y * s;
        ret[9] = y * z * t - x * s;
        ret[10] = z * z * t + c;
        ret[11] = 0;
        ret[12] = 0;
        ret[13] = 0;
        ret[14] = 0;
        ret[15] = 1;
        return ret;
    }
    private void ApplyMat4toAxis(ref Matrix4x4 l, ref Axes a)
    {
        a.x = l.MultiplyPoint3x4(a.x);
        a.y = l.MultiplyPoint3x4(a.y);
        a.z = l.MultiplyPoint3x4(a.z);
        return;
    }
    private void rotateAction(int gd_cnt, ref int[] is_edge, ref Matrix4x4 m, ref List<int> trilist)
    {
        int[] tritemp = new int[gd_cnt];
        for(int i = 0; i < gd_cnt; ++i)
        {
            Vector3 newP = m.MultiplyPoint3x4(geodesic_grid_base[i]);
            if(is_edge[i] > 0)
            {
                bool found = false;
                for(int j = 0; j < duplicated_points.Count; ++j)
                {
                    if(Vector3.Distance(newP, duplicated_points[j].vec) < 3e-4f) // == does NOT work
                    {
                        found = true;
                        duplicated_points[j].count--;
                        tritemp[i] = duplicated_points[j].index;
                        if(duplicated_points[j].count <= 0)
                        {
                            duplicated_points.RemoveAt(j);
                        }
                        break;
                    }
                }

                if(!found)
                {
                    tritemp[i] = _geodesicGrid.Count;
                    duplicated_points.Add(new Vpair(newP, is_edge[i], _geodesicGrid.Count));
                    _geodesicGrid.Add(newP);
                }
            }
            else
            {
                tritemp[i] = _geodesicGrid.Count;
                _geodesicGrid.Add(newP);
            }
        }
        int columnSize = (int)Mathf.Pow(2, _generations + 1) + 1;
        int offset = 0;
        while (columnSize > 1)
        {
            for (int i = 0; i < columnSize - 1; i++)
            {
                int j = i + 1;
                trilist.Add(tritemp[i + offset]);
                trilist.Add(tritemp[j + offset]);
                trilist.Add(tritemp[i + offset + columnSize]);

                if (offset != 0)
                {
                    trilist.Add(tritemp[j + offset]);
                    trilist.Add(tritemp[i + offset]);
                    trilist.Add(tritemp[i + offset - columnSize]);
                }
            }
            offset += columnSize;
            columnSize--;
        }
    }
    
    // DisplayPoints (paper implementation)
    #region PointsCalculation
    
    private void CreateMesh()
    {
        _allcutPoints.Clear();
        _geodesicGrid.Clear();
        duplicated_points.Clear();
        geodesic_grid_base.Clear();

        // list to store new points, start with seedPoint
        Queue<Vector3> cutPoints = new Queue<Vector3>();
        List<Plane> planes = new List<Plane>();
        cutPoints.Enqueue(_m);
        _allcutPoints.Add(_m);
        _allcutPoints.Add(_b);
        
        Vector3 cp;
        Vector3 t;

        for (int i = 0; i < _generations; i++)        // For every one cutpoint we generate two more 
        {
            int val = cutPoints.Count;
            for (int j = 0; j < val; j++)
            {
                cp = cutPoints.Peek();
                Vector3 c0 = CutPointOne(cp);
                cutPoints.Enqueue(c0);
                _allcutPoints.Add(c0);
                Vector3 c1 = CutPointTwo3DMethod(c0);
                cutPoints.Enqueue(c1);
                _allcutPoints.Add(c1);
                cutPoints.Dequeue();
            }
        }
        _allcutPoints.Sort((a, b) => b[1].CompareTo(a[1]));
        
        foreach (var point in _allcutPoints)
        {
            AddSlicingPlanes(point, ref planes);  // calculate all the planes and add them into the list for next step's use
        }
        
        for(int i = 0; i < _allcutPoints.Count; ++i)        
        {
            for(int j = 0; j < _allcutPoints.Count; ++j)
            {
                if(i + j >= _allcutPoints.Count+1) break;                  // pruning the process so that we don't get extra points
                t = CalculateGridPoints(planes[i*3+2], planes[j*3]);      // Picking two relevant planes to calculate grid point
                if(t[2] >= 0)
                {
                    _geodesicGrid.Add(t);
                }
            }
            if(i == 0)          
            {
                _geodesicGrid.Add(_a);

            }
            if(i == _allcutPoints.Count - 1)
            {
                _geodesicGrid.Add(_bb);
            }
        }

        // ----------------------------------------------------------
        geodesic_grid_base = _geodesicGrid;  // Base Triangle that has "base" coordinates
        
        int[] is_edge = new int[geodesic_grid_base.Count];
        int gd_cnt = geodesic_grid_base.Count;
        int side_max_cnt = (int)Mathf.Pow(2, (float)_generations + 1) + 1;
        int dup_avoid;
        int dup_jump;
        dup_avoid = 0;
        dup_jump = side_max_cnt - 1;
        while(dup_avoid < gd_cnt && dup_jump > 0)    // Remove duplicated grid points along three edges
        {
            if(dup_avoid == 0 || dup_avoid == side_max_cnt -1 || dup_avoid == gd_cnt - 1)
            {
                duplicated_points.Add(new Vpair(geodesic_grid_base[dup_avoid], 4, dup_avoid));
                is_edge[dup_avoid] = 4;
            }
            else
            {
                duplicated_points.Add(new Vpair(geodesic_grid_base[dup_avoid], 1, dup_avoid));
                is_edge[dup_avoid] = 1;
            }

            ++dup_avoid;

            if(dup_avoid == 0 || dup_avoid == side_max_cnt -1 || dup_avoid == gd_cnt - 1)
            {
                duplicated_points.Add(new Vpair(geodesic_grid_base[dup_avoid], 4, dup_avoid));
                is_edge[dup_avoid] = 4;
            }
            else
            {
                duplicated_points.Add(new Vpair(geodesic_grid_base[dup_avoid], 1, dup_avoid));
                is_edge[dup_avoid] = 1;
            }

            if(dup_avoid < side_max_cnt)
            {
                ++dup_avoid;
            }
            else
            {
                dup_jump--;
                dup_avoid += dup_jump;
            }
        }
        print("Before rotation Duplicated edges has a total of: " + duplicated_points.Count + " Grid Points");
        // ---------------------------------------------------------
        print("Each side has: " + side_max_cnt + " Grid Points, base grid has a total of: " + geodesic_grid_base.Count + " Grid Points");

        List<int> triangleList = new List<int>();

        int columnSize = (int)Mathf.Pow(2, _generations + 1) + 1;
        int offset = 0;
        while (columnSize > 1)               // add the original triangle face to the mesh
        {
            for (int i = 0; i < columnSize - 1; i++)
            {
                int j = i + 1;
                triangleList.Add(i + offset);
                triangleList.Add(j + offset);
                triangleList.Add(i + offset + columnSize);

                if (offset != 0)
                {
                    triangleList.Add(j + offset);
                    triangleList.Add(i + offset);
                    triangleList.Add(i + offset - columnSize);
                }
            }

            offset += columnSize;
            columnSize--;
        }

        for(int i = 1; i < _AllAxes.Count; ++i)         // Rotate 19 times to each triangle surfaces using calculated axes
        {
            Axes cardinal = new Axes(new Vector3(1,0,0), new Vector3(0,1,0), new Vector3(0,0,1));
            Vector3 reference_axis = Vector3.Cross(cardinal.z, _AllAxes[i].z).normalized;
            
            float rotangle = Vector3.Angle(_AllAxes[i].z, cardinal.z);
            Matrix4x4 m1 = fromRotation(Mathf.Deg2Rad * rotangle, reference_axis);
            ApplyMat4toAxis(ref m1, ref cardinal);
            
            rotangle = Vector3.Angle(_AllAxes[i].x, cardinal.x);
            Matrix4x4 m2 = fromRotation(Mathf.Deg2Rad * rotangle, cardinal.z);
            ApplyMat4toAxis(ref m2, ref cardinal);
            m2*= m1;
            rotateAction(gd_cnt, ref is_edge , ref m2, ref triangleList);  //rotate and add triangles to meshes
        }
        
        
        print("Rotated grid has a total of: " + _geodesicGrid.Count + " Grid Points");
        print("After rotation Duplicated edges has a total of: " + duplicated_points.Count + " Grid Points");

        Vector3[] vertices = _geodesicGrid.ToArray();
        Color[] vertexColors = new Color[vertices.Length];
        
        Vector2[] uvs = new Vector2[_geodesicGrid.Count];
        for(int i = 0; i < uvs.Length; ++i)
        {
            uvs[i] = new Vector2(
              0.5f + Mathf.Atan2(_geodesicGrid[i].x, _geodesicGrid[i].z)/ (2*Mathf.PI)
            , 0.5f + Mathf.Asin(_geodesicGrid[i].y)/(Mathf.PI)
            );
        }

        for (int i = 0; i < vertices.Length; i++)  //Applying heightmap to the sphere using uv coords
        {
            Color heightClr = heightmap.GetPixel((int)((uvs[i].x) * heightmap.width), (int)((1 - uvs[i].y) * heightmap.height));
            Color actualClr = colormap.GetPixel((int)((uvs[i].x) * colormap.width), (int)((1 - uvs[i].y) * colormap.height));
            float he = heightClr.r * 0.025f;
            Vector3 v = vertices[i].normalized;
            vertices[i] = new Vector3(vertices[i].x + v.x* he, vertices[i].y + v.y* he, vertices[i].z + + v.z* he );
            vertexColors[i] = actualClr;
        }
        
        Mesh mesh = new Mesh();
        if (vertices.Length >= Mathf.Pow(2, 16))
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangleList.ToArray();
        mesh.colors = vertexColors;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        
        _meshes.AddGeneration(mesh, _geodesicGrid.ToArray());
    }

    #endregion


    // simple icosphere creation
    #region Unused

    /*
    // default easy setup of an icosphere
    void IcoMesh()
    {
        float u = _lHalf;
        float v = Mathf.Sqrt(1 - _lHalfSq);

        _vertices = new Vector3[] 
        {
            new Vector3(0, -v, u),
            new Vector3(v, -u, 0),
            new Vector3(u, 0, v),
            new Vector3(v, u, 0),
            new Vector3(0, v, u),
            new Vector3(0, v, -u),
            new Vector3(-v, u, 0),
            new Vector3(-u, 0, -v),
            new Vector3(-v, -u, 0),
            new Vector3(0, -v, -u),
            new Vector3(-u, 0, v),
            new Vector3(u, 0, -v)
        };

        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;

        _mesh.vertices = _vertices;

        _triangles = new int[]
        {
            0,1,2,
            2,1,3,
            2,3,4,
            4,3,5,
            4,5,6,
            6,5,7,
            6,7,8,
            8,7,9,
            0,2,10,
            3,1,11,
            5,3,11,
            7,5,11,
            9,7,11,
            2,4,10,
            4,6,10,
            6,8,10,
            8,9,0,
            0,9,1,
            1,9,11,
            8,0,10
        };

        _mesh.triangles = _triangles;

        _vertexColors = new Color[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            if (_lerpColors)
            {
                _vertexColors[i] = Color.Lerp(_color1, _color2, (float)(i + 1) / _vertices.Length);
            }
            else
            {
                _vertexColors[i] = new Color(UnityEngine.Random.Range(0, 255) / 255f, UnityEngine.Random.Range(0, 255) / 255f,
                    UnityEngine.Random.Range(0, 255) / 255f);
            }
        }

        _mesh.colors = _vertexColors;
    }
    
    [ShowInInspector, FoldoutGroup("Mesh"), PropertyRange(1, 20), OnValueChanged("UpdateFaceColor")]
    private int _selectedFace = 1;
    private Color[] _selectedFacePreviousColors;
    private void UpdateFaceColor()
    {
        if (_triangle != null && _selectedFacePreviousColors != null)
        {
            _vertexColors[_triangle[0]] = _selectedFacePreviousColors[0];
            _vertexColors[_triangle[1]] = _selectedFacePreviousColors[1];
            _vertexColors[_triangle[2]] = _selectedFacePreviousColors[2];
            UpdateMesh();
        }

        int position = (_selectedFace - 1) * 3;
        _triangle = new[]
        {
            _mesh.triangles[position],
            _mesh.triangles[position + 1],
            _mesh.triangles[position + 2]
        };

        _selectedFacePreviousColors = new[]
        {
            _mesh.colors[_triangle[0]],
            _mesh.colors[_triangle[1]],
            _mesh.colors[_triangle[2]]
        };
        
        _vertexColors[_triangle[0]] = Color.white;
        _vertexColors[_triangle[1]] = Color.white;
        _vertexColors[_triangle[2]] = Color.white;
        UpdateMesh();
    }
    */
    #endregion
}
