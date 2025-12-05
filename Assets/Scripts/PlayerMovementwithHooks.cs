using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementWithHooks : NetworkBehaviour
{
    // SyncVar that synchronizes the player's movement speed across the network
    public readonly SyncVar<float> syncSpeed = new SyncVar<float>();

    private Vector3 _moveInput;
    private float _pitch;
    private float _yaw;
    [SerializeField] private float _mouseSensitivity = 0.5f;
    private PlayerData _playerData;
    private GameObject _playerHead;
    private void OnDisable()
    {
        // Unsubscribe from tick event to avoid leaks
        if (TimeManager != null)
            TimeManager.OnTick -= TimeManager_OnTick;
    }

    private void Start()
    {
        // Register callback when the SyncVar changes
        syncSpeed.OnChange += OnSpeedChange;

        // Subscribe to tick event when the object is active
        TimeManager.OnTick += TimeManager_OnTick;

        // Initialize default speed on the server (if not already set)
        if (syncSpeed.Value == 0f)
            syncSpeed.Value = 5f;

        _playerData = GetComponent<PlayerData>();
        _playerHead = transform.Find("PlayerHead").gameObject;

    }
    public void OnLook(InputValue value)
    {
        if (!IsOwner) return;
        Vector2 lookInput = value.Get<Vector2>() * _mouseSensitivity;
        _yaw += lookInput.x;
        _pitch -= lookInput.y;
        _pitch = Mathf.Clamp(_pitch, -90f, 90f);

        ApplyLook(_pitch, _yaw);
        SendLook(_pitch, _yaw);
    }
    private void ApplyLook(float pitch, float yaw)
    {
        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        _playerHead.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
    [ServerRpc]
    private void SendLook(float pitch, float yaw)
    {
        ApplyLook(pitch, yaw);
    }
    /// <summary>
    /// Called by FishNet's TimeManager on every network tick.
    /// </summary>
    private void TimeManager_OnTick()
    {
        // Only the owning client should read input
        if (!IsOwner)
            return;

        // Make sure we actually have a keyboard (e.g. not on some weird platform)
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // --- WASD movement using the new Input System ---
        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed) horizontal -= 1f;
        if (keyboard.dKey.isPressed) horizontal += 1f;
        if (keyboard.sKey.isPressed) vertical -= 1f;
        if (keyboard.wKey.isPressed) vertical += 1f;

        _moveInput = new Vector3(horizontal, 0f, vertical);

        // M key toggles speed (new Input System)
        if (keyboard.mKey != null && keyboard.mKey.wasPressedThisFrame)
            ChangeSpeed();
        // C key changes color (new Input System)
        if (keyboard.cKey != null && keyboard.cKey.wasPressedThisFrame)
            ChangeColor();
        // H key to get hit (new Input System)
        if (keyboard.hKey != null && keyboard.hKey.wasPressedThisFrame)
            GetHit();
        // Send input to the server (server-authoritative movement)
        if (_moveInput != Vector3.zero)
            MoveServer(_moveInput);
    }

    [ServerRpc]
    private void MoveServer(Vector3 input)
    {
        // Use TickDelta for tick-based movement instead of Time.deltaTime
        float delta = (float)TimeManager.TickDelta;

        // Calculate movement on the server only (server-authoritative)
        Vector3 movement = (transform.forward * input.z + transform.right * input.x).normalized * syncSpeed.Value;
        // Apply movement to server-side position
        transform.position += movement * delta;

        // Create callback message
        string callbackText = $"Moved by: {movement}";

        // Send callback only to the owning client
        MoveCallback(Owner, callbackText);
    }

    // First parameter MUST be NetworkConnection for a TargetRpc
    [TargetRpc]
    private void MoveCallback(NetworkConnection conn, string msg)
    {
        // Runs only on the client that owns this object
        Debug.Log($"[Callback] {msg}");
    }

    [ServerRpc]
    private void ChangeSpeed()
    {
        // Toggle between two speeds (server decides)
        syncSpeed.Value = syncSpeed.Value == 5f ? 10f : 5f;
    }
    [ServerRpc]
    private void ChangeColor()
    {
        // Callback message
        Debug.Log("Changing color on server...");

        // Change to a random color on the server
        _playerData.AssignColor();
    }
    [ServerRpc]
    private void GetHit()
    {
        // Callback message
        Debug.Log("Player got hit!");
        _playerData.ChangeHealth(-10);
    }

    public void OnSpeedChange(float prev, float next, bool asServer)
    {
        // Logs whenever the speed SyncVar changes
        Debug.Log($"Speed changed: {prev} → {next}");
    }
}
