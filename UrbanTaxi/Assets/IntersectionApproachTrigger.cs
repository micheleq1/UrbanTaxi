using UnityEngine;

public class IntersectionApproachTrigger : MonoBehaviour
{
    public IntersectionController intersection;

    void OnTriggerEnter(Collider other)
    {
        var car = other.GetComponentInParent<NpcCarWaypoint>();
        if (car != null && intersection != null)
            intersection.RequestEnter(car);
    }
}
