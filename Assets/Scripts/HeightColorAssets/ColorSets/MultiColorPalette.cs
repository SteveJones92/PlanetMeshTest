using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

[CreateAssetMenu(menuName = "ProceduralColors/MultiColorPalette")]
public class MultiColorPalette : ScriptableObject
{
    [OnValueChanged(nameof(InitializeRatios))]
    public ProceduralColor[] inputColors;

    [ToggleLeft, OnValueChanged(nameof(CalculateRatios))]
    public bool randomRatios;
    public float[] ratios;
    [Min(0)]
    public int detail = 100;
    [ReadOnly]
    public List<Color> outputColors;

    [ShowInInspector]
    public void Generate()
    {
        outputColors.Clear();
        CalculateRatios();
        for (int i = 0; i < inputColors.Length; i++)
        {
            BuildListOfColorVariations(inputColors[i]);
        }
    }

    private void InitializeRatios()
    {
        ratios = new float[inputColors.Length];
        float portion = 1f / inputColors.Length;
        for (int i = 0; i < ratios.Length; i++)
        {
            ratios[i] = portion;
        }
        
        ChangeRatios();
    }

    private void RandomizeRatios()
    {
        for (int i = 0; i < ratios.Length; i++)
        {
            ratios[i] = Random.Range(0, 1000);
        }
    }

    [ShowInInspector]
    private void CalculateRatios()
    {
        if (ratios.IsNullOrEmpty()) InitializeRatios();
        
        if (randomRatios)
        {
            RandomizeRatios();
        }
        
        float sum = 0f;
        foreach (var f in ratios)
        {
            sum += f;
        }

        for (int i = 0; i < ratios.Length; i++)
        {
            ratios[i] /= sum;
        }
        
        ChangeRatios();
    }

    private void ChangeRatios()
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
        return outputColors.ToArray();
    }

    private void BuildListOfColorVariations(ProceduralColor inputColor)
    {
        var tempColorList = GenerateRange(inputColor, inputColor.variationsToGenerate);
        outputColors.AddRange(tempColorList);
    }

    private List<Color> GenerateRange(ProceduralColor inputColor, int numberOfVariations)
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

    private Color SetLevel(Color aColor, float level)
    {
        float myH, myS, myV;
        ColorConvert.RGBToHSV(aColor, out myH, out myS, out myV);
        Color returnColor = ColorConvert.HSVToRGB(myH, myS, myV * level);
        return returnColor;
    }

    private Color HueShift(Color rgbColor, int degrees)
    {
        float myH, myS, myV;
        ColorConvert.RGBToHSV(rgbColor, out myH, out myS, out myV);

        degrees = Random.Range(-degrees, degrees);
        Color returnColor = ColorConvert.HSVToRGB(myH + degrees / 360f, myS, myV);
        return returnColor;
    }

}
