using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using TinkerWorX.AccidentalNoiseLibrary;
using UnityEngine;

public class TexturePrint : MonoBehaviour
{
    public Map map;
    
    // values that created this map
    [HideInInspector]
    public int width;
    [HideInInspector]
    public int height;
    [HideInInspector]
    public Vector2Int perlinScale;
    [HideInInspector]
    public float lacunarity;
    [HideInInspector]
    public float frequency;
    [HideInInspector]
    public float offset;
    [HideInInspector]
    public int octaves;
    [HideInInspector]
    public float h;
    [HideInInspector]
    public float gain;
    [HideInInspector]
    public bool wrapped;
    [HideInInspector]
    public bool stretched;
    [HideInInspector]
    public int seed;
    [HideInInspector]
    public float stretchPower;
    [HideInInspector]
    public FractalType fractalType;
    [HideInInspector]
    public BasisType basisType;
    [HideInInspector]
    public InterpolationType interpolationType;
}
