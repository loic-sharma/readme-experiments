$more = $true

do {
    $packages = Get-ChildItem -Filter "*.nupkg"
    $more = $packages.Length -gt 0

    $packages | % {
        $result = dotnet nuget push `
            --source https://apidev.nugettest.org/v3/index.json `
            -k $($env:API_KEY) `
            --timeout 300 `
            --skip-duplicate `
            $_.FullName

        Write-Host $result

        if ([string]::Join('', $result).Contains("already exists")) {
            Remove-Item $_.FullName
            Write-Host "Deleting $($_.Name)"
        }
    }
}
while ($more)