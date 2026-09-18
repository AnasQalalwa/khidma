# Drop and recreate the local Development database, then run the API seeder.
# Never prints passwords. Run this before a live demo and before capturing screenshots.
#
# Requires: .NET SDK, dotnet-ef (dotnet tool restore), ConnectionStrings:Default and
# Seed:AdminPassword / Seed:DemoPassword in Khidma.Api user-secrets.
# Stop any running API first; this script also tries to free ports 5000/5001.

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ApiProject = Join-Path $RepoRoot 'server\Khidma.Api'

function Stop-LocalKhidmaListeners {
    foreach ($port in 5000, 5001) {
        $listeners = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
        foreach ($listener in @($listeners)) {
            $proc = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
            if ($proc) {
                Write-Host "Stopping $($proc.ProcessName) (PID $($proc.Id)) on port $port"
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            }
        }
    }

    Start-Sleep -Seconds 1
}

function Test-LocalApi {
    try {
        $null = Invoke-WebRequest -Uri 'http://localhost:5000/api/catalog/categories' -UseBasicParsing -TimeoutSec 5 -MaximumRedirection 0
        return $true
    }
    catch {
        $status = 0
        try {
            $status = [int]$_.Exception.Response.StatusCode
        }
        catch {
            $status = 0
        }

        if ($status -ge 200 -and $status -lt 400) {
            return $true
        }

        if ($_.Exception.Message -match 'redirect') {
            return $true
        }

        return $false
    }
}

function Invoke-DotnetEf {
    param([Parameter(Mandatory = $true)][string[]]$EfArgs)

    & dotnet ef @EfArgs --project $ApiProject --startup-project $ApiProject
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef $($EfArgs -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Write-Host "Khidma demo reset from $RepoRoot"
Write-Host ''

Set-Location $RepoRoot
Stop-LocalKhidmaListeners

Write-Host 'Restoring .NET tools (dotnet-ef)...'
dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    throw 'dotnet tool restore failed.'
}

Write-Host 'Dropping Development database...'
& dotnet ef database drop -f --project $ApiProject --startup-project $ApiProject
if ($LASTEXITCODE -ne 0) {
    Write-Warning 'database drop reported an error; continuing with update in case the database was already gone.'
}

Write-Host 'Applying migrations...'
Invoke-DotnetEf -EfArgs @('database', 'update')

Write-Host 'Building the API...'
dotnet build $ApiProject -nologo
if ($LASTEXITCODE -ne 0) {
    throw 'dotnet build failed.'
}

Write-Host 'Starting the API once so the Development seeder runs...'
$stdoutLog = Join-Path $env:TEMP 'khidma-reset-demo.out.log'
$stderrLog = Join-Path $env:TEMP 'khidma-reset-demo.err.log'
Remove-Item $stdoutLog, $stderrLog -ErrorAction SilentlyContinue
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$run = Start-Process -FilePath 'dotnet' -ArgumentList @(
    'run',
    '--project', $ApiProject,
    '--launch-profile', 'https',
    '--no-build'
) -WorkingDirectory $RepoRoot -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog

function Get-SeederLog {
    $chunks = @()
    if (Test-Path $stdoutLog) {
        $chunks += Get-Content $stdoutLog -Raw -ErrorAction SilentlyContinue
    }
    if (Test-Path $stderrLog) {
        $chunks += Get-Content $stderrLog -Raw -ErrorAction SilentlyContinue
    }

    return ($chunks -join [Environment]::NewLine)
}

try {
    $ready = $false
    for ($attempt = 1; $attempt -le 90; $attempt++) {
        $run.Refresh()
        if ($run.HasExited) {
            throw "API exited during seed (exit $($run.ExitCode)). $(Get-SeederLog)"
        }

        $log = Get-SeederLog
        if ($log -match 'Application started') {
            $ready = $true
            break
        }

        if (Test-LocalApi) {
            $ready = $true
            break
        }

        Start-Sleep -Seconds 2
    }

    if (-not $ready) {
        throw "API did not become ready after seeding.`n$(Get-SeederLog)"
    }

    Write-Host 'Seeder finished.'
}
finally {
    if (-not $run.HasExited) {
        Write-Host "Stopping seeder API (PID $($run.Id))..."
        Stop-Process -Id $run.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
        Stop-LocalKhidmaListeners
    }
}

Write-Host ''
Write-Host 'Demo accounts (passwords are Seed:* user-secrets, never printed):'
Write-Host ''
Write-Host '  Email                       Role        Notes'
Write-Host '  --------------------------  ----------  -----------------------------------------------'
Write-Host '  admin@khidma.local          Admin       Catalog, verification, users, audit'
Write-Host '  customer@khidma.local       Customer    Ramallah'
Write-Host '  customer2@khidma.local      Customer    Nablus'
Write-Host '  provider1@khidma.local      Provider    Ramallah, Approved, Home Services'
Write-Host '  provider2@khidma.local      Provider    Hebron, PendingReview'
Write-Host '  provider3@khidma.local      Provider    Bethlehem, Approved (ineligible for Ramallah Plumbing)'
Write-Host '  provider4@khidma.local      Provider    Ramallah, Approved, Plumbing'
Write-Host ''
Write-Host 'Start the API and Vite when you are ready to demo:'
Write-Host '  dotnet run --project server/Khidma.Api --launch-profile https'
Write-Host '  cd client; npm run dev'
