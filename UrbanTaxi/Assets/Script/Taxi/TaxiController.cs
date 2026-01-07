using UnityEngine;
using System.Collections.Generic;

public class TaxiController : MonoBehaviour, IIntersectionVehicle
{
    [Header("Navigation")]
    public TaxiRoadNode currentNode;
    public TaxiRoadNode goalNode;

    [Header("Movement")]
    public float speed = 5f;
    public float laneOffset = 1.5f;

    [Header("Traffic Awareness")]
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
            return;
        }

        bool blockedByTraffic = IsBlockedAhead();
        bool blockedByIntersection = !canEnterIntersection;

        if (!blockedByTraffic && !blockedByIntersection)
        {
            segmentT += (speed / segmentLength) * Time.deltaTime;
            segmentT = Mathf.Clamp01(segmentT);
        }

        transform.position = GetSegmentPosition(segmentT);

        Vector3 dir = (targetNode.transform.position - currentNode.transform.position).normalized;
        if (dir.sqrMagnitude > 0.0001f)
            transform.forward = dir;

        if (segmentT >= 1f)
        {
            previousNode = currentNode;
            currentNode = targetNode;
            targetNode = null;

            transform.position = currentNode.transform.position
                + Vector3.Cross(Vector3.up, dir) * laneOffset;

            if (goalNode != null && currentNode == goalNode && !reachedGoal)
            {
                reachedGoal = true;
                StopForSeconds(5f);
                return;
            }

            RequestAgentDecision();
        }
    }

    // AGENT INTERACTION
    void RequestAgentDecision()
    {
        if (agent != null)
            agent.RequestDecision();
    }

    public void SetTargetNode(TaxiRoadNode node)
    {
        if (node == null || node == previousNode)
            return;

        targetNode = node;
        segmentT = 0f;
        segmentLength = Mathf.Max(
            0.01f,
            Vector3.Distance(currentNode.transform.position, targetNode.transform.position)
        );
    }

    // MOVEMENT HELPERS
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
        Vector3 origin = transform.position
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

    // ==========================
    // PUBLIC API
    // ==========================
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

    // STOP
    public void StopForSeconds(float seconds)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(StopCoroutine(seconds));
    }

    System.Collections.IEnumerator StopCoroutine(float seconds)
    {
        isStopped = true;
        yield return new WaitForSeconds(seconds);
        isStopped = false;
    }
}
