using System;
using System.Collections.Generic;

using Oxide.Game.Rust.Cui;

using UnityEngine;

using AP;
using AP.Keywords;

namespace Oxide.Plugins
{
    [Info("TestPlugin","WP","0.0.2")]
    internal class CustomUI : RustPlugin
    {
        private const string plVersion = "0.0.2";
        private readonly Dictionary<ulong, GuiInfo> GUIInfo;

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
            }
        }
        #endregion

        #region Utility
        private void DestroyUI(BasePlayer player, string name)
        {
            if (!string.IsNullOrEmpty(name)) CuiHelper.DestroyUi(player, name);
        }

        private string GetAnchorMin(string xMin, string yMin)
        {
            return string.Concat(string.Concat(xMin, " "), yMin);
        }
        private string GetAnchorMax(string xMax, string yMax)
        {
            return string.Concat(string.Concat(xMax, " "), yMax);
        }
        #endregion

        [ChatCommand("cuishow")]
        private void CmdCuiShow(BasePlayer player, string command, string[] args)
        {
            GuiInfo info;
            if (!GUIInfo.TryGetValue(player.userID, out info))
                GUIInfo[player.userID] = info = new GuiInfo();
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
            elements.Add(CreateLabel($"AwesomePerks ({plVersion})", 1, height, "0.15", "0.85"), info.UIMain);
            elements.Add(CreateLabel($"Уровень -  ({plVersion})", 1, height, "0.15", "0.85"), info.UIMain);
            elements.Add(CreateLabel(Characteristics.Strength.ToString() + " - ?", 3, height, "0.09", "0.85"), info.UIMain);
            elements.Add(CreateLabel(Characteristics.Perception.ToString() + " - ?", 4, height, "0.09", "0.85"), info.UIMain);
            elements.Add(CreateLabel(Characteristics.Endurance.ToString() + " - ?", 5, height, "0.09", "0.85"), info.UIMain);
            elements.Add(CreateLabel(Characteristics.Charisma.ToString() + " - ?", 6, height, "0.09", "0.85"), info.UIMain);
            elements.Add(CreateLabel(Characteristics.Intelligense.ToString() + " - ?", 7, height, "0.09", "0.85"), info.UIMain);
            elements.Add(CreateLabel(Characteristics.Agility.ToString() + " - ?", 8, height, "0.09", "0.85"), info.UIMain);
            elements.Add(CreateLabel(Characteristics.Luck.ToString() + " - ?", 9, height, "0.09", "0.85"), info.UIMain);
            CuiHelper.AddUi(player, elements);
        }
        
        [ConsoleCommand("Guihide.cmd")]
        private void CmdCuiHide(ConsoleSystem.Arg arg)
        {
            BasePlayer player = BasePlayer.Find(arg.Args[0]);
            GuiInfo info;
            if (!GUIInfo.TryGetValue(player.userID, out info)) return;
            DestroyUI(player, info.UIMain);
        }

        #region GUI
        private CuiLabel CreateLabel(string text, int i, float rowHeight, string xMin = "0", string xMax = "1")
        {
            return new CuiLabel
            {
                Text =
                {
                    Text = text,
                    FontSize = 15,
                    Align = TextAnchor.MiddleLeft,
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
                }
            };
        }
        #endregion
    }

    public class GuiInfo
    {
        public string UIMain { get; set; }
        public string UIContent { get; set; }
    }  
}

namespace AP
{
    public class Perk
    {
        private int currentLVL;
        private string name;
        public string Name { get { return name; } } // Название способности
        public Perks Type; //Тип из перечисления
        public int RequiredLVL;//Требуемый уровень характеристики
        public int MaxLVL;//Максимальный уровень способности
        public int CurrentLVL { get { return currentLVL; } }
        private int Cost;

        public Perk(Perks perk, int reqLVL, int maxLVL, int cost)
        {
            name = perk.ToString();
            Type = perk;
            RequiredLVL = reqLVL;
            MaxLVL = maxLVL;
            Cost = cost;
        }
    }

    public class Characteristic
    {
        private int currentLVL;
        public int CurrentLVL { get { return currentLVL; } }
        public string Name { get; }
        public Characteristics Type { get; }
        private const int MaxLVL = 10;
        private int Cost = 1;

        public Characteristic(Characteristics stat)
        {
            Name = stat.ToString();
            Type = stat;
            currentLVL = 1;
        }

        public void Increase(int value, ref int points)
        {
            if (currentLVL != MaxLVL)
            {
                currentLVL += value;
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
        private int currentLVL; //текущий уровень персонажа
        public int CurrentLVL { get { return currentLVL; } }
        private int currentEXP; //текущее количество опыта персонажа
        public int CurrentEXP { get { return currentEXP; } }
        private ulong ownerId; // Rust айди игрока
        public ulong OwnerId { get { return ownerId; } }
        private int upgradePoints; //количество очков улучшения
        public int UpgradePoint { get { return upgradePoints; } }
        private int expToNextLvl; //общее количество опыта для повышения уровня
        public int ExpToNextLvl { get { return expToNextLvl; } }
        private float earnedExpPercent;//количество опыта в процентах, которого не хватает до уровня
        public string EarnedExpPercent { get { return earnedExpPercent.ToString("N2")+"%"; } }
        private const int maxLVL = 255; //максимальный уровень персонажа

        private const float lvlMultiplier = 1.8f; //множитель для количества опыта для сл. уровня

        private Characteristic Strength;
        private Characteristic Perception;
        private Characteristic Endurance;
        private Characteristic Charisma;
        private Characteristic Intelligense;
        private Characteristic Agility;
        private Characteristic Luck;

        public List<Perk> Perks;

        public APCharacter(ulong owner)
        {
            ownerId = owner;
            currentLVL = 0;
            currentEXP = 0;
            upgradePoints = 5;
            expToNextLvl = (int)Math.Round(180*lvlMultiplier);
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
            if (currentLVL == maxLVL) return false; // если достигнут максимальный уровень
            //при оверэкспе повышаем уровень и переносим остаток на сл. уровень
            if (currentEXP + value > expToNextLvl)
            {
                int temp = value - (expToNextLvl - currentEXP);
                CharacterLVLUP();
                AddExpirience(temp);
                return true;
            }
            else
            {
                //при достижении границы перехода повышаем уровень и сбрасываем очки опыта до 0
                if (currentEXP + value == expToNextLvl)
                {
                    CharacterLVLUP();
                    OnExpValueChanged();
                    return true;
                }
                else //просто добавляем опыт
                {
                    currentEXP += value;
                    OnExpValueChanged();
                    return true;
                }
            }
        }
        public void IncreaseCharacteristic(Characteristics type, int value)
        {
            if (this.upgradePoints < value)
            {
                Console.WriteLine("Not enough points!");
                return;
            }
            switch (type)
            {
                case Characteristics.Strength:
                    this.Strength.Increase(value, ref upgradePoints);
                    break;
                case Characteristics.Perception:
                    this.Perception.Increase(value, ref upgradePoints);
                    break;
                case Characteristics.Endurance:
                    this.Endurance.Increase(value, ref upgradePoints);
                    break;
                case Characteristics.Charisma:
                    this.Charisma.Increase(value, ref upgradePoints);
                    break;
                case Characteristics.Intelligense:
                    this.Intelligense.Increase(value, ref upgradePoints);
                    break;
                case Characteristics.Agility:
                    this.Agility.Increase(value, ref upgradePoints);
                    break;
                case Characteristics.Luck:
                    this.Luck.Increase(value, ref upgradePoints);
                    break;
            }
        }

        #region Utils
        /// <summary>
        /// Вычисление прогресса опыта в процентах
        /// </summary>
        private void OnExpValueChanged()
        {
            earnedExpPercent = ((currentEXP * 100) / expToNextLvl);
        }
        /// <summary>
        /// Повышение уровня персонажа, если reduceExp = true, сбрасываем опыт до 0, если false, значение опыта не изменяется
        /// </summary>
        private void CharacterLVLUP()
        {
            currentLVL += 1; //добавляем уровень
            upgradePoints += 1; //добавляем поинты
            currentEXP = 0;
            expToNextLvl = currentLVL * (int)Math.Round((180 * lvlMultiplier) * lvlMultiplier); //определяемый кол-во опыта для сл. уровня
        }
        #endregion
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
        Luck
    }

    public enum Perks
    {
        Ghoul = 1,
        AquaMan
    }
}
