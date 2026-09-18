# Live smoke checks against a running Khidma API.
# Never prints passwords or other secrets.
#
# Environment (optional if you prefer prompts):
#   KHIDMA_BASE_URL          default https://localhost:5001
#   KHIDMA_ADMIN_EMAIL
#   KHIDMA_ADMIN_PASSWORD
#   KHIDMA_CUSTOMER_EMAIL
#   KHIDMA_CUSTOMER_PASSWORD
#   KHIDMA_PROVIDER_EMAIL
#   KHIDMA_PROVIDER_PASSWORD

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:PassCount = 0
$script:FailCount = 0
$script:BaseUrl = if ($env:KHIDMA_BASE_URL) { $env:KHIDMA_BASE_URL.TrimEnd('/') } else { 'https://localhost:5001' }

function Get-SecretValue {
    param(
        [Parameter(Mandatory = $true)][string]$EnvName,
        [Parameter(Mandatory = $true)][string]$Prompt
    )

    $fromEnv = [Environment]::GetEnvironmentVariable($EnvName)
    if (-not [string]::IsNullOrWhiteSpace($fromEnv)) {
        return $fromEnv
    }

    $secure = Read-Host -AsSecureString -Prompt $Prompt
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Invoke-KhidmaRequest {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session,
        [hashtable]$Headers,
        $Body,
        [string]$ContentType = 'application/json'
    )

    $params = @{
        Method          = $Method
        Uri             = "$script:BaseUrl$Path"
        UseBasicParsing = $true
    }

    if ($Session) {
        $params.WebSession = $Session
    }

    if ($Headers) {
        $params.Headers = $Headers
    }

    if ($null -ne $Body) {
        $params.Body = $Body
        $params.ContentType = $ContentType
    }

    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $params.SkipCertificateCheck = $true
    }

    return Invoke-WebRequest @params
}

function Get-XsrfToken {
    param([Microsoft.PowerShell.Commands.WebRequestSession]$Session)

    $null = Invoke-KhidmaRequest -Method GET -Path '/api/antiforgery/token' -Session $Session
    $cookie = $Session.Cookies.GetCookies($script:BaseUrl) | Where-Object { $_.Name -eq 'XSRF-TOKEN' } | Select-Object -First 1
    if (-not $cookie) {
        throw 'XSRF-TOKEN cookie was not set.'
    }

    return $cookie.Value
}

function Write-Step {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )

    try {
        & $Action
        $script:PassCount++
        Write-Host "PASS  $Name"
    }
    catch {
        $script:FailCount++
        $message = $_.Exception.Message
        if ($message -match '(?i)password|secret|token=|connectionstring') {
            $message = 'Step failed (details omitted because they may contain secrets).'
        }
        Write-Host "FAIL  $Name — $message"
    }
}

if ($PSVersionTable.PSVersion.Major -lt 6) {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
}

$adminEmail = Get-SecretValue 'KHIDMA_ADMIN_EMAIL' 'Admin email'
$adminPassword = Get-SecretValue 'KHIDMA_ADMIN_PASSWORD' 'Admin password'
$customerEmail = Get-SecretValue 'KHIDMA_CUSTOMER_EMAIL' 'Customer email'
$customerPassword = Get-SecretValue 'KHIDMA_CUSTOMER_PASSWORD' 'Customer password'
$providerEmail = Get-SecretValue 'KHIDMA_PROVIDER_EMAIL' 'Provider email'
$providerPassword = Get-SecretValue 'KHIDMA_PROVIDER_PASSWORD' 'Provider password'

Write-Host "Khidma smoke test against $script:BaseUrl"
Write-Host ''

Write-Step 'Public catalog is reachable' {
    $response = Invoke-KhidmaRequest -Method GET -Path '/api/catalog/services'
    if ($response.StatusCode -ne 200) {
        throw "Expected 200, got $($response.StatusCode)"
    }
}

$admin = New-Object Microsoft.PowerShell.Commands.WebRequestSession
Write-Step 'Admin login' {
    $token = Get-XsrfToken -Session $admin
    $response = Invoke-KhidmaRequest -Method POST -Path '/api/auth/login' -Session $admin -Headers @{
        'X-XSRF-TOKEN' = $token
    } -Body (@{ email = $adminEmail; password = $adminPassword } | ConvertTo-Json)
    if ($response.StatusCode -ne 200) {
        throw "Expected 200, got $($response.StatusCode)"
    }
}

Write-Step 'Admin stats, users, verifications, and audit logs' {
    foreach ($path in @(
            '/api/admin/stats',
            '/api/admin/users',
            '/api/admin/verifications',
            '/api/admin/providers',
            '/api/admin/audit-logs',
            '/api/admin/audit-logs/summary'
        )) {
        $response = Invoke-KhidmaRequest -Method GET -Path $path -Session $admin
        if ($response.StatusCode -ne 200) {
            throw "$path expected 200, got $($response.StatusCode)"
        }
    }
}

$customer = New-Object Microsoft.PowerShell.Commands.WebRequestSession
Write-Step 'Customer login' {
    $token = Get-XsrfToken -Session $customer
    $response = Invoke-KhidmaRequest -Method POST -Path '/api/auth/login' -Session $customer -Headers @{
        'X-XSRF-TOKEN' = $token
    } -Body (@{ email = $customerEmail; password = $customerPassword } | ConvertTo-Json)
    if ($response.StatusCode -ne 200) {
        throw "Expected 200, got $($response.StatusCode)"
    }
}

Write-Step 'Customer can list own requests' {
    $response = Invoke-KhidmaRequest -Method GET -Path '/api/service-requests/mine' -Session $customer
    if ($response.StatusCode -ne 200) {
        throw "Expected 200, got $($response.StatusCode)"
    }
}

Write-Step 'Login with wrong password is 401 and does not echo the password' {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $token = Get-XsrfToken -Session $session
    try {
        $null = Invoke-KhidmaRequest -Method POST -Path '/api/auth/login' -Session $session -Headers @{
            'X-XSRF-TOKEN' = $token
        } -Body (@{ email = $customerEmail; password = 'not-the-password' } | ConvertTo-Json)
        throw 'Expected login to fail.'
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        if ($status -ne 401) {
            throw "Expected 401, got $status"
        }
        if ($_.Exception.Message -match '(?i)not-the-password') {
            throw 'Failure details leaked the attempted password.'
        }
    }
}

$provider = New-Object Microsoft.PowerShell.Commands.WebRequestSession
Write-Step 'Provider login and verification status' {
    $token = Get-XsrfToken -Session $provider
    $login = Invoke-KhidmaRequest -Method POST -Path '/api/auth/login' -Session $provider -Headers @{
        'X-XSRF-TOKEN' = $token
    } -Body (@{ email = $providerEmail; password = $providerPassword } | ConvertTo-Json)
    if ($login.StatusCode -ne 200) {
        throw "Expected 200, got $($login.StatusCode)"
    }

    $available = Invoke-KhidmaRequest -Method GET -Path '/api/service-requests/available' -Session $provider
    if ($available.StatusCode -ne 200) {
        throw "Available requests expected 200, got $($available.StatusCode)"
    }

    $verification = Invoke-KhidmaRequest -Method GET -Path '/api/providers/me/verification' -Session $provider
    if ($verification.StatusCode -ne 200) {
        throw "Verification expected 200, got $($verification.StatusCode)"
    }
}

Write-Step 'State-changing request without CSRF token is rejected' {
    try {
        $null = Invoke-KhidmaRequest -Method POST -Path '/api/auth/logout' -Session $customer -Body '{}'
        throw 'Expected CSRF rejection.'
    }
    catch {
        var status = $_.Exception.Response.StatusCode.value__
        if ($status -ne 400) {
            throw "Expected 400, got $status"
        }
    }
}

Write-Host ''
Write-Host "Result: $($script:PassCount) passed, $($script:FailCount) failed."
if ($script:FailCount -gt 0) {
    exit 1
}
