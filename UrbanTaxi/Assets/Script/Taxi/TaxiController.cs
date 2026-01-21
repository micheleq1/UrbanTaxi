using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class TaxiController : MonoBehaviour, IIntersectionVehicle
{
   
    public TaxiRoadNode currentNode;
    public TaxiRoadNode goalNode;

    
    public float speed = 5f;
    public float laneOffset = 1.5f;

    
    public LayerMask carLayer;
    public LayerMask obstacleLayer;
    public float sensorLength = 8f;
    public float stopDistance = 2f;
    public float sensorHeight = 0.6f;
    public float sensorRadius = 0.6f;
    public float sensorForwardOffset = 0.5f;

    private TaxiRoadNode targetNode;
    private TaxiRoadNode previousNode;

    private float segmentT;
    private float segmentLength;

    private bool isStopped;
    private bool reachedGoal;
    private bool canEnterIntersection = true;

    private TaxiAgent agent;
    private Rigidbody rb;

    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        agent = GetComponent<TaxiAgent>();

        if (currentNode == null)
        {
            Debug.LogError("TaxiController: currentNode non assegnato!");
            enabled = false;
            return;
        }

        RequestAgentDecision();
    }

   
    void Update()
    {
        if (isStopped) return;

        if (targetNode == null)
        {
            
            RequestAgentDecision();
        }
    }


    void FixedUpdate()
    {
        if (isStopped) return;
        if (targetNode == null) return;

        bool blockedByTraffic = IsBlockedAhead();
        bool blockedByIntersection = !canEnterIntersection;

        if (!blockedByTraffic && !blockedByIntersection)
        {
            segmentT += (speed / segmentLength) * Time.fixedDeltaTime;
            segmentT = Mathf.Clamp01(segmentT);
        }

        Vector3 newPos = GetSegmentPosition(segmentT);
        rb.MovePosition(newPos);

        Vector3 dir = (targetNode.transform.position - currentNode.transform.position).normalized;
        if (dir.sqrMagnitude > 0.0001f)
            rb.MoveRotation(Quaternion.LookRotation(dir));

        if (segmentT >= 1f)
        {
            previousNode = currentNode;
            currentNode = targetNode;
            targetNode = null;

            rb.MovePosition(
                currentNode.transform.position +
                Vector3.Cross(Vector3.up, dir) * laneOffset
            );

            if (goalNode != null && currentNode == goalNode && !reachedGoal)
            {
                reachedGoal = true;
                agent?.OnReachedGoal();
                StopForSeconds(2f);
                return;
            }

            agent?.OnNodeReached();
            RequestAgentDecision();
        }
    }

    
    void RequestAgentDecision()
    {
        agent?.RequestDecision();
    }

public void SetTargetNode(TaxiRoadNode node)
{
    if (node == null || node == previousNode) 
        return;

    targetNode = node;
    segmentT = 0f;
    segmentLength = Mathf.Max(
        0.01f,
        Vector3.Distance(currentNode.transform.position, node.transform.position)
    );
}


    
    Vector3 GetSegmentPosition(float t)
    {
        Vector3 start = currentNode.transform.position;
        Vector3 end = targetNode.transform.position;
        Vector3 center = Vector3.Lerp(start, end, t);
        Vector3 right = Vector3.Cross(Vector3.up, (end - start).normalized);
        return center + right * laneOffset;
    }

    bool IsBlockedAhead()
    {
        Vector3 origin = rb.position
                       + Vector3.up * sensorHeight
                       + transform.forward * sensorForwardOffset;

        LayerMask mask = carLayer | obstacleLayer;

        if (Physics.SphereCast(origin, sensorRadius, transform.forward,
            out RaycastHit hit, sensorLength, mask, QueryTriggerInteraction.Ignore))
        {
            return hit.distance <= stopDistance;
        }

        return false;
    }

    
    public void SetGoalNode(TaxiRoadNode node)
    {
        goalNode = node;
        reachedGoal = false;
    }

    public bool HasReachedGoal() => reachedGoal;
    public void ClearReachedGoal() => reachedGoal = false;

    public void SetIntersectionPermission(bool canEnter)
    {
        canEnterIntersection = canEnter;
    }

    public TaxiRoadNode PreviousNode => previousNode;

    
public void StopForSeconds(float seconds)
{
    if (!gameObject.activeInHierarchy) return;
    StartCoroutine(StopCoroutine(seconds));
}

System.Collections.IEnumerator StopCoroutine(float seconds)
{
    isStopped = true;

    
    Vector3 savedVelocity = rb.velocity;
    Vector3 savedAngular = rb.angularVelocity;
    bool savedKinematic = rb.isKinematic;

    
    rb.velocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;
    rb.isKinematic = true;

    
    yield return new WaitForSecondsRealtime(seconds);

    
    rb.isKinematic = savedKinematic;
    rb.velocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;

    isStopped = false;
}

}
