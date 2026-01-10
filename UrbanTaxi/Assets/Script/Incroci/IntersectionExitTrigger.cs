using UnityEngine;

public class IntersectionExitTrigger : MonoBehaviour
{
    public IntersectionController intersection;

    private void OnTriggerExit(Collider other)
    {
        var vehicle = other.GetComponentInParent<IIntersectionVehicle>();
        if (vehicle == null || intersection == null) return;

        intersection.NotifyExit(vehicle);
    }
}
