using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

public class TexturePrint : MonoBehaviour
{
    private Texture2D _heightmap;
    private Texture2D _colormap;

    public void SetHeightMap(Texture2D tex)
    {
        _heightmap = tex;
    }

    public void SetColorMap(Texture2D tex)
    {
        _colormap = tex;
    }

    [ShowInInspector]
    private void PrintTextureMaps()
    {
        File.WriteAllBytes("Assets/Maps/height" + this.name + ".png", _heightmap.EncodeToPNG());
        File.WriteAllBytes("Assets/Maps/color" + this.name + ".png", _colormap.EncodeToPNG());
    }
}
