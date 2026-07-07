# Contributing

感谢参与 ONITalk。提交代码前请先创建 Issue 描述问题或方案，避免多人实现互相冲突的方向。

## 开发环境

- Windows 与 PowerShell 5.1+
- .NET 8 SDK
- 本机安装《缺氧》；完整模组构建需要游戏自带 DLL

```powershell
./scripts/doctor.ps1
./scripts/check-portable.ps1
./scripts/build.ps1
```

GitHub Actions 只运行可移植的 Core、本地化和敏感信息检查。由于游戏程序集是专有文件，仓库和 CI 不分发它们；提交者需要在本机完成 DLL 构建与相关游戏内验证。

## Pull Request 要求

- 一个 PR 聚焦一个问题。
- 新逻辑应补充 Core 检查，或说明为何只能游戏内验证。
- UI 文本必须加入英语模板、简中和西语目录。
- 不提交 API Key、ONITalk.json、存档、记忆、导出或完整 Player.log。
- 修改用户可见行为时同步更新 CHANGELOG.md。

## 第三方代码

`vendor/PLib` 按其 MIT 许可随源码保留。不要引入来源不明或许可不兼容的代码、图片和音频。
