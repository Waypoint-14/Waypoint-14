using Content.Client.UserInterface.Controls;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Medical;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;


namespace Content.Client.Eye.PenLight.UI

{

    [GenerateTypedNameReferences]

    public sealed partial class PenLightWindow : FancyWindow
    {
        private readonly IEntityManager _entityManager;
        private const int LightHeight = 150;
        private const int LightWidth = 900;

        public PenLightWindow()
        {
            RobustXamlLoader.Load(this);

            var dependencies = IoCManager.Instance!;
            _entityManager = dependencies.Resolve<IEntityManager>();
        }

        public void Diagnose(PenLightUserMessage msg)
        {
            var target = _entityManager.GetEntity(msg.TargetEntity);

            if (target == null
                || !_entityManager.TryGetComponent<DamageableComponent>(target, out var damageable))
            {
                NoPatientDataText.Visible = true;
                return;
            }

            NoPatientDataText.Visible = false;

            string entityName = Loc.GetString("pen-light-window-entity-unknown-text");
            if (_entityManager.HasComponent<MetaDataComponent>(target.Value))
            {
                entityName = Identity.Name(target.Value, _entityManager);
            }

            PatientName.Text = Loc.GetString(
                "pen-light-window-entity-eyes-text",
                ("entityName", entityName)
            );

            // Blind
            Blind.Text = msg.Blind == true ? Loc.GetString("pen-light-exam-blind-text") : string.Empty;

            // EyeDamage
            EyeDamage.Text = msg.EyeDamage == true ? Loc.GetString("pen-light-exam-eyedamage-text") : string.Empty;

            // Drunk
            Drunk.Text = msg.Drunk == true ? Loc.GetString("pen-light-exam-drunk-text") : string.Empty;

            // Hallucinating
            SeeingRainbows.Text = msg.SeeingRainbows == true ? Loc.GetString("pen-light-exam-hallucinating-text") : string.Empty;

            // Healthy
            Healthy.Text = msg.Healthy == true ? Loc.GetString("pen-light-exam-healthy-text") : string.Empty;

            SetHeight = LightHeight;
            SetWidth = LightWidth;
        }

    }
}
