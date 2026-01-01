using UnityEngine;

public class TaxiController : MonoBehaviour, IIntersectionVehicle
{
    // ==========================
    // NAVIGAZIONE
    // ==========================
    [Header("Navigation")]
    public TaxiRoadNode currentNode;
    public TaxiRoadNode goalNode;

    public float speed = 5f;

    // ==========================
    // CORSIA
    // ==========================
    [Header("Lane Settings")]
    public float laneOffset = 1.5f;

    // ==========================
    // SENSORI TRAFFICO
    // ==========================
    [Header("Traffic Awareness")]
    public LayerMask carLayer;
    public LayerMask obstacleLayer;
    public float sensorLength = 8f;
    public float stopDistance = 2f;
    public float sensorHeight = 0.6f;
    public float sensorRadius = 0.6f;
    public float sensorForwardOffset = 0.5f;

    // ==========================
    // INCROCI
    // ==========================
    private bool canEnterIntersection = true;

    // ==========================
    // GRAFO
    // ==========================
    private TaxiRoadNode targetNode;
    private TaxiRoadNode previousNode;

    // ==========================
    // MOVIMENTO PARAMETRICO
    // ==========================
    private float segmentT = 0f;
    private float segmentLength = 1f;

    // ==========================
    // UNITY
    // ==========================
    void Start()
    {
        if (currentNode == null)
        {
            Debug.LogError("TaxiController: currentNode non assegnato!");
            enabled = false;
            return;
        }

        PickNextNode();
        InitSegment();
    }

    void Update()
    {
        if (targetNode == null) return;

        bool blockedByTraffic = IsBlockedAhead();
        bool blockedByIntersection = !canEnterIntersection;

        // Avanza SOLO se non bloccato
        if (!blockedByTraffic && !blockedByIntersection)
        {
            segmentT += (speed / segmentLength) * Time.deltaTime;
            segmentT = Mathf.Clamp01(segmentT);
        }

        // ==========================
        // POSIZIONE (SEGMENTO DRITTO)
        // ==========================
        Vector3 pos = GetSegmentPosition(segmentT);
        transform.position = pos;

        // ==========================
        // ROTAZIONE SECCA (NESSUN SMOOTHING)
        // ==========================
        Vector3 dir = (targetNode.transform.position - currentNode.transform.position).normalized;
        if (dir.sqrMagnitude > 0.0001f)
            transform.forward = dir;

        // Fine segmento
        if (segmentT >= 1f)
        {
            previousNode = currentNode;
            currentNode = targetNode;
            PickNextNode();
            InitSegment();
        }
    }

    // ==========================
    // GRAFO
    // ==========================
    void PickNextNode()
    {
        if (currentNode.neighbors == null || currentNode.neighbors.Count == 0)
        {
            targetNode = null;
            return;
        }

        // ==========================
        // CASO 1: NESSUN OBIETTIVO → comportamento originale
        // ==========================
        if (goalNode == null)
        {
            // Evita inversioni immediate
            foreach (var n in currentNode.neighbors)
            {
                if (n != previousNode)
                {
                    targetNode = n;
                    return;
                }
            }

            // Fallback
            targetNode = currentNode.neighbors[0];
            return;
        }

        // ==========================
        // CASO 2: HO UN OBIETTIVO → avvicinati al goal
        // ==========================
        TaxiRoadNode bestNode = null;
        float bestDistance = float.MaxValue;

        foreach (var n in currentNode.neighbors)
        {
            if (n == previousNode)
                continue;

            float dist = Vector3.Distance(
                n.transform.position,
                goalNode.transform.position
            );

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestNode = n;
            }
        }

        // Se ho trovato un nodo migliore, usalo
        if (bestNode != null)
        {
            targetNode = bestNode;
            return;
        }

        // Fallback di sicurezza
        targetNode = currentNode.neighbors[0];
    }


    void InitSegment()
    {
        segmentT = 0f;
        segmentLength = Mathf.Max(
            0.01f,
            Vector3.Distance(
                currentNode.transform.position,
                targetNode.transform.position
            )
        );
    }

    // ==========================
    // POSIZIONE
    // ==========================
    Vector3 GetSegmentPosition(float t)
    {
        Vector3 start = currentNode.transform.position;
        Vector3 end = targetNode.transform.position;

        Vector3 center = Vector3.Lerp(start, end, t);
        Vector3 dir = (end - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, dir);

        return center + right * laneOffset;
    }

    // ==========================
    // SENSORI TRAFFICO
    // ==========================
    bool IsBlockedAhead()
    {
        Vector3 forwardDir = transform.forward;
        Vector3 origin = transform.position
                       + Vector3.up * sensorHeight
                       + forwardDir * sensorForwardOffset;

        LayerMask mask = carLayer | obstacleLayer;

        if (Physics.SphereCast(
            origin,
            sensorRadius,
            forwardDir,
            out RaycastHit hit,
            sensorLength,
            mask,
            QueryTriggerInteraction.Ignore))
        {
            if (hit.rigidbody == null || hit.rigidbody.gameObject != gameObject)
            {
                if (hit.distance <= stopDistance)
                    return true;
            }
        }

        return false;
    }

    // ==========================
    // INCROCI (CHIAMATO DAGLI SCRIPT INCROCIO)
    // ==========================
    public void SetIntersectionPermission(bool canEnter)
    {
        canEnterIntersection = canEnter;
    }
}
