# 安装与升级

## Steam 创意工坊

创意工坊版本发布后，订阅 ONITalk 并在《缺氧》的“模组”页面启用即可。首次启用或更新 DLL 后，游戏通常会要求重启。

## 本地安装

1. 关闭《缺氧》。
2. 下载 GitHub Release 中的 `ONITalk-v<版本>.zip`。
3. 将 ZIP 内的 `ONITalk` 文件夹解压到：

```text
Documents\Klei\OxygenNotIncluded\mods\Local\ONITalk
```

如果 Windows 将 Documents 重定向到 OneDrive，请使用实际的 Documents 目录。最终应能看到 `ONITalk\ONITalk.dll`、`mod.yaml` 和 `mod_info.yaml`。

## 配置

在游戏主菜单进入“模组”，找到 ONITalk，点击“选项”。API Key 只保存在本机配置：

```text
Documents\Klei\OxygenNotIncluded\mods\config\ONITalk\ONITalk.json
```

不要公开这个文件。没有 API Key 时可以选择“离线台词”。

## 升级

- 关闭游戏后覆盖旧的 ONITalk 文件夹。
- 不要删除 `mods\config\ONITalk`，除非希望重置配置与长期记忆。
- 旧配置会自动升级；升级前会保留 `.pre-v4.bak`。
- 不同殖民地的记忆按殖民地 GUID 分文件保存。

## 卸载

关闭游戏并删除 `mods\Local\ONITalk`。若还要删除设置和长期记忆，再删除 `mods\config\ONITalk`；此操作不可恢复。
