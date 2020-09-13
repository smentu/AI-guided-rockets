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
    [Tooltip("Whether to reset automatically after hitting ground")]
    public bool resetOnTouchdown = true;

    PlayerControls controls;

    [Header("physics parameters")]
    public float fuelMass = 80f;
    public float dryMass = 20f;
    public float thrustForce = 2500;
    [Range(0.0f, 1.0f)]
    public float startingFuel;
    public AnimationCurve thrustCurve;
    public float limitCeiling = 150;
    public float maxSpeed = 30f;
    public AnimationCurve speedLimitCurve;

    private GameObject landingPad;
    private float currentFuel;
    private bool touchDown;
    private float previousHeight;
    private float previousPitch;
    private float previousRoll;
    private Vector3 previousTargetVector;
    private float previousDistanceReward;


    // lander state variables
    private float inputX;
    private float inputY;
    private float thrust;
    private bool legsDeployed = false;
    private float speedLimit;
    private int nLegsTouching;
    private bool usingAI;
    //private bool gridFinsDeployed = false;

    // lander child components
    private List<GameObject> legs;
    private List<GameObject> gridFins;
    private List<ParticleSystem> rocketEffects;
    public LanderArenaControl arena;
    //private GameObject coneOrigin;

    // player inputs
    private Vector2 move;
    private float thrustInput;

    public override void Initialize()
    {
        base.Initialize();
        currentFuel = maxFuel;
        arena = GetComponentInParent<LanderArenaControl>();

        landingPad = arena.platform;
        //initialDistance = (thrustPoint.transform.position - landingPad.transform.position).magnitude;

        // collect and categorize legs and grid fins
        legs = new List<GameObject>();
        gridFins = new List<GameObject>();
        rocketEffects = new List<ParticleSystem>();

        foreach (Transform child in gameObject.GetComponentsInChildren<Transform>())
        {
            if (child.tag == "Leg")
            {
                legs.Add(child.gameObject);
                //Debug.Log("found leg: " + child.name);
            }
            else if (child.tag == "GridFinX" | child.tag == "GridFinZ" | child.tag == "GridFinXR" | child.tag == "GridFinZR")
            {
                //Debug.Log("found fin: " + child.name);
                gridFins.Add(child.gameObject);
            }
        }

        // collect rocket effects
        foreach (ParticleSystem ps in thrustPoint.gameObject.GetComponentsInChildren<ParticleSystem>())
        {
            //Debug.Log("found particle system: " + ps.name);
            rocketEffects.Add(ps);
        }

        //coneOrigin = arena.coneOrigin;
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
        } else
        {
            usingAI = true;
        }
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        //inputX = Mathf.Sign(vectorAction[0]) * Mathf.Abs(Mathf.Pow(vectorAction[0], 2));
        //inputY = Mathf.Sign(vectorAction[1]) * Mathf.Abs(Mathf.Pow(vectorAction[1], 2));
        inputX = vectorAction[0];
        inputY = vectorAction[1];
        // normalize thrust input to between 0 and 1
        thrustInput = ScaleAction(vectorAction[2], 0, 1);
    }

    public override void OnEpisodeBegin()
    {
        //SetReward(0.0f);
        //previousDistanceReward = 0;
        // reset arena
        arena.Reset();
        // clear and reset effects
        ResetEffects();
        SetEffects(0.0f);

        //initialDistance = (thrustPoint.transform.position - landingPad.transform.position).magnitude;
        //Debug.Log("the initial distance is: " + initialDistance);

        thrustInput = 0;

        touchDown = false;
        previousHeight = transform.position.y;
        previousTargetVector = computeTargetVector();
        //previousDistance = (transform.position - arena.platform.transform.position).magnitude;
        previousDistanceReward = ComputeDistanceReward();
        //SetReward(0.0f);
        SetReward(previousDistanceReward);

        foreach (GameObject leg in legs)
        {
            leg.GetComponent<LegSensor>().Reset();
        }


        previousPitch = -Mathf.Asin(transform.InverseTransformDirection(Vector3.up).z);
        previousRoll = -Mathf.Asin(transform.InverseTransformDirection(Vector3.up).x);

        nLegsTouching = 0;
    }

    //private void KillMomentum()
    //{
    //    GetComponent<Rigidbody>().velocity = Vector3.zero;
    //    GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    //}

    // Update is called once per frame
    void Update()
    {
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
            RequestDecision();
        } else
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

        // REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS 
        // REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS 
        // REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS 

        //AddReward(-0.1f * Mathf.Max(0, GetComponent<Rigidbody>().velocity.y * Time.deltaTime));

        float distanceReward = ComputeDistanceReward();

        //AddReward(ComputeDistanceReward() * Time.deltaTime);

        AddReward(distanceReward - previousDistanceReward);
        previousDistanceReward = distanceReward;

        int legsOnGround = 0;
        //float legsOnGroundScore = 0f;

        foreach (GameObject leg in legs)
        {
            if (leg.GetComponent<LegSensor>().OnPad()) // || leg.GetComponent<LegSensor>().OnGround())
            {
                legsOnGround += 1;
            }
        }
        //        legsOnGroundScore += 0.5f;
        //    } else if (leg.GetComponent<LegSensor>().OnGround())
        //    {
        //        legsOnGround += 1;
        //        legsOnGroundScore += 0.25f;
        //    }
        //}

        AddReward((legsOnGround - nLegsTouching) * 5);

        nLegsTouching = legsOnGround;

        //AddReward(legsOnGroundScore * Time.deltaTime);
        float height = transform.position.y;

        //float limitCeiling = 100;
        //float maxSpeed = 50f;
        float heightMultiplier = 1;

        if (height < limitCeiling && height > 0)
        {
            float effectiveHeight = height / limitCeiling;
            speedLimit = speedLimitCurve.Evaluate(effectiveHeight) * maxSpeed;
            heightMultiplier = 1 - height / limitCeiling;
        }
        else
        {
            speedLimit = 0;
        }

        //Debug.Log(GetComponent<Rigidbody>().velocity.magnitude - speedLimit);

        float speedDifference = GetComponent<Rigidbody>().velocity.magnitude - speedLimit;

        //AddReward(-0.5f * Mathf.Log(Mathf.Max(0f, previousHeight - height) * Mathf.Max(0f, GetComponent<Rigidbody>().velocity.magnitude - speedLimit) + 1, 10));
        //AddReward(0.01f * Mathf.Max(0f, previousHeight - height));

        float transformedSpeedReward = -0.3f * heightMultiplier * Mathf.Max(0f, previousHeight - height) * Sigmoid(speedDifference, 0.2f);
        //float transformedSpeedReward = -0.5f * Time.deltaTime * Sigmoid(speedDifference, 0.5f);

        if (height < limitCeiling && height > 0) // && height < previousHeight)
        {
            AddReward(transformedSpeedReward);
        }

        //punishment for going up
        //AddReward(-1f * Mathf.Max(0, GetComponent<Rigidbody>().velocity.y * Time.deltaTime));

        if (GetComponent<Rigidbody>().velocity.y > 5.0f && transform.position.y > 30)
        {
            Debug.Log("started going up");
            AddReward(-10f);
            EndEpisode();
        }

        //Debug.Log("speed limit: " + speedLimit);
        //Debug.Log("speed difference: " + transformedSpeedReward);

            previousHeight = height;

        thrust = thrustCurve.Evaluate(thrustInput);

        if (thrust > 0 && currentFuel > 0 && touchDown == false)
        {
            if (showEffects)
            {
                SetEffects(thrust);
            }

            GetComponent<Rigidbody>().mass = dryMass + fuelMass * currentFuel / maxFuel;
            GetComponent<Rigidbody>().AddForceAtPosition(thrustPoint.transform.up * thrust * thrustForce * Time.deltaTime, thrustPoint.transform.position, ForceMode.Impulse);
        } else
        {
            SetEffects(0.0f);
        }

        if (Vector3.Angle(transform.up, Vector3.up) > 100 && touchDown == false)
        {
            //Debug.Log("tilted");
            AddReward(-20);
            //KillMomentum();
            EndEpisode();
        }
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
        Vector3 targetVector = computeTargetVector();
        //Debug.Log("delta time: " + Time.deltaTime);
        Vector3 deltaTargetVector = (targetVector - previousTargetVector) / Mathf.Max(1e-4f, Time.deltaTime);
        previousTargetVector = targetVector;

        // scale this to be smaller
        targetVector *= 0.01f;
        deltaTargetVector *= 0.01f;

        // Pitch stuff
        float pitch = - Mathf.Asin(transform.InverseTransformDirection(Vector3.up).z);
        float roll = - Mathf.Asin(transform.InverseTransformDirection(Vector3.up).x);
        float deltaPitch = (pitch - previousPitch) / Mathf.Max(1e-4f, Time.deltaTime);
        float deltaRoll = (roll - previousRoll) / Mathf.Max(1e-4f, Time.deltaTime);
        previousPitch = pitch;
        previousRoll = roll;

        // OBSERVATIONS OBSERVATIONS OBSERVATIONS OBSERVATIONS OBSERVATIONS
        // OBSERVATIONS OBSERVATIONS OBSERVATIONS OBSERVATIONS OBSERVATIONS
        // OBSERVATIONS OBSERVATIONS OBSERVATIONS OBSERVATIONS OBSERVATIONS
        // OBSERVATIONS OBSERVATIONS OBSERVATIONS OBSERVATIONS OBSERVATIONS
        // OBSERVATIONS OBSERVATIONS OBSERVATIONS OBSERVATIONS OBSERVATIONS

        //Debug.Log("target vector: " + targetVector);
        //Debug.Log("target vector delta: " + deltaTargetVector);

        sensor.AddObservation(targetVector);
        sensor.AddObservation(deltaTargetVector);

        sensor.AddObservation(pitch);
        sensor.AddObservation(roll);
        sensor.AddObservation(deltaRoll);
        sensor.AddObservation(deltaPitch);

        //sensor.AddObservation(touchDown ? -1f : 1f);

        //Debug.Log(touchDown ? 1f: -1f);
    }

    public void CollisionDetected(Collision lc)
    {
        //float slamSpeedLimit = Academy.Instance.EnvironmentParameters.GetWithDefault("slam_speed", 10.0f);

        if (lc.gameObject.tag == "ground" || lc.gameObject.tag == "Finish")
        {
            //float collisionVelocity = lc.relativeVelocity.magnitude;
            //float punishment = -Mathf.Max(lc.relativeVelocity.magnitude - slamSpeedLimit, 0) / slamSpeedLimit;

            // Debug.Log("leg collided with speed: " + collisionVelocity + " and incurred punishment " + punishment);
            //AddReward(punishment);

            // end episode 10 seconds after first touchdown
            if (touchDown == false)
            {
                touchDown = true;

                // add punishment if the first touchdown exceeds speed limit
                //float collisionVelocity = GetComponent<Rigidbody>().velocity.magnitude;
                //float punishment = -(1 + Vector3.Angle(transform.up, Vector3.up) / 9) * Mathf.Max(collisionVelocity - slamSpeedLimit, 0) / slamSpeedLimit;
                //float reward = (10 - collisionVelocity); //Mathf.Pow(1 - Vector3.Angle(transform.up, Vector3.up) / 90, 3);

                float collisionVelocity = lc.relativeVelocity.magnitude;

                // add reward for landing slowly
                AddReward(30 * 3 / Mathf.Max(collisionVelocity, 3));

                //Debug.Log("touchdown reward: " + reward);
                Debug.Log("touchdown speed: " + collisionVelocity);

                //Invoke("KillMomentum", 10);
                if (resetOnTouchdown == true)
                {
                    Invoke("EndEpisode", 5);
                }
            }
        }
    }

    private float ComputeDistanceReward()
    {
        //Vector3 weightedPosition = Vector3.Scale(transform.position - arena.platform.transform.position + new Vector3(0, -15, 0), new Vector3(1, 0.4f, 1));
        Vector3 targetDelta = transform.position - arena.platform.transform.position;

        Vector2 horizontalDistance = new Vector2(targetDelta.x, targetDelta.z);

        float distanceReward = 150f / Mathf.Sqrt(Mathf.Max(9, horizontalDistance.magnitude));

        //Debug.Log(distanceReward);

        //Debug.Log("distance reward: " + weightedPosition);
        //Debug.Log("distance reward: " + distanceReward);

        return distanceReward;
    }

    private Vector3 computeTargetVector()
    {
        Vector3 horizontalForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 horizontalRight = new Vector3(transform.right.x, 0, transform.right.z).normalized;

        //Debug.Log("forward: " + horizontalForward);
        //Debug.Log("right: " + horizontalRight);

        float targetDistanceForward = Vector3.Dot(landingPad.transform.position - transform.position, horizontalForward);
        float targetDistanceRight = Vector3.Dot(landingPad.transform.position - transform.position, horizontalRight);
        float targetDistanceVertical = (landingPad.transform.position - transform.position).y;

        return new Vector3(targetDistanceRight, targetDistanceVertical, targetDistanceForward);
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

    private static float Sigmoid(float x, float steepness)
    {
        return (1.0f / (1.0f + Mathf.Exp(-x * steepness)) - 0.5f) * 2.0f;
    }

    private static float WeirdSigmoid(float x)
    {
        return (1.0f / (1.0f + Mathf.Exp(-x * 0.5f)) - 0.5f) * 2.0f / (1.0f + Mathf.Exp(-x * 0.1f - 2f));
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.TransformVector(previousTargetVector.normalized) * 10);
        //Gizmos.DrawRay(transform.position, horizontalForward * 10);
        //Gizmos.DrawRay(transform.position, horizontalRight * 10);
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawRay(transform.position, (landingPad.transform.position - transform.position).normalized * 20);
    }
}