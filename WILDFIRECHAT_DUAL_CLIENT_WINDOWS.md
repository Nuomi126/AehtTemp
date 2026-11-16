# 野火 IM 本机双客户端运行与验收

工作目录：`D:\BMS-Communication-Lab\wildfirechat-run`。Docker Desktop、MySQL57 服务和 JDK 17 用于启动应用服务。所有服务端口都绑定 `127.0.0.1`。

## 一键操作

在 Windows PowerShell 中运行：

```powershell
Set-Location 'D:\BMS-Communication-Lab\wildfirechat-run'
.\wildfirechat-local.cmd Start
.\wildfirechat-local.cmd Verify
.\wildfirechat-local.cmd Stop
```

`.cmd` 仅为本次 PowerShell 子进程指定脚本执行策略；不会修改系统策略。`Start` 复用现有 `wildfirechat-im` 容器和 H2 卷，如果服务已经运行就直接验证。它用 `D:\JavaProjects\jdk-17\bin\java.exe` 启动应用服务，并分别写入 `app-server\logs\startup-时间戳.stdout.log` 和 `startup-时间戳.stderr.log`。如果启动失败，先查看这两个文件。`Verify` 包括 HTTP 健康检查、Docker 端口仅本机绑定检查，以及两个独立 IM 客户端进程的双向实时通信。`Stop` 只停止应用服务和 `wildfirechat-im`，不停止共享的 MySQL57 服务，不删除数据库、容器、卷或备份。

只重跑双客户端测试可用：

```powershell
.\wildfirechat-local.cmd Duplex
```

## 手动双客户端聊天

打开两个独立的 PowerShell 窗口，均进入 `D:\BMS-Communication-Lab\wildfirechat-run`。窗口 B 运行：

```powershell
.\wildfirechat-local.cmd ChatB
```

记录 B 窗口显示的用户 ID。窗口 A 运行：

```powershell
.\wildfirechat-local.cmd ChatA
```

在 A 窗口输入 `/peer B的用户ID`，再输入一条文本。B 窗口收到这条消息后，直接输入回复文本；A 窗口应立即显示回复。每个窗口用 `/quit` 退出。

A 和 B 分别经 `http://127.0.0.1:8888/login` 登录，随后各自建立连接到 `127.0.0.1:18883` 的 IM 客户端进程。A、B 使用独立的测试手机号和每次新生成的客户端 ID；测试登录码由脚本从本地 `app-server\config\application.properties` 读取，不会输出或保存。客户端通过 IM 服务端的 MQTT `MS` 发送消息，收到 `MN` 实时通知后通过 `MP` 拉取正文。此流程没有使用管理 API 代发消息。

## 本次实测结果（2026-10-27）

- `GET http://127.0.0.1:8888/`：HTTP 200，正文 `Ok`；应用仅监听 `127.0.0.1:8888`。
- `GET http://127.0.0.1:18081/api/version`：HTTP 200；`GET http://127.0.0.1:28080/admin/health`：HTTP 200，业务码 0。Docker 五个映射端口均只绑定 `127.0.0.1`。
- A、B 分别通过应用登录，获得不同用户 ID 和 IM token；两个不同 PID 的客户端均收到成功的 MQTT CONNACK。
- A 发送 `A-to-B-3ab17de020f0`，服务端分配消息 ID `583740596502429697`，B 的 IM 连接实时收到并核对发送者和正文。
- B 回复 `B-to-A-35c2c87b2ced`，服务端分配消息 ID `583740598798811137`，A 的 IM 连接实时收到并核对发送者和正文。
- `.\wildfirechat-local.cmd Verify` 返回退出码 0，输出 `VERIFY PASS`。
- 对完整的 `Stop` → `Start` → `Verify` 流程再次实测均返回退出码 0。重启后的 A→B 消息 ID 为 `583741444018176001`，B→A 回复消息 ID 为 `583741446366986241`；服务最终保持运行。

## 排查与修改记录

1. 先前直接运行 `java` 实际使用 Oracle javapath 的 JDK 23；Windows Application Error 事件记录 `java.exe` 23.0.1 异常 `0xc0000409`，与先前退出码 `-1073740791` 一致。改为显式调用已有 JDK 17 后，Java 异常可正常输出到标准日志。未在项目目录内找到对应的 `hs_err_pid*.log`。
2. 检查现有 `target\app-0.74.jar`，其中包含 Spring Boot 2.2.10、MySQL Connector/J 8.0.28、`sdk-1.4.9.jar` 和 `common-1.4.9.jar`，无需重建。JDK 17 启动时的真实阻塞错误为 `Unknown database 'appdata'`。复用本机已运行的 MySQL57，只新建缺失的 `appdata` 数据库；应用的 Hibernate 自动创建所需表。没有修改其他数据库，也没有重建 IM 的 H2 数据。
3. `app-server\config\im.properties` 在本次接手时已指向 `http://127.0.0.1:28080`，且管理密钥与 IM 配置一致，因此没有改动或披露密钥。LDAP 已禁用；本机测试采用应用支持的测试登录码，无需真实短信服务。
4. 首次成功启动时应用监听所有接口。先备份 `app-server\config\application.properties` 为 `application.properties.bak_20261027_115059`，再增加 `server.address=127.0.0.1`；重启后确认只监听本机。
5. 新增 `wildfire_client.py`、`wildfirechat-local.ps1`、`wildfirechat-local.cmd`。脚本安装后发现 Docker 端口检查使用了宿主机端口作为容器内端口，先备份为 `wildfirechat-local.ps1.bak_时间戳` 再修正，随后 `Start` 和 `Verify` 均通过。手动聊天入口也完成登录与退出检查；为避免 Windows 控制台编码差异，其提示语使用英文。

## 客户端方案说明

本机现有社区版 IM 服务端可供上述小型协议客户端真实连接。官方 Web Chat 的 Web SDK 需要专业版服务端，官方 Android 示例中的内置协议栈也受官方服务限制，因此本次使用项目源码对应的 MQTT 客户端完成本机验收。两个客户端是独立进程和独立 IM 连接；当前交付的是终端聊天界面，没有浏览器聊天页面。测试登录码只适合本机测试，后续正式使用时应关闭该配置并接入正式登录流程。
