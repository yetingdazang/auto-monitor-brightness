# 自动显示器亮度

一个无需常驻、运行时无控制台窗口的 Windows 小工具，通过 DDC/CI 为所有支持的显示器设置日间和夜间亮度。

## 下载与安装

下载仓库中的 [AutoBrightness-v1.0.0.zip](AutoBrightness-v1.0.0.zip)，完整解压后双击 `Install.cmd`。

安装过程会显示命令窗口。安装器会把文件复制到 `%LOCALAPPDATA%\AutoMonitorBrightness`，创建当前用户的 Windows 定时任务，并立即执行一次亮度检查。完成后可以删除原解压文件夹。

默认按电脑本地时间运行：

| 时段 | 亮度 |
| --- | --- |
| 07:00 至 18:00 | 50% |
| 18:00 至次日 07:00 | 25% |

只在早晚切换时间和用户登录时检查，没有每隔几分钟的轮询。每次执行完就退出，日常运行没有程序窗口。需要当前用户已登录；任务不会主动唤醒休眠电脑。

## 修改设置

打开 `%LOCALAPPDATA%\AutoMonitorBrightness\settings.ini`，用记事本修改：

```ini
DayTime=07:00
NightTime=18:00
DayBrightness=50
NightBrightness=25
```

时间格式为 `HH:mm`，日间开始时间必须早于夜间开始时间；亮度范围为 0–100。修改后双击安装目录中的 `Install.cmd`，更新定时计划并立即应用。也可以双击 `AutoBrightness.exe` 立即检查；它没有操作界面。

## 兼容性与故障排查

- 面向 Windows 10/11，使用系统自带的 .NET Framework、Windows PowerShell 和任务计划程序，无需 PowerShell 7。
- 显示器必须支持 DDC/CI 亮度控制，部分显示器需要先在自身菜单中启用 DDC/CI。
- 某些笔记本内置屏幕、转接器或连接链路可能不支持此接口。
- 查看安装目录的 `brightness.log` 获取结果和错误。不支持的显示器会记录错误，其他支持的显示器仍会被处理。
- 程序未签名，Windows 或安全软件可能显示提示。
- 安装时如遇权限错误，请让电脑管理员检查任务计划权限或组织策略。

程序不联网，不存储密码。日志超过约 1 MB 会保留最后 200 行。

## 卸载

运行 `Uninstall.cmd` 删除定时任务。当前亮度会保留，之后可手动删除安装目录。

## 源码与构建

`source/SharedBrightnessApp.cs` 读取设置、判断时段并写入日志；`source/MonitorBrightnessControl.cs` 调用 Windows 显示器接口。运行 `source/Build.cmd` 可使用 Windows 自带的 .NET Framework C# 编译器重新构建无控制台程序。

已在作者的两台外接显示器上验证亮度读取与设置；未覆盖所有显示器及连接设备。
