param(
    [string]$HostAddress = "127.0.0.1",
    [int]$Port = 45454,
    [Parameter(Mandatory = $true)]
    [ValidateSet("json-sai", "qua-gioi-han", "tach-goi", "gop-goi", "chua-dang-nhap")]
    [string]$Case
)

$ErrorActionPreference = "Stop"
$utf8 = [System.Text.UTF8Encoding]::new($false)
$tcp = [System.Net.Sockets.TcpClient]::new()
$testSuffix = (Get-Random -Minimum 100000 -Maximum 999999).ToString()

function Connect-Server {
    $tcp.Connect($HostAddress, $Port)
    $script:stream = $tcp.GetStream()
    $script:reader = [System.IO.StreamReader]::new($script:stream, $utf8, $false, 1024, $true)
    $script:writer = [System.IO.StreamWriter]::new($script:stream, $utf8, 1024, $true)
    $script:writer.AutoFlush = $true
}

function Send-Line([string]$line) { $script:writer.WriteLine($line) }

function Read-Line([int]$timeoutMs = 5000) {
    $task = $script:reader.ReadLineAsync()
    if (-not $task.Wait($timeoutMs)) { throw "Quá thời gian chờ phản hồi của Server." }
    $line = $task.GetAwaiter().GetResult()
    if ($null -eq $line) { Write-Host "Kết nối đã đóng."; return $null }
    try {
        $message = $line | ConvertFrom-Json
        if ($null -ne $message.message) { Write-Host "$($message.type): $($message.message)" }
        else { Write-Host ($message | ConvertTo-Json -Compress -Depth 10) }
    } catch { Write-Host $line }
    return $line
}

function Read-UntilType([string]$type, [int]$timeoutMs = 5000) {
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    while ($watch.ElapsedMilliseconds -lt $timeoutMs) {
        $remaining = [Math]::Max(100, $timeoutMs - [int]$watch.ElapsedMilliseconds)
        $line = Read-Line $remaining
        if ($null -eq $line) { return }
        try { if (($line | ConvertFrom-Json).type -eq $type) { return } } catch { }
    }
    throw "Không nhận được phản hồi type='$type' trong thời gian cho phép."
}

try {
    Connect-Server
    switch ($Case) {
        "json-sai" {
            Send-Line "{not json}"
            Read-Line | Out-Null
            Send-Line ('{"type":"hello","name":"HuyTest' + $testSuffix + '","token":""}')
            Read-UntilType "welcome"
        }
        "qua-gioi-han" {
            [byte[]]$firstChunk = New-Object byte[] 65536
            [byte[]]$lastChunk = New-Object byte[] 4464
            for ($i = 0; $i -lt $firstChunk.Length; $i++) { $firstChunk[$i] = 65 }
            for ($i = 0; $i -lt $lastChunk.Length; $i++) { $lastChunk[$i] = 65 }
            $script:stream.Write($firstChunk, 0, $firstChunk.Length)
            $script:stream.Flush()
            Start-Sleep -Milliseconds 100
            $script:stream.Write($lastChunk, 0, $lastChunk.Length)
            $script:stream.Flush()
            Read-Line 5000 | Out-Null
            Start-Sleep -Milliseconds 300
            try { Send-Line ('{"type":"hello","name":"HuyOversize' + $testSuffix + '","token":""}') }
            catch { Write-Host "Kết nối bị đóng như mong đợi sau frame quá lớn."; return }
            try {
                $afterClose = Read-Line 1500
                if ($null -ne $afterClose) { throw "Server vẫn nhận dữ liệu sau frame quá giới hạn." }
            } catch {
                if ($_.Exception.Message -like '*Server vẫn nhận dữ liệu*') { throw }
                Write-Host "Socket đã đóng/reset sau frame quá lớn."
            }
            Write-Host "Server đã đóng kết nối sau frame quá giới hạn. Chạy lại bằng Client khác để xác nhận Server tiếp tục nhận kết nối."
        }
        "tach-goi" {
            $json = '{"type":"hello","name":"HuySplit' + $testSuffix + '","token":""}'
            $half = [Math]::Floor($json.Length / 2)
            $first = $utf8.GetBytes($json.Substring(0, $half))
            $second = $utf8.GetBytes($json.Substring($half) + "`n")
            $script:stream.Write($first, 0, $first.Length); $script:stream.Flush()
            Start-Sleep -Milliseconds 300
            $script:stream.Write($second, 0, $second.Length); $script:stream.Flush()
            Read-UntilType "welcome"
        }
        "gop-goi" {
            $joined = '{"type":"hello","name":"HuyBatch' + $testSuffix + '","token":""}' + "`n" + '{"type":"list_rooms"}' + "`n"
            $bytes = $utf8.GetBytes($joined)
            $script:stream.Write($bytes, 0, $bytes.Length); $script:stream.Flush()
            Read-UntilType "welcome"
            Read-UntilType "rooms"
            Read-UntilType "rooms"
        }
        "chua-dang-nhap" {
            Send-Line '{"type":"list_users"}'
            Read-Line | Out-Null
            Send-Line ('{"type":"hello","name":"HuyAuth' + $testSuffix + '","token":""}')
            Read-UntilType "welcome"
            Read-UntilType "history_result"
            Read-UntilType "users"
            Read-UntilType "rooms"
            Send-Line '{"type":"abc"}'
            Read-Line | Out-Null
        }
    }
}
finally {
    if ($null -ne $reader) { $reader.Dispose() }
    if ($null -ne $writer) { $writer.Dispose() }
    $tcp.Dispose()
}
