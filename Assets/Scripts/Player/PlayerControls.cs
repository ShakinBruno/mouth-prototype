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
        [SerializeField] private Transform raycastTarget;
        [SerializeField] private Camera mainCamera;

        [Header("State Change Values")] 
        [SerializeField] private CrouchParameters onStand;
        [SerializeField] private CrouchParameters onCrouch;
        
        [Header("Mappings")] 
        [SerializeField] private MovementMapping[] movementMappings;
        
        [Header("Ground Mask")] 
        [SerializeField] private LayerMask groundMask;

        [Header("Values"), Min(0)]
        [SerializeField] private float rotateSpeed = 100f;
        [SerializeField] private float toCrouchTransition = 3f;
        [SerializeField] private float speedTransition = 7f;
        [SerializeField] private float pushPower = 2f;

        [Serializable] private struct CrouchParameters
        {
            public float radius;
            public float height;
            public Vector3 raycastTargetPosition;
            public Vector3 cameraPosition;
            public Vector3 center;
            public bool toCrouch;
        }
        
        [Serializable] private struct MovementMapping
        {
            public PlayerState state;
            public float speed;
        }
        
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int IsCrouching = Animator.StringToHash("IsCrouching");
        private float gravity;
        private float moveSpeed;
        private float targetSpeed;
        private float middlePoint;
        private bool isGrounded;
        private bool isCrouching;

        private CharacterController controller;
        private Animator animator;
        private Coroutine activeCoroutine;
        private PlayerState oldState;
        private PlayerState newState
        {
            get => oldState;
            set
            {
                OnStateChange?.Invoke(oldState, value);
                oldState = value;
            }
        }

        private delegate void StateChangeEvent(PlayerState previousState,PlayerState newState);
        private event StateChangeEvent OnStateChange;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
        
            oldState = PlayerState.Default;
        }

        private void OnEnable()
        {
            OnStateChange += HandleStateChange;
        }

        private void OnDisable()
        {
            OnStateChange -= HandleStateChange;
        }

        private void Start()
        {
            middlePoint = mainCamera.transform.localPosition.y;
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

            if (nextState == PlayerState.Walk || nextState == PlayerState.Run)
            {
                activeCoroutine = StartCoroutine(MovementBehaviour(nextState, onStand));
            }
            else if (nextState == PlayerState.Crouch)
            {
                activeCoroutine = StartCoroutine(MovementBehaviour(nextState, onCrouch));
            }
        }

        private IEnumerator MovementBehaviour(PlayerState state, CrouchParameters onState)
        {
            yield return new WaitUntil(() => newState == state);
        
            targetSpeed = GetMovementMapping(state).speed;
            animator.SetBool(IsCrouching, onState.toCrouch);

            yield return UpdateParamsPosition(onState);
        }

        private IEnumerator UpdateParamsPosition(CrouchParameters onState)
        {
            var hasReachedTarget =
                Mathf.Approximately(controller.radius, onState.radius) &&
                Mathf.Approximately(controller.height, onState.height) &&
                raycastTarget.localPosition == onState.raycastTargetPosition &&
                mainCamera.transform.localPosition == onState.cameraPosition &&
                controller.center == onState.center;

            while (!hasReachedTarget)
            {
                controller.radius = Mathf.MoveTowards(controller.radius, onState.radius, toCrouchTransition * Time.deltaTime);
                controller.height = Mathf.MoveTowards(controller.height, onState.height, toCrouchTransition * Time.deltaTime);
                raycastTarget.localPosition = Vector3.MoveTowards(raycastTarget.localPosition, onState.raycastTargetPosition, toCrouchTransition * Time.deltaTime);
                mainCamera.transform.localPosition = Vector3.MoveTowards(mainCamera.transform.localPosition, onState.cameraPosition, toCrouchTransition * Time.deltaTime);
                controller.center = Vector3.MoveTowards(controller.center, onState.center, toCrouchTransition * Time.deltaTime);

                middlePoint = mainCamera.transform.localPosition.y;
                
                hasReachedTarget = Mathf.Approximately(controller.radius, onState.radius) &&
                                   Mathf.Approximately(controller.height, onState.height) &&
                                   raycastTarget.localPosition == onState.raycastTargetPosition &&
                                   mainCamera.transform.localPosition == onState.cameraPosition &&
                                   controller.center == onState.center;
                
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
    
        private MovementMapping GetMovementMapping(PlayerState state)
        {
            foreach (var mapping in movementMappings)
            {
                if (mapping.state == state)
                {
                    return mapping;
                }
            }

            return movementMappings[0];
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

        public float GetTargetSpeed()
        {
            return moveSpeed;
        }

        public float GetMiddlePoint()
        {
            return middlePoint;
        }
    }
}
