using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace InstrumentAnimation
{
    /// <summary>溶出实验主控：溶杯/加水/投片/加热/运行与取样、曲线数据。</summary>
    public class DissolutionExperiment : MonoBehaviour
    {
        public static DissolutionExperiment Instance { get; private set; }

        public WaterLevelController[] waterLevels = new WaterLevelController[4];
        public WaterInteractionUI waterUI;
        public float minutesPerSecond = 1f; // 1 现实秒 = 1 模拟分钟

        public Transform[] cupTransforms = new Transform[4];
        public Material dissolveMaterial;
        public PrintPaperUI printPaper;
        [System.NonSerialized] public TabletDissolver[] tabletVisuals = new TabletDissolver[4];

        [System.NonSerialized] public Cup[] cups = new Cup[4];

        public bool IsRunning { get; private set; }
        public float RunTimeMinutes { get; private set; }
        public float SampleElapsedMinutes { get; private set; }
        public float CountdownSeconds { get; private set; }
        public bool AllSamplesDone { get; private set; }
        public int CurrentSpeedAB { get; private set; }
        public int CurrentSpeedCD { get; private set; }
        public float heatingProgress { get; private set; }

        private Coroutine _heating;
        private Coroutine _run;
        private DissolutionTesterController _tester;
        private AudioSource _audio;
        private AudioClip _beepClip;

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < 4; i++)
            {
                if (cups[i] == null) cups[i] = new Cup(i);
                if (cups[i].sampleTimes == null || cups[i].sampleTimes.Length != 10)
                    cups[i].sampleTimes = new float[10];
            }
            var cfg = ExperimentConfig.Instance;
            if (cfg != null)
            {
                CurrentSpeedAB = cfg.initialSpeedAB;
                CurrentSpeedCD = cfg.initialSpeedCD;
            }
        }

        // ---------------- 溶杯交互 ----------------
        public void OnCupClicked(int cupIndex)
        {
            if (IsParameterPanelOpen()) return;
            if (waterUI != null) waterUI.Show(cupIndex, cups[cupIndex].hasWater);
        }

        public void TryPlaceTablet(int cupIndex)
        {
            var inv = TabletInventory.Instance;
            if (inv == null || !inv.IsHolding) { Toast.Display("请先选择药片"); return; }
            var cup = cups[cupIndex];
            if (!cup.hasWater) { Toast.Display("请先向溶杯 " + cup.cupName + " 加水"); return; }
            if (cup.HasTablet) { Toast.Display("溶杯 " + cup.cupName + " 已有药片"); return; }
            var tablet = inv.Take();
            cup.tablet = tablet;
            CreateTabletVisual(cupIndex);
            Toast.Display("已向溶杯 " + cup.cupName + " 投入 " + tablet.tabletName);
        }

        public void AddWater(int cupIndex)
        {
            var cup = cups[cupIndex];
            if (cup.hasWater) { Toast.Display("杯中有水，请先倒水"); return; }
            cup.hasWater = true;
            cup.currentTemperature = 26f;
            if (waterLevels[cupIndex] != null) waterLevels[cupIndex].Fill();
            Toast.Display("已向溶杯 " + cup.cupName + " 加水");
        }

        public void PourWater(int cupIndex)
        {
            var cup = cups[cupIndex];
            if (!cup.hasWater) { Toast.Display("杯中无水"); return; }
            cup.hasWater = false;
            cup.tablet = null;
            RemoveTabletVisual(cupIndex);
            if (waterLevels[cupIndex] != null) waterLevels[cupIndex].Pour();
            Toast.Display("已倒空溶杯 " + cup.cupName);
        }

        public void PickTablet(TabletDefinition tablet)
        {
            if (IsParameterPanelOpen()) return;
            var inv = TabletInventory.Instance;
            if (inv == null) { TabletInventory.EnsureExists(); inv = TabletInventory.Instance; }
            inv.Pick(tablet);
            Toast.Display("已添加药片：" + tablet.tabletName);
        }

        // ---------------- 打印纸条 ----------------
        public void PrintReport()
        {
            SyncSampleTimesFromConfig();
            var cfg = ExperimentConfig.Instance;
            var sb = new StringBuilder();
            sb.AppendLine("溶出度测试报告");
            sb.AppendLine("------------------------------");
            if (cfg != null)
            {
                sb.AppendLine("目标温度：" + cfg.targetTemperature.ToString("F1") + " ℃");
                sb.AppendLine("AB杯转速：初始 " + cfg.initialSpeedAB + " / 末端 " + cfg.endSpeedAB + " rpm");
                sb.AppendLine("CD杯转速：初始 " + cfg.initialSpeedCD + " / 末端 " + cfg.endSpeedCD + " rpm");
                sb.AppendLine("取样位置：AB " + cfg.samplePositionAB + " mL / CD " + cfg.samplePositionCD + " mL");
            }
            var schedule = GetMasterSchedule();
            sb.AppendLine("取样时间：" + (schedule.Count > 0 ? string.Join(", ", schedule.ConvertAll(x => x.ToString("0.#"))) + " min" : "未设置"));
            sb.AppendLine("------------------------------");
            for (int i = 0; i < 4; i++)
            {
                var cup = cups[i];
                string tablet = cup.HasTablet ? cup.tablet.tabletName : "无";
                float pct = cup.dissolutionProgress * 100f;
                sb.AppendLine(cup.cupName + "杯：" + tablet + "，最终溶出 " + pct.ToString("F1") + "%");
            }
            sb.AppendLine("------------------------------");
            sb.AppendLine("打印时间：" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            if (printPaper != null) printPaper.Show("溶出度测试报告", sb.ToString());
            else Toast.Display("打印完成");
        }

        public static bool IsParameterPanelOpen()
        {
            var root = GameObject.Find("ParameterPanelRoot");
            return root != null && root.activeInHierarchy;
        }

        public static bool IsWaterUIOpen()
        {
            return WaterInteractionUI.Instance != null && WaterInteractionUI.Instance.IsOpen;
        }

        // ---------------- 加热 / 降温 ----------------
        public void StartHeating()
        {
            var cfg = ExperimentConfig.Instance;
            if (cfg == null) return;
            if (_heating != null) StopCoroutine(_heating);
            _heating = StartCoroutine(HeatTo(cfg.targetTemperature));
        }

        public void StopHeating()
        {
            if (_heating != null) { StopCoroutine(_heating); _heating = null; }
        }

        IEnumerator HeatTo(float target)
        {
            var cfg = ExperimentConfig.Instance;
            float start = cfg.currentTemperature;
            float delta = Mathf.Abs(target - start);
            if (delta < 0.01f) { heatingProgress = 1f; _heating = null; yield break; }
            float duration = Mathf.Max(1f, delta / 2f);
            float t = 0f;
            heatingProgress = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                cfg.currentTemperature = Mathf.Lerp(start, target, p);
                heatingProgress = p;
                yield return null;
            }
            cfg.currentTemperature = target;
            heatingProgress = 1f;
            _heating = null;
            ParameterValidator.ValidateTemperature(target);
            Toast.Display("温度已调整到 " + target.ToString("F1") + "℃");
        }

        // ---------------- 运行 / 停止 ----------------
        public void StartRun()
        {
            if (IsRunning) return;
            _tester = FindObjectOfType<DissolutionTesterController>();
            if (_tester != null && !_tester.IsHeadDown())
            {
                Toast.Display("请将机头下降至正确位置");
                return;
            }
            if (!ValidateRunParameters()) return;
            IsRunning = true;
            if (_tester != null)
            {
                _tester.speedAB = CurrentSpeedAB;
                _tester.speedCD = CurrentSpeedCD;
                _tester.StartPaddles();
            }
            // 开始运行后才让杯内药片消融
            if (tabletVisuals != null)
                for (int i = 0; i < tabletVisuals.Length; i++)
                    if (tabletVisuals[i] != null) tabletVisuals[i].StartDissolve();
            _run = StartCoroutine(RunRoutine());
        }

        bool ValidateRunParameters()
        {
            var cfg = ExperimentConfig.Instance;
            if (cfg == null) return true;
            float[][] times = { cfg.sampleTimesA, cfg.sampleTimesB, cfg.sampleTimesC, cfg.sampleTimesD };
            for (int i = 0; i < 4; i++)
                if (!ParameterValidator.ValidateSampleTimes(times[i])) return false;
            ParameterValidator.ValidateTemperature(cfg.currentTemperature);
            return true;
        }

        public void StopRun()
        {
            if (!IsRunning) return;
            if (!AllSamplesDone)
            {
                Toast.Display("取样点未全部完成，无法停止");
                return;
            }
            IsRunning = false;
            if (_run != null) { StopCoroutine(_run); _run = null; }
            if (_tester != null) _tester.StopPaddles();
        }

        IEnumerator RunRoutine()
        {
            SyncSampleTimesFromConfig();
            var schedule = GetMasterSchedule();
            RunTimeMinutes = 0f;
            SampleElapsedMinutes = 0f;
            CountdownSeconds = 0f;
            AllSamplesDone = schedule.Count == 0;

            int nextIndex = 0;
            bool countingDown = false;
            float countdown = 0f;
            int lastBeepSecond = -1;
            float lastCurveMinute = -1f;

            if (CurveScreenController.Instance != null) CurveScreenController.Instance.ClearCurve();

            while (IsRunning)
            {
                float dt = Time.deltaTime;
                RunTimeMinutes += dt * minutesPerSecond;
                UpdateCurrentSpeeds(schedule, RunTimeMinutes);
                if (_tester != null)
                {
                    _tester.speedAB = CurrentSpeedAB;
                    _tester.speedCD = CurrentSpeedCD;
                }

                if (!countingDown && nextIndex < schedule.Count && RunTimeMinutes >= schedule[nextIndex])
                {
                    countingDown = true;
                    countdown = 5f;
                    CountdownSeconds = 5f;
                    lastBeepSecond = 5;
                    Beep();
                }

                if (countingDown)
                {
                    countdown -= dt;
                    CountdownSeconds = Mathf.Max(0f, countdown);
                    int sec = Mathf.CeilToInt(countdown);
                    if (sec != lastBeepSecond && sec > 0) { lastBeepSecond = sec; Beep(); }
                    if (countdown <= 0f)
                    {
                        countingDown = false;
                        CountdownSeconds = 0f;
                        nextIndex++;
                        SampleElapsedMinutes = nextIndex > 0 ? RunTimeMinutes - schedule[0] : 0f;
                        if (nextIndex >= schedule.Count) AllSamplesDone = true;
                    }
                }

                float whole = Mathf.Floor(RunTimeMinutes);
                if (whole != lastCurveMinute)
                {
                    lastCurveMinute = whole;
                    FeedCurve(RunTimeMinutes);
                }

                yield return null;
            }

            FeedCurve(RunTimeMinutes);
        }

        void SyncSampleTimesFromConfig()
        {
            var cfg = ExperimentConfig.Instance;
            if (cfg == null) return;
            float[][] times = { cfg.sampleTimesA, cfg.sampleTimesB, cfg.sampleTimesC, cfg.sampleTimesD };
            for (int i = 0; i < 4; i++)
            {
                if (cups[i] == null) cups[i] = new Cup(i);
                if (cups[i].sampleTimes == null || cups[i].sampleTimes.Length != 10)
                    cups[i].sampleTimes = new float[10];
                var src = times[i];
                if (src == null) continue;
                int n = Mathf.Min(10, src.Length);
                for (int j = 0; j < n; j++)
                    cups[i].sampleTimes[j] = src[j];
            }
        }

        List<float> GetMasterSchedule()
        {
            for (int i = 0; i < 4; i++)
            {
                var list = cups[i].GetValidSampleTimes();
                if (list.Count > 0)
                {
                    list.Sort();
                    return list;
                }
            }
            return new List<float>();
        }

        void UpdateCurrentSpeeds(List<float> schedule, float t)
        {
            var cfg = ExperimentConfig.Instance;
            if (cfg == null) return;
            if (schedule.Count == 0)
            {
                CurrentSpeedAB = cfg.initialSpeedAB;
                CurrentSpeedCD = cfg.initialSpeedCD;
                return;
            }
            float first = schedule[0];
            float last = schedule[schedule.Count - 1];
            float p = t <= first ? 0f : (t >= last ? 1f : (t - first) / Mathf.Max(0.001f, last - first));
            CurrentSpeedAB = Mathf.RoundToInt(Mathf.Lerp(cfg.initialSpeedAB, cfg.endSpeedAB, p));
            CurrentSpeedCD = Mathf.RoundToInt(Mathf.Lerp(cfg.initialSpeedCD, cfg.endSpeedCD, p));
        }

        void FeedCurve(float t)
        {
            var curve = CurveScreenController.Instance;
            if (curve == null) return;
            for (int i = 0; i < 4; i++)
            {
                var cup = cups[i];
                if (!cup.HasTablet) continue;
                int rpm = i < 2 ? CurrentSpeedAB : CurrentSpeedCD;
                float q = DissolutionModel.PercentAt(cup, t, rpm);
                cup.dissolutionProgress = q / 100f;
                curve.AddCupPoint(i, t, q);
            }
        }

        void CreateTabletVisual(int cupIndex)
        {
            RemoveTabletVisual(cupIndex);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "TabletInCup_" + cupIndex;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var cupT = cupTransforms != null && cupIndex < cupTransforms.Length ? cupTransforms[cupIndex] : null;
            if (cupT != null)
            {
                var cr = cupT.GetComponent<Renderer>();
                if (cr != null)
                {
                    var b = cr.bounds;
                    go.transform.position = new Vector3(b.center.x, b.min.y + 0.7f, b.center.z);
                }
                else go.transform.position = cupT.position;
            }
            else go.transform.position = transform.position + Vector3.down;

            go.transform.localScale = Vector3.one * 0.45f;
            var rend = go.GetComponent<Renderer>();
            if (dissolveMaterial != null) rend.sharedMaterial = dissolveMaterial;

            var dc = go.AddComponent<DissolveController>();
            go.AddComponent<DissolveBubbleParticles>();

            // 用纯色贴图保留药片本身颜色（消融材质的主色来自 _MainTex）
            var tablet = cups[cupIndex].tablet;
            var color = tablet != null ? tablet.color : Color.white;
            var inst = rend.material;
            if (inst.HasProperty("_MainTex"))
                inst.SetTexture("_MainTex", MakeSolidTexture(color));

            var td = go.AddComponent<TabletDissolver>();
            td.cupIndex = cupIndex;
            td.dissolve = dc;
            tabletVisuals[cupIndex] = td;
        }

        static Texture2D MakeSolidTexture(Color c)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            var c32 = (Color32)c;
            for (int i = 0; i < px.Length; i++) px[i] = c32;
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        void RemoveTabletVisual(int cupIndex)
        {
            var t = tabletVisuals != null && cupIndex < tabletVisuals.Length ? tabletVisuals[cupIndex] : null;
            if (t != null) Destroy(t.gameObject);
            if (tabletVisuals != null) tabletVisuals[cupIndex] = null;
        }

        void Beep()
        {
            if (_audio == null)
            {
                _audio = gameObject.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
            }
            if (_beepClip == null)
            {
                int rate = 44100;
                int len = rate / 12;
                _beepClip = AudioClip.Create("beep", len, 1, rate, false);
                var data = new float[len];
                for (int i = 0; i < len; i++)
                    data[i] = Mathf.Sin(2f * Mathf.PI * 880f * i / rate) * 0.25f;
                _beepClip.SetData(data, 0);
            }
            _audio.PlayOneShot(_beepClip);
        }
    }
}
