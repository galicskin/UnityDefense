using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCam;            // 1��Ī ī�޶�(�ڽ� ����)

    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpSpeed = 5f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField] private float sensX = 200f;          // deg/sec
    [SerializeField] private float sensY = 200f;          // deg/sec
    [SerializeField] private float pitchMin = -85f;
    [SerializeField] private float pitchMax = 85f;

    private CharacterController cc;
    private Vector2 moveInput;       // WASD
    private Vector2 lookInput;       // Mouse delta
    private bool jumpPressed;
    private float pitch;           // ī�޶� ���ϰ�(����)
    private float velY;            // �߷�/���� ����

    private Transform headAnchor;
    [SerializeField] private float eyeRatio = 0.92f; // ������ (ĸ�� ���� ����)
    [SerializeField] private float extraEyeOffset = 0f;
    void CameraInit()
    {
        // ī�޶� ������ MainCamera�� �� ī�޶� ����
        if (playerCam == null)
        {
            playerCam = Camera.main;
            if (playerCam == null)
            {
                var camGO = new GameObject("PlayerCamera");
                playerCam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }
        }

        // �Ӹ� ��Ŀ �����
        headAnchor = new GameObject("Head").transform;
        headAnchor.SetParent(transform, false);

        // ������ ��� (CharacterController ����)
        var cc = GetComponent<CharacterController>();
        float height = cc != null ? cc.height : 2f;
        float centerY = cc != null ? cc.center.y : 0f;
        float eyeY = centerY + (height * 0.5f * eyeRatio) + extraEyeOffset;

        headAnchor.localPosition = new Vector3(0f, eyeY, 0f);

        // ī�޶� �Ӹ� ��Ŀ�� ���̱�
        playerCam.transform.SetParent(headAnchor, false);
        playerCam.transform.localPosition = Vector3.zero;
        playerCam.transform.localRotation = Quaternion.identity;

        // Ŀ�� ���
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (playerCam == null)
            playerCam = GetComponentInChildren<Camera>(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        CameraInit();
    }

    void Update()
    {
        CameraMove(); // 1) �ü� ����
        Move();       // 2) �̵� �� �߷�
        ClickKeysRule(); // 3) ��Ÿ �ܹ� �Է�(��: ��ȣ�ۿ� Ű ��)
    }

    void LateUpdate()
    {
        // ī�޶� ��鸲/���� ȿ���� ���⿡ ������ �� ����ϴ�.
        // (���� ���ô� Pitch�� �̹� �ݿ��Ǿ� ���� ó�� ����)
    }

    // ====== Input System �̺�Ʈ ���ε� ======
    // PlayerInput ������Ʈ�� Actions���� �� �Լ��鿡 �����ϼ���.
    public void OnMove(InputAction.CallbackContext ctx)
    { 
        moveInput = ctx.ReadValue<Vector2>();
    }
    public void OnLook(InputAction.CallbackContext ctx) 
    {
        lookInput = ctx.ReadValue<Vector2>();
    } 
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started) jumpPressed = true;
        if (ctx.canceled) jumpPressed = false;
    }

    // ====== ��� ������ ======

    // �̵�(�߷�/���� ����)
    public void Move()
    {
        // ���� �Է��� ����� ��ȯ
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 world = transform.TransformDirection(input) * moveSpeed;

        // �߷�/����
        if (cc.isGrounded)
        {
            if (velY < 0f) velY = -2f;
            if (jumpPressed) velY = jumpSpeed;
        }
        velY += gravity * Time.deltaTime;

        Vector3 motion = world + Vector3.up * velY;
        cc.Move(motion * Time.deltaTime);
    }

    // ������ ���� ���� �ʹٸ�(����� Move() ���ο��� ó�� ��)
    public void Jump()
    {
        if (cc.isGrounded) velY = jumpSpeed;
    }

    // ī�޶�/�ü� ȸ��: ����(Yaw), ī�޶�(Pitch)
    public void CameraMove()
    {
        if (playerCam == null) return;

        // ���콺 ����: ������ ����
        float yawDelta = lookInput.x * sensX * Time.deltaTime;
        float pitchDelta = -lookInput.y * sensY * Time.deltaTime;

        // ���� Yaw
        transform.Rotate(0f, yawDelta, 0f);

        // ī�޶� Pitch(Ŭ����)
        pitch = Mathf.Clamp(pitch + pitchDelta, pitchMin, pitchMax);
        var camTr = playerCam.transform;
        camTr.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    // �ܹ� Ű ��Ģ/�켱���� ó��(��ȣ�ۿ�, �޸��� ���, �κ��丮 ��)
    public void ClickKeysRule()
    {
        // ��: ��Shift�� ������ ��� �޸���(���/Ȧ�� ���ⲯ)
        // var isRun = Keyboard.current.leftShiftKey.isPressed;
        // if (isRun) moveSpeed = runSpeed; else moveSpeed = walkSpeed;

        // ��: ��ȣ�ۿ� (E Ű)
        // if (Keyboard.current.eKey.wasPressedThisFrame) Interact();
    }
}
