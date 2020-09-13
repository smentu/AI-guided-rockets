using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LanderArenaControl : MonoBehaviour
{
    // arena contents
    public GameObject platform;

    public GameObject title;
    public GameObject infoBG;
    public GameObject rewardNumber;
    public GameObject speedNumber;
    public GameObject XInputBar;
    public GameObject YInputBar;
    public GameObject FuelBar;
    public GameObject ThrustBar;

    public GameObject deltaX;
    public GameObject deltaY;
    public GameObject height;
    public GameObject xSpeed;
    public GameObject ySpeed;
    public GameObject zSpeed;

    //public GameObject playerPrefab;
    //public GameObject followCamera;
    public GameObject player;
    //public GameObject coneOrigin;
    //public AnimationCurve speedLimitCurve;

    private RocketAgent playerAgent;
    [Tooltip("Whether to randomize y rotation")]
    public bool randomizeY = false;

    //public GameObject target;

    // arena params
    public float startingHeight = 400f;
    public float speedVariance = 3f;

    PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();

        controls.Gameplay.ResetSimulation.performed += ctx => playerAgent.EndEpisode();

        playerAgent = player.GetComponent<RocketAgent>();
        //followCamera.GetComponent<fixed_follow_camera>().player = player.transform;

        platform.name = "platform" + Random.Range(10, 99);
    }

    // Start is called before the first frame update
    void Start()
    {
        //Reset();
    }

    // Update is called once per frame
    void Update()
    {
        if (rewardNumber)
        {
            Vector3 playerPosition = player.transform.position;
            Vector3 playeVelocity = player.GetComponent<Rigidbody>().velocity;

            //float currentReward = ((originalDistance - distanceToTarget()) / originalDistance);
            float currentReward = playerAgent.GetCumulativeReward();
            rewardNumber.GetComponent<TextMeshProUGUI>().text = currentReward.ToString("0.0");

            float currentSpeed = playerAgent.GetSpeed();
            speedNumber.GetComponent<TextMeshProUGUI>().text = currentSpeed.ToString("0.0") + " m/s";

            Vector2 currentInputs = playerAgent.GetXYInputs();

            XInputBar.GetComponent<inputBarController>().setFill((currentInputs.x + 1) / 2);
            YInputBar.GetComponent<inputBarController>().setFill((currentInputs.y + 1) / 2);

            float fuelFraction = player.GetComponent<RocketAgent>().GetFuel();
            FuelBar.GetComponent<inputBarController>().setFill(fuelFraction);

            float thrustFraction = player.GetComponent<RocketAgent>().GetThrust();
            ThrustBar.GetComponent<inputBarController>().setFill(thrustFraction);

            deltaX.GetComponent<TextMeshProUGUI>().text = playerPosition.x.ToString("0") + " m";
            deltaY.GetComponent<TextMeshProUGUI>().text = playerPosition.z.ToString("0") + " m";
            height.GetComponent<TextMeshProUGUI>().text = playerPosition.y.ToString("0") + " m";
            xSpeed.GetComponent<TextMeshProUGUI>().text = playeVelocity.x.ToString("0") + " m/s";
            ySpeed.GetComponent<TextMeshProUGUI>().text = playeVelocity.z.ToString("0") + " m/s";
            zSpeed.GetComponent<TextMeshProUGUI>().text = playeVelocity.y.ToString("0") + " m/s";
        }
    }

    IEnumerator SetNewVelocityTwice(Vector3 newV)
    {
        //float yRotation = Random.Range(0, 364);
        //for (float i = 1; i <= 2; i += 1)
        //{
        //    player.transform.up = -newV.normalized;
        //    if (randomizeY == true)
        //    {
        //        player.transform.Rotate(0, yRotation, 0, Space.Self);
        //    }

        //    foreach (Rigidbody rb in gameObject.GetComponentsInChildren<Rigidbody>())
        //    {
        //        rb.angularVelocity = Vector3.zero;
        //        rb.velocity = newV;
        //    }

        //    //Debug.Log("reset n:o " + i);
        //    yield return null;
        //}

        // zero all movement
        Rigidbody rb = player.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        player.transform.up = -newV.normalized;
        if (randomizeY == true)
        {
            player.transform.Rotate(0, Random.Range(0, 364), 0, Space.Self);
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // wait for 0.1 seconds
        float startTime = Time.time;
        //for (float i = 1; i <= 10; i += 1)
        //    yield return null;
        while (Time.time < startTime + 0.1f)
        {
            yield return null;
        }

        rb.isKinematic = false;

        foreach (Rigidbody rbc in gameObject.GetComponentsInChildren<Rigidbody>())
        {
            rbc.angularVelocity = Vector3.zero;
            rbc.velocity = newV;
        }
        yield return null;
    }

    public void Reset()
    {

        playerAgent.ResetEffects();

        //float resetHeight = 400;

        float xOffset = Random.Range(-0.15f * startingHeight, 0.15f * startingHeight);
        float yOffset = Random.Range(-0.15f * startingHeight, 0.15f * startingHeight);

        float xDisturbance = 1 + Random.Range(-speedVariance, speedVariance);
        float yDisturbance = 1 + Random.Range(-speedVariance, speedVariance);

        //player = Instantiate(playerPrefab, new Vector3(xOffset, 3000, yOffset), Quaternion.identity, transform);
        player.transform.position = new Vector3(xOffset, startingHeight, yOffset) + platform.transform.position;

        Vector3 newVelocity = new Vector3(- 0.2f * xOffset + xDisturbance,
            -startingHeight / 8,
            -0.2f * yOffset + yDisturbance);

        //player.transform.position = new Vector3(0, 100, 0);
        //Vector3 newVelocity = new Vector3(0, -50, 0);

        //player.transform.up = -newVelocity.normalized;
        //
        //foreach (Rigidbody rb in gameObject.GetComponentsInChildren<Rigidbody>())
        //{
        //    rb.angularVelocity = Vector3.zero;
        //    rb.velocity = newVelocity;
        //}
        StartCoroutine(SetNewVelocityTwice(newVelocity));

        playerAgent.Refuel();

        if (player.GetComponent<RocketAgent>().IsUsingAI())
        {
            title.GetComponent<TextMeshProUGUI>().text = "Control:\tAI";
            infoBG.GetComponent<Image>().color = new Color32(0x00, 0x40, 0xFD, 0xFF);
        } else
        {
            title.GetComponent<TextMeshProUGUI>().text = "Control:\tPlayer";
            infoBG.GetComponent<Image>().color = new Color32(0x33, 0x33, 0x33, 0xFF);
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
}
