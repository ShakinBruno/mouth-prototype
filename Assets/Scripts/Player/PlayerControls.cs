using System;
using System.Collections;
using Mouth.AI;
using UnityEngine;
using UnityEngine.AI;

namespace Mouth.Player
{
    [RequireComponent(typeof(CharacterController), typeof(Animator))]
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
        [SerializeField] private float toCrouchTransition = 3f;
        [SerializeField] private float speedTransition = 7f;
        [SerializeField] private float pushPower = 2f;

        [Serializable] private struct PlayerMovementMapping
        {
            public PlayerState state;
            public float speed;
        }
    
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int IsCrouching = Animator.StringToHash("IsCrouching");
        private float radiusOnStart;
        private float heightOnStart;
        private float gravity;
        private float moveSpeed;
        private float targetSpeed;
        private bool isGrounded;
        private bool isCrouching;

        private CharacterController controller;
        private Animator animator;
        private Coroutine activeCoroutine;
        private Vector3 raycastPivotOnStart;
        private Vector3 centerOnStart;
        private PlayerState oldState;
        private PlayerState newState
        {
            get => oldState;
            set
            {
                ONStateChange?.Invoke(oldState, value);
                oldState = value;
            }
        }

        private delegate void StateChangeEvent(PlayerState previousState,PlayerState newState);
        private event StateChangeEvent ONStateChange;
    

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
        
            oldState = PlayerState.Default;
        }

        private void OnEnable()
        {
            ONStateChange += HandleStateChange;
        }

        private void OnDisable()
        {
            ONStateChange -= HandleStateChange;
        }

        private void Start()
        {
            raycastPivotOnStart = playerRaycastPivot.transform.localPosition;
            centerOnStart = controller.center;
            radiusOnStart = controller.radius;
            heightOnStart = controller.height;
        
            newState = PlayerState.Walk;
        }

        private void Update()
        {
            if (!controller.enabled) return;
            
            PlayerMovement();
            CalculateSpeedMovement();
            PlayerInput();

        }

        private void HandleStateChange(PlayerState previousState, PlayerState nextState)
        {
            if (previousState == nextState) return;

            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }

            switch (nextState)
            {
                case PlayerState.Walk:
                case PlayerState.Run:
                    activeCoroutine = StartCoroutine(MovementBehaviour(nextState, raycastPivotOnStart, centerOnStart,
                        radiusOnStart, heightOnStart, false));
                    break;
                case PlayerState.Crouch:
                    activeCoroutine = StartCoroutine(MovementBehaviour(nextState, raycastPivotPosition, controllerCenter,
                        controllerRadius, controllerHeight, true));
                    break;
            }
        }

        private IEnumerator MovementBehaviour(PlayerState state, Vector3 raycastPivotPos, Vector3 centerPos, float radius, float height, bool toCrouch)
        {
            yield return new WaitUntil(() => newState == state);
        
            targetSpeed = GetMovementMapping(state).speed;
            animator.SetBool(IsCrouching, toCrouch);

            yield return UpdateColliderPosition(raycastPivotPos, centerPos, radius, height);
        }

        private IEnumerator UpdateColliderPosition(Vector3 raycastPivotPos, Vector3 centerPos, float radius, float height)
        {
            var hasReachedTarget = playerRaycastPivot.transform.localPosition == raycastPivotPos &&
                                   controller.center == centerPos &&
                                   Mathf.Approximately(controller.radius, radius) &&
                                   Mathf.Approximately(controller.height, height);

            while (!hasReachedTarget)
            {
                var pivotPosition = playerRaycastPivot.transform.localPosition;

                hasReachedTarget = pivotPosition == raycastPivotPos &&
                                   controller.center == centerPos &&
                                   Mathf.Approximately(controller.radius, radius) &&
                                   Mathf.Approximately(controller.height, height);

                playerRaycastPivot.transform.localPosition = Vector3.MoveTowards(pivotPosition, raycastPivotPos, toCrouchTransition * Time.deltaTime);
                controller.center = Vector3.MoveTowards(controller.center, centerPos, toCrouchTransition * Time.deltaTime);
                controller.radius = Mathf.MoveTowards(controller.radius, radius, toCrouchTransition * Time.deltaTime);
                controller.height = Mathf.MoveTowards(controller.height, height, toCrouchTransition * Time.deltaTime);

                yield return null;
            }
        }

        private void PlayerInput()
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                newState = PlayerState.Run;
                isCrouching = false;
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                newState = PlayerState.Walk;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                if (isCrouching)
                {
                    newState = PlayerState.Walk;
                    isCrouching = false;
                }
                else
                {
                    newState = PlayerState.Crouch;
                    isCrouching = true;
                }
            }
        }

        private void PlayerMovement()
        {
            isGrounded = Physics.CheckSphere(groundChecker.position, controller.radius, groundMask);

            if (isGrounded && gravity < 0f)
            {
                gravity = Physics.gravity.y;
            }

            gravity += Physics.gravity.y * Time.deltaTime;

            var moveForward = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
            var rotateAround = Input.GetAxis("Horizontal") * rotateSpeed * Time.deltaTime;

            var moveDirection = transform.TransformDirection(new Vector3(0f, gravity * Time.deltaTime, moveForward));

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
            if (!hit.collider.CompareTag("Enemy")) return;
        
            var pushDirection = (hit.transform.position - transform.position).normalized;

            hit.transform.GetComponent<EnemyAI>().ChaseWhenHit();
            hit.transform.GetComponent<NavMeshAgent>().velocity = pushDirection * pushPower;
        }

        private void CalculateSpeedMovement()
        {
            var localVelocity = transform.InverseTransformDirection(controller.velocity);
            var speed = localVelocity.z;

            moveSpeed = Mathf.MoveTowards(moveSpeed, targetSpeed, speedTransition * Time.deltaTime);
            animator.SetFloat(Speed, speed);
        }

        public float GetMovementTransition()
        {
            return toCrouchTransition;
        }

        public float GetTargetSpeed()
        {
            return moveSpeed;
        }

        public bool GetIsCrouching()
        {
            return isCrouching;
        }
    }
}
