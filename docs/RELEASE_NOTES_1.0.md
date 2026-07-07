# ONITalk 1.0.0 发布说明（草案）

ONITalk 为《缺氧》加入上下文感知的复制人对话：复制人会围绕当前工作、环境、近期共同经历和殖民地事件说话，并通过可拖动的半透明聊天窗口保留本次游戏历史。

## 主要功能

- 状态、环境与闲聊触发，过滤睡觉等不合理场景；
- 最多三句的受控双向连续对话；
- 按殖民地隔离的关系、行动与重大事件记忆；
- 本地相关性检索和 Token 预算，不额外调用模型；
- DeepSeek、OpenAI、OpenRouter、Gemini、SiliconFlow、Ollama、百炼、Groq、Claude 兼容层及自定义接口；
- 断网或接口错误自动离线回退，并限制重复失败请求；
- 默认／自定义提示词，核心事实与输出规则保持保护；
- 英语、简体中文和西班牙语；
- 支持基础游戏与 Spaced Out!。

## 安装与隐私

下载 ZIP 后，将其中的 `ONITalk` 文件夹解压到 `Documents\Klei\OxygenNotIncluded\mods\Local`。详细步骤见 `INSTALL.md`。

使用远程模型会把当前游戏上下文发送到玩家选择的服务商。API Key 明文保存在本机配置，不会打入发布包；详细说明见 `PRIVACY.md`。

## 验证

- 71 项 Core 自动检查；
- 206 个本地化键完整覆盖简中与西语；
- 基础游戏与 Spaced Out! 真实加载；
- 两个真实殖民地记忆隔离；
- schema 4 配置升级、无效 Endpoint 离线回退与退避；
- 确定性 Release ZIP、文件白名单、密钥扫描和 SHA-256。

## 已知限制

- 第三方服务商模型名称和兼容接口可能随时间变化；预设允许手动修改。
- Anthropic 当前使用兼容层，尚未实现原生 Messages API Prompt Caching。
- 台词质量、费用和响应速度取决于玩家选择的模型与服务商。
