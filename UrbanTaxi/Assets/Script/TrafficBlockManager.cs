using System.Collections;
using UnityEngine;

public class TrafficBlockageSinglePoint : MonoBehaviour
{
    [Header("Blockage Prefab (container with children)")]
    public GameObject blockagePrefab;     // <-- Ostacoli

    [Header("Possible Points (BP_01..BP_N)")]
    public Transform[] blockagePoints;

    [Header("Timing")]
    public float spawnIntervalSeconds = 60f;
    public float activeDurationSeconds = 15f;

    [Header("Options")]
    public bool avoidSamePointTwice = true;

    public enum RotationMode
    {
        ExactPointRotation,
        RandomYawContinuous,
        RandomYaw90Steps
    }

    [Header("Rotation")]
    public RotationMode rotationMode = RotationMode.ExactPointRotation;
    public float randomYawDegrees = 30f;

    [Header("Spawn safety (avoid spawning on cars)")]
    public LayerMask carLayer;                 // <-- layer delle auto (NPC + taxi)
    public int maxAttempts = 10;               // quanti punti provare
    public Vector3 extraPadding = new Vector3(0.5f, 0.5f, 0.5f); // margine sicurezza
    public bool ignoreTriggers = true;

    private GameObject blockageObj;
    private int lastPointIndex = -1;

    // dati �ingombro� dal collider del prefab
    private Vector3 checkCenterLocal = Vector3.zero;
    private Vector3 checkHalfExtentsLocal = new Vector3(2f, 1f, 2f);

    void Awake()
    {
        if (blockagePrefab != null)
        {
            blockageObj = Instantiate(blockagePrefab);
            blockageObj.SetActive(false);

            // Prendo un BoxCollider dal prefab (consigliato sul ROOT del prefab)
            BoxCollider bc = blockageObj.GetComponent<BoxCollider>();
            if (bc == null) bc = blockageObj.GetComponentInChildren<BoxCollider>();

            if (bc != null)
            {
                checkCenterLocal = bc.center;
                checkHalfExtentsLocal = (bc.size * 0.5f) + extraPadding;
            }
            else
            {
                Debug.LogWarning("[TrafficBlockageSinglePoint] Nessun BoxCollider trovato nel prefab: uso dimensioni default.");
            }
        }
    }

    void Start()
    {
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (true)
        {
            TrySpawnAtFreePoint();
            yield return new WaitForSeconds(activeDurationSeconds);

            Despawn();
            yield return new WaitForSeconds(spawnIntervalSeconds);
        }
    }

    void TrySpawnAtFreePoint()
    {
        if (blockageObj == null || blockagePoints == null || blockagePoints.Length == 0)
        {
            Debug.LogError("[TrafficBlockageSinglePoint] blockagePrefab o blockagePoints non assegnati.");
            return;
        }

        int attempts = Mathf.Min(maxAttempts, blockagePoints.Length);

        for (int a = 0; a < attempts; a++)
        {
            int idx = PickRandomIndex(blockagePoints.Length, lastPointIndex, avoidSamePointTwice);
            Transform p = blockagePoints[idx];
            Quaternion rot = ComputeRotation(p);

            if (IsSpawnAreaFree(p.position, rot))
            {
                lastPointIndex = idx;
                blockageObj.transform.SetPositionAndRotation(p.position, rot);
                blockageObj.SetActive(true);
                return;
            }
        }

        Debug.Log("[TrafficBlockageSinglePoint] Nessun punto libero (macchine presenti). Spawn saltato.");
    }

    bool IsSpawnAreaFree(Vector3 pos, Quaternion rot)
    {
        // Centro del box in world space (root pos + rot * center locale)
        Vector3 centerWorld = pos + rot * checkCenterLocal;

        QueryTriggerInteraction qti = ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide;

        // Se trova collider di macchine -> non libero
        Collider[] hits = Physics.OverlapBox(centerWorld, checkHalfExtentsLocal, rot, carLayer, qti);
        return hits == null || hits.Length == 0;
    }

    Quaternion ComputeRotation(Transform point)
    {
        switch (rotationMode)
        {
            case RotationMode.ExactPointRotation:
                return point.rotation;

            case RotationMode.RandomYawContinuous:
                float yaw = Random.Range(-randomYawDegrees, randomYawDegrees);
                return point.rotation * Quaternion.Euler(0f, yaw, 0f);

            case RotationMode.RandomYaw90Steps:
                int step = Random.Range(0, 4);
                return point.rotation * Quaternion.Euler(0f, step * 90f, 0f);

            default:
                return point.rotation;
        }
    }

    void Despawn()
    {
        if (blockageObj != null) blockageObj.SetActive(false);
    }

    int PickRandomIndex(int length, int lastIndex, bool avoidSameTwice)
    {
        if (length <= 1 || !avoidSameTwice) return Random.Range(0, length);
        int r = Random.Range(0, length);
        while (r == lastIndex) r = Random.Range(0, length);
        return r;
    }

    void OnDrawGizmosSelected()
    {
        if (blockagePoints == null) return;

        Gizmos.color = Color.cyan;
        foreach (var p in blockagePoints)
        {
            if (!p) continue;
            Quaternion rot = p.rotation;
            Vector3 center = p.position + rot * Vector3.up; // solo per visualizzare approx
            Gizmos.matrix = Matrix4x4.TRS(center, rot, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(4f, 2f, 4f));
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
}
