using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using System.Collections;

public class LanderAgent : RocketAgent
{
    // parameters, effects, and physics
    [Header("Control parameters")]
    [Tooltip("Fuel capacity, corresponding to seconds of full thrust")]
    public float maxFuel = 100f;
    [Tooltip("How much the engine can vector away from zero")]
    public float maxEngineAngle = 20f;
    [Tooltip("How much the grid fins can swivel away from zero")]
    public float maxGridFinAngle = 20f;
    [Tooltip("How the thrust inputs corresponds to the actual thrust force applied")]
    public AnimationCurve thrustCurve;
    [Tooltip("Whether to reset automatically after hitting ground")]
    public bool resetOnTouchdown = true;
    [Tooltip("Whether to consume fuel")]
    public bool consumeFuel = false; 

    [Header("physics parameters")]
    public float fuelMass = 80f;            // mass of full tank of fuel
    public float dryMass = 20f;             // mass excluding fuel
    public float thrustForce = 1500;        // maximum thrust force produced by the engine
    [Range(0.0f, 1.0f)]
    public float startingFuel;              // what percentage of fuel will be included upon reset

    [Header("Component game objects")]
    [Tooltip("Whether to produce rocket effects")]
    public bool showEffects = false;        // whether to produce rocket effects
    public GameObject ExplosionPrefab;
    [Tooltip("Where the thrust force is applied")]
    public GameObject thrustPoint;          // empty game object on which thrust is applied
    public AudioSource rocketEngineSound;
    public AudioSource rocketEngineShutoffSound;
    public AudioSource bodyBonk;
    public AudioSource explosionSound;

    // lander state variables
    private Vector3 target;                 // the target position for the center of the rocket
    private float currentFuel;              // keep track of how much fuel is in the tank
    private bool touchDown;                 // whether the rocket has touched the ground
    private bool exploded;                  // whether the rocket has exploded
    private float previousPitch;            // temp variable for computing change in pitch
    private float previousRoll;             // temp variable for computing change in roll
    private Vector3 previousTargetVector;   // temp variable for computing change in target direction
    private float previousDistanceReward;   // temp variable for distance reward component
    private float inputX;
    private float inputY;
    private float thrust;
    private bool legsDeployed = false;      // whether legs are stowed or deployed
    private int nLegsTouching;              // keep track of how many legs are currently in contact with ground
    private bool usingAI;                   // whether control is manual or AI
    //private bool insideTargetVolume = false;  // whether the rocket is intersecting with the target volume during training
    //private bool gridFinsDeployed = false;

    // lander child components
    private List<GameObject> legs;          // list of legs attached to the rocket
    private List<GameObject> gridFins;      // list of grid fins attached to rocket
    private List<ParticleSystem> rocketEffects; // list of particle systems attached to rocket
    public LanderArenaControl arena;        // arena game object

    // inputs
    private Vector2 move;
    private float thrustInput;

    // control object for receiving manual inputs
    PlayerControls controls;

    public override void Initialize()
    {
        base.Initialize();  // intialization of the ml-agents agent class
        arena = GetComponentInParent<LanderArenaControl>();  // arena should be immediate parent

        if (arena.training)
        {
            // if training, try to align with the center of the target volume
            target = arena.targetVolume.transform.position;
        }
        else
        {
            // else try to place the center of the rocket 13 meters above the landing pad
            target = arena.platform.transform.position + new Vector3(0, 13, 0);
        }

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
    }

    public void Awake()
    {
        // initialize controls
        controls = new PlayerControls();
        controls.Gameplay.Enable();

        // define player input callbacks
        controls.Gameplay.ThrustDirection.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Gameplay.FireEngine.performed += ctx => thrustInput = ctx.ReadValue<float>();
        controls.Gameplay.ResetSimulation.performed += ctx => ToggleAI();
        controls.Gameplay.ThrustDirection.canceled += ctx => move = Vector2.zero;
        controls.Gameplay.FireEngine.canceled += ctx => thrustInput = 0.0f;

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
        inputX = vectorAction[0];
        inputY = vectorAction[1];
        // normalize thrust input to between 0 and 1
        thrustInput = ScaleAction(vectorAction[2], 0, 1);
    }

    public override void OnEpisodeBegin()
    {
        // reset arena
        arena.Reset();
        // clear and reset effects
        ResetEffects();
        // set thrust input to zero in the start just to be sure
        SetEffects(0.0f);

        thrustInput = 0;

        // initialize temp variables
        touchDown = false;
        exploded = false;
        previousTargetVector = computeTargetVector();
        previousDistanceReward = ComputeDistanceReward();
        previousPitch = -Mathf.Asin(transform.InverseTransformDirection(Vector3.up).z);
        previousRoll = -Mathf.Asin(transform.InverseTransformDirection(Vector3.up).x);
        nLegsTouching = 0;
        rocketEngineSound.volume = 0.0f;

        // set reward to zero in the start
        SetReward(0.0f);

        // reset leg sensors (not touching ground)
        foreach (GameObject leg in legs)
        {
            leg.GetComponent<LegSensor>().Reset();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // automatically deploy legs when below 100 meters
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

        // compute and apply thrust force
        thrust = thrustCurve.Evaluate(thrustInput);

        if (thrust > 0 && currentFuel > 0 && touchDown == false)
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
        } else
        {
            SetEffects(0.0f);
        }

        // REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS 
        // REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS 
        // REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS REWARDS 

        // reset if the rocket turns sideways
        if (Vector3.Angle(transform.up, Vector3.up) > 100 && touchDown == false)
        {
            Debug.Log("tilted");
            AddReward(-20);
            EndEpisode();
        }

        // reward based on distance to target
        float distanceReward = ComputeDistanceReward();

        AddReward(distanceReward - previousDistanceReward);
        previousDistanceReward = distanceReward;

        int legsOnGround = 0;
        foreach (GameObject leg in legs)
        {
            if (leg.GetComponent<LegSensor>().OnPad()) // || leg.GetComponent<LegSensor>().OnGround())
            {
                legsOnGround += 1;
            }
        }


        AddReward((legsOnGround - nLegsTouching) * 5);
        nLegsTouching = legsOnGround;

        //if (insideTargetVolume)
        if ((transform.position - target).magnitude < arena.GetComponent<LanderArenaControl>().getTargetRadius())
        {
            //Debug.Log("inside target volume");
            AddReward(5f * Time.deltaTime);
        }

        // punish going below ground plane during training
        AddReward(-Mathf.Log10(Mathf.Max(0, -transform.position.y) + 1) * Time.deltaTime);

    }

    public override void Heuristic(float[] actionsOut)
    {
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
        sensor.AddObservation(targetVector);        // target vector in a weird local coordinate system
        sensor.AddObservation(deltaTargetVector);   // derivative of target vector in a weird local coordinate system

        sensor.AddObservation(pitch);               // pitch of the rocket
        sensor.AddObservation(roll);                // roll of the rocket
        sensor.AddObservation(deltaRoll);           // derivative of roll
        sensor.AddObservation(deltaPitch);          // derivative of pitch
    }

    public void CollisionDetected(Collision lc)
    {
        //float slamSpeedLimit = Academy.Instance.EnvironmentParameters.GetWithDefault("slam_speed", 10.0f);

        if (lc.gameObject.tag == "ground" || lc.gameObject.tag == "Finish")
        {
            // end episode some seconds after first touchdown
            if (touchDown == false)
            {
                touchDown = true;
                StartCoroutine(StartFade(rocketEngineSound, 0.5f, 0.0f));
                //rocketEngineSound.volume = 0;
                rocketEngineShutoffSound.volume = rocketEngineSound.volume / 10;
                rocketEngineShutoffSound.Play();

                float collisionVelocity = lc.relativeVelocity.magnitude;

                // add reward for landing slowly
                //AddReward(30 * 3 / Mathf.Max(collisionVelocity, 3));
                float touchDownReward = Mathf.Max(40 - collisionVelocity, 0) / 2;
                AddReward(touchDownReward);

                if (collisionVelocity > 12.0f && showEffects && !exploded)
                {
                    exploded = true;
                    Explode();
                }

                //Debug.Log("touchdown reward: " + reward);
                Debug.Log("touchdown speed: " + collisionVelocity + ", reward: " + touchDownReward);

                //Invoke("KillMomentum", 10);
                if (resetOnTouchdown == true)
                {
                    StartCoroutine(TouchdDownCountdown(8));
                }
            }
        }
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    // entering the target volume
    //    if (other.name == arena.targetVolume.name)
    //    {
    //        //Debug.Log("Entered " + arena.targetVolume.name);
    //        insideTargetVolume = true;
    //    }
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    // exiting the target volume
    //    if (other.name == arena.targetVolume.name)
    //    {
    //        //Debug.Log("Exited " + arena.targetVolume.name);
    //        insideTargetVolume = false;
    //    }
    //}

    private float ComputeDistanceReward()
    {
        Vector3 targetDelta = transform.position - target;
        Vector3 weightedDistance = 0.3f * targetDelta;
        // the constants here are quite arbitrarily chosen
        float distanceReward = 75f / Mathf.Sqrt(Mathf.Max(4, weightedDistance.magnitude));
        return distanceReward;
    }

    private Vector3 computeTargetVector()
    {
        Vector3 horizontalForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 horizontalRight = new Vector3(transform.right.x, 0, transform.right.z).normalized;

        //Debug.Log("forward: " + horizontalForward);
        //Debug.Log("right: " + horizontalRight);

        float targetDistanceForward = Vector3.Dot(target - transform.position, horizontalForward);
        float targetDistanceRight = Vector3.Dot(target - transform.position, horizontalRight);
        float targetDistanceVertical = (target - transform.position).y;

        return new Vector3(targetDistanceRight, targetDistanceVertical, targetDistanceForward);
    }

    void SetEffects(float magnitude)
    {
        // set magnitude of rocket effects
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
        // set legs to deployed or stowed
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
            GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.InferenceOnly;
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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.TransformVector(previousTargetVector.normalized) * 10);
        //Gizmos.DrawRay(transform.position, horizontalForward * 10);
        //Gizmos.DrawRay(transform.position, horizontalRight * 10);
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawRay(transform.position, (target.transform.position - transform.position).normalized * 20);
    }

    void OnCollisionEnter(Collision collision)
    {
        // play funny sound when rocket body hits the ground
        bodyBonk.volume = Mathf.Min(1, collision.relativeVelocity.magnitude / 40f);
        bodyBonk.Play();

        if (showEffects && !exploded)
        {
            exploded = true;

            Explode();
        }
    }

    private void Explode()
    {
        explosionSound.Play();

        GameObject explosion = Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);
        //explosion.GetComponent<Rigidbody>().velocity = GetComponent<Rigidbody>().velocity / 2 + Vector3.up * 5;
        explosion.GetComponent<Rigidbody>().velocity = Vector3.up * 5;
    }

    public static IEnumerator StartFade(AudioSource audioSource, float duration, float targetVolume)
    {
        // fade away the engine sound when landing
        float currentTime = 0;
        float start = audioSource.volume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(start, targetVolume, currentTime / duration);
            yield return null;
        }
        yield break;
    }

    private IEnumerator TouchdDownCountdown(float duration)
    {
        float resetTime = Time.time + duration;

        while (Time.time < resetTime)
        {
            // if episode resets before end of countdown, do nothing
            if (touchDown == false)
            {
                yield break;
            } else
            {
                yield return null;
            }
        }

        Debug.Log("Ended episode with reward " + GetCumulativeReward());
        EndEpisode();
    }
}