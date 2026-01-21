using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

public class TaxiAgent : Agent
{
    private TaxiController taxi;

    
    private float prevDistToGoal = 0f;
    private const float MaxMapDist = 320f;

    
    public int maxDecisionSteps = 150;
    public float timeoutPenalty = -0.8f;
    private int decisionSteps = 0;

    
    public float episodeReward = 0f;
    public int episodeCount = 0;
    public float previousEpisodeReward = 0f;

    
    public bool drawBranchGizmos = true;
    public bool branchGizmosOnlyWhenPlaying = true;
    private Rigidbody selfRb;
    private readonly bool[] lastBlockedObs = new bool[4];
    private readonly TaxiRoadNode[] lastNeighbors = new TaxiRoadNode[4];
    private int lastObsFrame = -999999;

    
 
    
    public float nearMeters = 80f;

    
    public float farMeters = 200f;

    
    private float lastDistNorm = 0f;

    
    public float Pnear = 0.10f;

    
    public float Pfar = 0.80f;


    
    public bool debugBlockedRisk = true;

    
    public float debugThrottleSeconds = 0.35f;

    private float nextDebugTime = 0f;

    private void DebugThrottled(string msg)
    {
        if (!debugBlockedRisk) return;
        if (Time.time < nextDebugTime) return;
        nextDebugTime = Time.time + debugThrottleSeconds;
        

    }

    
    private float DistanceWeight01()
    {
        float near = Mathf.Clamp01(nearMeters / MaxMapDist);
        float far = Mathf.Clamp01(farMeters / MaxMapDist);

        float w = Mathf.InverseLerp(near, far, lastDistNorm); 

        
        w = w * w * (3f - 2f * w);

        return w;
    }

    public override void Initialize()
    {
        taxi = GetComponent<TaxiController>();
        selfRb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        
        if (episodeCount > 0)
            previousEpisodeReward = episodeReward;

        episodeCount++;
        episodeReward = 0f;
        decisionSteps = 0;

        if (taxi == null) taxi = GetComponent<TaxiController>();
        if (taxi == null || taxi.currentNode == null || taxi.goalNode == null) return;

        prevDistToGoal = Vector3.Distance(
            taxi.currentNode.transform.position,
            taxi.goalNode.transform.position
        );
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        if (taxi.currentNode == null || taxi.goalNode == null) return;

        Vector3 toGoal = taxi.goalNode.transform.position - taxi.transform.position;
        Vector3 toGoalLocal = taxi.transform.InverseTransformDirection(toGoal.normalized);

        sensor.AddObservation(toGoalLocal.x);
        sensor.AddObservation(toGoalLocal.z);

        float distToGoal = toGoal.magnitude;

        
        lastDistNorm = Mathf.Clamp01(distToGoal / MaxMapDist);
        sensor.AddObservation(lastDistNorm);

        List<TaxiRoadNode> neighbors = GetSortedNeighbors(taxi.currentNode);

        float currToGoal = Vector3.Distance(
            taxi.currentNode.transform.position,
            taxi.goalNode.transform.position
        );

        for (int i = 0; i < 4; i++)
        {
            if (neighbors != null && i < neighbors.Count && neighbors[i] != null)
            {
                TaxiRoadNode n = neighbors[i];
                lastNeighbors[i] = n;

                Vector3 toN = n.transform.position - taxi.transform.position;
                Vector3 toNLocal = taxi.transform.InverseTransformDirection(toN.normalized);

                sensor.AddObservation(toNLocal.x);
                sensor.AddObservation(toNLocal.z);

                float nToGoal = Vector3.Distance(n.transform.position, taxi.goalNode.transform.position);
                float improvement = (currToGoal - nToGoal) / MaxMapDist;
                sensor.AddObservation(Mathf.Clamp(improvement, -1f, 1f));

                bool blocked = IsChoiceBlocked(taxi.currentNode, n);
                lastBlockedObs[i] = blocked;

                if (taxi.currentNode.nodeType == RoadNodeType.IntersectionCenter)
                    sensor.AddObservation(blocked ? 1f : 0f);
                else
                    sensor.AddObservation(0f);
            }
            else
            {
                lastNeighbors[i] = null;
                lastBlockedObs[i] = false;

                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }

        lastObsFrame = Time.frameCount;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        decisionSteps++;

        if (decisionSteps > maxDecisionSteps)
        {
            AddEpisodeReward(timeoutPenalty);
            EndEpisode();
            return;
        }

        if (taxi == null || taxi.currentNode == null) return;

        int action = actions.DiscreteActions[0];

        List<TaxiRoadNode> neighbors = GetSortedNeighbors(taxi.currentNode);
        if (neighbors == null || neighbors.Count == 0) return;

        if (action < 0 || action >= neighbors.Count)
            action = 0;

        TaxiRoadNode chosen = neighbors[action];

        if (chosen == taxi.PreviousNode)
            AddEpisodeReward(-0.05f);

        
        bool blocked = lastBlockedObs[action];

        bool shouldPenalizeBlocked = blocked && taxi.currentNode.nodeType == RoadNodeType.IntersectionCenter;

        if (shouldPenalizeBlocked)
        {
            float w = DistanceWeight01();
            float pen = -Mathf.Lerp(Pnear, Pfar, w);
            AddEpisodeReward(pen);

            DebugThrottled($"[BLOCKED-RISK] (INTERSECTION) dist={(lastDistNorm * MaxMapDist):0}m w={w:0.00} pen={pen:0.000}");
        }


        
        taxi.SetTargetNode(chosen);
    }

    private void FixedUpdate()
    {
        if (StepCount > 0)
            AddEpisodeReward(-0.003f * Time.fixedDeltaTime);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = Random.Range(0, 4);
    }

    private List<TaxiRoadNode> GetSortedNeighbors(TaxiRoadNode node)
    {
        var list = new List<TaxiRoadNode>(node.neighbors);

        list.Sort((a, b) =>
        {
            Vector3 dirA = (a.transform.position - node.transform.position).normalized;
            Vector3 dirB = (b.transform.position - node.transform.position).normalized;

            float angA = Vector3.SignedAngle(taxi.transform.forward, dirA, Vector3.up);
            float angB = Vector3.SignedAngle(taxi.transform.forward, dirB, Vector3.up);

            return angA.CompareTo(angB);
        });

        return list;
    }

    private void AddEpisodeReward(float value)
    {
        AddReward(value);
        episodeReward += value;
    }

    public void OnReachedGoal()
    {
        AddEpisodeReward(10f);
        EndEpisode();
    }

    public void OnNodeReached()
    {
        if (taxi == null || taxi.currentNode == null || taxi.goalNode == null) return;

        float newDist = Vector3.Distance(
            taxi.currentNode.transform.position,
            taxi.goalNode.transform.position
        );

        float delta = prevDistToGoal - newDist;
        float shaped = Mathf.Clamp(delta / MaxMapDist, -1f, 1f);
        AddEpisodeReward(0.35f * shaped);

        prevDistToGoal = newDist;
    }

    

    private const float STOPPED_SPEED = 0.2f;

    private bool IsBranchBlocked(TaxiRoadNode from, TaxiRoadNode to)
    {
        if (taxi == null) taxi = GetComponent<TaxiController>();
        if (taxi == null || from == null || to == null) return false;

        Vector3 taxiPos = transform.position;

        Vector3 segDir = (to.transform.position - from.transform.position).normalized;
        if (segDir.sqrMagnitude < 0.0001f) return false;

        Vector3 roadRight = Vector3.Cross(Vector3.up, segDir).normalized;

        float sideSign = Mathf.Sign(Vector3.Dot(taxiPos - from.transform.position, roadRight));
        if (sideSign == 0f) sideSign = 1f;

        Vector3 toLanePos = to.transform.position + roadRight * taxi.laneOffset * sideSign;

        Vector3 dir = (toLanePos - taxiPos).normalized;

        Vector3 origin = taxiPos
                       + Vector3.up * taxi.sensorHeight
                       + dir * taxi.sensorForwardOffset;

        LayerMask mask = taxi.carLayer | taxi.obstacleLayer;

        float distToNode = Vector3.Distance(origin, toLanePos);
        float castLen = Mathf.Min(taxi.sensorLength, distToNode);
        if (castLen <= 0.01f) return false;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            taxi.sensorRadius,
            dir,
            castLen,
            mask,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0) return false;

        System.Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));

        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.rigidbody == null) return true;

            float speed = hit.rigidbody.velocity.magnitude;
            if (speed < STOPPED_SPEED) return true;
        }

        return false;
    }

    private bool TryGetStreetSegmentFromExit(
        TaxiRoadNode intersectionOut,
        out TaxiRoadNode streetA,
        out TaxiRoadNode streetB
    )
    {
        streetA = null;
        streetB = null;
        if (intersectionOut == null) return false;

        TaxiRoadNode end1 = null;
        foreach (var n in intersectionOut.neighbors)
        {
            if (n == null) continue;
            if (n.nodeType == RoadNodeType.StreetStart || n.nodeType == RoadNodeType.StreetEnd)
            {
                end1 = n;
                break;
            }
        }
        if (end1 == null) return false;

        TaxiRoadNode end2 = null;
        foreach (var n in end1.neighbors)
        {
            if (n == null) continue;
            if ((end1.nodeType == RoadNodeType.StreetStart && n.nodeType == RoadNodeType.StreetEnd) ||
                (end1.nodeType == RoadNodeType.StreetEnd && n.nodeType == RoadNodeType.StreetStart))
            {
                end2 = n;
                break;
            }
        }
        if (end2 == null) return false;

        float d1 = Vector3.Distance(intersectionOut.transform.position, end1.transform.position);
        float d2 = Vector3.Distance(intersectionOut.transform.position, end2.transform.position);

        if (d1 <= d2)
        {
            streetA = end1;
            streetB = end2;
        }
        else
        {
            streetA = end2;
            streetB = end1;
        }

        return true;
    }

    private bool IsFullStreetBlockedLane(TaxiRoadNode streetA, TaxiRoadNode streetB)
    {
        if (taxi == null) taxi = GetComponent<TaxiController>();
        if (taxi == null || streetA == null || streetB == null) return false;

        Vector3 a = streetA.transform.position;
        Vector3 b = streetB.transform.position;

        Vector3 segDir = (b - a);
        float segLen = segDir.magnitude;
        if (segLen < 0.01f) return false;
        segDir /= segLen;

        Vector3 roadRight = Vector3.Cross(Vector3.up, segDir).normalized;

        Vector3 aLane = a + roadRight * taxi.laneOffset;
        Vector3 bLane = b + roadRight * taxi.laneOffset;

        Vector3 dir = (bLane - aLane);
        float len = dir.magnitude;
        if (len < 0.01f) return false;
        dir /= len;

        Vector3 origin = aLane + Vector3.up * taxi.sensorHeight + dir * taxi.sensorForwardOffset;

        LayerMask mask = taxi.carLayer | taxi.obstacleLayer;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            taxi.sensorRadius,
            dir,
            len,
            mask,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0) return false;

        System.Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));

        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.rigidbody == null) return true;

            float speed = hit.rigidbody.velocity.magnitude;
            if (speed < STOPPED_SPEED) return true;
        }

        return false;
    }

    private bool IsStreetAfterExitBlockedLane(TaxiRoadNode intersectionOut)
    {
        if (!TryGetStreetSegmentFromExit(intersectionOut, out var a, out var b))
            return false;

        return IsFullStreetBlockedLane(a, b);
    }

    private bool IsChoiceBlocked(TaxiRoadNode current, TaxiRoadNode chosenNeighbor)
    {
        if (current == null || chosenNeighbor == null) return false;

        if (current.nodeType == RoadNodeType.IntersectionCenter)
            return IsStreetAfterExitBlockedLane(chosenNeighbor);

        return IsBranchBlocked(current, chosenNeighbor);
    }

    
}
