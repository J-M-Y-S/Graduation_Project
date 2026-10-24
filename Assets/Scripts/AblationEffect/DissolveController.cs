using System;
using UnityEngine;

/// <summary>
/// 药片消融效果控制器（沿用原有 _ClipRate + 材质透明度效果）。
/// 去掉键盘触发，改为由动画/状态机通过 StartDissolve / SetProgress / ResetDissolve 驱动。
/// </summary>
public class DissolveController : MonoBehaviour
{
    public float dissolveDuration = 3f;

    public event Action OnComplete;

    const string ClipRate = "_ClipRate";

    Renderer rend;
    Material mat;
    float progress;
    bool dissolving;

    public float Progress => Mathf.Clamp01(progress);

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material;
    }

    void Start()
    {
        ResetDissolve();
    }

    void Update()
    {
        if (!dissolving) return;
        progress += Time.deltaTime / dissolveDuration;
        ApplyProgress(progress);
        if (progress >= 1f)
        {
            dissolving = false;
            ApplyProgress(1f);
            OnComplete?.Invoke();
        }
    }

    /// <summary>按时间自动溶解。</summary>
    public void StartDissolve(float duration = -1f)
    {
        if (duration > 0f) dissolveDuration = duration;
        dissolving = true;
    }

    /// <summary>直接设置溶解进度（0~1），用于跟随外部动画/模型。</summary>
    public void SetProgress(float p)
    {
        dissolving = false;
        ApplyProgress(p);
    }

    /// <summary>重置为未溶解。</summary>
    public void ResetDissolve()
    {
        dissolving = false;
        ApplyProgress(0f);
        if (rend != null) rend.enabled = true;
    }

    void ApplyProgress(float p)
    {
        progress = Mathf.Clamp01(p);
        if (mat != null) mat.SetFloat(ClipRate, progress);
        if (rend != null) rend.enabled = progress < 1f;
    }
}
