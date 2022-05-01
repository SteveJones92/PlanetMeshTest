using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "ProceduralColors/MultiColorPalette")]
public class MultiColorPalette : ScriptableObject
{

    public ProceduralColor[] inputColors;
    public List<Color> outputColors;

    [ShowInInspector]
    public void Generate()
    {
        outputColors.Clear();
        for (int i = 0; i < inputColors.Length; i++)
        {
            BuildListOfColorVariations(inputColors[i]);
        }
    }

    public void ChangeDetailAndRatios(int detail, float[] ratios)
    {
        if (ratios.Length != inputColors.Length) return;

        int i = 0;
        foreach (var color in inputColors)
        {
            color.variationsToGenerate = (int)(detail * ratios[i]);
            i++;
        }
    }

    public Color[] GetColors()
    {
        Color[] colors = new Color[outputColors.Count];
        int i = 0;
        foreach (var color in outputColors)
        {
            colors[i] = color;
            i++;
        }

        return colors;
    }

    public void BuildListOfColorVariations(ProceduralColor inputColor)
    {
        var tempColorList = GenerateRange(inputColor, inputColor.variationsToGenerate);
        outputColors.AddRange(tempColorList);
    }

    public List<Color> GenerateRange(ProceduralColor inputColor, int numberOfVariations)
    {
        List<Color> generatedColorList = new List<Color>();
        float valueIncrement = 1f / inputColor.variationsToGenerate;

        for (int i = 1; i <= numberOfVariations; i++)
        {
            float ratio = valueIncrement * i;
            ratio = 0.25f + ratio * ( 1.0f - 0.25f );
            generatedColorList.Add(SetLevel(HueShift(inputColor.color, inputColor.degreesHueShift), ratio));
        }

        return generatedColorList;
    }

    public Color SetLevel(Color aColor, float level)
    {
        float myH, myS, myV;
        ColorConvert.RGBToHSV(aColor, out myH, out myS, out myV);
        Color returnColor = ColorConvert.HSVToRGB(myH, myS, myV * level);
        return returnColor;
    }

    public Color HueShift(Color rgbColor, int degrees)
    {
        float myH, myS, myV;
        ColorConvert.RGBToHSV(rgbColor, out myH, out myS, out myV);

        degrees = Random.Range(-degrees, degrees);
        Color returnColor = ColorConvert.HSVToRGB(myH + degrees / 360f, myS, myV);
        return returnColor;
    }

}
