using System.Collections.Generic;
using System.Linq;

/// <summary>
///     Pure static helper — no state, no dependencies.
///     Selects the next comic panel to display based on rank and loop eligibility.
/// </summary>
public static class PanelSelector
{
    /// <summary>
    ///     Returns the lowest-rank eligible panel above the current panel's rank.
    ///     If none exists (end of loop), wraps to the lowest-rank eligible panel overall.
    ///     Pass null for currentPanel on first launch to get the very first panel.
    ///     Returns null only if no panels are eligible for the given loop count.
    /// </summary>
    public static ComicPanel NextPanel(List<ComicPanel> comicPanels, ComicPanel currentPanel, LoopCount loopCount)
    {
        if (currentPanel == null)
            return LowestEligible(comicPanels, loopCount);

        return comicPanels
                   .Where(cp => cp.Rank > currentPanel.Rank && cp.FirstLoop <= loopCount)
                   .OrderBy(cp => cp.Rank)
                   .FirstOrDefault()
               ?? LowestEligible(comicPanels, loopCount); // null-coalescing wrap-around
    }

    /// <summary>Returns the eligible panel with the lowest rank, or null if none exist.</summary>
    private static ComicPanel LowestEligible(List<ComicPanel> comicPanels, LoopCount loopCount)
    {
        return comicPanels
            .Where(cp => cp.FirstLoop <= loopCount)
            .OrderBy(cp => cp.Rank)
            .FirstOrDefault();
    }
}

/*
// Claude developed all linq logic
*/