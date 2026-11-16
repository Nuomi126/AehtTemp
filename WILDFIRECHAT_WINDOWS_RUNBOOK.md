# Wildfire IM：本机 Windows 运维步骤

项目目录：`D:\BMS-Communication-Lab\wildfirechat-run`。在 Windows PowerShell 5.1 或 PowerShell 7 中进入该目录运行以下命令。Docker Desktop 需已启动。以下操作仅针对 `wildfirechat-im` 容器；不会清空 H2 卷。`-ExecutionPolicy RemoteSigned` 仅作用于这次启动的 PowerShell 子进程，不会永久更改系统执行策略。

## 启动与验证

```powershell
Set-Location 'D:\BMS-Communication-Lab\wildfirechat-run'
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\wildfirechat-windows.ps1 -Action Start
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\wildfirechat-windows.ps1 -Action Verify
```

`Verify` 检查 `/api/version`、本镜像实际提供的 `GET /admin/health`、签名后的 `POST /admin/user/all`（H2 读取），以及 TCP `127.0.0.1:18883`。四项均通过时输出 `VERIFY PASS`。

本地 `1.4.9` 镜像中的管理路由为 `GET /admin/health`。`/admin/healthcheck` 和 `/admin/customer` 未在本地 JAR 中注册；请求不存在的 `/admin/unknown` 也返回同样的空 HTTP 500。本地 JAR 的未知路由处理直接设置 HTTP 500，因此这几次 500 没有对应的服务端异常堆栈。请勿将这两个路径作为本镜像的健康判断标准。

## H2 备份与通信测试

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\wildfirechat-windows.ps1 -Action Backup
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\wildfirechat-windows.ps1 -Action Communication
```

`Backup` 会平稳停止此容器，从 `wildfirechat-h2db` 卷复制 `wfchat.mv.db` 到项目中的 `h2db_volume_backup_时间戳` 目录，再启动同一容器。输出备份路径及 SHA-256。`Communication` 会自行先做同样的备份，因此单独执行 `Backup` 只用于额外保存一个恢复点。

`Communication` 会创建两个 `wfctest_时间戳_a/b` 用户，发送一条单聊文本，再按 `messageUid` 读取并核对发送者、目标和正文。测试数据会留在 H2 中，便于复查；执行前不要对已有同名用户做清理。它验证服务端发送与持久化读取，不代替真实客户端登录和消息接收测试。

## 关闭与再次启动

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\wildfirechat-windows.ps1 -Action Stop
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\wildfirechat-windows.ps1 -Action Start
```

这些命令只停启 `wildfirechat-im`，不会删除容器、镜像或 H2 卷。需要查看服务端日志时，可用：

```powershell
docker logs --tail 100 wildfirechat-im
Get-Content .\logs\wildfirechat_warn.log -Tail 100
Get-Content .\logs\wildfirechat_error.log -Tail 100
```

## 2026-10-27 排查与修改记录

- `config\log4j2.xml` 原文件不合法；先将损坏版本另存为 `log4j2.xml.pre_restore_20261027_004534`，再从有效的 `log4j2.xml.bak_20261026` 原样恢复。恢复后 XML 校验通过，SHA-256 为 `395521C8433BFC4D8B17A831F7ECBE778B2F57679071D2DC397EB4F773564ED3`。
- 重启同一个容器后，启动日志显示 H2 数据库迁移版本 59 已校验，且无需迁移；没有新的 H2 锁异常。
- 在消息写入测试前，平稳停止容器并复制 H2 卷到 `h2db_volume_backup_20261027_005506`；`wfchat.mv.db` 的 SHA-256 为 `9D8F045E1DA128A3F9A97B5932268CC066AEAAA36AFA89654FD3A72F14740F31`。
- `POST /admin/user/all` 返回 HTTP 200、业务码 0；`GET /admin/health` 返回 HTTP 200、业务码 0；TCP 18883 可连接。
- 创建 `wfctest_20261027005642_a/b`，发送并读回 `messageUid=583632798498193409`，字段一致。
- 发现旧容器将 18081、28080、18883、18083、18084 均绑定到 `0.0.0.0` 和 `[::]`。按用户选择，先保存旧容器配置到 `wildfirechat-im.inspect.backup_20261027_010103.json`，再备份最新 H2 到 `h2db_volume_backup_20261027_010105`，将旧容器改名为 `wildfirechat-im-prelocal_20261027_010243` 并停用。新容器沿用原镜像、配置目录、媒体目录、日志目录和 `wildfirechat-h2db` 卷，五个端口均只绑定 `127.0.0.1`。
- 切换后 `Verify` 再次通过。`Communication` 自动生成 `h2db_volume_backup_20261027_010358`，随后创建 `wfctest_20261027010448_a/b`，发送并读回 `messageUid=583633815503667201`；错误日志没有新增记录。
- 旧容器仍可用于查看原配置，但不要与新容器同时启动，因为两者指向同一个 H2 卷。仅本机绑定意味着局域网内其他设备无法直接连接这些端口。
- 2026-10-27 修正脚本以兼容 Windows PowerShell 5.1，并在上述命令中为单次进程指定 `RemoteSigned`；此前直接运行 `.ps1` 会被本机默认的 `Restricted` 执行策略拦截。已使用 Windows PowerShell 5.1 实测 `Start`、`Verify` 和 `Communication` 均通过。最后一次通信测试先备份 H2 至 `h2db_volume_backup_20261027_102739`，随后由 `wfctest_20261027102938_a` 向 `wfctest_20261027102938_b` 发送并读回 `messageUid=583704891755626497`；容器仍在运行，错误日志无新增。
