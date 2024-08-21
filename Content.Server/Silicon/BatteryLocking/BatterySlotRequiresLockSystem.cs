using Content.Shared.Containers.ItemSlots;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Silicon.Components;
using Content.Shared.IdentityManagement; 

namespace Content.Server.Silicon.BatteryLocking;

public sealed class BatterySlotRequiresLockSystem : EntitySystem

{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!; 

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BatterySlotRequiresLockComponent, LockToggledEvent>(LockToggled);
        SubscribeLocalEvent<BatterySlotRequiresLockComponent, LockToggleAttemptEvent>(LockToggleAttempted); 

    }
    private void LockToggled(EntityUid uid, BatterySlotRequiresLockComponent component, LockToggledEvent args)
    {
        if (!TryComp<LockComponent>(uid, out var lockComp) || !TryComp<ItemSlotsComponent>(uid, out var itemslots))
            return;
        if (!_itemSlotsSystem.TryGetSlot(uid, component.ItemSlot, out var slot, itemslots))
            return;
        _itemSlotsSystem.SetLock(uid, slot, lockComp.Locked, itemslots);
    }

    private void LockToggleAttempted(EntityUid uid, BatterySlotRequiresLockComponent component, LockToggleAttemptEvent args)
    {
        if (args.User != uid && TryComp<SiliconComponent>(uid, out var siliconComp))
            _popupSystem.PopupEntity(Loc.GetString("batteryslotrequireslock-component-alert-owner", ("user", Identity.Entity(args.User, EntityManager))), uid, uid, PopupType.Large);
    }

}