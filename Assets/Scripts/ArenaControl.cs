using TMPro;
using UnityEngine;

public class ArenaControl : MonoBehaviour
{
    // arena contents
    public GameObject platform;
    public GameObject player;
    public GameObject targetPrefab;
    public GameObject rewardText;

    private GameObject target;

    // arena params
    public float minTargetRadius = 30f;
    public float maxTargetRadius = 50f;
    public float max_Target_Angle = 45f;

    private Vector3 center = new Vector3(0f, 0f, 0f);

    // arena state info
    private float targetRadius;
    private float targetVAngle;
    private float targetHAngle;

    private Vector3 playerOriginalPosition;
    private Quaternion playerOriginalRotation;

    private float currentReward = 0f;
    private float originalDistance;

    private float distanceToTarget()
    {
        return Vector3.Distance(player.transform.position, target.transform.position);
    }


    // Start is called before the first frame update
    void Start()
    {
        playerOriginalPosition = player.transform.position;
        playerOriginalRotation = player.transform.rotation;

        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        if (target.GetComponent<TargetCollision>().Collected())
        {
            Destroy(target.gameObject);
            // currentReward = 10.0f;
            Reset();
        }
        else
        {
            currentReward = ((originalDistance - distanceToTarget()) / originalDistance);
            player.GetComponent<RocketControl>().CollectTargetLocation(target.transform.position);
            rewardText.GetComponent<TextMeshProUGUI>().text = currentReward.ToString("0.00");
        }


    }

    private void Reset()
    {
        // delete target if one exists
        //if (target.scene.IsValid())
        //{
        //    Destroy(target.gameObject);
        //}

        // reset player state
        player.transform.position = playerOriginalPosition;
        player.transform.rotation = playerOriginalRotation;
        player.GetComponent<Rigidbody>().velocity = Vector3.zero;
        player.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        player.GetComponent<RocketControl>().Reset();


        // spawn target at random position above the platform
        targetRadius = Random.Range(minTargetRadius, maxTargetRadius);
        targetVAngle = Random.Range(0f, max_Target_Angle);
        targetHAngle = Random.Range(0f, 365f);

        // Debug.Log(targetRadius);
        // Debug.Log(targetVAngle);
        // Debug.Log(targetHAngle);

        Vector3 targetPos = center + Quaternion.Euler(0f, targetHAngle, targetVAngle) * Vector3.up * targetRadius;

        target = Instantiate(targetPrefab.gameObject);
        target.transform.position = targetPos;

        originalDistance = distanceToTarget();

    }
}
