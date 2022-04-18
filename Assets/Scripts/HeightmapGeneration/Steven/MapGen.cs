using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using TinkerWorX.AccidentalNoiseLibrary;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapGen : MonoBehaviour
{
    public GameObject planePrefab;
    public GameObject mapPrefab;
    [OnValueChanged(nameof(UpdateCurrentMap))]
    public int width;
    [OnValueChanged(nameof(UpdateCurrentMap))]
    public int height;

    [OnValueChanged(nameof(UpdateCurrentMap))]
    public Vector2Int perlinScale;

    [OnValueChanged(nameof(UpdateCurrentMap))]
    public float lacunarity;
    [OnValueChanged(nameof(UpdateCurrentMap))]
    public float frequency;
    [OnValueChanged(nameof(UpdateCurrentMap))]
    public float offset;
    [OnValueChanged(nameof(UpdateCurrentMap))]
    public int octaves;
    [OnValueChanged(nameof(UpdateCurrentMap))]
    public float h;
    [OnValueChanged(nameof(UpdateCurrentMap))]
    public float gain;
    
    [ShowInInspector, ShowIf("@_currentObj")]
    private GameObject _currentObj;
    
    // Start is called before the first frame update
    
    [ShowInInspector]
    void CreateMap()
    {
        // set up map
        Map map = new Map(width, height);
        map.SetHeightMapNoise(perlinScale.x, perlinScale.y, frequency, lacunarity, octaves, offset, h, gain);
        map.SetColors();
        
        CreateMapPreview(map);
    }

    public static Map CreateMap(int width, int height, int perlinX, int perlinY, float frequency, float lacunarity, int octaves, float offset, float h, float gain)
    {
        Map map = new Map(width, height);
        map.SetHeightMapNoise(perlinX, perlinY, frequency, lacunarity, octaves, offset, h, gain);
        map.SetColors();
        return map;
    }

    private void CreateMapPreview(Map map)
    {
        // create the parent object that holds the map items
        _currentObj = Instantiate(mapPrefab, transform);
        // give it a somewhat unique name
        _currentObj.name = "Map" + transform.childCount;
        
        // place it on the parent at a specific location
        var hmPlane = Instantiate(planePrefab,new Vector3(-7.5f, 0, 0), Quaternion.identity, _currentObj.transform);
        var cmPlane = Instantiate(planePrefab, new Vector3(7.5f, 0, 0), Quaternion.identity, _currentObj.transform);
        hmPlane.name = "Heightmap";
        cmPlane.name = "Colormap";
        
        // get the render components of the new planes
        MeshRenderer hmMeshRenderer = hmPlane.GetComponent<MeshRenderer>();
        MeshRenderer cmMeshRenderer = cmPlane.GetComponent<MeshRenderer>();
        
        // get the passed in map textures
        var texs = map.GetTextures();
        
        // ensure that the material is instanced
        hmMeshRenderer.sharedMaterial = new Material(hmMeshRenderer.sharedMaterial);
        cmMeshRenderer.sharedMaterial = new Material(cmMeshRenderer.sharedMaterial);
        
        // set the texture of it
        hmMeshRenderer.sharedMaterial.mainTexture = texs[0];
        cmMeshRenderer.sharedMaterial.mainTexture = texs[1];
        
        _currentObj.GetComponent<TexturePrint>().map = map;
    }

    private void UpdateCurrentMap()
    {
        if (!_currentObj)
        {
            return;
        }
        MeshRenderer hmMeshRenderer = null;
        MeshRenderer cmMeshRenderer = null;

        foreach (var child in _currentObj.GetComponentsInChildren<MeshRenderer>())
        {
            if (child.name.Equals("Heightmap"))
            {
                hmMeshRenderer = child;
            } else if (child.name.Equals("Colormap"))
            {
                cmMeshRenderer = child;
            }
        }

        if (!cmMeshRenderer || !hmMeshRenderer)
        {
            Debug.Log("Map items not found");
            return;
        }

        Map map = new Map(width, height);
        map.SetHeightMapNoise(perlinScale.x, perlinScale.y, frequency, lacunarity, octaves, offset, h, gain);
        map.SetColors();
        var texs = map.GetTextures();
        
        hmMeshRenderer.sharedMaterial.mainTexture = texs[0];
        cmMeshRenderer.sharedMaterial.mainTexture = texs[1];
        
        _currentObj.GetComponent<TexturePrint>().map = map;
    }

    [ShowInInspector]
    private void PrintCurrentMap()
    {
        _currentObj.GetComponent<TexturePrint>().map.CreateTextureImages(_currentObj.name);
    }

    [ShowInInspector]
    void ClearChildren()
    {
        while (transform.childCount > 0) DestroyImmediate(transform.GetChild(0).gameObject);
    }
}
