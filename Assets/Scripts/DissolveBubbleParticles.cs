using UnityEngine;

/// <summary>
/// 物体消融时，在表面产生类似气泡的粒子消散效果。
///
/// 通过读取材质上的溶解进度（_ClipRate）来和 DissolveController 保持同步：
/// 进度推进时按比例发射气泡，进度归零时清空残留粒子。
/// 粒子系统在运行时自动创建并配置，无需在场景里手动摆放。
///
/// 用法：把本脚本和 DissolveController 挂到同一个物体上即可。
/// </summary>
public class DissolveBubbleParticles : MonoBehaviour
{
    [Header("Shader 属性")]
    [Tooltip("溶解进度属性名，与 DissolveController 保持一致。")]
    public string dissolveProperty = "_ClipRate";

    [Header("发射量")]
    [Tooltip("整个消融过程发射的气泡总数（按进度均匀分布）。")]
    public float bubblesPerDissolve = 320f;

    [Header("气泡外观")]
    public Color bubbleColor = new Color(0.62f, 0.85f, 1f, 1f);
    [Tooltip("气泡最小尺寸（米）。")]
    public float bubbleSizeMin = 0.04f;
    [Tooltip("气泡最大尺寸（米）。")]
    public float bubbleSizeMax = 0.16f;
    [Tooltip("气泡存活时间（秒）。")]
    public float bubbleLifetime = 1.8f;
    [Tooltip("气泡上浮速度。")]
    public float bubbleSpeed = 1.2f;
    [Tooltip("气泡向四周随机飘散的强度。")]
    public float spread = 0.6f;

    Renderer _renderer;
    Material _material;
    bool _hasProperty;

    ParticleSystem _ps;
    ParticleSystem.EmitParams _emitParams;

    float _prevClip;
    float _emitAccum;

    // 网格表面随机采样缓存（用于把气泡发射到物体表面）
    Vector3[] _verts;
    int[] _tris;
    float[] _triAreas;
    float _totalArea;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _material = _renderer.material;
            _hasProperty = _material != null && _material.HasProperty(dissolveProperty);
        }

        CacheMeshSurface();
        SetupParticleSystem();
    }

    void Start()
    {
        if (_material != null && _hasProperty)
        {
            _prevClip = _material.GetFloat(dissolveProperty);
        }
    }

    void Update()
    {
        if (!_hasProperty) return;

        float clip = _material.GetFloat(dissolveProperty);

        // 进度回退说明被重置了，清空残留气泡
        if (clip < _prevClip)
        {
            _emitAccum = 0f;
            if (_ps != null) _ps.Clear();
        }

        float delta = Mathf.Max(0f, clip - _prevClip);
        _prevClip = clip;

        if (delta <= 0f || clip >= 1f) return;

        _emitAccum += delta * bubblesPerDissolve;
        int count = Mathf.FloorToInt(_emitAccum);
        if (count <= 0) return;
        _emitAccum -= count;

        for (int i = 0; i < count; i++)
        {
            EmitBubble();
        }
    }

    void EmitBubble()
    {
        if (_ps == null) return;

        Vector3 pos = RandomPointOnSurface();
        Vector3 vel = Random.insideUnitSphere * spread;
        vel.y = Mathf.Abs(vel.y) + bubbleSpeed; // 整体向上漂浮

        Color c = bubbleColor;
        c.a = Random.Range(0.5f, 1f);

        _emitParams.position = pos;
        _emitParams.velocity = vel;
        _emitParams.startSize = Random.Range(bubbleSizeMin, bubbleSizeMax);
        _emitParams.startLifetime = bubbleLifetime * Random.Range(0.8f, 1.2f);
        _emitParams.startColor = c;

        _ps.Emit(_emitParams, 1);
    }

    // ------------------------------------------------------------------
    // 粒子系统
    // ------------------------------------------------------------------
    void SetupParticleSystem()
    {
        var go = new GameObject("DissolveBubbles");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        _ps = go.AddComponent<ParticleSystem>();

        var main = _ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = bubbleLifetime;
        main.startSpeed = 0f; // 速度由 Emit 的 velocity 单独控制
        main.startSize = bubbleSizeMin;
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.maxParticles = 5000;
        main.playOnAwake = false;
        main.loop = true;

        // 不由系统按时间自动发射，全部由脚本 Emit
        var emission = _ps.emission;
        emission.enabled = false;

        var shape = _ps.shape;
        shape.enabled = false;

        // 颜色随生命周期淡出
        var colorOverLifetime = _ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.55f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // 大小随生命周期略微增大
        var sizeOverLifetime = _ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        var sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 1.45f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.World;
        renderer.sharedMaterial = CreateBubbleMaterial();

        _emitParams = new ParticleSystem.EmitParams();
    }

    Material CreateBubbleMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");

        if (shader == null)
        {
            Debug.LogWarning("DissolveBubbleParticles: 找不到合适的粒子 shader，将使用默认材质。", this);
            return null;
        }

        var mat = new Material(shader);
        mat.name = "DissolveBubbleMaterial";
        mat.color = Color.white; // 颜色交给粒子 startColor 控制，避免二次叠加
        mat.SetTexture("_MainTex", CreateBubbleTexture());
        return mat;
    }

    // 生成一张柔边的圆形气泡贴图
    Texture2D CreateBubbleTexture()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float radius = size * 0.5f;
        var center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius;
                float alpha = Mathf.Clamp01(1f - d);        // 中心到边缘的柔边
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.78f) * 7f); // 边缘高光
                float a = Mathf.Clamp01(alpha * 0.30f + rim);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return tex;
    }

    // ------------------------------------------------------------------
    // 表面随机采样
    // ------------------------------------------------------------------
    void CacheMeshSurface()
    {
        var filter = GetComponent<MeshFilter>();
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null || !mesh.isReadable || mesh.triangles.Length < 3)
        {
            _verts = null;
            _tris = null;
            _triAreas = null;
            _totalArea = 0f;
            return;
        }

        _verts = mesh.vertices;
        _tris = mesh.triangles;
        _triAreas = new float[_tris.Length / 3];
        _totalArea = 0f;

        for (int t = 0; t < _tris.Length; t += 3)
        {
            Vector3 a = _verts[_tris[t]];
            Vector3 b = _verts[_tris[t + 1]];
            Vector3 c = _verts[_tris[t + 2]];
            float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            _triAreas[t / 3] = area;
            _totalArea += area;
        }
    }

    Vector3 RandomPointOnSurface()
    {
        if (_verts == null || _tris == null || _triAreas == null || _totalArea <= 0f)
        {
            return RandomPointOnBounds();
        }

        // 按面积加权随机选一个三角形
        float r = Random.value * _totalArea;
        int index = 0;
        float acc = 0f;
        for (int i = 0; i < _triAreas.Length; i++)
        {
            acc += _triAreas[i];
            if (acc >= r)
            {
                index = i;
                break;
            }
        }

        int baseIndex = index * 3;
        Vector3 a = _verts[_tris[baseIndex]];
        Vector3 b = _verts[_tris[baseIndex + 1]];
        Vector3 c = _verts[_tris[baseIndex + 2]];

        // 随机重心坐标
        float u = Random.value;
        float v = Random.value;
        if (u + v > 1f)
        {
            u = 1f - u;
            v = 1f - v;
        }

        Vector3 local = a + u * (b - a) + v * (c - a);
        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
        local += normal * Random.Range(0.01f, 0.06f); // 沿法线略微外移

        return transform.TransformPoint(local);
    }

    Vector3 RandomPointOnBounds()
    {
        Bounds b = _renderer != null
            ? _renderer.bounds
            : new Bounds(transform.position, Vector3.one);

        Vector3 min = b.min;
        Vector3 max = b.max;
        int face = Random.Range(0, 6);

        switch (face)
        {
            case 0: return new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), max.z);
            case 1: return new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), min.z);
            case 2: return new Vector3(Random.Range(min.x, max.x), max.y, Random.Range(min.z, max.z));
            case 3: return new Vector3(Random.Range(min.x, max.x), min.y, Random.Range(min.z, max.z));
            case 4: return new Vector3(max.x, Random.Range(min.y, max.y), Random.Range(min.z, max.z));
            default: return new Vector3(min.x, Random.Range(min.y, max.y), Random.Range(min.z, max.z));
        }
    }
}
