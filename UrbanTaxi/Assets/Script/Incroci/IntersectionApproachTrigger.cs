using UnityEngine;

public class IntersectionApproachTrigger : MonoBehaviour
{
    public IntersectionController intersection;

   void OnTriggerEnter(Collider other)
    {
        var vehicle = other.GetComponentInParent<IIntersectionVehicle>();
        if (vehicle != null && intersection != null)
            intersection.RequestEnter(vehicle);
    }
}
