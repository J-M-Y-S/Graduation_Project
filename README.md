# 溶出仪模型

用 Unity 做的溶出仪三维模型。

模型是按三视图尺寸用代码生成的，长宽高 700 × 420 × 520 mm。没用贴图，外观靠基础材质的颜色、金属度、光滑度表现。

跑起来之后按空格，模型会慢慢溶解，表面冒气泡；按 R 恢复原样。

脚本：

- DissolutionTesterModel —— 生成模型，菜单 GameObject > 溶出仪 > 创建溶出仪模型
- DissolveController —— 控制溶解进度
- DissolveBubbleParticles —— 溶解时冒气泡
- AssignMaterialToChildren —— 编辑器工具，批量给子物体换材质

场景在 Assets/Scenes/SampleScene.unity。
