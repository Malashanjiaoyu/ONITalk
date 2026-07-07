# 阶段 2：DeepSeek 与模组选项

日期：2026-06-28  
版本：0.2.0

## 已交付

- 在 ONI 主菜单模组列表注册 ONITalk“选项”按钮。
- 内置 PLib Core/UI/Options 4.25.0.0，不依赖用户安装其他模组。
- DeepSeek 开关、完整接口地址、模型、API Key、语言和频率参数表单。
- API Key 输入遮挡；日志只记录是否已配置，不记录凭据或请求正文。
- 旧 `mods\ONITalk.json` 自动迁移到共享配置目录。
- 默认模型 `deepseek-v4-flash`，请求使用非思考模式。
- 远程请求单并发、模组重载取消、超时和失败自动回退离线台词。
- 版本与第三方许可文件进入构建产物和本地安装目录。

## 验证结果

- 核心检查：14/14 通过。
- Release 构建：0 警告，0 错误。
- 本地安装：逐文件 SHA-256 校验通过。
- 游戏非安全模式加载：ONITalk 0.2.0 初始化成功。
- 日志确认：`ONITalkConfig` 已注册为 ONITalk 的 PLib 模组选项。
- 配置迁移与新路径读取成功；日志未出现 ONITalk/Harmony 异常。

## 配置路径

```text
%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\config\ONITalk\ONITalk.json
```

## 下一项验证

在主菜单点开“模组”并点击 ONITalk“选项”，保存一次非敏感设置；随后检查 JSON 与日志，最后由用户在本机填写 DeepSeek API Key 做一次真实生成测试。
