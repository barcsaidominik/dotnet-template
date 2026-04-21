Get-ChildItem -Path .. -Recurse -File -Include *.cs, *.ts, *.md, *.json, *.yml, *yaml |
Where-Object { $_.FullName -notmatch '\\node_modules\\' } |
ForEach-Object {
    $content = Get-Content $_.FullName -Raw

    if ($content -match "(?<!`r)`n") {
        $content = $content -replace "(?<!`r)`n", "`r`n"
        [System.IO.File]::WriteAllText($_.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Átalakítva: $($_.FullName)"
    }
}