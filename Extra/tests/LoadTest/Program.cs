using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Channels;

if (args.Length != 6 ||
    !int.TryParse(args[1], out int port) || port is < 1 or > 65535 ||
    !int.TryParse(args[2], out int clientCount) || clientCount < 2 || clientCount % 2 != 0 ||
    !int.TryParse(args[3], out int roomRequests) || roomRequests < 1)
{
    Console.WriteLine("Cách dùng: LoadTest <host> <port> <số-client-chẵn> <số-lần-list_rooms> <thư-mục-kết-quả> <nhãn>");
    return 2;
}

string host = args[0];
string outputDirectory = Path.GetFullPath(args[4]);
string label = string.Concat(args[5].Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_'));
Directory.CreateDirectory(outputDirectory);
var clients = new List<LoadClient>();
string runId = Guid.NewGuid().ToString("N")[..6];
string[] clientNames = Enumerable.Range(1, clientCount).Select(i => $"L{runId}{i:0000}").ToArray();
var samples = new List<MetricSample>();
using var sampleStop = new CancellationTokenSource();
Task? sampler = null;
int errors = 0;

try
{
    for (int i = 0; i < clientCount; i++)
    {
        var client = new LoadClient();
        await client.ConnectAsync(host, port);
        clients.Add(client);
        await client.SendAsync(new JsonObject { ["type"] = "hello", ["name"] = clientNames[i], ["token"] = "" });
        JsonObject welcome = await client.WaitForTypeAsync("welcome", TimeSpan.FromSeconds(10));
        if (welcome["name"]?.GetValue<string>() != clientNames[i])
            throw new InvalidOperationException("Server trả tên đăng nhập không khớp.");
    }

    using (Process? serverProcess = FindServerProcess())
        if (serverProcess is null) throw new InvalidOperationException("Không tìm thấy tiến trình CaroServer để đo CPU/RAM. Hãy chạy Server trước.");

    sampler = SampleServerAsync(samples, sampleStop.Token);
    var pairs = Enumerable.Range(0, clientCount / 2)
        .Select(i => PlayShortMatchAsync(clients[i * 2], clients[i * 2 + 1], clientNames[i * 2], clientNames[i * 2 + 1]));
    await Task.WhenAll(pairs);

    foreach (LoadClient client in clients) client.DrainPendingMessages();
    var latencies = new List<double>(roomRequests);
    Stopwatch total = Stopwatch.StartNew();
    for (int i = 0; i < roomRequests; i++)
    {
        LoadClient client = clients[i % clients.Count];
        Stopwatch watch = Stopwatch.StartNew();
        await client.SendAsync(new JsonObject { ["type"] = "list_rooms" });
        await client.WaitForTypeAsync("rooms", TimeSpan.FromSeconds(10));
        watch.Stop();
        latencies.Add(watch.Elapsed.TotalMilliseconds);
    }
    total.Stop();

    sampleStop.Cancel();
    if (sampler is not null) await sampler;
    string summaryPath = Path.Combine(outputDirectory, $"perf_{clientCount}client_{label}.txt");
    string csvPath = Path.Combine(outputDirectory, $"perf_{clientCount}client_{label}.csv");
    await File.WriteAllLinesAsync(csvPath,
        new[] { "timestamp,cpu_percent,ram_mb" }.Concat(samples.Select(s =>
            $"{s.Timestamp:O},{s.CpuPercent.ToString("F2", CultureInfo.InvariantCulture)},{s.RamMb.ToString("F2", CultureInfo.InvariantCulture)}")));

    double[] sorted = latencies.Order().ToArray();
    double average = sorted.Length == 0 ? 0 : sorted.Average();
    double p95 = sorted.Length == 0 ? 0 : sorted[Math.Clamp((int)Math.Ceiling(sorted.Length * .95) - 1, 0, sorted.Length - 1)];
    double rps = total.Elapsed.TotalSeconds <= 0 ? 0 : sorted.Length / total.Elapsed.TotalSeconds;
    double cpu = samples.Count == 0 ? 0 : samples.Average(s => s.CpuPercent);
    double ram = samples.Count == 0 ? 0 : samples.Average(s => s.RamMb);
    string[] summary =
    [
        $"Host: {host}:{port}", $"Clients: {clientCount}", $"list_rooms requests: {sorted.Length}",
        $"Average latency (ms): {average:F2}", $"P95 latency (ms): {p95:F2}",
        $"Throughput (requests/s): {rps:F2}", $"Average CaroServer CPU (% of total machine): {cpu:F2}",
        $"Average CaroServer RAM (MB): {ram:F2}", $"Errors: {errors}",
        $"CPU logical processors: {Environment.ProcessorCount}", $"CSV: {Path.GetFileName(csvPath)}"
    ];
    await File.WriteAllLinesAsync(summaryPath, summary, Encoding.UTF8);
    Console.WriteLine(string.Join(Environment.NewLine, summary));
    Console.WriteLine($"Đã lưu kết quả: {summaryPath}");
    return errors == 0 ? 0 : 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Load test thất bại: {ex.Message}");
    return 1;
}
finally
{
    sampleStop.Cancel();
    foreach (LoadClient client in clients) await client.DisposeAsync();
}

static async Task PlayShortMatchAsync(LoadClient playerX, LoadClient playerO, string nameX, string nameO)
{
    await playerX.SendAsync(new JsonObject { ["type"] = "invite", ["target"] = nameO });
    await playerO.WaitForTypeAsync("invite_received", TimeSpan.FromSeconds(10));
    await playerO.SendAsync(new JsonObject { ["type"] = "invite_reply", ["from"] = nameX, ["accept"] = true });
    await playerX.WaitForTypeAsync("invite_result", TimeSpan.FromSeconds(10));
    JsonObject initialX = await playerX.WaitForBoardCountAsync(0, TimeSpan.FromSeconds(10));
    await playerO.WaitForBoardCountAsync(0, TimeSpan.FromSeconds(10));
    int room = initialX["room"]!.GetValue<int>();
    int[][] moves = [[7, 4], [7, 5], [8, 4], [8, 5], [9, 4], [9, 5]];
    for (int i = 0; i < moves.Length; i++)
    {
        LoadClient player = i % 2 == 0 ? playerX : playerO;
        await player.SendAsync(new JsonObject
        {
            ["type"] = "move", ["room"] = room, ["row"] = moves[i][0], ["col"] = moves[i][1]
        });
        await playerX.WaitForBoardCountAsync(i + 1, TimeSpan.FromSeconds(10));
        await playerO.WaitForBoardCountAsync(i + 1, TimeSpan.FromSeconds(10));
    }
}

static async Task SampleServerAsync(List<MetricSample> samples, CancellationToken cancel)
{
    using var process = FindServerProcess();
    TimeSpan priorCpu = process?.TotalProcessorTime ?? TimeSpan.Zero;
    Stopwatch clock = Stopwatch.StartNew();
    if (process is not null && !process.HasExited)
    {
        process.Refresh();
        samples.Add(new MetricSample(DateTimeOffset.Now, 0, process.WorkingSet64 / (1024d * 1024d)));
    }
    try
    {
        while (!cancel.IsCancellationRequested)
        {
            await Task.Delay(200, cancel).ConfigureAwait(false);
            if (process is null || process.HasExited) continue;
            process.Refresh();
            TimeSpan currentCpu = process.TotalProcessorTime;
            double interval = Math.Max(.001, clock.Elapsed.TotalSeconds);
            double cpuPercent = (currentCpu - priorCpu).TotalSeconds / interval / Environment.ProcessorCount * 100;
            samples.Add(new MetricSample(DateTimeOffset.Now, Math.Clamp(cpuPercent, 0, 100), process.WorkingSet64 / (1024d * 1024d)));
            priorCpu = currentCpu;
            clock.Restart();
        }
    }
    catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
}

static Process? FindServerProcess() => Process.GetProcessesByName("CaroServer")
    .OrderByDescending(p => { try { return p.StartTime; } catch { return DateTime.MinValue; } })
    .FirstOrDefault();

sealed record MetricSample(DateTimeOffset Timestamp, double CpuPercent, double RamMb);

sealed class LoadClient : IAsyncDisposable
{
    private readonly TcpClient _tcp = new() { NoDelay = true };
    private readonly Channel<JsonObject> _incoming = Channel.CreateUnbounded<JsonObject>();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private NetworkStream _stream = null!;
    private Task _reader = Task.CompletedTask;

    public async Task ConnectAsync(string host, int port)
    {
        await _tcp.ConnectAsync(host, port);
        _stream = _tcp.GetStream();
        _reader = ReadLoopAsync();
    }

    public async Task SendAsync(JsonObject message)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message.ToJsonString() + "\n");
        await _sendLock.WaitAsync();
        try { await _stream.WriteAsync(bytes); }
        finally { _sendLock.Release(); }
    }

    public async Task<JsonObject> WaitForTypeAsync(string type, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (true)
        {
            JsonObject item = await _incoming.Reader.ReadAsync(cts.Token);
            if (item["type"]?.GetValue<string>() == type) return item;
            if (item["type"]?.GetValue<string>() == "error")
                throw new InvalidOperationException(item["message"]?.GetValue<string>() ?? "Server trả error.");
        }
    }

    public async Task<JsonObject> WaitForBoardCountAsync(int expected, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (true)
        {
            JsonObject item = await _incoming.Reader.ReadAsync(cts.Token);
            if (item["type"]?.GetValue<string>() == "error")
                throw new InvalidOperationException(item["message"]?.GetValue<string>() ?? "Server trả error.");
            if (item["type"]?.GetValue<string>() != "snapshot" || item["board"] is not JsonArray board) continue;
            int occupied = board.Sum(row => (row?.GetValue<string>() ?? "").Count(ch => ch is 'X' or 'O'));
            if (occupied >= expected) return item;
        }
    }

    public void DrainPendingMessages()
    {
        while (_incoming.Reader.TryRead(out _)) { }
    }

    private async Task ReadLoopAsync()
    {
        using var reader = new StreamReader(_stream, new UTF8Encoding(false), false, 4096, leaveOpen: true);
        try
        {
            while (await reader.ReadLineAsync() is { } line)
            {
                if (JsonNode.Parse(line) is JsonObject json) await _incoming.Writer.WriteAsync(json);
            }
            _incoming.Writer.TryComplete(new IOException("Server đã đóng kết nối."));
        }
        catch (Exception ex) when (ex is IOException or SocketException or System.Text.Json.JsonException or ObjectDisposedException)
        { _incoming.Writer.TryComplete(ex); }
    }

    public async ValueTask DisposeAsync()
    {
        _tcp.Dispose();
        try { await _reader; } catch (Exception) { }
        _sendLock.Dispose();
    }
}
