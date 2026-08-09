# ============================================================
#  install_apk.ps1   APK 安装到实体机工具
#  特性：
#    1. 循环请求 APK 路径，窗口不自动关闭，输入 exit 才退出
#    2. 自动清理路径里的多余引号（含弯引号）和首尾空白
#    3. 每次安装前显示当前 adb 设备，多设备时可指定
#  用法：双击 install_apk.bat，或在 PowerShell 里运行本脚本
# ============================================================

# adb 可执行文件：PATH 里有就写 'adb'，否则写 adb.exe 绝对路径
$adb = 'adb'
# 安装参数：-r 覆盖安装保留数据；-d 允许降级版本（开发期反复装很实用）
$installArgs = @('-r', '-d')

# ---------- 检查 adb ----------
if (-not (Get-Command $adb -ErrorAction SilentlyContinue)) {
    Write-Host '错误：找不到 adb。请把 Android SDK 的 platform-tools 加入 PATH，'
    Write-Host '或把本文件顶部的 $adb 改成 adb.exe 的绝对路径。' -ForegroundColor Red
    Read-Host '按回车退出'
    exit 1
}

# ---------- 取在线（已授权）设备串号 ----------
function Get-OnlineDevices {
    $lines = & $adb devices 2>$null | Select-Object -Skip 1 | Where-Object { $_ -match '\S' }
    $serials = @()
    foreach ($line in $lines) {
        $parts = $line -split '\s+'
        if ($parts.Count -ge 2 -and $parts[1] -eq 'device') {
            $serials += $parts[0]
        }
    }
    return $serials
}

Write-Host '============================================' -ForegroundColor Cyan
Write-Host '   APK 安装到实体机工具' -ForegroundColor Cyan
Write-Host '============================================' -ForegroundColor Cyan

while ($true) {
    Write-Host ''
    Write-Host '--------------------------------------------' -ForegroundColor DarkGray

    # 显示当前设备
    $devs = @(Get-OnlineDevices)
    if ($devs.Count -eq 0) {
        Write-Host '当前没有已授权设备。请插线并开启 USB 调试，或先 adb connect IP:5555。' -ForegroundColor Red
    }
    else {
        Write-Host '当前设备：' -ForegroundColor Yellow
        $devs | ForEach-Object { Write-Host ('  ' + $_) -ForegroundColor Green }
    }

    $raw = Read-Host 'APK 路径（拖文件进来或粘贴；输入 exit 退出）'

    if ($raw -match '^(exit|quit|q|退出)$') {
        Write-Host '再见！' -ForegroundColor Cyan
        break
    }

    # ---------- 清理路径：去引号（含弯引号）+ 去首尾空白 ----------
    $path = $raw
    $path = $path.Replace('"', '')                                # 去双引号
    $path = $path.Replace("'", '')                                # 去单引号
    $path = $path.Replace([string][char]0x201C, '')               # 去弯引号 “
    $path = $path.Replace([string][char]0x201D, '')               # 去弯引号 ”
    $path = $path.Trim()                                          # 去首尾空白

    if ([string]::IsNullOrWhiteSpace($path)) {
        Write-Host '[提示] 输入为空，已跳过。' -ForegroundColor Yellow
        continue
    }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Write-Host ('[错误] 文件不存在：' + $path) -ForegroundColor Red
        continue
    }
    if ([IO.Path]::GetExtension($path) -ne '.apk') {
        Write-Host ('[警告] 该文件不是 .apk（' + [IO.Path]::GetExtension($path) + '），仍尝试安装。') -ForegroundColor Yellow
    }

    # ---------- 选定目标设备 ----------
    $devs = @(Get-OnlineDevices)
    if ($devs.Count -eq 0) {
        Write-Host '[错误] 没有可安装的设备，已取消本次安装。' -ForegroundColor Red
        continue
    }
    if ($devs.Count -eq 1) {
        $target = $devs[0]
    }
    else {
        $choice = Read-Host ('检测到多个设备，输入要安装的设备串号 [' + ($devs -join ', ') + ']（回车用第一个）')
        if ([string]::IsNullOrWhiteSpace($choice)) { $target = $devs[0] } else { $target = $choice }
    }

    Write-Host ''
    Write-Host ('开始安装 -> ' + $target + ' : ' + $path) -ForegroundColor Cyan
    & $adb -s $target install $installArgs $path

    if ($LASTEXITCODE -eq 0) {
        Write-Host '安装成功 [OK]' -ForegroundColor Green
    }
    else {
        Write-Host ('安装失败（adb 退出码 ' + $LASTEXITCODE + '）。') -ForegroundColor Red
        Write-Host '常见原因：设备未授权 / 签名冲突 / 版本降级（可先卸载旧版再装）。' -ForegroundColor DarkGray
    }
    Write-Host '可继续输入下一个 APK，或输入 exit 退出。' -ForegroundColor DarkGray
}
