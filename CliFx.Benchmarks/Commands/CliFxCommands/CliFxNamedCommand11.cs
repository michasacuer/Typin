﻿namespace CliFx.Benchmarks.Commands.CliFxCommands
{
    using System.Threading.Tasks;
    using CliFx.Attributes;

    [Command("named-command11")]
    public class CliFxNamedCommandCommand11 : ICommand
    {
        [CommandOption("str", 's')]
        public string? StrOption { get; set; }

        [CommandOption("int", 'i')]
        public int IntOption { get; set; }

        [CommandOption("bool", 'b')]
        public bool BoolOption { get; set; }

        public ValueTask ExecuteAsync(IConsole console)
        {
            return default;
        }
    }
}