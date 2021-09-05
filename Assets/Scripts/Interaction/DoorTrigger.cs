using Mouth.AI;
using UnityEngine;

namespace Mouth.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class DoorTrigger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Door doorBody;
    
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                doorBody.InvokeDoorEvent(false);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                if (other.transform.GetComponent<EnemyAI>()?.GetEnemyState() == EnemyState.Patrol)
                {
                    doorBody.InvokeDoorEvent(true);
                }
            }
        }
    }
}
