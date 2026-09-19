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
        GetInput();       // 获取输入
        UpdateAnimation();// 更新动画
        FlipController(); // 角色翻转
    }

    /// <summary>
    /// 简化输入处理：用GetAxis获取平滑输入，替代大量KeyDown/KeyUp判断
    /// </summary>
    private void GetInput()
    {
        // 获取水平/垂直输入，自动处理按键按下/抬起
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D ←→
        float vertical = Input.GetAxisRaw("Vertical");     // W/S ↑↓

        // 计算目标速度
        currentVelocity = new Vector2(horizontal * moveSpeed, vertical * moveSpeed);

        // 应用速度到刚体
        rb.velocity = currentVelocity;
    }

    /// <summary>
    /// 优化动画控制：精准判断输入状态，而非仅靠速度
    /// 修复“无按键仍播放走路动画”的核心问题
    /// </summary>
    private void UpdateAnimation()
    {
        // 提取水平/垂直输入
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 基础状态判断
        bool isMovingHorizontal = Mathf.Abs(horizontal) > 0.1f; // 水平移动
        bool isMovingUp = vertical > 0.1f;                       // 向上移动
        bool isMovingDown = vertical < -0.1f;                    // 向下移动
        bool isIdle = !isMovingHorizontal && !isMovingUp && !isMovingDown; // 完全静止

        // 动画状态赋值
        anim.SetBool(AnimWalk, isMovingHorizontal && !isMovingUp && !isMovingDown); // 仅水平移动时播放走路
        anim.SetBool(AnimBack, isMovingUp && !isMovingHorizontal);                  // 仅向上移动时播放back
        anim.SetBool(AnimForward, isMovingDown && !isMovingHorizontal);             // 仅向下移动时播放forward
        anim.SetBool(AnimIdle, isIdle);                                             // 完全静止时idle
        anim.SetBool(AnimBackIdle, isIdle);                                         // 原逻辑：backidle=idle
        anim.SetBool(AnimForwardIdle, isIdle);                                      // 原逻辑：forwardidle=idle
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

    // 调试用，显示当前速度和朝向
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 20), $"速度：{rb.velocity} 朝向右：{isFacingRight}");
    }
}