using UnityEngine;

public class PlayerControls : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform groundChecker;

    [Header("Ground Mask")] 
    [SerializeField] private LayerMask groundMask;

    [Header("Values"), Min(0)] 
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private float fromWalkToRunMultiplier = 5f;
    [SerializeField] private float gravityMultiplier = 1f;
    
    private static readonly int Speed = Animator.StringToHash("Speed");
    private float velocity;
    private float moveSpeed;
    private bool isGrounded;

    private CharacterController controller;
    private Animator animator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        moveSpeed = walkSpeed;
    }

    private void Update()
    {
        PlayerMovement();
        UpdateMovementAnimations();
        PlayerSprint();
    }

    private void PlayerSprint()
    {
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            moveSpeed = Mathf.Lerp(moveSpeed, walkSpeed, Time.deltaTime * fromWalkToRunMultiplier);
        }
        else
        {
            moveSpeed = Mathf.Lerp(moveSpeed, runSpeed, Time.deltaTime * fromWalkToRunMultiplier);
        }
    }

    private void PlayerMovement()
    {
        isGrounded = Physics.CheckSphere(groundChecker.position, controller.radius, groundMask);

        if (isGrounded && velocity < 0f)
        {
            velocity = Physics.gravity.y;
        }

        velocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;

        var moveForward = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        var rotateAround = Input.GetAxis("Horizontal") * rotateSpeed * Time.deltaTime;

        var moveDirection = transform.TransformDirection(new Vector3(0f, velocity * Time.deltaTime, moveForward));

        controller.Move(moveDirection);
        transform.Rotate(0f, rotateAround, 0f);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!hit.transform.TryGetComponent<EnemyAI>(out var enemy)) return;

        enemy.wasEnemyHit = true;
        enemy.EnemyState = EnemyState.Hostility;
    }

    private void UpdateMovementAnimations()
    {
        var localVelocity = transform.InverseTransformDirection(controller.velocity);
        var speed = localVelocity.z;
        animator.SetFloat(Speed, speed);
    }
}
