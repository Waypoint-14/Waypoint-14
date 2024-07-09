using Content.Shared.ActionBlocker;
using Content.Shared.Input;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Standing;

// Unfortunately cannot be shared because some standing conditions are server-side only
public sealed class LayingDownSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly Shared.Standing.StandingStateSystem _standing = default!; // WHY IS THERE TWO DIFFERENT STANDING SYSTEMS?!
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleStanding, InputCmdHandler.FromDelegate(ToggleStanding, handle: false, outsidePrediction: false))
            .Register<LayingDownSystem>();

        SubscribeLocalEvent<LayingDownComponent, StoodEvent>(DoRefreshMovementSpeed);
        SubscribeLocalEvent<LayingDownComponent, DownedEvent>(DoRefreshMovementSpeed);
        SubscribeLocalEvent<LayingDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<LayingDownSystem>();
    }

    private void DoRefreshMovementSpeed(EntityUid uid, LayingDownComponent component, object args)
    {
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshMovementSpeed(EntityUid uid, LayingDownComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (TryComp<StandingStateComponent>(uid, out var standingState) && standingState.Standing)
            return;

        args.ModifySpeed(component.DownedSpeedMultiplier, component.DownedSpeedMultiplier);
    }

    private void ToggleStanding(ICommonSession? session)
    {
        if (session is not { } playerSession)
            return;

        if ((playerSession.AttachedEntity is not { Valid: true } uid || !Exists(uid)))
            return;

        if (!TryComp<StandingStateComponent>(uid, out var standingState) || !TryComp<LayingDownComponent>(uid, out var layingDown))
            return;

        var success = ToggleStandingImpl(uid, standingState, layingDown, out var popupBranch);

        // If successful, show popup to self and others. Otherwise, only to self.
        _popups.PopupEntity(Loc.GetString($"laying-comp-{popupBranch}-self", ("entity", uid)), uid, uid);

        if (success)
        {
            _popups.PopupEntity(Loc.GetString($"laying-comp-{popupBranch}-other", ("entity", uid)), uid, Filter.PvsExcept(uid), true);

            layingDown.CooldownUntil = _timing.CurTime + layingDown.Cooldown;
        }
    }

    private bool ToggleStandingImpl(EntityUid uid, StandingStateComponent standingState, LayingDownComponent layingDown, out string popupBranch)
    {
        var success = layingDown.CooldownUntil <= _timing.CurTime;

        // Note: &= leads to the right-hand side being evaluated regardless of what's on left-hand, so we use normal assignment here instead.
        if (_standing.IsDown(uid, standingState))
        {
            success = success && _standing.Stand(uid, standingState, force: false);
            popupBranch = success ? "stand-success" : "stand-fail";
        }
        else
        {
            success = success && _standing.Down(uid, standingState: standingState, playSound: true, dropHeldItems: false);
            popupBranch = success ? "lay-success" : "lay-fail";
        }

        return success;
    }
}
