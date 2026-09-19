using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    [Header("移动配置")]
    public Rigidbody2D rb;
    public Animator anim;
    [SerializeField] private int moveSpeed = 3; // 基础移动速度

    [Header("状态标记")]
    private bool isFacingRight = true; // 角色朝向
    private Vector2 currentVelocity;   // 当前速度缓存，减少重复获取
    private float horizontalInput;     // 本帧水平输入缓存（Update采集，FixedUpdate/动画共用）
    private float verticalInput;       // 本帧垂直输入缓存

    // 动画参数哈希值
    private static readonly int AnimWalk = Animator.StringToHash("walk");
    private static readonly int AnimBack = Animator.StringToHash("back");
    private static readonly int AnimForward = Animator.StringToHash("forward");
    private static readonly int AnimIdle = Animator.StringToHash("idle");
    private static readonly int AnimBackIdle = Animator.StringToHash("backidle");
    private static readonly int AnimForwardIdle = Animator.StringToHash("forwardidle");

    private void Start()
    {
        // 玩家的跨场景保留由 PlayerManager 统一负责，此处不再调用 DontDestroyOnLoad
        // 组件由 prefab 保证存在（见 RequireComponent），仅做获取
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();

        // 刚体基础设置
        rb.gravityScale = 0; // 2D顶视角
        rb.freezeRotation = true;
    }

    private void Update()
    {
        GetInput();       // 采集输入（仅此一处读取输入轴）
        UpdateAnimation();// 更新动画
        FlipController(); // 角色翻转
    }

    private void FixedUpdate()
    {
        // 物理速度统一在FixedUpdate中应用
        rb.velocity = currentVelocity;
    }

    /// <summary>
    /// 输入采集：全脚本唯一的 GetAxisRaw 读取点，结果缓存供动画/物理复用
    /// </summary>
    private void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal"); // A/D ←→
        verticalInput = Input.GetAxisRaw("Vertical");     // W/S ↑↓

        // 计算目标速度
        currentVelocity = new Vector2(horizontalInput * moveSpeed, verticalInput * moveSpeed);
    }

    /// <summary>
    /// 动画状态判定（优先级：静止 > 水平 > 垂直），
    /// 保证任意输入组合下至少一个状态为true，修复斜向移动时动画卡在上一状态的问题。
    /// TODO：未来可在编辑器内把6个Bool参数合并为单一int状态参数，进一步简化状态机。
    /// </summary>
    private void UpdateAnimation()
    {
        bool isIdle = Mathf.Approximately(horizontalInput, 0f) && Mathf.Approximately(verticalInput, 0f);

        if (isIdle)
        {
            SetAnimState(false, false, false, true);
        }
        else if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            // 水平移动优先（与翻转逻辑一致），斜向时也播放走路
            SetAnimState(true, false, false, false);
        }
        else if (verticalInput > 0.1f)
        {
            SetAnimState(false, true, false, false); // 仅向上：back
        }
        else
        {
            SetAnimState(false, false, true, false); // 仅向下：forward
        }
    }

    /// <summary>
    /// 一次性写入六个动画状态参数，保证互斥
    /// </summary>
    private void SetAnimState(bool walk, bool back, bool forward, bool idle)
    {
        anim.SetBool(AnimWalk, walk);
        anim.SetBool(AnimBack, back);
        anim.SetBool(AnimForward, forward);
        anim.SetBool(AnimIdle, idle);
        anim.SetBool(AnimBackIdle, idle);  // 原逻辑：backidle与idle同步
        anim.SetBool(AnimForwardIdle, idle); // 原逻辑：forwardidle与idle同步
    }

    /// <summary>
    /// 简化角色翻转逻辑
    /// </summary>
    private void FlipController()
    {
        // 仅当水平输入变化且朝向不符时翻转
        if (currentVelocity.x > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (currentVelocity.x < 0 && isFacingRight)
        {
            Flip();
        }
    }

    /// <summary>
    /// 角色翻转核心方法
    /// </summary>
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        // 更高效的翻转方式
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }
}
