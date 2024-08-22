using Content.Client.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Nutrition.EntitySystems;

public sealed class FoodGuideDataSystem : SharedFoodGuideDataSystem
{
    public override void Initialize()
    {
        SubscribeNetworkEvent<FoodGuideRegistryChangedEvent>(OnReceiveRegistryUpdate);
    }

    private void OnReceiveRegistryUpdate(FoodGuideRegistryChangedEvent message)
    {
        Registry = message.Changeset;
    }

    public bool TryGetData(EntProtoId result, out FoodGuideEntry entry)
    {
        var index = Registry.FindIndex(it => it.Result == result);
        if (index == -1)
        {
            entry = default;
            return false;
        }

        entry = Registry[index];
        return true;
    }
}
