using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace TestBackendService.Controllers;

[ApiController]
[Route("util")]
public class UtilityController : ControllerBase
{
    private static readonly List<byte[]> _allocations = [];
    private readonly ILogger<UtilityController> _logger;

    public UtilityController(ILogger<UtilityController> logger)
    {
        _logger = logger;
    }

    // GET /util/ping
    [HttpGet("ping")]
    public ActionResult<object> Ping() => Ok(new { message = "pong", timeUtc = DateTime.UtcNow });

    // GET /util/health
    [HttpGet("health")]
    public ActionResult<object> Health() => Ok(new { status = "Healthy", timeUtc = DateTime.UtcNow });

    // GET /util/ready
    [HttpGet("ready")]
    public ActionResult<object> Ready() => Ok(new { status = "Ready" });

    // GET /util/version
    [HttpGet("version")]
    public ActionResult<object> Version()
    {
        var asm = Assembly.GetEntryAssembly() ?? typeof(Program).Assembly;
        var version = asm.GetName().Version?.ToString() ?? "unknown";
        var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return Ok(new { version, informational });
    }

    // GET /util/env
    [HttpGet("env")]
    public ActionResult<IDictionary<string, string>> Env()
    {
        var dict = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(kvp => (string)kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
        return Ok(dict);
    }

    // GET /util/whoami
    [HttpGet("whoami")]
    public ActionResult<object> WhoAmI()
    {
        var host = Dns.GetHostName();
        var addresses = Dns.GetHostAddresses(host).Select(a => a.ToString()).ToArray();
        return Ok(new { host, addresses, processId = Environment.ProcessId });
    }

    // POST /util/echo (echoes JSON body)
    [HttpPost("echo")]
    public ActionResult<object> Echo([FromBody] object body)
    {
        return Ok(new { receivedAtUtc = DateTime.UtcNow, body });
    }

    // GET /util/delay/1000 (delay in ms)
    [HttpGet("delay/{ms:int}")]
    public async Task<ActionResult<object>> Delay(int ms, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(ms, ct);
        sw.Stop();
        return Ok(new { requestedMs = ms, elapsedMs = sw.ElapsedMilliseconds });
    }

    // GET /util/error/503
    [HttpGet("error/{code:int}")]
    public ActionResult<object> Error(int code)
    {
        var clamped = Math.Clamp(code, 100, 599);
        return StatusCode(clamped, new { error = $"Generated error {clamped}" });
    }

    // GET /util/headers
    [HttpGet("headers")]
    public ActionResult<IDictionary<string, string>> Headers()
    {
        var headers = Request.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
        return Ok(headers);
    }

    // GET /util/files?path=relative/path
    // This inspects files under a root directory (default ./data or DATA_ROOT env).
    [HttpGet("files")]
    public ActionResult<object> Files([FromQuery] string? path = null)
    {
        var root = Environment.GetEnvironmentVariable("DATA_ROOT");
        root = string.IsNullOrWhiteSpace(root) ? Path.Combine(AppContext.BaseDirectory, "data") : root;

        var target = string.IsNullOrEmpty(path) ? root : Path.GetFullPath(Path.Combine(root, path));

        // prevent path traversal outside of root
        var fullRoot = Path.GetFullPath(root);
        if (!target.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Path is outside of allowed root" });

        if (Directory.Exists(target))
        {
            var entries = Directory.EnumerateFileSystemEntries(target)
                .Select(p => new
                {
                    name = Path.GetFileName(p),
                    path = Path.GetRelativePath(fullRoot, p),
                    type = System.IO.File.Exists(p) ? "file" : "dir",
                    size = System.IO.File.Exists(p) ? (long?)new FileInfo(p).Length : null
                }).ToArray();
            return Ok(new { root = fullRoot, target = target, entries });
        }
        if (System.IO.File.Exists(target))
        {
            var info = new FileInfo(target);
            string content;
            using (var stream = info.OpenText())
            {
                content = stream.ReadToEnd();
            }
            // limit size returned
            var truncated = content.Length > 16_000 ? content[..16_000] + "..." : content;
            return Ok(new { root = fullRoot, target, size = info.Length, content = truncated });
        }
        return NotFound(new { error = "Path not found", root = fullRoot, target });
    }

    // POST /util/allocate/50 (MB) - allocates memory for testing, POST /util/clearallocations to free
    [HttpPost("allocate/{mb:int}")]
    public ActionResult<object> Allocate(int mb)
    {
        mb = Math.Clamp(mb, 1, 1024);
        var bytes = mb * 1024 * 1024;
        var buffer = GC.AllocateUninitializedArray<byte>(bytes, pinned: false);
        // Touch memory so it's committed
        for (var i = 0; i < buffer.Length; i += 4096) buffer[i] = 1;
        _allocations.Add(buffer);
        _logger.LogInformation("Allocated {MB} MB, total chunks: {Count}", mb, _allocations.Count);
        return Ok(new { allocatedMb = mb, chunks = _allocations.Count, totalMb = _allocations.Sum(b => b.Length) / (1024 * 1024) });
    }

    [HttpPost("clearallocations")]
    public ActionResult<object> ClearAllocations()
    {
        var total = _allocations.Sum(b => b.Length) / (1024 * 1024);
        _allocations.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        return Ok(new { cleared = true, freedMb = total });
    }
}
