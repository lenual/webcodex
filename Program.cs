
using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

Console.OutputEncoding = Encoding.UTF8;
return await App.RunAsync(args);

static class App
{
    static readonly JsonSerializerOptions JsonOut = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static async Task<int> RunAsync(string[] args)
    {
        var textModeRequested = args.Any(a => string.Equals(a, "--text", StringComparison.OrdinalIgnoreCase) ||
                                              string.Equals(a, "-text", StringComparison.OrdinalIgnoreCase));
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return args.Length == 0 ? 1 : 0;
        }

        try
        {
            var command = args[0].ToLowerInvariant();
            var parsed = CliOptions.Parse(args.Skip(1).ToArray());

            return command switch
            {
                "launch" => await LaunchAsync(parsed),
                "probe" => await ProbeAsync(parsed),
                "new" => await NewAsync(parsed),
                "ask" => await AskAsync(parsed),
                "send" => await SendAsync(parsed),
                "read" => await ReadAsync(parsed),
                "delete" => await DeleteAsync(parsed),
                _ => FailUsage($"Unknown command: {args[0]}")
            };
        }
        catch (CliException ex)
        {
            if (textModeRequested)
                Console.Error.WriteLine(ex.Message);
            else
                Print(new JsonObject
                {
                    ["ok"] = false,
                    ["state"] = "usage_error",
                    ["error"] = ex.Message
                });
            return 1;
        }
        catch (Exception ex)
        {
            if (textModeRequested)
                Console.Error.WriteLine(ex.Message);
            else
                Print(new JsonObject
                {
                    ["ok"] = false,
                    ["state"] = "error",
                    ["error"] = ex.Message
                });
            return 1;
        }
    }

    static bool IsHelp(string s) => s is "-h" or "--help" or "help";

    static int FailUsage(string message) => throw new CliException(message);

    static async Task<int> LaunchAsync(CliOptions o)
    {
        if (o.Text) throw new CliException("--text is supported only by ask, send --wait, and read.");
        var port = o.Port;
        if (await CdpTransport.IsReadyAsync(port))
        {
            Print(new JsonObject
            {
                ["ok"] = true,
                ["state"] = "already_ready",
                ["port"] = port,
                ["cdpReady"] = true
            });
            return 0;
        }

        if (!OperatingSystem.IsWindows())
            throw new InvalidOperationException("launch is supported only on Windows.");

        var aumid = o.Aumid ?? "OpenAI.Codex_2p2nqsd0c76g0!App";
        var arguments = $"--remote-debugging-address=127.0.0.1 --remote-debugging-port={port}";
        var pid = AppxLauncher.Launch(aumid, arguments);

        var sw = Stopwatch.StartNew();
        var ready = false;
        while (sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (await CdpTransport.IsReadyAsync(port))
            {
                ready = true;
                break;
            }
            await Task.Delay(250);
        }

        Print(new JsonObject
        {
            ["ok"] = ready,
            ["state"] = ready ? "ready" : "launched_but_cdp_not_ready",
            ["pid"] = pid,
            ["port"] = port,
            ["cdpReady"] = ready,
            ["hint"] = ready ? null : "If ChatGPT was already running without CDP, fully exit it and run A11yChat.exe launch again."
        });
        return ready ? 0 : 1;
    }

    static async Task<int> ProbeAsync(CliOptions o)
    {
        if (o.Text) throw new CliException("--text is supported only by ask, send --wait, and read.");
        var result = await ChatBridge.ProbeAsync(o.Port);
        result["configuredModel"] = ChatBridge.FixedModel;
        result["configuredThinkingEffort"] = ChatBridge.FixedThinkingEffort;
        Print(result);
        return result["ok"]?.GetValue<bool>() == true ? 0 : 1;
    }

    static async Task<int> NewAsync(CliOptions o)
    {
        if (o.Text) throw new CliException("--text is supported only by ask, send --wait, and read.");
        var message = o.RequirePositional(0, "Usage: A11yChat.exe new \"message\" [--plugin NAME]");
        var result = await ChatBridge.NewConversationAsync(message, o.Plugin, o.Port);
        Print(result);
        return result["ok"]?.GetValue<bool>() == true ? 0 : StateExitCode(result["state"]?.GetValue<string>());
    }

    static async Task<int> AskAsync(CliOptions o)
    {
        var message = o.RequirePositional(0, "Usage: A11yChat.exe ask \"message\" [--timeout 120] [--poll 750] [--plugin NAME]");
        var total = Stopwatch.StartNew();

        var created = await ChatBridge.NewConversationAsync(message, o.Plugin, o.Port);
        if (created["ok"]?.GetValue<bool>() != true)
            return EmitResult(created, o, StateExitCode(created["state"]?.GetValue<string>()));

        var conversationId = created["serverConversationId"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new InvalidOperationException("new did not return serverConversationId.");

        var read = await ChatBridge.ReadUntilAsync(
            conversationId,
            wait: true,
            timeoutSec: o.TimeoutSec,
            pollMs: o.PollMs,
            port: o.Port,
            afterNodeId: null);

        var result = new JsonObject
        {
            ["ok"] = read.ExitCode == 0 && read.Data["complete"]?.GetValue<bool>() == true,
            ["state"] = Clone(read.Data["state"]),
            ["complete"] = Clone(read.Data["complete"]),
            ["conversationId"] = conversationId,
            ["clientConversationId"] = Clone(created["clientConversationId"]),
            ["model"] = Clone(created["model"]),
            ["thinkingEffort"] = Clone(created["thinkingEffort"]),
            ["plugin"] = Clone(created["plugin"]),
            ["pluginSystemHint"] = Clone(created["pluginSystemHint"]),
            ["title"] = Clone(read.Data["title"]),
            ["text"] = Clone(read.Data["text"]),
            ["messageStatus"] = Clone(read.Data["messageStatus"]),
            ["endTurn"] = Clone(read.Data["endTurn"]),
            ["isComplete"] = Clone(read.Data["isComplete"]),
            ["finishType"] = Clone(read.Data["finishType"]),
            ["requestId"] = Clone(read.Data["requestId"]),
            ["turnExchangeId"] = Clone(read.Data["turnExchangeId"]),
            ["workingTurnId"] = Clone(read.Data["workingTurnId"]),
            ["readWaitedMs"] = Clone(read.Data["waitedMs"]),
            ["totalElapsedMs"] = (long)total.Elapsed.TotalMilliseconds,
            ["documentTitle"] = Clone(read.Data["documentTitle"])
        };

        return EmitResult(result, o, read.ExitCode);
    }

    static async Task<int> SendAsync(CliOptions o)
    {
        var conversationId = o.RequirePositional(0, "Usage: A11yChat.exe send <conversation-id> \"message\" [--wait] [--plugin NAME]");
        var message = o.RequirePositional(1, "Usage: A11yChat.exe send <conversation-id> \"message\" [--wait] [--plugin NAME]");
        if (o.Text && !o.Wait)
            throw new CliException("--text with send requires --wait.");

        var total = Stopwatch.StartNew();
        var sent = await ChatBridge.SendAsync(conversationId, message, o.Plugin, o.Port);
        if (sent["ok"]?.GetValue<bool>() != true)
            return EmitResult(sent, o, StateExitCode(sent["state"]?.GetValue<string>()));

        if (!o.Wait)
        {
            Print(sent);
            return 0;
        }

        var parent = sent["parentMessageId"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(parent))
            throw new InvalidOperationException("send did not return parentMessageId.");

        var read = await ChatBridge.ReadUntilAsync(
            conversationId,
            wait: true,
            timeoutSec: o.TimeoutSec,
            pollMs: o.PollMs,
            port: o.Port,
            afterNodeId: parent);

        var result = new JsonObject
        {
            ["ok"] = read.ExitCode == 0 && read.Data["complete"]?.GetValue<bool>() == true,
            ["state"] = Clone(read.Data["state"]),
            ["complete"] = Clone(read.Data["complete"]),
            ["conversationId"] = conversationId,
            ["parentMessageId"] = parent,
            ["conversationSource"] = Clone(sent["conversationSource"]),
            ["model"] = Clone(sent["model"]),
            ["thinkingEffort"] = Clone(sent["thinkingEffort"]),
            ["plugin"] = Clone(sent["plugin"]),
            ["pluginSystemHint"] = Clone(sent["pluginSystemHint"]),
            ["streamRequestId"] = Clone(sent["streamRequestId"]),
            ["title"] = Clone(read.Data["title"]),
            ["text"] = Clone(read.Data["text"]),
            ["messageStatus"] = Clone(read.Data["messageStatus"]),
            ["endTurn"] = Clone(read.Data["endTurn"]),
            ["isComplete"] = Clone(read.Data["isComplete"]),
            ["finishType"] = Clone(read.Data["finishType"]),
            ["requestId"] = Clone(read.Data["requestId"]),
            ["turnExchangeId"] = Clone(read.Data["turnExchangeId"]),
            ["workingTurnId"] = Clone(read.Data["workingTurnId"]),
            ["readWaitedMs"] = Clone(read.Data["waitedMs"]),
            ["totalElapsedMs"] = (long)total.Elapsed.TotalMilliseconds,
            ["documentTitle"] = Clone(read.Data["documentTitle"])
        };

        return EmitResult(result, o, read.ExitCode);
    }

    static async Task<int> ReadAsync(CliOptions o)
    {
        var conversationId = o.RequirePositional(0, "Usage: A11yChat.exe read <conversation-id> [--wait] [--after-node ID]");
        var read = await ChatBridge.ReadUntilAsync(
            conversationId,
            wait: o.Wait,
            timeoutSec: o.TimeoutSec,
            pollMs: o.PollMs,
            port: o.Port,
            afterNodeId: o.AfterNodeId);
        return EmitResult(read.Data, o, read.ExitCode);
    }

    static async Task<int> DeleteAsync(CliOptions o)
    {
        if (o.Text) throw new CliException("--text is not supported by delete.");
        if (o.Wait) throw new CliException("--wait is not supported by delete.");
        if (o.Plugin is not null) throw new CliException("--plugin is not supported by delete.");
        if (o.AfterNodeId is not null) throw new CliException("--after-node is not supported by delete.");

        var conversationId = o.RequirePositional(0, "Usage: A11yChat.exe delete <conversation-id>");
        if (o.Positionals.Count > 1)
            throw new CliException("Usage: A11yChat.exe delete <conversation-id>");

        var result = await ChatBridge.DeleteAsync(conversationId, o.Port);
        Print(result);
        return result["ok"]?.GetValue<bool>() == true
            ? 0
            : StateExitCode(result["state"]?.GetValue<string>());
    }

    static int EmitResult(JsonObject result, CliOptions o, int exitCode)
    {
        if (!o.Text)
        {
            Print(result);
            return exitCode;
        }

        if (exitCode == 0)
        {
            var text = result["text"]?.GetValue<string>() ?? string.Empty;
            Console.Out.Write(text);
            return 0;
        }

        var state = result["state"]?.GetValue<string>() ?? "error";
        var message = state switch
        {
            "timeout" => "A11yChat: timeout",
            "deleted" => "A11yChat: deleted",
            "not_found" => "A11yChat: not_found",
            "failed" => "A11yChat: failed",
            "fetch_error" => "A11yChat: fetch_error",
            "plugin_not_found" => "A11yChat: plugin_not_found",
            "plugin_not_connected" => "A11yChat: plugin_not_connected",
            "plugin_not_supported" => "A11yChat: plugin_not_supported",
            "plugin_ambiguous" => "A11yChat: plugin_ambiguous",
            _ => $"A11yChat: {state}"
        };
        Console.Error.WriteLine(message);
        return exitCode;
    }

    static int StateExitCode(string? state) => state switch
    {
        "timeout" => 2,
        "deleted" or "not_found" or "fetch_error" => 3,
        "failed" => 4,
        _ => 1
    };

    static JsonNode? Clone(JsonNode? node) => node?.DeepClone();

    static void Print(JsonNode node) =>
        Console.WriteLine(node.ToJsonString(JsonOut));

    static void PrintHelp()
    {
        Console.WriteLine("""
A11yChat - background control for ordinary ChatGPT Desktop Chat via CDP

Commands:
  A11yChat.exe launch [--port 9222]
  A11yChat.exe probe
  A11yChat.exe new  "message" [--plugin NAME]
  A11yChat.exe ask  "message" [--timeout 120] [--poll 750] [--plugin NAME] [--text]
  A11yChat.exe send <conversation-id> "message" [--wait] [--timeout 120] [--poll 750] [--plugin NAME] [--text]
  A11yChat.exe read <conversation-id> [--wait] [--timeout 120] [--poll 750] [--after-node ID] [--text]
  A11yChat.exe delete <conversation-id>

Plugin selection:
  --plugin NAME explicitly selects a connected ChatGPT plugin/app for this submission.
  Example: --plugin WebTunnel
  Unavailable plugins fail instead of silently falling back.

Work handoff:
  new, ask, and send remove Work handoff local functions before request dispatch, keeping A11yChat turns in ordinary Chat.

Text mode:
  --text writes only the assistant text to stdout on success.
  Errors go to stderr and keep the normal non-zero exit code.
  Supported by ask, send --wait, and read.

Exit codes:
  0 success / complete
  1 usage or internal error
  2 timeout
  3 deleted / not_found / fetch_error
  4 generation failed
""");
    }
}

sealed class CliOptions
{
    public List<string> Positionals { get; } = [];
    public int Port { get; private set; } = 9222;
    public bool Wait { get; private set; }
    public bool Text { get; private set; }
    public string? Plugin { get; private set; }
    public int TimeoutSec { get; private set; } = 120;
    public int PollMs { get; private set; } = 750;
    public string? AfterNodeId { get; private set; }
    public string? Aumid { get; private set; }

    public string RequirePositional(int index, string usage)
    {
        if (index >= Positionals.Count || string.IsNullOrWhiteSpace(Positionals[index]))
            throw new CliException(usage);
        return Positionals[index];
    }

    public static CliOptions Parse(string[] args)
    {
        var o = new CliOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a.ToLowerInvariant())
            {
                case "--wait":
                case "-wait":
                    o.Wait = true;
                    break;

                case "--text":
                case "-text":
                    o.Text = true;
                    break;

                case "--plugin":
                case "-plugin":
                    o.Plugin = Next(args, ref i, a);
                    if (string.IsNullOrWhiteSpace(o.Plugin))
                        throw new CliException($"{a} requires a non-empty plugin name.");
                    break;

                case "--port":
                case "-port":
                    o.Port = ParseInt(Next(args, ref i, a), a, 1, 65535);
                    break;

                case "--timeout":
                case "--timeout-sec":
                case "-timeoutsec":
                    o.TimeoutSec = ParseInt(Next(args, ref i, a), a, 1, 86400);
                    break;

                case "--poll":
                case "--poll-ms":
                case "-pollms":
                    o.PollMs = ParseInt(Next(args, ref i, a), a, 200, 60000);
                    break;

                case "--after-node":
                case "-afternodeid":
                    o.AfterNodeId = Next(args, ref i, a);
                    break;

                case "--aumid":
                    o.Aumid = Next(args, ref i, a);
                    break;

                default:
                    if (a.StartsWith('-'))
                        throw new CliException($"Unknown option: {a}");
                    o.Positionals.Add(a);
                    break;
            }
        }

        return o;
    }

    static string Next(string[] args, ref int i, string option)
    {
        if (++i >= args.Length)
            throw new CliException($"Missing value for {option}");
        return args[i];
    }

    static int ParseInt(string value, string option, int min, int max)
    {
        if (!int.TryParse(value, out var n) || n < min || n > max)
            throw new CliException($"Invalid value for {option}: {value}");
        return n;
    }
}

static class ChatBridge
{
    public const string FixedModel = "gpt-5-6-thinking";
    public const string FixedThinkingEffort = "max";

    static string Q(string? value) => JsonSerializer.Serialize(value);

    public static Task<JsonObject> ProbeAsync(int port)
    {
        var js = """
(async()=>{
 const names=performance.getEntriesByType('resource').map(e=>e.name);
 let appInitial=names.find(n=>/\/assets\/app-initial-[^/]+\.js(?:\?|$)/.test(n))||[...document.scripts].map(s=>s.src).find(n=>/\/assets\/app-initial-[^/]+\.js(?:\?|$)/.test(n));
 if(!appInitial){
   const entry=[...document.scripts].map(s=>s.src).find(n=>/\/assets\/index-[^/]+\.js(?:\?|$)/.test(n));
   if(entry){const source=await(await fetch(entry)).text(),match=source.match(/app-initial-[A-Za-z0-9_-]+\.js/);if(match)appInitial=new URL(match[0],entry).href}
 }
 if(!appInitial)throw new Error('Chat app-initial module not found');
 await import(appInitial);
 const rootEl=document.getElementById('root'),rk=rootEl&&Object.getOwnPropertyNames(rootEl).find(k=>k.startsWith('__reactContainer$')),root=rk?rootEl[rk]:null;
 const isScope=o=>o&&typeof o==='object'&&typeof o.get==='function'&&typeof o.set==='function'&&o.queryClient;
 let scope=null,stack=root?[root]:[],seen=new Set();
 while(stack.length&&!scope){const f=stack.pop();if(!f||typeof f!=='object'||seen.has(f))continue;seen.add(f);const vals=[];if(f.memoizedProps&&typeof f.memoizedProps==='object')vals.push(...Object.values(f.memoizedProps));let h=f.memoizedState,i=0;while(h&&i++<30){vals.push(h.memoizedState);if(h.memoizedState&&typeof h.memoizedState==='object')vals.push(...Object.values(h.memoizedState));h=h.next}let c=f.dependencies?.firstContext,j=0;while(c&&j++<30){vals.push(c.memoizedValue);c=c.next}scope=vals.find(isScope)||null;if(f.child)stack.push(f.child);if(f.sibling)stack.push(f.sibling)}
 let model=null;if(scope){const qs=scope.queryClient.getQueryCache().getAll();for(let i=qs.length-1;i>=0;i--){const q=qs[i],k=q.queryKey,d=q.state?.data;if(Array.isArray(k)&&k[0]==='chatgpt-conversation-details'&&d){model=d.intended_default_model_slug||d.default_model_slug||k[1]?.requestedDefaultModel||null;if(model)break}}}
 return {ok:!!scope,title:document.title,href:location.href,visibility:document.visibilityState,module:appInitial,model};
})()
""";
        return CdpTransport.EvaluateObjectAsync(port, js, TimeSpan.FromSeconds(20));
    }

    public static Task<JsonObject> NewConversationAsync(string message, string? plugin, int port)
    {
        var js = NewJs
            .Replace("__MESSAGE_JSON__", Q(message))
            .Replace("__PLUGIN_JSON__", Q(plugin))
            .Replace("__FIXED_MODEL_JSON__", Q(FixedModel))
            .Replace("__FIXED_EFFORT_JSON__", Q(FixedThinkingEffort));
        return CdpTransport.EvaluateObjectAsync(port, js, TimeSpan.FromSeconds(30));
    }

    public static Task<JsonObject> SendAsync(string conversationId, string message, string? plugin, int port)
    {
        var js = SendJs
            .Replace("__CID_JSON__", Q(conversationId))
            .Replace("__MESSAGE_JSON__", Q(message))
            .Replace("__PLUGIN_JSON__", Q(plugin))
            .Replace("__FIXED_MODEL_JSON__", Q(FixedModel))
            .Replace("__FIXED_EFFORT_JSON__", Q(FixedThinkingEffort));
        return CdpTransport.EvaluateObjectAsync(port, js, TimeSpan.FromSeconds(90));
    }

    public static Task<JsonObject> DeleteAsync(string conversationId, int port)
    {
        var js = DeleteJs
            .Replace("__CID_JSON__", Q(conversationId));
        return CdpTransport.EvaluateObjectAsync(port, js, TimeSpan.FromSeconds(30));
    }

    public static async Task<ReadOutcome> ReadUntilAsync(
        string conversationId,
        bool wait,
        int timeoutSec,
        int pollMs,
        int port,
        string? afterNodeId)
    {
        var started = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(timeoutSec);

        while (true)
        {
            JsonObject result;
            try
            {
                var js = ReadJs
                    .Replace("__CID_JSON__", Q(conversationId))
                    .Replace("__AFTER_NODE_JSON__", Q(afterNodeId));
                var remaining = timeout - started.Elapsed;
                var singleTimeout = remaining < TimeSpan.FromSeconds(20)
                    ? (remaining > TimeSpan.FromMilliseconds(250) ? remaining : TimeSpan.FromMilliseconds(250))
                    : TimeSpan.FromSeconds(20);
                result = await CdpTransport.EvaluateObjectAsync(port, js, singleTimeout);
            }
            catch (OperationCanceledException ex)
            {
                result = new JsonObject
                {
                    ["ok"] = false,
                    ["state"] = "transient_error",
                    ["complete"] = false,
                    ["conversationId"] = conversationId,
                    ["error"] = ex.Message
                };
            }
            catch (TimeoutException ex)
            {
                result = new JsonObject
                {
                    ["ok"] = false,
                    ["state"] = "transient_error",
                    ["complete"] = false,
                    ["conversationId"] = conversationId,
                    ["error"] = ex.Message
                };
            }

            result["waitedMs"] = (long)started.Elapsed.TotalMilliseconds;
            var state = result["state"]?.GetValue<string>();

            if (!wait)
                return new ReadOutcome(result, StateExitCode(state, result));

            if (state == "complete")
                return new ReadOutcome(result, 0);

            if (state == "failed")
                return new ReadOutcome(result, 4);

            if (state is "deleted" or "not_found")
                return new ReadOutcome(result, 3);

            if (started.Elapsed >= timeout)
            {
                result["state"] = "timeout";
                result["complete"] = false;
                result["timeoutSec"] = timeoutSec;
                return new ReadOutcome(result, 2);
            }

            var delayMs = state == "transient_error"
                ? Math.Max(pollMs, 5000)
                : pollMs;
            await Task.Delay(delayMs);
        }
    }

    static int StateExitCode(string? state, JsonObject result)
    {
        if (result["ok"]?.GetValue<bool>() == true)
            return 0;
        return state switch
        {
            "timeout" => 2,
            "deleted" or "not_found" or "fetch_error" => 3,
            "failed" => 4,
            _ => 1
        };
    }

    const string NewJs = """
(async()=>{
 const requestedMessage=__MESSAGE_JSON__,requestedPlugin=__PLUGIN_JSON__,model=__FIXED_MODEL_JSON__,thinkingEffort=__FIXED_EFFORT_JSON__;
 const names=performance.getEntriesByType('resource').map(e=>e.name);
 let appInitial=names.find(n=>/\/assets\/app-initial-[^/]+\.js(?:\?|$)/.test(n))||[...document.scripts].map(s=>s.src).find(n=>/\/assets\/app-initial-[^/]+\.js(?:\?|$)/.test(n));
 if(!appInitial){
   const entry=[...document.scripts].map(s=>s.src).find(n=>/\/assets\/index-[^/]+\.js(?:\?|$)/.test(n));
   if(entry){const source=await(await fetch(entry)).text(),match=source.match(/app-initial-[A-Za-z0-9_-]+\.js/);if(match)appInitial=new URL(match[0],entry).href}
 }
 if(!appInitial)throw new Error('Chat app-initial module not found');
 const m=await import(appInitial);
 const functionSource=v=>{if(typeof v!=='function')return'';try{return String(v)}catch{return''}};
 const submitFn=Object.values(m).find(v=>{const s=functionSource(v);return s.includes('onClientThreadIdChange')&&s.includes('onServerThreadIdChange')&&s.includes('isSubmissionCurrent')})??m.vet;
 if(typeof submitFn!=='function')throw new Error('Chat submit function not found');
 const rootEl=document.getElementById('root'),rk=rootEl&&Object.getOwnPropertyNames(rootEl).find(k=>k.startsWith('__reactContainer$')),root=rk?rootEl[rk]:null;
 const isScope=o=>o&&typeof o==='object'&&typeof o.get==='function'&&typeof o.set==='function'&&o.queryClient;
 let scope=null,stack=root?[root]:[],seen=new Set();
 while(stack.length&&!scope){const f=stack.pop();if(!f||typeof f!=='object'||seen.has(f))continue;seen.add(f);const vals=[];if(f.memoizedProps&&typeof f.memoizedProps==='object')vals.push(...Object.values(f.memoizedProps));let h=f.memoizedState,i=0;while(h&&i++<30){vals.push(h.memoizedState);if(h.memoizedState&&typeof h.memoizedState==='object')vals.push(...Object.values(h.memoizedState));h=h.next}let c=f.dependencies?.firstContext,j=0;while(c&&j++<30){vals.push(c.memoizedValue);c=c.next}scope=vals.find(isScope)||null;if(f.child)stack.push(f.child);if(f.sibling)stack.push(f.sibling)}
 if(!scope)throw new Error('ChatGPT internal scope not found');

 const resolvePlugin=(name)=>{
   if(name==null||String(name).trim()==='')return{ok:true,name:null,systemHint:null,systemHints:[]};
   const raw=String(name).trim();
   if(raw.startsWith('plugin:'))return{ok:true,name:raw,systemHint:raw,systemHints:[raw]};
   const wanted=String(name).trim().toLowerCase(),matches=[];
   for(const q of scope.queryClient.getQueryCache().getAll()){
     const k=q.queryKey,d=q.state?.data;
     if(!Array.isArray(k)||k[0]!=='chatgpt-system-hints'||d==null||typeof d!=='object')continue;
     for(const [key,v] of Object.entries(d)){
       if(v==null||typeof v!=='object'||typeof v.system_hint!=='string')continue;
       if(v.is_plugin!==true&&!v.system_hint.startsWith('plugin:'))continue;
       const labels=[v.name,v.action_label,v.short_label].filter(x=>typeof x==='string').map(x=>x.trim().toLowerCase());
       if(labels.includes(wanted))matches.push({key,name:v.name??String(name).trim(),systemHint:v.system_hint,isConnected:v.is_connected,requiredConversationModes:v.required_conversation_modes});
     }
   }
   const unique=[...new Map(matches.map(x=>[x.systemHint,x])).values()];
   if(unique.length===0)return{ok:false,state:'plugin_not_found',error:'Plugin not found in ChatGPT Desktop: '+name};
   if(unique.length>1)return{ok:false,state:'plugin_ambiguous',error:'Plugin name is ambiguous: '+name};
   const p=unique[0];
   if(p.isConnected===false)return{ok:false,state:'plugin_not_connected',error:'Plugin is not connected: '+p.name};
   if(Array.isArray(p.requiredConversationModes)&&p.requiredConversationModes.length>0&&!p.requiredConversationModes.includes('primary_assistant'))return{ok:false,state:'plugin_not_supported',error:'Plugin does not support ordinary Chat: '+p.name};
   return{ok:true,name:p.name,systemHint:p.systemHint,systemHints:[p.systemHint]};
 };
 const plugin=resolvePlugin(requestedPlugin);
 if(!plugin.ok)return{ok:false,state:plugin.state,error:plugin.error,plugin:requestedPlugin,model,thinkingEffort};
 const systemHints=plugin.systemHints;

 const installWorkHandoffFilter=()=>{
   const originalGet=scope.get;
   const blocked=new Set(['handoff','continue_in_work']);
   let active=true;

   const messageText=msg=>{
     const c=msg?.content;
     if(Array.isArray(c?.parts))return c.parts.filter(x=>typeof x==='string').join('\n');
     if(typeof c?.text==='string')return c.text;
     return '';
   };

   const matchesRequest=req=>{
     if(req==null||typeof req!=='object')return false;
     const messages=Array.isArray(req.messages)?req.messages:[];
     return messages.some(msg=>msg?.author?.role==='user'&&messageText(msg).trim()===requestedMessage.trim());
   };

   scope.get=function(atom,...rest){
     const value=originalGet.call(scope,atom,...rest);
     if(!active||!value||typeof value!=='object'||typeof value.startCompletionStream!=='function')return value;
     return new Proxy(value,{
       get(target,prop){
         if(prop==='startCompletionStream'){
           return async function(options,...args){
             let nextOptions=options;
             const req=options?.request;
             if(matchesRequest(req)){
               const nextReq={...req};
               if(Array.isArray(req.local_function_names)){
                 nextReq.local_function_names=req.local_function_names.filter(x=>!blocked.has(typeof x==='string'?x:(x?.name??'')));
               }
               if(Array.isArray(req.local_function_signatures)){
                 nextReq.local_function_signatures=req.local_function_signatures.filter(x=>!blocked.has(x?.name??''));
               }
               nextOptions={...options,request:nextReq};
             }
             return target.startCompletionStream.call(target,nextOptions,...args);
           };
         }
         const v=Reflect.get(target,prop,target);
         return typeof v==='function'?v.bind(target):v;
       }
     });
   };

   return {
     restore:()=>{if(active){active=false;scope.get=originalGet}}};
 };
 const workHandoffFilter=installWorkHandoffFilter();
 const state={clientConversationId:null,serverConversationId:null};
 return await new Promise(resolve=>{let settled=false;const finish=o=>{if(settled)return;settled=true;clearTimeout(timer);resolve(o)},timer=setTimeout(()=>finish({ok:false,state:'timeout',error:'Timed out waiting for server conversation id',model,thinkingEffort,plugin:plugin.name,pluginSystemHint:plugin.systemHint,...state}),20000);let p;try{p=submitFn(scope,{prompt:requestedMessage,model,requestedDefaultModel:model,thinkingEffort,systemHints,isTemporaryChat:false,isSubmissionCurrent:()=>true,isRequestCurrent:()=>true,onClientThreadIdChange:id=>state.clientConversationId=id,onServerThreadIdChange:id=>{state.serverConversationId=id;finish({ok:true,state:'created',model,thinkingEffort,plugin:plugin.name,pluginSystemHint:plugin.systemHint,clientConversationId:state.clientConversationId,serverConversationId:id,title:document.title,href:location.href})}})}catch(e){workHandoffFilter.restore();finish({ok:false,state:'error',error:String(e?.stack||e),model,thinkingEffort,plugin:plugin.name,pluginSystemHint:plugin.systemHint});return}Promise.resolve(p).then(r=>{workHandoffFilter.restore();state.streamRequestId=r?.streamRequestId??null;if(state.serverConversationId)finish({ok:true,state:'created',model,thinkingEffort,plugin:plugin.name,pluginSystemHint:plugin.systemHint,clientConversationId:state.clientConversationId,serverConversationId:state.serverConversationId,streamRequestId:state.streamRequestId,title:document.title,href:location.href})}).catch(e=>{workHandoffFilter.restore();finish({ok:false,state:'error',error:String(e?.stack||e),model,thinkingEffort,plugin:plugin.name,pluginSystemHint:plugin.systemHint,...state})})});
})()
""";

    const string SendJs = """
(async()=>{
 try{
   const requestedConversationId=__CID_JSON__,requestedMessage=__MESSAGE_JSON__,requestedPlugin=__PLUGIN_JSON__,model=__FIXED_MODEL_JSON__,thinkingEffort=__FIXED_EFFORT_JSON__;
   const names=performance.getEntriesByType('resource').map(e=>e.name);
   let appInitial=names.find(n=>/\/assets\/app-initial-[^/]+\.js(?:\?|$)/.test(n))||[...document.scripts].map(s=>s.src).find(n=>/\/assets\/app-initial-[^/]+\.js(?:\?|$)/.test(n));
   if(!appInitial){
     const entry=[...document.scripts].map(s=>s.src).find(n=>/\/assets\/index-[^/]+\.js(?:\?|$)/.test(n));
     if(entry){const source=await(await fetch(entry)).text(),match=source.match(/app-initial-[A-Za-z0-9_-]+\.js/);if(match)appInitial=new URL(match[0],entry).href}
   }
   if(!appInitial)throw new Error('Chat app-initial module not found');
   const m=await import(appInitial);
   const functionSource=v=>{if(typeof v!=='function')return'';try{return String(v)}catch{return''}};
   const submitFn=Object.values(m).find(v=>{const s=functionSource(v);return s.includes('onClientThreadIdChange')&&s.includes('onServerThreadIdChange')&&s.includes('isSubmissionCurrent')})??m.vet;
   const fetchFn=Object.values(m).find(v=>{const s=functionSource(v);return s.includes('/conversations/{conversation_id}')&&s.includes('/conversation/{conversation_id}')&&s.includes('Conversation pagination cursor did not advance')})??m.azt;
   if(typeof submitFn!=='function')throw new Error('Chat submit function not found');
   if(typeof fetchFn!=='function')throw new Error('Chat conversation fetch function not found');

   const rootEl=document.getElementById('root'),rk=rootEl&&Object.getOwnPropertyNames(rootEl).find(k=>k.startsWith('__reactContainer$')),root=rk?rootEl[rk]:null;
   const isScope=o=>o&&typeof o==='object'&&typeof o.get==='function'&&typeof o.set==='function'&&o.queryClient;
   let scope=null,stack=root?[root]:[],seen=new Set();
   while(stack.length&&!scope){const f=stack.pop();if(!f||typeof f!=='object'||seen.has(f))continue;seen.add(f);const vals=[];if(f.memoizedProps&&typeof f.memoizedProps==='object')vals.push(...Object.values(f.memoizedProps));let h=f.memoizedState,i=0;while(h&&i++<30){vals.push(h.memoizedState);if(h.memoizedState&&typeof h.memoizedState==='object')vals.push(...Object.values(h.memoizedState));h=h.next}let c=f.dependencies?.firstContext,j=0;while(c&&j++<30){vals.push(c.memoizedValue);c=c.next}scope=vals.find(isScope)||null;if(f.child)stack.push(f.child);if(f.sibling)stack.push(f.sibling)}
   if(!scope)throw new Error('ChatGPT internal scope not found');

   const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
   const fetchConversation=async()=>{
     let lastError=null;
     for(let attempt=0;attempt<7;attempt++){
       try{return await fetchFn(scope,requestedConversationId)}
       catch(e){
         lastError=e;
         const error=String(e?.stack||e);
         const transient=error.includes('Too many requests')||error.includes('429')||error.includes('rate_limit');
         if(!transient||attempt===6)throw e;
         await sleep(Math.min(1500*(2**attempt),15000));
       }
     }
     throw lastError;
   };
   const convo=await fetchConversation();
   const conversationSource='server-fetch';
   const parentMessageId=convo?.current_node??convo?.currentNode;
   if(!parentMessageId)throw new Error('Could not determine current_node for conversation');


   const resolvePlugin=(name)=>{
   if(name==null||String(name).trim()==='')return{ok:true,name:null,systemHint:null,systemHints:[]};
   const raw=String(name).trim();
   if(raw.startsWith('plugin:'))return{ok:true,name:raw,systemHint:raw,systemHints:[raw]};
   const wanted=String(name).trim().toLowerCase(),matches=[];
   for(const q of scope.queryClient.getQueryCache().getAll()){
     const k=q.queryKey,d=q.state?.data;
     if(!Array.isArray(k)||k[0]!=='chatgpt-system-hints'||d==null||typeof d!=='object')continue;
     for(const [key,v] of Object.entries(d)){
       if(v==null||typeof v!=='object'||typeof v.system_hint!=='string')continue;
       if(v.is_plugin!==true&&!v.system_hint.startsWith('plugin:'))continue;
       const labels=[v.name,v.action_label,v.short_label].filter(x=>typeof x==='string').map(x=>x.trim().toLowerCase());
       if(labels.includes(wanted))matches.push({key,name:v.name??String(name).trim(),systemHint:v.system_hint,isConnected:v.is_connected,requiredConversationModes:v.required_conversation_modes});
     }
   }
   const unique=[...new Map(matches.map(x=>[x.systemHint,x])).values()];
   if(unique.length===0)return{ok:false,state:'plugin_not_found',error:'Plugin not found in ChatGPT Desktop: '+name};
   if(unique.length>1)return{ok:false,state:'plugin_ambiguous',error:'Plugin name is ambiguous: '+name};
   const p=unique[0];
   if(p.isConnected===false)return{ok:false,state:'plugin_not_connected',error:'Plugin is not connected: '+p.name};
   if(Array.isArray(p.requiredConversationModes)&&p.requiredConversationModes.length>0&&!p.requiredConversationModes.includes('primary_assistant'))return{ok:false,state:'plugin_not_supported',error:'Plugin does not support ordinary Chat: '+p.name};
   return{ok:true,name:p.name,systemHint:p.systemHint,systemHints:[p.systemHint]};
 };
   const plugin=resolvePlugin(requestedPlugin);
   if(!plugin.ok)return{ok:false,state:plugin.state,error:plugin.error,conversationId:requestedConversationId,parentMessageId,conversationSource,plugin:requestedPlugin,model,thinkingEffort};
   const systemHints=plugin.systemHints;

   const installWorkHandoffFilter=()=>{
     const originalGet=scope.get;
     const blocked=new Set(['handoff','continue_in_work']);
     let active=true;

     const messageText=msg=>{
       const c=msg?.content;
       if(Array.isArray(c?.parts))return c.parts.filter(x=>typeof x==='string').join('\n');
       if(typeof c?.text==='string')return c.text;
       return '';
     };

     const matchesRequest=req=>{
       if(req==null||typeof req!=='object')return false;
       if(req.conversation_id!=null&&String(req.conversation_id)!==String(requestedConversationId))return false;
       const messages=Array.isArray(req.messages)?req.messages:[];
       return messages.some(msg=>msg?.author?.role==='user'&&messageText(msg).trim()===requestedMessage.trim());
     };

     scope.get=function(atom,...rest){
       const value=originalGet.call(scope,atom,...rest);
       if(!active||!value||typeof value!=='object'||typeof value.startCompletionStream!=='function')return value;
       return new Proxy(value,{
         get(target,prop){
           if(prop==='startCompletionStream'){
             return async function(options,...args){
               let nextOptions=options;
               const req=options?.request;
               if(matchesRequest(req)){
                 const nextReq={...req};
                 if(Array.isArray(req.local_function_names)){
                   nextReq.local_function_names=req.local_function_names.filter(x=>!blocked.has(typeof x==='string'?x:(x?.name??'')));
                 }
                 if(Array.isArray(req.local_function_signatures)){
                   nextReq.local_function_signatures=req.local_function_signatures.filter(x=>!blocked.has(x?.name??''));
                 }
                 nextOptions={...options,request:nextReq};
               }
               return target.startCompletionStream.call(target,nextOptions,...args);
             };
           }
           const v=Reflect.get(target,prop,target);
           return typeof v==='function'?v.bind(target):v;
         }
       });
     };

     return {
       restore:()=>{if(active){active=false;scope.get=originalGet}}};
   };

   const workHandoffFilter=installWorkHandoffFilter();
   try{
     const result=await submitFn(scope,{prompt:requestedMessage,conversationId:requestedConversationId,parentMessageId,model,requestedDefaultModel:model,thinkingEffort,systemHints,isTemporaryChat:false,isSubmissionCurrent:()=>true,isRequestCurrent:()=>true});
     return {ok:true,state:'sent',conversationId:requestedConversationId,parentMessageId,conversationSource,model,thinkingEffort,plugin:plugin.name,pluginSystemHint:plugin.systemHint,streamRequestId:result?.streamRequestId??null,title:document.title,href:location.href};
   }finally{
     workHandoffFilter.restore();
   }
 }catch(e){
   const error=String(e?.stack||e);
   const deleted=error.includes('conversation_deleted');
   const notFound=error.includes('conversation_not_found')||error.includes('not_found');
   return {ok:false,state:deleted?'deleted':notFound?'not_found':'fetch_error',complete:false,conversationId:__CID_JSON__,error};
 }
})()
""";

    const string DeleteJs = """
(async()=>{
 try{
   const conversationId=__CID_JSON__;
   const names=performance.getEntriesByType('resource').map(e=>e.name);
   let appInitial=names.find(n=>/\/assets\/app-initial-[^/]+\.js(?:\?|$)/.test(n))||[...document.scripts].map(s=>s.src).find(n=>/\/assets\/app-initial-[^/]+\.js(?:\?|$)/.test(n));
   if(!appInitial){
     const entry=[...document.scripts].map(s=>s.src).find(n=>/\/assets\/index-[^/]+\.js(?:\?|$)/.test(n));
     if(entry){const source=await(await fetch(entry)).text(),match=source.match(/app-initial-[A-Za-z0-9_-]+\.js/);if(match)appInitial=new URL(match[0],entry).href}
   }
   if(!appInitial)throw new Error('Chat app-initial module not found');
   const m=await import(appInitial);

   const deleteFn=Object.values(m).find(v=>{
     if(typeof v!=='function')return false;
     let src='';
     try{src=String(v)}catch{return false}
     return src.includes('/conversation/id/{conversation_id}')&&
            src.includes('conversation_deleted')&&
            src.includes('safeDelete');
   });
   if(typeof deleteFn!=='function')throw new Error('Chat delete function not found');

   const rootEl=document.getElementById('root'),rk=rootEl&&Object.getOwnPropertyNames(rootEl).find(k=>k.startsWith('__reactContainer$')),root=rk?rootEl[rk]:null;
   const isScope=o=>o&&typeof o==='object'&&typeof o.get==='function'&&typeof o.set==='function'&&o.queryClient;
   let scope=null,stack=root?[root]:[],seen=new Set();
   while(stack.length&&!scope){const f=stack.pop();if(!f||typeof f!=='object'||seen.has(f))continue;seen.add(f);const vals=[];if(f.memoizedProps&&typeof f.memoizedProps==='object')vals.push(...Object.values(f.memoizedProps));let h=f.memoizedState,i=0;while(h&&i++<30){vals.push(h.memoizedState);if(h.memoizedState&&typeof h.memoizedState==='object')vals.push(...Object.values(h.memoizedState));h=h.next}let c=f.dependencies?.firstContext,j=0;while(c&&j++<30){vals.push(c.memoizedValue);c=c.next}scope=vals.find(isScope)||null;if(f.child)stack.push(f.child);if(f.sibling)stack.push(f.sibling)}
   if(!scope)throw new Error('ChatGPT internal scope not found');

   await deleteFn({scope,conversationId});
   return {ok:true,state:'deleted',conversationId};
 }catch(e){
   const error=String(e?.stack||e);
   const alreadyDeleted=error.includes('conversation_deleted');
   const notFound=error.includes('conversation_not_found')||error.includes('not_found')||/\b404\b/.test(error);
   const transient=error.includes('Too many requests')||error.includes('429')||error.includes('rate_limit');
   if(alreadyDeleted)return{ok:true,state:'deleted',conversationId};
   return {ok:false,state:notFound?'not_found':transient?'transient_error':'delete_error',conversationId,error};
 }
})()
""";

    const string ReadJs = """
(async()=>{
 try{
   const cid=__CID_JSON__,afterNodeId=__AFTER_NODE_JSON__;
   const names=performance.getEntriesByType('resource').map(e=>e.name);
   let appInitial=names.find(n=>/\/assets\/app-initial-[^/]+\.js(?:\?|$)/.test(n))||[...document.scripts].map(s=>s.src).find(n=>/\/assets\/app-initial-[^/]+\.js(?:\?|$)/.test(n));
   if(!appInitial){
     const entry=[...document.scripts].map(s=>s.src).find(n=>/\/assets\/index-[^/]+\.js(?:\?|$)/.test(n));
     if(entry){const source=await(await fetch(entry)).text(),match=source.match(/app-initial-[A-Za-z0-9_-]+\.js/);if(match)appInitial=new URL(match[0],entry).href}
   }
   if(!appInitial)throw new Error('Chat app-initial module not found');
   const m=await import(appInitial);
   const functionSource=v=>{if(typeof v!=='function')return'';try{return String(v)}catch{return''}};
   const fetchFn=Object.values(m).find(v=>{const s=functionSource(v);return s.includes('/conversations/{conversation_id}')&&s.includes('/conversation/{conversation_id}')&&s.includes('Conversation pagination cursor did not advance')})??m.azt;
   if(typeof fetchFn!=='function')throw new Error('Chat conversation fetch function not found');

   const rootEl=document.getElementById('root'),rk=rootEl&&Object.getOwnPropertyNames(rootEl).find(k=>k.startsWith('__reactContainer$')),root=rk?rootEl[rk]:null;
   const isScope=o=>o&&typeof o==='object'&&typeof o.get==='function'&&typeof o.set==='function'&&o.queryClient;
   let scope=null,stack=root?[root]:[],seen=new Set();
   while(stack.length&&!scope){const f=stack.pop();if(!f||typeof f!=='object'||seen.has(f))continue;seen.add(f);const vals=[];if(f.memoizedProps&&typeof f.memoizedProps==='object')vals.push(...Object.values(f.memoizedProps));let h=f.memoizedState,i=0;while(h&&i++<30){vals.push(h.memoizedState);if(h.memoizedState&&typeof h.memoizedState==='object')vals.push(...Object.values(h.memoizedState));h=h.next}let c=f.dependencies?.firstContext,j=0;while(c&&j++<30){vals.push(c.memoizedValue);c=c.next}scope=vals.find(isScope)||null;if(f.child)stack.push(f.child);if(f.sibling)stack.push(f.sibling)}
   if(!scope)throw new Error('ChatGPT internal scope not found');

     const q=scope.queryClient.getQueryCache().find({queryKey:['chatgpt-conversation',cid],exact:true});
   let convo=q?.state?.data??null;
   let conversationSource='cache';
   if(afterNodeId!=null){
     convo=await fetchFn(scope,cid);
     conversationSource=q?.state?.data?'server-refresh':'cold-fetch';
   }else if(!convo){
     convo=await fetchFn(scope,cid);
     conversationSource='cold-fetch';
   }

   const currentNode=convo?.current_node??convo?.currentNode??null;
   const node=currentNode?convo?.mapping?.[currentNode]:null;
   const msg=node?.message??null;
   const role=msg?.author?.role??null;
   const messageStatus=msg?.status??null;
   const endTurn=msg?.end_turn===true;
   const isComplete=msg?.metadata?.is_complete===true;
   const finishType=msg?.metadata?.finish_details?.type??null;

   let text=null;
   const content=msg?.content;
   if(Array.isArray(content?.parts))text=content.parts.filter(x=>typeof x==='string').join('\n');
   else if(typeof content?.text==='string')text=content.text;

   let afterNodeSatisfied=afterNodeId==null;
   if(afterNodeId!=null&&currentNode!=null&&currentNode!==afterNodeId){
     let walk=currentNode,guard=0;
     while(walk&&guard++<10000){
       if(walk===afterNodeId){afterNodeSatisfied=true;break}
       walk=convo?.mapping?.[walk]?.parent??null;
     }
   }

   const complete=afterNodeSatisfied&&role==='assistant'&&messageStatus==='finished_successfully'&&endTurn&&isComplete;
   const lowerStatus=String(messageStatus||'').toLowerCase();
   const lowerFinish=String(finishType||'').toLowerCase();
   const failureLike=/fail|error|cancel|interrupt/.test(lowerStatus)||['error','cancelled','interrupted'].includes(lowerFinish);

   let state='pending';
   if(complete)state='complete';
   else if(failureLike)state='failed';

   return {ok:true,state,complete,conversationId:cid,title:convo?.title??null,currentNode,afterNodeId,afterNodeSatisfied,role,messageStatus,endTurn,isComplete,finishType,contentType:content?.content_type??null,text,modelSlug:msg?.metadata?.model_slug??msg?.metadata?.resolved_model_slug??null,thinkingEffort:msg?.metadata?.thinking_effort??null,requestId:msg?.metadata?.request_id??null,turnExchangeId:msg?.metadata?.turn_exchange_id??null,workingTurnId:msg?.metadata?.working_turn_id??null,createTime:msg?.create_time??null,updateTime:msg?.update_time??null,documentTitle:document.title,conversationSource};
 }catch(e){
   const error=String(e?.stack||e);
   const deleted=error.includes('conversation_deleted');
   const notFound=error.includes('conversation_not_found')||error.includes('not_found');
   const transient=error.includes('Too many requests')||error.includes('429')||error.includes('rate_limit');
   return {ok:false,state:deleted?'deleted':notFound?'not_found':transient?'transient_error':'fetch_error',complete:false,conversationId:__CID_JSON__,error};
 }
})()
""";
}

record ReadOutcome(JsonObject Data, int ExitCode);

static class CdpTransport
{
    static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public static async Task<bool> IsReadyAsync(int port)
    {
        try
        {
            using var response = await Http.GetAsync($"http://127.0.0.1:{port}/json/version");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<JsonObject> EvaluateObjectAsync(int port, string expression, TimeSpan timeout)
    {
        var target = await FindMainTargetAsync(port);
        using var ws = new ClientWebSocket();
        using var cts = new CancellationTokenSource(timeout);

        await ws.ConnectAsync(new Uri(target.WebSocketDebuggerUrl), cts.Token);

        var request = new JsonObject
        {
            ["id"] = 1,
            ["method"] = "Runtime.evaluate",
            ["params"] = new JsonObject
            {
                ["expression"] = expression,
                ["awaitPromise"] = true,
                ["returnByValue"] = true
            }
        };

        var bytes = Encoding.UTF8.GetBytes(request.ToJsonString());
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, cts.Token);

        while (ws.State == WebSocketState.Open)
        {
            var text = await ReceiveMessageAsync(ws, cts.Token);
            var root = JsonNode.Parse(text)?.AsObject()
                       ?? throw new InvalidOperationException("Invalid CDP JSON response.");

            if (root["id"]?.GetValue<int?>() != 1)
                continue;

            var exception = root["result"]?["exceptionDetails"];
            if (exception is not null)
            {
                var description = exception["exception"]?["description"]?.GetValue<string>()
                                  ?? exception["text"]?.GetValue<string>()
                                  ?? "Unknown renderer error";
                throw new InvalidOperationException($"ChatGPT renderer error: {description}");
            }

            var value = root["result"]?["result"]?["value"];
            if (value is JsonObject obj)
                return (JsonObject)obj.DeepClone();

            if (value is JsonValue jv && jv.TryGetValue<string>(out var s))
            {
                var parsed = JsonNode.Parse(s);
                if (parsed is JsonObject parsedObj)
                    return parsedObj;
            }

            throw new InvalidOperationException(
                $"CDP expression did not return an object. Raw response: {text}");
        }

        throw new InvalidOperationException("CDP WebSocket disconnected.");
    }

    static async Task<TargetInfo> FindMainTargetAsync(int port)
    {
        var json = await Http.GetStringAsync($"http://127.0.0.1:{port}/json/list");
        var arr = JsonNode.Parse(json)?.AsArray()
                  ?? throw new InvalidOperationException("Invalid /json/list response.");

        foreach (var node in arr)
        {
            if (node is not JsonObject o) continue;
            if (o["type"]?.GetValue<string>() == "page" &&
                o["url"]?.GetValue<string>() == "app://-/index.html")
            {
                var ws = o["webSocketDebuggerUrl"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(ws))
                    return new TargetInfo(
                        o["id"]?.GetValue<string>() ?? "",
                        o["title"]?.GetValue<string>() ?? "",
                        ws);
            }
        }

        throw new InvalidOperationException(
            $"ChatGPT main renderer not found on CDP port {port}. Start ChatGPT with remote debugging enabled.");
    }

    static async Task<string> ReceiveMessageAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        using var ms = new MemoryStream();

        while (true)
        {
            var result = await ws.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new InvalidOperationException("CDP WebSocket closed.");
            ms.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
                break;
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    sealed record TargetInfo(string Id, string Title, string WebSocketDebuggerUrl);
}

[Flags]
enum ActivateOptions
{
    None = 0,
    DesignMode = 1,
    NoErrorUI = 2,
    NoSplashScreen = 4
}

[ComImport]
[Guid("2e941141-7f97-4756-ba1d-9decde894a3d")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IApplicationActivationManager
{
    int ActivateApplication(
        [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
        [MarshalAs(UnmanagedType.LPWStr)] string arguments,
        ActivateOptions options,
        out uint processId);

    int ActivateForFile(IntPtr appUserModelId, IntPtr itemArray, IntPtr verb, out uint processId);
    int ActivateForProtocol(IntPtr appUserModelId, IntPtr itemArray, out uint processId);
}

[ComImport]
[Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
class ApplicationActivationManager
{
}

static class AppxLauncher
{
    public static uint Launch(string appId, string args)
    {
        var manager = (IApplicationActivationManager)new ApplicationActivationManager();
        var hr = manager.ActivateApplication(appId, args, ActivateOptions.None, out var pid);
        Marshal.ThrowExceptionForHR(hr);
        return pid;
    }
}

sealed class CliException(string message) : Exception(message);
