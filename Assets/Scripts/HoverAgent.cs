using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using System.Collections;

public class HoverAgent : RocketAgent
{
    [Header("Hover arena")]
    public GameObject arena;

    [Tooltip("How much the engine can vector away from zero")]
    public float maxEngineAngle = 20f;
    [Tooltip("How much the grid fins can swivel away from zero")]
    public float maxGridFinAngle = 20f;
    [Tooltip("Where the thrust force is applied")]
    public GameObject thrustPoint;
    public GameObject target;

    PlayerControls controls;

    [Header("physics parameters")]
    public float fuelMass = 80f;
    public float dryMass = 20f;
    public float maxFuel = 100; 
    public float thrustForce = 2500;
    [Range(0.0f, 1.0f)]
    public float startingFuel;
    public AnimationCurve thrustCurve;

    [Header("effects")]
    public AudioSource rocketEngineSound;
    public AudioSource rocketEngineShutoffSound;
    public AudioSource bodyBonk;
    [Tooltip("Whether to produce rocket effects")]
    public bool showEffects = false;

    // lander state variables
    private float inputX;
    private float inputY;
    private float thrust;
    private bool legsDeployed = false;
    private bool gridFinsDeployed = false;
    private bool usingAI;
    private float currentFuel;
    private float previousPitch;
    private float previousRoll;
    private Vector3 previousTargetVector;
    private float previousDistanceReward;

    private bool insideTargetVolume = false;

    // lander child components
    private List<GameObject> legs;
    private List<GameObject> gridFins;
    private List<ParticleSystem> rocketEffects;

    // player inputs
    private Vector2 move;
    private float thrustInput;

    public override void Initialize()
    {
        base.Initialize();

        // collect and categorize legs and grid fins
        legs = new List<GameObject>();
        gridFins = new List<GameObject>();
        rocketEffects = new List<ParticleSystem>();

        foreach (Transform child in gameObject.GetComponentsInChildren<Transform>())
        {
            if (child.tag == "Leg")
            {
                legs.Add(child.gameObject);
            }
            else if (child.tag == "GridFinX" | child.tag == "GridFinZ" | child.tag == "GridFinXR" | child.tag == "GridFinZR")
            {
                gridFins.Add(child.gameObject);
            }
        }

        // collect rocket effects
        foreach (ParticleSystem ps in thrustPoint.gameObject.GetComponentsInChildren<ParticleSystem>())
        {
            rocketEffects.Add(ps);
        }
    }

    public void Awake()
    {
        // initialize controls
        controls = new PlayerControls();
        // define player input callbacks
        controls.Gameplay.ThrustDirection.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Gameplay.FireEngine.performed += ctx => thrustInput = ctx.ReadValue<float>();
        controls.Gameplay.ResetSimulation.performed += ctx => ToggleAI();

        controls.Gameplay.ThrustDirection.canceled += ctx => move = Vector2.zero;
        controls.Gameplay.FireEngine.canceled += ctx => thrustInput = 0.0f;

        thrustInput = 0.0f;

        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly)
        {
            usingAI = false;
        }
        else
        {
            usingAI = true;
        }
    }
    public override void OnEpisodeBegin()
    {
        SetReward(0.0f);
        insideTargetVolume = false;

        arena.GetComponent<HoverArenaControl>().Reset();

        ResetEffects();
        SetEffects(0.0f);
        thrustInput = 0;
        rocketEngineSound.volume = 0.0f;

        previousDistanceReward = ComputeDistanceReward();

        Refuel();

        SetLegs(legsDeployed);
        SetFins(gridFinsDeployed);
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        inputX = vectorAction[0];
        inputY = vectorAction[1];
        // normalize thrust input to between 0 and 1
        thrustInput = ScaleAction(vectorAction[2], 0, 1);
    }


    void FixedUpdate()
    {
        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly)
        {
            RequestDecision();
        }
        else
        {
            // otherwise only every second update
            if (StepCount % 2 == 0)
            {
                RequestDecision();
            }
            else
            {
                RequestAction();
            }
        }


        // turn motor
        thrustPoint.transform.localRotation = Quaternion.Euler(-inputY * maxEngineAngle, 0, inputX * maxEngineAngle);
        // turn grid fins
        foreach (GameObject fin in gridFins)
        {
            float targetPosition;
            // when going down we need to reverse grid fin inputs to make them match effect of the rocket input
            float goingUp = Mathf.Sign(Vector3.Dot(GetComponent<Rigidbody>().velocity, transform.up) + 0.02f);

            switch (fin.tag)
            {
                case "GridFinX":
                    //targetPosition = - goingUp * Move.y * 20f;
                    targetPosition = -goingUp * inputY * maxGridFinAngle;
                    break;
                case "GridFinXR":
                    targetPosition = goingUp * inputY * maxGridFinAngle;
                    //targetPosition = goingUp * Move.y * 20f;
                    break;
                case "GridFinZ":
                    targetPosition = goingUp * inputX * maxGridFinAngle;
                    break;
                case "GridFinZR":
                    targetPosition = -goingUp * inputX * maxGridFinAngle;
                    break;
                default:
                    Debug.Log("reached default case");
                    targetPosition = 0;
                    break;
            }

            try
            {
                fin.GetComponent<FoldingFinControl>().setAngle(targetPosition);
            }
            catch
            {
                Debug.Log("Fin probably broke");
            }
        }

        thrust = thrustCurve.Evaluate(thrustInput);

        if (thrust > 0 && currentFuel > 0)
        {
            // sound
            rocketEngineSound.volume = thrust;
            rocketEngineSound.pitch = 0.5f * thrust + 0.5f;

            if (showEffects)
            {
                SetEffects(thrust);
            }

            GetComponent<Rigidbody>().mass = dryMass + fuelMass * currentFuel / maxFuel;
            GetComponent<Rigidbody>().AddForceAtPosition(thrustPoint.transform.up * thrust * thrustForce * Time.deltaTime, thrustPoint.transform.position, ForceMode.Impulse);
        }
        else
        {
            SetEffects(0.0f);
            rocketEngineSound.volume = 0;
        }

        // REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS
        // REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS
        // REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS
        // REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS

        if (Vector3.Angle(transform.up, Vector3.up) > 100)
        {
            //Debug.Log("tilted");
            AddReward(-20);
            //KillMomentum();
            EndEpisode();
        }

        if (insideTargetVolume)
        {
            //Debug.Log("inside target volume");
            AddReward(3f * Time.deltaTime);
        }

        float distanceReward = ComputeDistanceReward();
        AddReward(distanceReward - previousDistanceReward);
        previousDistanceReward = distanceReward;
    }

    public override void Heuristic(float[] actionsOut)
    {
        controls.Gameplay.Enable();

        actionsOut[0] = move.x;
        actionsOut[1] = move.y;
        // scale thrustInput to between -1 and 1 to match the neural network
        actionsOut[2] = (thrustInput * 2f) - 1f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target distance stuff
        Vector3 targetVector = ComputeTargetVector();
        //Debug.Log("delta time: " + Time.deltaTime);
        Vector3 deltaTargetVector = (targetVector - previousTargetVector) / Mathf.Max(1e-4f, Time.deltaTime);
        previousTargetVector = targetVector;

        // scale this to be smaller
        targetVector *= 0.01f;
        deltaTargetVector *= 0.01f;

        // Pitch stuff
        float pitch = -Mathf.Asin(transform.InverseTransformDirection(Vector3.up).z);
        float roll = -Mathf.Asin(transform.InverseTransformDirection(Vector3.up).x);
        float deltaPitch = (pitch - previousPitch) / Mathf.Max(1e-4f, Time.deltaTime);
        float deltaRoll = (roll - previousRoll) / Mathf.Max(1e-4f, Time.deltaTime);
        previousPitch = pitch;
        previousRoll = roll;

        sensor.AddObservation(targetVector);
        sensor.AddObservation(deltaTargetVector);

        sensor.AddObservation(pitch);
        sensor.AddObservation(roll);
        sensor.AddObservation(deltaRoll);
        sensor.AddObservation(deltaPitch);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == target.name)
        {
            Debug.Log("Entered " + target.name);
            insideTargetVolume = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name == target.name)
        {
            Debug.Log("Exited " + target.name);
            insideTargetVolume = false;
        }
    }

    private Vector3 ComputeTargetVector()
    {
        Vector3 horizontalForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 horizontalRight = new Vector3(transform.right.x, 0, transform.right.z).normalized;

        float targetDistanceForward = Vector3.Dot(target.transform.position - transform.position, horizontalForward);
        float targetDistanceRight = Vector3.Dot(target.transform.position - transform.position, horizontalRight);
        float targetDistanceVertical = (target.transform.position - transform.position).y;

        return new Vector3(targetDistanceRight, targetDistanceVertical, targetDistanceForward);
    }

    private void ToggleAI()
    {
        if (usingAI)
        {
            GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.HeuristicOnly;
            usingAI = false;
            EndEpisode();
        }
        else
        {
            GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.Default;
            usingAI = true;
            EndEpisode();
        }
    }

    private float ComputeDistanceReward()
    {
        return 30.0f / Mathf.Max(1, Mathf.Sqrt((transform.position - target.transform.position).magnitude / 10f));
    }

    void SetEffects(float magnitude)
    {
        foreach (ParticleSystem ps in rocketEffects)
        {
            if (magnitude == 0.0f)
            {
                ParticleSystem.EmissionModule em = ps.emission;
                em.enabled = false;
            }
            else
            {
                ParticleSystem.EmissionModule em = ps.emission;
                em.enabled = true;

                ParticleSystem.MainModule mm = ps.main;
                if (ps.tag == "smoke")
                {
                    mm.startSize = Random.Range(1 * magnitude, 3 * magnitude);
                }
                else
                {
                    mm.startSize = magnitude;
                }
            }
        }
    }

    void SetLegs(bool legsFlag)
    {
        float targetAngle;

        if (legsFlag == true)
        {
            //Debug.Log("opened legs");
            targetAngle = 130f;
        }
        else
        {
            targetAngle = 0f;
        }


        foreach (GameObject leg in legs)
        {
            try
            {
                JointSpring spr = leg.GetComponent<HingeJoint>().spring;
                spr.targetPosition = targetAngle;
                leg.GetComponent<HingeJoint>().spring = spr;
            }
            catch
            {
                Debug.Log("Leg probably broke");
            }
        }
    }

    void SetFins(bool finsFlag)
    {
        foreach (GameObject fin in gridFins)
        {
            fin.GetComponent<FoldingFinControl>().setFold(finsFlag);
        }
    }

    public override void ResetEffects()
    {
        //Debug.Log(rocketEffects);
        foreach (ParticleSystem ps in rocketEffects)
        {
            ps.Clear();
        }
    }

    public override Vector2 GetXYInputs()
    {
        return new Vector2(inputX, inputY);
    }

    public override float GetSpeed()
    {
        return GetComponent<Rigidbody>().velocity.magnitude;
    }
    public override float GetFuel()
    {
        return currentFuel / maxFuel;
    }

    public override float GetThrust()
    {
        return thrust;
    }

    public override void Refuel()
    {
        currentFuel = startingFuel * maxFuel;
    }

    public override bool IsUsingAI()
    {
        return usingAI;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.TransformVector(previousTargetVector.normalized) * 10);
    }
}
