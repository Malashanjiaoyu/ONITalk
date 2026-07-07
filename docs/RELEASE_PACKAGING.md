# Release 打包

运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-release.ps1
```

脚本会先执行完整 Release 构建和自动检查，然后在 `artifacts/releases` 生成：

- `ONITalk-v<版本>.zip`
- `ONITalk-v<版本>.sha256`

ZIP 内固定使用 `ONITalk/` 根目录，可直接解压到《缺氧》的 `mods/Local`。打包只接受明确白名单中的 DLL、清单、翻译和 PLib 许可文件；若文本文件出现非空 API Key 或常见 `sk-` 密钥格式，打包立即失败。

该脚本不会打包玩家配置、殖民地记忆、导出记录、日志或真实 API Key。发布包包含 ONITalk、PLib 的许可证与第三方说明；公开发布前仍需在干净用户目录进行一次安装测试。
