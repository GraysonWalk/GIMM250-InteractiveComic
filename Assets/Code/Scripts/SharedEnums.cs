using System;

public enum LoopCount
{
    Loop0 = 0,
    Loop1 = 1,
    Loop2 = 2,
    Loop3 = 3,
    Loop4 = 4
}

/// <summary>
///     Boundary helpers for <see cref="LoopCount"/>.
/// </summary>
public static class LoopCountBounds
{
    /// <summary>
    ///     The highest defined loop, derived automatically from the enum.
    /// </summary>
    public static readonly LoopCount Last =
        (LoopCount)(Enum.GetValues(typeof(LoopCount)).Length - 1);
}

// Rename OptionA / OptionB to the real thematic names once they are decided.
// Only change these values in this file — all consumers update automatically.
public enum ScienceChoice    { None, OptionA, OptionB }
public enum PhilosophyChoice { None, OptionA, OptionB }
public enum LeadershipChoice { None, OptionA, OptionB }