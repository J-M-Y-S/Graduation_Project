using UnityEngine;

public class DissolveController : MonoBehaviour
{
    public float dissolveDuration = 3f;
    public KeyCode triggerKey = KeyCode.Space;
    public KeyCode resetKey = KeyCode.R;

    const string ClipRate = "_ClipRate";

    Renderer rend;
    Material mat;
    float progress;
    bool dissolving;

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
        if (Input.GetKeyDown(resetKey)) ResetDissolve();
        if (Input.GetKeyDown(triggerKey)) dissolving = true;
        if (!dissolving) return;

        progress += Time.deltaTime / dissolveDuration;
        mat.SetFloat(ClipRate, Mathf.Clamp01(progress));

        if (progress >= 1f)
        {
            dissolving = false;
            rend.enabled = false;
        }
    }

    void ResetDissolve()
    {
        dissolving = false;
        progress = 0f;
        mat.SetFloat(ClipRate, 0f);
        rend.enabled = true;
    }
}
