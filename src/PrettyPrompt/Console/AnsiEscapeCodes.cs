﻿#region License Header
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
#endregion

using PrettyPrompt.Highlighting;
using System.Linq;

namespace PrettyPrompt.Consoles;

public static class AnsiEscapeCodes
{
    private const char Escape = '\u001b';
    private const string ResetForegroundColor = "39";
    private const string ResetBackgroundColor = "49";
    private const string Bold = "1";
    private const string Underline = "4";
    private const string Reverse = "7";
    public static readonly string ClearLine = $"{Escape}[0K";
    public static readonly string ClearToEndOfScreen = $"{Escape}[0J";
    public static readonly string ClearEntireScreen = $"{Escape}[2J";

    /// <summary>
    /// index starts at 1!
    /// </summary>
    public static string MoveCursorToColumn(int index) => $"{Escape}[{index}G";

    public static string MoveCursorUp(int count) => count == 0 ? "" : $"{Escape}[{count}A";
    public static string MoveCursorDown(int count) => count == 0 ? "" : $"{Escape}[{count}B";
    public static string MoveCursorRight(int count) => count == 0 ? "" : $"{Escape}[{count}C";
    public static string MoveCursorLeft(int count) => count == 0 ? "" : $"{Escape}[{count}D";

    public static string ForegroundColor(byte r, byte g, byte b) => ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.RGB(r, g, b)));
    public static string BackgroundColor(byte r, byte g, byte b) => ToAnsiEscapeSequence(new ConsoleFormat(Background: AnsiColor.RGB(r, g, b)));

    public static readonly string Black = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.Black));
    public static readonly string Red = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.Red));
    public static readonly string Green = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.Green));
    public static readonly string Yellow = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.Yellow));
    public static readonly string Blue = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.Blue));
    public static readonly string Magenta = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.Magenta));
    public static readonly string Cyan = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.Cyan));
    public static readonly string White = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.White));
    public static readonly string BrightBlack = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.BrightBlack));
    public static readonly string BrightRed = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.BrightRed));
    public static readonly string BrightGreen = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.BrightGreen));
    public static readonly string BrightYellow = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.BrightYellow));
    public static readonly string BrightBlue = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.BrightBlue));
    public static readonly string BrightMagenta = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.BrightMagenta));
    public static readonly string BrightCyan = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.BrightCyan));
    public static readonly string BrightWhite = ToAnsiEscapeSequence(new ConsoleFormat(Foreground: AnsiColor.BrightWhite));
    public static readonly string Reset = $"{Escape}[0m";

    public static string SetColors(AnsiColor fg, AnsiColor bg) =>
        ToAnsiEscapeSequence(new ConsoleFormat(Foreground: fg, Background: bg));

    public static string ToAnsiEscapeSequence(ConsoleFormat formatting) =>
       Escape
        + "["
        + string.Join(
            separator: ";",
            values: (formatting.Inverted
                ? new[]
                {
                        ResetForegroundColor,
                        ResetBackgroundColor,
                        Reverse
                }
                : new[]
                {
                        formatting.Foreground?.Foreground ?? ResetForegroundColor,
                        formatting.Background?.Background ?? ResetBackgroundColor,
                        formatting.Bold ? Bold : null,
                        formatting.Underline ? Underline : null,
                }
            ).Where(format => format is not null)
          )
        + "m";
}
