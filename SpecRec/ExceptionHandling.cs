using System;

namespace SpecRec
{
    [Flags]
    public enum ExceptionHandling
    {
        None = 0,
        SkipVerify = 1,
        PassThrough = 2,
        Trace = 4
    }
}