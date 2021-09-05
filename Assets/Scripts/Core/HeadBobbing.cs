using Mouth.Player;
using UnityEngine;

namespace Mouth.Core
{
    [RequireComponent(typeof(Camera))]
    public class HeadBobbing : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerControls player;
    
        [Header("Camera Position On Crouch")]
        [SerializeField] private float cameraCrouchYPos = 1f;
    
        [Header("Values"), Min(0)]
        [SerializeField] private float bobbingSpeed = 0.18f;
        [SerializeField] private float bobbingAmplitude = 0.2f;
    
        private float timer;
        private float cameraStartPos;
        private float middlePoint;

        private void Start()
        {
            cameraStartPos = transform.localPosition.y;
            middlePoint = cameraStartPos;
        }

        private void Update () 
        {
            UpdateCameraOnCrouch();
            HeadBob();
        }

        private void HeadBob()
        {
            var waveSlice = 0f;
            var verticalInput = Input.GetAxis("Vertical");
            var currentPosition = transform.localPosition;

            if (Mathf.Abs(verticalInput) == 0f)
            {
                timer = 0f;
            }
            else
            {
                waveSlice = Mathf.Sin(timer);
                timer += bobbingSpeed * player.GetTargetSpeed() * Time.deltaTime;

                if (timer > Mathf.PI * 2f)
                {
                    timer -= Mathf.PI * 2f;
                }
            }

            if (waveSlice != 0f)
            {
                var translateChange = waveSlice * bobbingAmplitude;

                translateChange = Mathf.Abs(verticalInput) * translateChange;
                currentPosition.y = middlePoint + translateChange;
            }
            else
            {
                currentPosition.y = middlePoint;
            }

            transform.localPosition =  currentPosition;
        }

        private void UpdateCameraOnCrouch()
        {
            if (player.GetIsCrouching())
            {
                middlePoint = Mathf.MoveTowards(middlePoint, cameraCrouchYPos, player.GetMovementTransition() * Time.deltaTime);
            }
            else
            {
                middlePoint = Mathf.MoveTowards(middlePoint, cameraStartPos, player.GetMovementTransition() * Time.deltaTime);
            }
        }
    }
}

