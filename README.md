# 溶出仪模型

实验室溶出仪的三维模型展示，用 Unity 制作。

模型按三视图尺寸程序化生成，长宽高 700 × 420 × 520 mm，不使用贴图，外观由基础材质的
颜色、金属度、光滑度表现，兼容内置渲染管线的 Standard 与 URP 的 Lit。

场景运行后物体逐渐消融，表面伴随气泡粒子；按 Space 开始溶解，按 R 重置。

脚本：

- DissolutionTesterModel —— 生成溶出仪模型，菜单 GameObject > 溶出仪 > 创建溶出仪模型
- DissolveController —— 推进溶解进度，控制开始与重置
- DissolveBubbleParticles —— 溶解时在表面产生气泡粒子
- AssignMaterialToChildren —— 编辑器工具，批量给子物体赋同一材质

场景文件在 Assets/Scenes/SampleScene.unity。
