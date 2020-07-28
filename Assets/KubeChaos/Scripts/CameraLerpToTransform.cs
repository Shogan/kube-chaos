using UnityEngine;
using DG.Tweening;

public class CameraLerpToTransform : MonoBehaviour
{
    private Camera thisCam;
    public float targetOrthographicSize = 6.8f;
    public Transform target;
    public float speed;
    public float cameraDepth = -10f;

    public float minX, minY, maxX, maxY;

    private void Awake()
    {
        thisCam = GetComponent<Camera>();
    }

    // Use this for initialization
    void Start()
    {
        thisCam.DOOrthoSize(targetOrthographicSize, 5f);
    }

    // Update is called once per frame
    void Update()
    {
        var newPosition = Vector2.Lerp(transform.position, target.position, Time.deltaTime * speed);
        var camPosition = new Vector3(newPosition.x, newPosition.y, cameraDepth);

        var v3 = camPosition;
        var newX = Mathf.Clamp(v3.x, minX, maxX);
        var newY = Mathf.Clamp(v3.y, minY, maxY);
        transform.position = new Vector3(newX, newY, cameraDepth);

        
    }
}
