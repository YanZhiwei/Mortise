﻿using System.Drawing;

namespace Mortise.Accessibility.Highlighter.Abstractions;

public interface IHighlightRectangle
{
    public void SetLocation(Rectangle rectangle, string? content = null);

    public void Show();

    public void Hide();

    public void Close();
}