# 阶段 12：GitHub 发布工程

状态：已完成；公开仓库、CI 与 v0.10.1 预发行版已上线。

## 已完成

- 使用 `D:\Software\Git` 初始化有效 Git 仓库，默认分支为 `main`。
- 安装 GitHub CLI 2.96.0 到 `D:\Software\GitHubCLI`。
- 扩展 `.gitignore` 与 `.gitattributes`，排除配置、API Key、存档、记忆、日志、构建产物和内部工作状态。
- 清理公开文档中的个人 Windows 用户路径。
- 新增 `scripts/check-secrets.ps1` 与统一的 `scripts/check-portable.ps1`。
- 新增 GitHub Actions、Bug／Feature Issue 表单和 PR 模板。
- 新增安装、隐私、故障排查、安全、贡献、第三方许可和 1.0 发布说明草案。
- Git 暂存模拟确认不会纳入 ONITalk.json、存档、记忆、日志、构建产物或本地 Codex 文件。

## 验证结果

- 可移植检查：通过。
- Core：71 项通过。
- 本地化：206 个键，简中／西语通过。
- 敏感信息与个人路径扫描：通过。
- 完整 Release 构建：0 警告、0 错误。

## 已确认的发布设置

1. Git 提交作者使用 GitHub 账号 `Malashanjiaoyu` 与隐私邮箱。
2. 远程仓库使用公开的 `Malashanjiaoyu/ONITalk`。

- 许可证：MIT。
- 可见性：公开。

## 发布结果

- 仓库：`https://github.com/Malashanjiaoyu/ONITalk`
- 首次提交：`eceda38 Prepare ONITalk for public release`
- GitHub Actions：首次 `portable-checks` 成功。
- 预发行版：`https://github.com/Malashanjiaoyu/ONITalk/releases/tag/v0.10.1`
- 附件：确定性 ZIP 与 SHA-256 校验文件。
