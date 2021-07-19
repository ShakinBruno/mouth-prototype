using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PatrolPath : MonoBehaviour
{
    [Header("Enemy's Route")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("Values")] 
    [SerializeField] private float waypointGizmoRadius = 0.3f;

    public int GetNextIndex(int previousWaypointIndex)
    {
        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, waypoints.Count);
        } 
        while (randomIndex == previousWaypointIndex);
        return randomIndex;
    }

    public Vector3 GetCurrentWaypoint(int waypointIndex)
    {
        return waypoints[waypointIndex].position;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        foreach (var waypoint in waypoints)
        { 
            Gizmos.DrawSphere(waypoint.position, waypointGizmoRadius);
        }
    }
}
