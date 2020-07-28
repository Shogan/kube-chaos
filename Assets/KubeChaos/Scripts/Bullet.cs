using UnityEngine;

public class Bullet : MonoBehaviour {

    /// <summary>
    /// The speed at which the bullet will travel at
    /// </summary>
    public float speed = 1f;

    /// <summary>
    /// The sprite renderer for the bullet - used to modify bullet color when bullets are instantiated if custom color is selected in Weapon Configuration.
    /// </summary>
    public SpriteRenderer bulletSpriteRenderer;

    /// <summary>
    /// The direction bullet is travelling in radians.
    /// </summary>
    public float directionAngle;

    /// <summary>
    /// The direction bullet is travelling in degrees.
    /// </summary>
    public float directionDegrees;

    /// <summary>
    /// Used when calculating reflection / ricochet angles when the bullet impacts a collider.
    /// </summary>
    public Vector3 newDirection;

    /// <summary>
    /// Used to set bullet travel direction based on directionAngle
    /// </summary>
    public float bulletXPosition;

    /// <summary>
    /// Used to set bullet travel direction based on directionAngle
    /// </summary>
    public float bulletYPosition;

    /// <summary>
    /// Used to simplify access to the bullet transforms' eulerAngles property.
    /// </summary>
    private Vector3 currentBulletEuler;

    // Use this for initialization
    void Start()
    {
        // Cache the sprite renderer on start when bullets are initially created and pooled for better performance
        bulletSpriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        // When the bullet is enabled, ensure it faces the correct direction
        transform.eulerAngles = new Vector3(0.0f, 0.0f, directionAngle * Mathf.Rad2Deg);
    }

    /// <summary>
    /// This is a basic reflection algorithm (works the same way that Vector3.Reflect() works, but specifically for Vector2. Not currently used.
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="normal"></param>
    /// <returns></returns>
    public static Vector2 Reflect(Vector2 vector, Vector2 normal)
    {
        return vector - 2 * Vector2.Dot(vector, normal) * normal;
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        gameObject.SetActive(false);
    }
    
    // Update is called once per frame
    void Update()
    {
        directionDegrees = directionAngle*Mathf.Rad2Deg;

        if (gameObject != null)
        {
            // Account for bullet movement at any angle
            bulletXPosition += Mathf.Cos(directionAngle) * speed * Time.deltaTime;
            bulletYPosition += Mathf.Sin(directionAngle) * speed * Time.deltaTime;

            transform.position = new Vector2(bulletXPosition, bulletYPosition);

            // If the bullet is no longer visible by the main camera, then set it back to disabled, which means the bullet pooling system will then be able to re-use this bullet.
            if (!bulletSpriteRenderer.isVisible) gameObject.SetActive(false);
        }
    }
}
