# Live §8.4 security-matrix walk against a running Khidma API.
# Creates its own request/offer/booking data. Never prints passwords.
#
# Environment:
#   KHIDMA_BASE_URL
#   KHIDMA_ADMIN_EMAIL / KHIDMA_ADMIN_PASSWORD
#   KHIDMA_CUSTOMER_EMAIL / KHIDMA_CUSTOMER_PASSWORD
#   KHIDMA_PROVIDER_EMAIL / KHIDMA_PROVIDER_PASSWORD     (Provider A, eligible)
#   KHIDMA_PROVIDER_B_EMAIL / KHIDMA_PROVIDER_B_PASSWORD (Provider B, ineligible city)

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

function Get-StatusCode {
    param($ErrorRecord)
    return [int]$ErrorRecord.Exception.Response.StatusCode.value__
}

function Get-ErrorBody {
    param($ErrorRecord)
    try {
        $stream = $ErrorRecord.Exception.Response.GetResponseStream()
        if (-not $stream) {
            return ''
        }
        $reader = New-Object System.IO.StreamReader($stream)
        return $reader.ReadToEnd()
    }
    catch {
        return ''
    }
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

function Assert-NoCustomerContact {
    param([string]$Json)
    if ($Json -match '(?i)"(email|phone|phoneNumber|contact|fullName|defaultContact)"') {
        throw 'Provider-facing JSON contained a customer contact field.'
    }
}

if ($PSVersionTable.PSVersion.Major -lt 6) {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
}

$adminEmail = Get-SecretValue 'KHIDMA_ADMIN_EMAIL' 'Admin email'
$adminPassword = Get-SecretValue 'KHIDMA_ADMIN_PASSWORD' 'Admin password'
$customerEmail = Get-SecretValue 'KHIDMA_CUSTOMER_EMAIL' 'Customer email'
$customerPassword = Get-SecretValue 'KHIDMA_CUSTOMER_PASSWORD' 'Customer password'
$providerEmail = Get-SecretValue 'KHIDMA_PROVIDER_EMAIL' 'Provider A email'
$providerPassword = Get-SecretValue 'KHIDMA_PROVIDER_PASSWORD' 'Provider A password'
$providerBEmail = Get-SecretValue 'KHIDMA_PROVIDER_B_EMAIL' 'Provider B email'
$providerBPassword = Get-SecretValue 'KHIDMA_PROVIDER_B_PASSWORD' 'Provider B password'

Write-Host "Khidma security matrix against $script:BaseUrl"
Write-Host ''

$anon = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$anonToken = Get-XsrfToken -Session $anon

Write-Step 'Register role Admin is 400' {
    try {
        $null = Invoke-KhidmaRequest -Method POST -Path '/api/auth/register' -Session $anon -Headers @{
            'X-XSRF-TOKEN' = $anonToken
        } -Body (@{
                fullName = 'Nope'
                email    = "evil-$([guid]::NewGuid().ToString('N'))@khidma.local"
                password = 'ValidPass1!'
                role     = 'Admin'
                city     = 'Ramallah'
            } | ConvertTo-Json)
        throw 'Expected registration to fail.'
    }
    catch {
        if ((Get-StatusCode $_) -ne 400) {
            throw "Expected 400, got $(Get-StatusCode $_)"
        }
    }
}

Write-Step 'Register role admin is 400' {
    try {
        $null = Invoke-KhidmaRequest -Method POST -Path '/api/auth/register' -Session $anon -Headers @{
            'X-XSRF-TOKEN' = $anonToken
        } -Body (@{
                fullName = 'Nope'
                email    = "evil-$([guid]::NewGuid().ToString('N'))@khidma.local"
                password = 'ValidPass1!'
                role     = 'admin'
                city     = 'Ramallah'
            } | ConvertTo-Json)
        throw 'Expected registration to fail.'
    }
    catch {
        if ((Get-StatusCode $_) -ne 400) {
            throw "Expected 400, got $(Get-StatusCode $_)"
        }
    }
}

Write-Step 'Register role " Admin " is 400' {
    try {
        $null = Invoke-KhidmaRequest -Method POST -Path '/api/auth/register' -Session $anon -Headers @{
            'X-XSRF-TOKEN' = $anonToken
        } -Body (@{
                fullName = 'Nope'
                email    = "evil-$([guid]::NewGuid().ToString('N'))@khidma.local"
                password = 'ValidPass1!'
                role     = ' Admin '
                city     = 'Ramallah'
            } | ConvertTo-Json)
        throw 'Expected registration to fail.'
    }
    catch {
        if ((Get-StatusCode $_) -ne 400) {
            throw "Expected 400, got $(Get-StatusCode $_)"
        }
    }
}

$customer = Connect-KhidmaUser -Email $customerEmail -Password $customerPassword
$providerA = Connect-KhidmaUser -Email $providerEmail -Password $providerPassword
$providerB = Connect-KhidmaUser -Email $providerBEmail -Password $providerBPassword
$admin = Connect-KhidmaUser -Email $adminEmail -Password $adminPassword

Write-Step 'Mutation without X-XSRF-TOKEN is 400' {
    try {
        $null = Invoke-KhidmaRequest -Method POST -Path '/api/auth/logout' -Session $customer.Session -Body '{}'
        throw 'Expected CSRF rejection.'
    }
    catch {
        if ((Get-StatusCode $_) -ne 400) {
            throw "Expected 400, got $(Get-StatusCode $_)"
        }
    }
}

$services = (Invoke-KhidmaRequest -Method GET -Path '/api/catalog/services' -Session $customer.Session).Content | ConvertFrom-Json
$plumbing = $services | Where-Object { $_.name -eq 'Plumbing' } | Select-Object -First 1
$meA = (Invoke-KhidmaRequest -Method GET -Path '/api/providers/me' -Session $providerA.Session).Content | ConvertFrom-Json
$preferredDate = [DateTimeOffset]::UtcNow.AddDays(8).ToString('o')

$request = Invoke-KhidmaRequest -Method POST -Path '/api/service-requests' -Session $customer.Session -Headers @{
    'X-XSRF-TOKEN' = $customer.Token
} -Body (@{
        serviceId     = $plumbing.id
        title         = 'Ceiling light not working'
        description   = "The hallway ceiling light flickers and then stays off.`n[$([guid]::NewGuid().ToString('N').Substring(0, 8))]"
        city          = $meA.city
        preferredDate = $preferredDate
        budgetMin     = 40
        budgetMax     = 90
    } | ConvertTo-Json)
$requestId = ($request.Content | ConvertFrom-Json).id

Write-Step 'Provider B list does not include the ineligible request' {
    $available = Invoke-KhidmaRequest -Method GET -Path '/api/service-requests/available' -Session $providerB.Session
    if ($available.StatusCode -ne 200) {
        throw "Expected 200, got $($available.StatusCode)"
    }
    $page = $available.Content | ConvertFrom-Json
    $hit = @($page.items) | Where-Object { $_.id -eq $requestId }
    if ($hit) {
        throw 'Ineligible provider saw the request in /available.'
    }
}

Write-Step 'Provider B direct URL is 404, never 200' {
    try {
        $null = Invoke-KhidmaRequest -Method GET -Path "/api/service-requests/$requestId" -Session $providerB.Session
        throw 'Expected 404.'
    }
    catch {
        $status = Get-StatusCode $_
        if ($status -ne 404) {
            throw "Expected 404, got $status"
        }
    }
}

Write-Step 'Provider B POST offer is 403' {
    try {
        $null = Invoke-KhidmaRequest -Method POST -Path "/api/service-requests/$requestId/offers" -Session $providerB.Session -Headers @{
            'X-XSRF-TOKEN' = $providerB.Token
        } -Body (@{
                price         = 50
                message       = 'I can replace the ceiling fixture this week.'
                estimatedDate = $preferredDate
            } | ConvertTo-Json)
        throw 'Expected 403.'
    }
    catch {
        $status = Get-StatusCode $_
        if ($status -ne 403 -and $status -ne 404) {
            throw "Expected 403 or 404, got $status"
        }
    }
}

$offer = Invoke-KhidmaRequest -Method POST -Path "/api/service-requests/$requestId/offers" -Session $providerA.Session -Headers @{
    'X-XSRF-TOKEN' = $providerA.Token
} -Body (@{
        price         = 75
        message       = 'I can diagnose the fitting and replace the bulb holder.'
        estimatedDate = $preferredDate
    } | ConvertTo-Json)
$offerId = ($offer.Content | ConvertFrom-Json).id

Write-Step 'Provider-facing request JSON has no customer contact' {
    $detail = Invoke-KhidmaRequest -Method GET -Path "/api/service-requests/$requestId" -Session $providerA.Session
    Assert-NoCustomerContact $detail.Content
    $available = Invoke-KhidmaRequest -Method GET -Path '/api/service-requests/available' -Session $providerA.Session
    Assert-NoCustomerContact $available.Content
}

Write-Step 'Provider B cannot withdraw Provider A offer' {
    try {
        $null = Invoke-KhidmaRequest -Method POST -Path "/api/offers/$offerId/withdraw" -Session $providerB.Session -Headers @{
            'X-XSRF-TOKEN' = $providerB.Token
        } -Body '{}'
        throw 'Expected 403.'
    }
    catch {
        $status = Get-StatusCode $_
        if ($status -notin 403, 404) {
            throw "Expected 403 or 404, got $status"
        }
    }
}

$booking = Invoke-KhidmaRequest -Method POST -Path "/api/offers/$offerId/accept" -Session $customer.Session -Headers @{
    'X-XSRF-TOKEN' = $customer.Token
} -Body '{}'
$bookingId = ($booking.Content | ConvertFrom-Json).id

Write-Step 'Provider B cannot read Provider A booking' {
    try {
        $null = Invoke-KhidmaRequest -Method GET -Path "/api/bookings/$bookingId" -Session $providerB.Session
        throw 'Expected 403 or 404.'
    }
    catch {
        $status = Get-StatusCode $_
        if ($status -notin 403, 404) {
            throw "Expected 403 or 404, got $status"
        }
    }
}

Write-Step 'Accept on a Booked request is 409' {
    try {
        $null = Invoke-KhidmaRequest -Method POST -Path "/api/offers/$offerId/accept" -Session $customer.Session -Headers @{
            'X-XSRF-TOKEN' = $customer.Token
        } -Body '{}'
        throw 'Expected 409.'
    }
    catch {
        if ((Get-StatusCode $_) -ne 409) {
            throw "Expected 409, got $(Get-StatusCode $_)"
        }
    }
}

Write-Step 'Complete a Scheduled booking is 409' {
    try {
        $null = Invoke-KhidmaRequest -Method POST -Path "/api/bookings/$bookingId/complete" -Session $providerA.Session -Headers @{
            'X-XSRF-TOKEN' = $providerA.Token
        } -Body '{}'
        throw 'Expected 409.'
    }
    catch {
        if ((Get-StatusCode $_) -ne 409) {
            throw "Expected 409, got $(Get-StatusCode $_)"
        }
    }
}

$null = Invoke-KhidmaRequest -Method POST -Path "/api/bookings/$bookingId/start" -Session $providerA.Session -Headers @{
    'X-XSRF-TOKEN' = $providerA.Token
} -Body '{}'
$null = Invoke-KhidmaRequest -Method POST -Path "/api/bookings/$bookingId/complete" -Session $providerA.Session -Headers @{
    'X-XSRF-TOKEN' = $providerA.Token
} -Body '{}'

Write-Step 'Second review is 409' {
    $body = @{ rating = 5; comment = 'Great work.' } | ConvertTo-Json
    $null = Invoke-KhidmaRequest -Method POST -Path "/api/bookings/$bookingId/review" -Session $customer.Session -Headers @{
        'X-XSRF-TOKEN' = $customer.Token
    } -Body $body
    try {
        $null = Invoke-KhidmaRequest -Method POST -Path "/api/bookings/$bookingId/review" -Session $customer.Session -Headers @{
            'X-XSRF-TOKEN' = $customer.Token
        } -Body $body
        throw 'Expected 409.'
    }
    catch {
        if ((Get-StatusCode $_) -ne 409) {
            throw "Expected 409, got $(Get-StatusCode $_)"
        }
    }
}

Write-Step 'Path traversal upload is 400' {
    $boundary = [guid]::NewGuid().ToString('N')
    $file = [System.Text.Encoding]::ASCII.GetBytes("%PDF-1.4`n")
    $header = [System.Text.Encoding]::ASCII.GetBytes(
        "--$boundary`r`nContent-Disposition: form-data; name=`"documentType`"`r`n`r`nProfessionalCertificate`r`n--$boundary`r`nContent-Disposition: form-data; name=`"file`"; filename=`"..\\..\\secret.pdf`"`r`nContent-Type: application/pdf`r`n`r`n")
    $footer = [System.Text.Encoding]::ASCII.GetBytes("`r`n--$boundary--`r`n")
    $payload = New-Object byte[] ($header.Length + $file.Length + $footer.Length)
    [Array]::Copy($header, 0, $payload, 0, $header.Length)
    [Array]::Copy($file, 0, $payload, $header.Length, $file.Length)
    [Array]::Copy($footer, 0, $payload, $header.Length + $file.Length, $footer.Length)
    try {
        $null = Invoke-KhidmaRequest -Method POST -Path '/api/providers/me/verification-documents' -Session $providerA.Session -Headers @{
            'X-XSRF-TOKEN' = $providerA.Token
        } -Body $payload -ContentType "multipart/form-data; boundary=$boundary"
        throw 'Expected 400.'
    }
    catch {
        if ((Get-StatusCode $_) -ne 400) {
            throw "Expected 400, got $(Get-StatusCode $_)"
        }
    }
}

Write-Host ''
Write-Host "Result: $($script:PassCount) passed, $($script:FailCount) failed."
if ($script:FailCount -gt 0) {
    exit 1
}
