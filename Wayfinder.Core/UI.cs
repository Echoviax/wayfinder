using Bang.Entities;
using Bang.StateMachines;
using FMOD;
using HarmonyLib;
using Murder;
using Murder.Core.Geometry;
using Murder.Core.Graphics;
using Murder.Core.Input;
using Murder.Services;
using Murder.Utilities;
using Road.Assets;
using Road.Core;
using Road.Services;
using Road.StateMachines;
using System.Numerics;

// still don't know why i agreed to do this
namespace Wayfinder.UI
{
    public static class WayfinderMenuManager
    {
        public static bool IsWayfinderMenuOpen = false;
        public static Bang.World ActiveWorld = null;
        public static MainMenu ActiveMainMenu = null;
    }

    public class WayfinderModMenuStateMachine : StateMachine
    {
        private GenericMenuInfo<OptionsStateMachine.OptionsData> _optionsMenu;
        private MenuInfo _tabsMenu;

        private float _menuOpenTime;
        private float _lastSettingChangeTime;
        private float _lastTabChangeTime;
        private float _quitTime;

        private float _tabExtraWidth;
        private float _totalTabsWidth;
        private int _tabSpacing;

        public WayfinderModMenuStateMachine()
        {
            State(MainState);
        }

        private IEnumerator<Wait> MainState()
        {
            WayfinderMenuManager.IsWayfinderMenuOpen = true;
            _menuOpenTime = Game.NowUnscaled;
            _quitTime = float.MaxValue;

            if (WayfinderMenuManager.ActiveMainMenu != null)
            {
                var traverse = Traverse.Create(WayfinderMenuManager.ActiveMainMenu);
                traverse.Field("_currentMode").SetValue(5);
                traverse.Field("_cameraTarget").SetValue(new Vector2(0f, 200f));
            }

            var sounds = LibraryServices.GetUiSoundDatabase();
            SoundServices.Play(sounds.MainMenu.Menu.SelectionChange);

            _tabsMenu = new MenuInfo(new MenuOption("Wayfinder Mods"));
            UpdateMenuOptions();
            _optionsMenu.Select(1, Game.NowUnscaled);

            Entity.SetCustomDraw(Draw);

            bool quit = false;
            while (true)
            {
                yield return Wait.NextFrame;

                Point pressedValue = Game.Input.GetAxis(101).PressedValue;
                bool submitted = Game.Input.VerticalMenu(ref _optionsMenu);

                if (submitted)
                {
                    if (_optionsMenu.Selection == 0)
                        _optionsMenu.Select(1, Game.NowUnscaled);
                    else if (_optionsMenu.Selection == _optionsMenu.Length - 1)
                        quit = true;
                    else
                        pressedValue.X = 1;
                }

                if (pressedValue.X != 0)
                {
                    var loadedMods = Core.LoaderCore.LoadedMods;

                    if (_optionsMenu.Selection >= 1 && _optionsMenu.Selection <= loadedMods.Count)
                    {
                        _lastSettingChangeTime = Game.NowUnscaled;
                        SoundServices.Play(sounds.BaseMenu.SelectionChange);
                        var targetMod = loadedMods[_optionsMenu.Selection - 1];
                        Core.LoaderCore.ToggleMod(targetMod);

                        _optionsMenu.Options[_optionsMenu.Selection].Check = targetMod.IsEnabled;
                    }
                }

                // 106 is back command. esc for example
                if (quit || Game.Input.PressedAndConsume(106))
                {
                    SoundServices.Play(sounds.MainMenu.Back);
                    _quitTime = Game.NowUnscaled;

                    float waitStartTime = Game.NowUnscaled;
                    while (Game.NowUnscaled - waitStartTime < 0.15f)
                        yield return Wait.NextFrame;

                    break;
                }
            }

            // go back!!
            Entity.RemoveCustomDraw();
            WayfinderMenuManager.IsWayfinderMenuOpen = false;

            if (WayfinderMenuManager.ActiveMainMenu != null)
            {
                var traverse = Traverse.Create(WayfinderMenuManager.ActiveMainMenu);
                traverse.Field("_currentMode").SetValue(0);
                traverse.Field("_cameraTarget").SetValue(new Vector2(0f, 100f));
            }

            Entity.Destroy();
        }

        private void UpdateMenuOptions()
        {
            int currentSelection = _optionsMenu.Options != null ? _optionsMenu.Selection : 1;
            var loadedMods = Core.LoaderCore.LoadedMods;
            var optionsArray = new OptionsStateMachine.OptionsData[loadedMods.Count == 0 ? 3 : loadedMods.Count + 2];

            // empty for tab list
            optionsArray[0] = default;

            if (loadedMods.Count == 0)
            {
                optionsArray[1] = new OptionsStateMachine.OptionsData("No mods loaded")
                {
                    Tooltip = "Check your 'Mods' folder."
                };
            }
            else
            {
                for (int i = 0; i < loadedMods.Count; i++)
                {
                    var mod = loadedMods[i];
                    optionsArray[i + 1] = new OptionsStateMachine.OptionsData(loadedMods[i].Mod.Name)
                    {
                        Check = mod.IsEnabled,
                        Tooltip = mod.Mod.Description ?? "No mod description provided"
                    };
                }
            }

            // empty for back arrow
            optionsArray[^1] = default;

            _optionsMenu = new GenericMenuInfo<OptionsStateMachine.OptionsData>(optionsArray)
            {
                Sounds = LibraryServices.GetUiSoundDatabase().BaseMenu
            };

            // in case menu reloads while open for whatever reason
            if (currentSelection >= _optionsMenu.Length - 1)
                currentSelection = 1;

            _optionsMenu.Select(currentSelection, Game.NowUnscaled);
        }

        private void Draw(RenderContext render)
        {
            int fontHeight = RoadFonts.PixelFont.GetFontHeight();
            Vector2 screenCenter = render.Camera.Size / 2f;
            UiSkinAsset uiSkin = LibraryServices.GetUiSkin(); // theme

            // options container size
            Vector2 menuContainerSize = new Vector2(180f + _tabExtraWidth, 140f);

            int currentTabXOffset = 0;
            if (_totalTabsWidth == 0f && _tabsMenu.Length > 0)
            {
                for (int i = 0; i < _tabsMenu.Length; i++)
                    _totalTabsWidth += RoadFonts.LargeFont.GetLineWidth(_tabsMenu.GetOptionText(i));

                if (_totalTabsWidth >= menuContainerSize.X - 20f)
                {
                    _tabExtraWidth = 24f;
                    menuContainerSize.X += _tabExtraWidth;
                }
                _tabSpacing = Calculator.RoundToInt((menuContainerSize.X - _totalTabsWidth) / Math.Max(1, (float)_tabsMenu.Length - 1f));
            }

            float quitTransitionProgress = Calculator.ClampTime(_quitTime, Game.NowUnscaled, 0.1f);
            float openTransitionEased = Ease.CubeIn(Calculator.ClampTime(_menuOpenTime, Game.NowUnscaled, 0.25f));
            float yOffsetAnimation = 20f * Ease.CubeIn(1f - openTransitionEased);

            // If quitting, add to the Y offset so it drops down
            if (Game.NowUnscaled - _quitTime < 1f)
                yOffsetAnimation += quitTransitionProgress * 20f;

            Color shadowColor = uiSkin.MainMenuStyle.Shadow;
            Color accentColor = Palette.Colors[5];
            float tabSwitchProgress = Calculator.ClampTime(Game.NowUnscaled - _lastTabChangeTime, 0.15f);

            Vector2 tabsBasePosition = screenCenter + new Vector2((0f - menuContainerSize.X) / 2f, yOffsetAnimation + 5f);

            // Draw tabs... which is pointless... for now?
            for (int j = 0; j < _tabsMenu.Length; j++)
            {
                bool isSelectedTab = _tabsMenu.Selection == j;
                string optionText = _tabsMenu.GetOptionText(j);
                float tabYOffset = isSelectedTab ? (1f - Ease.CubeIn(tabSwitchProgress)) : 0f;

                currentTabXOffset += render.UiBatch.DrawText(12, optionText, tabsBasePosition + new Vector2(currentTabXOffset, tabYOffset), new DrawInfo(0.15f)
                {
                    Color = (isSelectedTab ? Palette.Colors[6] : uiSkin.MainMenuStyle.Color) * openTransitionEased,
                    Shadow = shadowColor * openTransitionEased
                }).X + _tabSpacing;
            }

            float cursorMoveProgress = Calculator.ClampTime(Game.NowUnscaled - _optionsMenu.LastMoved, 0.15f);
            float settingChangeProgress = Calculator.ClampTime(_lastSettingChangeTime, Game.NowUnscaled, 0.15f);

            // horizontal line separator
            float separatorLineProgress = Calculator.ClampTime(Game.NowUnscaled - _menuOpenTime, 1f);
            render.UiBatch.DrawHorizontalLine(Calculator.RoundToInt(tabsBasePosition.X), Calculator.RoundToInt(tabsBasePosition.Y) + 15, (int)(separatorLineProgress * menuContainerSize.X), Color.Lerp(Palette.Colors[6], Palette.Colors[2], separatorLineProgress));

            int currentOptionYOffset = 0;
            int optionLineHeight = fontHeight > 8 ? 12 : 11;
            Vector2 optionsBasePosition = screenCenter + new Point((0f - menuContainerSize.X) / 2f, yOffsetAnimation + 14f + (fontHeight > 8 ? -2f : 0f));
            Vector2 selectedOptionPosition = Vector2.Zero;

            // mod options
            for (int k = 0; k < _optionsMenu.Length; k++)
            {
                OptionsStateMachine.OptionsData optionsData = _optionsMenu.Options[k];
                bool isSelectedOption = _optionsMenu.Selection == k;

                if (isSelectedOption)
                    selectedOptionPosition = optionsBasePosition + new Vector2(0f, currentOptionYOffset + (fontHeight > 8 ? 4 : 0));

                if (string.IsNullOrEmpty(optionsData.Label))
                {
                    currentOptionYOffset += optionLineHeight;
                    continue;
                }

                Point textBounds = render.UiBatch.DrawText(11, optionsData.Label, optionsBasePosition + new Vector2(0f, currentOptionYOffset), new DrawInfo(0.8f)
                {
                    Color = (isSelectedOption ? accentColor : uiSkin.MainMenuStyle.Color) * openTransitionEased,
                    Shadow = uiSkin.MainMenuStyle.Shadow * openTransitionEased
                });

                float checkboxBounceOffset = isSelectedOption ? (2f - 2f * Ease.CubeOut(settingChangeProgress)) : 0f;
                float checkboxYAdjust = fontHeight > 8 ? 7 : 4;

                // checkboxes
                if (optionsData.Check.HasValue)
                {
                    bool isChecked = optionsData.Check.Value;
                    render.UiBatch.DrawPortrait(isChecked ? uiSkin.Icons.Checked : uiSkin.Icons.Objective, optionsBasePosition + new Vector2(menuContainerSize.X, (float)currentOptionYOffset + checkboxBounceOffset + checkboxYAdjust), new DrawInfo(0.5f)
                    {
                        Origin = new Vector2(0.5f, 0f)
                    });
                }

                // little info boxes!
                if (isSelectedOption && !string.IsNullOrEmpty(optionsData.Tooltip))
                {
                    Vector2 tooltipBasePosition = optionsBasePosition + new Vector2(menuContainerSize.X + 6f, currentOptionYOffset);
                    Point tooltipTextSize = render.UiBatch.DrawText(11, optionsData.Tooltip, tooltipBasePosition, 100, new DrawInfo(0.5f)
                    {
                        Color = Palette.Colors[3] * cursorMoveProgress,
                        Origin = new Vector2(0f, 0f)
                    });

                    render.UiBatch.DrawRectangleOutline(new Rectangle(tooltipBasePosition - new Vector2(2f, 2f), new Vector2(104f, tooltipTextSize.Y + 4)), Palette.Colors[1]);
                }

                currentOptionYOffset += optionLineHeight;
            }

            // cursor
            if (_optionsMenu.Selection != 0 && _optionsMenu.Selection != _optionsMenu.Length - 1)
            {
                Vector2 previousCursorPos = optionsBasePosition + new Vector2(-6f, optionLineHeight * _optionsMenu.PreviousSelection);
                Vector2 targetCursorPos = selectedOptionPosition + new Vector2(-6f, 2f);
                Vector2 currentCursorPos = Vector2.Lerp(previousCursorPos, targetCursorPos, Ease.Evaluate(cursorMoveProgress, EaseKind.BackOut));

                // This is the line creating the bounce when toggling settings
                currentCursorPos.X += 2f - 2f * Ease.BounceInOut(settingChangeProgress);

                render.UiBatch.DrawPortrait(uiSkin.MainMenu.HandCursor, currentCursorPos, new DrawInfo(0.25f)
                {
                    Color = Color.White.FadeAlpha(openTransitionEased),
                    Origin = new Vector2(0f, 0f)
                });
            }

            // back button
            bool isBackButtonSelected = _optionsMenu.Selection == _optionsMenu.Length - 1;
            float backButtonBounceOffset = isBackButtonSelected ? (2f - 2f * Ease.CubeOut(cursorMoveProgress)) : 0f;

            render.UiBatch.DrawSprite(uiSkin.MainMenu.BackOptions.Sprite, new Vector2(screenCenter.X, screenCenter.Y + 87f + backButtonBounceOffset), new DrawInfo(0.7f)
            {
                Color = Color.White.FadeAlpha(openTransitionEased),
                Origin = new Vector2(0f, 0f),
                Outline = isBackButtonSelected ? new Color?(Palette.Colors[4]) : null
            }, new AnimationInfo(isBackButtonSelected ? "selected" : "off"));
        }
    }
}