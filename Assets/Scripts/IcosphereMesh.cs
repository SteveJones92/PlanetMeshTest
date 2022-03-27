using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using System.Timers;


[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class IcosphereMesh : MonoBehaviour
{
    [Title("Length, Height, Z")]
    [ShowInInspector, ReadOnly, FoldoutGroup("L, H, Z"), HideLabel, SuffixLabel("L")]
    private float _l;
    [ShowInInspector, ReadOnly, FoldoutGroup("L, H, Z"), HideLabel, SuffixLabel("H")]
    private float _h;
    [ShowInInspector, ReadOnly, FoldoutGroup("L, H, Z"), HideLabel, SuffixLabel("Z")]
    private float _z;
    
    [Title("L^2, L/2, (L/2)^2, h/3")]
    // common use
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Calcuations"), HideLabel, SuffixLabel("L^2")]
    private float _lSq;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Calcuations"), HideLabel, SuffixLabel("L/2")]
    private float _lHalf;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Calcuations"), HideLabel, SuffixLabel("(L/2)^2")]
    private float _lHalfSq;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Calcuations"), HideLabel, SuffixLabel("h/3")]
    private float _hDiv3;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Calcuations"), HideLabel, SuffixLabel("sqrt(3/4)")]
    private float _sqrt3On4;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Calcuations"), HideLabel, SuffixLabel("cos(PI/5)")]
    private float _s;
    
    [Title("vertices of the triangle")]
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("a")]
    // a = (ax, ay, az) = (-2h/3, 0, z) page5
    private Vector3 _a;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("b")]
    // b = (bx, by, bz) = (h/3, l/2, z) page5
    private Vector3 _b;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("bb")]
    // bb = (bx, b.-y, bz)
    private Vector3 _bb;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("p")]
    // p = (px, py, pz) = (ax, 0, -2az) page5
    private Vector3 _p;
    [Title("midpoints for the spherical icosahedron triangle ribs")]
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("magBxBz")]
    private float _magBxBz;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("c")]
    // c = (cx, cy, cz) = (bx / |bx, bz|, 0, bz / |bx, bz|)
    private Vector3 _c;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("m")]
    // m = (mx, my, mz) = (-cx / 2, sqrt(3 / 4) cx, cz)
    private Vector3 _m;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("fv90Cw_a")]
    // FrontViewRotation90Cw of _a
    private Vector3 _ffa;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("mow")]
    // outward scaled m
    private Vector3 _mow;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("pow")]
    // outward scaled p
    private Vector3 _pow;

    [ShowInInspector, FoldoutGroup("Mesh"),  InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
    private Mesh _mesh;
    [ShowInInspector, FoldoutGroup("Mesh")]
    private int[] _triangles;
    [ShowInInspector, FoldoutGroup("Mesh")]
    private Vector3[] _vertices;
    [ShowInInspector, FoldoutGroup("Mesh"), ColorPalette("Tropical")]
    private Color[] _vertexColors;

    //[ShowInInspector, FoldoutGroup("Mesh"), PropertyRange(1, 20), OnValueChanged("UpdateFaceColor")]
    private int _selectedFace = 1;
    public static double ConvertDegreesToRadians (double degrees)
    {
        double radians = (Math.PI / 180) * degrees;
        return (radians);
    }
    //[ShowInInspector]
    private Color[] _selectedFacePreviousColors;
    //[ShowInInspector]
    private int[] _triangle;
    private static System.Timers.Timer aTimer;
    public static int IntPower(int x, int power)
    {
        if (power == 0) return 1;
        if (power == 1) return x;

        int n = 15;
        while ((power <<= 1) >= 0) n--;

        int tmp = x;
        while (--n > 0)
            tmp = tmp * tmp * 
                 (((power <<= 1) < 0)? x : 1);
        return tmp;
    }        
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

    [Title("Functions")]
    [ShowInInspector]
    private void Start()
    {
        _l = Length();
        _lHalf = _l / 2;
        _lSq = Mathf.Pow(_l, 2);
        _lHalfSq = Mathf.Pow(_lHalf, 2);
        _h = Height();
        _hDiv3 = _h / 3;
        _z = ZValue();
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
        _mow = EllipseScaleOutward(_m);
        _pow = EllipseScaleOutward(_p);
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        DisplayPoints();
        //IcoMesh();
        //UpdateFaceColor();
    }

    float Length()
    {
        // l = 4 / sqrt(10 + sqrt(20)) page4
        return 4 / Mathf.Sqrt(10 + Mathf.Sqrt(20));
    }

    float Height()
    {
        //float l = length();
        // h = sqrt(l^2 - (l/2)^2) page5
        return Mathf.Sqrt(_lSq - _lHalfSq);
    }

    float ZValue()
    {
        //float h = height();
        // z = sqrt(1 - (2h/3)^2 page5
        return Mathf.Sqrt(1 - Mathf.Pow(2 * _h / 3, 2));
    }

    Vector3 RotateClockwise(Vector3 point)
    {
        // frcw (Qxyz) = (qy * sqrt(3/4) - qx / 2, -qx * sqrt(3/4) - qy / 2, qz)
        return new Vector3(point.y * _sqrt3On4 - point.x / 2, -point.x * _sqrt3On4 - point.y / 2, point.z);
    }

    Vector3 RotateCounterClockwise(Vector3 point)
    {
        // frccw (Qxyz) = (-qy * sqrt(3/4) - qx / 2, qx * sqrt(3/4) - qy / 2, qz)
        return new Vector3(-point.y * _sqrt3On4 - point.x / 2, point.x * _sqrt3On4 - point.y / 2, point.z);
    }

    Vector3 FrontViewRotation90Cw(Vector3 point)
    {
        // ff(Qxyz) = (qz, qy, -qx)
        return new Vector3(point.z, point.y, -point.x);
    }

    Vector3 FrontView2D(Vector3 point)
    {
        // fxz(Qxyz) = (qx, 0, qz)
        return new Vector3(point.x, 0, point.z);
    }

    Vector3 GetUnitVectorComponent(Vector3 point)
    {
        // fy(Qxyz) = (qx, sqrt(1 - qx^2 - qz^2), qz) -- qx^2 + qz^2 must be smaller than or equal to 1
        float qxSq = Mathf.Pow(point.x, 2);
        float qzSq = Mathf.Pow(point.z, 2);
        if (qxSq + qzSq > 1) throw new InvalidOperationException("qx^2 + qz^2 must be smaller than or equal to 1");
        return new Vector3(point.x, Mathf.Sqrt(1 - qxSq - qzSq), point.z);
    }
    
    Vector3 GetUnitVectorComponentZ(Vector3 point)
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
    Line ConstructLine(Vector3 q, Vector3 r)
    {
        Vector3 lv = (r - q).normalized;
        return new Line(lv, q - lv * (Vector3.Dot(q, lv)));
    }
    
    // fu page11
    // To find the intersection points of a line intersecting the unit sphere
    Vector3 FindIntersectionPoint(Line l)
    {
        float lpMag = l.point.magnitude;
        //if (lpMag >= 1) throw new InvalidOperationException("Lp magnitude must be smaller than 1");
        if (lpMag > 1) return new Vector3(0,0,-100f);
        Vector3 val = l.normal * Mathf.Sqrt(1 - Mathf.Pow(lpMag, 2));
        Vector3 point = l.point + val;
        if (point.z < 0)
        {
            point = l.point - val;
        }
        
        return point;
    }
    
    // fm page 11
    // 2D front view mirror point q along line L
    // fm(q, L) = fxz(q) + ff(^(fxz(L^v)) * (2 * (ff(^fxz(L^v)) . (fxz(Lp) - fxz(q))))
    Vector3 FrontViewMirrorPointAlongLine(Vector3 q, Line l)
    {
        Vector3 fxzq = FrontView2D(q);
        Vector3 fxzlp = FrontView2D(l.point);
        Vector3 fxzlv = FrontView2D(l.normal);
        Vector3 fxzlvUnitV = fxzlv.normalized;
        return fxzq + FrontViewRotation90Cw(fxzlvUnitV) * Vector3.Dot((2 * FrontViewRotation90Cw(fxzlvUnitV)), fxzlp - fxzq);
    }
    
    // fow
    // scale outward ellipse to circle
    // fow (Qxyz) = a ∗ (a · q) + ff(a) ∗ ((ff(a) · q)/s)
    Vector3 EllipseScaleOutward(Vector3 q)
    {
        float aDotq = Vector3.Dot(_a, q);
        return _a * aDotq + _ffa * (Vector3.Dot(_ffa, q) / _s);
    }
    
    // fiw
    // scale inward circle to ellipse
    // fiw (Qxyz) = a ∗ (a · q) + ff(a) ∗ ((ff(a) · q)/s)
    Vector3 CircleScaleInward(Vector3 q)
    {
        float aDotq = Vector3.Dot(_a, q);
        return _a * aDotq + _ffa * (Vector3.Dot(_ffa, q) * _s);
    }

    Vector3 CutPointOne(Vector3 seedPoint)
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

        return GetUnitVectorComponent(
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

    Vector3 CutPointTwo3DMethod(Vector3 c0)
    {
        c0 = RotateCounterClockwise(c0);
        c0[1] *= -1;
        return c0;
    }
    
    Vector3 CutPointTwo2DMethod(Vector3 c0)
    {
        // omit fy (GetUnitVectorComponent)
        // not working
        return GetUnitVectorComponent(CircleScaleInward(FrontViewMirrorPointAlongLine(EllipseScaleOutward(c0), ConstructLine(Vector3.zero, _mow))));
    }
    

    Vector3[] FindCutPoints(Vector3 seedPoint)
    {
        // doubles, can initialize array just fine if needed
        List<Vector3> newPoints = new List<Vector3>();

        Vector3 c0 = CutPointOne(seedPoint);
        //Vector3 c1 = CutPointTwo3DMethod(c0);
        newPoints.Add(c0);
        Vector3 c4 = RotateCounterClockwise(c0);
        c4 = new Vector3(c4.x, -c4.y, c4.z);
        newPoints.Add(c4);
        return newPoints.ToArray();
    }


    Vector3[] GetNewPoints(Vector3 side1, Vector3 side2, int num)
    {
        //float leftRightRange = _b.x - RotateClockwise(_m).x;
        float portion = 1 / (float)(num + 1);
        portion *= 2;
        float val = 1 - portion;
        
        Vector3[] points = new Vector3[num];
        for (int i = 0; i < num; i++)
        {
            Vector3 newPoint = new Vector3(side1.x, side1.y * val, side1.z);
            newPoint = GetUnitVectorComponentZ(newPoint);
            //newPoint.x += 1 * (newPoint.x - RotateClockwise(_m).x) / leftRightRange;
            points[i] = newPoint;
            val -= portion;
        }
        
        return points;
    }

    Plane[] CalculateSlicingPlanes(Vector3 cutPoint)
    {
        Vector3 n = new Vector3(_p.z - cutPoint.z, 0, cutPoint.x - _p.x).normalized;
        Plane c = new Plane(n, Vector3.Dot(n, _p));
        Plane d = new Plane(RotateClockwise(c.unitVector), c.distToOrigin);
        Plane e = new Plane(RotateCounterClockwise(c.unitVector), c.distToOrigin);
        return new Plane[] { c, d, e };
    }

    void AddSlicingPlanes(Vector3 cutPoint, ref List<Plane> param_planes)
    {
        Vector3 n = new Vector3(_p.z - cutPoint.z, 0, cutPoint.x - _p.x).normalized;
        Plane c = new Plane(n, Vector3.Dot(n, _p));
        param_planes.Add(c);
        param_planes.Add(new Plane(RotateClockwise(c.unitVector), c.distToOrigin));     // Clockwise + 1
        param_planes.Add(new Plane(RotateCounterClockwise(c.unitVector), c.distToOrigin));   // anticlockwise + 2
        return;
    }
    Vector3 CalculateGridPoints(Plane a, Plane b)
    {
        Vector3 l = Vector3.Cross(a.unitVector, b.unitVector).normalized;
        Line f = new Line(Vector3.Cross(a.unitVector, l).normalized, a.unitVector * a.distToOrigin);
        Line g = new Line(Vector3.Cross(b.unitVector, l).normalized, b.unitVector * b.distToOrigin);
        Vector3 lp = g.point + g.normal * (Vector3.Dot(f.point - g.point, a.unitVector) / Vector3.Dot(a.unitVector, g.normal));

        Line ll = new Line(l, lp);
        Vector3 t = FindIntersectionPoint(ll);
        return t;
    }
    // a line L is defined by its normal vector v and the point p on the line where it is nearest to the origin.
    // A line can be constructed as: L = (L^v, Lp)
    struct Line
    {
        public Vector3 normal { get; }
        public Vector3 point { get; }

        public Line(Vector3 normal, Vector3 point)
        {
            this.normal = normal;
            this.point = point;
        }
    }
    
    // a plane P is defined by its normal vector in unit vector ˆv, and distance d to the origin (0, 0, 0)
    // Hence the point on the plane nearest to the Cartesian origin can be found by using Pˆv∗ Pd
    // To refer to a planes component we use the following:
    //      Pˆv: the unit vector component of the plane, containing an x, y and z component.
    //      Pd: the minimal distance from the origin to the plane.
    // A plane can be constructed as P = (Pˆv,Pd).
    struct Plane
    {
        public Vector3 unitVector { get; }
        public float distToOrigin { get; }

        public Plane(Vector3 v, float distToOrigin)
        {
            this.unitVector = v;
            this.distToOrigin = distToOrigin;
        }
    }
    [ShowInInspector, OnValueChanged("DisplayPoints"), CustomValueDrawer("MinMaxGenerations")]
    private float _generations = 1;
    
    [ShowInInspector, OnValueChanged("DisplayPoints"), MinValue(1)]
    private float _perlinXYScale = 1;
    [ShowInInspector, OnValueChanged("DisplayPoints"), MinValue(1)]
    private float _perlinOutputScale = 1;

    private static int MinMaxGenerations(int value, GUIContent label)
    {
        return EditorGUILayout.IntSlider(label, value, 0, 8);
    }
    
    private List<Vector3> _displayPoints = new List<Vector3>();
    private List<Vector3> geodesic_grid = new List<Vector3>();     // Result array
    private List<Vector3> allcutPoints = new List<Vector3>();      // Array that stores all the generated cutpoints including one of the apex point
    [ShowInInspector]
    void DisplayPoints()
    {
        float timer = Time.realtimeSinceStartup;
        _displayPoints.Clear();
        allcutPoints.Clear();
        geodesic_grid.Clear();
        // 3 corners
        //_displayPoints.Add(_a);
        //_displayPoints.Add(_b);
        //_displayPoints.Add(_bb);
        //_displayPoints.Add(Vector3.forward);

        // list to store new points, start with seedPoint
        Queue<Vector3> cutPoints = new Queue<Vector3>();
        List<Plane> planes = new List<Plane>();
        cutPoints.Enqueue(_m);
        allcutPoints.Add(_m);
        allcutPoints.Add(_b);
        
        Vector3 cp;
        Vector3 t;
        
        
        for (int i = 0; i < (int) _generations; i++)        // For every one cutpoint we generate two more 
        {
            int val = cutPoints.Count;
            for (int j = 0; j < val; j++)
            {
                cp = cutPoints.Peek();
                Vector3 c0 = CutPointOne(cp);
                cutPoints.Enqueue(c0);
                allcutPoints.Add(c0);
                Vector3 c1 = CutPointTwo3DMethod(c0);
                cutPoints.Enqueue(c1);
                allcutPoints.Add(c1);
                cutPoints.Dequeue();
            }
        }
        allcutPoints.Sort(delegate(Vector3 a, Vector3 b)      // Sort the cut points by Y value
        {
            return b[1].CompareTo(a[1]);
        });

        int planecount = 0;
        for(int i = 0; i < allcutPoints.Count; ++i)
        {
            AddSlicingPlanes(allcutPoints[i], ref planes);  // calculate all the planes and add them into the list for next step's use
            planecount += 3;
        }
        
        for(int i = 0; i < allcutPoints.Count; ++i)        
        {
            for(int j = 0; j < allcutPoints.Count; ++j)
            {
                if(i + j >= allcutPoints.Count+1) break;                  // pruning the process so that we don't get extra points
                t = CalculateGridPoints(planes[i*3+2], planes[j*3]);      // Picking two relevant planes to calculate grid point
                if(t[2] >= 0)
                {
                    geodesic_grid.Add(t);
                    _displayPoints.Add(t);
                }
            }
            if(i == 0)          
            {
                geodesic_grid.Add(_a);
                _displayPoints.Add(_a);

            }
            if(i == allcutPoints.Count - 1)
            {
                _displayPoints.Add(_bb);
                geodesic_grid.Add(_bb);
            }
        }
        //double sval = Math.Sin(ConvertDegreesToRadians(41.811f));
        //double cval = Math.Cos(ConvertDegreesToRadians(41.811f));
        //double sval1 = Math.Sin(ConvertDegreesToRadians(60f));
        //double cval1 = Math.Cos(ConvertDegreesToRadians(60f));
        Vector3 eulerAngles = new Vector3(0f, 41.811f ,60f);                       // Rotate y 41.811 degree and z 60 degree
        Quaternion rotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
        Matrix4x4 m = Matrix4x4.Rotate(rotation);
        for(int i = 0; i < geodesic_grid.Count; ++i)
        {
            //geodesic_grid[i] = m.MultiplyPoint3x4(geodesic_grid[i]);         // uncomment this to see the rotation
            //geodesic_grid[i] = new Vector3( geodesic_grid[i].x* (float)cval1 -  (float)sval1* geodesic_grid[i].y,  geodesic_grid[i].x* (float)sval1 +  (float)cval1* geodesic_grid[i].y, geodesic_grid[i].z);
            //geodesic_grid[i] = new Vector3( geodesic_grid[i][0]* (float)cval +  (float)sval* geodesic_grid[i][2], geodesic_grid[i].y, geodesic_grid[i][0]* -(float)sval +  (float)cval* geodesic_grid[i][2]);
            //_displayPoints.Add(geodesic_grid[i]);                            // uncomment this to see the rotation
        }

        _vertices = geodesic_grid.ToArray();
        
        for (int i = 0; i < _vertices.Length; i++)
        {
            _vertices[i] = new Vector3(_vertices[i].x, _vertices[i].y, _vertices[i].z + Mathf.PerlinNoise(_vertices[i].x * _perlinXYScale, _vertices[i].y * _perlinXYScale) / _perlinOutputScale);
        }
        
        List<int> triangleList = new List<int>();

        int columnSize = (int)Mathf.Pow(2, (int)_generations + 1) + 1;
        int offset = 0;
        // only go up to last 2
        while (columnSize > 1)
        {
            for (int i = 0; i < columnSize - 1; i++)
            {
                int j = i + 1;
                //Debug.Log(i + offset + " " + (j + offset) + " " + (i + offset + columnSize));
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

        _triangles = triangleList.ToArray();
        
        _vertexColors = new Color[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            _vertexColors[i] = Color.Lerp(Color.red, Color.blue, (float)(i + 1) / _vertices.Length);
            //_vertexColors[i] = new Color(UnityEngine.Random.Range(0, 255) / 255f, UnityEngine.Random.Range(0, 255) / 255f,
                //UnityEngine.Random.Range(0, 255) / 255f);
        }

        UpdateMesh();
        Debug.Log(Time.realtimeSinceStartup - timer);
    }
    
    private Vector3[] _newGridPoints;// = new List<Vector3>();
    [ShowInInspector]
    void DisplayGrid()
    {
        float timer = Time.realtimeSinceStartup;
        //_newGridPoints.Clear();
        // number of vertices that will finally be placed
        int numVerts = (int)(Mathf.Pow(2, (int)_generations + 1) + 1) * (int)(Mathf.Pow(2,(int)_generations) + 1);
        _newGridPoints = new Vector3[numVerts];
        // number of items in columns and row (defines the split)
        int columnSize = (int)Mathf.Pow(2, (int)_generations + 1) + 1;
        // amount to shift will always be the same
        float horizontalShift = .910593f / (columnSize - 1);
        // adjust down for diminishing height
        float height = 1.0514622f / 2f;
        // amount shifted
        int horizontalSpot = 0;
        int leftSideIndex = columnSize - 1;
        float x = 0.303531f;
        int index = 0;
        while (true)
        {
            float verticalShift = height / (columnSize - 1);
            if (columnSize == 1) verticalShift = 0f;
            float columnSpot = 0;
            for (int i = 0; i < columnSize; i++)
            {
                float y = height - (verticalShift * 2f) * columnSpot;
                
                Vector3 v = GetUnitVectorComponentZ(new Vector3(x > 0 ? x + GetOffsetX(y) * (x / 0.303531f) : x, y, 0f));
                _newGridPoints[index++] = v;
                //_newGridPoints.Add(v);
                columnSpot++;
            }

            horizontalSpot++;
            columnSize--;
            if (columnSize == 0) break;
            height = RotateCounterClockwise(_newGridPoints[leftSideIndex - horizontalSpot]).y;
            x = RotateCounterClockwise(_newGridPoints[leftSideIndex - horizontalSpot ]).x;
        }

        _vertices = _newGridPoints;//.ToArray();
        
        for (int i = 0; i < _vertices.Length; i++)
        {
            _vertices[i] = new Vector3(_vertices[i].x, _vertices[i].y, _vertices[i].z + Mathf.PerlinNoise(_vertices[i].x * _perlinXYScale, _vertices[i].y * _perlinXYScale) / _perlinOutputScale);
        }

        List<int> triangleList = new List<int>();

        columnSize = (int)Mathf.Pow(2, (int)_generations + 1) + 1;
        int offset = 0;
        // only go up to last 2
        while (columnSize > 1)
        {
            for (int i = 0; i < columnSize - 1; i++)
            {
                int j = i + 1;
                //Debug.Log(i + offset + " " + (j + offset) + " " + (i + offset + columnSize));
                triangleList.Add(j + offset);
                triangleList.Add(i + offset);
                triangleList.Add(i + offset + columnSize);

                if (offset != 0)
                {
                    triangleList.Add(i + offset);
                    triangleList.Add(j + offset);
                    triangleList.Add(i + offset - columnSize);
                }
            }

            offset += columnSize;
            columnSize--;
        }

        _triangles = triangleList.ToArray();

        _vertexColors = new Color[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            //_vertexColors[i] = Color.Lerp(Color.red, Color.blue, (float)(i + 1) / _vertices.Length);
            _vertexColors[i] = new Color(UnityEngine.Random.Range(0, 255) / 255f, UnityEngine.Random.Range(0, 255) / 255f,
                UnityEngine.Random.Range(0, 255) / 255f);
        }
        UpdateMesh();
        //Debug.Log(_newGridPoints.Count);
        Debug.Log(Time.realtimeSinceStartup - timer);
    }

    // curve offset from 2 points
    float GetOffsetX(float x)
    {
        return -(1 / 5.19f) * Mathf.Pow(x, 2) + 0.0532911f;
    }
    
    int timer_count = 0;
    [ShowInInspector]
    void AnimatedDisplay()
    {
        SetTimer();
        timer_count = 0;
        _displayPoints.Clear();
    }

    private void SetTimer()
    {
        aTimer = new System.Timers.Timer(200);
        // Hook up the Elapsed event for the timer. 
        aTimer.Elapsed += OnTimedEvent;
        aTimer.AutoReset = true;
        aTimer.Enabled = true;
    }

    private void OnTimedEvent(object source, ElapsedEventArgs e)
    {
        _displayPoints.Add(geodesic_grid[timer_count++]);
        if(timer_count >= geodesic_grid.Count)
        {
            aTimer.Stop();
        }
    }

    [ShowInInspector, ToggleLeft] private bool displayPoints = false;
    [ShowInInspector, ToggleLeft] private bool displayGrid = false;
    
    private Vector3 _t;
    private void OnDrawGizmos()
    {
        _t = transform.localScale;
        Gizmos.color = Color.green;
        if (displayPoints)
        {
            foreach (var point in _displayPoints)
            {
                Gizmos.DrawSphere(point, .005f);
            }
        }

        Gizmos.color = Color.blue;
        if (displayGrid)
        {
            foreach (var point in _newGridPoints)
            {
                Gizmos.DrawSphere(point, .005f);
            }
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(Vector3.forward, .01f);
    }

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
        /*
        mesh.normals = new Vector3[]
        {
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back
        };
        */
        
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
            _vertexColors[i] = Color.Lerp(Color.red, Color.blue, (float)(i + 1) / _vertices.Length);
        }

        _mesh.colors = _vertexColors;
    }

    [ShowInInspector]
    private void UpdateMesh()
    {
        _mesh.Clear();
        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
        _mesh.colors = _vertexColors;
    }
}
