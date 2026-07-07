# 阶段 8：多服务商接口与配置验证

状态：完成并通过真实游戏验证。

## 配置结构

ONITalk 继续使用原有 `provider`、`endpoint`、`model` 和 `apiKey` 字段，旧 DeepSeek 配置无需迁移。游戏选项中由一个自定义面板共同编辑这些字段，因此验证按钮能够读取玩家尚未保存的最新输入。

服务商配置包含默认 Endpoint、推荐模型、是否需要 Key、随机度上限和说明。选择明确预设时总是替换为该服务商的推荐地址，避免把其他平台的 Endpoint 意外带入；自定义模式保留玩家输入。

## 已实现预设

- DeepSeek；
- OpenAI；
- OpenRouter；
- Google Gemini；
- SiliconFlow（硅基流动）；
- Ollama 本地模型；
- 阿里云百炼／通义千问；
- Groq；
- Anthropic Claude 官方 OpenAI 兼容层；
- 自定义 OpenAI-Compatible；
- 离线台词。

## 验证请求

- 发送固定的最小 system/user 消息，输出上限 8 Token。
- 使用独立客户端和 15 秒超时，不占用正式对话会话。
- 成功结果包含响应声明的模型和毫秒耗时。
- 错误结果区分配置缺失、无效 URL、HTTP 状态、服务商错误、超时、非 JSON 和空内容。
- 若服务商错误意外包含当前 Key，展示前再次替换为 `***`。
- UI 在验证进行中忽略重复点击。

## 请求差异

- DeepSeek 继续发送关闭 thinking 的参数。
- Claude 兼容层将 temperature 上限限制为 1。
- Ollama 和自定义接口允许空 Key；其余预设验证前要求 Key。
- 所有接口共同使用非流式 Chat Completions 文本响应，兼容字符串及文本分段 content。

## 自动验证

- Core 检查：58/58 通过。
- Release 构建：0 警告、0 错误。
- 覆盖：服务商 ID 唯一、旧 `openai-compatible` 迁移、预设默认值切换、跨平台旧地址清除、自定义内容保留、Key 要求和温度差异。

## 游戏内验收

1. 主菜单进入“模组 → ONITalk → 选项”，确认统一服务商面板完整显示。
2. 依次选择至少两个预设，确认 Endpoint 和模型立即变化。
3. 选回 DeepSeek，确认现有 Key 仍在遮挡输入框中。
4. 点击“验证配置”，确认显示连接成功、模型和耗时。
5. 保存后进入存档，确认真实对话仍正常生成。
6. 检查 Player.log 中没有 Key、UI 异常、请求异常或记忆错误。

## 实测结果

- 游戏实际加载 `ONITalk 0.8.0.0`，统一服务商配置面板可以正常打开和保存。
- 服务商预设切换、Endpoint／模型联动、Key 遮挡和“验证配置”已完成手动验收。
- 保存后 Provider 为 `deepseek`，Endpoint 为官方 Chat Completions 地址，模型为 `deepseek-v4-flash`，Key 配置状态为已填写。
- Player.log 只记录“Key 已配置”布尔状态，没有输出 Key 内容。
- 本轮没有 ONITalk 的 UI、请求、配置、记忆或 Harmony 警告与错误。
