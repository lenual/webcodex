# A11yChat

A11yChat 是一个面向 Windows ChatGPT Desktop 的无障碍/自动化命令行工具。
D:\WebCodex\A11yChat\dist\A11yChat.exe launch

它通过 **CDP（Chrome DevTools Protocol）连接 ChatGPT Desktop 自身的 Electron renderer**，调用普通 Chat 模式现有的内部提交与读取路径，从而可以在后台：

- 创建新的普通 ChatGPT 对话；
- 向已有普通 Chat 继续发送消息；
- 等待某一轮 assistant 真正完成；
- 后台读取指定 conversation；
- 保持用户当前正在看的 Chat 不切换、不抢焦点。

它**不是** Codex thread 工具，也**不使用** OpenAI Developer API。

---

## 为什么叫 A11yChat？

**a11y** 是 accessibility（无障碍）的常见缩写。

写法来自：

```text
accessibility
a + 11 个中间字母 + y
= a11y
```

这个项目最初就是为了让无法依赖键盘快捷键、鼠标点击、焦点切换或前台 UI automation 的辅助程序，也能操作 ChatGPT Desktop 的普通 Chat，所以叫 **A11yChat**。

---

## 当前能力

```text
A11yChat.exe launch
A11yChat.exe probe
A11yChat.exe new  "message" [--plugin NAME]
A11yChat.exe ask  "message" [--plugin NAME]
A11yChat.exe send <conversation-id> "message" [--plugin NAME]
A11yChat.exe send <conversation-id> "message" --wait [--plugin NAME]
A11yChat.exe read <conversation-id>
A11yChat.exe read <conversation-id> --wait
A11yChat.exe delete <conversation-id>
```

另外支持稳定的纯文本输出：

```text
A11yChat.exe ask "message" --text
A11yChat.exe send <conversation-id> "message" --wait --text
A11yChat.exe read <conversation-id> --text
```

---

## 设计目标

A11yChat 的核心目标是：

- 不模拟键盘；
- 不模拟鼠标；
- 不点击 DOM；
- 不调用 `Page.navigate`；
- 不调用 `Target.activateTarget`；
- 不调用 `Page.bringToFront`；
- 不切换当前 ChatGPT conversation；
- 不改变用户当前焦点；
- 不额外启动浏览器；
- 不走 Codex thread；
- 不依赖 OpenAI Developer API。

已验证的后台操作返回中会包含：

```json
{
}
```

---

# 工作原理

整体路径：

```text
A11yChat.exe
    |
    | CDP / Runtime.evaluate
    v
ChatGPT Desktop Electron renderer
    |
    | React internal scope/store
    |
    +--> m.vet(...)   创建/发送普通 Chat
    |
    +--> m.azt(...)   读取服务端 conversation tree
    |
    v
ChatGPT Desktop 自己的普通 Chat pipeline
```

A11yChat 不自行复制 ChatGPT 的 cookie、session token、integrity token 或请求签名。

发送消息仍然由 **ChatGPT Desktop 自己的 renderer 和内部网络层**完成，因此当前登录状态、认证、integrity/attestation 等请求准备逻辑仍由 Desktop 管理。A11yChat 自身固定提交模型与推理强度。

---

## 普通 Chat，不是 Codex

普通 Chat conversation 在 Desktop 中使用真正的 ChatGPT conversation id，例如：

```text
6aba272b-5a24-83ee-b043-1a5590f671a3
```

对应侧栏类型为普通 Chat history，而不是 Codex app-server thread。

A11yChat 创建的 conversation 会进入正常 ChatGPT 聊天历史。

---

# 要求

当前实现针对：

- Windows x64；
- ChatGPT Desktop；
- Desktop 以 CDP remote debugging 模式启动。

正式发布的 `A11yChat.exe` 是 self-contained 单文件版本，本身不要求安装 PowerShell、Python 或额外 .NET Runtime。

> 注意：项目依赖 ChatGPT Desktop 的内部 renderer 实现。Desktop 更新后内部模块导出名称或数据结构可能发生变化，因此升级 ChatGPT Desktop 后应先运行 `probe` 做兼容性检查。

---

# 启动 ChatGPT Desktop + CDP

最简单：

```powershell
A11yChat.exe launch
```

默认监听：

```text
127.0.0.1:9222
```

成功示例：

```json
{
  "ok": true,
  "state": "ready",
  "port": 9222,
  "cdpReady": true
}
```

如果 CDP 已经开启：

```json
{
  "ok": true,
  "state": "already_ready",
  "port": 9222,
  "cdpReady": true
}
```

## 如果 ChatGPT 已经在运行但没有 CDP

Electron 的 remote debugging 参数通常必须在主进程启动时生效。

因此需要：

1. 完全退出 ChatGPT Desktop；
2. 确保旧主进程已经结束；
3. 再运行：

```powershell
A11yChat.exe launch
```

---

# 命令

## 1. probe

检查 CDP、主 renderer、内部 scope 和模型信息：

```powershell
A11yChat.exe probe
```

示例：

```json
{
  "ok": true,
  "title": "当前聊天标题",
  "href": "app://-/index.html",
  "visibility": "visible",
  "module": "./assets/app-initial-....js",
  "model": "gpt-5-6-thinking"
}
```

升级 ChatGPT Desktop 后建议首先运行此命令。

---

## 2. new

后台创建新的普通 Chat，并发送第一条用户消息：

```powershell
A11yChat.exe new "你好"

# 显式选择插件
A11yChat.exe new "检查项目状态" --plugin WebTunnel
```

它会返回真正的服务端 conversation id：

```json
{
  "ok": true,
  "state": "created",
  "clientConversationId": "local-chatgpt:...",
  "serverConversationId": "6aba...",
  "model": "gpt-5-6-thinking",
}
```

`new` 只负责成功提交并拿到 conversation id，**不等待 assistant 完成**。

如果希望一条命令直接拿最终回复，请使用 `ask`。

---

## 3. ask

后台创建新 Chat，并等待这一轮 assistant 真正完成：

```powershell
A11yChat.exe ask "解释一下量子纠缠"

# 显式选择 WebTunnel
A11yChat.exe ask "读取 README 第一行" --plugin WebTunnel
```

示例：

```json
{
  "ok": true,
  "state": "complete",
  "complete": true,
  "conversationId": "6aba...",
  "text": "量子纠缠是……",
  "messageStatus": "finished_successfully",
  "endTurn": true,
  "isComplete": true,
  "finishType": "stop",
}
```

可指定等待参数：

```powershell
A11yChat.exe ask "问题" --timeout 120 --poll 750
```

---

## 4. send

向已有普通 Chat 后台发送新消息：

```powershell
A11yChat.exe send <conversation-id> "继续解释"

# 本轮显式选择 WebTunnel
A11yChat.exe send <conversation-id> "检查项目状态" --plugin WebTunnel
```

此命令提交后立即返回，不等待 assistant 完成。

发送前，A11yChat 会通过 Desktop 内部 conversation fetch 获取服务端最新 `current_node`，将其作为本次发送的 `parentMessageId`。

因此不会依赖可能已经过期的 renderer cache。

---

## 5. send --wait

向已有普通 Chat 发送消息，并等待**这一轮新回复**完成：

```powershell
A11yChat.exe send <conversation-id> "继续解释" --wait

# 本轮显式选择 WebTunnel，并等待这一轮完成
A11yChat.exe send <conversation-id> "继续检查" --plugin WebTunnel --wait
```

这是已有 conversation 最推荐的同步调用方式。

A11yChat 不会简单地“看到最后一条 assistant 已完成就返回”。

它会：

1. 发送前获取服务端最新 parent node；
2. 提交新消息；
3. 将发送前的 parent 记为 `AfterNodeId`；
4. 重新刷新服务端 conversation tree；
5. 确认新 `current_node` 是该 parent 的后代；
6. 再检查 assistant 完成状态。

这样可以避免把**上一轮旧回复**误认为本次发送的结果。

---

## 6. read

读取指定 conversation 当前的最终节点：

```powershell
A11yChat.exe read <conversation-id>
```

返回可能包括：

```text
pending
complete
failed
deleted
not_found
fetch_error
```

即使目标 conversation 当前没有加载在 renderer cache 中，A11yChat 也可以通过 Desktop 自己的内部 conversation fetch 后台读取。

---

## 7. read --wait

等待指定 conversation 完成：

```powershell
A11yChat.exe read <conversation-id> --wait
```

可指定：

```powershell
A11yChat.exe read <conversation-id> --wait --timeout 120 --poll 750
```

### 固定留在普通 Chat

A11yChat 不再等待 Work handoff 出现后再处理，而是在 `new` / `ask` / `send` 的请求真正发出前，从最终 ordinary Chat request 中移除 Work handoff local functions。

当前 Desktop 的普通 Chat 请求在未过滤时会包含：

```text
local_function_signatures = [
  { name: "handoff", ... }
]
```

A11yChat 在 `startCompletionStream(...)` 的最终 `request` 上仅过滤：

```text
handoff
continue_in_work
```

因此模型在 A11yChat 提交的这一轮里拿不到“转 Work”的本地工具。`new` 和不带 `--wait` 的 `send` 即使 A11yChat 进程已经退出，之后也不会出现需要再次调用 A11yChat 才能处理的 Work handoff。

过滤是**单次提交级别**的：

- 不修改 ChatGPT Desktop 全局设置；
- 不切换当前 UI；
- 不使用 DOM 点击；
- 不需要后台 guardian / 常驻服务；
- 不改变普通 Chat 的 conversation/history；
- 不影响 plugin `system_hints`。

`--plugin WebTunnel` 已实测与该过滤同时工作：最终 request 中 `local_function_signatures=[]`，同时保留 WebTunnel 的 plugin system hint，并真实产生 WebTunnel tool 调用。

`read` / `read --wait` 保持纯读取/等待职责，不负责处理 Work handoff。

---

## 8. delete

删除指定的普通 ChatGPT conversation：

```powershell
A11yChat.exe delete <conversation-id>
```

成功返回：

```json
{
  "ok": true,
  "state": "deleted",
  "conversationId": "..."
}
```

并返回：

```text
exit code = 0
```

删除调用使用 ChatGPT Desktop 自己的普通 Chat 删除路径：

```text
DELETE /conversation/id/{conversation_id}
```

A11yChat 不直接从外部重建请求，而是在 renderer 内动态找到 Desktop 当前的删除函数并调用。当前 build 中该函数的 minified export 是 `mMt`，但 A11yChat 不硬编码该名字，而是按以下函数源码特征定位：

```text
/conversation/id/{conversation_id}
conversation_deleted
safeDelete
```

Desktop 自己的删除函数会同步清理 conversation/sidebar/query cache。A11yChat 不做 DOM 点击，也不调用 `Page.navigate`。

删除是幂等的：同一个 conversation 再执行一次 `delete`，如果 Desktop 返回 `conversation_deleted`，A11yChat 仍视为已经达到目标状态并返回成功。

注意 `deleted` 在不同命令里的退出码语义不同：

```text
delete <id> 成功删除 / 已经删除
-> state = deleted
-> exit code = 0

read <id> 发现 conversation 已删除
-> state = deleted
-> exit code = 3
```

`delete` 不支持：

```text
--text
--wait
--plugin
--after-node
```

---
# 如何判断 assistant 真正完成？
A11yChat **不使用固定 sleep 时间作为完成判断**。

只有当前 conversation tree 的最新节点满足：

```text
author.role == "assistant"
message.status == "finished_successfully"
message.end_turn == true
message.metadata.is_complete == true
```

才会返回：

```json
{
  "state": "complete",
  "complete": true
}
```

同时还会返回：

```text
finishType
requestId
turnExchangeId
workingTurnId
currentNode
```

便于调用端审计。

---

# --text 纯文本模式

默认输出是 JSON，适合程序解析和调试。

如果调用方只想拿 assistant 最终文本，可以加：

```text
--text
```

## 新 Chat

```powershell
A11yChat.exe ask "你好" --text
```

成功时 stdout：

```text
你好！有什么我可以帮你的？
```

不会附加 JSON、conversation id、状态标签或其它文字。

---

## 已有 Chat

```powershell
A11yChat.exe send <conversation-id> "继续" --wait --text
```

stdout 只包含这一轮 assistant 最终文本。

> `send --text` 必须同时使用 `--wait`，因为不等待时还不存在稳定的 assistant 最终文本。

---

## 读取

```powershell
A11yChat.exe read <conversation-id> --text
```

---

## --text 错误契约

成功时：

```text
exit code = 0
stdout    = assistant text
stderr    = 空
```

失败时：

```text
stdout    = 空
stderr    = 一行稳定错误
exit code = 非 0
```

例如 conversation 已删除：

```text
A11yChat: deleted
```

其它错误：

```text
A11yChat: timeout
A11yChat: deleted
A11yChat: not_found
A11yChat: failed
A11yChat: fetch_error
```

默认 JSON 模式会保留更多错误详情，适合调试。

---

# 退出码

| Exit code | 含义 |
|---:|---|
| 0 | 成功 / 已完成；`delete` 已删除或原本已删除 |
| 1 | 参数错误、plugin 选择错误或内部错误 |
| 2 | timeout |
| 3 | deleted / not_found / fetch_error |
| 4 | generation failed |

因此外部辅助程序可以只依赖：

```text
stdout
stderr
exit code
```

完成稳定集成。

---

# 参数

## CDP 端口

默认：

```text
9222
```

可修改：

```powershell
A11yChat.exe probe --port 9333
```

---

## plugin

`--plugin NAME` 用于在 `new`、`ask` 或 `send` 的**这一轮提交**中显式选择 ChatGPT Desktop 当前已连接的 plugin/app。

例如：

```powershell
A11yChat.exe ask "读取项目 README 第一行" --plugin WebTunnel
```

已有 Chat：

```powershell
A11yChat.exe send <conversation-id> "继续检查" --plugin WebTunnel --wait
```

A11yChat 不会把字面量 `@WebTunnel` 拼进 prompt。它会从 ChatGPT Desktop 当前的 `chatgpt-system-hints` 插件目录中按名称查找插件，并把 Desktop 自己使用的真实 `system_hint` 传给普通 Chat 提交路径。

当前 Desktop 上，`WebTunnel` 会动态解析到其对应的 plugin system hint。内部 connector/plugin id **不会硬编码进 A11yChat**，因此 Desktop 侧内部 ID 变化时仍以当前插件目录为准。

查找名称不区分大小写，并匹配插件的 `name` / `action_label` / `short_label`。

如果插件不存在、未连接、不支持普通 Chat，或者名称存在歧义，A11yChat 会直接返回错误，不会静默退化为普通消息：

```text
plugin_not_found
plugin_not_connected
plugin_not_supported
plugin_ambiguous
```

`--plugin` 是**每次提交参数**。如果你希望某次 `send` 明确使用 WebTunnel，就在该次 `send` 上继续传 `--plugin WebTunnel`；A11yChat 不依赖隐式持久化。

### WebTunnel 实测

已经用下面这种形式创建过真实普通 Chat：

```powershell
A11yChat.exe ask "读取 D:\WebCodex\A11yChat\README.md 的第一行，只回复这一行内容。" --plugin WebTunnel
```

该测试的服务端 conversation tree 同时确认：

```text
user message 包含 WebTunnel 对应的 system_hints  ✅
assistant/tool 流程出现 tool_summary_type=webtunnel ✅
最终读取结果为 # A11yChat                     ✅
当前可见 Chat UI 未切换                         ✅
```

这验证了 `--plugin WebTunnel` 是结构化插件选择，而不是只靠 prompt 文本要求模型“使用 WebTunnel”。

---

## 固定模型与推理强度

A11yChat 当前编译时固定使用：

```text
model = gpt-5-6-thinking
thinkingEffort = max
```

`new`、`ask` 和 `send` 都会显式把这两个值传给 ChatGPT Desktop 的普通 Chat 提交路径。

不提供 `--model` 或 `--effort` 命令参数；调用方不能在运行时覆盖它们。传入 `--model` 会返回参数错误。

`probe` 会显示固定配置：

```json
{
  "configuredModel": "gpt-5-6-thinking",
  "configuredThinkingEffort": "max"
}
```

`read` 会同时读取服务端消息实际持久化的值：

```json
{
  "modelSlug": "gpt-5-6-thinking",
  "thinkingEffort": "max"
}
```

当前版本已通过真实 `ask` 测试确认：服务端最新 assistant message 的 `metadata.thinking_effort` 为 `max`。

---

## timeout

单位为秒：

```powershell
--timeout 120
```

默认：

```text
120
```

---

## poll

单位为毫秒：

```powershell
--poll 750
```

默认：

```text
750
```

---

# 是否需要常驻进程？

**不需要。**

当前设计是普通 CLI：

```text
启动 A11yChat.exe
    |
连接正在运行的 ChatGPT Desktop CDP
    |
执行 ask / send / read
    |
输出结果
    |
A11yChat.exe 退出
```

ChatGPT Desktop 本身继续运行。

也就是说：

```powershell
A11yChat.exe ask "问题" --text
```

完成后 A11yChat 进程就退出，不需要后台常驻 server。

如果未来有极高频调用需求，可以再增加 JSONL/stdin server 模式，但当前并不是必要条件。

---

# 安全注意事项

CDP 对 renderer 拥有很高权限。

A11yChat 默认只使用：

```text
127.0.0.1
```

请不要把 remote debugging 端口暴露到：

- 局域网；
- 公网；
- `0.0.0.0`；
- 不受信任的容器或用户环境。

不要把 CDP 当成普通低权限 IPC 接口。

A11yChat 本身也不需要读取或导出 ChatGPT cookie、session token 或认证信息。

---

# 当前实现的兼容性风险

A11yChat 使用 ChatGPT Desktop 当前 renderer 中的内部实现，包括当前观察到的：

```text
m.vet(...)
m.azt(...)
```

这些不是对外公开的稳定 API。

因此 ChatGPT Desktop 更新后可能出现：

- bundle hash 改变；
- export 名称变化；
- React scope 结构变化；
- conversation 数据结构变化；
- 内部 submit/fetch 参数变化。

项目已经避免硬编码具体 bundle hash作为唯一入口，并通过运行时资源列表定位 `app-initial-*.js`，但内部导出变化仍可能需要适配。

升级 Desktop 后建议：

```powershell
A11yChat.exe probe
```

然后再做一轮最小 `ask` 测试。

---

# 构建

项目要求 .NET 8 SDK 或更新版本。

开发构建：

```powershell
dotnet build -c Release
```

---

## 发布 self-contained 单文件 EXE

```powershell
dotnet publish -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

生成的 EXE 可以在目标 Windows x64 机器上独立运行，不要求额外安装 .NET Runtime。

---

# 项目结构

```text
A11yChat/
├─ A11yChat.csproj
├─ Program.cs
├─ README.md
└─ dist/
   ├─ A11yChat.exe
   └─ README.md
```

正式可执行文件位于：

```text
D:\WebCodex\A11yChat\dist\A11yChat.exe
```

`dist` 是当前发布目录；项目不需要使用 `%LOCALAPPDATA%\ChatGPT-CDP` 之类的额外运行目录。

当前主线是 C# / .NET 版本。

早期 PowerShell 原型仍可作为调试和回退参考，但新功能应优先维护 C# 实现。

---

# 已验证场景

当前版本已经实际验证：

- CDP 主 renderer 发现；
- 普通 Chat background new；
- 普通 Chat background ask；
- cold-cache conversation read；
- 已有 conversation background send；
- 已有 conversation cold send；
- `send --wait` 防旧回复竞态；
- 服务端最新 parent 获取；
- assistant 完成状态检测；
- deleted conversation 检测；
- `delete <conversation-id>` 普通 Chat 删除；
- `delete` 幂等：重复删除仍成功；
- `--text` stdout/stderr 契约；
- `--plugin NAME` 显式插件选择；
- `--plugin WebTunnel` 解析 Desktop 真实 system hint；
- A11yChat 新建普通 Chat 后真实调用 WebTunnel；
- 不存在插件时直接 `plugin_not_found`，不静默降级；
- conversation fetch 遇到 429 / `Too many requests` 时，等待模式按 transient error 重试；
- 最终 dispatch request 中移除 `handoff` / `continue_in_work` local functions；
- 明确要求转 Work 的 `new` 实测 `local_function_signatures=[]`；
- 已有 conversation 的 `send` 实测 `local_function_signatures=[]`；
- 源头过滤后 conversation tree 中 `handoffCalls=0` / `handoffTools=0`；
- `--plugin WebTunnel` 与源头过滤同时工作，plugin system hint 保留且真实调用 WebTunnel；
- Work 测试文件未被创建；
- 当前 Chat UI 不切换。

测试中已经实际得到过：

```text
CS_ASK_OK
CS_SEND_OK
TEXT_MODE_OK
TEXT_SEND_OK
HARNESS-API-OK-20260924
# A11yChat  # --plugin WebTunnel 真实文件读取结果
```

---

# License

当前项目尚未指定开源许可证。

如果计划公开发布，请在发布前补充合适的 `LICENSE` 文件。





