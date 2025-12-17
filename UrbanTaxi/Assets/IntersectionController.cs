using System.Collections.Generic;
using UnityEngine;

public class IntersectionController : MonoBehaviour
{
    private NpcCarWaypoint currentCar = null;
    private Queue<NpcCarWaypoint> queue = new Queue<NpcCarWaypoint>();

    public void RequestEnter(NpcCarWaypoint car)
    {
        if (car == null) return;

        if (car == currentCar) return;
        foreach (var c in queue) if (c == car) return;

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

    public void NotifyExit(NpcCarWaypoint car)
    {
        if (car == null) return;
        if (car != currentCar) return;

        currentCar = null;

        if (queue.Count > 0)
        {
            var next = queue.Dequeue();
            currentCar = next;
            next.SetIntersectionPermission(true);
        }
    }
}
