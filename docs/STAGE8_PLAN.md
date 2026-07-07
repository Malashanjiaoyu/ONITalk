# 阶段 8 规划：多服务商接口与配置验证

状态：规划范围已实现并通过真实游戏验收。实施结果见 `STAGE8_REPORT.md`。

## 阶段目标

- 不再把 ONITalk 的远程生成体验绑定到 DeepSeek。
- 以统一的 OpenAI-Compatible 客户端覆盖大多数服务商，避免重复维护多套请求代码。
- 为常用服务提供一键预设，同时保留端点和模型名的完全可编辑能力。
- 增加玩家主动触发的“验证配置”功能，不影响游戏对话和殖民地记忆。

## 8A：统一服务商预设

首批预设：

- DeepSeek；
- OpenAI；
- OpenRouter；
- Google Gemini；
- SiliconFlow（硅基流动）；
- Ollama 本地模型；
- 自定义 OpenAI-Compatible 接口。

每个预设包含显示名称、默认 Endpoint、模型提示、是否需要 API Key 和可选请求头。选择预设后自动填写推荐值，但玩家仍可修改 Endpoint 和模型名。

自定义兼容接口用于覆盖 Kimi、GLM、MiniMax、LM Studio、Together 等遵循 Chat Completions 结构的服务，不为每个平台复制一套客户端。

## 8B：扩展适配

- 阿里云百炼／通义千问：支持不同地域与 Workspace 专属地址。
- Groq：使用其 OpenAI-Compatible 地址，重点验证参数兼容性和低延迟。
- Anthropic Claude：先评估官方 OpenAI 兼容层；若稳定性或 Prompt Caching 需求不满足，再实现原生 Messages API 适配器。

## 8C：验证配置

模组选项页增加“验证配置”按钮：

- 仅在玩家点击时发送一次最小测试请求；
- 显示服务商、模型、成功状态和响应耗时；
- 失败时显示 HTTP 状态、超时、鉴权、模型不存在或响应格式错误等简洁原因；
- 不生成头顶台词，不进入聊天历史，不修改关系或行动记忆；
- API Key 不进入日志、错误详情或预览界面；
- 验证期间不得阻塞游戏主线程，可取消并防止重复点击产生并发请求。

## 8D：兼容性与测试

- 统一处理 `choices[0].message.content`、错误 JSON 和空响应。
- 按服务商裁剪不支持的可选参数，避免 `temperature`、思考模式等差异导致 400。
- Ollama 和其他本地接口允许无真实 API Key，并给出服务未启动的明确提示。
- 模型名不锁死在模组版本中；预设只提供推荐值，允许玩家手动更新。
- 为每种预设增加配置归一化和请求构造测试。
- 至少完成一个云端接口、一个聚合接口和一个本地接口的真实游戏验证。

## 交付成果

- 统一服务商配置结构和请求适配层；
- 主菜单模组选项中的服务商预设与验证按钮；
- DeepSeek、OpenAI、OpenRouter、Gemini、SiliconFlow、Ollama、自定义、百炼、Groq 和 Claude 支持；
- 不含任何真实 Key 的配置示例与用户文档；
- 自动检查、构建结果和游戏内验收记录；
- 对应版本的 README、CHANGELOG 和 Steam/GitHub 可复用更新说明。

## 实施顺序

1. 先重构统一配置和预设，不改变现有 DeepSeek 行为。
2. 接入首批六个预设与自定义接口。
3. 完成共用“验证配置”按钮。
4. 加入百炼、Groq 和 Claude 差异处理。
5. 逐个平台测试后再部署公开版本。
