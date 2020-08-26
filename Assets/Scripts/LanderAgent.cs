using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine.Rendering.PostProcessing;

public class LanderAgent : Agent
{
    [Tooltip("Fuel capacity, corresponding to seconds of full thrust")]
    public float maxFuel = 100f;
    [Tooltip("How much the engine can vector away from zero")]
    public float maxEngineAngle = 20f;
    public float maxGridFinAngle = 20f;
    public GameObject target;
    PlayerControls controls;

    public GameObject thrustPoint;
    private ParticleSystem.EmissionModule em;
    public bool makeSmoke = false;

    [Header("physics parameters")]
    public float fuelMass = 80f;
    public float dryMass = 20f;
    public float thrustForce = 1700f;
    public Vector3 massOffset = new Vector3(0, -2, 0);

    // public GameObject rocketArena;
    private ArenaControl arena;
    private float currentFuel;
    //private Rigidbody rigidbody;
    private float previousDistance;
    private float originalDistance;

    private float engineX;
    private float engineY;
    private bool firing = false;
    private bool legsDeployed = false;
    private bool gridFinsDeployed = false;

    private Vector2 Move;
    // private float playerFiring;

    public override void Initialize()
    {
        base.Initialize();
        //currentFuel = maxFuel;
        arena = GetComponentInParent<ArenaControl>();
        target = arena.target;
    }

    bool floatToBool(float value)
    {
        if (value > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool toggleValue(bool value)
    {
        if (value)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        engineX = vectorAction[0];
        engineY = vectorAction[1];
        firing = floatToBool(vectorAction[2]);
        legsDeployed = floatToBool(vectorAction[3]);
        gridFinsDeployed = floatToBool(vectorAction[4]);
    }

    private void Awake()
    {
        //rigidbody = GetComponent<Rigidbody>();
        em = GetComponentInChildren<ParticleSystem>().emission;

        //GetComponent<Rigidbody>().mass = dryMass + fuelMass;
        //gameObject.GetComponent<Rigidbody>().centerOfMass = massOffset;

        controls = new PlayerControls();

        // define player input callbacks
        //controls.Gameplay.FireEngine.performed += ctx => playerFiring = 1f;
        controls.Gameplay.ThrustDirection.performed += ctx => Move = ctx.ReadValue<Vector2>();

        controls.Gameplay.ThrustDirection.canceled += ctx => Move = Vector2.zero;
        //controls.Gameplay.FireEngine.canceled += ctx => playerFiring = 0f;

        //previousBest = (target.transform.position - transform.position).magnitude;
    }

    public Vector2 GetInputs()
    {
        return new Vector2(engineX, engineY);
    }

    public float GetFuel()
    {
        return currentFuel / maxFuel;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        thrustPoint.transform.localRotation = Quaternion.Euler(-engineY * maxEngineAngle, 0, engineX * maxEngineAngle);

        if (firing && currentFuel > 0)
        {
            // Debug.Log("applying thrust");
            if (makeSmoke)
            {
                em.enabled = true;
            }
            // currentFuel = currentFuel - Time.deltaTime;

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
        controls.Gameplay.Enable();

        actionsOut[0] = Move.x;
        actionsOut[1] = Move.y;
        //actionsOut[2] = playerFiring;
    }

    public override void OnEpisodeBegin()
    {
        arena.Reset();

        GetComponentInChildren<ParticleSystem>().Clear();
        currentFuel = maxFuel;
        previousDistance = (target.transform.position - transform.position).magnitude;

        originalDistance = previousDistance;
        //Debug.Log("reset previous best: " + previousBest);

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

        //Debug.Log("asdfasdfsadf");
        //Debug.Log(deltaX);
        //Debug.Log(deltaY);
        //Debug.Log(targetDistance);
        //Debug.Log(upVector);
        //Debug.Log(velocityVector);
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