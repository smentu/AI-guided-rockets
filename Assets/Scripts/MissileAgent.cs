using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

public class MissileAgent : RocketAgent
{
    [Tooltip("Fuel capacity, corresponding to seconds of full thrust")]
    public float maxFuel=20f;
    [Tooltip("How much the engine can vector away from zero")]
    public float maxEngineAngle=20f;
    public GameObject target;

    public GameObject thrustPoint;
    private ParticleSystem.EmissionModule em;
    public bool makeSmoke = false;

    [Header("physics parameters")]
    public float fuelMass = 8f;
    public float dryMass = 2f;
    public float thrustForce = 15f;
    public Vector3 massOffset = new Vector3(0, -2, 0);

    // public GameObject rocketArena;
    private ArenaControl arena;
    private float currentFuel;
    //private Rigidbody rigidbody;
    private float previousDistance;
    private float originalDistance;

    private Vector2 Move;
    private float engineX;
    private float engineY;
    private bool firing = false;
    private bool usingAI;
    // private float playerFiring;

    PlayerControls controls;

    public override Vector2 GetXYInputs()
    {
        return new Vector2(engineX, engineY);
    }

    public override float GetSpeed()
    {
        return GetComponent<Rigidbody>().velocity.magnitude;
    }
    public override float GetFuel()
    {
        return currentFuel / maxFuel;
    }

    public override void Initialize()
    {
        base.Initialize();
        //currentFuel = maxFuel;
        arena = GetComponentInParent<ArenaControl>();
        target = arena.target;
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        engineX = vectorAction[0];
        engineY = vectorAction[1];
        firing = true;
    }

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Gameplay.Enable();

        em = GetComponentInChildren<ParticleSystem>().emission;

        // define player input callbacks
        controls.Gameplay.ThrustDirection.performed += ctx => Move = ctx.ReadValue<Vector2>();
        controls.Gameplay.ResetSimulation.performed += ctx => ToggleAI();

        controls.Gameplay.ThrustDirection.canceled += ctx => Move = Vector2.zero;
        //controls.Gameplay.FireEngine.canceled += ctx => playerFiring = 0f;

        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly)
        {
            usingAI = false;
        }
        else
        {
            usingAI = true;
        }
    }

    void FixedUpdate()
    {
        thrustPoint.transform.localRotation = Quaternion.Euler(-engineY * maxEngineAngle, 0, engineX * maxEngineAngle);

        if (firing && currentFuel > 0)
        {
            if (makeSmoke)
            {
                em.enabled = true;
            }
            // add punishment for using fuel
            AddReward(-0.1f * Time.deltaTime);

            GetComponent<Rigidbody>().mass = dryMass + fuelMass * currentFuel / maxFuel;
            GetComponent<Rigidbody>().AddForceAtPosition(thrustPoint.transform.up * thrustForce * Time.deltaTime, thrustPoint.transform.position, ForceMode.Impulse);
        }
        else
        {
            em.enabled = false;
        }

        float distanceToTarget = (target.transform.position - transform.position).magnitude;

        if (distanceToTarget < previousDistance)
        {
            AddReward(previousDistance - distanceToTarget);
        }
        else
        {
            // add small punishment for moving away from target
            AddReward(0.75f * (previousDistance - distanceToTarget));
        }
        previousDistance = distanceToTarget;
        //Debug.Log("new previous best: " + previousBest);

        if (StepCount % 5 == 0)
        {
            RequestDecision();
        }
        else
        {
            RequestAction();
        }
    }

    public override void Heuristic(float[] actionsOut)
    {
        actionsOut[0] = Move.x;
        actionsOut[1] = Move.y;
    }

    public override void OnEpisodeBegin()
    {
        arena.Reset();

        GetComponentInChildren<ParticleSystem>().Clear();
        currentFuel = maxFuel;
        previousDistance = (target.transform.position - transform.position).magnitude;

        originalDistance = previousDistance;

        engineX = 0f;
        engineY = 0f;
        firing = false;

        SetReward(0f);
    }

    public override void CollectObservations(VectorSensor sensor) 
    {
        Vector3 targetDirection = (target.transform.position - transform.position).normalized;
        float targetDistance = (target.transform.position - transform.position).magnitude;

        Vector3 stripX = targetDirection - Vector3.Dot(targetDirection, transform.forward) * transform.forward;
        Vector3 stripZ = targetDirection - Vector3.Dot(targetDirection, transform.right) * transform.right;

        float deltaX = Vector3.SignedAngle(stripX, transform.up, transform.forward);
        float deltaY = Vector3.SignedAngle(stripZ, transform.up, transform.right);

        deltaX /= 180f;
        deltaY /= 180f;
        targetDistance /= originalDistance;

        sensor.AddObservation(deltaX);
        sensor.AddObservation(deltaY);
        sensor.AddObservation(targetDistance);

        Vector3 upVector = GetComponent<Transform>().up;
        Vector3 velocityVector = GetComponent<Rigidbody>().velocity * 0.01f;

        sensor.AddObservation(upVector);
        sensor.AddObservation(velocityVector);
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

    public override bool IsUsingAI()
    {
        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.name == target.name)
        {
            AddReward(10f);
            Debug.Log("reached target");
            EndEpisode();
        }
    }
}