#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Game.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXUtility.Classes;
using SFXUtility.Library;
using SFXUtility.Library.Logger;

#endregion

namespace SFXUtility.Features.Events
{
    internal class Game : Child<Events>
    {
        private bool _onEndTriggerd;
        private bool _onStartTriggerd;

        public Game(Events parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Game"; }
        }

        protected override void OnEnable()
        {
            LeagueSharp.Game.OnNotify += OnGameNotify;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            LeagueSharp.Game.OnNotify -= OnGameNotify;
            base.OnDisable();
        }

        protected override sealed void OnLoad()
        {
            try
            {
                LeagueSharp.Game.OnStart += delegate { _onStartTriggerd = true; };

                Menu = new Menu(Name, Name);
                var startMenu = new Menu("OnStart", Name + "OnStart");
                startMenu.AddItem(new MenuItem(startMenu.Name + "Delay", "Delay").SetValue(new Slider(20, 0, 75)));
                startMenu.AddItem(
                    new MenuItem(startMenu.Name + "Greeting", "Greeting").SetValue(
                        new StringList(new[] { "gl & hf", "good luck & have fun" })));
                startMenu.AddItem(new MenuItem(startMenu.Name + "SayGreeting", "Say Greeting").SetValue(false));

                var endMenu = new Menu("OnEnd", Name + "OnEnd");
                endMenu.AddItem(
                    new MenuItem(endMenu.Name + "Ending", "Goodbye").SetValue(
                        new StringList(new[] { "gg", "gg wp", "good game", "well played" })));
                endMenu.AddItem(new MenuItem(endMenu.Name + "SayEnding", "Say Goodbye").SetValue(false));

                Menu.AddSubMenu(startMenu);
                Menu.AddSubMenu(endMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            try
            {
                if (_onStartTriggerd)
                {
                    var map = Utility.Map.GetMap().Type;
                    if (Menu.Item(Name + "OnStartSayGreeting").GetValue<bool>() &&
                        !GameObjects.Heroes.Any(
                            h =>
                                h.Level >=
                                (map == Utility.Map.MapType.CrystalScar || map == Utility.Map.MapType.HowlingAbyss
                                    ? 3
                                    : 1)))
                    {
                        Utility.DelayAction.Add(
                            Menu.Item(Name + "OnStartDelay").GetValue<Slider>().Value * 1000,
                            delegate
                            {
                                LeagueSharp.Game.Say(
                                    "/all " + Menu.Item(Name + "OnStartGreeting").GetValue<StringList>().SelectedValue);
                            });
                    }
                }
                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameNotify(GameNotifyEventArgs args)
        {
            if (_onEndTriggerd ||
                (args.EventId != GameEventId.OnEndGame && args.EventId != GameEventId.OnHQDie &&
                 args.EventId != GameEventId.OnHQKill))
            {
                return;
            }

            _onEndTriggerd = true;
            try
            {
                if (Menu.Item(Name + "OnEndSayEnding").GetValue<bool>())
                {
                    LeagueSharp.Game.Say("/all " + Menu.Item(Name + "OnEndEnding").GetValue<StringList>().SelectedValue);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}