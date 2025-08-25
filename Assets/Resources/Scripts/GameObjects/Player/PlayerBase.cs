using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCam;            // 1��Ī ī�޶�(�ڽ� ����)

    public Camera GetCamera() 
    {
        if (playerCam != null)
            return playerCam;

        Debug.LogWarning("Player camera is not assigned!");
        return null;
    }

    [Header("Move")] 
    [SerializeField] protected const float moveSpeed = 5f;
    [SerializeField] protected const float jumpSpeed = 5f;
    [SerializeField] protected const float gravity = -20f;

    [Header("Look")]
    [SerializeField] private const float sensX = 200f;          // deg/sec
    [SerializeField] private const float sensY = 200f;          // deg/sec
    [SerializeField] private const float pitchMin = -85f;
    [SerializeField] private const float pitchMax = 85f;

    private CharacterController characterController;
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
        characterController = GetComponent<CharacterController>();
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
        if (characterController.isGrounded)
        {
            if (velY < 0f) velY = -2f;
            if (jumpPressed) velY = jumpSpeed;
        }
        velY += gravity * Time.deltaTime;

        Vector3 motion = world + Vector3.up * velY;
        characterController.Move(motion * Time.deltaTime);
    }

    // ������ ���� ���� �ʹٸ�(����� Move() ���ο��� ó�� ��)
    public void Jump()
    {
        if (characterController.isGrounded) velY = jumpSpeed;
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

    private bool BuildPressed = false;
    public void OnBuildTower(InputAction.CallbackContext ctx)
    {
        if (ctx.started) BuildPressed = true;
        
    }

    // �ܹ� Ű ��Ģ/�켱���� ó��(��ȣ�ۿ�, �޸��� ���, �κ��丮 ��)
    public void ClickKeysRule()
    {
        if (BuildPressed)
        {
            BuildPressed = false;
            var buildSystem = GameSystemManager.Instance.GetSystem<BuildingSystem>();
            buildSystem.SetPreviewTower(TowerId.test);
            if (buildSystem.CheckBuildCondition(TowerId.test))
            {
                buildSystem.CreateTower(TowerId.test);
                //Create Tower
            }
            else
            {
                //don't Create Tower
            }

        }
        // ��: ��Shift�� ������ ��� �޸���(���/Ȧ�� ���ⲯ)
        // var isRun = Keyboard.current.leftShiftKey.isPressed;
        // if (isRun) moveSpeed = runSpeed; else moveSpeed = walkSpeed;

        // ��: ��ȣ�ۿ� (E Ű)
        // if (Keyboard.current.eKey.wasPressedThisFrame) Interact();
    }

    
}
