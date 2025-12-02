using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : NetworkBehaviour
{
    // InputActions
    private InputActionReference moveAction;
    private InputAction _moveAction;

    readonly public SyncVar<float> syncSpeed = new SyncVar<float>();
    public float speed = 5f;
    private Vector3 _input;
    // Server-side Limits
    [Header("ServerLimits")]
    [Tooltip("Clamps PlayerMovement in m/s")]
    [SerializeField] private const float maxSpeed = 10f;

    private void Start()
    {
        syncSpeed.OnChange += OnSpeedChange;
        if(moveAction != null)
        {
            _moveAction = moveAction.action;
        }
        if(_moveAction != null && !_moveAction.enabled)
        {
            _moveAction.Enable();
        }
    }

    void Update()
    {
        // Nur lokaler Client liest Input
        if (!IsOwner)
            return;

        _input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        if (Input.GetKey(KeyCode.M))
        {
            ChangeSpeed();
        }

        // Input an Server senden
        MoveServer(_input);
    }

    [ServerRpc]
    private void MoveServer(Vector3 input)
    {
        // Bewegung nur auf dem Server berechnen (authoritative)
        Vector3 movement = input.normalized * speed * Time.deltaTime;
        transform.position += movement;
    }

    [ServerRpc]
    private void ChangeSpeed()
    {
        syncSpeed.Value = 10f;
    }

    public void OnSpeedChange(float prev, float next, bool asServer)
    {
        speed = syncSpeed.Value;
    }
}