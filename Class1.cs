using System;
using System.Collections.Generic;
using System.Reflection;
using Modding;
using UnityEngine;

namespace NovaMod
{
    public class NovaMod : Mod
    {
        public override string GetVersion() => "Beta 1.1.2";

        private GameObject _uiObject;

        public override void Initialize()
        {
            _uiObject = new GameObject("CompletionCheckUI");
            _uiObject.AddComponent<CompletionCheckGUI>();
            UnityEngine.Object.DontDestroyOnLoad(_uiObject);
            Modding.Logger.Log("112% Tracker (modes 1/2/3) loaded. Press F5 to show/hide, 1-3 to switch mode, F6 to change language.");
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
        private int _currentMode = 1;           // 1 = 112%, 2 = True Ending, 3 = 112+%
        private List<string> _missingItems = new List<string>();
        private Vector2 _scrollPos;
        private float _completionPercentage = -1f;
        private string _statusMessage = "";

        private static bool _useEnglish = false;
        private static Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();

        private string T(string ru, string en) => _useEnglish ? en : ru;

        public void OnGUI()
        {
            // Переключение видимости и смена языка (работают всегда)
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

            // Переключение режимов (только когда UI открыт)
            if (_showUI && Event.current != null && Event.current.type == EventType.KeyDown)
            {
                int newMode = -1;
                if (Event.current.keyCode == KeyCode.Alpha1 || Event.current.keyCode == KeyCode.Keypad1) newMode = 1;
                if (Event.current.keyCode == KeyCode.Alpha2 || Event.current.keyCode == KeyCode.Keypad2) newMode = 2;
                if (Event.current.keyCode == KeyCode.Alpha3 || Event.current.keyCode == KeyCode.Keypad3) newMode = 3;

                if (newMode != -1 && newMode != _currentMode)
                {
                    _currentMode = newMode;
                    _scrollPos = Vector2.zero;
                    RefreshData();
                }
            }

            if (!_showUI) return;

            // Отрисовка
            GUI.backgroundColor = Color.black;
            Rect windowRect = new Rect(50, 50, 400, 600);

            string title;
            switch (_currentMode)
            {
                case 1:
                    title = T("ЧЕК-ЛИСТ 112%", "112% CHECKLIST");
                    break;
                case 2:
                    title = T("ЧЕК-ЛИСТ TRUE ENDING", "TRUE ENDING CHECKLIST");
                    break;
                case 3:
                    title = T("ЧЕК-ЛИСТ 112+%", "112+% CHECKLIST");
                    break;
                default:
                    title = T("ЧЕК-ЛИСТ", "CHECKLIST");
                    break;
            }
            GUI.Box(windowRect, title);

            GUI.Label(new Rect(70, 80, 350, 20), T("F6: язык | 1/2/3: режим", "F6: language | 1/2/3: mode"));
            GUI.Label(new Rect(70, 100, 350, 40), _statusMessage);

            if (_missingItems.Count == 0 && _completionPercentage >= 112f && _currentMode == 1)
            {
                GUI.Label(new Rect(70, 150, 350, 30), T("Поздравляем! Все 112% собраны.", "Congratulations! All 112% completed."));
                return;
            }
            if (_missingItems.Count == 0 && _completionPercentage < 0)
                return;

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

            _missingItems = GetMissingItemsForMode(pd, _currentMode);
        }

        private List<string> GetMissingItemsForMode(PlayerData pd, int mode)
        {
            switch (mode)
            {
                case 1: return GetMissing112(pd);
                case 2: return GetMissingTrueEnding(pd);
                case 3: return GetMissing112Plus(pd);
                default: return new List<string> { T("Неизвестный режим", "Unknown mode") };
            }
        }

        // ===================== РЕЖИМ 1: 112% =====================
        private List<string> GetMissing112(PlayerData pd)
        {
            List<string> missing = new List<string>();

            // Боссы
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

            // Амулеты (1-36)
            missing.Add(T("Амулеты:", "Charms:"));
            for (int i = 1; i <= 36; i++)
            {
                if (!ReadBoolField(pd, "gotCharm_" + i))
                {
                    missing.Add(T($"Получить {GetCharmNameRu(i)}", $"Collect {GetCharmNameEn(i)}"));
                }
            }
            missing.Add("");

            // Способности
            missing.Add(T("Способности:", "Abilities:"));
            if (!ReadBoolField(pd, "hasDash")) missing.Add(T("Получить Накидку мотылька", "Obtain Mothwing Cloak"));
            if (!ReadBoolField(pd, "hasWalljump")) missing.Add(T("Получить Клещню богомола", "Obtain Mantis Claw"));
            if (!ReadBoolField(pd, "hasSuperDash")) missing.Add(T("Получить Кристальное сердце", "Obtain Crystal Heart"));
            if (!ReadBoolField(pd, "hasDoubleJump")) missing.Add(T("Получить Монаршие крылья", "Obtain Monarch Wings"));
            if (!ReadBoolField(pd, "hasAcidArmour")) missing.Add(T("Получить Слезу Измы", "Obtain Isma's Tear"));
            if (!ReadBoolField(pd, "hasKingsBrand")) missing.Add(T("Получить Тавро Короля", "Obtain King's Brand"));
            if (!ReadBoolField(pd, "hasShadowDash")) missing.Add(T("Получить Теневой плащ", "Obtain Shade Cloak"));
            missing.Add("");

            // Гвоздь
            missing.Add(T("Гвоздь:", "Nail:"));
            int nail = ReadIntField(pd, "nailSmithUpgrades");
            if (nail < 4) missing.Add(T($"Гвоздь улучшен {nail}/4", $"Nail upgraded {nail}/4"));
            missing.Add("");

            // Техники гвоздя
            missing.Add(T("Техники гвоздя:", "Nail Arts:"));
            if (!ReadBoolField(pd, "hasUpwardSlash")) missing.Add(T("Выучить Великий удар", "Learn Great Slash"));
            if (!ReadBoolField(pd, "hasDashSlash")) missing.Add(T("Выучить Рассекающий удар", "Learn Dash Slash"));
            if (!ReadBoolField(pd, "hasCyclone")) missing.Add(T("Выучить Ураганный удар", "Learn Cyclone Slash"));
            missing.Add("");

            // Заклинания
            missing.Add(T("Заклинания:", "Spells:"));
            if (ReadIntField(pd, "fireballLevel") < 1) missing.Add(T("Мстительный дух", "Vengeful Spirit"));
            if (ReadIntField(pd, "fireballLevel") < 2) missing.Add(T("Теневая душа", "Shade Soul"));
            if (ReadIntField(pd, "quakeLevel") < 1) missing.Add(T("Опустошающее пике", "Desolate Dive"));
            if (ReadIntField(pd, "quakeLevel") < 2) missing.Add(T("Нисходящая тьма", "Descending Dark"));
            if (ReadIntField(pd, "screamLevel") < 1) missing.Add(T("Воющие духи", "Howling Wraiths"));
            if (ReadIntField(pd, "screamLevel") < 2) missing.Add(T("Вопль бездны", "Abyss Shriek"));
            missing.Add("");

            // Маски / Сосуды
            int health = ReadIntField(pd, "maxHealth");
            if (health < 9) missing.Add(T($"Маски: {health}/9", $"Masks: {health}/9"));
            int soulFrags = ReadIntField(pd, "MPReserveMax");
            if (soulFrags > 0 && soulFrags < 99)
                missing.Add(T($"Сосуды души: {soulFrags / 33}/3 колб", $"Soul Vessels: {soulFrags / 33}/3"));
            else if (soulFrags == 0)
                missing.Add(T("Нет ни одной колбы", "No soul vessels"));
            missing.Add("");

            // Гвоздь Грёз
            missing.Add(T("Гвоздь грёз:", "Dream Nail:"));
            if (!ReadBoolField(pd, "hasDreamNail")) missing.Add(T("Получить Гвоздь Грёз", "Obtain Dream Nail"));
            if (!ReadBoolField(pd, "dreamNailUpgraded")) missing.Add(T("Пробуждение Гвоздя Грёз", "Upgrade Dream Nail"));
            if (!ReadBoolField(pd, "mothDeparted")) missing.Add(T("Последние слова Провидицы", "Seer's final words"));
            missing.Add("");

            // Воины Грёз
            missing.Add(T("Воины грёз:", "Dream Warriors:"));
            if (!ReadBoolField(pd, "killedGhostAladar")) missing.Add(T("Убить Горба", "Defeat Gorb"));
            if (!ReadBoolField(pd, "killedGhostXero")) missing.Add(T("Убить Ксеро", "Defeat Xero"));
            if (!ReadBoolField(pd, "killedGhostMarmu")) missing.Add(T("Убить Марму", "Defeat Marmu"));
            if (!ReadBoolField(pd, "killedGhostHu")) missing.Add(T("Убить Старейшину Ху", "Defeat Elder Hu"));
            if (!ReadBoolField(pd, "killedGhostGalien")) missing.Add(T("Убить Гальена", "Defeat Galien"));
            if (!ReadBoolField(pd, "killedGhostNoEyes")) missing.Add(T("Убить Незрячую", "Defeat No Eyes"));
            if (!ReadBoolField(pd, "killedGhostMarkoth")) missing.Add(T("Убить Маркота", "Defeat Markoth"));
            missing.Add("");

            // Грезящие
            missing.Add(T("Грезящие:", "Dreamers:"));
            if (!ReadBoolField(pd, "lurienDefeated")) missing.Add(T("Убить Лурьен", "Defeat Lurien"));
            if (!ReadBoolField(pd, "monomonDefeated")) missing.Add(T("Убить Мономону", "Defeat Monomon"));
            if (!ReadBoolField(pd, "hegemolDefeated")) missing.Add(T("Убить Херру", "Defeat Herrah"));
            missing.Add("");

            // Колизей
            if (!ReadBoolField(pd, "colosseumTrial3")) missing.Add(T("Испытание Глупца", "Trial of the Fool"));
            missing.Add("");

            // Гримм
            missing.Add(T("Гримм:", "Grimm:"));
            if (!ReadBoolField(pd, "gotCharm_37")) missing.Add(T("Получить Ловкача", "Collect Grimmchild"));
            if (!ReadBoolField(pd, "gotCharm_38")) missing.Add(T("Получить Щит Грёз", "Collect Dreamshield"));
            if (!ReadBoolField(pd, "gotCharm_39")) missing.Add(T("Получить Беспечную песнь", "Collect Carefree Melody"));
            if (!ReadBoolField(pd, "gotCharm_40")) missing.Add(T("Получить Мрачное дитя", "Collect Grimmchild"));
            if (!ReadBoolField(pd, "killedGrimm")) missing.Add(T("Убить Маэстро Труппы", "Defeat Troupe Master Grimm"));
            if (!ReadBoolField(pd, "defeatedNightmareGrimm")) missing.Add(T("Завершить ритуал Гримма или изгнать труппу", "Complete Grimm ritual or banish"));
            missing.Add("");

            // Улей и Божий кров
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

        // ===================== РЕЖИМ 2: TRUE ENDING =====================
        private List<string> GetMissingTrueEnding(PlayerData pd)
        {
            List<string> missing = new List<string>();
            // Добавьте свои пункты здесь
            missing.Add(T("В разработке", "Soon"));

            int health = ReadIntField(pd, "maxHealth");
            int health_pieces = ReadIntField(pd, "heartPieces");
            if (health < 9) missing.Add(T($"Осколки масок: {(health - 5) * 4+health_pieces}/16", "Soon"));

            int vessel = ReadIntField(pd, "MPReserveMax");
            int vessel_fragments = ReadIntField(pd, "vesselFragment");
            if (vessel < 99) missing.Add(T($"Осколки масок: {vessel/33 + vessel_fragments}/9", "Soon"));

            if (!ReadBoolField(pd, "hasSpell")) missing.Add(T("Заклинание", "Soon"));
            if (ReadIntField(pd, "fireballLevel") < 1) missing.Add(T("Мстительный дух", "Soon"));
            if (ReadIntField(pd, "fireballLevel") < 2) missing.Add(T("Теневая душа", "Soon"));
            if (ReadIntField(pd, "quakeLevel") < 1) missing.Add(T("Опустошающее пике", "Soon"));
            if (ReadIntField(pd, "quakeLevel") < 2) missing.Add(T("Нисходящая тьма", "Soon"));
            if (ReadIntField(pd, "screamLevel") < 1) missing.Add(T("Воющие духи", "Soon"));
            if (ReadIntField(pd, "screamLevel") < 2) missing.Add(T("Вопль бездны", "Soon"));

            if (!ReadBoolField(pd, "hasNailArt")) missing.Add(T("Техника гвоздя", "Soon"));
            if (!ReadBoolField(pd, "hasCyclone")) missing.Add(T("Ураганный удар", "Soon"));
            if (!ReadBoolField(pd, "hasDashSlash")) missing.Add(T("Рассекающий удар", "Soon"));
            if (!ReadBoolField(pd, "hasUpwardSlash")) missing.Add(T("Великий удар", "Soon"));
            if (!ReadBoolField(pd, "hasAllNailArts")) missing.Add(T("Все техники гвоздя", "Soon"));

            if (!ReadBoolField(pd, "hasDreamNail")) missing.Add(T("Гвоздь грёз", "Soon"));
            if (!ReadBoolField(pd, "hasDreamGate")) missing.Add(T("Врата грёз", "Soon"));
            if (!ReadBoolField(pd, "dreamNailUpgraded")) missing.Add(T("Пробуждение гвоздя грёз", "Soon"));

            if (!ReadBoolField(pd, "hasDash")) missing.Add(T("Накидка мотылька", "Soon"));
            if (!ReadBoolField(pd, "hasWalljump")) missing.Add(T("Клещня богомола", "Soon"));
            if (!ReadBoolField(pd, "hasSuperDash")) missing.Add(T("Кристальное сердце", "Soon"));
            if (!ReadBoolField(pd, "hasShadowDash")) missing.Add(T("Теневой плащ", "Soon"));
            if (!ReadBoolField(pd, "hasAcidArmour")) missing.Add(T("Слеза Измы", "Soon"));
            if (!ReadBoolField(pd, "hasDoubleJump")) missing.Add(T("Монаршие крылья", "Soon"));

            if (!ReadBoolField(pd, "hasLantern")) missing.Add(T("Светомуший фонарь", "Soon"));
            if (!ReadBoolField(pd, "hasTramPass")) missing.Add(T("Проездной", "Soon"));
            if (!ReadBoolField(pd, "hasQuill")) missing.Add(T("Перо", "Soon"));
            if (!ReadBoolField(pd, "hasSlykey") & !ReadBoolField(pd, "gaveSlykey")) missing.Add(T("Ключ лавочника", "Soon"));
            if (ReadBoolField(pd, "hasSlykey") & !ReadBoolField(pd, "gaveSlykey")) missing.Add(T("Отдать Ключ лавочника", "Soon"));
            if (!ReadBoolField(pd, "hasWhiteKey")) missing.Add(T("Элегантный ключ", "Soon"));
            if (!ReadBoolField(pd, "usedWhiteKey")) missing.Add(T("Использовать Элегантный ключ", "Soon"));
            if (!ReadBoolField(pd, "hasMenderKey")) missing.Add(T("Ключ Починочного Жука", "Soon"));
            if (!ReadBoolField(pd, "hasKingsBrand")) missing.Add(T("Тавро Короля", "Soon"));

            if (!ReadBoolField(pd, "foundTrinket1")) missing.Add(T("Дневник странника", "Soon"));
            if (!ReadBoolField(pd, "foundTrinket2")) missing.Add(T("Печать Халлоунеста", "Soon"));
            if (!ReadBoolField(pd, "foundTrinket3")) missing.Add(T("Идол Короля", "Soon"));
            if (!ReadBoolField(pd, "foundTrinket4")) missing.Add(T("Загадочное яйцо", "Soon"));
            if (ReadIntField(pd, "soldTrinket1") < 15) missing.Add(T("Все Дневники странников", "Soon"));
            if (ReadIntField(pd, "soldTrinket2") < 15) missing.Add(T("Все Печати Халлоунеста", "Soon"));
            if (ReadIntField(pd, "soldTrinket3") < 7) missing.Add(T("Все Идолы Короля", "Soon"));
            if (ReadIntField(pd, "soldTrinket4") < 3) missing.Add(T("Все Загадочные яйца", "Soon"));
            if (ReadIntField(pd, "rancidEggs") < 1) missing.Add(T("Тухлое яйцо", "Soon"));
            if (ReadIntField(pd, "rancidEggs") < 80) missing.Add(T("Все Тухлые яйца (80)", "Soon"));
            if (!ReadBoolField(pd, "gotLurkerKey")) missing.Add(T("Бледный соглядатай", "Soon"));

            if (ReadIntField(pd, "guardiansDefeated") < 3) missing.Add(T("Все Грезящие", "Soon"));
            if (!ReadBoolField(pd, "lurienDefeated")) missing.Add(T("Убить Лурьен", "Soon"));
            if (!ReadBoolField(pd, "hegemolDefeated")) missing.Add(T("Убить Хегемоль", "Soon"));
            if (!ReadBoolField(pd, "monomonDefeated")) missing.Add(T("Убить Мономону", "Soon"));

            if (!ReadBoolField(pd, "metElderbug")) missing.Add(T("Встретить Старейшину", "Soon"));
            if (!ReadBoolField(pd, "elderbugHistory1")) missing.Add(T("Старейшина: История Халлоунеста 1", "Soon"));
            if (!ReadBoolField(pd, "elderbugHistory2")) missing.Add(T("Старейшина: История Халлоунеста 2", "Soon"));
            if (!ReadBoolField(pd, "elderbugHistory3")) missing.Add(T("Старейшина: История Халлоунеста 3", "Soon"));
            if (!ReadBoolField(pd, "elderbugSpeechSly")) missing.Add(T("Старейшина: Слай", "Soon"));
            if (!ReadBoolField(pd, "elderbugSpeechStation")) missing.Add(T("Старейшина: Рогач", "Soon"));
            if (!ReadBoolField(pd, "elderbugSpeechEggTemple")) missing.Add(T("Старейшина: Храм Чёрного Яйца", "Soon"));
            if (!ReadBoolField(pd, "elderbugSpeechMapShop")) missing.Add(T("Старейшина: Магазин карт", "Soon"));

            return missing;
        }

        // ===================== РЕЖИМ 3: 112+% =====================
        private List<string> GetMissing112Plus(PlayerData pd)
        {
            List<string> missing = new List<string>();
            // Добавьте свои пункты здесь
            missing.Add(T("В разработке", "Soon"));
            return missing;
        }

        // =============== Вспомогательные методы ===============
        private string GetCharmNameRu(int id)
        {
            switch (id)
            {
                case 1: return "Загребущий рой";
                case 2: return "Капризный компас";
                case 3: return "Песнь гусеничек";
                case 4: return "Крепкий панцирь";
                case 5: return "Панцирь бальдра";
                case 6: return "Ярость павшего";
                case 7: return "Быстрый фокус";
                case 8: return "Живительное сердце";
                case 9: return "Живительное ядро";
                case 10: return "Герб защитника";
                case 11: return "Тремогнездо";
                case 12: return "Колючки страданий";
                case 13: return "Метку гордости";
                case 14: return "Непоколебимое тело";
                case 15: return "Тяжёлый выпад";
                case 16: return "Пронизывающую тень";
                case 17: return "Споровый гриб";
                case 18: return "Длинный гвоздь";
                case 19: return "Шаманский камень";
                case 20: return "Ловца душ";
                case 21: return "Пожирателя душ";
                case 22: return "Пылающее чрево";
                case 23: return "Хрупкое сердце";
                case 24: return "Хрупкую жадность";
                case 25: return "Хрупкую сила";
                case 26: return "Ореол мастера гвоздя";
                case 27: return "Благословение Джони";
                case 28: return "Облик Унн";
                case 29: return "Кровь Улья";
                case 30: return "Повелителя грёз";
                case 31: return "Трюкача";
                case 32: return "Быстрый удар";
                case 33: return "Искажателя заклинаний";
                case 34: return "Глубокий фокус";
                case 35: return "Элегию куколки";
                case 36: return "Душу короля";
                default: return "Амулет " + id;
            }
        }

        private string GetCharmNameEn(int id)
        {
            switch (id)
            {
                case 1: return "Gathering Swarm";
                case 2: return "Wayward Compass";
                case 3: return "Grubsong";
                case 4: return "Stalwart Shell";
                case 5: return "Baldur Shell";
                case 6: return "Fury of the Fallen";
                case 7: return "Quick Focus";
                case 8: return "Lifeblood Heart";
                case 9: return "Lifeblood Core";
                case 10: return "Defender's Crest";
                case 11: return "Flukenest";           // Исправлено (соответствует Тремогнезду)
                case 12: return "Thorns of Agony";
                case 13: return "Mark of Pride";
                case 14: return "Steady Body";
                case 15: return "Heavy Blow";
                case 16: return "Sharp Shadow";
                case 17: return "Spore Shroom";
                case 18: return "Longnail";
                case 19: return "Shaman Stone";
                case 20: return "Soul Catcher";
                case 21: return "Soul Eater";
                case 22: return "Glowing Womb";        // Исправлено (соответствует Пылающему чреву)
                case 23: return "Fragile Heart";
                case 24: return "Fragile Greed";
                case 25: return "Fragile Strength";
                case 26: return "Nailmaster's Glory";
                case 27: return "Joni's Blessing";
                case 28: return "Shape of Unn";
                case 29: return "Hiveblood";
                case 30: return "Dream Wielder";
                case 31: return "Dashmaster";          // (Трюкач)
                case 32: return "Quick Slash";
                case 33: return "Spell Twister";
                case 34: return "Deep Focus";
                case 35: return "Grubberfly's Elegy";
                case 36: return "Kingsoul";            // Добавлено (Душа короля)
                default: return "Charm " + id;
            }
        }

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
