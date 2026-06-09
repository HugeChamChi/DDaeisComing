function Fix-Namespaces ($Path) {
    $content = Get-Content $Path
    $hasUI = $content -match 'Bathhouse\.UI\.'
    $hasData = $content -match 'Bathhouse\.Data\.'
    $hasManagers = $content -match 'Bathhouse\.Managers\.'
    $hasSysCol = $content -match 'System\.Collections\.Generic\.'
    
    $newContentText = ($content -join "`n") -replace 'Bathhouse\.Managers\.', '' -replace 'Bathhouse\.Data\.', '' -replace 'Bathhouse\.UI\.', '' -replace 'System\.Collections\.Generic\.', ''
    $content = $newContentText -split "`n"

    $usings = @()
    if ($hasUI -and ($content -notmatch 'using Bathhouse\.UI;')) { $usings += 'using Bathhouse.UI;' }
    if ($hasData -and ($content -notmatch 'using Bathhouse\.Data;')) { $usings += 'using Bathhouse.Data;' }
    if ($hasManagers -and ($content -notmatch 'using Bathhouse\.Managers;')) { $usings += 'using Bathhouse.Managers;' }
    if ($hasSysCol -and ($content -notmatch 'using System\.Collections\.Generic;')) { $usings += 'using System.Collections.Generic;' }
    
    if ($usings.Length -gt 0) {
        $firstUsing = 0
        for ($i=0; $i -lt $content.Length; $i++) {
            if ($content[$i] -match '^using ') {
                $firstUsing = $i
                break
            }
        }
        $newContent = @()
        if ($firstUsing -gt 0) {
            $newContent += $content[0..($firstUsing-1)]
        }
        $newContent += $usings
        if ($content.Length -gt 0) {
            $newContent += $content[$firstUsing..($content.Length-1)]
        }
        $newContent | Set-Content $Path -Encoding UTF8
    } else {
        $content | Set-Content $Path -Encoding UTF8
    }
}
Get-ChildItem Assets\Scripts -Filter *.cs -Recurse | ForEach-Object { Fix-Namespaces $_.FullName }
