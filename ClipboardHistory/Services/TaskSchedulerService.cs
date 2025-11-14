using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ClipboardHistory.Services
{
    /// <summary>
    /// 使用 Windows 任务计划程序实现管理员权限开机自启
    /// </summary>
    public class TaskSchedulerService
    {
        private const string TaskName = "ClipboardHistory_Startup";
        private const string TaskDescription = "何小工-剪贴板历史工具开机自启";

        /// <summary>
        /// 检查开机自启任务是否存在
        /// </summary>
        public bool IsStartupTaskEnabled()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Query /TN \"{TaskName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                if (process == null) return false;

                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查任务状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建开机自启任务（以最高权限运行）
        /// </summary>
        public bool CreateStartupTask()
        {
            try
            {
                // 获取当前可执行文件路径
                string exePath = GetExecutablePath();
                if (string.IsNullOrEmpty(exePath))
                {
                    Console.WriteLine("无法获取可执行文件路径");
                    return false;
                }

                // 如果任务已存在，先删除
                if (IsStartupTaskEnabled())
                {
                    DeleteStartupTask();
                }

                // 使用 schtasks 命令创建任务
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Create /TN \"{TaskName}\" " +
                               $"/TR \"\\\"{exePath}\\\"\" " +
                               $"/SC ONLOGON " +
                               $"/RL HIGHEST " +
                               $"/F " +
                               $"/DESC \"{TaskDescription}\"",
                    UseShellExecute = true,
                    Verb = "runas", // 请求管理员权限
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return false;

                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("开机自启任务创建成功");
                    return true;
                }
                else
                {
                    Console.WriteLine($"创建任务失败，退出代码: {process.ExitCode}");
                    return false;
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // 用户取消了 UAC 提示
                Console.WriteLine("用户取消了管理员权限请求");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建开机自启任务失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除开机自启任务
        /// </summary>
        public bool DeleteStartupTask()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Delete /TN \"{TaskName}\" /F",
                    UseShellExecute = true,
                    Verb = "runas", // 请求管理员权限
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return false;

                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("开机自启任务已删除");
                    return true;
                }
                else
                {
                    Console.WriteLine($"删除任务失败，退出代码: {process.ExitCode}");
                    return false;
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // 用户取消了 UAC 提示
                Console.WriteLine("用户取消了管理员权限请求");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除开机自启任务失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取当前可执行文件的完整路径
        /// </summary>
        private string GetExecutablePath()
        {
            try
            {
                // 尝试获取主模块路径（适用于发布后的 exe）
                using var process = Process.GetCurrentProcess();
                string exePath = process.MainModule?.FileName;

                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    return exePath;
                }

                // 备用方案：使用 Assembly 路径
                var assembly = Assembly.GetExecutingAssembly();
                exePath = assembly.Location;

                // 如果是 .dll，尝试找对应的 .exe
                if (exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    exePath = Path.ChangeExtension(exePath, ".exe");
                }

                if (File.Exists(exePath))
                {
                    return exePath;
                }

                Console.WriteLine("无法定位可执行文件");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取可执行文件路径失败: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 检查当前进程是否以管理员权限运行
        /// </summary>
        public static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}
