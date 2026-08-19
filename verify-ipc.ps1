$ErrorActionPreference = 'Stop'
$exe = 'E:\Data\Hermes File\projects\PowerProfile\PowerProfile.App\bin\Debug\net10.0-windows\PowerProfile.exe'
$p = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -PassThru
try {
    $client = [System.IO.Pipes.NamedPipeClientStream]::new('.', 'PowerProfile.LocalApi.v1', [System.IO.Pipes.PipeDirection]::InOut)
    $client.Connect(10000)
    $writer = [System.IO.StreamWriter]::new($client)
    $writer.AutoFlush = $true
    $reader = [System.IO.StreamReader]::new($client)
    $writer.WriteLine('{"id":"smoke","version":1,"command":"getState","payload":{}}')
    $response = $reader.ReadLine()
    if ([string]::IsNullOrWhiteSpace($response)) { throw 'empty IPC response' }
    Write-Output $response
    if ($response -notmatch '"ok":true') { throw 'IPC response was not successful' }
    $client.Dispose()
} finally {
    if ($p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force }
}
