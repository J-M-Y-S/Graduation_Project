using UnityEngine;

public class DissolveBubbleParticles : MonoBehaviour
{
    public float bubblesPerDissolve = 320f;
    public Color bubbleColor = new Color(0.62f, 0.85f, 1f);
    public float bubbleSizeMin = 0.04f;
    public float bubbleSizeMax = 0.16f;
    public float bubbleLifetime = 1.8f;
    public float bubbleSpeed = 1.2f;
    public float spread = 0.6f;

    const string ClipRate = "_ClipRate";

    Renderer rend;
    Material mat;
    ParticleSystem ps;
    ParticleSystem.EmitParams emit;
    float lastProgress;
    float pending;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material;
        CreateParticles();
    }

    void Start()
    {
        lastProgress = mat.GetFloat(ClipRate);
    }

    void Update()
    {
        float progress = mat.GetFloat(ClipRate);
        float delta = progress - lastProgress;
        lastProgress = progress;

        if (delta < 0f)          // 进度回退，说明被重置了
        {
            pending = 0f;
            ps.Clear();
            return;
        }

        pending += delta * bubblesPerDissolve;
        int count = (int)pending;
        pending -= count;

        for (int i = 0; i < count; i++) EmitBubble();
    }

    void EmitBubble()
    {
        Bounds b = rend.bounds;
        emit.position = new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            Random.Range(b.min.z, b.max.z));

        Vector3 v = Random.insideUnitSphere * spread;
        v.y = Mathf.Abs(v.y) + bubbleSpeed;
        emit.velocity = v;

        emit.startSize = Random.Range(bubbleSizeMin, bubbleSizeMax);
        emit.startLifetime = bubbleLifetime * Random.Range(0.8f, 1.2f);
        emit.startColor = bubbleColor;

        ps.Emit(emit, 1);
    }

    void CreateParticles()
    {
        var go = new GameObject("Bubbles");
        go.transform.SetParent(transform, false);

        ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.startLifetime = bubbleLifetime;
        main.maxParticles = 3000;

        ps.emission.enabled = false;

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        fade.color = new ParticleSystem.MinMaxGradient(new Gradient
        {
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        });

        var pr = go.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"))
        {
            mainTexture = BubbleTexture()
        };

        emit = new ParticleSystem.EmitParams();
    }

    Texture2D BubbleTexture()
    {
        int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);

        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d)));
            }
        }

        tex.Apply();
        return tex;
    }
}
