using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Random = System.Random;


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
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("m2")]
    private Vector3 _m2;
    [ShowInInspector, ReadOnly, FoldoutGroup("Common Vectors"), HideLabel, SuffixLabel("m3")]
    private Vector3 _m3;
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
        _sqrt3On4 = Mathf.Sqrt(3 / 4f);
        _s = Mathf.Cos(Mathf.PI / 5);
        _a = new Vector3(-2 * _hDiv3, 0, _z);
        _b = new Vector3(_hDiv3, _lHalf, _z);
        _bb = new Vector3(_b.x, -_b.y, _b.z);
        _p = new Vector3(_a.x, 0, -2 * _a.z);
        _magBxBz = Mathf.Sqrt(Mathf.Pow(_b.x, 2) + Mathf.Pow(_b.z, 2));
        _c = new Vector3(_b.x / _magBxBz, 0, _b.z / _magBxBz);
        _m = new Vector3(-_c.x / 2, _sqrt3On4 * _c.x, _c.z);
        _m2 = RotateClockwise(_m);
        _m3 = RotateCounterClockwise(_m);
        _ffa = FrontViewRotation90Cw(_a);
        _mow = EllipseScaleOutward(_m);
        _pow = EllipseScaleOutward(_p);
        DisplayPoints();
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
        return Mathf.Sqrt(1 - Mathf.Pow(2 * _h / 3f, 2));
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
        float qxSq = Mathf.Pow(point.x, 2f);
        float qySq = Mathf.Pow(point.y, 2f);
        if (qxSq + qySq > 1) throw new InvalidOperationException("qx^2 + qz^2 must be smaller than or equal to 1");
        return new Vector3(point.x, point.y, Mathf.Sqrt(1f - qxSq - qySq));
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
        if (lpMag >= 1) throw new InvalidOperationException("Lp magnitude must be smaller than 1");
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

        return fxzq + FrontViewRotation90Cw(fxzlvUnitV) * Vector3.Dot((2 * fxzlvUnitV), fxzlp - fxzq);
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
        return RotateCounterClockwise(c0);
    }
    
    Vector3 CutPointTwo2DMethod(Vector3 c0)
    {
        // omit fy (GetUnitVectorComponent)
        // not working
        return CircleScaleInward(FrontViewMirrorPointAlongLine(EllipseScaleOutward(c0), ConstructLine(Vector3.zero, _mow)));
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

    Plane[] CalculateSlicingPlanes(Vector3 cutPoint)
    {
        Vector3 n = new Vector3(_p.z - cutPoint.z, 0, cutPoint.x - _p.x).normalized;
        Plane c = new Plane(n, Vector3.Dot(n, _p));
        Plane d = new Plane(RotateClockwise(c.unitVector), c.distToOrigin);
        Plane e = new Plane(RotateCounterClockwise(c.unitVector), c.distToOrigin);
        return new Plane[] { c, d, e };
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

    Vector3[] GetNewPoints(Vector3 side1, int num)
    {
        float portion = 1 / (float)(num + 1);
        portion *= 2;
        float val = 1 - portion;
        
        Vector3[] points = new Vector3[num];
        for (int i = 0; i < num; i++)
        {
            Vector3 newPoint = new Vector3(side1.x, side1.y * val, side1.z);
            newPoint = GetUnitVectorComponentZ(newPoint);
            points[i] = newPoint;
            val -= portion;
        }
        
        return points;
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

    private static int MinMaxGenerations(int value, GUIContent label)
    {
        return EditorGUILayout.IntSlider(label, value, 0, 5);
    }
    
    private List<Vector3> _displayPoints = new List<Vector3>();
    [ShowInInspector]
    void DisplayPoints()
    {
        _displayPoints.Clear();
        // center
        //_displayPoints.Add(Vector3.forward);

        // list to store new points, start with seedPoint
        List<Vector3> cutPoints = new List<Vector3>();
        cutPoints.Add(_m);
        
        Vector3 cp;
        int pos = 0;
        for (int i = 0; i < (int) _generations; i++)
        {
            int val = cutPoints.Count;
            for (int j = pos; j < val; j++)
            {
                cp = cutPoints[j];
                pos++;
                
                foreach (var point in FindCutPoints(cp))
                {
                    cutPoints.Add(point);
                }
            }
        }
   
        cutPoints.Sort((x, y) => (y.x.CompareTo(x.x)));

        _displayPoints.Add(_b);

        for (int i = cutPoints.Count - 1; i >= 0; i--) _displayPoints.Add(RotateClockwise(cutPoints[i]));

        _displayPoints.Add(_bb);
        
        int num = cutPoints.Count - 1;
        foreach (var point in cutPoints)
        {
            _displayPoints.Add(point);
            foreach (var newPoint in GetNewPoints(point, num--))
            {
                _displayPoints.Add(newPoint);
            }
            _displayPoints.Add(new Vector3(point.x, -point.y, point.z));
        }
        _displayPoints.Add(_a);
        // place vertices
        _vertices = _displayPoints.ToArray();

        
        for (int i = 0; i < _vertices.Length; i++)
        {
            //_vertices[i] = new Vector3(_vertices[i].x, _vertices[i].y, _vertices[i].z + UnityEngine.Random.Range(-0.03f, 0.03f));
        }

        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        
        _mesh.vertices = _vertices;

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
        _mesh.triangles = _triangles;

        _vertexColors = new Color[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            //_vertexColors[i] = Color.Lerp(Color.red, Color.blue, (float)(i + 1) / _vertices.Length);
            _vertexColors[i] = new Color(UnityEngine.Random.Range(0, 255) / 255f, UnityEngine.Random.Range(0, 255) / 255f,
                UnityEngine.Random.Range(0, 255) / 255f);
        }

        _mesh.colors = _vertexColors;

        Debug.Log(_displayPoints.Count);
    }

    private List<Vector3> _newGridPoints = new List<Vector3>();
    [ShowInInspector]
    void DisplayGrid()
    {
        _newGridPoints.Clear();
        // number of vertices that will finally be placed
        int numVerts = (int)(Mathf.Pow(2, (int)_generations + 1) + 1) * (int)(Mathf.Pow(2,(int)_generations) + 1);
        // number of items in columns and row (defines the split)
        int columnSize = (int)Mathf.Pow(2, (int)_generations + 1) + 1;
        // amount to shift will always be the same
        float horizontalShift = .910593f / (columnSize - 1);
        // adjust down for diminishing height
        float heightAdjust = 0.5257311f / (columnSize - 1);
        float height = 1.0514622f;
        // amount shifted
        int horizontalSpot = 0;
        while (columnSize > 0)
        {
            float verticalShift = height / (columnSize - 1);
            if (columnSize == 1) verticalShift = 0f;
            float columnSpot = 0;
            for (int i = 0; i < columnSize; i++)
            {
                float x = 0.303531f - horizontalShift * horizontalSpot;
                float y = (height / 2f) - verticalShift * columnSpot;
                if (x > 0) x += GetOffsetX(y) * (x / 0.303531f);
                
                Vector3 v = GetUnitVectorComponentZ(new Vector3(x, y, 0f));
                _newGridPoints.Add(v);
                columnSpot++;
            }

            horizontalSpot++;
            columnSize--;
            height -= heightAdjust * 2;
        }

        _vertices = _newGridPoints.ToArray();
    }

    // curve offset from 2 points
    float GetOffsetX(float x)
    {
        return -(1 / 5.19f) * Mathf.Pow(x, 2) + 0.0532911f;
    }
    
    // curve offset from 2 points
    float GetOffsetY(float x)
    {
        return -(1 / 5.19f) * Mathf.Pow(x, 2) + 0.0532911f;
    }

    private Vector3 _t;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        _t = transform.localScale;
        int i = 0;
        foreach (var point in _displayPoints)
        {
            i++;
            //Handles.Label(point, i.ToString());
            //Gizmos.DrawSphere(point, .005f);
        }
        Gizmos.color = Color.blue;
        foreach (var point in _newGridPoints)
        {
            Gizmos.DrawSphere(point, .005f);
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(Vector3.forward, 0.005f);
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
