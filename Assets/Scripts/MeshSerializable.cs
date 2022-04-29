using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshSerializable
{
    [OdinSerialize] public Vector3[] Vertices;
    [OdinSerialize] public int[] Triangles;
    [OdinSerialize] public IndexFormat IndexFormat;

    public MeshSerializable(Mesh mesh)
    {
        this.IndexFormat = mesh.indexFormat;
        this.Vertices = mesh.vertices;
        this.Triangles = mesh.triangles;
    }

    public Mesh GetMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat;
        mesh.vertices = Vertices;
        mesh.triangles = Triangles;
        return mesh;
    }
}
