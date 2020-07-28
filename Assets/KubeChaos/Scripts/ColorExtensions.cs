using UnityEngine;
using System.Collections;
using System.Linq;
using System;

public static class ColorExtensions
{
    public static Vector3 GetHue(this Color color) { return color.GetRGB().normalized; }

    public static Vector3 GetRGB(this Color color) { return (Vector3)(Vector4)color; }

    public static float GetValue(this Color color) { return color.GetRGB().magnitude; }
    
    public static Color WithRGB(this Color color, Vector3 rgb)
    {
        for (int i = 0; i < 3; i++) color[i] = rgb[i];
        return color;
    }

    public static Color WithSaturation(this Color color, float saturation)
    {
        if (color.r == color.g && color.g == color.b) return color;

        Vector3 colorWithSaturation;
        var value = color.GetValue();
        var colorWithSaturation0 = value * Vector3.one.normalized;
        if (saturation == 0) colorWithSaturation = colorWithSaturation0;
        else
        {
            Vector3 fullySaturated = (Vector4)color;
            var minChannel = color.GetRGB().Min();
            for (int i = 0; i < 3; i++)
                if (fullySaturated[i] == minChannel) fullySaturated[i] = 0;
            fullySaturated = fullySaturated.normalized * value;
            colorWithSaturation = Vector3.Slerp(colorWithSaturation0, fullySaturated, saturation);
        }
        return color.WithRGB(colorWithSaturation);
    }

    public static Color[] GetRandomBrightColours(int amount)
    {
        var randomColours = new Color[amount];

        for (var i = 0; i < randomColours.Length - 1; i++)
        {
            randomColours[i] = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

            var h = 0f;
            var s = 0f;
            var v = 0f;

            h = UnityEngine.Random.Range(0.1f, 255f);
            s = UnityEngine.Random.Range(0.85f, 1f);
            v = UnityEngine.Random.Range(0.85f, 1f);

            var r = 0f;
            var g = 0f;
            var b = 0f;

            HsvToRgb(h, s, v, out r, out g, out b);
            randomColours[i] = new Color(r, g, b);
        }

        return randomColours;
    }

    public static Color[] GetRandomBrightColours(int amount, int seed)
    {
        var randomColours = new Color[amount];
        var seededRnd = new System.Random(seed);

        for (var i = 0; i < randomColours.Length - 1; i++)
        {
            var seededChoice1 = seededRnd.Next(1, 100);
            var seededChoice2 = seededRnd.Next(1, 100);
            var seededChoice3 = seededRnd.Next(1, 100);

            var seededH = seededRnd.Next(1, 255) * 1f;
            var seededS = seededRnd.Next(85, 100) * 0.01f;
            var seededV = seededRnd.Next(85, 100) * 0.01f;

            randomColours[i] = new Color(seededChoice1 * 0.01f, seededChoice2 * 0.01f, seededChoice3 * 0.01f);

            var h = 0f;
            var s = 0f;
            var v = 0f;

            h = seededH;
            s = seededS;
            v = seededV;

            var r = 0f;
            var g = 0f;
            var b = 0f;

            HsvToRgb(h, s, v, out r, out g, out b);
            randomColours[i] = new Color(r, g, b);
        }

        return randomColours;
    }

    public static Color GetPastelColor(Color mix)
    {
        var red = UnityEngine.Random.Range(0f, 1f);
        var green = UnityEngine.Random.Range(0f, 1f);
        var blue = UnityEngine.Random.Range(0f, 1f);

        red = (red + mix.r) / 2;
        green = (green + mix.g) / 2;
        blue = (blue + mix.b) / 2;

        return new Color(red, green, blue, 1f);
    }

    public static Color[] GetRandomPastelColors(int size)
    {
        var randomColours = GetRandomBrightColours(size);

        var colors = new Color[size];
        var returnColors = new Color[size];

        for (int index = 0; index < colors.Length; index++)
        {
            var rc = randomColours[index];
            colors = Enumerable.Range(0, colors.Length)
            .Select(i => rc.WithSaturation((float)i / (colors.Length - 1)))
            .ToArray();

            returnColors[index] = colors[index];
        }

        return returnColors;
    }

    public static Color[] GetRandomPastelColors(int size, int seed)
    {
        var seededRnd = new System.Random(seed);

        var randomColours = GetRandomBrightColours(size, seed);
        var colors = new Color[size];
        var returnColors = new Color[size];

        for (int index = 0; index < colors.Length; index++)
        {
            var rc = randomColours[index];
            colors = Enumerable.Range(0, colors.Length)
            .Select(i => rc.WithSaturation((float)i / (colors.Length - 1)))
            .ToArray();

            returnColors[index] = colors[index];
        }

        return returnColors;
    }

    public static Color InvertColor(Color color)
    {
        return new Color(1f - color.r, 1f - color.g, 1f - color.b, 1f);
    }

    public static Color[] GetRandomThreeSetColours()
    {
        var rcs = GetRandomBrightColours(20);
        var rc = rcs[10];
        var colors = new Color[13];

        colors = Enumerable.Range(0, colors.Length)
            .Select(i => rc.WithSaturation((float)i / (colors.Length - 1)))
            .ToArray();

        var colorSet = new Color[4];

        var colorSetCount = 0;

        for (int index = 0; index < colors.Length - 1; index++)
        {
            var c = colors[index];
            if (index % 3 == 0)
            {
                colorSet[colorSetCount] = c;
                colorSetCount++;
            }
        }

        return colorSet;
    }

    static void HsvToRgb(double h, double S, double V, out float r, out float g, out float b)
    {
        double H = h;
        while (H < 0) { H += 360; };
        while (H >= 360) { H -= 360; };
        double R, G, B;
        if (V <= 0)
        { R = G = B = 0; }
        else if (S <= 0)
        {
            R = G = B = V;
        }
        else
        {
            double hf = H / 60.0;
            int i = (int)Math.Floor(hf);
            double f = hf - i;
            double pv = V * (1 - S);
            double qv = V * (1 - S * f);
            double tv = V * (1 - S * (1 - f));
            switch (i)
            {

                case 0:
                    R = V;
                    G = tv;
                    B = pv;
                    break;

                case 1:
                    R = qv;
                    G = V;
                    B = pv;
                    break;
                case 2:
                    R = pv;
                    G = V;
                    B = tv;
                    break;

                case 3:
                    R = pv;
                    G = qv;
                    B = V;
                    break;
                case 4:
                    R = tv;
                    G = pv;
                    B = V;
                    break;

                case 5:
                    R = V;
                    G = pv;
                    B = qv;
                    break;

                case 6:
                    R = V;
                    G = tv;
                    B = pv;
                    break;
                case -1:
                    R = V;
                    G = pv;
                    B = qv;
                    break;

                default:
                    R = G = B = V;
                    break;
            }
        }
        r = Clamp((int)(R * 255.0));
        g = Clamp((int)(G * 255.0));
        b = Clamp((int)(B * 255.0));

        r = r / 255.0f;
        g = g / 255.0f;
        b = b / 255.0f;
    }

    static int Clamp(int i)
    {
        if (i < 0) return 0;
        if (i > 255) return 255;
        return i;
    }

    public static bool IsFloatWithinRange(this float value, float minimum, float maximum)
    {
        return value >= minimum && value <= maximum;
    }

    public static bool IsIntWithinRange(this int value, int minimum, int maximum)
    {
        return value >= minimum && value <= maximum;
    }
}