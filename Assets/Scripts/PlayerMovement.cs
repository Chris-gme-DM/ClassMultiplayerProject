using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : NetworkBehaviour
{
    readonly public SyncVar<float> syncSpeed = new SyncVar<float>();
    public float speed = 5f;
    private Vector3 _moveInput;
    private Vector3 _lookInput;
    private Rigidbody _rigidbody;
    private CinemachineCamera _cinemachineCamera;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
    }
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        if (!base.Owner.IsLocalClient) return;
        
        _cinemachineCamera.Follow = this.transform;
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
        //        _moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        if (Input.GetKey(KeyCode.M))
        {
            ChangeSpeed();
        }

        // Input an Server senden
    }

    private void FixedUpdate()
    {
        // Bewegung mit Rigidbody
        if (!IsOwner ) return;
        Vector3 forwardMovement = transform.forward * _moveInput.y;
        Vector3 rightMovement = transform.right * _moveInput.x;
        Vector3 direction = (forwardMovement + rightMovement).normalized;
        Vector3 targetVelocity = direction * speed;
        _rigidbody.linearVelocity = new Vector3(targetVelocity.x, _rigidbody.linearVelocity.y, targetVelocity.z);
    }

//   [ServerRpc]
//   private void MoveServer(Vector3 input)
//   {
//       // Bewegung nur auf dem Server berechnen (authoritative)
//       Vector3 movement = input.normalized * speed * Time.deltaTime;
//       transform.position += movement;
//   }

    [ServerRpc]
    private void ChangeSpeed()
    {
        syncSpeed.Value = 10f;
    }

    public void OnSpeedChange(float prev, float next, bool asServer)
    {
        speed = syncSpeed.Value;
    }
    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        _moveInput = value.Get<Vector2>();
    }
    public void OnLook(InputValue value)
    {
        if (!IsOwner) return;
        _lookInput = value.Get<Vector2>();
    }
}