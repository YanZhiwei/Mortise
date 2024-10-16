﻿using Mortise.BrowserAccessibility.Models;
using System.Diagnostics;
using System.Drawing;

namespace Mortise.Browser.Platform;

public interface ISystemInteraction
{
    /// <summary>
    ///     根据坐标获取窗口句柄
    /// </summary>
    /// <param name="point">Point</param>
    /// <returns>窗口句柄</returns>
    public IntPtr GetWindowHandle(Point point = default);

    /// <summary>
    ///     根据坐标获取对应进程
    /// </summary>
    /// <param name="point">Point</param>
    /// <returns>Process</returns>
    public Process? GetProcess(Point point = default);

    public Point ScreenPointToWebPoint(Point screenPoint);

    public Rectangle WebPointToScreenRectangle(Point screenPoint, DomRect domRect);
}