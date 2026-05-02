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
    ///     Accepts IEnumerable&lt;IComicPanel&gt; — List&lt;ComicPanel&gt; satisfies this via covariance.
    /// </summary>
    public static IComicPanel NextPanel(IEnumerable<IComicPanel> comicPanels, IComicPanel currentPanel, LoopCount loopCount)
    {
        if (currentPanel == null)
            return LowestEligible(comicPanels, loopCount);

        return comicPanels
                   .Where(cp => cp.Rank > currentPanel.Rank && IsEligible(cp, loopCount))
                   .OrderBy(cp => cp.Rank)
                   .FirstOrDefault()
               ?? LowestEligible(comicPanels, loopCount); // null-coalescing wrap-around
    }

    /// <summary>Returns the eligible panel with the lowest rank, or null if none exist.</summary>
    private static IComicPanel LowestEligible(IEnumerable<IComicPanel> comicPanels, LoopCount loopCount)
    {
        return comicPanels
            .Where(cp => IsEligible(cp, loopCount))
            .OrderBy(cp => cp.Rank)
            .FirstOrDefault();
    }

    /// <summary>True when the panel should appear in the given loop.</summary>
    private static bool IsEligible(IComicPanel cp, LoopCount loopCount) =>
        cp.FirstLoop <= loopCount && cp.LastLoop >= loopCount;
}