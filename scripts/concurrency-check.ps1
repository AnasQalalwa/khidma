# Concurrent offer-accept check against a running SQL Server-backed Khidma API.
# Starts two overlapping accept requests and expects one 200, one 409, and a single booking.
# Never prints passwords.
#
# Environment:
#   KHIDMA_BASE_URL
#   KHIDMA_CUSTOMER_EMAIL / KHIDMA_CUSTOMER_PASSWORD
#   KHIDMA_PROVIDER_EMAIL / KHIDMA_PROVIDER_PASSWORD

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$BaseUrl = if ($env:KHIDMA_BASE_URL) { $env:KHIDMA_BASE_URL.TrimEnd('/') } else { 'https://localhost:5001' }

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
        $Body
    )

    $params = @{
        Method          = $Method
        Uri             = "$BaseUrl$Path"
        WebSession      = $Session
        UseBasicParsing = $true
    }

    if ($Headers) {
        $params.Headers = $Headers
    }

    if ($null -ne $Body) {
        $params.Body = $Body
        $params.ContentType = 'application/json'
    }

    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $params.SkipCertificateCheck = $true
    }

    return Invoke-WebRequest @params
}

function Get-XsrfToken {
    param([Microsoft.PowerShell.Commands.WebRequestSession]$Session)

    $null = Invoke-KhidmaRequest -Method GET -Path '/api/antiforgery/token' -Session $Session
    $cookie = $Session.Cookies.GetCookies($BaseUrl) | Where-Object { $_.Name -eq 'XSRF-TOKEN' } | Select-Object -First 1
    if (-not $cookie) {
        throw 'XSRF-TOKEN cookie was not set.'
    }

    return $cookie.Value
}

function Connect-KhidmaUser {
    param(
        [Parameter(Mandatory = $true)][string]$Email,
        [Parameter(Mandatory = $true)][string]$Password
    )

    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $token = Get-XsrfToken -Session $session
    $login = Invoke-KhidmaRequest -Method POST -Path '/api/auth/login' -Session $session -Headers @{
        'X-XSRF-TOKEN' = $token
    } -Body (@{ email = $Email; password = $Password } | ConvertTo-Json)

    if ($login.StatusCode -ne 200) {
        throw 'Login failed.'
    }

    return @{
        Session = $session
        Token   = Get-XsrfToken -Session $session
    }
}

if ($PSVersionTable.PSVersion.Major -lt 6) {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
}

$customerEmail = Get-SecretValue 'KHIDMA_CUSTOMER_EMAIL' 'Customer email'
$customerPassword = Get-SecretValue 'KHIDMA_CUSTOMER_PASSWORD' 'Customer password'
$providerEmail = Get-SecretValue 'KHIDMA_PROVIDER_EMAIL' 'Provider email'
$providerPassword = Get-SecretValue 'KHIDMA_PROVIDER_PASSWORD' 'Provider password'

Write-Host "Khidma concurrency check against $BaseUrl"

$customer = Connect-KhidmaUser -Email $customerEmail -Password $customerPassword
$provider = Connect-KhidmaUser -Email $providerEmail -Password $providerPassword

$services = (Invoke-KhidmaRequest -Method GET -Path '/api/catalog/services' -Session $customer.Session).Content | ConvertFrom-Json
$plumbing = $services | Where-Object { $_.name -eq 'Plumbing' } | Select-Object -First 1
if (-not $plumbing) {
    throw 'Plumbing service was not found in the catalog.'
}

$me = (Invoke-KhidmaRequest -Method GET -Path '/api/providers/me' -Session $provider.Session).Content | ConvertFrom-Json
$preferredDate = [DateTimeOffset]::UtcNow.AddDays(8).ToString('o')
$city = $me.city

$requestBody = @{
    serviceId     = $plumbing.id
    title         = "Concurrency check $(Get-Date -Format 'yyyyMMddHHmmss')"
    description   = 'Temporary request used by scripts/concurrency-check.ps1.'
    city          = $city
    preferredDate = $preferredDate
    budgetMin     = 40
    budgetMax     = 90
} | ConvertTo-Json

$request = Invoke-KhidmaRequest -Method POST -Path '/api/service-requests' -Session $customer.Session -Headers @{
    'X-XSRF-TOKEN' = $customer.Token
} -Body $requestBody
$requestId = ($request.Content | ConvertFrom-Json).id

$offer = Invoke-KhidmaRequest -Method POST -Path "/api/service-requests/$requestId/offers" -Session $provider.Session -Headers @{
    'X-XSRF-TOKEN' = $provider.Token
} -Body (@{
        price         = 75
        message       = 'Concurrency probe offer.'
        estimatedDate = $preferredDate
    } | ConvertTo-Json)

if ($offer.StatusCode -ne 200) {
    throw "Offer submit failed with $($offer.StatusCode). The provider must be approved and not suspended."
}

$offerId = ($offer.Content | ConvertFrom-Json).id
$acceptUri = "$BaseUrl/api/offers/$offerId/accept"
$cookieHeader = ($customer.Session.Cookies.GetCookies($BaseUrl) | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join '; '

$jobScript = {
    param($Uri, $CookieHeader, $Token, $SkipCert)
    if ($SkipCert -and $PSVersionTable.PSVersion.Major -ge 6) {
        return Invoke-WebRequest -Method POST -Uri $Uri -Headers @{
            'X-XSRF-TOKEN' = $Token
            Cookie         = $CookieHeader
        } -Body '{}' -ContentType 'application/json' -SkipCertificateCheck -UseBasicParsing
    }

    return Invoke-WebRequest -Method POST -Uri $Uri -Headers @{
        'X-XSRF-TOKEN' = $Token
        Cookie         = $CookieHeader
    } -Body '{}' -ContentType 'application/json' -UseBasicParsing
}

$skipCert = $PSVersionTable.PSVersion.Major -ge 6
$job1 = Start-Job -ScriptBlock $jobScript -ArgumentList $acceptUri, $cookieHeader, $customer.Token, $skipCert
$job2 = Start-Job -ScriptBlock $jobScript -ArgumentList $acceptUri, $cookieHeader, $customer.Token, $skipCert
$results = $job1, $job2 | Wait-Job | Receive-Job
$job1, $job2 | Remove-Job -Force

$statuses = @()
foreach ($result in $results) {
    if ($result -is [System.Management.Automation.ErrorRecord]) {
        $response = $result.Exception.Response
        if ($response) {
            $statuses += [int]$response.StatusCode
        }
        else {
            throw $result
        }
    }
    else {
        $statuses += [int]$result.StatusCode
    }
}

$statuses = $statuses | Sort-Object
Write-Host ("Accept statuses: " + ($statuses -join ', '))

if ($statuses -notcontains 200 -or $statuses -notcontains 409) {
    throw "Expected 200 and 409, got $($statuses -join ', ')."
}

$bookings = (Invoke-KhidmaRequest -Method GET -Path '/api/bookings/mine' -Session $customer.Session).Content | ConvertFrom-Json
$matches = @($bookings.items | Where-Object { $_.serviceRequestId -eq $requestId })
if ($matches.Count -ne 1) {
    throw "Expected exactly one booking for the request, found $($matches.Count)."
}

Write-Host 'PASS  Concurrent accept produced 200 + 409 and a single booking.'
