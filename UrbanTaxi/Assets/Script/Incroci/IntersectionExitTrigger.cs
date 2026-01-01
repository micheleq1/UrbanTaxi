using UnityEngine;

public class IntersectionExitTrigger : MonoBehaviour
{
    public IntersectionController intersection;

    void OnTriggerExit(Collider other)
    {
        var vehicle = other.GetComponentInParent<IIntersectionVehicle>();
        if (vehicle != null && intersection != null)
            intersection.NotifyExit(vehicle);
    }
}
