# gitcontroll —— 版本管控配置与规范

本文件夹集中存放本仓库的版本管控配置文件与协作约定，使工程根目录只保留
Unity 工程本身（`Assets`、`Packages`、`ProjectSettings`）与项目说明。

## 文件说明

| 文件 | 作用 | 生效方式 |
| --- | --- | --- |
| `.gitignore` | Unity 忽略规则，排除 `Library`、`Temp`、`Logs`、`UserSettings`、`obj` 等自动生成目录以及 IDE 缓存与自动生成的工程文件 | Git 仅识别根目录下的 `.gitignore`，因此实际生效的副本位于 `.git/info/exclude` |
| `.gitattributes` | 换行符与文件类型规则，统一 LF 换行并把 `.fbx`、`.png`、`.dll` 等标记为二进制 | 实际生效的副本位于 `.git/info/attributes` |

`.git/info/` 位于 Git 仓库内部，不会被提交，因此这些配置不会分散在工程根目录中。
**修改上述任一规则时，需同步更新本文件夹的文档版本与 `.git/info/` 下的生效副本。**

## 分支管理

采用 Git Flow 思路组织开发：

| 分支 | 角色 | 说明 |
| --- | --- | --- |
| `main` | 稳定版本 | 随时可演示、可交付；只接受来自 `develop` 的合并 |
| `develop` | 日常开发 | 日常改动直接提交于此，是功能分支的合并目标 |
| `feature/*` | 功能开发 | 新功能、新模块、新场景，从 `develop` 切出，完成并验证后合回 |
| `fix/*` | 缺陷修复 | 从 `develop` 切出，修复完成后合回 |

`develop` 上的内容验证稳定后合并进 `main`，并打版本 tag 作为里程碑。

## 提交信息规范

采用 Conventional Commits 格式，重点说明**为什么改**以及改动带来的影响：

```
<type>(<scope>): <一句话简述>

<详细说明：改动动机、实现方式、影响范围>
```

`type` 取值：

| type | 含义 |
| --- | --- |
| `feat` | 新功能 |
| `fix` | 缺陷修复 |
| `refactor` | 重构（不改变外部行为） |
| `perf` | 性能优化 |
| `docs` | 文档 |
| `assets` | 美术、模型、材质、场景资源 |
| `chore` | 构建、依赖、配置等杂项 |

`scope` 使用模块名，例如 `DissolutionTester`、`Shaders`、`Particles`、`Scene`。

提交信息需能还原当时的决策过程，避免 `update`、`修改`、`fix bug` 这类无实质内容的表述。
