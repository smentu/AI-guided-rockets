using UnityEngine;

public class FoldingFinControl : MonoBehaviour
{
    public GameObject fin;
    public float liftCoefficient;
    public float dragCoefficient;
    private bool isDeployed = true;

    // Start is called before the first frame update
    void Start()
    {
        setFold(isDeployed);
        fin.GetComponent<GridFinPhysics>().liftCoefficient = liftCoefficient;
        fin.GetComponent<GridFinPhysics>().dragCoefficient = dragCoefficient;
    }

    public void setFold(bool deployed)
    {
        //Debug.Log("set fins to " + deployed);
        JointSpring foldSpring = GetComponent<HingeJoint>().spring;
        if (deployed)
        {
            foldSpring.targetPosition = 0;
            fin.GetComponent<GridFinPhysics>().liftCoefficient = liftCoefficient;
        } else
        {
            foldSpring.targetPosition = -90;
            fin.GetComponent<GridFinPhysics>().liftCoefficient = dragCoefficient;
        }
        GetComponent<HingeJoint>().spring = foldSpring;
        isDeployed = deployed;
    }

    public void setAngle(float targetAngle)
    {
        try
        {
            JointSpring spr = fin.GetComponent<HingeJoint>().spring;
            if (isDeployed)
            {
                spr.targetPosition = targetAngle;
            }
            else
            {
                spr.targetPosition = 0f;
            }
            fin.GetComponent<HingeJoint>().spring = spr;
        }
        catch
        {
            Debug.Log("Fin probably broke");
        }
    }
}
