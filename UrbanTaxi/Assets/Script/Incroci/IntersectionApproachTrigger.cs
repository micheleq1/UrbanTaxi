using UnityEngine;

public class IntersectionApproachTrigger : MonoBehaviour
{
    public IntersectionController intersection;

    private void OnTriggerEnter(Collider other)
    {
        var vehicle = other.GetComponentInParent<IIntersectionVehicle>();
        if (vehicle == null || intersection == null) return;

        intersection.RequestEnter(vehicle);
    }
}
