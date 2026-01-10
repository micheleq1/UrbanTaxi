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
    public int maxDecisionSteps = 80;     // prova 80-120, in base alla mappa
    public float timeoutPenalty = -0.5f;  // penalità quando supera max step
    private int decisionSteps = 0;

    [Header("Episode Stats")]
    public float episodeReward = 0f; // reward totale dell'episodio corrente
    public int episodeCount = 0;     // numero episodi avviati
    public bool debugBranchTraffic = false;
    public int debugEveryNDecisions = 5;
    private int debugDecisionCounter = 0;

    [Header("Gizmos - Branch Sensors")]
    public bool drawBranchGizmos = true;
    public int branchGizmoSteps = 12;     // quante sfere per il “tubo”
    public bool branchGizmosOnlyWhenPlaying = true; // evita chiamate fisiche in edit mode


    public override void Initialize()
    {
        taxi = GetComponent<TaxiController>();
    }

    public override void OnEpisodeBegin()
    {
        // aggiorna contatori episodio
        episodeCount++;
        episodeReward = 0f;
        decisionSteps = 0;

        Debug.Log($"Inizio episodio #{episodeCount}");

        if (taxi == null) taxi = GetComponent<TaxiController>();
        if (taxi == null || taxi.currentNode == null || taxi.goalNode == null) return;

        prevDistToGoal = Vector3.Distance(
            taxi.currentNode.transform.position,
            taxi.goalNode.transform.position
        );
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (taxi.currentNode == null || taxi.goalNode == null)
            return;

        Vector3 toGoal = taxi.goalNode.transform.position - taxi.transform.position;

        // Direzione locale verso goal (x,z) già in [-1,1]
        Vector3 toGoalLocalDir = taxi.transform.InverseTransformDirection(toGoal.normalized);
        sensor.AddObservation(toGoalLocalDir.x);
        sensor.AddObservation(toGoalLocalDir.z);

        // Distanza normalizzata [0,1]
        float distToGoal = toGoal.magnitude;
        sensor.AddObservation(Mathf.Clamp01(distToGoal / MaxMapDist));

        List<TaxiRoadNode> neighbors = GetSortedNeighbors(taxi.currentNode);

        float currToGoal = Vector3.Distance(
            taxi.currentNode.transform.position,
            taxi.goalNode.transform.position
        );

        for (int i = 0; i < 4; i++)
        {
            if (i < neighbors.Count)
            {
                TaxiRoadNode n = neighbors[i];

                // direzione locale verso vicino
                Vector3 toN = n.transform.position - taxi.transform.position;
                Vector3 toNLocalDir = taxi.transform.InverseTransformDirection(toN.normalized);

                sensor.AddObservation(toNLocalDir.x);
                sensor.AddObservation(toNLocalDir.z);

                // improvement verso goal
                float nToGoal = Vector3.Distance(n.transform.position, taxi.goalNode.transform.position);
                float improvement = (currToGoal - nToGoal) / MaxMapDist;
                sensor.AddObservation(Mathf.Clamp(improvement, -1f, 1f));

                float blocked = BranchBlockedByStoppedCarOrObstacle(taxi.currentNode, n);
                sensor.AddObservation(blocked); // 0 libero, 1 bloccato
                if (blocked == 1)
                    Debug.Log("osservazione strada " + i + ": " + blocked);
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        decisionSteps++;

        // timeout a step
        if (decisionSteps > maxDecisionSteps)
        {
            Debug.Log("Termine episodio: troppi step");
            AddEpisodeReward(timeoutPenalty);
            EndEpisode();
            return;
        }

        if (taxi == null || taxi.currentNode == null) return;

        int action = actions.DiscreteActions[0]; // 0..3

        List<TaxiRoadNode> neighbors = GetSortedNeighbors(taxi.currentNode);
        if (neighbors == null || neighbors.Count == 0) return;

        // azione non valida
        if (action < 0 || action >= neighbors.Count)
        {
            action = 0;
        }

        TaxiRoadNode chosen = neighbors[action];

        // evita oscillazione: tornare subito al nodo precedente
        if (chosen == taxi.PreviousNode)
        {
            AddEpisodeReward(-0.05f);

            for (int i = 0; i < neighbors.Count; i++)
            {
                if (neighbors[i] != taxi.PreviousNode)
                {
                    chosen = neighbors[i];
                    break;
                }
            }
        }

        float blocked = BranchBlockedByStoppedCarOrObstacle(taxi.currentNode, chosen);
        AddEpisodeReward(-0.2f * blocked); // penalità solo se blocked=1

        // poi esegui la scelta
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

            return angA.CompareTo(angB); // sinistra -> destra
        });

        return list;
    }

    // usa sempre questo metodo al posto di AddReward()
    private void AddEpisodeReward(float value)
    {
        AddReward(value);
        episodeReward += value;
    }

    public void OnReachedGoal()
    {
        AddEpisodeReward(10f);
        EndEpisode();
        Debug.Log("Goal raggiunto! Reward +10");
    }

    public void OnNodeReached()
    {
        if (taxi == null || taxi.currentNode == null || taxi.goalNode == null) return;

        float newDist = Vector3.Distance(
            taxi.currentNode.transform.position,
            taxi.goalNode.transform.position
        );

        float delta = prevDistToGoal - newDist; // >0 se ti avvicin

        float shaped = Mathf.Clamp(delta / MaxMapDist, -1f, 1f);
        AddEpisodeReward(0.35f * shaped);

        prevDistToGoal = newDist;
    }

    private const float STOPPED_SPEED = 0.2f; // sotto questa = ferma

    private float BranchBlockedByStoppedCarOrObstacle(TaxiRoadNode from, TaxiRoadNode to)
    {
        Vector3 a = from.transform.position;
        Vector3 b = to.transform.position;

        Vector3 dir = (b - a).normalized;
        float len = Vector3.Distance(a, b);

        // Origine: sul nodo corrente, alzata e leggermente in avanti lungo il ramo
        Vector3 origin = a + Vector3.up * taxi.sensorHeight + dir * taxi.sensorForwardOffset;

        LayerMask mask = taxi.carLayer | taxi.obstacleLayer;

        float castLen = Mathf.Min(taxi.sensorLength, len);
        if (castLen <= 0.01f) return 0f;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            taxi.sensorRadius,
            dir,
            castLen,
            mask,
            QueryTriggerInteraction.Ignore
        );


        if (hits == null || hits.Length == 0)
            return 0f;

        // Ordina per distanza: controlli prima ciò che sta più vicino
        System.Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));

        foreach (var hit in hits)
        {
            // Ostacolo statico (Obstacle) -> consideralo bloccato
            if (hit.rigidbody == null)
            {
                Debug.Log("ostacolo individuato");
                return 1f;

            }

            // Se è un'auto (Car), controlla se è ferma
            float fwdSpeed = Vector3.Dot(hit.rigidbody.velocity, dir);

            if (fwdSpeed < STOPPED_SPEED)
                return 1f; // auto ferma sul ramo -> ramo bloccato
        }

        return 0f; // nessuna auto ferma trovata
    }
    private void OnDrawGizmosSelected()
    {
        if (!drawBranchGizmos) return;
        if (taxi == null) taxi = GetComponent<TaxiController>();
        if (taxi == null || taxi.currentNode == null) return;

        if (branchGizmosOnlyWhenPlaying && !Application.isPlaying) return;

        List<TaxiRoadNode> neighbors = GetSortedNeighbors(taxi.currentNode);
        if (neighbors == null || neighbors.Count == 0) return;

        // Palette colori per i 4 rami (se non bloccati)
        Color[] baseColors = new Color[]
        {
        Color.cyan,
        Color.green,
        Color.magenta,
        Color.white
        };

        int steps = Mathf.Max(2, branchGizmoSteps);

        for (int i = 0; i < 4; i++)
        {
            if (i >= neighbors.Count) break;

            TaxiRoadNode to = neighbors[i];

            // calcolo geometria identica al tuo BranchBlocked...
            Vector3 a = taxi.currentNode.transform.position;
            Vector3 b = to.transform.position;

            Vector3 dir = (b - a).normalized;
            float len = Vector3.Distance(a, b);

            Vector3 origin = a + Vector3.up * taxi.sensorHeight + dir * taxi.sensorForwardOffset;

            float castLen = Mathf.Min(taxi.sensorLength, len);
            if (castLen <= 0.01f) continue;

            Vector3 end = origin + dir * castLen;

            // stesso check “blocked”
            float blocked = BranchBlockedByStoppedCarOrObstacle(taxi.currentNode, to);

            Color c = (blocked >= 0.5f) ? Color.red : baseColors[i];

            // Asse
            Gizmos.color = c;
            Gizmos.DrawLine(origin, end);

            // Sfere inizio/fine (raggio reale)
            Gizmos.DrawWireSphere(origin, taxi.sensorRadius);
            Gizmos.DrawWireSphere(end, taxi.sensorRadius);

            // “Tubo” (serie di sfere lungo il ramo)
            Gizmos.color = new Color(c.r, c.g, c.b, 0.25f);
            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                Vector3 p = Vector3.Lerp(origin, end, t);
                Gizmos.DrawWireSphere(p, taxi.sensorRadius);
            }
        }
    }
}

