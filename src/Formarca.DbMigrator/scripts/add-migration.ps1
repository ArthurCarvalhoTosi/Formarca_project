param(
    [Parameter(Mandatory = $true)]
    [string]$Name
)

$ErrorActionPreference = "Stop"

# scripts/ -> Formarca.DbMigrator/ -> src/ -> repo root
$root = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$infrastructure = Join-Path $root "src\Formarca.Infrastructure"
$migrationsDir = Join-Path $PSScriptRoot "migrations"

if (-not (Test-Path $migrationsDir)) {
    New-Item -ItemType Directory -Path $migrationsDir | Out-Null
}

Push-Location $root
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Adding EF migration '$Name'..."
    dotnet ef migrations add $Name --project src/Formarca.Infrastructure
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $migrationsPath = Join-Path $infrastructure "Persistence\Migrations"
    $migrationFile = Get-ChildItem -Path $migrationsPath -Filter "*_$Name.cs" |
        Where-Object { $_.Name -notlike "*.Designer.cs" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $migrationFile) {
        throw "Could not find migration file for '$Name'."
    }

    $timestamp = $migrationFile.BaseName.Split("_")[0]
    $sqlRelative = "src/Formarca.DbMigrator/scripts/migrations/${timestamp}_${Name}.sql"

    Write-Host "Exporting SQL script to $sqlRelative ..."
    dotnet ef migrations script --idempotent --project src/Formarca.Infrastructure --output $sqlRelative
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

Write-Host "Done."
Write-Host "Migration: $($migrationFile.FullName)"
Write-Host "SQL:       $(Join-Path $root $sqlRelative)"
