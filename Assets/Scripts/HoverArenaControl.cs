using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;

public class HoverArenaControl : MonoBehaviour
{
    [Header("Player and target")]
    public GameObject player;
    public GameObject target;

    [Header("Spawning parameters")]
    public float deadAngle;
    public float startingDistance = 100f;

    [Header("Info panel components")]
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
    public GameObject deltaZ;
    public GameObject xSpeed;
    public GameObject ySpeed;
    public GameObject zSpeed;

    private RocketAgent playerAgent;

    PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
        //controls.Gameplay.ResetSimulation.performed += ctx => playerAgent.EndEpisode();
        playerAgent = player.GetComponent<RocketAgent>();
        target.name = "target" + Random.Range(10, 99);
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
            Vector3 playerVelocity = player.GetComponent<Rigidbody>().velocity;

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
            deltaZ.GetComponent<TextMeshProUGUI>().text = playerPosition.y.ToString("0") + " m";
            xSpeed.GetComponent<TextMeshProUGUI>().text = playerVelocity.x.ToString("0") + " m/s";
            ySpeed.GetComponent<TextMeshProUGUI>().text = playerVelocity.z.ToString("0") + " m/s";
            zSpeed.GetComponent<TextMeshProUGUI>().text = playerVelocity.y.ToString("0") + " m/s";
        }
    }

    IEnumerator SetNewVelocityTwice(Vector3 newV)
    {
        // zero all movement
        Rigidbody rb = player.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        player.transform.up = -newV.normalized;
        
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
        //playerAgent.ResetEffects();

        float vAngle = Random.Range(deadAngle, 180 - deadAngle);
        float hAngle = Random.Range(0f, 365f);

        float target_size = Academy.Instance.EnvironmentParameters.GetWithDefault("target_size", 50.0f);
        target.transform.localScale = new Vector3(target_size, target_size, target_size);

        player.transform.position = target.transform.position + Quaternion.Euler(0f, hAngle, vAngle) * Vector3.up * startingDistance;

        StartCoroutine(SetNewVelocityTwice(Vector3.zero));

        if (title)
        {
            if (player.GetComponent<RocketAgent>().IsUsingAI())
            {
                title.GetComponent<TextMeshProUGUI>().text = "Control:\tAI";
                infoBG.GetComponent<Image>().color = new Color32(0x00, 0x40, 0xFD, 0xFF);
            }
            else
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
