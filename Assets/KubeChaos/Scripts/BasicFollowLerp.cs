using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicFollowLerp : MonoBehaviour
{
    public Transform target;
    public float speed = 1f;
    public LineRenderer lr;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var pos = Vector2.Lerp(transform.localPosition, target.position, Time.deltaTime * speed);
        transform.position = pos;
    }
}
