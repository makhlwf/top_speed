$ErrorActionPreference = 'Stop'

function Fail([string]$message) {
    Write-Host $message -ForegroundColor Red
    exit 1
}

function Parse-JsonStrict([string]$path, [string]$displayPath) {
    if (-not ("System.Text.Json.JsonDocument" -as [type])) {
        Fail "Commit blocked: strict JSON validation requires PowerShell 7 (System.Text.Json)."
    }

    $jsonText = Get-Content -LiteralPath $path -Raw
    try {
        $options = [System.Text.Json.JsonDocumentOptions]::new()
        $options.AllowTrailingCommas = $false
        $options.CommentHandling = [System.Text.Json.JsonCommentHandling]::Disallow
        $document = [System.Text.Json.JsonDocument]::Parse($jsonText, $options)
        $document.Dispose()
        return $jsonText
    } catch {
        Fail "Commit blocked: invalid JSON in '$displayPath'. Details: $($_.Exception.Message)"
    }
}

$repoRoot = (& git rev-parse --show-toplevel).Trim()
if ([string]::IsNullOrWhiteSpace($repoRoot)) {
    Fail "Commit blocked: could not resolve repository root."
}

$versionFile = Join-Path $repoRoot "top_speed_net/TopSpeed.Shared/Protocol/VersionInfo.cs"
$infoFile = Join-Path $repoRoot "info.json"

if (-not (Test-Path -LiteralPath $versionFile)) {
    Fail "Commit blocked: missing file '$versionFile'."
}

if (-not (Test-Path -LiteralPath $infoFile)) {
    Fail "Commit blocked: missing file '$infoFile'."
}

$stagedFiles = & git -C $repoRoot diff --cached --name-only --diff-filter=ACMR
if ($LASTEXITCODE -ne 0) {
    Fail "Commit blocked: failed to read staged files."
}

$stagedJsonFiles = @($stagedFiles | Where-Object { $_ -like '*.json' })
foreach ($relativePath in $stagedJsonFiles) {
    $normalizedRelativePath = $relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar
    $fullPath = Join-Path $repoRoot $normalizedRelativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        continue
    }

    Parse-JsonStrict -path $fullPath -displayPath $relativePath | Out-Null
}

$source = Get-Content -LiteralPath $versionFile -Raw

function Get-ConstantValue([string]$typeName, [string]$name) {
    $typePattern = "public\s+static\s+class\s+$typeName\s*\{([\s\S]*?)\}"
    $typeMatch = [System.Text.RegularExpressions.Regex]::Match($source, $typePattern)
    if (-not $typeMatch.Success) {
        Fail "Commit blocked: could not find '$typeName' in VersionInfo.cs."
    }

    $typeBody = $typeMatch.Groups[1].Value
    $pattern = "public\s+const\s+\w+\s+$name\s*=\s*(\d+)\s*;"
    $match = [System.Text.RegularExpressions.Regex]::Match($typeBody, $pattern)
    if (-not $match.Success) {
        Fail "Commit blocked: could not read '$name' from $typeName."
    }

    return [int]$match.Groups[1].Value
}

$year = Get-ConstantValue "ReleaseVersionInfo" "ClientYear"
$month = Get-ConstantValue "ReleaseVersionInfo" "ClientMonth"
$day = Get-ConstantValue "ReleaseVersionInfo" "ClientDay"
$revision = Get-ConstantValue "ReleaseVersionInfo" "ClientRevision"
$expectedVersion = "$year.$month.$day.$revision"

try {
    $infoJsonText = Parse-JsonStrict -path $infoFile -displayPath "info.json"
    $info = $infoJsonText | ConvertFrom-Json
} catch {
    Fail "Commit blocked: info.json is not valid JSON. Details: $($_.Exception.Message)"
}

$actualVersion = $info.version
if ([string]::IsNullOrWhiteSpace($actualVersion)) {
    Fail "Commit blocked: info.json is missing the 'version' key."
}

if ($actualVersion -ne $expectedVersion) {
    $message = @"
Commit blocked: client version mismatch.
- ReleaseVersionInfo client version: $expectedVersion
- info.json version: $actualVersion

Fix: update info.json 'version' to '$expectedVersion' and commit again.
"@
    Fail $message
}

exit 0
