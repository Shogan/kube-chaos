using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A very quickly hacked together dual analog stick style movement script.
/// </summary>
public class Move : MonoBehaviour
{
    public float speed = 2f;
    private Rigidbody2D rb;
    private Animator playerAnimator;

    public float currentXRamp;
    public float currentYRamp;
    public float rampCountFactor = 1f;
    public float rampUpTimeHoriz = 1f;
    public float rampUpTimeVert = 1f;
    public bool isMoving;
    public bool useAnimator = false;
    public bool lerpTurnRate;
    public float trackingTurnRate = 2f;
    public float aimAngle;
    private float lastAimAngle;
    public Vector2 direction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (useAnimator)
            playerAnimator = GetComponent<Animator>();
    }

    // Start is called before the first frame update
    void Start()
    {
        currentXRamp = 0f;
        currentYRamp = 0f;
    }

    Vector3 AngleLerp(Vector3 StartAngle, Vector3 FinishAngle, float t)
    {
        float xLerp = Mathf.LerpAngle(StartAngle.x, FinishAngle.x, t);
        float yLerp = Mathf.LerpAngle(StartAngle.y, FinishAngle.y, t);
        float zLerp = Mathf.LerpAngle(StartAngle.z, FinishAngle.z, t);
        Vector3 Lerped = new Vector3(xLerp, yLerp, zLerp);
        return Lerped;
    }

    // Update is called once per frame
    void Update()
    {
        var inputX = Input.GetAxis("Horizontal");
        var inputY = Input.GetAxis("Vertical");

        var posInputX = Mathf.Abs(inputX);
        var posInputY = Mathf.Abs(inputY);

        if (posInputX > 0.2f || posInputY > 0.2f)
        {
            if (posInputX > 0f && currentXRamp < rampUpTimeHoriz)
            {
                currentXRamp += rampCountFactor * Time.deltaTime;
            }

            if (posInputY > 0f && currentYRamp < rampUpTimeVert)
            {
                currentYRamp += rampCountFactor * Time.deltaTime;
            }

            isMoving = true;
            if (useAnimator && playerAnimator != null)
            {
                playerAnimator.SetBool("IsMoving", true);
            }
        }
        else
        {
            if (currentXRamp > 0f)
            {
                currentXRamp -= rampCountFactor * Time.deltaTime;
            }

            if (currentYRamp > 0f)
            {
                currentYRamp -= rampCountFactor * Time.deltaTime;
            }

            isMoving = false;
            if (useAnimator && playerAnimator != null)
            {
                playerAnimator.SetBool("IsMoving", false);
            }
        }

        rb.velocity = new Vector2(currentXRamp * inputX * speed , currentYRamp * inputY * speed);
        if (isMoving)
            direction = rb.velocity.normalized;

        if (lerpTurnRate && isMoving)
        {
            var wantedAimAngle = Mathf.Atan2(direction.y, direction.x);
            aimAngle = wantedAimAngle;
        }
        else
        {
            if (!lerpTurnRate && isMoving)
            {
                aimAngle = Mathf.Atan2(direction.y, direction.x);
                if (aimAngle < 0f)
                {
                    aimAngle = Mathf.PI * 2 + aimAngle;
                }
            }
            
        }

        if (isMoving)
            lastAimAngle = aimAngle;

        var aimAngleDegrees = (aimAngle * Mathf.Rad2Deg);
        if (lerpTurnRate && isMoving)
        {
            var t = Time.deltaTime * trackingTurnRate;
            float rot = Mathf.LerpAngle(transform.eulerAngles.z, aimAngleDegrees, t);
            transform.rotation = Quaternion.Euler(0, 0, rot);
        }
        else
        {
            transform.eulerAngles = new Vector3(0.0f, 0.0f, aimAngleDegrees);
        }

        if (rampUpTimeHoriz < 0f) rampUpTimeHoriz = 0f;
        if (rampUpTimeVert < 0f) rampUpTimeVert = 0f;

    }

    private IEnumerator Rotation()
    {
        // Initialize the time variables
        float currentTime = 0f;
        float duration = 1f;

        // Figure out the current angle/axis
        Quaternion sourceOrientation = transform.rotation;
        float sourceAngle;
        Vector3 sourceAxis;
        sourceOrientation.ToAngleAxis(out sourceAngle, out sourceAxis);

        // Calculate a new target orientation
        float targetAngle = (Random.value - 0.5f) * 3600f + sourceAngle; // Source +/- 1800
        Vector3 targetAxis = Random.onUnitSphere;

        while (currentTime < duration)
        {
            // Might as well wait first, especially on the first iteration where there'd be nothing to do otherwise.
            yield return null;

            currentTime += Time.deltaTime;
            float progress = currentTime / duration; // From 0 to 1 over the course of the transformation.

            // Interpolate to get the current angle/axis between the source and target.
            float currentAngle = Mathf.Lerp(sourceAngle, targetAngle, progress);
            var currentAxis = Vector3.Slerp(sourceAxis, targetAxis, progress);

            // Assign the current rotation
            transform.rotation = Quaternion.AngleAxis(currentAngle, currentAxis);
        }
    }
}
