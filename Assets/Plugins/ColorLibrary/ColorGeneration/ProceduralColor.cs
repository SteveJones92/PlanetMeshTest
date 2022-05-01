using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class ProceduralColor
{
    public Color color;
    public int degreesHueShift;
    public int variationsToGenerate = 8;
}
