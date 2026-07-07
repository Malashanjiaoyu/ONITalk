# Security Policy

## Supported versions

安全修复优先覆盖最新公开版本。

## Reporting a vulnerability

不要在公开 Issue 中粘贴 API Key、配置文件或包含私人殖民地内容的导出。仓库启用 Private vulnerability reporting 后，请使用 GitHub Security Advisory 私下报告；启用前可联系仓库维护者并只提供最小复现信息。

如果 Key 已经公开，请立即在对应 AI 服务商后台撤销并重新生成。删除 GitHub 内容不能保证旧 Key 未被复制。

## Scope

重点问题包括：

- API Key 被写入日志、导出或发布包；
- 跨殖民地读取错误记忆；
- 不受控的重复付费请求；
- 恶意配置或模型响应导致任意文件访问或代码执行。
