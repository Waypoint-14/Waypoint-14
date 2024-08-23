using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Chemistry.EntitySystems;
using Content.Client.Guidebook.Richtext;
using Content.Client.Message;
using Content.Client.Nutrition.EntitySystems;
using Content.Client.UserInterface.ControlExtensions;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook.Controls;

/// <summary>
///     Control for embedding a food recipe into a guidebook.
/// </summary>
[UsedImplicitly, GenerateTypedNameReferences]
public sealed partial class GuideFoodEmbed : BoxContainer, IDocumentTag, ISearchableControl
{
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly FoodGuideDataSystem _foodGuideData;
    private readonly ISawmill _logger = default!;

    public GuideFoodEmbed()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _foodGuideData = _systemManager.GetEntitySystem<FoodGuideDataSystem>();
        _logger = Logger.GetSawmill("food guide");
        MouseFilter = MouseFilterMode.Stop;
    }

    public GuideFoodEmbed(FoodGuideEntry entry) : this()
    {
        GenerateControl(entry);
    }

    public bool CheckMatchesSearch(string query)
    {
        return FoodName.GetMessage()?.Contains(query) == true || FoodDescription.GetMessage()?.Contains(query) == true;
    }

    public void SetHiddenState(bool state, string query)
    {
        Visible = CheckMatchesSearch(query) ? state : !state;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("Food", out var id))
        {
            _logger.Error("Food embed tag is missing food prototype argument.");
            return false;
        }

        if (!_foodGuideData.TryGetData(id, out var data))
        {
            _logger.Warning($"Specified food prototype \"{id}\" does not have any known sources.");
            return false;
        }

        GenerateControl(data);

        control = this;
        return true;
    }

    private void GenerateControl(FoodGuideEntry data)
    {
        _prototype.TryIndex(data.Result, out var proto);
        if (proto == null)
        {
            FoodName.SetMarkup(Loc.GetString("guidebook-food-unknown-proto", ("id", data.Result)));
            return;
        }

        var composition = data.Composition
            .Select(it => _prototype.TryIndex<ReagentPrototype>(it.Reagent.Prototype, out var reagent) ? (reagent, it.Quantity) : (null, 0))
            .Where(it => it.reagent is not null)
            .Cast<(ReagentPrototype, FixedPoint2)>()
            .ToList();

        #region Colors

        CalculateColors(composition, out var textColor, out var backgroundColor);

        NameBackground.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = backgroundColor
        };
        FoodName.SetMarkup(Loc.GetString("guidebook-food-name", ("color", textColor), ("name", proto.Name)));

        #endregion

        #region Sources

        foreach (var source in data.Sources.OrderBy(it => it.OutputCount))
        {
            var control = new GuideFoodSource(proto, source, _prototype);
            SourcesDescriptionContainer.AddChild(control);
        }

        #endregion

        #region Composition

        foreach (var (reagent, quantity) in composition)
        {
            var control = new GuideFoodComposition(reagent, quantity);
            CompositionDescriptionContainer.AddChild(control);
        }

        #endregion

        FormattedMessage description = new();
        description.AddText(proto?.Description ?? string.Empty);
        // Cannot describe food flavor or smth beause food is entirely server-side

        FoodDescription.SetMessage(description);
    }

    private void CalculateColors(List<(ReagentPrototype, FixedPoint2)> composition, out Color text, out Color background)
    {
        // Background color is calculated as the weighted average of the colors of the composition.
        // Text color is determined based on background luminosity.
        float r = 0, g = 0, b = 0;
        FixedPoint2 weight = 0;

        foreach (var (proto, quantity) in composition)
        {
            var tcolor = proto.SubstanceColor;
            var prevalence =
                quantity <= 0 ? 0f
                : weight == 0f ? 1f
                : (quantity / (weight + quantity)).Float();

            r = r * (1 - prevalence) + tcolor.R * prevalence;
            g = g * (1 - prevalence) + tcolor.G * prevalence;
            b = b * (1 - prevalence) + tcolor.B * prevalence;

            if (quantity > 0)
                weight += quantity;
        }

        // Copied from GuideReagentEmbed which was probably copied from stackoverflow. This is the formula for color luminosity.
        var lum = 0.2126f * r + 0.7152f * g + 0.0722f;

        background = new Color(r, g, b);
        text = lum > 0.5f ? Color.Black : Color.White;
    }
}
