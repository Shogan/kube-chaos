using UnityEngine;
using System.Collections;

public static class Vector3Extensions
{
    public static float Dot(this Vector3 vectorA, Vector3 vectorB)
    { return Vector3.Dot(vectorA, vectorB); }

    public static float Min(this Vector3 vector)
    {
        var min = vector.x;
        for (int i = 1; i < 3; i++) if (vector[i] < min) min = vector[i];
        return min;
    }
}