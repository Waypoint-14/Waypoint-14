using System.Linq;
using Content.Client.Stylesheets;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls;

/// <summary>
///     A simple yet good-looking tab container using normal UI elements with multiple styles
///     <br />
///     Because nobody else could do it better.
/// </summary>
[GenerateTypedNameReferences]
public sealed partial class NeoTabContainer : BoxContainer
{
    private readonly Dictionary<Control, BaseButton> _tabs = new();
    private readonly List<Control> _controls = new();
    private readonly ButtonGroup _tabGroup = new(false);

    /// All children within the <see cref="ContentContainer"/>
    public OrderedChildCollection Contents => ContentContainer.Children;
    /// All children within the <see cref="ContentContainer"/> that are visible
    public List<Control> VisibleContents => Contents.Where(c => c == CurrentControl).ToList();

    /// All children within the <see cref="TabContainer"/>
    public OrderedChildCollection Tabs => TabContainer.Children;
    /// All children within the <see cref="TabContainer"/> that are visible
    public List<Control> VisibleTabs => Tabs.Where(c => c.Visible).ToList();

    public Control? CurrentControl { get; private set; }
    public int? CurrentTab => _controls.FirstOrDefault(control => control == CurrentControl) switch
    {
        { } control => _controls.IndexOf(control),
        _ => null,
    };


    /// <inheritdoc cref="NeoTabContainer"/>
    public NeoTabContainer()
    {
        RobustXamlLoader.Load(this);

        LayoutChanged(Horizontal);
        ScrollingChanged(HScrollEnabled, VScrollEnabled);
    }

    //TODO This sucks, put this on some post-init if that exists
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        foreach (var child in Children.Where(child => child.Name is not nameof(Container)).ToList())
        {
            child.Orphan();
            AddTab(child, child.Name ?? "Untitled Tab");
        }
    }

    protected override void ChildRemoved(Control child)
    {
        if (_tabs.Remove(child, out var button))
            button.Dispose();

        // Set the current tab to a different control
        if (CurrentControl == child)
        {
            var previous = _controls.IndexOf(child) - 1;

            if (previous > -1)
                SelectTab(_controls[previous]);
            else
                CurrentControl = null;
        }

        _controls.Remove(child);
        base.ChildRemoved(child);
        UpdateTabMerging();
    }


    /// <summary>
    ///     Adds a tab to this container
    /// </summary>
    /// <param name="control">The tab contents</param>
    /// <param name="title">The title of the tab</param>
    /// <param name="updateTabMerging">Whether the tabs should fix their styling automatically. Useful if you're doing tons of updates at once</param>
    /// <returns>The index of the new tab</returns>
    public int AddTab(Control control, string title, bool updateTabMerging = true)
    {
        var button = new Button
        {
            Text = title,
            Group = _tabGroup,
            MinHeight = 32,
            MaxHeight = 32,
            HorizontalExpand = true,
        };
        button.OnPressed += _ => SelectTab(control);

        TabContainer.AddChild(button);
        ContentContainer.AddChild(control);
        _controls.Add(control);
        _tabs.Add(control, button);

        // Show it if it has content
        if (ContentContainer.ChildCount > 1)
            control.Visible = false;
        else
            // Select it if it's the only tab
            SelectTab(control);

        if (updateTabMerging)
            UpdateTabMerging();
        return ChildCount - 1;
    }

    /// <summary>
    ///     Removes the tab associated with the given index
    /// </summary>
    /// <param name="index">The index of the tab to remove</param>
    /// <param name="updateTabMerging">Whether the tabs should fix their styling automatically. Useful if you're doing tons of updates at once</param>
    /// <returns>True if the tab was removed, false otherwise</returns>
    public bool RemoveTab(int index, bool updateTabMerging = true)
    {
        if (index < 0 || index >= _controls.Count)
            return false;

        var control = _controls[index];
        RemoveTab(control, updateTabMerging);
        return true;
    }

    /// <summary>
    ///     Removes the tab associated with the given control
    /// </summary>
    /// <param name="control">The control to remove</param>
    /// <param name="updateTabMerging">Whether the tabs should fix their styling automatically. Useful if you're doing tons of updates at once</param>
    /// <returns>True if the tab was removed, false otherwise</returns>
    public bool RemoveTab(Control control, bool updateTabMerging = true)
    {
        if (!_tabs.TryGetValue(control, out var button))
            return false;

        button.Dispose();
        control.Dispose();
        if (updateTabMerging)
            UpdateTabMerging();
        return true;
    }


    /// <summary>
    ///     Sets the title of the tab associated with the given index
    /// </summary>
    public void SetTabTitle(int index, string title)
    {
        if (index < 0 || index >= _controls.Count)
            return;

        var control = _controls[index];
        SetTabTitle(control, title);
    }

    /// <summary>
    ///     Sets the title of the tab associated with the given control
    /// </summary>
    public void SetTabTitle(Control control, string title)
    {
        if (!_tabs.TryGetValue(control, out var button))
            return;

        if (button is Button b)
            b.Text = title;
    }

    /// <summary>
    ///     Shows or hides the tab associated with the given index
    /// </summary>
    public void SetTabVisible(int index, bool visible)
    {
        if (index < 0 || index >= _controls.Count)
            return;

        var control = _controls[index];
        SetTabVisible(control, visible);
    }

    /// <summary>
    ///     Shows or hides the tab associated with the given control
    /// </summary>
    public void SetTabVisible(Control control, bool visible)
    {
        if (!_tabs.TryGetValue(control, out var button))
            return;

        button.Visible = visible;
        UpdateTabMerging();
    }

    /// <summary>
    ///     Selects the tab associated with the control
    /// </summary>
    public void SelectTab(Control control)
    {
        if (CurrentControl != null)
            CurrentControl.Visible = false;

        var button = _tabs[control];
        button.Pressed = true;
        control.Visible = true;
        CurrentControl = control;
    }

    /// <summary>
    ///     Sets the style of every visible tab's Button to be Open to Right, Both, or Left depending on position unless <see cref="Horizontal"/> is true
    /// </summary>
    public void UpdateTabMerging()
    {
        if (!Horizontal)
            return;

        var visibleTabs = VisibleTabs;

        switch (visibleTabs.Count)
        {
            case 0:
                return;
            case 1:
            {
                var button = visibleTabs[0];
                button.RemoveStyleClass(StyleBase.ButtonOpenRight);
                button.RemoveStyleClass(StyleBase.ButtonOpenBoth);
                button.RemoveStyleClass(StyleBase.ButtonOpenLeft);
                return;
            }
        }

        for (var i = 0; i < visibleTabs.Count; i++)
        {
            var button = visibleTabs[i];
            button.RemoveStyleClass(StyleBase.ButtonOpenRight);
            button.RemoveStyleClass(StyleBase.ButtonOpenBoth);
            button.RemoveStyleClass(StyleBase.ButtonOpenLeft);

            if (i == 0)
                button.AddStyleClass(StyleBase.ButtonOpenRight);
            else if (i == visibleTabs.Count - 1)
                button.AddStyleClass(StyleBase.ButtonOpenLeft);
            else
                button.AddStyleClass(StyleBase.ButtonOpenBoth);
        }
    }
}
