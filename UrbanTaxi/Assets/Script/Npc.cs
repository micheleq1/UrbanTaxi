using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class NpcCarWaypoint : MonoBehaviour, IIntersectionVehicle
{
    
    public Transform[] waypoints;
    public bool loop = true;

    
    public float speed = 6f;
    public float turnSpeed = 6f;
    public float reachDistance = 1.5f;

    
    public LayerMask carLayer;
    public LayerMask obstacleLayer;
    public float sensorLength = 8f;
    public float stopDistance = 2f;
    public float sensorHeight = 0.6f;

    
    public float sensorRadius = 0.6f;
    public float sensorForwardOffset = 0.5f;

    
    public float incidentCheckInterval = 20f;
    public float incidentProbability = 0.05f;
    public float incidentDuration = 20f;

    
    public ParticleSystem smokeFX;

   
    private bool canEnterIntersection = true;
    public void SetIntersectionPermission(bool canEnter) => canEnterIntersection = canEnter;

    
    private Rigidbody rb;
    private int idx = 0;

    private bool isBroken = false;
    private float incidentTimer = 0f;
    private float incidentCheckTimer = 0f;

    
    private float segmentT = 0f;
    private float segmentLength = 1f;
    private Vector3 segmentStart;
    private Vector3 segmentEnd;

    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;        
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        InitSegment();
    }

    void InitSegment()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        segmentStart = rb.position;
        segmentEnd = waypoints[idx].position;
        segmentLength = Mathf.Max(0.01f, Vector3.Distance(segmentStart, segmentEnd));
        segmentT = 0f;
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (waypoints[idx] == null) return;

        
        if (isBroken)
        {
            incidentTimer += Time.fixedDeltaTime;
            if (incidentTimer >= incidentDuration)
                EndIncident();
        }

        Vector3 dir = segmentEnd - rb.position;
        dir.y = 0f;

        
        if (segmentT >= 1f || dir.magnitude < reachDistance)
        {
            idx++;
            if (idx >= waypoints.Length)
                idx = loop ? 0 : waypoints.Length - 1;

            InitSegment();
            return;
        }

        Vector3 forward = dir.normalized;

        
        Quaternion look = Quaternion.LookRotation(forward);
        rb.MoveRotation(
            Quaternion.Slerp(rb.rotation, look, turnSpeed * Time.fixedDeltaTime)
        );

        float desiredSpeed = speed;

        
        if (isBroken || !canEnterIntersection)
        {
            desiredSpeed = 0f;
        }
        else
        {
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

            
            LayerMask sensorMask = carLayer | obstacleLayer;

            Vector3 forwardDir = rb.rotation * Vector3.forward;
            Vector3 origin = rb.position + Vector3.up * sensorHeight + forwardDir * sensorForwardOffset;

            if (Physics.SphereCast(
                origin,
                sensorRadius,
                forwardDir,
                out RaycastHit hit,
                sensorLength,
                sensorMask,
                QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && hit.collider.attachedRigidbody != rb)
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

        
        float step = (desiredSpeed / segmentLength) * Time.fixedDeltaTime;
        segmentT = Mathf.Clamp01(segmentT + step);

        Vector3 targetPos = Vector3.Lerp(segmentStart, segmentEnd, segmentT);
        rb.MovePosition(targetPos);
    }

    
    void StartIncident()
    {
        isBroken = true;
        incidentTimer = 0f;
        if (smokeFX != null) smokeFX.Play();
    }

    void EndIncident()
    {
        isBroken = false;
        incidentTimer = 0f;
        if (smokeFX != null) smokeFX.Stop();
    }

    
    void OnDrawGizmosSelected()
    {
        Vector3 fwd = Application.isPlaying && rb != null
            ? (rb.rotation * Vector3.forward)
            : transform.forward;

        Vector3 origin = transform.position + Vector3.up * sensorHeight + fwd * sensorForwardOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, sensorRadius);
        Gizmos.DrawLine(origin, origin + fwd * sensorLength);
    }

    
    public bool IsBroken() => isBroken;
    public float GetSpeedMagnitude() => speed * (isBroken ? 0f : 1f);
}
