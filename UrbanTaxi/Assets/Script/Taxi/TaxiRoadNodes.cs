using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TaxiRoadNode : MonoBehaviour
{
    public List<TaxiRoadNode> neighbors = new List<TaxiRoadNode>();

    public float CostTo(TaxiRoadNode other)
    {
        return Vector3.Distance(transform.position, other.transform.position);
    }
}
