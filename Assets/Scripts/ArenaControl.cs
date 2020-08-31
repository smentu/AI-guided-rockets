using TMPro;
using UnityEngine;
using Unity.MLAgents;

public class ArenaControl : MonoBehaviour
{
    // arena contents
    public GameObject platform;
    public GameObject player;
    public GameObject rewardText;
    public GameObject speedText;
    public GameObject XInputBar;
    public GameObject YInputBar;
    public GameObject FuelBar;

    private RocketAgent playerAgent;

    public GameObject target;

    // arena params
    public float minTargetRadius = 30f;
    public float maxTargetRadius = 50f;
    public float max_Target_Angle = 45f;

    // arena state info
    private float targetRadius;
    private float targetVAngle;
    private float targetHAngle;

    private Vector3 playerOriginalPosition;
    private Quaternion playerOriginalRotation;

    PlayerControls controls;

    private void Awake()
    {
        //controls = new PlayerControls();

        //controls.Gameplay.ResetSimulation.performed += ctx => Reset();

        playerOriginalPosition = player.transform.position;
        playerOriginalRotation = player.transform.rotation;

        Reset();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (player)
        {
            playerAgent = player.GetComponent<RocketAgent>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (rewardText)
        {
            //float currentReward = ((originalDistance - distanceToTarget()) / originalDistance);
            float currentReward = playerAgent.GetCumulativeReward();
            rewardText.GetComponent<TextMeshProUGUI>().text = currentReward.ToString("0.00");

            float currentSpeed = playerAgent.GetSpeed();
            speedText.GetComponent<TextMeshProUGUI>().text = currentSpeed.ToString("0.0") + " m/s";

            Vector2 currentInputs = playerAgent.GetXYInputs();

            XInputBar.GetComponent<inputBarController>().setFill((currentInputs.x + 1) / 2);
            YInputBar.GetComponent<inputBarController>().setFill((currentInputs.y + 1) / 2);

            float fuelFraction = player.GetComponent<MissileAgent>().GetFuel();

            FuelBar.GetComponent<inputBarController>().setFill(fuelFraction);
        }
    }

    public void Reset()
    {
        // reset player state
        player.transform.position = playerOriginalPosition;
        player.transform.rotation = playerOriginalRotation;
        player.GetComponent<Rigidbody>().velocity = Vector3.zero;
        player.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        //player.GetComponent<RocketControl>().Reset();


        float targetHeight = Academy.Instance.EnvironmentParameters.GetWithDefault("target_height", 75.0f);

        // spawn target at random position above the platform
        // targetRadius = Random.Range(sdfsadf, targetHeight);
        targetVAngle = Random.Range(0f, max_Target_Angle);
        targetHAngle = Random.Range(0f, 365f);

        // Debug.Log(targetRadius);
        // Debug.Log(targetVAngle);
        // Debug.Log(targetHAngle);
        Vector3 targetPos = platform.transform.position + Quaternion.Euler(0f, targetHAngle, targetVAngle) * Vector3.up * targetHeight;

        // target = Instantiate(targetPrefab.gameObject);
        target.transform.position = targetPos;

        //target.transform.localScale = new Vector3(targetScale, targetScale, targetScale);

        // originalDistance = distanceToTarget();

    }
}
