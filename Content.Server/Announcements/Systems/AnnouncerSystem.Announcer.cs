using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Announcements.Prototypes;
using Content.Shared.CCVar;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Utility;

namespace Content.Server.Announcements.Systems;

public sealed partial class AnnouncerSystem
{
    private void OnRoundRestarting(RoundRestartCleanupEvent ev)
    {
        var announcer = _config.GetCVar(CCVars.Announcer);
        if (string.IsNullOrEmpty(announcer))
            SetAnnouncer(PickAnnouncer());
        else
            SetAnnouncer(announcer);
    }


    /// <summary>
    ///     Picks a random announcer
    /// </summary>
    /// <remarks>Probably not very useful for any other system</remarks>
    public AnnouncerPrototype PickAnnouncer()
    {
        var list = _proto.Index<WeightedRandomPrototype>(_config.GetCVar(CCVars.AnnouncerList));
        var modWeights = list.Weights.Where(a => !_config.GetCVar(CCVars.AnnouncerBlacklist).Contains(a.Key));

        list = new WeightedRandomPrototype();
        foreach (var (key, value) in modWeights)
            list.Weights.Add(key, value);

        return _proto.Index<AnnouncerPrototype>(list.Pick());
    }


    /// <summary>
    ///     Sets the announcer
    /// </summary>
    /// <param name="announcerId">ID of the announcer to choose</param>
    public void SetAnnouncer(string announcerId)
    {
        if (!_proto.TryIndex<AnnouncerPrototype>(announcerId, out var announcer))
            DebugTools.Assert($"Set announcer {announcerId} does not exist, attempting to use previously set one.");
        else
            Announcer = announcer;
    }

    /// <summary>
    ///     Sets the announcer
    /// </summary>
    /// <param name="announcer">The announcer prototype to set the current announcer to</param>
    public void SetAnnouncer(AnnouncerPrototype announcer)
    {
        Announcer = announcer;
    }
}
