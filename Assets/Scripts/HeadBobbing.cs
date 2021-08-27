using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HeadBobbing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerControls player;
    
    [Header("Camera Position On Crouch")]
    [SerializeField] private float cameraPosOnCrouch;
    
    [Header("Values"), Min(0)]
    [SerializeField] private float bobbingSpeed = 0.18f;
    [SerializeField] private float bobbingAmount = 0.2f;
    
    private float cameraPosOnStart;
    private float middlePoint;
    private float timer;
    
    private void Start()
    {
        cameraPosOnStart = transform.localPosition.y;
        middlePoint = cameraPosOnStart;
    }

    private void Update () 
    {
        UpdateCameraOnCrouch();
        
        transform.localPosition = HeadBobCurrentPos();
    }

    private Vector3 HeadBobCurrentPos()
    {
        var waveSlice = 0f;
        var verticalInput = Input.GetAxis("Vertical") * Time.deltaTime;
        var currentPosition = transform.localPosition;

        if (Mathf.Abs(verticalInput) == 0f)
        {
            timer = 0f;
        }
        else
        {
            waveSlice = Mathf.Sin(timer);
            timer += bobbingSpeed * player.GetTargetSpeed();

            if (timer > Mathf.PI * 2f)
            {
                timer -= Mathf.PI * 2f;
            }
        }

        if (waveSlice != 0)
        {
            var translateChange = waveSlice * bobbingAmount;

            translateChange = Mathf.Abs(verticalInput) * translateChange;
            currentPosition.y = middlePoint + translateChange;
        }
        else
        {
            currentPosition.y = middlePoint;
        }

        return currentPosition;
    }

    private void UpdateCameraOnCrouch()
    {
        if (player.GetIsCrouching())
        {
            middlePoint = Mathf.MoveTowards(middlePoint, cameraPosOnCrouch, player.GetMovementTransition() * Time.deltaTime);
        }
        else
        {
            middlePoint = Mathf.MoveTowards(middlePoint, cameraPosOnStart, player.GetMovementTransition() * Time.deltaTime);
        }
    }
}
