using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Transform gunPoint;
    public bool weaponRelativeToComponent;
    public float bulletSpeed = 12f;
    public float weaponFireRate = 0.07f;
    public float bulletRandomness = 0f;
    public bool useGamepadController = false;

    /// <summary>
    /// The aim angle the weapon system is currently at in radians.
    /// </summary>
    public float aimAngle;

    private Vector2 facingDirection;
    private float coolDown;

    // Start is called before the first frame update
    void Start()
    {
        useGamepadController = KubeManager.Instance.joystickEnabled;
    }

    private void HandleShooting()
    {
        coolDown -= Time.deltaTime;

        var rightStickHoriz = Input.GetAxis("RightStickHorizontal");
        var rightStickVert = Input.GetAxis("RightStickVertical");
        var fire1 = Input.GetAxis("Fire1");

        if (useGamepadController)
        {
            CalculateAimAndFacingAngles(new Vector2(rightStickHoriz, rightStickVert));
        }
        else
        {
            var worldMousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f));
            // Calculate based on whether this weapon configuration is set relative to the WeaponSystem object, or the assigned gunpoint object.
            if (weaponRelativeToComponent)
            {
                facingDirection = worldMousePosition - gunPoint.transform.position;
            }
            else
            {
                facingDirection = worldMousePosition - transform.position;
            }

            CalculateAimAndFacingAngles(facingDirection);
        }

        if (fire1 >= 1f || Mathf.Abs(rightStickVert) >= 0.2f || Mathf.Abs(rightStickHoriz) > 0.2f)
        {
            ShootWithCoolDown();
        }
    }

    private void ShootWithCoolDown()
    {
        if (coolDown <= 0f)
        {
            ProcessBullets();
            coolDown = weaponFireRate;
        }
    }

    /// <summary>
    /// Processing of bullets to fire is done here. Very dumbed down version my 2D Shooter Weapon and Bullet System (See Unity Asset Store).
    /// </summary>
    private void ProcessBullets()
    {
        var bulletSpreadInitial = 0f;
        var bulletSpacingInitial = 0f;
        var bulletSpreadIncrement = 0f;
        var bulletSpacingIncrement = 0f;

        var bullet = GetBulletFromPool();
        var bulletComponent = (Bullet)bullet.GetComponent(typeof(Bullet));


        var offsetX = Mathf.Cos(aimAngle - Mathf.PI / 2) * (bulletSpacingInitial - 0f * bulletSpacingIncrement);
        var offsetY = Mathf.Sin(aimAngle - Mathf.PI / 2) * (bulletSpacingInitial - 0f * bulletSpacingIncrement);

        bulletComponent.directionAngle = aimAngle + bulletSpreadInitial + 0f * bulletSpreadIncrement;

        bulletComponent.speed = bulletSpeed;

        // Setup the point at which bullets need to be placed based on all the parameters
        var initialPosition = gunPoint.position + (gunPoint.transform.forward * (bulletSpacingInitial - 0f * bulletSpacingIncrement));
        var bulletPosition = new Vector3(initialPosition.x + offsetX + Random.Range(0f, 1f) * bulletRandomness - bulletRandomness / 2,
            initialPosition.y + offsetY + Random.Range(0f, 1f) * bulletRandomness - bulletRandomness / 2, 0f);

        bullet.transform.position = bulletPosition;

        bulletComponent.bulletXPosition = bullet.transform.position.x;
        bulletComponent.bulletYPosition = bullet.transform.position.y;

        // Activate the bullet to get it going
        bullet.SetActive(true);
    }

    private GameObject GetBulletFromPool()
    {
        return ObjectPoolManager.instance.GetUsableBeam2Bullet();
    }

    // Update is called once per frame
    void Update()
    {
        facingDirection = Vector2.zero;
        var worldMousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f));

        // Calculate based on whether this weapon configuration is set relative to the WeaponSystem object, or the assigned gunpoint object.
        if (weaponRelativeToComponent)
        {
            facingDirection = worldMousePosition - gunPoint.transform.position;
        }
        else
        {
            facingDirection = worldMousePosition - transform.position;
        }

        CalculateAimAndFacingAngles(facingDirection);
        HandleShooting();
    }

    /// <summary>
    /// Calculate aim angle and other settings that apply to all ShooterType orientations
    /// </summary>
    /// <param name="facingDirection"></param>
    private void CalculateAimAndFacingAngles(Vector2 facingDirection)
    {
        aimAngle = Mathf.Atan2(facingDirection.y, facingDirection.x);
        if (aimAngle < 0f)
        {
            aimAngle = Mathf.PI * 2 + aimAngle;
        }

        // Rotate the GameObject to face the direction of the mouse cursor (the object with the weaponsystem component attached, or, if the weapon configuration specifies relative to the gunpoint, rotate the gunpoint instead.
        if (weaponRelativeToComponent)
        {
            gunPoint.transform.eulerAngles = new Vector3(0.0f, 0.0f, aimAngle * Mathf.Rad2Deg);
        }
        else
        {
            transform.eulerAngles = new Vector3(0.0f, 0.0f, aimAngle * Mathf.Rad2Deg);
        }
    }
}
