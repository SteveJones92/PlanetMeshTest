using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;


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
        _ffa = FrontViewRotation90Cw(_a);
        _mow = EllipseScaleOutward(_m);
        _pow = EllipseScaleOutward(_p);
        IcoMesh();
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
    
    /*
    Vector3[] Triangle()
    {
        //float h = height();
        //float l = length();
        //float z = zValue();
        
        // vertices of the triangle
        // a = (ax, ay, az) = (-2h/3, 0, z) page5
        Vector3 a = new Vector3(-2 * _hDiv3, 0, _z);
        // b = (bx, by, bz) = (h/3, l/2, z) page5
        Vector3 b = new Vector3(_hDiv3, _lHalf, _z);
        // bb = (bx, b.-y, bz)
        Vector3 bb = new Vector3(b.x, -b.y, b.z);
        // p = (px, py, pz) = (ax, 0, -2az) page5
        Vector3 p = new Vector3(a.x, 0, -2 * a.z);
        
        // midpoints for the spherical icosahedron triangle ribs
        // c = (cx, cy, cz) = (bx / |bx, bz|, 0, bz / |bx, bz|)
        float magBxBz = Mathf.Sqrt(Mathf.Pow(b.x, 2) + Mathf.Pow(b.z, 2));
        Vector3 c = new Vector3(b.x / magBxBz, 0, b.z / magBxBz);
        // m = (mx, my, mz) = (-cx / 2, sqrt(3 / 4) cx, cz)
        Vector3 m = new Vector3(-c.x / 2, _sqrt3On4 * c.x, c.z);

        return new Vector3[] { a, b, bb, p, c, m};
    }
    */

    Vector3 RotateClockwise(Vector3 point)
    {
        // frcw (Qxyz) = (qy * sqrt(3/4) - qx / 2, -qx * sqrt(3/4) - qy / 2, qz)
        return new Vector3(point.y * _sqrt3On4 - point.x / 2, -point.x * _sqrt3On4 - point.y / 2, point.z);
    }
    
    Vector3 RotateCounterClockwise(Vector3 point)
    {
        // frccw (Qxyz) = (-qy * sqrt(3/4) - qx / 2, qx * sqrt(3/4) - qy / 2, qz)
        return new Vector3(point.y * _sqrt3On4 - point.x / 2, -point.x * _sqrt3On4 - point.y / 2, point.z);
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

    Vector3 GetUnitVector(Vector3 point)
    {
        // fy(Qxyz) = (qx, sqrt(1 - qx^2 - qz^2), qz) -- qx^2 + qz^2 must be small than or equal to 1
        float qxSq = Mathf.Pow(point.x, 2);
        float qzSq = Mathf.Pow(point.z, 2);
        if (qxSq + qzSq > 1) throw new InvalidOperationException("qx^2 + qz^2 must be small than or equal to 1");
        return new Vector3(point.x, Mathf.Sqrt(1 - qxSq - qzSq), point.z);
    }
    
    // fL page10
    // To construct a line L with vector Lˆv and point Lp where the point Lp is the point on
    // the line that is the nearest to the origin, based on 2 vectors q and r, which are points on that line
    // fL (Qxyz, Rxyz) = (L^v, Lp) = ( ^(r-q), q - ^(r-q) * (q . ^(r-q) ) = (L^v, q - L^v * (q . L^v))
    Line ConstructLine(Vector3 q, Vector3 r)
    {
        Vector3 lv = GetUnitVector(r - q);
        return new Line(lv, q - lv * (Vector3.Dot(q, lv)));
    }
    
    // fu page11
    // To find the intersection points of a line intersecting the unit sphere
    Vector3[] FindIntersectionPoints(Line l)
    {
        float lpMag = l.point.magnitude;
        if (lpMag >= 1) throw new InvalidOperationException("Lp magnitude must be smaller than 1");
        float val = Mathf.Sqrt(1 - Mathf.Pow(lpMag, 2));
        return new Vector3[] { l.point + l.normal * val, l.point - l.normal * val };
    }
    
    // fm page 11
    // 2D front view mirror point q along line L
    // fm(q, L) = fxz(q) + ff(^(fxz(L^v)) * (2 * (ff(^fxz(L^v)) . (fxz(Lp) - fxz(q))))
    Vector3 FrontViewMirrorPointAlongLine(Vector3 q, Line l)
    {
        Vector3 fxzq = FrontView2D(q);
        Vector3 fxzlp = FrontView2D(l.point);
        Vector3 fxzlv = FrontView2D(l.normal);
        Vector3 fxzlvUnitV = GetUnitVector(fxzlv);

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
    }

    [ShowInInspector]
    private void UpdateMesh()
    {
        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
    }
    
}
