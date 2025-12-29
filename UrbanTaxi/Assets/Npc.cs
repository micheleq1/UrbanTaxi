using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NpcCarWaypoint : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform[] waypoints;
    public bool loop = true;

    [Header("Movement")]
    public float speed = 6f;
    public float turnSpeed = 6f;
    public float reachDistance = 1.5f;

    [Header("Traffic Awareness")]
    public LayerMask carLayer;
    public LayerMask obstacleLayer;     // <-- AGGIUNTO (metti qui il layer Obstacles)
    public float sensorLength = 8f;
    public float stopDistance = 2f;
    public float sensorHeight = 0.6f;
    public float brakeStrength = 8f;

    [Header("Obstacle Sensor")]
    public float sensorRadius = 0.6f;   // <-- raggio del “cono” di visione
    public float sensorForwardOffset = 0.5f; // parte un po’ avanti (para-urti)

    // Incroci
    private bool canEnterIntersection = true;
    public void SetIntersectionPermission(bool canEnter) => canEnterIntersection = canEnter;

    private Rigidbody rb;
    private int idx = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (waypoints[idx] == null) return;

        Transform target = waypoints[idx];
        Vector3 dir = target.position - rb.position;
        dir.y = 0f;

        if (dir.magnitude < reachDistance)
        {
            idx++;
            if (idx >= waypoints.Length)
                idx = loop ? 0 : waypoints.Length - 1;
            return;
        }

        Vector3 forward = dir.normalized;

        Quaternion look = Quaternion.LookRotation(forward);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, look, turnSpeed * Time.fixedDeltaTime));

        float desiredSpeed = speed;

        // 1) STOP incrocio
        if (!canEnterIntersection)
        {
            desiredSpeed = 0f;
        }
        else
        {
            // 2) Sensore: auto + ostacoli
            LayerMask sensorMask = carLayer | obstacleLayer;

            Vector3 forwardDir = rb.rotation * Vector3.forward;
            Vector3 origin = rb.position + Vector3.up * sensorHeight + forwardDir * sensorForwardOffset;

            // Uno SphereCast centrale è molto più affidabile di 3 Raycast
            if (Physics.SphereCast(origin, sensorRadius, forwardDir, out RaycastHit hit, sensorLength, sensorMask, QueryTriggerInteraction.Ignore))
            {
                // Evita di “vedere” se stesso
                if (hit.rigidbody == null || hit.rigidbody != rb)
                {
                    float d = hit.distance;

                    if (d <= stopDistance) desiredSpeed = 0f;
                    else
                    {
                        float t = Mathf.InverseLerp(stopDistance, sensorLength, d);
                        desiredSpeed = Mathf.Lerp(0f, speed, t);
                    }
                }
            }
        }

        // Applica velocità con frenata morbida
        Vector3 targetVel = forward * desiredSpeed;
        Vector3 vel = Vector3.Lerp(
            rb.velocity,
            new Vector3(targetVel.x, rb.velocity.y, targetVel.z),
            brakeStrength * Time.fixedDeltaTime
        );
        rb.velocity = vel;
    }

    void OnDrawGizmosSelected()
    {
        // gizmo sensore (in editor non abbiamo rb.rotation affidabile se non in play)
        Vector3 fwd = Application.isPlaying && rb != null ? (rb.rotation * Vector3.forward) : transform.forward;
        Vector3 origin = transform.position + Vector3.up * sensorHeight + fwd * sensorForwardOffset;

        Gizmos.DrawWireSphere(origin, sensorRadius);
        Gizmos.DrawLine(origin, origin + fwd * sensorLength);
        Gizmos.DrawWireSphere(origin + fwd * sensorLength, sensorRadius);
    }
}
