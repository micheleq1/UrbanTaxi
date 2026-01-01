using UnityEngine;
using WrightAngle.Waypoint;

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

    private enum State { GoPickup, GoDropoff }
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
        if (state == State.GoPickup && passengerObj.activeSelf)
        {
            if (Vector3.Distance(taxi.transform.position, passengerObj.transform.position) <= pickupDistance)
            {
                passengerWaypoint.DeactivateWaypoint();
                passengerObj.SetActive(false);

                SpawnDestination();
            }
        }
        else if (state == State.GoDropoff && destinationObj.activeSelf)
        {
            if (Vector3.Distance(taxi.transform.position, destinationObj.transform.position) <= dropoffDistance)
            {
                destinationWaypoint.DeactivateWaypoint();
                destinationObj.SetActive(false);

                SpawnPassenger();
            }
        }
    }

    // =========================
    // PASSENGER
    // =========================
    void SpawnPassenger()
    {
        Transform sp = passengerSpawnPoints[Random.Range(0, passengerSpawnPoints.Length)];
        passengerObj.transform.SetPositionAndRotation(sp.position, sp.rotation);
        passengerObj.SetActive(true);

        passengerWaypoint.ActivateWaypoint();

        state = State.GoPickup;
    }

    // =========================
    // DESTINATION
    // =========================
    void SpawnDestination()
    {
        Transform dp = destinationPoints[Random.Range(0, destinationPoints.Length)];
        destinationObj.transform.SetPositionAndRotation(dp.position, dp.rotation);
        destinationObj.SetActive(true);

        destinationWaypoint.ActivateWaypoint();

        state = State.GoDropoff;
    }
}
