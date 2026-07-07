# ONITalk v0.10.1 pre-release

这是 ONITalk 的首个公开 GitHub 预发行版，用于在 Steam 创意工坊发布前收集安装与兼容性反馈。

## 功能

- 上下文感知的复制人短对话与最多三句连续回应；
- 半透明、可拖动、可缩放的常驻聊天窗口；
- 按殖民地隔离的关系、行动和重大事件记忆；
- 本地智能记忆检索、预算控制和注入预览；
- DeepSeek、OpenAI、OpenRouter、Gemini、SiliconFlow、Ollama、百炼、Groq、Claude 兼容层及自定义接口；
- 远程错误自动离线回退与指数退避；
- 默认／自定义提示词；
- 英语、简体中文、西班牙语；
- 支持基础游戏和 Spaced Out!。

## 安装

1. 关闭《缺氧》。
2. 下载 `ONITalk-v0.10.1.zip` 并校验可选的 SHA-256 文件。
3. 将 ZIP 内 `ONITalk` 文件夹解压到 `Documents\Klei\OxygenNotIncluded\mods\Local`。
4. 启动游戏，在“模组”页面启用 ONITalk。

详细说明见仓库根目录的 `INSTALL.md`、`PRIVACY.md` 和 `TROUBLESHOOTING.md`。

## 验证

- GitHub Actions 可移植检查通过；
- 71 项 Core 检查通过；
- 206 个本地化键通过简中／西语覆盖检查；
- 本地完整 Release 构建 0 警告、0 错误；
- 基础游戏与 Spaced Out! 真实加载通过；
- 两个真实殖民地的长期记忆隔离通过。

## 隐私提醒

API Key 明文保存在玩家本机配置中。请勿公开 `ONITalk.json`、完整 `Player.log` 或殖民地记忆导出。发布 ZIP 不包含这些文件。
