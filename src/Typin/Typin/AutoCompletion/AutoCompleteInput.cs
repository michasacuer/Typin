﻿namespace Typin.AutoCompletion
{
    using System;
    using System.Collections.Generic;
    using Typin.Console;

    internal class AutoCompleteInput
    {
        private readonly LineInputHandler _lineInputHandler;
        private string[] _completions = Array.Empty<string>();
        private int _completionStart;
        private int _completionsIndex;

        private bool IsInAutoCompleteMode => AutoCompletionHandler != null && _completions.Length != 0;

        public InputHistoryProvider History { get; }
        public IAutoCompletionHandler? AutoCompletionHandler { get; set; }

        /// <summary>
        /// Initializes an instance of <see cref="AutoCompleteInput"/>.
        /// </summary>
        public AutoCompleteInput(IConsole console,
                                 HashSet<ShortcutDefinition>? userDefinedShortcut = null)
        {
            History = new InputHistoryProvider();

            var keyActions = new HashSet<ShortcutDefinition>
            {
                // History
                new ShortcutDefinition(ConsoleKey.UpArrow, () =>
                {
                    if (History.SelectionUp())
                    {
                        _lineInputHandler.ClearLine();
                        _lineInputHandler.Write(History.GetSelection());
                    }
                }),
                new ShortcutDefinition(ConsoleKey.DownArrow, () =>
                {
                    if (History.SelectionDown())
                    {
                        _lineInputHandler.ClearLine();
                        _lineInputHandler.Write(History.GetSelection());
                    }
                }),

                // AutoComplete
                new ShortcutDefinition(ConsoleKey.Tab, () =>
                {
                    if (IsInAutoCompleteMode)
                        NextAutoComplete();
                    else
                        InitAutoComplete();
                }),
                new ShortcutDefinition(ConsoleKey.Tab, ConsoleModifiers.Shift, () =>
                {
                    if (IsInAutoCompleteMode)
                        PreviousAutoComplete();
                    else
                        InitAutoComplete(true);
                })
            };

            _lineInputHandler = new LineInputHandler(console, keyActions, userDefinedShortcut);
            _lineInputHandler.InputModified += InputModifiedCallback;
        }

        private void InputModifiedCallback()
        {
            ResetAutoComplete();
            History.ResetSelection();
        }

        /// <summary>
        /// Reads a line from input.
        /// </summary>
        public string ReadLine()
        {
            string text = _lineInputHandler.ReadLine();

            ResetAutoComplete();

            if (History.IsEnabled)
            {
                History.TryAddEntry(text);
                History.ResetSelection();
            }

            return text;
        }

        /// <summary>
        /// Reads a line from array.
        /// </summary>
        public string ReadLine(params ConsoleKeyInfo[] line)
        {
            string text = _lineInputHandler.Read(line);

            ResetAutoComplete();

            if (History.IsEnabled)
            {
                History.TryAddEntry(text);
                History.ResetSelection();
            }

            return text;
        }

        private void InitAutoComplete(bool fromEnd = false)
        {
            if (AutoCompletionHandler is null || !_lineInputHandler.IsEndOfLine)
                return;

            string text = _lineInputHandler.CurrentInput;

            _completionStart = text.LastIndexOfAny(AutoCompletionHandler.Separators);
            _completionStart = _completionStart == -1 ? 0 : _completionStart + 1;

            _completions = AutoCompletionHandler.GetSuggestions(text, _completionStart);

            if (_completions.Length == 0)
                return;

            _lineInputHandler.ClearLine();
            _completionsIndex = fromEnd ? _completions.Length - 1 : 0;
            _lineInputHandler.Write(_completions[_completionsIndex]);
        }

        private void NextAutoComplete()
        {
            _lineInputHandler.Backspace(_lineInputHandler.CursorPosition - _completionStart);

            if (++_completionsIndex == _completions.Length)
                _completionsIndex = 0;

            _lineInputHandler.Write(_completions[_completionsIndex]);
        }

        private void PreviousAutoComplete()
        {
            _lineInputHandler.Backspace(_lineInputHandler.CursorPosition - _completionStart);

            if (--_completionsIndex == -1)
                _completionsIndex = _completions.Length - 1;

            _lineInputHandler.Write(_completions[_completionsIndex]);
        }

        private void ResetAutoComplete()
        {
            _completions = Array.Empty<string>();
            _completionsIndex = 0;
        }
    }
}
