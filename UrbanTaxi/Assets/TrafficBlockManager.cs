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
    public float activeDurationSeconds = 30f;

    [Header("Options")]
    public bool avoidSamePointTwice = true;

    [Header("Rotation")]
    public float randomYawDegrees = 30f;

    private GameObject blockageObj;
    private int lastPointIndex = -1;

    void Awake()
    {
        if (blockagePrefab != null)
        {
            blockageObj = Instantiate(blockagePrefab);
            blockageObj.SetActive(false);
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
            SpawnAtOneRandomPoint();
            yield return new WaitForSeconds(activeDurationSeconds);

            Despawn();
            yield return new WaitForSeconds(spawnIntervalSeconds);
        }
    }

    void SpawnAtOneRandomPoint()
    {
        if (blockageObj == null || blockagePoints == null || blockagePoints.Length == 0)
        {
            Debug.LogError("[TrafficBlockageSinglePoint] blockagePrefab o blockagePoints non assegnati.");
            return;
        }

        int idx = PickRandomIndex(blockagePoints.Length, lastPointIndex, avoidSamePointTwice);
        lastPointIndex = idx;

        Transform p = blockagePoints[idx];

        float yaw = Random.Range(-randomYawDegrees, randomYawDegrees);
        Quaternion rot = p.rotation * Quaternion.Euler(0f, yaw, 0f);

        blockageObj.transform.SetPositionAndRotation(p.position, rot);
        blockageObj.SetActive(true);
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
}
