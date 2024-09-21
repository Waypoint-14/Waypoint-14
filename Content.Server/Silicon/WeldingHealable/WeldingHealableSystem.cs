using Content.Server.Silicon.WeldingHealing;
using Content.Server.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Silicon.WeldingHealing;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Server.Silicon.WeldingHealable
{
    public sealed class WeldingHealableSystem : SharedWeldingHealableSystem
    {
        [Dependency] private readonly SharedToolSystem _toolSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger= default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer= default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<WeldingHealableComponent, InteractUsingEvent>(Repair);
            SubscribeLocalEvent<WeldingHealableComponent, SiliconRepairFinishedEvent>(OnRepairFinished);
        }

        private void OnRepairFinished(EntityUid uid, WeldingHealableComponent healableComponentcomponent, SiliconRepairFinishedEvent args)
        {
            if (args.Cancelled || args.Used == null
                || !EntityManager.TryGetComponent(args.Target, out DamageableComponent? damageable)
                || !EntityManager.TryGetComponent(args.Used, out WeldingHealingComponent? component)
                || damageable.DamageContainerID != null
                && !component.DamageContainers.Contains(damageable.DamageContainerID))
                return;

            var damageChanged = _damageableSystem.TryChangeDamage(uid, component.Damage, true, false, origin: args.User);


            if (!HasDamage(damageable, component))
                return;

            if (TryComp(args.Used, out WelderComponent? welder) &&
                TryComp(args.Used, out SolutionContainerManagerComponent? solutionContainer))
            {
                Entity<SolutionComponent>? sol = new();
                if (!_solutionContainer.ResolveSolution(((EntityUid) args.Used, solutionContainer), welder.FuelSolutionName, ref sol, out _))
                    return;
                _solutionContainer.RemoveReagent(sol.Value, welder.FuelReagent, component.FuelCost);
            }

            var str = Loc.GetString("comp-repairable-repair",
                ("target", uid),
                ("tool", args.Used!));
            _popup.PopupEntity(str, uid, args.User);


            if (args.Used.HasValue)
            {
                args.Handled = _toolSystem.UseTool(args.Used.Value, args.User, uid, args.Delay, component.QualityNeeded, new SiliconRepairFinishedEvent
                {
                    Delay = args.Delay
                });
            }
        }



        private async void Repair(EntityUid uid, WeldingHealableComponent healableComponent, InteractUsingEvent args)
        {
            if (args.Handled
                || !EntityManager.TryGetComponent(args.Used, out WeldingHealingComponent? component)
                || !EntityManager.TryGetComponent(args.Target, out DamageableComponent? damageable)
                || damageable.DamageContainerID != null
                && !component.DamageContainers.Contains(damageable.DamageContainerID)
                || !HasDamage(damageable, component)
                || !_toolSystem.HasQuality(args.Used, component.QualityNeeded))
                return;

            float delay = component.DoAfterDelay;

            // Add a penalty to how long it takes if the user is repairing itself
            if (args.User == args.Target)
            {
                if (!component.AllowSelfHeal)
                    return;

                delay *= component.SelfHealPenalty;
            }

            // Run the repairing doafter
            args.Handled = _toolSystem.UseTool(args.Used, args.User, args.Target, delay, component.QualityNeeded, new SiliconRepairFinishedEvent
            {
                Delay = delay,
            });

        }
        private bool HasDamage(DamageableComponent component, WeldingHealingComponent healable)
        {
            var damageableDict = component.Damage.DamageDict;
            var healingDict = healable.Damage.DamageDict;
            foreach (var type in healingDict)
            {
                if (damageableDict[type.Key].Value > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
