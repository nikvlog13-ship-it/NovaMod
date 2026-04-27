using System;
using System.Collections.Generic;
using System.Reflection;
using Modding;
using UnityEngine;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace NovaMod
{
    public class NovaMod : Mod
    {
        public override string GetVersion() => "Beta 1.1.0";

        private GameObject _uiObject;

        public override void Initialize()
        {
            _uiObject = new GameObject("CompletionCheckUI");
            _uiObject.AddComponent<CompletionCheckGUI>();
            UnityEngine.Object.DontDestroyOnLoad(_uiObject);
            Modding.Logger.Log("112% Tracker (correct percentage) loaded. Press F5 to show/hide, F6 to switch language.");
        }

        public void Unload()
        {
            if (_uiObject != null)
                UnityEngine.Object.Destroy(_uiObject);
        }
    }

    public class CompletionCheckGUI : MonoBehaviour
    {
        private bool _showUI = false;
        private List<string> _missingItems = new List<string>();
        private Vector2 _scrollPos;
        private float _completionPercentage = -1f;
        private string _statusMessage = "";

        private static bool _useEnglish = false;
        private static Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();

        private string T(string ru, string en) => _useEnglish ? en : ru;

        public void OnGUI()
        {
            if (Event.current != null &&
                Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.F5)
            {
                _showUI = !_showUI;
                if (_showUI) RefreshData();
            }

            if (Event.current != null &&
                Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.F6)
            {
                _useEnglish = !_useEnglish;
                if (_showUI) RefreshData();
            }

            if (!_showUI) return;

            GUI.backgroundColor = Color.black;
            Rect windowRect = new Rect(50, 50, 400, 600);
            GUI.Box(windowRect, T("ЧЕК-ЛИСТ 112%", "112% CHECKLIST"));

            GUI.Label(new Rect(70, 80, 350, 20), T("F6: язык", "F6: language"));
            GUI.Label(new Rect(70, 100, 350, 40), _statusMessage);

            if (_missingItems.Count == 0 && _completionPercentage >= 112f)
            {
                GUI.Label(new Rect(70, 150, 350, 30), T("Поздравляем! Все 112% собраны.", "Congratulations! All 112% completed."));
                return;
            }

            _scrollPos = GUI.BeginScrollView(
                new Rect(70, 120, 350, 450),
                _scrollPos,
                new Rect(0, 0, 330, _missingItems.Count * 20)
            );

            for (int i = 0; i < _missingItems.Count; i++)
                GUI.Label(new Rect(0, i * 20, 330, 25), "• " + _missingItems[i]);

            GUI.EndScrollView();
        }

        private void RefreshData()
        {
            _missingItems.Clear();
            _completionPercentage = -1f;
            _statusMessage = "";

            PlayerData pd = GameManager.instance?.playerData ?? PlayerData.instance;
            if (pd == null)
            {
                _statusMessage = T("PlayerData недоступен. Загрузите сохранение.", "PlayerData unavailable. Load a save.");
                return;
            }

            _completionPercentage = ReadFloatField(pd, "completionPercentage");
            string percentText = T("Текущий процент: ", "Current percentage: ");
            _statusMessage = percentText + _completionPercentage + "%";
            if (_completionPercentage < 0)
                _statusMessage += " " + T("(ошибка чтения)", "(read error)");

            _missingItems = GetMissingElements(pd);
        }

        private List<string> GetMissingElements(PlayerData pd)
        {
            List<string> missing = new List<string>();

            // Боссы / Bosses
            missing.Add(T("Боссы:", "Bosses:"));
            if (!ReadBoolField(pd, "killedBigBuzzer")) missing.Add(T("Убить Матку Жужж", "Kill Gruz Mother"));
            if (!ReadBoolField(pd, "killedFalseKnight")) missing.Add(T("Убить Ложного Рыцаря", "Kill False Knight"));
            if (!ReadBoolField(pd, "killedHornet")) missing.Add(T("Убить Первую Хорнет", "Kill Hornet (Protector)"));
            if (!ReadBoolField(pd, "killedDungDefender")) missing.Add(T("Убить Навозного Защитника", "Kill Dung Defender"));
            if (!ReadBoolField(pd, "killedMawlek")) missing.Add(T("Убить Задумчивого Чревня", "Kill Brooding Mawlek"));
            if (!ReadBoolField(pd, "killedMageLord")) missing.Add(T("Убить Мастера Душ", "Kill Soul Master"));
            if (!ReadBoolField(pd, "killedMantisLord")) missing.Add(T("Убить Лордов Богомолов", "Kill Mantis Lords"));
            if (!ReadBoolField(pd, "killedMimicSpider")) missing.Add(T("Убить Носка", "Kill Nosk"));
            if (!ReadBoolField(pd, "killedInfectedKnight")) missing.Add(T("Убить Разбитого Сосуда", "Kill Broken Vessel"));
            if (!ReadBoolField(pd, "collectorDefeated")) missing.Add(T("Убить Коллекционера", "Kill The Collector"));
            if (!ReadBoolField(pd, "killedMegaJellyfish")) missing.Add(T("Убить Ууму", "Kill Uumuu"));
            if (!ReadBoolField(pd, "hornetOutskirtsDefeated")) missing.Add(T("Убить Вторую Хорнет", "Kill Hornet (Sentinel)"));
            if (!ReadBoolField(pd, "killedTraitorLord")) missing.Add(T("Убить Предавшего Лорда", "Kill Traitor Lord"));
            if (!ReadBoolField(pd, "killedBlackKnight")) missing.Add(T("Убить Рыцарей Хранителей", "Kill Watcher Knights"));
            missing.Add("");

            // Амулеты / Charms
            missing.Add(T("Амулеты:", "Charms:"));
            if (!ReadBoolField(pd, "gotCharm_1")) missing.Add(T("Получить Загребущий рой", "Collect Gathering Swarm"));
            if (!ReadBoolField(pd, "gotCharm_2")) missing.Add(T("Получить Капризный компас", "Collect Wayward Compass"));
            if (!ReadBoolField(pd, "gotCharm_3")) missing.Add(T("Получить Песнь гусеничек", "Collect Grubsong"));
            if (!ReadBoolField(pd, "gotCharm_4")) missing.Add(T("Получить Крепкий панцирь", "Collect Stalwart Shell"));
            if (!ReadBoolField(pd, "gotCharm_5")) missing.Add(T("Получить Панцирь бальдра", "Collect Baldur Shell"));
            if (!ReadBoolField(pd, "gotCharm_6")) missing.Add(T("Получить Ярость павшего", "Collect Fury of the Fallen"));
            if (!ReadBoolField(pd, "gotCharm_7")) missing.Add(T("Получить Быстрый фокус", "Collect Quick Focus"));
            if (!ReadBoolField(pd, "gotCharm_8")) missing.Add(T("Получить Живительное сердце", "Collect Lifeblood Heart"));
            if (!ReadBoolField(pd, "gotCharm_9")) missing.Add(T("Получить Живительное ядро", "Collect Lifeblood Core"));
            if (!ReadBoolField(pd, "gotCharm_10")) missing.Add(T("Получить Герб защитника", "Collect Defender's Crest"));
            if (!ReadBoolField(pd, "gotCharm_11")) missing.Add(T("Получить Тремогнездо", "Collect Glowing Womb"));
            if (!ReadBoolField(pd, "gotCharm_12")) missing.Add(T("Получить Колючки страданий", "Collect Thorns of Agony"));
            if (!ReadBoolField(pd, "gotCharm_13")) missing.Add(T("Получить Метку гордости", "Collect Mark of Pride"));
            if (!ReadBoolField(pd, "gotCharm_14")) missing.Add(T("Получить Непоколибимое тело", "Collect Steady Body"));
            if (!ReadBoolField(pd, "gotCharm_15")) missing.Add(T("Получить Тяжёлый выпад", "Collect Heavy Blow"));
            if (!ReadBoolField(pd, "gotCharm_16")) missing.Add(T("Получить Пронизывающую тень", "Collect Sharp Shadow"));
            if (!ReadBoolField(pd, "gotCharm_17")) missing.Add(T("Получить Споровый гриб", "Collect Spore Shroom"));
            if (!ReadBoolField(pd, "gotCharm_18")) missing.Add(T("Получить Длинный гвоздь", "Collect Longnail"));
            if (!ReadBoolField(pd, "gotCharm_19")) missing.Add(T("Получить Шаманский камень", "Collect Shaman Stone"));
            if (!ReadBoolField(pd, "gotCharm_20")) missing.Add(T("Получить Ловца душ", "Collect Soul Catcher"));
            if (!ReadBoolField(pd, "gotCharm_21")) missing.Add(T("Получить Пожирателя душ", "Collect Soul Eater"));
            if (!ReadBoolField(pd, "gotCharm_22")) missing.Add(T("Получить Пылающее чрево", "Collect Flukenest"));
            if (!ReadBoolField(pd, "gotCharm_23")) missing.Add(T("Получить Хрупкое сердце", "Collect Fragile Heart"));
            if (!ReadBoolField(pd, "gotCharm_24")) missing.Add(T("Получить Хрупкую жадность", "Collect Fragile Greed"));
            if (!ReadBoolField(pd, "gotCharm_25")) missing.Add(T("Получить Хрупкую силу", "Collect Fragile Strength"));
            if (!ReadBoolField(pd, "gotCharm_26")) missing.Add(T("Получить Ореол мастера гвоздя", "Collect Nailmaster's Glory"));
            if (!ReadBoolField(pd, "gotCharm_27")) missing.Add(T("Получить Благословение Джони", "Collect Joni's Blessing"));
            if (!ReadBoolField(pd, "gotCharm_28")) missing.Add(T("Получить Облик Унн", "Collect Shape of Unn"));
            if (!ReadBoolField(pd, "gotCharm_29")) missing.Add(T("Получить Кровь Улья", "Collect Hiveblood"));
            if (!ReadBoolField(pd, "gotCharm_30")) missing.Add(T("Получить Повелителя грёз", "Collect Dream Wielder"));
            if (!ReadBoolField(pd, "gotCharm_31")) missing.Add(T("Получить Трюкача", "Collect Dashmaster"));
            if (!ReadBoolField(pd, "gotCharm_32")) missing.Add(T("Получить Быстрый удар", "Collect Quick Slash"));
            if (!ReadBoolField(pd, "gotCharm_33")) missing.Add(T("Получить Искажатель заклинаний", "Collect Spell Twister"));
            if (!ReadBoolField(pd, "gotCharm_34")) missing.Add(T("Получить Глубокий фокус", "Collect Deep Focus"));
            if (!ReadBoolField(pd, "gotCharm_35")) missing.Add(T("Получить Элегию куколки", "Collect Grubberfly's Elegy"));
            if (!ReadBoolField(pd, "gotCharm_36")) missing.Add(T("Получить Душу короля", "Collect Kingsoul"));
            missing.Add("");

            // Способности / Abilities
            missing.Add(T("Способности:", "Abilities:"));
            if (!ReadBoolField(pd, "hasDash")) missing.Add(T("Получить Накидку мотылька", "Obtain Mothwing Cloak"));
            if (!ReadBoolField(pd, "hasWalljump")) missing.Add(T("Получить Клещню богомола", "Obtain Mantis Claw"));
            if (!ReadBoolField(pd, "hasSuperDash")) missing.Add(T("Получить Кристальное сердце", "Obtain Crystal Heart"));
            if (!ReadBoolField(pd, "hasDoubleJump")) missing.Add(T("Получить Монаршие крылья", "Obtain Monarch Wings"));
            if (!ReadBoolField(pd, "hasAcidArmour")) missing.Add(T("Получить Слезу Измы", "Obtain Isma's Tear"));
            if (!ReadBoolField(pd, "hasKingsBrand")) missing.Add(T("Получить Тавро короля", "Obtain King's Brand"));
            if (!ReadBoolField(pd, "hasShadowDash")) missing.Add(T("Получить Теневой плащ", "Obtain Shade Cloak"));
            missing.Add("");

            // Гвоздь / Nail
            missing.Add(T("Гвоздь:", "Nail:"));
            int nail = ReadIntField(pd, "nailSmithUpgrades");
            if (nail < 4) missing.Add(T($"Гвоздь улучшен {nail}/4", $"Nail upgraded {nail}/4"));
            missing.Add("");

            // Техники гвоздя / Nail Arts
            missing.Add(T("Техники гвоздя:", "Nail Arts:"));
            if (!ReadBoolField(pd, "hasUpwardSlash")) missing.Add(T("Выучить Великий удар", "Learn Great Slash"));
            if (!ReadBoolField(pd, "hasDashSlash")) missing.Add(T("Выучить Рассекающий удар", "Learn Dash Slash"));
            if (!ReadBoolField(pd, "hasCyclone")) missing.Add(T("Выучить Ураганный удар", "Learn Cyclone Slash"));
            missing.Add("");

            // Заклинания / Spells
            missing.Add(T("Заклинания:", "Spells:"));
            if (ReadIntField(pd, "fireballLevel") < 1) missing.Add(T("Мстительный дух", "Vengeful Spirit"));
            if (ReadIntField(pd, "fireballLevel") < 2) missing.Add(T("Теневая душа", "Shade Soul"));
            if (ReadIntField(pd, "quakeLevel") < 1) missing.Add(T("Опустошающее пике", "Desolate Dive"));
            if (ReadIntField(pd, "quakeLevel") < 2) missing.Add(T("Нисхождение тьмы", "Descending Dark"));
            if (ReadIntField(pd, "screamLevel") < 1) missing.Add(T("Воющие призраки", "Howling Wraiths"));
            if (ReadIntField(pd, "screamLevel") < 2) missing.Add(T("Вопль бездны", "Abyss Shriek"));
            missing.Add("");

            // Маски / Masks
            int health = ReadIntField(pd, "maxHealth");
            if (health < 9) missing.Add(T($"Маски: {health}/9", $"Masks: {health}/9"));

            // Сосуды души / Soul Vessels
            int soulFrags = ReadIntField(pd, "MPReserveMax");
            if (soulFrags > 0 && soulFrags < 99)
                missing.Add(T($"Сосуды души: {soulFrags / 33}/3 колб", $"Soul Vessels: {soulFrags / 33}/3"));
            else if (soulFrags == 0)
                missing.Add(T("Нет ни одной колбы", "No soul vessels"));
            missing.Add("");

            // Гвоздь Грёз / Dream Nail
            missing.Add(T("Гвоздь грёз:", "Dream Nail:"));
            if (!ReadBoolField(pd, "hasDreamNail")) missing.Add(T("Получить Гвоздь Грёз", "Obtain Dream Nail"));
            if (!ReadBoolField(pd, "dreamNailUpgraded")) missing.Add(T("Улучшить Гвоздь Грёз", "Upgrade Dream Nail"));
            if (!ReadBoolField(pd, "mothDeparted")) missing.Add(T("Последние слова Провидицы", "Seer's final words"));
            missing.Add("");

            // Воины Грёз / Dream Warriors
            missing.Add(T("Воины грёз:", "Dream Warriors:"));
            if (!ReadBoolField(pd, "killedGhostAladar")) missing.Add(T("Убить Горба", "Defeat Gorb"));
            if (!ReadBoolField(pd, "killedGhostXero")) missing.Add(T("Убить Ксеро", "Defeat Xero"));
            if (!ReadBoolField(pd, "killedGhostMarmu")) missing.Add(T("Убить Марму", "Defeat Marmu"));
            if (!ReadBoolField(pd, "killedGhostHu")) missing.Add(T("Убить Старейшину Ху", "Defeat Elder Hu"));
            if (!ReadBoolField(pd, "killedGhostGalien")) missing.Add(T("Убить Гальена", "Defeat Galien"));
            if (!ReadBoolField(pd, "killedGhostNoEyes")) missing.Add(T("Убить Незрячую", "Defeat No Eyes"));
            if (!ReadBoolField(pd, "killedGhostMarkoth")) missing.Add(T("Убить Маркота", "Defeat Markoth"));
            missing.Add("");

            // Грезящие / Dreamers
            missing.Add(T("Грезящие:", "Dreamers:"));
            if (!ReadBoolField(pd, "lurienDefeated")) missing.Add(T("Убить Лурьена", "Defeat Lurien"));
            if (!ReadBoolField(pd, "monomonDefeated")) missing.Add(T("Убить Мономону", "Defeat Monomon"));
            if (!ReadBoolField(pd, "hegemolDefeated")) missing.Add(T("Убить Хегемоля", "Defeat Herrah"));
            missing.Add("");

            // Колизей / Coliseum
            if (!ReadBoolField(pd, "colosseumTrial3")) missing.Add(T("Испытание Глупца", "Trial of the Fool"));
            missing.Add("");

            // Труппа Гримма / Grimm Troupe
            missing.Add(T("Гримм:", "Grimm:"));
            if (!ReadBoolField(pd, "gotCharm_37")) missing.Add(T("Получить Ловкача", "Collect Grimmchild"));
            if (!ReadBoolField(pd, "gotCharm_38")) missing.Add(T("Получить Щит Грёз", "Collect Dreamshield"));
            if (!ReadBoolField(pd, "gotCharm_39")) missing.Add(T("Получить Беспечную песнь", "Collect Carefree Melody"));
            if (!ReadBoolField(pd, "gotCharm_40")) missing.Add(T("Получить Мрачное дитя", "Collect Grimmchild"));
            if (!ReadBoolField(pd, "killedGrimm")) missing.Add(T("Убить Маэстро Труппы", "Defeat Troupe Master Grimm"));
            if (!ReadBoolField(pd, "defeatedNightmareGrimm")) missing.Add(T("Завершить ритуал Гримма или изгнать труппу", "Complete Grimm ritual or banish"));
            missing.Add("");

            // Улей и Божий кров / Hive & Godhome
            missing.Add(T("Улей и Божий кров:", "Hive & Godhome:"));
            if (!ReadBoolField(pd, "killedHiveKnight")) missing.Add(T("Убить Рыцаря улья", "Defeat Hive Knight"));
            if (!ReadBoolField(pd, "godseekerUnlocked")) missing.Add(T("Разблокировать Божий кров", "Unlock Godhome"));
            for (int i = 1; i <= 4; i++)
            {
                string fieldName = "bossDoorStateTier" + i;
                object doorState = GetFieldValue(pd, fieldName);
                if (doorState != null)
                {
                    if (!ReadChildBoolField(doorState, "completed"))
                        missing.Add(T($"Пантеон {i}", $"Pantheon {i}"));
                }
                else
                {
                    if (!ReadBoolField(pd, fieldName))
                        missing.Add(T($"Пантеон {i}", $"Pantheon {i}"));
                }
            }

            return missing;
        }

        // =============== Вспомогательные методы рефлексии ===============
        private bool ReadBoolField(PlayerData pd, string name)
        {
            FieldInfo fi = GetField(pd, name);
            if (fi != null && fi.FieldType == typeof(bool))
                return (bool)fi.GetValue(pd);
            try { return pd.GetBool(name); }
            catch { return false; }
        }

        private int ReadIntField(PlayerData pd, string name)
        {
            FieldInfo fi = GetField(pd, name);
            if (fi != null && (fi.FieldType == typeof(int) || fi.FieldType == typeof(byte) ||
                               fi.FieldType == typeof(short) || fi.FieldType == typeof(long)))
                return Convert.ToInt32(fi.GetValue(pd));
            try { return pd.GetInt(name); }
            catch { return 0; }
        }

        private float ReadFloatField(PlayerData pd, string name)
        {
            FieldInfo fi = GetField(pd, name);
            if (fi != null && (fi.FieldType == typeof(float) || fi.FieldType == typeof(double)))
                return Convert.ToSingle(fi.GetValue(pd));
            try { return pd.GetFloat(name); }
            catch { return -1f; }
        }

        private FieldInfo GetField(PlayerData pd, string name)
        {
            if (_fieldCache.TryGetValue(name, out FieldInfo fi))
                return fi;
            Type t = pd.GetType();
            fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null)
                _fieldCache[name] = fi;
            return fi;
        }

        private object GetFieldValue(PlayerData pd, string name)
        {
            FieldInfo fi = GetField(pd, name);
            return fi?.GetValue(pd);
        }

        private bool ReadChildBoolField(object obj, string fieldName)
        {
            if (obj == null) return false;
            Type t = obj.GetType();
            FieldInfo fi = t.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null && fi.FieldType == typeof(bool))
                return (bool)fi.GetValue(obj);
            return false;
        }
    }
}