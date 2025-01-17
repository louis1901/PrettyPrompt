﻿#region License Header
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NSubstitute;
using NSubstitute.Core;
using PrettyPrompt.Consoles;

namespace PrettyPrompt.Tests;

public static class ConsoleStub
{
    private static readonly Regex FormatStringSplit = new(@"({\d+}|.)", RegexOptions.Compiled);

    public static IConsole NewConsole(int width = 100, int height = 100)
    {
        var console = Substitute.For<IConsole>();
        console.BufferWidth.Returns(width);
        console.WindowHeight.Returns(height);
        return console;
    }

    public static IReadOnlyList<string> GetAllOutput(this IConsole consoleStub) =>
        consoleStub.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(Console.Write))
            .Select(call => (string)call.GetArguments().Single())
            .ToArray();

    public static string GetFinalOutput(this IConsole consoleStub)
    {
        return consoleStub.GetAllOutput()[^2]; // second to last. The last is always the newline drawn after the prompt is submitted
    }

    /// <summary>
    /// Stub Console.ReadKey to return a series of keystrokes (<see cref="ConsoleKeyInfo" />).
    /// Keystrokes are specified as a <see cref="FormattableString"/> with any special keys,
    /// like modifiers or navigation keys, represented as FormattableString arguments (of type
    /// <see cref="ConsoleModifiers"/> or <see cref="ConsoleKey"/>).
    /// </summary>
    /// <example>$"{Control}LHello{Enter}" is turned into Ctrl-L, H, e, l, l, o, Enter key</example>
    public static ConfiguredCall StubInput(this IConsole consoleStub, params FormattableString[] inputs)
    {
        var keys = inputs
            .SelectMany(line => MapToConsoleKeyPresses(line))
            .ToList();

        return consoleStub.StubInput(keys);
    }

    /// <summary>
    /// Stub Console.ReadKey to return a series of keystrokes (<see cref="ConsoleKeyInfo" />).
    /// Keystrokes are specified as a <see cref="FormattableString"/> with any special keys,
    /// like modifiers or navigation keys, represented as FormattableString arguments (of type
    /// <see cref="ConsoleModifiers"/> or <see cref="ConsoleKey"/>) and with optional Action to be invoked after key press.
    /// Use <see cref="Input(FormattableString)" and <see cref="Input(FormattableString, Action)"/> methods to create inputs./>
    /// </summary>
    public static ConfiguredCall StubInput(this IConsole consoleStub, params FormattableStringWithAction[] inputs)
    {
        var keys = inputs
            .SelectMany(EnumerateKeys)
            .ToList();

        return consoleStub
            .ReadKey(intercept: true)
            .Returns(keys.First(), keys.Skip(1).ToArray());

        IEnumerable<Func<CallInfo, ConsoleKeyInfo>> EnumerateKeys(FormattableStringWithAction input)
        {
            var keyPresses = MapToConsoleKeyPresses(input.Input);
            if (keyPresses.Count > 0)
            {
                for (int i = 0; i < keyPresses.Count - 1; i++)
                {
                    yield return _ => keyPresses[i];
                }
                yield return _ =>
                {
                    input.ActionAfter?.Invoke();
                    return keyPresses[^1];
                };
            }
            else if (input.ActionAfter != null)
            {
                throw new InvalidOperationException("you can specify 'actionAfter' only after keyPress");
            }
        }
    }

    public static ConfiguredCall StubInput(this IConsole consoleStub, List<ConsoleKeyInfo> keys)
    {
        return consoleStub
            .ReadKey(intercept: true)
            .Returns(keys.First(), keys.Skip(1).ToArray());
    }

    private static List<ConsoleKeyInfo> MapToConsoleKeyPresses(FormattableString input)
    {
        ConsoleModifiers modifiersPressed = 0;
        // split the formattable strings into a mix of format placeholders (e.g. {0}, {1}) and literal characters.
        // For the format placeholders, we can get the arguments as their original objects (ConsoleModifiers or ConsoleKey).
        return FormatStringSplit
            .Matches(input.Format)
            .Aggregate(
                seed: new List<ConsoleKeyInfo>(),
                func: (list, key) =>
                {
                    if (key.Value.StartsWith('{') && key.Value.EndsWith('}'))
                    {
                        var formatArgument = input.GetArgument(int.Parse(key.Value.Trim('{', '}')));
                        modifiersPressed = AppendFormatStringArgument(list, key, modifiersPressed, formatArgument);
                    }
                    else
                    {
                        modifiersPressed = AppendLiteralKey(list, key.Value.Single(), modifiersPressed);
                    }

                    return list;
                }
            );
    }

    private static ConsoleModifiers AppendLiteralKey(List<ConsoleKeyInfo> list, char keyChar, ConsoleModifiers modifiersPressed)
    {
        list.Add(CharToConsoleKey(keyChar).ToKeyInfo(keyChar, modifiersPressed));
        return 0;
    }

    public static ConsoleKey CharToConsoleKey(char keyChar) =>
        keyChar switch
        {
            '.' => ConsoleKey.OemPeriod,
            ',' => ConsoleKey.OemComma,
            '-' => ConsoleKey.OemMinus,
            '+' => ConsoleKey.OemPlus,
            '\'' => ConsoleKey.Oem7,
            '/' => ConsoleKey.Divide,
            '!' => ConsoleKey.D1,
            '@' => ConsoleKey.D2,
            '#' => ConsoleKey.D3,
            '$' => ConsoleKey.D4,
            '%' => ConsoleKey.D5,
            '^' => ConsoleKey.D6,
            '&' => ConsoleKey.D7,
            '*' => ConsoleKey.D8,
            '(' => ConsoleKey.D9,
            ')' => ConsoleKey.D0,
            <= (char)255 => (ConsoleKey)char.ToUpper(keyChar),
            _ => ConsoleKey.Oem1
        };

    private static ConsoleModifiers AppendFormatStringArgument(List<ConsoleKeyInfo> list, Match key, ConsoleModifiers modifiersPressed, object formatArgument)
    {
        switch (formatArgument)
        {
            case ConsoleModifiers modifier:
                return modifiersPressed | modifier;
            case ConsoleKey consoleKey:
                var parsed = char.TryParse(key.Value, out char character);
                list.Add(consoleKey.ToKeyInfo(parsed ? character : MapSpecialKey(consoleKey), modifiersPressed));
                return 0;
            default: throw new ArgumentException("Unknown value: " + formatArgument, nameof(formatArgument));
        }
    }

    private static char MapSpecialKey(ConsoleKey consoleKey) =>
        consoleKey switch
        {
            ConsoleKey.Backspace => '\b',
            ConsoleKey.Tab => '\t',
            ConsoleKey.Oem7 => '\'',
            ConsoleKey.Spacebar => ' ',
            _ => '\0' // home, enter, arrow keys, etc
            };

    public static FormattableStringWithAction Input(FormattableString input) => new(input);
    public static FormattableStringWithAction Input(FormattableString input, Action actionAfter) => new(input, actionAfter);

    public readonly struct FormattableStringWithAction
    {
        public readonly FormattableString Input;
        public readonly Action ActionAfter;

        public FormattableStringWithAction(FormattableString input)
            : this(input, null) { }

        public FormattableStringWithAction(FormattableString input, Action actionAfter)
        {
            Input = input;
            ActionAfter = actionAfter;
        }
    }
}
