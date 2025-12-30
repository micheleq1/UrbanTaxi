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
    public LayerMask obstacleLayer;
    public float sensorLength = 8f;
    public float stopDistance = 2f;
    public float sensorHeight = 0.6f;
    public float brakeStrength = 8f;

    [Header("Obstacle Sensor")]
    public float sensorRadius = 0.6f;
    public float sensorForwardOffset = 0.5f;

    [Header("Random Breakdown")]
    public float incidentCheckInterval = 20f;
    public float incidentProbability = 0.20f;
    public float incidentDuration = 30f;

    [Header("Visual Effects")]
    public ParticleSystem smokeFX;

    // Incroci
    private bool canEnterIntersection = true;
    public void SetIntersectionPermission(bool canEnter) => canEnterIntersection = canEnter;

    private Rigidbody rb;
    private int idx = 0;

    // Stato incidente
    private bool isBroken = false;
    private float incidentTimer = 0f;
    private float incidentCheckTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // ==========================
        // GESTIONE INCIDENTE
        // ==========================
        if (isBroken)
        {
            incidentTimer += Time.fixedDeltaTime;
            if (incidentTimer >= incidentDuration)
                EndIncident();
        }

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

        // ==========================
        // BLOCCO INCIDENTE
        // ==========================
        if (isBroken)
        {
            desiredSpeed = 0f;
        }
        else
        {
            // 🔴 INCIDENTI SOLO SE L'AUTO È LIBERA (NO INCROCI)
            if (canEnterIntersection)
            {
                incidentCheckTimer += Time.fixedDeltaTime;
                if (incidentCheckTimer >= incidentCheckInterval)
                {
                    incidentCheckTimer = 0f;
                    if (Random.value < incidentProbability)
                        StartIncident();
                }
            }

            // ==========================
            // INCROCI
            // ==========================
            if (!canEnterIntersection)
            {
                desiredSpeed = 0f;
            }
            else
            {
                // Sensore auto + ostacoli
                LayerMask sensorMask = carLayer | obstacleLayer;

                Vector3 forwardDir = rb.rotation * Vector3.forward;
                Vector3 origin = rb.position + Vector3.up * sensorHeight + forwardDir * sensorForwardOffset;

                if (Physics.SphereCast(origin, sensorRadius, forwardDir,
                    out RaycastHit hit, sensorLength, sensorMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.rigidbody == null || hit.rigidbody != rb)
                    {
                        float d = hit.distance;

                        if (d <= stopDistance)
                            desiredSpeed = 0f;
                        else
                        {
                            float t = Mathf.InverseLerp(stopDistance, sensorLength, d);
                            desiredSpeed = Mathf.Lerp(0f, speed, t);
                        }
                    }
                }
            }
        }

        // ==========================
        // APPLICA VELOCITÀ
        // ==========================
        Vector3 targetVel = forward * desiredSpeed;
        Vector3 vel = Vector3.Lerp(
            rb.velocity,
            new Vector3(targetVel.x, rb.velocity.y, targetVel.z),
            brakeStrength * Time.fixedDeltaTime
        );

        rb.velocity = vel;
    }

    // ==========================
    // INCIDENTE
    // ==========================
    void StartIncident()
    {
        isBroken = true;
        incidentTimer = 0f;

        if (smokeFX != null)
            smokeFX.Play();
    }

    void EndIncident()
    {
        isBroken = false;
        incidentTimer = 0f;

        if (smokeFX != null)
            smokeFX.Stop();
    }

    void OnDrawGizmosSelected()
    {
        Vector3 fwd = Application.isPlaying && rb != null
            ? (rb.rotation * Vector3.forward)
            : transform.forward;

        Vector3 origin = transform.position + Vector3.up * sensorHeight + fwd * sensorForwardOffset;

        Gizmos.DrawWireSphere(origin, sensorRadius);
        Gizmos.DrawLine(origin, origin + fwd * sensorLength);
        Gizmos.DrawWireSphere(origin + fwd * sensorLength, sensorRadius);
    }

    // ==========================
    // METODI USATI DAGLI INCROCI
    // ==========================
    public bool IsBroken()
    {
        return isBroken;
    }

    public float GetSpeedMagnitude()
    {
        return rb.velocity.magnitude;
    }
}
