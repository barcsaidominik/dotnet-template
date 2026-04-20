#Requires -Version 7
<#
.SYNOPSIS
    Starts the backend APIs, refreshes the OpenAPI specs, then regenerates the frontend API clients.
.DESCRIPTION
    1. Starts Template.Api            (http://localhost:5281)
    2. Starts Template.Products.Api   (http://localhost:7135)
    3. Waits until both /openapi/v1.json endpoints respond
    4. Saves raw JSON to backend/Api.json and backend/ProductsApi.json
    5. Runs npm run generate:api in the frontend directory
    6. Stops both APIs

    The script injects development-safe fallback configuration for startup-only requirements
    such as JWT, internal service auth and database connection string so OpenAPI refresh does
    not fail just because user-secrets are incomplete. If a service still exits, the script
    prints the captured stdout/stderr log tail instead of timing out silently.
.EXAMPLE
    pwsh .\scripts\Refresh-OpenApi.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot    = Split-Path -Parent $PSScriptRoot
$Backend     = Join-Path $RepoRoot 'backend'
$Frontend    = Join-Path $RepoRoot 'frontend'
$LogDir      = Join-Path $RepoRoot '.codex-temp\openapi-refresh'
$FrontendGeneratedTemp = Join-Path $Frontend 'src\app\generated\client$'
$FrontendGeneratedClient = Join-Path $Frontend 'src\app\generated\client'
$FrontendProductsGeneratedTemp = Join-Path $Frontend 'src\app\generated\products-client$'
$FrontendProductsGeneratedClient = Join-Path $Frontend 'src\app\generated\products-client'
$NgOpenApiGen = Join-Path $Frontend 'node_modules\.bin\ng-openapi-gen.cmd'
$ApiUrl      = 'http://localhost:5281'
$ProdUrl     = 'http://localhost:7135'
$ProdGrpcUrl = 'http://localhost:7136'
$ApiCsproj   = Join-Path $Backend 'src' 'services' 'Template.Api' 'Template.Api.csproj'
$ProdCsproj  = Join-Path $Backend 'src' 'services' 'Template.Products.Api' 'Template.Products.Api.csproj'

$ApiProcess  = $null
$ProdProcess = $null
$ApiStdOutLog = Join-Path $LogDir 'template-api.stdout.log'
$ApiStdErrLog = Join-Path $LogDir 'template-api.stderr.log'
$ProdStdOutLog = Join-Path $LogDir 'template-products-api.stdout.log'
$ProdStdErrLog = Join-Path $LogDir 'template-products-api.stderr.log'

function Get-EnvOrFallback {
    param(
        [string]$Name,
        [string]$Fallback
    )

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $Fallback
    }

    return $value
}

function New-RefreshEnvironment {
    return @{
        'ASPNETCORE_ENVIRONMENT' = 'Development'
        'DOTNET_ENVIRONMENT' = 'Development'
        'ConnectionStrings__DefaultConnection' = (Get-EnvOrFallback `
            -Name 'ConnectionStrings__DefaultConnection' `
            -Fallback 'Host=localhost;Port=5432;Database=template_db;Username=postgres;Password=postgres')
        'JwtSettings__Secret' = (Get-EnvOrFallback `
            -Name 'JwtSettings__Secret' `
            -Fallback 'openapi-refresh-development-secret-0123456789')
        'InternalServiceAuth__Token' = (Get-EnvOrFallback `
            -Name 'InternalServiceAuth__Token' `
            -Fallback 'openapi-refresh-internal-service-token-0123456789')
        'Frontend__BaseUrl' = (Get-EnvOrFallback `
            -Name 'Frontend__BaseUrl' `
            -Fallback 'http://localhost:4200')
        'ProductsService__GrpcBaseUrl' = (Get-EnvOrFallback `
            -Name 'ProductsService__GrpcBaseUrl' `
            -Fallback $ProdGrpcUrl)
    }
}

function Get-LogTail {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return '[no log output captured]'
    }

    $content = @(Get-Content -LiteralPath $Path -Tail 60 -ErrorAction SilentlyContinue)
    if ($content.Count -eq 0) {
        return '[log file is empty]'
    }

    return ($content -join [Environment]::NewLine)
}

function Clear-GeneratorPath {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Write-Host "    Removing stale generator path: $Path"
    Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue
}

function Invoke-NgOpenApiGeneration {
    param(
        [string]$ConfigPath,
        [string]$FinalOutputPath,
        [string]$TempOutputPath
    )

    Clear-GeneratorPath -Path $TempOutputPath

    & $NgOpenApiGen --config $ConfigPath
    if ($LASTEXITCODE -eq 0) {
        return
    }

    if (-not (Test-Path -LiteralPath $TempOutputPath)) {
        throw "ng-openapi-gen failed for $ConfigPath"
    }

    $tempFiles = Get-ChildItem -LiteralPath $TempOutputPath -Force -ErrorAction SilentlyContinue
    if ($null -eq $tempFiles -or $tempFiles.Count -eq 0) {
        throw "ng-openapi-gen failed for $ConfigPath and did not leave usable temp output."
    }

    Write-Host "    ng-openapi-gen reported a Windows file-operation error, salvaging temp output from $TempOutputPath" -ForegroundColor Yellow
    Clear-GeneratorPath -Path $FinalOutputPath
    New-Item -ItemType Directory -Force -Path $FinalOutputPath | Out-Null
    Copy-Item -Path (Join-Path $TempOutputPath '*') -Destination $FinalOutputPath -Recurse -Force
}

function Invoke-FrontendClientGeneration {
    Push-Location $Frontend
    try {
        npm run generate:types
        if ($LASTEXITCODE -ne 0) {
            throw 'npm run generate:types failed'
        }

        Invoke-NgOpenApiGeneration `
            -ConfigPath (Join-Path $Frontend 'ng-openapi-gen.json') `
            -FinalOutputPath $FrontendGeneratedClient `
            -TempOutputPath $FrontendGeneratedTemp

        npm run generate:types:products
        if ($LASTEXITCODE -ne 0) {
            throw 'npm run generate:types:products failed'
        }

        Invoke-NgOpenApiGeneration `
            -ConfigPath (Join-Path $Frontend 'ng-openapi-gen-products.json') `
            -FinalOutputPath $FrontendProductsGeneratedClient `
            -TempOutputPath $FrontendProductsGeneratedTemp
    }
    finally {
        Pop-Location
    }
}

function Stop-Services {
    foreach ($proc in @($ApiProcess, $ProdProcess) | Where-Object { $_ -ne $null }) {
        try {
            if (-not $proc.HasExited) {
                $proc.Kill($true)   # $true = kill entire process tree
                $proc.WaitForExit(3000) | Out-Null
            }
        }
        finally {
            $proc.Dispose()
        }
    }
}

function Start-ServiceProcess {
    param(
        [string]$Name,
        [string]$Csproj,
        [string]$LaunchProfile,
        [string]$StdOutLogPath,
        [string]$StdErrLogPath
    )

    Write-Host "    Starting $Name..."

    return Start-Process `
        -FilePath 'dotnet' `
        -ArgumentList @('run', '--project', $Csproj, '--launch-profile', $LaunchProfile, '--no-build') `
        -WorkingDirectory (Split-Path -Parent $Csproj) `
        -Environment (New-RefreshEnvironment) `
        -RedirectStandardOutput $StdOutLogPath `
        -RedirectStandardError $StdErrLogPath `
        -WindowStyle Hidden `
        -PassThru
}

function Wait-ForSpec {
    param(
        [string]$BaseUrl,
        [string]$Name,
        [System.Diagnostics.Process]$Process,
        [string]$StdOutLogPath,
        [string]$StdErrLogPath
    )

    $url     = "$BaseUrl/openapi/v1.json"
    $elapsed = 0
    Write-Host "    Waiting for $Name " -NoNewline
    while ($true) {
        if ($Process.HasExited) {
            Write-Host ' FAILED' -ForegroundColor Red
            $stdoutTail = Get-LogTail -Path $StdOutLogPath
            $stderrTail = Get-LogTail -Path $StdErrLogPath
            throw @"
$Name exited before becoming ready.
Exit code: $($Process.ExitCode)

STDOUT tail:
$stdoutTail

STDERR tail:
$stderrTail
"@
        }

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
                $stdoutTail = Get-LogTail -Path $StdOutLogPath
                $stderrTail = Get-LogTail -Path $StdErrLogPath
                throw @"
Timed out waiting for $Name at $url

STDOUT tail:
$stdoutTail

STDERR tail:
$stderrTail
"@
            }
        }
    }
}

try {
    Write-Host ''
    Write-Host '==> Preparing logs...' -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
    Remove-Item -LiteralPath $ApiStdOutLog, $ApiStdErrLog, $ProdStdOutLog, $ProdStdErrLog `
        -Force -ErrorAction SilentlyContinue

    # ---------------------------------------------------------- Start services
    Write-Host '==> Starting services...' -ForegroundColor Cyan
    $ApiProcess  = Start-ServiceProcess `
        -Name 'Template.Api' `
        -Csproj $ApiCsproj `
        -LaunchProfile 'http' `
        -StdOutLogPath $ApiStdOutLog `
        -StdErrLogPath $ApiStdErrLog
    $ProdProcess = Start-ServiceProcess `
        -Name 'Template.Products.Api' `
        -Csproj $ProdCsproj `
        -LaunchProfile 'Template.Products.Api' `
        -StdOutLogPath $ProdStdOutLog `
        -StdErrLogPath $ProdStdErrLog

    # -------------------------------------------------------------- Wait loop
    Wait-ForSpec `
        -BaseUrl $ApiUrl `
        -Name 'Template.Api' `
        -Process $ApiProcess `
        -StdOutLogPath $ApiStdOutLog `
        -StdErrLogPath $ApiStdErrLog
    Wait-ForSpec `
        -BaseUrl $ProdUrl `
        -Name 'Template.Products.Api' `
        -Process $ProdProcess `
        -StdOutLogPath $ProdStdOutLog `
        -StdErrLogPath $ProdStdErrLog

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
    Invoke-FrontendClientGeneration

    Write-Host ''
    Write-Host 'Done! API clients are up to date.' -ForegroundColor Green
}
finally {
    Write-Host ''
    Write-Host '==> Stopping services...' -ForegroundColor Cyan
    Stop-Services
}
