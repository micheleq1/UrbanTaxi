using UnityEngine;

public class IntersectionExitTrigger : MonoBehaviour
{
    public IntersectionController intersection;

    void OnTriggerEnter(Collider other)
    {
        var car = other.GetComponentInParent<NpcCarWaypoint>();
        if (car != null && intersection != null)
            intersection.NotifyExit(car);
    }
}
