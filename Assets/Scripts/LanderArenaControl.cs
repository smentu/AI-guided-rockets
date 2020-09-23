using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;

public class LanderArenaControl : MonoBehaviour
{
    // arena contents
    public GameObject platform;
    public GameObject targetVolume;
    public bool training = true;

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

    public GameObject player;

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

        if (training)
        {
            platform.SetActive(false);
            targetVolume.SetActive(true);
            targetVolume.name = "target" + Random.Range(10, 99);
        }
        else
        {
            platform.SetActive(true);
            targetVolume.SetActive(false);
            platform.name = "platform" + Random.Range(10, 99);
        }

    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // if info canvas is set up, update all the readings
        if (rewardNumber)
        {
            Vector3 playerPosition = player.transform.position;
            Vector3 playeVelocity = player.GetComponent<Rigidbody>().velocity;

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
        // to avoid physics glitches we turn physics off for 0.1 seconds before setting new velocity
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
        if (training)
        {
            float target_size = Academy.Instance.EnvironmentParameters.GetWithDefault("target_size", 50.0f);
            targetVolume.transform.localScale = new Vector3(target_size, target_size, target_size);
        }

        playerAgent.ResetEffects();

        float xOffset = Random.Range(-0.15f * startingHeight, 0.15f * startingHeight);
        float yOffset = Random.Range(-0.15f * startingHeight, 0.15f * startingHeight);

        float xDisturbance = 1 + Random.Range(-speedVariance, speedVariance);
        float yDisturbance = 1 + Random.Range(-speedVariance, speedVariance);

        player.transform.position = new Vector3(xOffset, startingHeight, yOffset) + platform.transform.position;

        Vector3 newVelocity = new Vector3(- 0.2f * xOffset + xDisturbance,
            -startingHeight / 8,
            -0.2f * yOffset + yDisturbance);

        // set velocity
        StartCoroutine(SetNewVelocityTwice(newVelocity));

        playerAgent.Refuel();

        // change title and color of info canvas depending on control type
        if (title)
        {
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
