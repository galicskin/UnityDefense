using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCam;            // 1인칭 카메라(자식 권장)

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
    private float pitch;           // 카메라 상하각(로컬)
    private float velY;            // 중력/점프 적분

    private Transform headAnchor;
    [SerializeField] private float eyeRatio = 0.92f; // 눈높이 (캡슐 높이 비율)
    [SerializeField] private float extraEyeOffset = 0f;
    void CameraInit()
    {
        // 카메라 없으면 MainCamera나 새 카메라 생성
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

        // 머리 앵커 만들기
        headAnchor = new GameObject("Head").transform;
        headAnchor.SetParent(transform, false);

        // 눈높이 계산 (CharacterController 기준)
        var cc = GetComponent<CharacterController>();
        float height = cc != null ? cc.height : 2f;
        float centerY = cc != null ? cc.center.y : 0f;
        float eyeY = centerY + (height * 0.5f * eyeRatio) + extraEyeOffset;

        headAnchor.localPosition = new Vector3(0f, eyeY, 0f);

        // 카메라를 머리 앵커에 붙이기
        playerCam.transform.SetParent(headAnchor, false);
        playerCam.transform.localPosition = Vector3.zero;
        playerCam.transform.localRotation = Quaternion.identity;

        // 커서 잠금
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
        CameraMove(); // 1) 시선 먼저
        Move();       // 2) 이동 및 중력
        ClickKeysRule(); // 3) 기타 단발 입력(예: 상호작용 키 등)
    }

    void LateUpdate()
    {
        // 카메라 흔들림/보정 효과를 여기에 넣으면 덜 떤답니다.
        // (현재 예시는 Pitch만 이미 반영되어 별도 처리 없음)
    }

    // ====== Input System 이벤트 바인딩 ======
    // PlayerInput 컴포넌트의 Actions에서 이 함수들에 연결하세요.
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

    // ====== 기능 구현부 ======

    // 이동(중력/점프 포함)
    public void Move()
    {
        // 로컬 입력을 월드로 변환
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 world = transform.TransformDirection(input) * moveSpeed;

        // 중력/점프
        if (characterController.isGrounded)
        {
            if (velY < 0f) velY = -2f;
            if (jumpPressed) velY = jumpSpeed;
        }
        velY += gravity * Time.deltaTime;

        Vector3 motion = world + Vector3.up * velY;
        characterController.Move(motion * Time.deltaTime);
    }

    // 점프만 따로 쓰고 싶다면(현재는 Move() 내부에서 처리 중)
    public void Jump()
    {
        if (characterController.isGrounded) velY = jumpSpeed;
    }

    // 카메라/시선 회전: 몸통(Yaw), 카메라(Pitch)
    public void CameraMove()
    {
        if (playerCam == null) return;

        // 마우스 감도: 프레임 독립
        float yawDelta = lookInput.x * sensX * Time.deltaTime;
        float pitchDelta = -lookInput.y * sensY * Time.deltaTime;

        // 몸통 Yaw
        transform.Rotate(0f, yawDelta, 0f);

        // 카메라 Pitch(클램프)
        pitch = Mathf.Clamp(pitch + pitchDelta, pitchMin, pitchMax);
        var camTr = playerCam.transform;
        camTr.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    private bool BuildPressed = false;
    public void OnBuildTower(InputAction.CallbackContext ctx)
    {
        if (ctx.started) BuildPressed = true;
        
    }

    // 단발 키 규칙/우선순위 처리(상호작용, 달리기 토글, 인벤토리 등)
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
        // 예: 좌Shift를 누르면 잠깐 달리기(토글/홀드 취향껏)
        // var isRun = Keyboard.current.leftShiftKey.isPressed;
        // if (isRun) moveSpeed = runSpeed; else moveSpeed = walkSpeed;

        // 예: 상호작용 (E 키)
        // if (Keyboard.current.eKey.wasPressedThisFrame) Interact();
    }

    
}
