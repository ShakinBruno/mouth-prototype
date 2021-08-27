using System;
using System.Collections;
using UnityEngine;

public class PlayerControls : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform groundChecker;
    [SerializeField] private Transform playerRaycastPivot;

    [Header("Mappings")] 
    [SerializeField] private PlayerMovementMapping[] playerMovementMappings;
    
    [Header("Placement On Crouch"), Min(0)] 
    [SerializeField] private float controllerRadius;
    [SerializeField] private float controllerHeight;
    [SerializeField] private Vector3 raycastPivotPosition;
    [SerializeField] private Vector3 controllerCenter;

    [Header("Ground Mask")] 
    [SerializeField] private LayerMask groundMask;

    [Header("Values"), Min(0)]
    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private float movementParametersTransition = 10f;

    [Serializable] private struct PlayerMovementMapping
    {
        public PlayerState state;
        public float speed;
    }
    
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsCrouching = Animator.StringToHash("IsCrouching");
    private float radiusOnStart;
    private float heightOnStart;
    private float velocity;
    private float targetSpeed;
    private bool isGrounded;
    private bool isCrouching;

    private CharacterController controller;
    private Animator animator;
    private Coroutine activeCoroutine;

    private Vector3 raycastPivotOnStart;
    private Vector3 centerOnStart;
    private PlayerState playerState;
    private PlayerState PlayerState
    {
        get => playerState;
        set
        {
            ONStateChange?.Invoke(playerState, value);
            playerState = value;
        }
    }

    private delegate void StateChangeEvent(PlayerState previousState,PlayerState newState);
    private event StateChangeEvent ONStateChange;
    

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        playerState = PlayerState.Default;
    }

    private void OnEnable()
    {
        ONStateChange += HandleSpeedChange;
        ONStateChange += HandleStateChange;
    }

    private void OnDisable()
    {
        ONStateChange -= HandleStateChange;
        ONStateChange -= HandleSpeedChange;
    }

    private void Start()
    {
        raycastPivotOnStart = playerRaycastPivot.transform.localPosition;
        centerOnStart = controller.center;
        radiusOnStart = controller.radius;
        heightOnStart = controller.height;
        
        PlayerState = PlayerState.Walk;
    }

    private void Update()
    {
        PlayerMovement();
        UpdateMovementAnimations();
        PlayerInput();
    }

    private void HandleSpeedChange(PlayerState previousState, PlayerState newState)
    {
        if (previousState == newState) return;
        
        targetSpeed = GetMovementMapping(newState).speed;
    }
    
    private void HandleStateChange(PlayerState previousState, PlayerState newState)
    {
        if (previousState == newState) return;

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }

        switch (newState)
        {
            case PlayerState.Walk:
            case PlayerState.Run:
                activeCoroutine = StartCoroutine(MovementBehaviour(newState, raycastPivotOnStart, centerOnStart, radiusOnStart, heightOnStart, false));
                break;
            case PlayerState.Crouch:
                activeCoroutine = StartCoroutine(MovementBehaviour(newState, raycastPivotPosition, controllerCenter, controllerRadius, controllerHeight, true));
                break;
        }
    }

    private IEnumerator MovementBehaviour(PlayerState state, Vector3 raycastPivotPos, Vector3 centerPos, float radius, float height, bool toCrouch)
    {
        yield return new WaitUntil(() => PlayerState == state);
        
        animator.SetBool(IsCrouching, toCrouch);
        
        while (playerRaycastPivot.transform.localPosition != raycastPivotPos)
        {
            playerRaycastPivot.transform.localPosition = Vector3.MoveTowards(playerRaycastPivot.transform.localPosition, raycastPivotPos, movementParametersTransition * Time.deltaTime);
            controller.center = Vector3.MoveTowards(controller.center, centerPos, movementParametersTransition * Time.deltaTime);
            controller.radius = Mathf.MoveTowards(controller.radius, radius, movementParametersTransition * Time.deltaTime);
            controller.height = Mathf.MoveTowards(controller.height, height, movementParametersTransition * Time.deltaTime);

            yield return null;
        }
    }

    private void PlayerInput()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            PlayerState = PlayerState.Run;
            isCrouching = false;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            PlayerState = PlayerState.Walk;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isCrouching)
            {
                PlayerState = PlayerState.Walk;
                isCrouching = false;
            }
            else
            {
                PlayerState = PlayerState.Crouch;
                isCrouching = true;
            }
        }
    }

    private void PlayerMovement()
    {
        isGrounded = Physics.CheckSphere(groundChecker.position, controller.radius, groundMask);

        if (isGrounded && velocity < 0f)
        {
            velocity = Physics.gravity.y;
        }

        velocity += Physics.gravity.y * Time.deltaTime;

        var moveForward = Input.GetAxis("Vertical") * targetSpeed * Time.deltaTime;
        var rotateAround = Input.GetAxis("Horizontal") * rotateSpeed * Time.deltaTime;

        var moveDirection = transform.TransformDirection(new Vector3(0f, velocity * Time.deltaTime, moveForward));

        controller.Move(moveDirection);
        transform.Rotate(0f, rotateAround, 0f);
    }
    
    private PlayerMovementMapping GetMovementMapping(PlayerState state)
    {
        foreach (var mapping in playerMovementMappings)
        {
            if (mapping.state == state)
            {
                return mapping;
            }
        }

        return playerMovementMappings[0];
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        hit.transform.GetComponent<EnemyAI>()?.ChaseWhenHit();
    }

    private void UpdateMovementAnimations()
    {
        var localVelocity = transform.InverseTransformDirection(controller.velocity);
        var speed = localVelocity.z;

        animator.SetFloat(Speed, speed);
    }

    public float GetMovementTransition()
    {
        return movementParametersTransition;
    }

    public float GetTargetSpeed()
    {
        return targetSpeed;
    }

    public bool GetIsCrouching()
    {
        return isCrouching;
    }
}
