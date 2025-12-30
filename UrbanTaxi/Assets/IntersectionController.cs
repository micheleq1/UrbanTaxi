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

        // evita doppioni in coda
        foreach (var c in queue)
            if (c == car) return;

        if (currentCar == null)
        {
            currentCar = car;
            car.SetIntersectionPermission(true);   // solo questa può entrare
        }
        else
        {
            queue.Enqueue(car);
            car.SetIntersectionPermission(false);  // tutte le altre si fermano
        }
    }

    // chiamata dal trigger di uscita (meglio su OnTriggerExit)
    public void NotifyExit(NpcCarWaypoint car)
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
