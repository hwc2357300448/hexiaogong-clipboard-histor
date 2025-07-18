@echo off
echo ========================================
echo 正在创建剪贴板历史安装包...
echo ========================================
echo.
echo 检查NSIS是否已安装...

REM 检查常见的NSIS安装路径
set NSIS_PATH=""
if exist "C:\Program Files (x86)\NSIS\makensis.exe" (
    set NSIS_PATH="C:\Program Files (x86)\NSIS\makensis.exe"
    echo 找到NSIS: %NSIS_PATH%
) else if exist "C:\Program Files\NSIS\makensis.exe" (
    set NSIS_PATH="C:\Program Files\NSIS\makensis.exe"
    echo 找到NSIS: %NSIS_PATH%
) else (
    echo 错误: 未找到NSIS安装程序
    echo.
    echo 请按照以下步骤安装NSIS:
    echo 1. 访问 https://nsis.sourceforge.io/Download
    echo 2. 下载最新版本的NSIS
    echo 3. 运行安装程序并完成安装
    echo 4. 重新运行此脚本
    echo.
    pause
    exit /b 1
)

echo.
echo 正在编译安装程序...
echo 使用脚本: setup.nsi
echo 输出文件: ClipboardHistory-Setup.exe
echo.

%NSIS_PATH% /V2 setup.nsi

if %errorlevel% equ 0 (
    echo.
    echo ===========================================
    echo 🎉 安装包创建成功！
    echo ===========================================
    echo.
    echo 输出文件: ClipboardHistory-Setup.exe
    echo 文件大小: 
    for %%I in ("ClipboardHistory-Setup.exe") do echo %%~zI 字节
    echo.
    echo 安装包功能:
    echo ✓ 现代化的安装界面
    echo ✓ 自动创建桌面快捷方式
    echo ✓ 开始菜单快捷方式
    echo ✓ 可选的开机自启动
    echo ✓ 完整的卸载功能
    echo ✓ 注册表清理
    echo.
    echo 现在可以分发这个安装包了！
    echo.
    echo 测试建议:
    echo 1. 右键以管理员身份运行安装包
    echo 2. 按照安装向导完成安装
    echo 3. 测试程序功能
    echo 4. 测试卸载功能
    echo.
) else (
    echo.
    echo ❌ 错误: 安装包创建失败
    echo.
    echo 可能的原因:
    echo 1. 缺少必要的文件（检查publish目录）
    echo 2. 权限不足（尝试以管理员身份运行）
    echo 3. NSIS脚本语法错误
    echo 4. 文件被占用（关闭正在运行的程序）
    echo.
    echo 请检查上述问题后重试。
)

echo.
echo 按任意键退出...
pause > nul