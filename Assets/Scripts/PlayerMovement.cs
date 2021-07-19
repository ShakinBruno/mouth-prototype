using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform groundChecker;

    [Header("LayerMask")] 
    [SerializeField] private LayerMask groundMask;
    
    [Header("Values")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private float gravityMultiplier = 1f;

    private const float gravity = 9.81f;
    private float velocity;
    private bool isGrounded;

    private CharacterController controller;
    private EnemyAI enemy;
    
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        enemy = GameObject.FindWithTag("Enemy").GetComponent<EnemyAI>();
    }

    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundChecker.position, controller.radius, groundMask);

        if (isGrounded && velocity < 0f)
        {
            velocity = -gravity;
        }
        
        velocity -= gravity * gravityMultiplier * Time.deltaTime;
        
        var moveForward = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        var rotateAround = Input.GetAxis("Horizontal") * rotateSpeed * Time.deltaTime;

        var moveDirection = transform.TransformDirection(new Vector3(0f, velocity * Time.deltaTime, moveForward));
        
        controller.Move(moveDirection);
        transform.Rotate(0f, rotateAround, 0f);
    }
    
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Enemy"))
        {
            enemy.StartChasing();
        }
    }
}
