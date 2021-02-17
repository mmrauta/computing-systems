﻿namespace VirtualMachine
{
    public enum CommandType
    {
        Arithmetic,
        Push,
        Pop,
        Label,
        Goto,
        If,
        Function,
        Return,
        Call,
        Unrecognized
    }
}
