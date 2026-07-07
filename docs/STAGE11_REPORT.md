# 阶段 11：发布前技术回归

状态：已完成；自动化与集中游戏内验收全部通过。

## 已完成

- 记忆仓库除了按殖民地 GUID 分文件，还会核对文档内部 `colonyId`；不一致时拒绝载入。
- 本机已有两个真实殖民地记忆文件，自检确认 GUID、文件名和 schema 3 文档互相独立。
- 配置引入 schema 4，旧文件会在升级前生成 `.pre-v4.bak`。
- 兼容早期 `useDeepSeek`／`deepSeekEnabled`、旧 `language` 和 Light／Balanced／Rich／Custom 值。
- 网络错误统一离线回退；连续失败使用最长五分钟的指数退避，成功后立即复位。
- `supportedContent` 同时声明 `VANILLA_ID` 和 `EXPANSION1_ID`。
- `scripts/preflight.ps1` 会生成 `artifacts/preflight-report.txt`。

## 自动检查

- Core：71 项。
- 本地化：206 个键，简中和西语全覆盖。
- 真实存档隔离：2 个殖民地文件通过。
- Release：确定性 ZIP、文件白名单、敏感信息扫描和 SHA-256。

## 集中游戏内验收

1. 正常接口下进入本体存档并出现台词。
2. 临时将 Endpoint 改为 `http://127.0.0.1:1/v1/chat/completions`，确认仍出现离线台词且日志记录退避；随后恢复服务商预设。
3. 进入 Spaced Out! 存档，确认窗口、台词和殖民地记忆正常加载。
4. 重启后确认配置文件已升级到 schema 4，同时存在迁移前备份。

## 已完成的游戏内验收

- schema 1 配置成功升级到 schema 4，迁移前备份存在。
- 无效本地 Endpoint 首次失败后立即生成离线台词并记录 30 秒退避；退避期间下一句未再次发起远程请求。
- 0.10.1 修正退避期间误报“服务商配置不完整”的日志文案。
- Spaced Out! 真实加载通过：日志确认 `EXPANSION1_ID loaded: True`、星群 `expansion1::clusters/VanillaSandstoneCluster`，随后 ONITalk 控制器、殖民地记忆、聊天窗口和双人对话全部正常运行。
- 同一 DLC 会话内没有 ONITalk 异常；唯一远程警告来自主动设置的无效 Endpoint，且按预期离线回退。
