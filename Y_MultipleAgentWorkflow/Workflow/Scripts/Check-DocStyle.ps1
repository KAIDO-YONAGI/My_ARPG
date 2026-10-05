#Requires -Version 7.0
<#
.SYNOPSIS
检查工作流文档库是否符合《文档写法》规范。

.DESCRIPTION
逐文件扫描 Markdown，报告违反写法的位置。退出码：0 通过，1 有 error。
DeveloperLog 保留时间线追述，模板与 Skill 安装的配置方法文档不参与检查。

.PARAMETER WorkflowRoot
工作流根目录，默认为脚本所在目录向上两级。

.PARAMETER Format
输出格式：json 或 text。
#>
[CmdletBinding()]
param(
    [string]$WorkflowRoot = (Join-Path $PSScriptRoot '..\..'),
    [ValidateSet('json', 'text')]
    [string]$Format = 'text'
)

$ErrorActionPreference = 'Stop'

$script:Root = [IO.Path]::GetFullPath($WorkflowRoot)

$script:ExcludedFiles = @(
    'DeveloperLog.md',
    'Workflow_Configuration_Guide.md',
    'README.md'
)

$script:ExcludedDirs = @(
    'Templates'
)

$script:PathPattern = '(?-i)Assets[/\\]'
$script:ExtensionPattern = '\.(?:cs|unity|prefab|asset|png|jpg|mat|controller|rendertexture|anim|fbx|mp3|wav|uxml|uss|shader)\b'
$script:HistoryPattern = '已由|已于|现已|此前|曾经|原为|撤回|更正|已经删除|变更记录|版本变更|历史线索|待收敛|不再'
$script:NegationPattern = '并非|而不是|而非|虽然|但是'
$script:ParentheticalPattern = '（[^）]{10,}）'
$script:RevisionHeadingPattern = '^#+.*(?:变更记录|更新记录|版本历史|修订记录|Changelog)'

function Get-CheckFiles {
    Get-ChildItem -Path $script:Root -Recurse -File -Filter '*.md' |
        Where-Object {
            $script:ExcludedFiles -notcontains $_.Name -and
            ($_.DirectoryName -split '[\\/]' | Where-Object { $script:ExcludedDirs -contains $_ }).Count -eq 0
        } |
        Sort-Object FullName
}

function Find-Matches {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$ExcerptText
    )

    if (-not $ExcerptText) { $ExcerptText = $Text }

    $result = @()
    $lines = $Text -split "\r?\n"
    $excerptLines = $ExcerptText -split "\r?\n"
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match $Pattern) {
            $excerpt = if ($i -lt $excerptLines.Length) { $excerptLines[$i].Trim() } else { $lines[$i].Trim() }
            $result += [pscustomobject]@{
                line    = $i + 1
                excerpt = $excerpt
            }
        }
    }
    return $result
}

function Hide-Internal {
    param(
        [string]$Text
    )

    # 库内引用与包 ID 属于合法标识，检查前遮蔽，避免误报。
    $hidden = $Text -replace '\bcom\.[A-Za-z0-9_.\-]+', 'PKGID'
    $hidden = $hidden -replace '[A-Za-z0-9_\\/.\-]*Y_MultipleAgentWorkflow[A-Za-z0-9_\\/.\-]*', 'LIBPATH'
    $hidden = $hidden -replace '[\w\\/.\-]*\.md', 'DOC'
    $hidden = $hidden -replace '(?:[A-Za-z0-9_\-/\\]+/)?[A-Za-z0-9_\-]+\.(?:ps1|json|gitignore)', 'TOOL'
    return $hidden
}

$checks = @()
$files = Get-CheckFiles

foreach ($file in $files) {
    $relative = $file.FullName.Substring($script:Root.Length).TrimStart('\', '/')
    $text = Get-Content $file.FullName -Raw
    $masked = Hide-Internal $text

    $pathHits = Find-Matches -Text $masked -Pattern $script:PathPattern -ExcerptText $text
    if ($pathHits.Count -gt 0) {
        $checks += [pscustomobject]@{
            level   = 'error'
            name    = "path:$relative"
            message = "$($pathHits.Count) 处路径引用，首处第 $($pathHits[0].line) 行：$($pathHits[0].excerpt)"
        }
    }

    $extHits = Find-Matches -Text $masked -Pattern $script:ExtensionPattern -ExcerptText $text
    if ($extHits.Count -gt 0) {
        $checks += [pscustomobject]@{
            level   = 'error'
            name    = "extension:$relative"
            message = "$($extHits.Count) 处文件扩展名引用，首处第 $($extHits[0].line) 行：$($extHits[0].excerpt)"
        }
    }

    $lineNoHits = Find-Matches -Text $masked -Pattern '\.[A-Za-z]+:\d+' -ExcerptText $text
    if ($lineNoHits.Count -gt 0) {
        $checks += [pscustomobject]@{
            level   = 'error'
            name    = "linenumber:$relative"
            message = "$($lineNoHits.Count) 处行号引用，首处第 $($lineNoHits[0].line) 行：$($lineNoHits[0].excerpt)"
        }
    }

    $historyHits = Find-Matches -Text $text -Pattern $script:HistoryPattern
    if ($historyHits.Count -gt 0) {
        $checks += [pscustomobject]@{
            level   = 'error'
            name    = "history:$relative"
            message = "$($historyHits.Count) 处来历或变化叙述，首处第 $($historyHits[0].line) 行：$($historyHits[0].excerpt)"
        }
    }

    $headingHits = Find-Matches -Text $text -Pattern $script:RevisionHeadingPattern
    if ($headingHits.Count -gt 0) {
        $checks += [pscustomobject]@{
            level   = 'error'
            name    = "revision-heading:$relative"
            message = "存在修订记录小节，第 $($headingHits[0].line) 行：$($headingHits[0].excerpt)"
        }
    }

    $parenHits = Find-Matches -Text $text -Pattern $script:ParentheticalPattern
    if ($parenHits.Count -gt 0) {
        $checks += [pscustomobject]@{
            level   = 'warning'
            name    = "parenthetical:$relative"
            message = "$($parenHits.Count) 处括号附注，首处第 $($parenHits[0].line) 行：$($parenHits[0].excerpt)"
        }
    }

    $negationHits = Find-Matches -Text $text -Pattern $script:NegationPattern
    if ($negationHits.Count -gt 2) {
        $checks += [pscustomobject]@{
            level   = 'warning'
            name    = "negation:$relative"
            message = "$($negationHits.Count) 处否定或转折句式"
        }
    }
}

if ($checks.Count -eq 0) {
    $checks += [pscustomobject]@{
        level   = 'pass'
        name    = 'doc-style'
        message = "$($files.Count) 份文档符合写法规范。"
    }
}

$errorCount = @($checks | Where-Object { $_.level -eq 'error' }).Count
$warningCount = @($checks | Where-Object { $_.level -eq 'warning' }).Count
$passCount = @($checks | Where-Object { $_.level -eq 'pass' }).Count

$result = [pscustomobject]@{
    success = ($errorCount -eq 0)
    summary = [pscustomobject]@{
        scanned  = $files.Count
        pass     = $passCount
        warning  = $warningCount
        error    = $errorCount
    }
    checks  = $checks
}

if ($Format -eq 'json') {
    $result | ConvertTo-Json -Depth 6
}
else {
    foreach ($c in $checks) {
        '{0,-8} {1,-42} {2}' -f $c.level, $c.name, $c.message
    }
    ''
    '扫描 {0} 份文档，pass {1} / warning {2} / error {3}' -f $files.Count, $passCount, $warningCount, $errorCount
}

if ($errorCount -gt 0) { exit 1 }
exit 0
