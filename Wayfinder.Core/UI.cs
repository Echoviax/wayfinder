using Bang.Entities;
using Bang.StateMachines;
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
        private float _quitTime;
        private float _totalTabsWidth;

        private Core.ModInstance _viewingConfigFor = null;
        private List<string> _currentConfigKeys = new List<string>();
        private List<string> _currentConfigTypes = new List<string>();

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
                    else if (_viewingConfigFor == null)
                    {
                        var loadedMods = Core.LoaderCore.LoadedMods;
                        if (_optionsMenu.Selection >= 1 && _optionsMenu.Selection <= loadedMods.Count)
                        {
                            SoundServices.Play(sounds.MainMenu.Menu.MenuSubmit);
                            _viewingConfigFor = loadedMods[_optionsMenu.Selection - 1];
                            UpdateMenuOptions();
                            _optionsMenu.Select(1, Game.NowUnscaled);
                        }
                    }
                    else
                        pressedValue.X = 1; // simulate right click on enter in a submenu. Might remove, depends on testing
                }

                if (pressedValue.X != 0)
                {
                    _lastSettingChangeTime = Game.NowUnscaled;
                    SoundServices.Play(sounds.BaseMenu.SelectionChange);

                    if (_viewingConfigFor == null)
                    {
                        var loadedMods = Core.LoaderCore.LoadedMods;
                        if (_optionsMenu.Selection >= 1 && _optionsMenu.Selection <= loadedMods.Count)
                        {
                            var targetMod = loadedMods[_optionsMenu.Selection - 1];
                            Core.LoaderCore.ToggleMod(targetMod);

                            _optionsMenu.Options[_optionsMenu.Selection].Check = targetMod.IsEnabled;
                        }

                    }
                    else
                    {
                        int dataIndex = _optionsMenu.Selection - 1;
                        if (dataIndex >= 0 && dataIndex < _currentConfigKeys.Count)
                        {
                            string key = _currentConfigKeys[dataIndex];
                            string type = _currentConfigTypes[dataIndex];
                            var config = _viewingConfigFor.Config;
                            var oldData = _optionsMenu.Options[_optionsMenu.Selection];

                            if (type == "bool")
                            {
                                bool val = config.BoolSettings[key];
                                config.SetBool(key, !val);
                                _optionsMenu.Options[_optionsMenu.Selection].Check = !val;
                            }
                            else if (type == "int")
                            {
                                int val = config.IntSettings[key];
                                val += pressedValue.X;
                                config.SetInt(key, val);

                                _optionsMenu.Options[_optionsMenu.Selection].Setting = val.ToString();
                            }
                            else if (type == "float")
                            {
                                float val = config.FloatSettings[key];
                                val += pressedValue.X * 0.1f;
                                val = (float)Math.Round(val, 2);
                                config.SetFloat(key, val);

                                _optionsMenu.Options[_optionsMenu.Selection].Setting = val.ToString("0.0");
                            }

                            // feed update to the mod
                            if (_viewingConfigFor.Mod is API.IConfigurableMod liveUpdateMod)
                                liveUpdateMod.InitializeConfig(config);
                        }
                    }
                }

                // 106 is back command. esc for example
                if (quit || Game.Input.PressedAndConsume(106))
                {
                    SoundServices.Play(sounds.MainMenu.Back);

                    if (_viewingConfigFor != null)
                    {
                        // back to mod list
                        quit = false;
                        _viewingConfigFor = null;
                        UpdateMenuOptions();
                        _optionsMenu.Select(1, Game.NowUnscaled);
                    }
                    else
                    {
                        // quit the mod menu
                        _quitTime = Game.NowUnscaled;
                        float waitStartTime = Game.NowUnscaled;
                        while (Game.NowUnscaled - waitStartTime < 0.15f)
                            yield return Wait.NextFrame;
                        break;
                    }
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

            if (_viewingConfigFor == null)
            {
                _tabsMenu = new MenuInfo(new MenuOption("Wayfinder Mods"));
                var loadedMods = Core.LoaderCore.LoadedMods;
                var optionsArray = new OptionsStateMachine.OptionsData[loadedMods.Count == 0 ? 3 : loadedMods.Count + 2];
                optionsArray[0] = default;

                if (loadedMods.Count == 0)
                    optionsArray[1] = new OptionsStateMachine.OptionsData("No mods loaded") { Tooltip = "Check your 'Mods' folder." };
                else
                {
                    for (int i = 0; i < loadedMods.Count; i++)
                    {
                        var mod = loadedMods[i];
                        optionsArray[i + 1] = new OptionsStateMachine.OptionsData(mod.Mod.Name)
                        {
                            Check = mod.IsEnabled,
                            Tooltip = $"{(string.IsNullOrEmpty(mod.Mod.Description) ? "No description." : mod.Mod.Description)}{(mod.Mod is API.IConfigurableMod ? "\n\nPress Enter to configure." : "")}"
                        };
                    }
                }
                optionsArray[^1] = default;
                _optionsMenu = new GenericMenuInfo<OptionsStateMachine.OptionsData>(optionsArray) { Sounds = LibraryServices.GetUiSoundDatabase().BaseMenu };
            }
            else
            {
                _tabsMenu = new MenuInfo(new MenuOption($"{_viewingConfigFor.Mod.Name} Config"));
                _currentConfigKeys.Clear();
                _currentConfigTypes.Clear();

                var config = _viewingConfigFor.Config;
                int settingCount = config.BoolSettings.Count + config.IntSettings.Count;

                var optionsArray = new OptionsStateMachine.OptionsData[settingCount == 0 ? 3 : settingCount + 2];
                optionsArray[0] = default;

                if (settingCount == 0)
                {
                    optionsArray[1] = new OptionsStateMachine.OptionsData("No settings available.") { Tooltip = "This mod does not have a config file." };
                }
                else
                {
                    int index = 1;

                    foreach (var kvp in config.BoolSettings)
                    {
                        optionsArray[index] = new OptionsStateMachine.OptionsData(kvp.Key) { Check = kvp.Value };
                        _currentConfigKeys.Add(kvp.Key);
                        _currentConfigTypes.Add("bool");
                        index++;
                    }

                    foreach (var kvp in config.IntSettings)
                    {
                        optionsArray[index] = new OptionsStateMachine.OptionsData(kvp.Key)
                        {
                            Setting = kvp.Value.ToString()
                        };
                        _currentConfigKeys.Add(kvp.Key);
                        _currentConfigTypes.Add("int");
                        index++;
                    }

                    foreach (var kvp in config.FloatSettings)
                    {
                        optionsArray[index] = new OptionsStateMachine.OptionsData(kvp.Key)
                        {
                            Setting = kvp.Value.ToString("0.0")
                        };
                        _currentConfigKeys.Add(kvp.Key);
                        _currentConfigTypes.Add("float");
                        index++;
                    }
                }

                optionsArray[^1] = default;
                _optionsMenu = new GenericMenuInfo<OptionsStateMachine.OptionsData>(optionsArray) { Sounds = LibraryServices.GetUiSoundDatabase().BaseMenu };
            }

            if (currentSelection >= _optionsMenu.Length - 1)
                currentSelection = 1;

            _optionsMenu.Select(currentSelection, Game.NowUnscaled);
        }

        private void Draw(RenderContext render)
        {
            int fontHeight = RoadFonts.PixelFont.GetFontHeight();
            Vector2 screenCenter = render.Camera.Size / 2f;
            UiSkinAsset uiSkin = LibraryServices.GetUiSkin();

            _totalTabsWidth = 0f;
            if (_tabsMenu.Length > 0)
            {
                for (int i = 0; i < _tabsMenu.Length; i++)
                    _totalTabsWidth += RoadFonts.LargeFont.GetLineWidth(_tabsMenu.GetOptionText(i));
            }

            // expand the menu container if the config title is long
            Vector2 menuContainerSize = new Vector2(Math.Max(180f, _totalTabsWidth + 40f), 140f);

            float quitTransitionProgress = Calculator.ClampTime(_quitTime, Game.NowUnscaled, 0.1f);
            float openTransitionEased = Ease.CubeIn(Calculator.ClampTime(_menuOpenTime, Game.NowUnscaled, 0.25f));
            float yOffsetAnimation = 20f * Ease.CubeIn(1f - openTransitionEased);

            if (Game.NowUnscaled - _quitTime < 1f)
                yOffsetAnimation += quitTransitionProgress * 20f;

            Color shadowColor = uiSkin.MainMenuStyle.Shadow;
            Color accentColor = Palette.Colors[5];

            Vector2 tabsBasePosition = screenCenter + new Vector2((0f - menuContainerSize.X) / 2f, yOffsetAnimation + 5f);

            // draw tab title
            render.UiBatch.DrawText(12, _tabsMenu.GetOptionText(0), tabsBasePosition, new DrawInfo(0.15f)
            {
                Color = Palette.Colors[6] * openTransitionEased,
                Shadow = shadowColor * openTransitionEased
            });

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

                // numerical things
                else if (optionsData.Setting != null)
                {
                    string settingText = optionsData.Setting;

                    // shifts the text left by 8px to make room for the right arrow
                    Vector2 textPosition = optionsBasePosition + new Vector2(menuContainerSize.X - 8f, (float)currentOptionYOffset + checkboxBounceOffset);

                    Point textSize = render.UiBatch.DrawText(11, settingText, textPosition, new DrawInfo(0.8f)
                    {
                        Color = (isSelectedOption ? accentColor : uiSkin.MainMenuStyle.Color) * openTransitionEased,
                        Shadow = uiSkin.MainMenuStyle.Shadow * openTransitionEased,
                        Origin = new Vector2(1f, 0f)
                    });

                    // draw arrows on hover
                    if (isSelectedOption)
                    {
                        float arrowSlide = 1f - Ease.CubeOut(cursorMoveProgress);

                        render.UiBatch.DrawSprite(uiSkin.ScrollArrow, optionsBasePosition + new Vector2(menuContainerSize.X - 16f - (float)textSize.X - arrowSlide + settingChangeProgress, currentOptionYOffset + checkboxYAdjust), new DrawInfo(0.8f)
                        {
                            ImageFlip = ImageFlip.Horizontal
                        }, new AnimationInfo("right"));

                        render.UiBatch.DrawSprite(uiSkin.ScrollArrow, optionsBasePosition + new Vector2(menuContainerSize.X + arrowSlide + settingChangeProgress, currentOptionYOffset + checkboxYAdjust), new DrawInfo(0.8f)
                        {
                            ImageFlip = ImageFlip.None
                        }, new AnimationInfo("right"));
                    }
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