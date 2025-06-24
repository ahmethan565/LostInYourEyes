using UnityEngine;
using Photon.Pun;

public class PlayerHeadBob : MonoBehaviourPunCallbacks
{
    [Header("Setup")]
    [Tooltip("The camera transform to apply head bobbing to. This should be a child of your player character.")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("The initial local position of the camera. This is used as the base for bobbing.")]
    [SerializeField] private Vector3 initialCameraLocalPosition;

    [Tooltip("Reference to the CharacterController. If you're using a different controller, you'll need to adapt the GetCurrentSpeed method.")]
    [SerializeField] private CharacterController characterController;

    [Header("Head Bob Settings - Walk")]
    [Tooltip("Amplitude of head bobbing when walking (how much the camera moves).")]
    [SerializeField] private float walkBobAmplitude = 0.015f;
    [Tooltip("Frequency of head bobbing when walking (how fast the camera moves).")]
    [SerializeField] private float walkBobFrequency = 8f;

    [Header("Head Bob Settings - Run")]
    [Tooltip("Amplitude of head bobbing when running.")]
    [SerializeField] private float runBobAmplitude = 0.03f;
    [Tooltip("Frequency of head bobbing when running.")]
    [SerializeField] private float runBobFrequency = 12f;

    [Header("Head Bob Settings - Jump Land")]
    [Tooltip("Amplitude of the head bob when landing from a jump.")]
    [SerializeField] private float jumpLandBobAmplitude = 0.05f;
    [Tooltip("Duration of the jump land bob.")]
    [SerializeField] private float jumpLandBobDuration = 0.2f;

    [Header("Smoothness Settings")]
    [Tooltip("How smoothly the head bob interpolates between states (e.g., stopping, starting, changing speed).")]
    [SerializeField] private float smoothTime = 0.1f;

    [Header("External Control Flags")]
    [Tooltip("Set this to true when the player is aiming down sights.")]
    public bool isAiming = false;
    [Tooltip("Set this to true when the player is running.")]
    public bool isRunning = false;

    // Private variables for head bob logic
    private float _timer;
    private Vector3 _targetCameraLocalPosition;
    private Vector3 _currentCameraLocalVelocity;
    private bool _wasGrounded;
    private float _jumpLandTimer;

    // Event for footstep sounds (optional)
    public delegate void FootstepEventHandler();
    public static event FootstepEventHandler OnFootstep;

    private void Start()
    {
        // Only apply head bob to the local player
        if (!photonView.IsMine)
        {
            enabled = false; // Disable the script for remote players
            return;
        }

        if (cameraTransform == null)
        {
            Debug.LogError("PlayerHeadBob: Camera Transform is not assigned! Head bobbing will not work.", this);
            enabled = false;
            return;
        }

        if (characterController == null)
        {
            Debug.LogWarning("PlayerHeadBob: CharacterController is not assigned. Ensure GetCurrentSpeed method is adapted for your custom controller.", this);
        }

        // Store the initial local position of the camera
        // This assumes the camera is a child of the player character and its local position is relevant.
        if (initialCameraLocalPosition == Vector3.zero)
        {
            initialCameraLocalPosition = cameraTransform.localPosition;
        }

        _targetCameraLocalPosition = initialCameraLocalPosition;
        _wasGrounded = characterController != null ? characterController.isGrounded : true; // Assume grounded if no controller
    }

    private void Update()
    {
        if (!photonView.IsMine) return; // Double-check for safety

        HandleJumpLandingBob();
        ApplyHeadBob();
    }

    private void ApplyHeadBob()
    {
        if (isAiming)
        {
            // Smoothly return to initial position when aiming
            _targetCameraLocalPosition = initialCameraLocalPosition;
            cameraTransform.localPosition = Vector3.SmoothDamp(cameraTransform.localPosition, _targetCameraLocalPosition, ref _currentCameraLocalVelocity, smoothTime);
            _timer = 0; // Reset timer to prevent jump when stopping ADS
            return;
        }

        float speed = GetCurrentSpeed();

        if (speed > 0.1f) // Player is moving
        {
            float currentAmplitude = isRunning ? runBobAmplitude : walkBobAmplitude;
            float currentFrequency = isRunning ? runBobFrequency : walkBobFrequency;

            _timer += Time.deltaTime * currentFrequency;

            float xBob = Mathf.Cos(_timer) * currentAmplitude;
            float yBob = Mathf.Sin(_timer * 2) * currentAmplitude * 0.8f; // Vertical bob is usually slightly less pronounced and twice the frequency

            _targetCameraLocalPosition = initialCameraLocalPosition + new Vector3(xBob, yBob, 0f);

            // Optional: Footstep sound integration
            // This will trigger a footstep sound at the peak/trough of the bob
            if (Mathf.Abs(_timer % (Mathf.PI * 2)) < (currentFrequency * Time.deltaTime) * 0.5f) // Check if timer is near a full cycle
            {
                OnFootstep?.Invoke();
            }
        }
        else // Player is idle
        {
            _targetCameraLocalPosition = initialCameraLocalPosition;
            _timer = 0; // Reset timer when idle to prevent immediate bob when starting to move
        }

        // Apply jump land bob on top of regular bob
        if (_jumpLandTimer > 0)
        {
            float landBobProgress = 1 - (_jumpLandTimer / jumpLandBobDuration);
            float landBobOffset = Mathf.Sin(landBobProgress * Mathf.PI) * jumpLandBobAmplitude; // Smooth ease-out effect
            _targetCameraLocalPosition.y -= landBobOffset; // Subtract to move camera down slightly
        }

        cameraTransform.localPosition = Vector3.SmoothDamp(cameraTransform.localPosition, _targetCameraLocalPosition, ref _currentCameraLocalVelocity, smoothTime);
    }

    private float GetCurrentSpeed()
    {
        if (characterController != null)
        {
            // Use characterController.velocity to get the actual movement speed
            // We only care about horizontal movement for bobbing
            Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);
            return horizontalVelocity.magnitude;
        }
        // If you're using a custom controller, you'll need to replace this
        // with your own logic to get the player's current ground speed.
        // For example, if you have a custom movement script, you might expose a public property for speed.
        return 0f; // Return 0 if no character controller is assigned and no custom logic is provided.
    }

    private void HandleJumpLandingBob()
    {
        if (characterController == null) return;

        bool isCurrentlyGrounded = characterController.isGrounded;

        if (!_wasGrounded && isCurrentlyGrounded)
        {
            // Player just landed
            _jumpLandTimer = jumpLandBobDuration;
        }

        if (_jumpLandTimer > 0)
        {
            _jumpLandTimer -= Time.deltaTime;
        }

        _wasGrounded = isCurrentlyGrounded;
    }

    // Call this method from your player movement script when the player starts running
    public void SetRunning(bool running)
    {
        isRunning = running;
    }

    // Call this method from your aiming script when the player starts/stops aiming
    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }

    // Optional: Reset the bobbing state
    public void ResetBobbing()
    {
        _timer = 0;
        _targetCameraLocalPosition = initialCameraLocalPosition;
        cameraTransform.localPosition = initialCameraLocalPosition;
        _currentCameraLocalVelocity = Vector3.zero;
        _jumpLandTimer = 0;
    }
}