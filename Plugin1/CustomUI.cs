using System;
using System.Collections.Generic;

using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins;

using UnityEngine;

using AP;
using AP.Keywords;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Oxide.Plugins
{
    [Info("TestPlugin","WP","0.0.7")]
    internal class CustomUI : RustPlugin
    {
        private const string plVersion = "0.0.7";
        private readonly Dictionary<ulong, GuiInfo> GUIInfo;
        private List<APCharacter> APMembers; //тут должны храниться данные игроков

        public CustomUI()
        {
            GUIInfo = new Dictionary<ulong, GuiInfo>();
        }
        
        #region Hooks
        void Unload()
        {
           foreach(var player in BasePlayer.activePlayerList)
            {
                GuiInfo info;
                if (!GUIInfo.TryGetValue(player.userID, out info)) continue;
                DestroyUI(player, info.UIMain);
                DestroyUI(player, info.UIHud);
            }
            SaveAPData();
        }

        void Loaded() //происходит до serverinitialize
        {
            try
            {
                APMembers = Interface.Oxide.DataFileSystem.ReadObject<List<APCharacter>>(APKeys.DataFileName);
            }
            catch (Exception ex)
            {
                Puts($"Невозможно наполнить контейнер, {ex}");
            }
            finally
            {
                if (APMembers == null)
                    APMembers = new List<APCharacter>();
            }
        }

        void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player == null) return;
            APCharacter pers = FindCharacter(player);
            if(pers.IsChatSubscriber)
                pers.RegStateHandler(new APCharacter.CharacterStateHandler(ChatMessage));
            pers.RegStateHandler(new APCharacter.CharacterLvlUpHandler(OnCharacterLvlUp));
            pers.RegStateHandler(new APCharacter.CharacterExpHandler(OnExpValueChanged));
            InitializeGui(player);
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            APCharacter pers = FindCharacter(player);
            if (pers.IsChatSubscriber)
                pers.UnregStateHandler(new APCharacter.CharacterStateHandler(ChatMessage));
            pers.UnregStateHandler(new APCharacter.CharacterLvlUpHandler(OnCharacterLvlUp));
            pers.UnregStateHandler(new APCharacter.CharacterExpHandler(OnExpValueChanged));
            GUIInfo.Remove(player.userID);
        }
        #endregion

        #region Config

        private void SaveAPData(BasePlayer player) //пытаемся сохранить список персонажей в файл
        {
            Interface.Oxide.DataFileSystem.WriteObject(APKeys.DataFileName, APMembers);
            ChatMessage(player, APKeys.DataSaved);
        }
        private void SaveAPData() //пытаемся сохранить список персонажей в файл
        {
            Interface.Oxide.DataFileSystem.WriteObject(APKeys.DataFileName, APMembers);
        }
        #endregion

        #region Utility
        private void DestroyUI(BasePlayer player, string name)
        {
            if (!string.IsNullOrEmpty(name)) CuiHelper.DestroyUi(player, name);
        }

        /// <summary>
        /// Выполняет поиск по списку персонажей, и возвращает первого с таким ID, если вхождений нет, возвращает нового.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private APCharacter FindCharacter(BasePlayer player)
        {
            var userID = player.userID;
            APCharacter Character;
            if (player.displayName == null) return null;
            if (player.userID < 70000000000) return null;
            /*if (!APMembers.TryGetValue(userID, out Character))
                APMembers[userID] = Character = new APCharacter(player);*/
            Character = APMembers.Find(x => x.OwnerId == player.userID);
            if(Character == null)
            {
                Puts("Персонаж не найден, создаем нового.");
                Character = new APCharacter(player);
		APMembers.Add(Character);
            }
            return Character;
        }

        private string GetAnchorMin(string xMin, string yMin)
        {
            return string.Concat(string.Concat(xMin, " "), yMin);
        }
        private string GetAnchorMax(string xMax, string yMax)
        {
            return string.Concat(string.Concat(xMax, " "), yMax);
        }
        
        private void ChatMessage(BasePlayer player, string mes) // метод для делегатов APCharacter
        {
            if (player?.net == null) return;
            player.ChatMessage(mes);
        }
        private void OnCharacterLvlUp(APCharacter pers) //lvlup handler
        {
            ProfileUI(BasePlayer.FindByID(pers.OwnerId));
        }
        private void OnExpValueChanged(APCharacter pers)
        {
            ProgressUI(BasePlayer.FindByID(pers.OwnerId));
        }
        #endregion

        #region Commands
        [ConsoleCommand("statup.cmd")]
        void StatUp(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            Characteristics type;
            APCharacter pers = FindCharacter(player);
            int value = 0;
            string[] pair = args.Args[0].Split('-');
            try
            {
                type = (Characteristics)Enum.Parse(typeof(Characteristics), pair[0], true);
                value = int.Parse(pair[1]);
                pers.IncreaseCharacteristic(type, value);
                ProfileUI(player);
            }
            catch (Exception ex)
            {
                Puts("Unable to convert args[]!, {ex}");
            }    
        } //работает

        [ChatCommand("announce")]
        private void ConfigureChat(BasePlayer player, string command, string[] args)//включение и отключение сообщений в чате
        {
            if (player == null) return;
            var pers = FindCharacter(player);
            if (args[0] == "on")
            {
                if (!pers.IsChatSubscriber)
                {
                    pers.IsChatSubscriber = true;
                    pers.RegStateHandler(ChatMessage);
                    return;
                }
            }
            if (args[0] == "off")
            {
                if (pers.IsChatSubscriber)
                {
                    pers.IsChatSubscriber = false;
                    pers.UnregStateHandler(ChatMessage);
                    return;
                }
            }
            ChatMessage(player, "Unknown parameter");
        }//работает

        [ChatCommand("guihide")]
        private void CmdCuiHide(BasePlayer player)
        {
            GuiInfo info;
            if (!GUIInfo.TryGetValue(player.userID, out info)) return;
            DestroyUI(player, info.UIMain);
            DestroyUI(player, info.UIHud);
        }//работает
        #endregion

        #region AdminCommands
        [ChatCommand("addexp")]
        private void AdminExpGive(BasePlayer player, string command, string[] args) //работает
        {
            if (player == null) return;
            if (!player.IsAdmin) return;
            int value;
            if (!int.TryParse(args[0], out value)) return;
            APCharacter pers = APMembers.Find(x => x.OwnerId == player.userID);
            pers.AddExpirience(value);
        }
        [ChatCommand("save")]
        private void SaveThis(BasePlayer player, string command, string[] args)
        {
            SaveAPData(player);
        }
        #endregion

        [ChatCommand("cuishow")]
        private void CmdCuiShow(BasePlayer player)
        {
            ProgressUI(player);
            ProfileUI(player);
        }

        #region GUI
        private void InitializeGui(BasePlayer player)
        {
            if (player == null) return;
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
                timer.Once(1, () => InitializeGui(player));
            else
            ProgressUI(player);
        }
        private CuiLabel CreateLabel(string text, int i, float rowHeight, string xMin = "0", string xMax = "1", TextAnchor txtAnchor = TextAnchor.MiddleLeft, int fontSize = 15)
        {
            return new CuiLabel
            {
                Text =
                {
                    Text = text,
                    FontSize = fontSize,
                    Align = txtAnchor,
                    Color = "1.0 1.0 0.0 1.0"
                },
                RectTransform =
                {
                    AnchorMin = GetAnchorMin(xMin, $"{1 - rowHeight * i + i * .002f}"),
                    AnchorMax = GetAnchorMax(xMax, $"{1 - rowHeight * (i-1) + i * .002f}")
                }
            };
        }
        private CuiButton CreateButton(string command, int i, float rowHeight, string content = "+", int fontSize = 15, string xMin = "0", string xMax = "1")
        {
            return new CuiButton
            {
                Button =
                {
                    Command = command,
                    Color = "0.8 0.8 0.8 1.0"
                },
                RectTransform =
                {
                    AnchorMin = GetAnchorMin(xMin, $"{1 - rowHeight * i + i * .002f}"),
                    AnchorMax = GetAnchorMax(xMax, $"{1 - rowHeight * (i-1) + i * .002f}")
                },
                Text =
                {
                    Text = content,
                    FontSize = fontSize,
                    Align = TextAnchor.MiddleCenter
                }
            };
        }
        private CuiPanel CreatePanel(string xMin = "0", string xMax = "1", string yMin = "0", string yMax = "1", string color = "0.3451 0.5529 0.7725 0.75")
        {
            return new CuiPanel
            {
                Image =
                {
                    Color = color
                },
                RectTransform =
                {
                    AnchorMin = GetAnchorMin(xMin, yMin),
                    AnchorMax = GetAnchorMax(xMax, yMax)
                },
                CursorEnabled = false
            };
        }
        private void ProgressUI(BasePlayer player) //панелька опыта
        {
            GuiInfo info;
            if (!GUIInfo.TryGetValue(player.userID, out info))
                GUIInfo[player.userID] = info = new GuiInfo();
            else
                DestroyUI(player, info.UIHud);
            var pers = FindCharacter(player);
            if (!string.IsNullOrEmpty(info.UIHud))
                DestroyUI(player, info.UIHud);
            var elements = new CuiElementContainer();
            info.UIHud = elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.3451 0.5529 0.7725 0.75"
                },
                RectTransform =
                {
                    AnchorMin = GetAnchorMin("0.65","0.024"),
                    AnchorMax = GetAnchorMax("0.83","0.135")
                },
                CursorEnabled = false
            });
            elements.Add(new CuiButton
            {
                Button =
                {
                    Close = info.UIHud,
                    Color = "1.0 1.0 0.6078 0.0"
                },
                RectTransform =
                {
                    AnchorMin = GetAnchorMin("0.9","0.9"),
                    AnchorMax = GetAnchorMax("0.99","0.99")
                },
                Text =
                {
                    Text = "X",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.0 0.0 0.0 0.0"
                }
            }, info.UIHud);
            elements.Add(CreateLabel($"{pers.ExpPercentString}, {pers.CurrentLVL} level", 1, 0.68f, "0.05", "0.95", TextAnchor.MiddleCenter, 17), info.UIHud);
            elements.Add(CreatePanel("0.05", "0.95", "0.1", "0.35", $"0,1686 {Math.Round(pers.EarnedExpPercent / 100)} 0,4314, 0.9"), info.UIHud);
            elements.Add(CreateLabel($"{pers.CurrentEXP}/{pers.ExpToNextLvl}", 1, 1.55f, "0.05", "0.95", TextAnchor.MiddleCenter), info.UIHud);
            CuiHelper.AddUi(player, elements);
        }
        private void ProfileUI(BasePlayer player)
        {
            GuiInfo info;
            if (player == null) return;
            APCharacter pers = APMembers.Find(x => x.OwnerId == player.userID);
            if (!GUIInfo.TryGetValue(player.userID, out info))
                GUIInfo[player.userID] = info = new GuiInfo();
            else
                DestroyUI(player, info.UIMain);
            const float height = 1 / (6f * 1.5f);   
            var elements = new CuiElementContainer();
            info.UIMain = elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.3451 0.5529 0.7725 0.75"
                },
                RectTransform =
                {
                    AnchorMin = GetAnchorMin("0.75","0.35"),
                    AnchorMax = GetAnchorMax("0.98","0.75")
                },
                CursorEnabled = true
            });
            elements.Add(new CuiButton
            {
                Button =
                {
                    Close = info.UIMain,
                    Color = "1.0 1.0 0.6078 1.0"
                },
                RectTransform =
                {
                    AnchorMin = GetAnchorMin("0.9","0.9"),
                    AnchorMax = GetAnchorMax("0.99","0.99")
                },
                Text =
                {
                    Text = "X",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.0 0.0 0.0 1.0"
                }
            }, info.UIMain);
            elements.Add(CreateLabel($"{pers.SteamName}", 1, height, "0.05", "0.85"), info.UIMain);
            elements.Add(CreateLabel($"Уровень - {pers.CurrentLVL}, доступно ОУ - {pers.UpgradePoints}", 2, height, "0.05", "0.85"), info.UIMain);
            elements.Add(CreateLabel(pers.GetStatString(Characteristics.Strength), 3, height, "0.05", "0.85"), info.UIMain);
            elements.Add(CreateButton("statup.cmd strength-1", 3, height, "+", 15, "0.86", "0.92"), info.UIMain);
            elements.Add(CreateLabel(pers.GetStatString(Characteristics.Perception), 4, height, "0.05", "0.85"), info.UIMain);
            elements.Add(CreateButton("statup.cmd perception-1", 4, height, "+", 15, "0.86", "0.92"), info.UIMain);
            elements.Add(CreateLabel(pers.GetStatString(Characteristics.Endurance), 5, height, "0.05", "0.85"), info.UIMain);
            elements.Add(CreateButton("statup.cmd endurance-1", 5, height, "+", 15, "0.86", "0.92"), info.UIMain);
            elements.Add(CreateLabel(pers.GetStatString(Characteristics.Charisma), 6, height, "0.05", "0.85"), info.UIMain);
            elements.Add(CreateButton("statup.cmd charisma-1", 6, height, "+", 15, "0.86", "0.92"), info.UIMain);
            elements.Add(CreateLabel(pers.GetStatString(Characteristics.Intelligense), 7, height, "0.05", "0.85"), info.UIMain);
            elements.Add(CreateButton("statup.cmd intelligense-1", 7, height, "+", 15, "0.86", "0.92"), info.UIMain);
            elements.Add(CreateLabel(pers.GetStatString(Characteristics.Agility), 8, height, "0.05", "0.85"), info.UIMain);
            elements.Add(CreateButton("statup.cmd agility-1", 8, height, "+", 15, "0.86", "0.92"), info.UIMain);
            elements.Add(CreateLabel(pers.GetStatString(Characteristics.Luck), 9, height, "0.05", "0.85"), info.UIMain);
            elements.Add(CreateButton("statup.cmd luck-1", 9, height, "+", 15, "0.86", "0.92"), info.UIMain);
            CuiHelper.AddUi(player, elements);
        }
        #endregion
    }  
}

namespace AP
{
    public class Perk
    {
        public string Name { get; set; } // Название способности
        public Perks Type; //Тип из перечисления
        public int RequiredLVL;//Требуемый уровень характеристики
        public int MaxLVL = 3;//Максимальный уровень способности
        public int CurrentLVL { get; set; }
        public int Cost = 3;

        public Perk(Perks perk, int reqLVL)
        {
            Name = perk.ToString();
            CurrentLVL = 1;
            Type = perk;
            RequiredLVL = reqLVL;
        }
        public void Increase(int value)
        {
            if (CurrentLVL != MaxLVL)
                CurrentLVL += 1;
        }
    }

    public class Characteristic
    {
        public int CurrentLVL { get; set; }
        public string Name { get; set; }
        public Characteristics Type { get; set; }
        public int MaxLVL = 10;
        public int Cost = 1;

        public Characteristic(Characteristics stat)
        {
            Name = stat.ToString();
            Type = stat;
            CurrentLVL = 1;
        }
        /// <summary>
        /// Увеличивает хар-ку, value - значение, на которое увеличивается, points  - ссылка на upgradePoints
        /// </summary>
        /// <param name="value"></param>
        /// <param name="points"></param>
        public void Increase(int value, ref int points)
        {
            if (CurrentLVL != MaxLVL)
            {
                CurrentLVL += value;
                points -= Cost * value;
            }
            else
            {
                return;
            }
        }
    }

    public class APCharacter
    {
        public delegate void CharacterStateHandler(BasePlayer player, string mes);
        CharacterStateHandler _handler;
        public delegate void CharacterLvlUpHandler(APCharacter pers);
        CharacterLvlUpHandler _lvlHandler;
        public delegate void CharacterExpHandler(APCharacter pers);
        CharacterExpHandler _expHandler;
        public bool IsChatSubscriber { get; set; } //выводить сообщения в чат или нет
        public int CurrentLVL { get; set; }
        public int CurrentEXP { get; set; }
        public ulong OwnerId { get; set; }
        public string SteamName { get; set; }
        public int UpgradePoints;
        public int ExpToNextLvl { get; set; }
        public float EarnedExpPercent {get; set;}//количество опыта в процентах, которого не хватает до уровня
        public string ExpPercentString 
                                        {   
                                            get 
                                            { 
                                                return EarnedExpPercent.ToString("N2")+"%";
                                            }
                                            set 
                                            {
                                                
                                            }   
                                        }
        private const int maxLVL = 255; //максимальный уровень персонажа

        private const float lvlMultiplier = 1.8f; //множитель для количества опыта для сл. уровня

        public Characteristic Strength { get; set; }
        public Characteristic Perception { get; set; }
        public Characteristic Endurance { get; set; }
        public Characteristic Charisma { get; set; }
        public Characteristic Intelligense { get; set; }
        public Characteristic Agility { get; set; }
        public Characteristic Luck { get; set; }

        public List<Perk> Perks;

        public APCharacter(BasePlayer player)
        {
            OwnerId = player.userID;
            this.SteamName = player.displayName;
            CurrentLVL = 0;
            CurrentEXP = 0;
            UpgradePoints = 5;
            IsChatSubscriber = true;
            ExpToNextLvl = (int)Math.Round(180*lvlMultiplier);
            Strength = new Characteristic(Characteristics.Strength);
            Perception = new Characteristic(Characteristics.Perception);
            Endurance = new Characteristic(Characteristics.Endurance);
            Charisma = new Characteristic(Characteristics.Charisma);
            Intelligense = new Characteristic(Characteristics.Intelligense);
            Agility = new Characteristic(Characteristics.Agility);
            Luck = new Characteristic(Characteristics.Luck);
            Perks = new List<Perk>();
        }
        /// <summary>
        /// Функция добавления опыта, возвращает true при удачном добавлении, false при некорректном значении или при достижении максимального уровня
        /// </summary>
        /// <param name="value">Количество опыта</param>
        /// <returns></returns>
        public bool AddExpirience(int value)
        {
            if (value < 0) return false; //если передано отрицательное число
            if (CurrentLVL == maxLVL) return false; // если достигнут максимальный уровень
            //при оверэкспе повышаем уровень и переносим остаток на сл. уровень
            if (_handler != null)
                _handler(BasePlayer.FindByID(OwnerId), $"You got {value} experience points!");
            if (_expHandler != null)
                _expHandler(this);
            if (CurrentEXP + value > ExpToNextLvl)
            {
                int temp = value - (ExpToNextLvl - CurrentEXP);
                CharacterLVLUP();
                AddExpirience(temp);
                return true;
            }
            else
            {
                //при достижении границы перехода повышаем уровень и сбрасываем очки опыта до 0
                if (CurrentEXP + value == ExpToNextLvl)
                {
                    CharacterLVLUP();
                    OnExpValueChanged();
                    return true;
                }
                else //просто добавляем опыт
                {
                    CurrentEXP += value;
                    OnExpValueChanged();
                    return true;
                }
            }
        }
        public void IncreaseCharacteristic(Characteristics type, int value)
        {
            if (this.UpgradePoints < value)
            {
                if (_handler != null)
                    _handler(BasePlayer.FindByID(OwnerId), $"Not enough skill points!");
                return;
            }
            switch (type)
            {
                case Characteristics.Strength:
                    this.Strength.Increase(value, ref UpgradePoints);
                    if (_handler != null)
                        _handler(BasePlayer.FindByID(OwnerId), $"Your Strength is increased by {value}");
                    break;
                case Characteristics.Perception:
                    this.Perception.Increase(value, ref UpgradePoints);
                    if (_handler != null)
                        _handler(BasePlayer.FindByID(OwnerId), $"Your Perception is increased by {value}");
                    break;
                case Characteristics.Endurance:
                    this.Endurance.Increase(value, ref UpgradePoints);
                    if (_handler != null)
                        _handler(BasePlayer.FindByID(OwnerId), $"Your Endurance is increased by {value}");
                    break;
                case Characteristics.Charisma:
                    this.Charisma.Increase(value, ref UpgradePoints);
                    if (_handler != null)
                        _handler(BasePlayer.FindByID(OwnerId), $"Your Charisma is increased by {value}");
                    break;
                case Characteristics.Intelligense:
                    this.Intelligense.Increase(value, ref UpgradePoints);
                    if (_handler != null)
                        _handler(BasePlayer.FindByID(OwnerId), $"Your Intelligense is increased by {value}");
                    break;
                case Characteristics.Agility:
                    this.Agility.Increase(value, ref UpgradePoints);
                    if (_handler != null)
                        _handler(BasePlayer.FindByID(OwnerId), $"Your Agility is increased by {value}");
                    break;
                case Characteristics.Luck:
                    this.Luck.Increase(value, ref UpgradePoints); if (_handler != null)
                        _handler(BasePlayer.FindByID(OwnerId), $"Your Luck is increased by {value}");
                    break;
            }
        }
        #region Utils
        /// <summary>
        /// Вычисление прогресса опыта в процентах
        /// </summary>
        private void OnExpValueChanged()
        {
            EarnedExpPercent = ((CurrentEXP * 100) / ExpToNextLvl);
        }
        /// <summary>
        /// Повышение уровня персонажа, если reduceExp = true, сбрасываем опыт до 0, если false, значение опыта не изменяется
        /// </summary>
        private void CharacterLVLUP()
        {
            CurrentLVL += 1; //добавляем уровень
            if (_handler != null)
                _handler(BasePlayer.FindByID(OwnerId), $"Congratulations, you have reached level {CurrentLVL}!");
            if (_lvlHandler != null)
                _lvlHandler(this);
            UpgradePoints += 1; //добавляем поинты
            if (_handler != null)
                _handler(BasePlayer.FindByID(OwnerId), $"You have {UpgradePoints} skill points!");
            CurrentEXP = 0;
            ExpToNextLvl = CurrentLVL * (int)Math.Round((180 * lvlMultiplier) * lvlMultiplier); //определяемый кол-во опыта для сл. уровня
        }
        #region Characteristic return
        public int GetStrenght()
        {
            return this.Strength.CurrentLVL;
        }
        public int GetPerception()
        {
            return this.Perception.CurrentLVL;
        }
        public int GetEndurance()
        {
            return this.Endurance.CurrentLVL;
        }
        public int GetChrisma()
        {
            return this.Charisma.CurrentLVL;
        }
        public int GetIntelligense()
        {
            return this.Intelligense.CurrentLVL;
        }
        public int GetAgility()
        {
            return this.Agility.CurrentLVL;
        }
        public int GetLuck()
        {
            return this.Luck.CurrentLVL;
        }

        public string GetStatString(Characteristics type)
        {
            switch (type)
            {
                case Characteristics.Strength:
                    return string.Concat(string.Concat(this.Strength.Name, " - "), this.Strength.CurrentLVL.ToString());
                case Characteristics.Perception:
                    return string.Concat(string.Concat(this.Perception.Name, " - "), this.Perception.CurrentLVL.ToString());
                case Characteristics.Endurance:
                    return string.Concat(string.Concat(this.Endurance.Name, " - "), this.Endurance.CurrentLVL.ToString());
                case Characteristics.Charisma:
                    return string.Concat(string.Concat(this.Charisma.Name, " - "), this.Charisma.CurrentLVL.ToString());
                case Characteristics.Intelligense:
                    return string.Concat(string.Concat(this.Intelligense.Name, " - "), this.Intelligense.CurrentLVL.ToString());
                case Characteristics.Agility:
                    return string.Concat(string.Concat(this.Agility.Name, " - "), this.Agility.CurrentLVL.ToString());
                case Characteristics.Luck:
                    return string.Concat(string.Concat(this.Luck.Name, " - "), this.Luck.CurrentLVL.ToString());
            }
            return "Unknown parameter";
        }
        #endregion

        public void RegStateHandler(CharacterStateHandler del)
        {
            _handler += del;
            _handler(BasePlayer.FindByID(OwnerId), $"Now you can receive messages about changes to the character in the chat");
        }
        public void UnregStateHandler(CharacterStateHandler del)
        {
            _handler(BasePlayer.FindByID(OwnerId), $"Now you can not receive messages about changes to the character in the chat");
            _handler -= del;
        }
        public void RegStateHandler(CharacterLvlUpHandler del)
        {
            _lvlHandler += del;
        }
        public void UnregStateHandler(CharacterLvlUpHandler del)
        {
            _lvlHandler -= del;
        }
        public void RegStateHandler(CharacterExpHandler del)
        {
            _expHandler += del;
        }
        public void UnregStateHandler(CharacterExpHandler del)
        {
            _expHandler -= del;
        }
        #endregion
    }

    public class GuiInfo
    {
        public string UIMain { get; set; }
        public string UIHud { get; set; }
    }
}

namespace AP.Keywords
{
    public enum Characteristics
    {
        Strength = 1,
        Perception,
        Endurance,
        Charisma,
        Intelligense,
        Agility,
        Luck  // uda4a
    }

    public enum Perks
    {
        Ghoul = 1,
        AquaMan
    }

    static class APKeys
    {
        public const string DataFileName = "APData";
        public const string DataSaved = "Data saved successfully";
    }
}
