using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using Unity.MLAgents.Policies;  

public class LanderAgent : RocketAgent
{
    [Tooltip("Fuel capacity, corresponding to seconds of full thrust")]
    public float maxFuel = 100f;
    [Tooltip("How much the engine can vector away from zero")]
    public float maxEngineAngle = 20f;
    [Tooltip("How much the grid fins can swivel away from zero")]
    public float maxGridFinAngle = 20f;
    [Tooltip("Where the thrust force is applied")]
    public GameObject thrustPoint;
    [Tooltip("Whether to produce rocket effects")]
    public bool showEffects = false;

    PlayerControls controls;

    [Header("physics parameters")]
    public float fuelMass = 80f;
    public float dryMass = 20f;
    public float thrustForce = 2500;
    [Range(0.0f, 1.0f)]
    public float startingFuel;
    public AnimationCurve thrustCurve;

    private GameObject landingPad;
    private float currentFuel;
    private float initialDistance;
    private float previousDistanceReward;
    private bool touchDown = false;

    // lander state variables
    private float inputX;
    private float inputY;
    private float thrust;
    private bool legsDeployed = false;
    //private bool gridFinsDeployed = false;

    // lander child components
    private List<GameObject> legs;
    private List<GameObject> gridFins;
    private List<ParticleSystem> rocketEffects;
    private LanderArenaControl arena;

    // player inputs
    private Vector2 move;
    private float thrustInput;

    public override void Initialize()
    {
        base.Initialize();
        currentFuel = maxFuel;
        arena = GetComponentInParent<LanderArenaControl>();

        landingPad = arena.platform;
        initialDistance = (thrustPoint.transform.position - landingPad.transform.position).magnitude;

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
            Debug.Log("found particle system: " + ps.name);
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

        controls.Gameplay.ThrustDirection.canceled += ctx => move = Vector2.zero;
        controls.Gameplay.FireEngine.canceled += ctx => thrustInput = 0.0f;

        thrustInput = 0.0f;
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        inputX = vectorAction[0];
        inputY = vectorAction[1];
        // normalize thrust input to between 0 and 1
        thrustInput = ScaleAction(vectorAction[2], 0, 1);
    }

    public override void OnEpisodeBegin()
    {
        //SetReward(0.0f);
        previousDistanceReward = 0;
        // reset arena
        arena.Reset();
        // clear and reset effects
        ResetEffects();
        SetEffects(0.0f);

        //initialDistance = (thrustPoint.transform.position - landingPad.transform.position).magnitude;
        //Debug.Log("the initial distance is: " + initialDistance);

        thrustInput = 0;
        touchDown = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (showEffects && currentFuel > 0)
        {
            SetEffects(thrust);
        } else
        {
            SetEffects(0.0f);
        }

        if (transform.position.y < 100)
        {
            if (legsDeployed == false)
            {
                SetLegs(true);
                legsDeployed = true;
            }
        }
        else
        {
            if (legsDeployed == true)
            {
                SetLegs(false);
                legsDeployed = false;
            }
        }
    }

    void FixedUpdate()
    {
        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly)
        {
            // if exclusively using heuristic, fetch inputs every update
            RequestDecision();
            RequestAction();
        } else
        {
            // otherwise only every third update
            if (StepCount % 3 == 0)
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
            // consume fuel
            currentFuel = currentFuel - Time.deltaTime * thrust;
            // add punishment for using fuel
            AddReward(-thrust * Time.deltaTime);

            GetComponent<Rigidbody>().mass = dryMass + fuelMass * currentFuel / maxFuel;
            GetComponent<Rigidbody>().AddForceAtPosition(thrustPoint.transform.up * thrust * thrustForce * Time.deltaTime, thrustPoint.transform.position, ForceMode.Impulse);
        }

        // set reward according to current distance to target
        Vector3 targetVector = landingPad.transform.position - thrustPoint.transform.position;
        float distanceToTarget = targetVector.magnitude;
        // a linear and inverse component
        float inverseRewardComponent = 100 * 1 / Mathf.Max(1f, Mathf.Sqrt(distanceToTarget));
        float xyDistanceComponent = -Mathf.Sqrt(Mathf.Pow(targetVector.x, 2) + Mathf.Pow(targetVector.z, 2) + Mathf.Pow(0.01f * targetVector.y, 2));
        //Debug.Log(distanceToTarget);
        float distanceReward = inverseRewardComponent + xyDistanceComponent;
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
        Vector3 targetDelta = landingPad.transform.position - thrustPoint.transform.position;
        float logX = Mathf.Sign(targetDelta.x) * Mathf.Log(Mathf.Abs(targetDelta.x) + 1);
        float logY = Mathf.Sign(targetDelta.y) * Mathf.Log(Mathf.Abs(targetDelta.y) + 1);
        float logZ = Mathf.Sign(targetDelta.z) * Mathf.Log(Mathf.Abs(targetDelta.z) + 1);

        Vector3 logTargetDelta = new Vector3(logX, logY, logZ);
        Vector3 upVector = GetComponent<Transform>().up;
        Vector3 angularVelocity = GetComponent<Rigidbody>().angularVelocity;
        Vector3 velocityVector = GetComponent<Rigidbody>().velocity * 0.01f;

        sensor.AddObservation(logTargetDelta);
        sensor.AddObservation(velocityVector);
        sensor.AddObservation(angularVelocity);
        sensor.AddObservation(upVector);
    }

    void OnCollisionEnter(Collision collider)
    {
        if (collider.gameObject.tag == "ground" || collider.gameObject.tag == "Finish")
        {
            float collisionVelocity = collider.relativeVelocity.magnitude;
            float punishment = -5f * collisionVelocity;

            //Debug.Log("body collided with speed: " + collisionVelocity + " and incurred punishment " + punishment);
            AddReward(punishment);
            if (touchDown == false)
            {
                EndEpisode();   
            }
        }
    }

    public void CollisionDetected(Collision lc)
    {
        if (lc.gameObject.tag == "ground" || lc.gameObject.tag == "Finish")
        {
            float collisionVelocity = lc.relativeVelocity.magnitude;
            float punishment = -0.5f * Mathf.Max(lc.relativeVelocity.magnitude - 1, 0);

            //Debug.Log("child collided with speed: " + collisionVelocity + " and incurred punishment " + punishment);
            AddReward(punishment);

            // end episode 4 seconds after first touchdown
            if (touchDown == false)
            {
                touchDown = true;
                Invoke("EndEpisode", 4);

                // reward for landing on the landing pad
                if (lc.gameObject.tag == "Finish")
                {
                    Debug.Log("Touched down on the landing pad");
                    AddReward(50);
                }
            }
        }
    }

    void SetEffects(float magnitude)
    {
        // Debug.Log("set effects to: " + magnitude);
        //Debug.Log(rocketEffects);
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
}