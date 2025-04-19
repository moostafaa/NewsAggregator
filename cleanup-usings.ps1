$projectPath = Get-Location

# Install dotnet format if not already installed
if (-not (dotnet tool list --global | Select-String "dotnet-format")) {
    Write-Host "Installing dotnet-format tool..."
    dotnet tool install -g dotnet-format
}

# Clean up unused usings with dotnet format
Write-Host "Cleaning up unused usings in project..."
dotnet format "$projectPath/NewsAggregator.sln" --folder --include-generated --diagnostics IDE0005 --severity info

Write-Host "Unused usings cleanup completed." 