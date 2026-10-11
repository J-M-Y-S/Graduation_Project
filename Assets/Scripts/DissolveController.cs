using UnityEngine;

/// <summary>
/// 控制物体消融。按下触发键后，把 DissolveShader 的溶解进度
/// 从 0 匀速推进到 1，物体随之逐渐溶解并最终消失。
///
/// 用法：把本脚本挂到使用 DissolveShader 材质的物体上（例如 Cube），
/// 运行后按 Space 即可开始消融，按 R 可重置让物体重新出现。
/// </summary>
public class DissolveController : MonoBehaviour
{
    [Header("Shader 属性")]
    [Tooltip("控制溶解量的属性名，DissolveShader 里为 _ClipRate。")]
    public string dissolveProperty = "_ClipRate";

    [Header("消融设置")]
    [Tooltip("从开始消融到完全消失的时长（秒）。")]
    public float dissolveDuration = 3f;
    [Tooltip("开始消融的按键。")]
    public KeyCode triggerKey = KeyCode.Space;
    [Tooltip("重置（重新出现）的按键，方便反复测试。")]
    public KeyCode resetKey = KeyCode.R;

    [Header("行为")]
    [Tooltip("消融完成后是否禁用渲染器（彻底从画面中消失）。")]
    public bool disableRendererWhenDone = true;
    [Tooltip("消融完成后是否同时禁用碰撞体。")]
    public bool disableCollidersWhenDone = true;

    /// <summary>当前是否正在消融。</summary>
    public bool IsDissolving { get; private set; }

    /// <summary>消融进度：0 = 完整，1 = 完全消融。</summary>
    public float Progress { get; private set; }

    Renderer _renderer;
    Material _material;
    bool _hasProperty;
    Collider[] _colliders;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            // .material 会生成实例，避免修改共享材质资源
            _material = _renderer.material;
            _hasProperty = _material != null && _material.HasProperty(dissolveProperty);
        }

        _colliders = GetComponentsInChildren<Collider>(true);
    }

    void Start()
    {
        if (_renderer == null)
        {
            Debug.LogWarning("DissolveController: 当前物体没有 Renderer，脚本已停用。", this);
            enabled = false;
            return;
        }

        if (!_hasProperty)
        {
            Debug.LogWarning($"DissolveController: 材质中找不到属性 \"{dissolveProperty}\"，请确认 shader 属性名。", this);
        }

        ResetDissolve();
    }

    void Update()
    {
        if (Input.GetKeyDown(resetKey))
        {
            ResetDissolve();
            return;
        }

        if (Input.GetKeyDown(triggerKey))
        {
            StartDissolve();
        }

        if (!IsDissolving) return;

        Progress = Mathf.MoveTowards(
            Progress,
            1f,
            (1f / Mathf.Max(dissolveDuration, 0.01f)) * Time.deltaTime);

        ApplyDissolve();

        if (Progress >= 1f)
        {
            FinishDissolve();
        }
    }

    /// <summary>开始消融。</summary>
    public void StartDissolve()
    {
        if (!_hasProperty) return;

        // 防止上次消融完成后物体被禁用而无法再次触发
        if (_renderer != null && !_renderer.enabled)
        {
            _renderer.enabled = true;
        }

        IsDissolving = true;
    }

    /// <summary>重置到完整状态，让物体重新出现。</summary>
    public void ResetDissolve()
    {
        IsDissolving = false;
        Progress = 0f;
        ApplyDissolve();

        if (_renderer != null) _renderer.enabled = true;
        if (_colliders != null)
        {
            foreach (Collider col in _colliders)
            {
                if (col != null) col.enabled = true;
            }
        }
    }

    void ApplyDissolve()
    {
        if (_material != null && _hasProperty)
        {
            _material.SetFloat(dissolveProperty, Mathf.Clamp01(Progress));
        }
    }

    void FinishDissolve()
    {
        IsDissolving = false;
        ApplyDissolve();

        if (disableRendererWhenDone && _renderer != null)
        {
            _renderer.enabled = false;
        }

        if (disableCollidersWhenDone && _colliders != null)
        {
            foreach (Collider col in _colliders)
            {
                if (col != null) col.enabled = false;
            }
        }
    }
}
