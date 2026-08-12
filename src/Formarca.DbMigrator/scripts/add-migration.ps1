param(
    [Parameter(Mandatory = $true)]
    [string]$Name
)

$ErrorActionPreference = "Stop"

# scripts/ -> Formarca.DbMigrator/ -> src/ -> repo root
$root = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$infrastructure = Join-Path $root "src\Formarca.Infrastructure"
$startup = Join-Path $root "src\Formarca.Web"
$migrationsDir = Join-Path $PSScriptRoot "migrations"

if (-not (Test-Path $migrationsDir)) {
    New-Item -ItemType Directory -Path $migrationsDir | Out-Null
}

Write-Host "Adding EF migration '$Name'..."
dotnet ef migrations add $Name `
    --project $infrastructure `
    --startup-project $startup `
    --output-dir Persistence/Migrations

$migrationsPath = Join-Path $infrastructure "Persistence\Migrations"
$migrationFile = Get-ChildItem -Path $migrationsPath -Filter "*_$Name.cs" |
    Where-Object { $_.Name -notlike "*.Designer.cs" } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $migrationFile) {
    throw "Could not find migration file for '$Name'."
}

$timestamp = $migrationFile.BaseName.Split("_")[0]
$sqlFile = Join-Path $migrationsDir "${timestamp}_${Name}.sql"

Write-Host "Exporting SQL script to $sqlFile ..."
dotnet ef migrations script `
    --idempotent `
    --project $infrastructure `
    --startup-project $startup `
    --output $sqlFile

Write-Host "Done."
Write-Host "Migration: $($migrationFile.FullName)"
Write-Host "SQL:       $sqlFile"
