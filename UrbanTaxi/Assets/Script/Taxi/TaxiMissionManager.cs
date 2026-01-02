using UnityEngine;
using WrightAngle.Waypoint;
using System.Collections;

public class TaxiMissionManager : MonoBehaviour
{
    [Header("Taxi")]
    public TaxiController taxi;

    [Header("Prefabs")]
    public GameObject passengerPrefab;
    public GameObject destinationPrefab;

    [Header("Spawn Points")]
    public Transform[] passengerSpawnPoints;
    public Transform[] destinationPoints;

    [Header("Distances")]
    public float pickupDistance = 2.5f;
    public float dropoffDistance = 3f;

    [Header("Stop Settings")]
    public float stopTimeAtDestination = 5f;

    private GameObject passengerObj;
    private GameObject destinationObj;

    private WaypointTarget passengerWaypoint;
    private WaypointTarget destinationWaypoint;

    //Stati della missione
    private enum State
    {
        WaitingPickup,
        GoingToPickup,
        WaitingDropoff,
        GoingToDropoff
    }

    private State state;

    void Awake()
    {
        passengerObj = Instantiate(passengerPrefab);
        passengerObj.SetActive(false);
        passengerWaypoint = passengerObj.GetComponentInChildren<WaypointTarget>(true);

        destinationObj = Instantiate(destinationPrefab);
        destinationObj.SetActive(false);
        destinationWaypoint = destinationObj.GetComponentInChildren<WaypointTarget>(true);
    }

    void Start()
    {
        SpawnPassenger();
    }

    void Update()
    {
        if (taxi.HasReachedGoal())
        {
            if (state == State.GoingToPickup)
            {
                StartCoroutine(HandlePickup());
            }
            else if (state == State.GoingToDropoff)
            {
                StartCoroutine(HandleDropoff());
            }
        }
    }


    IEnumerator HandlePickup()
    {
        state = State.WaitingPickup;

        passengerObj.SetActive(false); // pickup

        yield return new WaitForSeconds(0.1f); // sicurezza frame

        SpawnDestination(); 
    }

    void SpawnDestination()
    {
        Transform dp = destinationPoints[Random.Range(0, destinationPoints.Length)];
        destinationObj.transform.position = dp.position;
        
        destinationObj.SetActive(true);
        destinationWaypoint.ActivateWaypoint();

        TaxiRoadNode dropNode = FindClosestNode(destinationObj.transform.position);
        taxi.SetGoalNode(dropNode);

        state = State.GoingToDropoff;
    }

    IEnumerator HandleDropoff()
    {
        state = State.WaitingDropoff;

        destinationWaypoint.DeactivateWaypoint();
        destinationObj.SetActive(false);
        
        yield return new WaitForSeconds(0.1f);
        SpawnPassenger(); // nuova corsa
    }

    TaxiRoadNode FindClosestNode(Vector3 pos)
    {
        TaxiRoadNode[] nodes = FindObjectsOfType<TaxiRoadNode>();

        TaxiRoadNode best = null;
        float bestDist = float.MaxValue;

        foreach (var n in nodes)
        {
            float d = Vector3.Distance(pos, n.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = n;
            }
        }

        return best;
    }

    void SpawnPassenger()
    {
        Transform sp = passengerSpawnPoints[Random.Range(0, passengerSpawnPoints.Length)];
        passengerObj.transform.position = sp.position;
        passengerObj.SetActive(true);

        TaxiRoadNode pickupNode = FindClosestNode(passengerObj.transform.position);
        taxi.SetGoalNode(pickupNode);

        state = State.GoingToPickup;
    }
}
