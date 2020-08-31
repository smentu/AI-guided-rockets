using System.Diagnostics.Tracing;
using TMPro;
using UnityEngine;

public class LanderArenaControl : MonoBehaviour
{
    // arena contents
    public GameObject platform;
    public GameObject rewardText;
    public GameObject speedText;
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
    public GameObject followCamera;
    public GameObject player;
    private RocketAgent playerAgent;

    //public GameObject target;

    // arena params
    public float minTargetRadius = 30f;
    public float maxTargetRadius = 50f;
    public float max_Target_Angle = 45f;

    PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();

        controls.Gameplay.ResetSimulation.performed += ctx => playerAgent.EndEpisode();

        playerAgent = player.GetComponent<RocketAgent>();
        followCamera.GetComponent<fixed_follow_camera>().player = player.transform;
    }

    // Start is called before the first frame update
    void Start()
    {
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        if (rewardText)
        {
            Vector3 playerPosition = player.transform.position;
            Vector3 playeVelocity = player.GetComponent<Rigidbody>().velocity;

            //float currentReward = ((originalDistance - distanceToTarget()) / originalDistance);
            float currentReward = playerAgent.GetCumulativeReward();
            rewardText.GetComponent<TextMeshProUGUI>().text = currentReward.ToString("0.0");

            float currentSpeed = playerAgent.GetSpeed();
            speedText.GetComponent<TextMeshProUGUI>().text = currentSpeed.ToString("0.0") + " m/s";

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

    public void Reset()
    {

        playerAgent.ResetEffects();

        float resetHeight = 400;

        float xOffset = Random.Range(-0.1f * resetHeight, 0.1f * resetHeight);
        float yOffset = Random.Range(-0.1f * resetHeight, 0.1f * resetHeight);

        float xDisturbance = Random.Range(-2, 2);
        float yDisturbance = Random.Range(-2, 2);

        //player = Instantiate(playerPrefab, new Vector3(xOffset, 3000, yOffset), Quaternion.identity, transform);
        player.transform.position = new Vector3(xOffset, resetHeight, yOffset) + platform.transform.position;

        Vector3 newVelocity = new Vector3(- 0.2f * xOffset + xDisturbance,
            -resetHeight / 10,
            -0.2f * yOffset + yDisturbance);
        //player.transform.position = new Vector3(0, 100, 0);
        //Vector3 newVelocity = new Vector3(0, -50, 0);

        player.transform.up = -newVelocity.normalized;

        foreach (Rigidbody rb in gameObject.GetComponentsInChildren<Rigidbody>())
        {
            rb.angularVelocity = Vector3.zero;
            rb.velocity = newVelocity;
        }

        playerAgent.Refuel();
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
