using System;
using Timberborn.BaseComponentSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace PersistentWorkAreas
{
    public sealed class WorkAreaFragment : IEntityPanelFragment
    {
        private static readonly Color Cream = new Color(1f, .95f, .79f);
        private static readonly Color Gold = new Color(.92f, .72f, .35f);
        private readonly WorkAreaService _service;
        private readonly ILoc _loc;
        private VisualElement _root;
        private Button _toggle;
        private VisualElement _box;
        private VisualElement _check;
        private Label _state;
        private Label _hint;
        private BaseComponent _entity;
        private bool _pinned;
        private bool _hovered;
        private bool _focused;
        public WorkAreaFragment(WorkAreaService service, ILoc loc) { _service = service; _loc = loc; }

        public VisualElement InitializeFragment()
        {
            _root = new VisualElement { name = "PersistentWorkAreasFragment" };
            _root.style.paddingLeft = _root.style.paddingRight = 10;
            _root.style.paddingTop = _root.style.paddingBottom = 10;
            _root.style.marginTop = _root.style.marginBottom = 5;
            _root.style.backgroundColor = new Color(.10f, .18f, .17f, .98f);
            _root.style.color = Cream;
            Border(_root, new Color(.34f, .46f, .37f), 1);
            _root.style.borderLeftWidth = 4;
            Round(_root, 5);

            var heading = new Label(_loc.T(LocKeys.Heading));
            heading.style.fontSize = 11;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.color = Gold;
            heading.style.marginBottom = 7;
            _root.Add(heading);

            // A fully drawn control avoids relying on Unity's default Toggle theme,
            // which Timberborn does not render as a visible checkbox in this panel.
            _toggle = new Button(() => _service.SetPinned(_entity, !_service.IsPinned(_entity)))
            {
                name = "PersistentWorkAreasPin",
                tooltip = _loc.T(LocKeys.PinTooltip)
            };
            _toggle.style.flexDirection = FlexDirection.Row;
            _toggle.style.alignItems = Align.Center;
            _toggle.style.minHeight = 46;
            _toggle.style.paddingLeft = _toggle.style.paddingRight = 10;
            _toggle.style.paddingTop = _toggle.style.paddingBottom = 8;
            _toggle.style.marginLeft = _toggle.style.marginRight = 0;
            _toggle.style.marginTop = _toggle.style.marginBottom = 0;
            Border(_toggle, Gold, 2);
            Round(_toggle, 4);

            _box = new VisualElement { pickingMode = PickingMode.Ignore };
            _box.style.width = _box.style.height = 24;
            _box.style.flexShrink = 0;
            _box.style.marginRight = 10;
            _box.style.alignItems = Align.Center;
            _box.style.justifyContent = Justify.Center;
            Border(_box, Gold, 2);
            Round(_box, 3);
            // Draw the check using borders, so no special font glyph is required.
            _check = new VisualElement { pickingMode = PickingMode.Ignore };
            _check.style.width = 7;
            _check.style.height = 12;
            _check.style.borderRightWidth = _check.style.borderBottomWidth = 3;
            _check.style.borderRightColor = _check.style.borderBottomColor = new Color(.09f, .18f, .13f);
            _check.style.rotate = new Rotate(Angle.Degrees(40));
            _check.style.marginTop = -4;
            _box.Add(_check);
            _toggle.Add(_box);
            var title = new Label(_loc.T(LocKeys.PinTitle)) { pickingMode = PickingMode.Ignore };
            title.style.flexGrow = 1;
            title.style.flexShrink = 1;
            title.style.minWidth = 0;
            title.style.whiteSpace = WhiteSpace.Normal;
            title.style.unityTextAlign = TextAnchor.MiddleLeft;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14;
            title.style.color = Cream;
            _toggle.Add(title);
            _state = new Label { pickingMode = PickingMode.Ignore };
            _state.style.flexShrink = 0;
            _state.style.marginLeft = 8;
            _state.style.minWidth = 32;
            _state.style.paddingLeft = _state.style.paddingRight = 5;
            _state.style.paddingTop = _state.style.paddingBottom = 3;
            _state.style.fontSize = 11;
            _state.style.unityFontStyleAndWeight = FontStyle.Bold;
            _state.style.unityTextAlign = TextAnchor.MiddleCenter;
            Round(_state, 3);
            _toggle.Add(_state);
            _toggle.RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; Paint(); });
            _toggle.RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; Paint(); });
            _toggle.RegisterCallback<FocusInEvent>(_ => { _focused = true; Paint(); });
            _toggle.RegisterCallback<FocusOutEvent>(_ => { _focused = false; Paint(); });
            _root.Add(_toggle);
            _hint = new Label();
            _hint.style.fontSize = 12;
            _hint.style.color = new Color(.77f, .84f, .77f);
            _hint.style.whiteSpace = WhiteSpace.Normal;
            _hint.style.marginTop = 7;
            _root.Add(_hint);
            _service.Changed += Refresh;
            Paint();
            ClearFragment();
            return _root;
        }

        public void ShowFragment(BaseComponent entity) { _entity = entity; Refresh(); }
        public void ClearFragment()
        {
            _entity = null; _hovered = false; _focused = false;
            if (_root != null) _root.style.display = DisplayStyle.None;
        }
        public void UpdateFragment() { }
        private void Refresh()
        {
            if (_root == null) return;
            bool supports = WorkAreaService.Supports(_entity);
            _root.style.display = supports ? DisplayStyle.Flex : DisplayStyle.None;
            _pinned = supports && _service.IsPinned(_entity);
            _toggle.SetEnabled(_service.Available);
            _hint.text = _loc.T(!_service.Available ? LocKeys.HintUnavailable
                : _pinned ? LocKeys.HintPinned : LocKeys.HintUnpinned);
            Paint();
        }
        private void Paint()
        {
            bool emphasis = (_hovered || _focused) && _service.Available;
            Color accent = _pinned ? new Color(.66f, .88f, .46f) : Gold;
            _root.style.borderLeftColor = accent;
            _toggle.style.backgroundColor = emphasis ? new Color(.25f, .36f, .26f)
                : _pinned ? new Color(.19f, .30f, .22f) : new Color(.15f, .23f, .21f);
            Border(_toggle, emphasis ? Cream : accent, 2);
            Border(_box, accent, 2);
            _box.style.backgroundColor = _pinned ? accent : new Color(.07f, .13f, .12f);
            _check.style.display = _pinned ? DisplayStyle.Flex : DisplayStyle.None;
            _state.text = _loc.T(_pinned ? LocKeys.StateOn : LocKeys.StateOff);
            _state.style.backgroundColor = _pinned ? accent : new Color(.29f, .29f, .21f);
            _state.style.color = _pinned ? new Color(.09f, .18f, .13f) : Cream;
        }
        private static void Border(VisualElement element, Color color, float width)
        {
            element.style.borderTopWidth = element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = element.style.borderRightWidth = width;
            element.style.borderTopColor = element.style.borderBottomColor = color;
            element.style.borderLeftColor = element.style.borderRightColor = color;
        }
        private static void Round(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = radius;
        }
        internal static Button MakeButton(string text, Action clicked)
        {
            var button = new Button(clicked) { text = text };
            button.style.backgroundColor = new Color(.16f, .31f, .27f, .98f);
            button.style.color = Cream;
            button.style.fontSize = 14;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.paddingLeft = button.style.paddingRight = 12;
            button.style.paddingTop = button.style.paddingBottom = 7;
            Border(button, new Color(.52f, .67f, .47f), 1);
            return button;
        }
    }
}
