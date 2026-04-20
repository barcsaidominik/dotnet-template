#Requires -Version 7
<#
.SYNOPSIS
    Starts the backend APIs, refreshes the OpenAPI specs, then regenerates the frontend API clients.
.DESCRIPTION
    1. Builds backend/Template.slnx
    2. Starts Template.Api        (http://localhost:5281)
    3. Starts Template.Products.Api  (http://localhost:7135)
    4. Waits until both /openapi/v1.json endpoints respond
    5. Saves raw JSON to backend/Api.json and backend/ProductsApi.json
    6. Runs npm run generate:api in the frontend directory
    7. Stops both APIs
.EXAMPLE
    pwsh .\scripts\Refresh-OpenApi.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot    = Split-Path -Parent $PSScriptRoot
$Backend     = Join-Path $RepoRoot 'backend'
$Frontend    = Join-Path $RepoRoot 'frontend'
$ApiUrl      = 'http://localhost:5281'
$ProdUrl     = 'http://localhost:7135'
$ApiCsproj   = Join-Path $Backend 'src' 'services' 'Template.Api' 'Template.Api.csproj'
$ProdCsproj  = Join-Path $Backend 'src' 'services' 'Template.Products.Api' 'Template.Products.Api.csproj'

$ApiProcess  = $null
$ProdProcess = $null

function Stop-Services {
    foreach ($proc in @($ApiProcess, $ProdProcess) | Where-Object { $_ -ne $null }) {
        if (-not $proc.HasExited) {
            $proc.Kill($true)   # $true = kill entire process tree
            $proc.WaitForExit(3000) | Out-Null
        }
        $proc.Dispose()
    }
}

function Wait-ForSpec {
    param([string]$BaseUrl, [string]$Name)
    $url     = "$BaseUrl/openapi/v1.json"
    $elapsed = 0
    Write-Host "    Waiting for $Name " -NoNewline
    while ($true) {
        try {
            $null = Invoke-WebRequest -Uri $url -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
            Write-Host ' ready' -ForegroundColor Green
            return
        }
        catch {
            Start-Sleep -Seconds 2
            $elapsed += 2
            Write-Host '.' -NoNewline
            if ($elapsed -ge 90) {
                Write-Host ' TIMEOUT' -ForegroundColor Red
                throw "Timed out waiting for $Name at $url"
            }
        }
    }
}

try {
    # ------------------------------------------------------------------ Build
    Write-Host ''
    Write-Host '==> Building backend...' -ForegroundColor Cyan
    Push-Location $Backend
    try {
        dotnet build 'Template.slnx' --nologo -v q
        if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }
    }
    finally {
        Pop-Location
    }

    # ---------------------------------------------------------- Start services
    Write-Host '==> Starting services...' -ForegroundColor Cyan

    $startArgs = @{
        FilePath    = 'dotnet'
        PassThru    = $true
        WindowStyle = 'Hidden'
    }

    $ApiProcess  = Start-Process @startArgs -ArgumentList "run --project `"$ApiCsproj`" --launch-profile http --no-build"
    $ProdProcess = Start-Process @startArgs -ArgumentList "run --project `"$ProdCsproj`" --launch-profile `"Template.Products.Api`" --no-build"

    # -------------------------------------------------------------- Wait loop
    Wait-ForSpec -BaseUrl $ApiUrl  -Name 'Template.Api'
    Wait-ForSpec -BaseUrl $ProdUrl -Name 'Template.Products.Api'

    # --------------------------------------------------------------- Fetch specs
    Write-Host ''
    Write-Host '==> Fetching OpenAPI specs...' -ForegroundColor Cyan

    $apiJson  = (Invoke-WebRequest -Uri "$ApiUrl/openapi/v1.json"  -UseBasicParsing).Content
    $prodJson = (Invoke-WebRequest -Uri "$ProdUrl/openapi/v1.json" -UseBasicParsing).Content

    [System.IO.File]::WriteAllText((Join-Path $Backend 'Api.json'),         $apiJson,  [System.Text.Encoding]::UTF8)
    [System.IO.File]::WriteAllText((Join-Path $Backend 'ProductsApi.json'), $prodJson, [System.Text.Encoding]::UTF8)

    Write-Host '    Saved backend/Api.json'
    Write-Host '    Saved backend/ProductsApi.json'

    # -------------------------------------------------------- Generate frontend
    Write-Host ''
    Write-Host '==> Generating frontend API clients...' -ForegroundColor Cyan
    Push-Location $Frontend
    try {
        npm run generate:api
        if ($LASTEXITCODE -ne 0) { throw 'npm run generate:api failed' }
    }
    finally {
        Pop-Location
    }

    Write-Host ''
    Write-Host 'Done! API clients are up to date.' -ForegroundColor Green
}
finally {
    Write-Host ''
    Write-Host '==> Stopping services...' -ForegroundColor Cyan
    Stop-Services
}
