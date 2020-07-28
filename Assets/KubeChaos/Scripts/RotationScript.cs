using UnityEngine;
using System.Collections;
using System.IO;

/// <summary>
/// Simple rotation script used for demo scenes.
/// </summary>
public class RotationScript : MonoBehaviour
{

    public bool randomRotationEnabled;
    public float rotationsPerMinuteMax;
    public bool rotationEnabled = false;
    public float rotationsPerMinute = 0f;
    private Transform transformCached;

    void Start()
    {
        transformCached = transform;

        if (randomRotationEnabled)
        {
            rotationsPerMinute = Random.Range(rotationsPerMinute, rotationsPerMinuteMax);
        }
    }

    void Update()
    {
        if (!rotationEnabled) return;
        transformCached.Rotate(0, 0, rotationsPerMinute * Time.deltaTime, Space.Self);
    }
}