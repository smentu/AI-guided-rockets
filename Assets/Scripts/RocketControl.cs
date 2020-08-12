using UnityEngine;
using System;

public class RocketControl : MonoBehaviour
{
    // internals
    public GameObject ThrustPoint;
    public float ThrustForce;
    public float MaxThrustAngle = 45;
    public Vector3 massOffset = new Vector3(0, -2, 0);
    public GameObject TrailParticles;
    private ParticleSystem.EmissionModule em;

    // input stuff
    PlayerControls controls;
    private Vector2 Move;
    private bool playerFiring;
    private bool firing;

    // autopilot
    private bool autopilotFlag = false;
    private Vector3 targetDirection;
    // private Vector3 targetRotation;
    private float targetDistance;
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

    public float pWeight = 1.0f;
    public float iWeight = 0.1f;
    public float dWeight = 0.2f;


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

    private void Awake()
    {
        controls = new PlayerControls();

        // define player input callbacks
        controls.Gameplay.AutopilotToggle.performed += ctx => ToggleAutopilot();
        controls.Gameplay.FireEngine.performed += ctx => playerFiring = true;
        controls.Gameplay.ThrustDirection.performed += ctx => Move = ctx.ReadValue<Vector2>();

        controls.Gameplay.ThrustDirection.canceled += ctx => Move = Vector2.zero;
        controls.Gameplay.FireEngine.canceled += ctx => playerFiring = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        // set the center of mass properly
        gameObject.GetComponent<Rigidbody>().centerOfMass = massOffset;

        // disable particle system
        em = TrailParticles.GetComponent<ParticleSystem>().emission;
        em.enabled = true;
    }

    public void CollectTargetLocation(Vector3 location)
    {
        targetDirection = (location - transform.position).normalized;
        targetDistance = (location - transform.position).magnitude;

        //deltaAngleZ = Vector3.SignedAngle(transform.up, targetDirection, transform.forward);
        //deltaAngleX = Vector3.SignedAngle(transform.up, targetDirection, transform.right);

    }

    // Update is called once per frame

    private void Update()
    {
        if (autopilotFlag == true)
        {
            stripX = targetDirection - Vector3.Dot(targetDirection, transform.forward) * transform.forward;
            stripZ = targetDirection - Vector3.Dot(targetDirection, transform.right) * transform.right;

            deltaX = Vector3.SignedAngle(stripX, transform.up, transform.forward);
            deltaY = Vector3.SignedAngle(stripZ, transform.up, transform.right);

            // Debug.Log(deltaX.ToString());
            // Debug.Log(deltaY.ToString());

            PTerm = new Vector2(deltaX, deltaY);
            ITerm = 0.7f * ITerm + PTerm;
            DTerm = - (PTerm - previousInputs) / Time.deltaTime;

            CombinedInput = pWeight * PTerm + iWeight * ITerm + dWeight * DTerm;

            xInput = Sigmoid(0.1 * CombinedInput.x);
            yInput = Sigmoid(0.1 * CombinedInput.y);

            ThrustPoint.transform.localRotation = Quaternion.Euler(yInput * MaxThrustAngle, 0, xInput * MaxThrustAngle);
            
            // Debug.DrawRay(transform.position, transform.up * 3, Color.red);
            // Debug.DrawRay(transform.position, transform.forward * 3, Color.blue);
            // Debug.DrawRay(transform.position, transform.right * 3, Color.green);
            firing = true;

            previousInputs = PTerm;
        }
        else
        {
            ThrustPoint.transform.localRotation = Quaternion.Euler(Move.y * MaxThrustAngle, 0, Move.x * MaxThrustAngle);
            firing = playerFiring;
        }
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
        return 1.0f / (1.0f + (float)Math.Exp(-value)) - 0.5f;
    }

    // Fixed updata is called at fixed time intervals
    void FixedUpdate()
    {
        if (firing)
        {
            // Debug.Log("applying thrust");
            em.enabled = true;
            gameObject.GetComponent<Rigidbody>().AddForceAtPosition(ThrustPoint.transform.up * ThrustForce, ThrustPoint.transform.position, ForceMode.VelocityChange);
        }
        else
        {
            em.enabled = false;
        }
    }

    public void Reset()
    {
        TrailParticles.GetComponent<ParticleSystem>().Clear();
    }
}