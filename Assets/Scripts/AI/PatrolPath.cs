using UnityEngine;

namespace Mouth.AI
{
    public class PatrolPath : MonoBehaviour
    {
        [Header("Enemy's Route")]
        [SerializeField] private Transform[] waypoints;

        [Header("Values"), Min(0)] 
        [SerializeField] private float waypointGizmoRadius = 0.3f;

        public int GetNextIndex(int previousIndex)
        {
            int randomIndex;
        
            do
            {
                randomIndex = Random.Range(0, waypoints.Length);
            } 
            while (randomIndex == previousIndex);
        
            return randomIndex;
        }

        public Vector3 GetRandomWaypoint(int waypointIndex)
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
}
