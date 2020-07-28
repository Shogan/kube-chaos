using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityThreading;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System;

public class Pod : MonoBehaviour
{
    public string podName;
    public float scaleVariance = 0.1f;
    private float startScale;
    public LineRenderer lr;
    public Transform lrPodTarget;
    public Transform lrTextTarget;
    public Transform canvasTransform;
    public Rigidbody2D rb;
    public Vector3 canvasTweenMovement;
    public Vector2 randomStartDirection;
    public float speed = 2f;
    private TweenerCore<Vector3, Vector3, VectorOptions> tween;
    public BoxCollider2D boxCollider;
    public TMP_Text podText;

    private void Awake()
    {
        startScale = transform.localScale.x;
    }

    // Start is called before the first frame update
    void Start()
    {
        randomStartDirection = new Vector2(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
        canvasTweenMovement = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0f);

        transform.DOScale(startScale - scaleVariance, 1f)
            .SetEase(Ease.InBounce)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // Update is called once per frame
    void Update()
    {
        if (lr != null)
        {
            lr.SetPosition(0, lrPodTarget.position);
            lr.SetPosition(1, lrTextTarget.position);
        }
    }
}
