using Bang.Entities;
using Bang.StateMachines;
using Murder;
using Murder.Core.Geometry;
using Murder.Core.Graphics;
using Murder.Core.Input;
using Murder.Services;
using Road.Core;
using Road.Services;

namespace Wayfinder.UI
{
    public static class WayfinderMenuManager
    {
        public static bool IsWayfinderMenuOpen = false;
        public static Bang.World ActiveWorld = null;
    }

    public class WayfinderModMenuStateMachine : StateMachine
    {
        private MenuInfo _menuInfo;

        public WayfinderModMenuStateMachine()
        {
            State(MainState);
        }

        private IEnumerator<Wait> MainState()
        {
            WayfinderMenuManager.IsWayfinderMenuOpen = true;

            var sounds = LibraryServices.GetUiSoundDatabase();
            SoundServices.Play(sounds.MainMenu.Menu.SelectionChange);

            _menuInfo = new MenuInfo(
                new MenuOption("Wayfinder Mod Settings") { SoundOnClick = false },
                new MenuOption("--- Coming Soon ---") { SoundOnClick = false },
                new MenuOption(Road.RoadGame.Resources.Menu.GoBack)
            )
            {
                Sounds = sounds.MainMenu.Menu
            };

            Entity.SetCustomDraw(Draw);

            while (true)
            {
                if (Game.Input.VerticalMenu(ref _menuInfo))
                {
                    if (_menuInfo.Selection == 2)
                    {
                        SoundServices.Play(sounds.MainMenu.Back);
                        break;
                    }
                    else
                    {
                        // other options will go here...
                        SoundServices.Play(sounds.OnError);
                    }
                }

                if (Game.Input.PressedAndConsume(106))
                {
                    SoundServices.Play(sounds.MainMenu.Back);
                    break;
                }

                yield return Wait.NextFrame;
            }

            WayfinderMenuManager.IsWayfinderMenuOpen = false;
            Entity.Destroy();
        }

        private void Draw(RenderContext render)
        {
            render.UiBatch.DrawRectangle(new Rectangle(0, 0, render.Camera.Width, render.Camera.Height), Palette.Colors[1] * 0.85f, 0.05f);

            var center = render.Camera.Size / 2f;
            var uiSkin = LibraryServices.GetUiSkin();
            var position = center + new Point(0, -20);

            RenderServices.DrawVerticalMenu(render.UiBatch, in position, uiSkin.MainMenuStyle, _menuInfo);
        }
    }
}