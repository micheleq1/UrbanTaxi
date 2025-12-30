using System.Collections.Generic;
using UnityEngine;

public class IntersectionController : MonoBehaviour
{
    private NpcCarWaypoint currentCar = null;
    private Queue<NpcCarWaypoint> queue = new Queue<NpcCarWaypoint>();

    [Header("Safety")]
    [Tooltip("Tempo massimo (s) in cui un'auto può occupare l'incrocio")]
    public float maxOccupationTime = 5f;

    private float occupationTimer = 0f;

    void Update()
    {
        if (currentCar == null) return;

        // Se l'auto è ferma o guasta troppo a lungo → rilascio forzato
        if (currentCar.IsBroken() || currentCar.GetSpeedMagnitude() < 0.1f)
        {
            occupationTimer += Time.deltaTime;

            if (occupationTimer >= maxOccupationTime)
            {
                ForceRelease();
            }
        }
        else
        {
            occupationTimer = 0f;
        }
    }

    public void RequestEnter(NpcCarWaypoint car)
    {
        if (car == null) return;
        if (car == currentCar) return;
        foreach (var c in queue) if (c == car) return;

        if (currentCar == null)
        {
            currentCar = car;
            occupationTimer = 0f;
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

        ReleaseCurrent();
    }

    private void ForceRelease()
    {
        if (currentCar != null)
            currentCar.SetIntersectionPermission(false);

        ReleaseCurrent();
    }

    private void ReleaseCurrent()
    {
        currentCar = null;
        occupationTimer = 0f;

        if (queue.Count > 0)
        {
            var next = queue.Dequeue();
            currentCar = next;
            next.SetIntersectionPermission(true);
        }
    }
}
