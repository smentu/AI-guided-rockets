using UnityEngine;
using Unity.MLAgents;

public class RocketAgent : Agent
{
    //public LanderArenaControl arena;

    public virtual Vector2 GetXYInputs()
    {
        return Vector2.zero;
    }

    public virtual float GetSpeed()
    {
        return 0f;
    }

    public virtual float GetFuel()
    {
        return 0f;
    }

    public virtual float GetThrust()
    {
        return 0f;
    }

    public virtual void ResetEffects()
    {

    }

    public virtual void Refuel()
    {

    }
}
