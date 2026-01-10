using UnityEngine;

public class TaxiRoadGraphGizmos : MonoBehaviour
{
    public TaxiRoadNode[] allNodes;
    public float nodeSize = 0.3f;

    private void OnDrawGizmos()
    {
        if (allNodes == null || allNodes.Length == 0) return;

        // Disegna nodi
        Gizmos.color = Color.red;
        foreach (var node in allNodes)
        {
            if (node == null) continue;
            Gizmos.DrawSphere(node.transform.position, nodeSize);
        }

        // Disegna connessioni
        Gizmos.color = Color.blue;
        foreach (var node in allNodes)
        {
            if (node == null) continue;

            foreach (var neighbor in node.neighbors)
            {
                if (neighbor == null) continue;

                Gizmos.DrawLine(
                    node.transform.position,
                    neighbor.transform.position
                );
            }
        }
    }
}
