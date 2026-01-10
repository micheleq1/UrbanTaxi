using System.Collections.Generic;
using UnityEngine;

public class IntersectionController : MonoBehaviour
{
    [Header("Fail-Safe")]
    public float maxOccupancyTime = 25f;
    private IIntersectionVehicle currentCar = null;
    private Queue<IIntersectionVehicle> queue = new Queue<IIntersectionVehicle>();

    private float occupancyTimer = 0f;

    // =========================
    // CHIAMATO DAI TRIGGER
    // =========================

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
            occupancyTimer = 0f; // reset timer
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
        occupancyTimer = 0f;

        if (queue.Count > 0)
        {
            var next = queue.Dequeue();
            currentCar = next;
            next.SetIntersectionPermission(true);
            occupancyTimer = 0f;
        }
    }

    // =========================
    // FAIL-SAFE CONTROLLATO DA FISICA
    // =========================

    private void FixedUpdate()
    {
        if (currentCar == null) return;

        occupancyTimer += Time.fixedDeltaTime;

        if (occupancyTimer >= maxOccupancyTime)
        {
            // Forza rilascio incrocio se qualcosa è andato storto
            ReleaseCurrent();
        }
    }
}
