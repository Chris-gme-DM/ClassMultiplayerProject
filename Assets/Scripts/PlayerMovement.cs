using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : NetworkBehaviour
{
    readonly public SyncVar<float> syncSpeed = new SyncVar<float>();
    public float speed = 5f;
    private Vector2 _moveInput;
    private Rigidbody _rigidbody;
    private CinemachineCamera _cinemachineCamera;
    // Server-side Limits
    [Header("ServerLimits")]
    [Tooltip("Clamps PlayerMovement in m/s")]
    [SerializeField] private const float maxSpeed = 10f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
    }
    private void Start()
    {
        syncSpeed.OnChange += OnSpeedChange;
    }

    void Update()
    {
        // Nur lokaler Client liest Input
        if (!IsOwner)
            return;

        _moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        if (Input.GetKey(KeyCode.M))
        {
            ChangeSpeed();
        }

        // Input an Server senden
        MoveServer(_moveInput);
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