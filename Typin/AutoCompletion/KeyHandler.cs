﻿namespace Typin.AutoCompletion
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Typin.Console;

    internal sealed class KeyHandler
    {
        private readonly IConsole _console;
        private readonly Dictionary<string, Action> _keyActions;
        private readonly StringBuilder _text = new StringBuilder();

        /// <summary>
        /// Cursor position relative to input.
        /// </summary>
        public int CursorPosition { get; private set; }

        /// <summary>
        /// Last console key info.
        /// </summary>
        public ConsoleKeyInfo LastKey { get; private set; }

        public bool IsStartOfLine => CursorPosition == 0;
        public bool IsEndOfLine => CursorPosition == _text.Length;
        public bool IsEndOfBuffer => _console.CursorLeft == _console.BufferWidth - 1;

        /// <summary>
        /// Current input text.
        /// </summary>
        public string Text => _text.ToString();

        /// <summary>
        /// Initializes an instance of <see cref="KeyHandler"/>.
        /// </summary>
        public KeyHandler(IConsole console)
        {
            _console = console;

            _keyActions = new Dictionary<string, Action>
            {
                ["LeftArrow"] = () => MoveCursorLeft(),
                ["RightArrow"] = MoveCursorRight,

                ["Home"] = () =>
                {
                    while (!IsStartOfLine)
                        MoveCursorLeft();
                },
                ["End"] = () =>
                {
                    while (!IsEndOfLine)
                        MoveCursorRight();
                },
                ["Backspace"] = () => Backspace(),
                ["Delete"] = Delete,
                ["Insert"] = () => { }, // TODO: how to change to insertion/normal mode
                ["Escape"] = ClearLine,

                ["ControlLeftArrow"] = () => DoUntilPrevWordOrWhitespace(() => MoveCursorLeft()),
                ["ControlRightArrow"] = () => DoUntilNextWordOrWhitespace(MoveCursorRight),
                ["ControlBackspace"] = () => BackspacePrevWord(),
                ["ControlDelete"] = () => DoUntilPrevWordOrWhitespace(Delete)
            };
        }

        /// <summary>
        /// Initializes an instance of <see cref="KeyHandler"/>.
        /// </summary>
        public KeyHandler(IConsole console, Dictionary<string, Action> actions) :
            this(console)
        {
            foreach (KeyValuePair<string, Action> action in actions)
                if (!_keyActions.TryAdd(action.Key, action.Value))
                    //Replace when alreadey exists
                    _keyActions[action.Key] = action.Value;
        }

        /// <summary>
        /// Handles key input.
        /// </summary>
        public void Handle(ConsoleKeyInfo keyInfo)
        {
            LastKey = keyInfo;

            if (_keyActions.TryGetValue(BuildKeyInput(keyInfo), out Action action))
            {
                action.Invoke();
            }
            else if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) &&
                     !keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt))
            {
                Write('^');
                Write(keyInfo.Key.ToString());
            }
            else
                Write(keyInfo.KeyChar);
        }

        private string BuildKeyInput(ConsoleKeyInfo keyInfo)
        {
            if (keyInfo.Modifiers != ConsoleModifiers.Control &&
                keyInfo.Modifiers != ConsoleModifiers.Shift)
                return keyInfo.Key.ToString();

            return string.Concat(keyInfo.Modifiers.ToString(), keyInfo.Key.ToString());
        }

        /// <summary>
        /// Resets key handler to allow proper process of next line.
        /// </summary>
        public void Reset()
        {
            CursorPosition = 0;
            _text.Clear();
        }

        private void MoveCursorLeft(int count = 1)
        {
            if (CursorPosition < count)
                count = CursorPosition;

            if (_console.CursorLeft < count)
                _console.SetCursorPosition(_console.BufferWidth - 1, _console.CursorTop - 1);
            else
                _console.SetCursorPosition(_console.CursorLeft - count, _console.CursorTop);

            CursorPosition -= count;
        }

        private void MoveCursorRight()
        {
            if (IsEndOfLine)
                return;

            if (IsEndOfBuffer)
                _console.SetCursorPosition(0, _console.CursorTop + 1);
            else
                _console.SetCursorPosition(_console.CursorLeft + 1, _console.CursorTop);

            ++CursorPosition;
        }

        public void ClearLine()
        {
            while (!IsStartOfLine)
                Backspace();

            _text.Clear();
        }

        public void Write(string str)
        {
            foreach (char character in str)
                Write(character);
        }

        public void Write(char c)
        {
            if (IsEndOfLine)
            {
                _text.Append(c);
                _console.Output.Write(c.ToString());
                CursorPosition++;
            }
            else
            {
                int left = _console.CursorLeft;
                int top = _console.CursorTop;
                string str = _text.ToString().Substring(CursorPosition);
                _text.Insert(CursorPosition, c);
                _console.Output.Write(c.ToString() + str);
                _console.SetCursorPosition(left, top);
                MoveCursorRight();
            }
        }

        public void Backspace(int count = 1)
        {
            if (count > CursorPosition)
                count = CursorPosition;

            MoveCursorLeft(count);
            int index = CursorPosition;
            _text.Remove(index, count);

            string replacement = _text.ToString().Substring(index);
            int left = _console.CursorLeft;
            int top = _console.CursorTop;

            /* TODO: FIX:
             * dotnet BlazorExample.dll> webhost Unhandled exception. System.ArgumentOutOfRangeException: Length cannot be less than zero. (Parameter 'length')
   at System.Text.StringBuilder.Remove(Int32 startIndex, Int32 length)
   at Typin.AutoCompletion.KeyHandler.Backspace(Int32 count) in X:\GitHub\Typin\Typin\AutoCompletion\KeyHandler.cs:line 185
   at Typin.AutoCompletion.AutoCompleteInput.NextAutoComplete() in X:\GitHub\Typin\Typin\AutoCompletion\AutoCompleteInput.cs:line 157
   at Typin.AutoCompletion.AutoCompleteInput.<.ctor>b__14_2() in X:\GitHub\Typin\Typin\AutoCompletion\AutoCompleteInput.cs:line 56
   at Typin.AutoCompletion.KeyHandler.Handle(ConsoleKeyInfo keyInfo) in X:\GitHub\Typin\Typin\AutoCompletion\KeyHandler.cs:line 88
   at Typin.AutoCompletion.AutoCompleteInput.ReadLine() in X:\GitHub\Typin\Typin\AutoCompletion\AutoCompleteInput.cs:line 87
   at Typin.InteractiveCliApplication.<>c__DisplayClass6_0.<GetInput>b__3() in X:\GitHub\Typin\Typin\InteractiveCliApplication.cs:line 117
   at Typin.Console.ConsoleExtensions.WithForegroundColor(IConsole console, ConsoleColor foregroundColor, Action action) in X:\GitHub\Typin\Typin\Console\IConso
le.cs:line 129
   at Typin.InteractiveCliApplication.GetInput(IConsole console, String executableName) in X:\GitHub\Typin\Typin\InteractiveCliApplication.cs:line 112
   at Typin.InteractiveCliApplication.RunInteractivelyAsync(RootSchema root) in X:\GitHub\Typin\Typin\InteractiveCliApplication.cs:line 75
   at Typin.InteractiveCliApplication.PreExecuteCommand(IReadOnlyList`1 commandLineArguments, RootSchema root) in X:\GitHub\Typin\Typin\InteractiveCliApplicatio
n.cs:line 62
   at Typin.CliApplication.RunAsync(IReadOnlyList`1 commandLineArguments, IReadOnlyDictionary`2 environmentVariables) in X:\GitHub\Typin\Typin\CliApplication.cs
:line 175
   at Typin.CliApplication.RunAsync(IReadOnlyList`1 commandLineArguments) in X:\GitHub\Typin\Typin\CliApplication.cs:line 149
   at Typin.CliApplication.RunAsync() in X:\GitHub\Typin\Typin\CliApplication.cs:line 128
   at BlazorExample.Program.Main() in X:\GitHub\Typin\Examples\BlazorExample\Program.cs:line 10
   at BlazorExample.Program.<Main>()
             */

            string spaces = new string(' ', count);
            _console.Output.Write(string.Format("{0}{1}", replacement, spaces));
            _console.SetCursorPosition(left, top);
        }

        public void BackspacePrevWord()
        {
            DoUntilPrevWordOrWhitespace(() => Backspace());
        }

        private void Delete()
        {
            if (IsEndOfLine)
                return;

            int index = CursorPosition;
            _text.Remove(index, 1);

            string replacement = _text.ToString().Substring(index);
            int left = _console.CursorLeft;
            int top = _console.CursorTop;
            _console.Output.Write(string.Format("{0} ", replacement));
            _console.SetCursorPosition(left, top);
        }

        private void DoUntilPrevWordOrWhitespace(Action action)
        {
            int v = CursorPosition - 1;
            if (v < 0)
                return;

            if (char.IsWhiteSpace(_text[v]))
            {
                do
                {
                    action();
                }
                while (!IsStartOfLine && char.IsWhiteSpace(_text[CursorPosition - 1]));

                return;
            }

            do
            {
                action();
            }
            while (!IsStartOfLine && !char.IsWhiteSpace(_text[CursorPosition - 1]));
        }

        private void DoUntilNextWordOrWhitespace(Action action)
        {
            if (IsEndOfLine)
                return;

            if (char.IsWhiteSpace(_text[CursorPosition]))
            {
                do
                {
                    action();
                }
                while (!IsEndOfLine && char.IsWhiteSpace(_text[CursorPosition]));

                return;
            }

            do
            {
                action();
            }
            while (!IsEndOfLine && !char.IsWhiteSpace(_text[CursorPosition]));
        }
    }
}
