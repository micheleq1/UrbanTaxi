using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

public class TaxiAgent : Agent
{
    private TaxiController taxi;

    [Header("Progress Reward")]
    private float prevDistToGoal = 0f;
    private const float MaxMapDist = 320f;

    [Header("Episode by Steps")]
    public int maxDecisionSteps = 80;
    public float timeoutPenalty = -0.5f;
    private int decisionSteps = 0;

    [Header("Episode Stats")]
    public float episodeReward = 0f;
    public int episodeCount = 0;

    [Header("Gizmos")]
    public bool drawBranchGizmos = true;
    public bool branchGizmosOnlyWhenPlaying = true;
    private Rigidbody selfRb;
    private readonly bool[] lastBlockedObs = new bool[4];
    private readonly TaxiRoadNode[] lastNeighbors = new TaxiRoadNode[4];
    private int lastObsFrame = -999999;


    public override void Initialize()
    {
        taxi = GetComponent<TaxiController>();
        selfRb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
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
        sensor.AddObservation(Mathf.Clamp01(distToGoal / MaxMapDist));

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
                if (lastBlockedObs[i] == true)
                    Debug.Log("strada chiusa +" + i);

                sensor.AddObservation(blocked ? 1f : 0f);
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

        // ✅ segna il frame ESATTO in cui hai calcolato i branch
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
    {
        AddEpisodeReward(-0.05f);
    }

    // ✅ USA SOLO L’OSSERVAZIONE MEMORIZZATA
    bool blocked = lastBlockedObs[action];

        if (blocked)
        {
            Debug.Log("penalità per strada chiusa");
            AddEpisodeReward(-0.3f);
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

    // =====================================================
    // SENSORI
    // =====================================================

    private const float STOPPED_SPEED = 0.2f;

    // Ramo normale (limitato da sensorLength)
    private bool IsBranchBlocked(TaxiRoadNode from, TaxiRoadNode to)
    {
        if (taxi == null) taxi = GetComponent<TaxiController>();
        if (taxi == null || from == null || to == null) return false;

        // Punto reale del taxi (in corsia)
        Vector3 taxiPos = transform.position;

        // Direzione centrale del segmento (serve per calcolare la "destra" della strada)
        Vector3 segDir = (to.transform.position - from.transform.position).normalized;
        if (segDir.sqrMagnitude < 0.0001f) return false;

        // Vettore right della strada
        Vector3 roadRight = Vector3.Cross(Vector3.up, segDir).normalized;

        // Determina su quale lato corsia sta il taxi (così l'offset va dalla parte giusta)
        float sideSign = Mathf.Sign(Vector3.Dot(taxiPos - from.transform.position, roadRight));
        if (sideSign == 0f) sideSign = 1f;

        // Punto "to" spostato in corsia (non al centro strada)
        Vector3 toLanePos = to.transform.position + roadRight * taxi.laneOffset * sideSign;

        // ✅ Origine come TaxiController: dal taxi + altezza + un filo avanti nella direzione della corsia
        Vector3 dir = (toLanePos - taxiPos).normalized;

        Vector3 origin = taxiPos
                       + Vector3.up * taxi.sensorHeight
                       + dir * taxi.sensorForwardOffset;

        LayerMask mask = taxi.carLayer | taxi.obstacleLayer;

        // ✅ Lunghezza fissa: non dipende dal nodo
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
            // ignora il taxi stesso
            if (hit.collider != null && hit.collider.transform.IsChildOf(transform))
                continue;

            // ostacolo statico
            if (hit.rigidbody == null) return true;

            // auto ferma (consiglio: magnitude, evita falsi positivi)
            float speed = hit.rigidbody.velocity.magnitude;
            if (speed < STOPPED_SPEED) return true;
        }

        return false;
    }


    // Strada completa StreetStart → StreetEnd
    private bool IsFullStreetBlocked(TaxiRoadNode from, TaxiRoadNode to)
    {
        Vector3 a = from.transform.position;
        Vector3 b = to.transform.position;

        Vector3 dir = (b - a).normalized;
        float len = Vector3.Distance(a, b);

        Vector3 origin = a + Vector3.up * taxi.sensorHeight + dir * taxi.sensorForwardOffset;

        LayerMask mask = taxi.carLayer | taxi.obstacleLayer;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            taxi.sensorRadius,
            dir,
            len,
            mask,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0) return false;

        System.Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));

        foreach (var hit in hits)
        {
            // ✅ ignora il taxi stesso
            if (hit.collider != null && hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.rigidbody == null) return true;

            float fwdSpeed = Vector3.Dot(hit.rigidbody.velocity, dir);
            if (fwdSpeed < STOPPED_SPEED) return true;
        }

        return false;
    }

    // Trova e controlla la strada dopo un'uscita di incrocio
    private bool IsStreetAfterExitBlocked(TaxiRoadNode intersectionOut)
    {
        TaxiRoadNode streetStart = null;
        TaxiRoadNode streetEnd = null;

        foreach (var n in intersectionOut.neighbors)
            if (n.nodeType == RoadNodeType.StreetStart)
                streetStart = n;

        if (streetStart == null) return false;

        foreach (var n in streetStart.neighbors)
            if (n.nodeType == RoadNodeType.StreetEnd)
                streetEnd = n;

        if (streetEnd == null) return false;

        return IsFullStreetBlocked(streetStart, streetEnd);
    }

    // Funzione unica usata dall’agente
    private bool IsChoiceBlocked(TaxiRoadNode current, TaxiRoadNode chosenNeighbor)
    {
        if (current.nodeType == RoadNodeType.IntersectionCenter)
            return IsStreetAfterExitBlocked(chosenNeighbor);

        return IsBranchBlocked(current, chosenNeighbor);
    }

    // =====================================================
    // GIZMOS = IDENTICI AI SENSORI
    // =====================================================

    private void OnDrawGizmos()
    {
        if (!drawBranchGizmos) return;

        if (taxi == null)
            taxi = GetComponent<TaxiController>();

        if (taxi == null || taxi.currentNode == null) return;
        if (branchGizmosOnlyWhenPlaying && !Application.isPlaying) return;

        // ✅ disegna SOLO nel frame in cui CollectObservations ha calcolato i neighbor
        if (Time.frameCount != lastObsFrame)
            return;

        for (int i = 0; i < 4; i++)
        {
            TaxiRoadNode to = lastNeighbors[i];
            if (to == null) continue;

            Gizmos.color = lastBlockedObs[i] ? Color.red : Color.green;

            // disegna la stessa geometria del sensore (ma SENZA rifare cast)
            DrawRaySegmentLane(taxi.currentNode, to, taxi.sensorLength);
        }
    }



    private void DrawRaySegmentLane(TaxiRoadNode from, TaxiRoadNode to, float length)
    {
        if (taxi == null || from == null || to == null) return;

        Vector3 taxiPos = transform.position;

        // Direzione del segmento centrale from->to (serve per calcolare la destra della strada)
        Vector3 segDir = (to.transform.position - from.transform.position).normalized;
        if (segDir.sqrMagnitude < 0.0001f) return;

        Vector3 roadRight = Vector3.Cross(Vector3.up, segDir).normalized;

        // Capisce da che lato è il taxi rispetto alla strada (così offset coerente)
        float sideSign = Mathf.Sign(Vector3.Dot(taxiPos - from.transform.position, roadRight));
        if (sideSign == 0f) sideSign = 1f;

        // Punto di arrivo "in corsia" (non al centro strada)
        Vector3 toLanePos = to.transform.position + roadRight * taxi.laneOffset * sideSign;

        // Direzione del raggio: taxi -> punto corsia
        Vector3 dir = (toLanePos - taxiPos).normalized;
        if (dir.sqrMagnitude < 0.0001f) return;

        // Origine come nel sensore: taxi + altezza + piccolo offset in avanti
        Vector3 origin = taxiPos + Vector3.up * taxi.sensorHeight + dir * taxi.sensorForwardOffset;

        // Lunghezza fissa (non si ferma al nodo)
        float len = length;
        Vector3 end = origin + dir * len;

        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(origin, taxi.sensorRadius);
        Gizmos.DrawWireSphere(end, taxi.sensorRadius);
    }

}
