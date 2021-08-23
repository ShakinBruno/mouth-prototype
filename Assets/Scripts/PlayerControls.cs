using System.Collections;
using UnityEngine;

public class PlayerControls : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform groundChecker;
    [SerializeField] private Transform playerRaycastPivot;

    [Header("Placement On Crouch"), Min(0)] 
    [SerializeField] private float controllerRadius;
    [SerializeField] private float controllerHeight;
    [SerializeField] private Vector3 cameraPosition;
    [SerializeField] private Vector3 raycastPivotPosition;
    [SerializeField] private Vector3 controllerCenter;

    [Header("Ground Mask")] 
    [SerializeField] private LayerMask groundMask;

    [Header("Values"), Min(0)] 
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private float speedTransition = 5f;
    [SerializeField] private float cameraAndCollidersTransition = 10f;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsCrouching = Animator.StringToHash("IsCrouching");
    private float radiusOnStart;
    private float heightOnStart;
    private float velocity;
    private float targetSpeed;
    private bool isGrounded;
    private bool isRunning;
    private bool isCrouching;

    private CharacterController controller;
    private Animator animator;
    private Coroutine activeCoroutine;
    private Camera mainCamera;
    private Vector3 cameraOnStart;
    private Vector3 raycastPivotOnStart;
    private Vector3 centerOnStart;
    private PlayerState playerState;
    private PlayerState PlayerState
    {
        get => playerState;
        set
        {
            onStateChange?.Invoke(playerState, value);
            playerState = value;
        }
    }

    private delegate void StateChangeEvent(PlayerState previousState,PlayerState newState);
    private StateChangeEvent onStateChange;
    

    private void Awake()
    {
        mainCamera = Camera.main;
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        onStateChange += HandleStateChange;
        playerState = PlayerState.Default;
    }

    private void Start()
    {
        cameraOnStart = mainCamera.transform.localPosition;
        raycastPivotOnStart = playerRaycastPivot.transform.localPosition;
        centerOnStart = controller.center;
        radiusOnStart = controller.radius;
        heightOnStart = controller.height;
        PlayerState = PlayerState.Walk;
        targetSpeed = walkSpeed;
    }

    private void Update()
    {
        PlayerMovement();
        UpdateMovementAnimations();
        PlayerInput();
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
                activeCoroutine = StartCoroutine(WalkBehaviour());
                break;
            case PlayerState.Run:
                activeCoroutine = StartCoroutine(RunBehaviour());
                break;
            case PlayerState.Crouch:
                activeCoroutine = StartCoroutine(CrouchBehaviour());
                break;
        }
    }

    private IEnumerator WalkBehaviour()
    {
        yield return new WaitUntil(() => PlayerState == PlayerState.Walk);

        yield return ResetCrouchStats();

        while (PlayerState == PlayerState.Walk)
        {
            targetSpeed = Mathf.Lerp(targetSpeed, walkSpeed, speedTransition * Time.deltaTime);

            yield return null;
        }
    }

    private IEnumerator RunBehaviour()
    {
        yield return new WaitUntil(() => PlayerState == PlayerState.Run);

        yield return ResetCrouchStats();

        while (isRunning)
        {
            targetSpeed = Mathf.Lerp(targetSpeed, runSpeed, speedTransition * Time.deltaTime);

            yield return null;
        }
        
        PlayerState = PlayerState.Walk;
    }

    private IEnumerator CrouchBehaviour()
    {
        yield return new WaitUntil(() => PlayerState == PlayerState.Crouch);
        
        var isStillCrouching = true;
        
        animator.SetBool(IsCrouching, true);
        
        yield return UpdateCameraAndColliders(cameraPosition, raycastPivotPosition, controllerCenter, controllerRadius, controllerHeight);

        while (isStillCrouching)
        {
            if (isCrouching)
            {
                isStillCrouching = false;
            }

            targetSpeed = Mathf.Lerp(targetSpeed, crouchSpeed, speedTransition * Time.deltaTime);
            
            yield return null;
        }

        animator.SetBool(IsCrouching, false);
        
        yield return UpdateCameraAndColliders(cameraOnStart, raycastPivotOnStart, centerOnStart, radiusOnStart, heightOnStart); // camera and colliders not updating from crouch to sprint
        
        PlayerState = PlayerState.Walk;
    }

    private IEnumerator UpdateCameraAndColliders(Vector3 cameraPos, Vector3 raycastPivotPos, Vector3 centerPos, float radius, float height)
    {
        while (mainCamera.transform.localPosition != cameraPos)
        {
            mainCamera.transform.localPosition = Vector3.MoveTowards(mainCamera.transform.localPosition, cameraPos, cameraAndCollidersTransition * Time.deltaTime);
            playerRaycastPivot.transform.localPosition = Vector3.MoveTowards(playerRaycastPivot.transform.localPosition, raycastPivotPos, cameraAndCollidersTransition * Time.deltaTime);
            controller.center = Vector3.MoveTowards(controller.center, centerPos, cameraAndCollidersTransition * Time.deltaTime);
            controller.radius = Mathf.MoveTowards(controller.radius, radius, cameraAndCollidersTransition * Time.deltaTime);
            controller.height = Mathf.MoveTowards(controller.height, height, cameraAndCollidersTransition * Time.deltaTime);

            yield return null;
        }
    }

    private IEnumerator ResetCrouchStats()
    {
        animator.SetBool(IsCrouching, false);
        
        yield return UpdateCameraAndColliders(cameraOnStart, raycastPivotOnStart, centerOnStart, radiusOnStart, heightOnStart);
    }

    private void PlayerInput()
    {
        isRunning = Input.GetKey(KeyCode.LeftShift);
        isCrouching = Input.GetKeyDown(KeyCode.C);

        if (isRunning)
        {
            PlayerState = PlayerState.Run;
        }
        else if (isCrouching)
        {
            PlayerState = PlayerState.Crouch;
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
