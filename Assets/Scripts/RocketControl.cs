using UnityEngine;
using System;
using System.Collections.Generic;

public class RocketControl : MonoBehaviour
{
    [Header("Model stuff")]
    public GameObject ThrustPoint;
    public float ThrustForce = 15;
    public float MaxThrustAngle = 20;
    public GameObject TrailParticles;
    private ParticleSystem.EmissionModule em;
    public List<GameObject> Legs;
    PlayerControls controls;
    private Vector2 Move;
    private bool playerFiring;
    private bool firing;

    [Header("Fuel and mass")]
    public float maxFuel = 100;
    private float currentFuel;
    public Vector3 massOffset = new Vector3(0, -2, 0);
    public float fuelMass = 8.0f;
    public float dryMass = 2.0f;

    // autopilot
    private bool autopilotFlag = false;
    private bool legsFlag = false;
    private Vector3 targetDirection;
    private Vector3 targetLocation = new Vector3(0, 100, 0);
    private float deltaX;
    private float deltaY;
    private Vector3 stripX;
    private Vector3 stripZ;
    private float xInput;
    private float yInput;

    // pid controller internals
    private Vector2 previousInputs = Vector2.zero;
    private Vector2 PTerm = Vector2.zero;
    private Vector2 ITerm = Vector2.zero;
    private Vector2 DTerm = Vector2.zero;
    private Vector2 CombinedInput = Vector2.zero;

    [Header("PID parameters")]
    public float pWeight = 1.0f;
    public float iWeight = 0.1f;
    public float dWeight = 0.2f;

    public Vector2 getInputValues()
    {
        return new Vector2((xInput + 1) / 2, (yInput + 1) / 2);
    }

    public float getFuel()
    {
        return currentFuel / maxFuel;
    }

    void ToggleAutopilot()
    {
        if (autopilotFlag == false)
        {
            Debug.Log("activated autopilot");
            autopilotFlag = true;
        }
        else
        {
            Debug.Log("deactivated autopilot");
            autopilotFlag = false;
        }
    }

    void ToggleLegs()
    {
        float targetAngle;

        if (legsFlag == false)
        {
            Debug.Log("opened legs");
            targetAngle = 130f;
            legsFlag = true;
        }
        else
        {
            Debug.Log("closed legs");
            targetAngle = 0f;
            legsFlag = false;
        }


        foreach (GameObject leg in Legs)
        {
            try
            {
                JointSpring spr = leg.GetComponent<HingeJoint>().spring;
                spr.targetPosition = targetAngle;
                leg.GetComponent<HingeJoint>().spring = spr;
            } catch
            {
                Debug.Log("Leg probably broke");
            }
        }
    }

    private void Awake()
    {
        GetComponent<Rigidbody>().mass = dryMass + fuelMass;

        controls = new PlayerControls();

        // define player input callbacks
        controls.Gameplay.AutopilotToggle.performed += ctx => ToggleAutopilot();
        controls.Gameplay.LegsToggle.performed += ctx => ToggleLegs();
        controls.Gameplay.FireEngine.performed += ctx => playerFiring = true;
        controls.Gameplay.ThrustDirection.performed += ctx => Move = ctx.ReadValue<Vector2>();

        controls.Gameplay.ThrustDirection.canceled += ctx => Move = Vector2.zero;
        controls.Gameplay.FireEngine.canceled += ctx => playerFiring = false;

        // put attached legs in list
        foreach (Transform child in transform)
        {
            if (child.tag == "Leg")
            {
                Legs.Add(child.gameObject);
                // Debug.Log(child.name);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        currentFuel = maxFuel;
        // set the center of mass properly
        gameObject.GetComponent<Rigidbody>().centerOfMass = massOffset;

        // disable particle system
        em = TrailParticles.GetComponent<ParticleSystem>().emission;
        em.enabled = false;
    }

    public void CollectTargetLocation(Vector3 location)
    {
        targetLocation = location;

        //deltaAngleZ = Vector3.SignedAngle(transform.up, targetDirection, transform.forward);
        //deltaAngleX = Vector3.SignedAngle(transform.up, targetDirection, transform.right);

    }

    // Update is called once per frame

    private void Update()
    {
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
    }

    private static float Sigmoid(double value)
    {
        return (1.0f / (1.0f + (float)Math.Exp(-value)) - 0.5f) * 2.0f;
    }

    // Fixed updata is called at fixed time intervals
    void FixedUpdate()
    {

        if (autopilotFlag == true)
        {
            targetDirection = (targetLocation - transform.position).normalized;
            // float targetDistance = (targetLocation - transform.position).magnitude;

            stripX = targetDirection - Vector3.Dot(targetDirection, transform.forward) * transform.forward;
            stripZ = targetDirection - Vector3.Dot(targetDirection, transform.right) * transform.right;

            deltaX = Vector3.SignedAngle(stripX, transform.up, transform.forward);
            deltaY = Vector3.SignedAngle(stripZ, transform.up, transform.right);

            // Debug.Log(deltaX.ToString());
            // Debug.Log(deltaY.ToString());

            PTerm = new Vector2(deltaX, deltaY);
            ITerm = 0.7f * ITerm + PTerm;
            DTerm = -(PTerm - previousInputs) / Time.deltaTime;

            CombinedInput = pWeight * PTerm + iWeight * ITerm + dWeight * DTerm;

            // Debug.Log("PTerm" + (pWeight * PTerm).ToString());
            // Debug.Log("ITerm" + (iWeight * ITerm).ToString());
            // Debug.Log("DTerm" + (dWeight * DTerm).ToString());
            // Debug.Log("Combined" + CombinedInput.ToString());

            xInput = Sigmoid(0.1 * CombinedInput.x);
            yInput = Sigmoid(0.1 * CombinedInput.y);

            ThrustPoint.transform.localRotation = Quaternion.Euler(yInput * MaxThrustAngle, 0, xInput * MaxThrustAngle);

            // Debug.DrawRay(transform.position, transform.up * 3, Color.red);
            // Debug.DrawRay(transform.position, transform.forward * 3, Color.blue);
            // Debug.DrawRay(transform.position, transform.right * 3, Color.green);
            firing = true;

            previousInputs = new Vector2(deltaX, deltaY);
        }
        else
        {

            xInput = Move.x;
            yInput = Move.y;

            ThrustPoint.transform.localRotation = Quaternion.Euler(-Move.y * MaxThrustAngle, 0, Move.x * MaxThrustAngle);
            firing = playerFiring;
        }

        if (firing && currentFuel > 0)
        {
            // Debug.Log("applying thrust");
            em.enabled = true;
            currentFuel = currentFuel - Time.deltaTime;

            GetComponent<Rigidbody>().mass = dryMass + fuelMass * getFuel();
            gameObject.GetComponent<Rigidbody>().AddForceAtPosition(ThrustPoint.transform.up * ThrustForce * Time.deltaTime, ThrustPoint.transform.position, ForceMode.Impulse);
        }
        else
        {
            em.enabled = false;
        }
    }

    public void Reset()
    {
        TrailParticles.GetComponent<ParticleSystem>().Clear();
        currentFuel = maxFuel;
        GetComponent<Rigidbody>().mass = dryMass + fuelMass;
    }
}