using System.Collections.Generic;
using UnityEngine;

    public class IntersectionController : MonoBehaviour
{
    private IIntersectionVehicle currentCar = null;
    private Queue<IIntersectionVehicle> queue = new Queue<IIntersectionVehicle>();

    public void RequestEnter(IIntersectionVehicle car)
    {
        if (car == null) return;
        if (car == currentCar) return;

        foreach (var c in queue)
            if (c == car) return;

        if (currentCar == null)
        {
            currentCar = car;
            car.SetIntersectionPermission(true);
        }
        else
        {
            queue.Enqueue(car);
            car.SetIntersectionPermission(false);
        }
    }

    public void NotifyExit(IIntersectionVehicle car)
    {
        if (car == null) return;
        if (car != currentCar) return;

        ReleaseCurrent();
    }

    private void ReleaseCurrent()
    {
        currentCar = null;

        if (queue.Count > 0)
        {
            var next = queue.Dequeue();
            currentCar = next;
            next.SetIntersectionPermission(true);
        }
    }
}
