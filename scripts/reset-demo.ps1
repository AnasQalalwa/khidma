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
$CatalogUrl = 'https://localhost:5001/api/catalog/categories'

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
        if ($PSVersionTable.PSVersion.Major -ge 6) {
            $null = Invoke-WebRequest -Uri $CatalogUrl -UseBasicParsing -SkipCertificateCheck -TimeoutSec 5
        }
        else {
            try {
                Add-Type -TypeDefinition @'
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class KhidmaTrustAllCerts : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest req, int problem) {
        return true;
    }
}
'@
            }
            catch {
                # Type already loaded in this session.
            }

            [System.Net.ServicePointManager]::CertificatePolicy = New-Object KhidmaTrustAllCerts
            [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
            $null = Invoke-WebRequest -Uri $CatalogUrl -UseBasicParsing -TimeoutSec 5
        }

        return $true
    }
    catch {
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

Write-Host 'Starting the API once so the Development seeder runs...'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$run = Start-Process -FilePath 'dotnet' -ArgumentList @(
    'run',
    '--project', $ApiProject,
    '--launch-profile', 'https'
) -WorkingDirectory $RepoRoot -PassThru -WindowStyle Hidden

try {
    $ready = $false
    for ($attempt = 1; $attempt -le 45; $attempt++) {
        if ($run.HasExited) {
            throw "API exited during seed (exit $($run.ExitCode)). Check that user-secrets include ConnectionStrings:Default and Seed:* passwords."
        }

        Start-Sleep -Seconds 2
        if (Test-LocalApi) {
            $ready = $true
            break
        }
    }

    if (-not $ready) {
        throw 'API did not become ready on https://localhost:5001 after seeding.'
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
