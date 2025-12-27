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
    public float sensorLength = 8f;
    public float stopDistance = 2f;
    public float sensorHeight = 0.6f;
    public float brakeStrength = 8f;

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

        // Direzione verso waypoint
        Transform target = waypoints[idx];
        Vector3 dir = target.position - rb.position;
        dir.y = 0f;

        // Passa al prossimo waypoint
        if (dir.magnitude < reachDistance)
        {
            idx++;
            if (idx >= waypoints.Length)
                idx = loop ? 0 : waypoints.Length - 1;
            return;
        }

        Vector3 forward = dir.normalized;

        // Rotazione verso waypoint
        Quaternion look = Quaternion.LookRotation(forward);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, look, turnSpeed * Time.fixedDeltaTime));

        // Velocità desiderata base
        float desiredSpeed = speed;

        // 1) STOP/precedenze incrocio (se non ho permesso, mi fermo)
        if (!canEnterIntersection)
        {
            desiredSpeed = 0f;
        }
        else
        {
            // 2) Sensori frontali (robusti): centro + sinistra + destra
            Vector3 forwardDir = rb.rotation * Vector3.forward; // direzione reale fisica
            Vector3 origin = rb.position + Vector3.up * sensorHeight;

            // offset laterali (larghezza “mezza corsia”)
            Vector3 right = rb.rotation * Vector3.right;
            float sideOffset = 0.35f;

            bool hasCarAhead = false;
            float nearestDist = sensorLength;

            RaycastHit hit;
            if (Physics.Raycast(origin, forwardDir, out hit, sensorLength, carLayer))
            {
                if (hit.rigidbody != null && hit.rigidbody != rb) { hasCarAhead = true; nearestDist = Mathf.Min(nearestDist, hit.distance); }
            }
            if (Physics.Raycast(origin + right * sideOffset, forwardDir, out hit, sensorLength, carLayer))
            {
                if (hit.rigidbody != null && hit.rigidbody != rb) { hasCarAhead = true; nearestDist = Mathf.Min(nearestDist, hit.distance); }
            }
            if (Physics.Raycast(origin - right * sideOffset, forwardDir, out hit, sensorLength, carLayer))
            {
                if (hit.rigidbody != null && hit.rigidbody != rb) { hasCarAhead = true; nearestDist = Mathf.Min(nearestDist, hit.distance); }
            }

            if (hasCarAhead)
            {
                float d = nearestDist;

                if (d <= stopDistance) desiredSpeed = 0f;
                else
                {
                    float t = Mathf.InverseLerp(stopDistance, sensorLength, d);
                    desiredSpeed = Mathf.Lerp(0f, speed, t);
                }
            }

        }

        // Applica velocità con frenata morbida (mantiene la Y attuale)
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
        Vector3 origin = transform.position + Vector3.up * sensorHeight;
        Vector3 forwardDir = (Application.isPlaying && rb != null) ? (rb.rotation * Vector3.forward) : transform.forward;
        Gizmos.DrawLine(origin, origin + forwardDir * sensorLength);

    }
}
