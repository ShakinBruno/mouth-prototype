using Mouth.Player;
using UnityEngine;

namespace Mouth.Core
{
    [RequireComponent(typeof(Camera))]
    public class HeadBobbing : MonoBehaviour
    {
        [Header("Values"), Min(0)]
        [SerializeField] private float bobbingSpeed = 0.18f;
        [SerializeField] private float bobbingAmplitude = 0.2f;
    
        private float timer;

        private PlayerControls player;

        private void Awake()
        {
            player = GameObject.FindWithTag("Player").GetComponent<PlayerControls>();
        }

        private void Update () 
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
                currentPosition.y = player.GetMiddlePoint() + translateChange;
            }
            else
            {
                currentPosition.y = player.GetMiddlePoint();
            }

            transform.localPosition =  currentPosition;
        }
    }
}

