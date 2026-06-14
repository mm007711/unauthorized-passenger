using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class GalStoryFile
{
    public string title = "GAL Template";
    public string startNode = "start_001";
    public string defaultBackground = "bedroom";
    public string textTable = "Text/story_text.csv";
    public List<GalBackgroundEntry> backgrounds = new List<GalBackgroundEntry>();
    public List<GalArtProfile> artProfiles = new List<GalArtProfile>();
    public List<GalPortraitEntry> portraits = new List<GalPortraitEntry>();
    public List<GalLanguageEntry> languages = new List<GalLanguageEntry>();
    public List<GalExplorePoint> explorePoints = new List<GalExplorePoint>();
    public List<GalStoryNode> nodes = new List<GalStoryNode>();
}

[Serializable]
public class GalBackgroundEntry
{
    public string id;
    public string displayName;
    public string path;
}

[Serializable]
public class GalArtProfile
{
    public string id;
    public string displayName;
    public string uiSkin;
    public string backgroundFolder;
}

[Serializable]
public class GalLanguageEntry
{
    public string id;
    public string displayName;
    public string tablePath;
    public string textTable;
}

[Serializable]
public class GalExplorePoint
{
    public string id;
    public string displayName;
    public string scene;
    public string nodeId;
    public string background;
    public string requiredFlag;
    public float x = 0.5f;
    public float y = 0.5f;
    public float width = 170f;
    public float height = 48f;
    public List<GalStoryCommand> commands = new List<GalStoryCommand>();
}

[Serializable]
public class GalStoryNode
{
    public string id;
    public string speaker;
    [TextArea(2, 8)] public string text;
    public string background;
    public string portraitSlot;
    public string portraitCharacter;
    public string portraitExpression;
    public string portraitFacing;
    public string portraitAnimation;
    public string portraitPath;
    public string nextId;
    public List<GalStoryChoice> choices = new List<GalStoryChoice>();
    public List<GalStoryCommand> commands = new List<GalStoryCommand>();
}

[Serializable]
public class GalStoryChoice
{
    public string id;
    public string text;
    public string nextId;
    public string requiredFlag;
    public List<GalStoryCommand> commands = new List<GalStoryCommand>();
}

[Serializable]
public class GalStoryCommand
{
    public string command;
    public string key;
    public string value;
    public string slot;
    public string character;
    public string expression;
    public string facing;
    public string animation;
    public string path;
    public float amount;
}

[Serializable]
public class GalTemplateSaveData
{
    public int version = 1;
    public bool isExploring;
    public string currentNodeId;
    public string currentBackgroundId;
    public string savedAt;
    public List<string> flags = new List<string>();
    public List<string> inventory = new List<string>();
    public List<string> readNodes = new List<string>();
}

[Serializable]
public class GalTemplateSettings
{
    public float textSpeed = 42f;
    public float autoDelay = 1.2f;
    public float masterVolume = 0.8f;
    public bool fullscreen = true;
    public bool skipUnreadText;
    public string language = "zh-CN";
    public string artProfile = "default";
}

public class GalHistoryLine
{
    public string speaker;
    public string text;
}

public class GalTextEntry
{
    public string key;
    public string speaker;
    public string text;
    public string portraitSlot;
    public string portraitCharacter;
    public string portraitExpression;
    public string portraitFacing;
    public string portraitAnimation;
    public string portraitPath;
}

public class GalRawTextRow
{
    public string key;
    public readonly Dictionary<string, string> values = new Dictionary<string, string>();
}

public class GalTemplateRuntime : MonoBehaviour
{
    private const string StoryRelativePath = "GAL/gal_story.json";
    private const string DefaultTextTableRelativePath = "Text/story_text.csv";
    private const string SaveFolderName = "GalTemplate";
    private const int SaveSlotCount = 6;
    private const int QuickSaveSlot = 1;

    private static GalTemplateRuntime instance;

    private enum GalOverlayPage
    {
        None,
        Settings,
        SaveLoad,
        History,
        PortraitDebug
    }

    private readonly Dictionary<string, GalStoryNode> nodesById = new Dictionary<string, GalStoryNode>();
    private readonly Dictionary<string, GalBackgroundEntry> backgroundsById = new Dictionary<string, GalBackgroundEntry>();
    private readonly Dictionary<string, GalExplorePoint> explorePointsById = new Dictionary<string, GalExplorePoint>();
    private readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    private readonly HashSet<string> flags = new HashSet<string>();
    private readonly HashSet<string> inventory = new HashSet<string>();
    private readonly HashSet<string> readNodes = new HashSet<string>();
    private readonly List<GalHistoryLine> history = new List<GalHistoryLine>();
    private readonly Dictionary<string, GalRawTextRow> rawTextRowsByKey = new Dictionary<string, GalRawTextRow>();
    private readonly Dictionary<string, GalTextEntry> textEntriesByKey = new Dictionary<string, GalTextEntry>();

    private GalStoryFile story;
    private GalTemplateSettings settings = new GalTemplateSettings();
    private string storyPath;
    private string textTablePath;
    private DateTime storyLastWriteTimeUtc;
    private DateTime textTableLastWriteTimeUtc;
    private float hotReloadTimer;
    private GalStoryNode currentNode;
    private string currentNodeId;
    private string currentBackgroundId;
    private string currentLine;
    private bool currentNodeCommandsExecuted;
    private bool currentNodeWasReadBefore;
    private bool isTyping;
    private bool isInGame;
    private bool isExploring;
    private bool isAwaitingChoice;
    private bool isSettingsOpen;
    private bool isSaveLoadOpen;
    private GalOverlayPage currentOverlayPage = GalOverlayPage.None;
    private GalOverlayPage previousOverlayPage = GalOverlayPage.None;
    private bool isAutoMode;
    private bool isSkipMode;
    private bool isDialogueHidden;
    private Coroutine typingRoutine;
    private Coroutine autoRoutine;
    private Coroutine toastRoutine;
    private Font uiFont;

    private Canvas canvas;
    private GameObject backgroundRoot;
    private GameObject backgroundWashRoot;
    private RawImage backgroundImage;
    private AspectRatioFitter backgroundAspect;
    private GalPortraitController portraitController;
    private Coroutine sceneTransitionRoutine;
    private GameObject transitionRoot;
    private Image transitionImage;
    private Text transitionText;
    private bool isTransitioning;
    private GameObject mainMenuRoot;
    private Text menuTitleText;
    private Text primaryActionLabel;
    private Text saveInfoText;
    private Button primaryActionButton;
    private Button newGameButton;
    private Text newGameButtonLabel;
    private Text mainMenuSettingsButtonLabel;
    private Text quitButtonLabel;
    private GameObject dialogueRoot;
    private Text speakerText;
    private Text dialogueText;
    private Text continueHintText;
    private Transform choiceContainer;
    private GameObject exploreRoot;
    private Transform exploreButtonContainer;
    private RectTransform exploreButtonAreaRect;
    private AspectRatioFitter exploreButtonAreaAspect;
    private Text exploreTitleText;
    private GameObject hudRoot;
    private Text autoButtonLabel;
    private Text skipButtonLabel;
    private Text hudSaveButtonLabel;
    private Text hudLoadButtonLabel;
    private Text hudHideButtonLabel;
    private Text hudHistoryButtonLabel;
    private Text hudSettingsButtonLabel;
    private Text hudDebugButtonLabel;
    private Text hudTitleButtonLabel;
    private GameObject fbxHudRoot;
    private Text fbxBackButtonLabel;
    private Text fbxSaveButtonLabel;
    private Text fbxLoadButtonLabel;
    private Text fbxHistoryButtonLabel;
    private Text fbxSettingsButtonLabel;
    private Text fbxTitleButtonLabel;
    private GameObject toastRoot;
    private Text toastText;
    private GameObject historyRoot;
    private Text historyText;
    private Text historyTitleText;
    private Text historyBackButtonLabel;
    private Text historyExitButtonLabel;
    private Button historyBackButton;
    private GameObject saveLoadRoot;
    private Text saveLoadTitleText;
    private Transform saveSlotContainer;
    private Button saveLoadBackButton;
    private Text saveLoadBackButtonLabel;
    private Text saveLoadExitButtonLabel;
    private bool saveLoadPanelForSaving;
    private GameObject settingsRoot;
    private Text settingsTitleText;
    private Text textSpeedValueText;
    private Text autoDelayValueText;
    private Text volumeValueText;
    private Text languageValueText;
    private Text settingsTextSpeedLabel;
    private Text settingsAutoDelayLabel;
    private Text settingsVolumeLabel;
    private Text settingsFullscreenLabel;
    private Text settingsSkipUnreadLabel;
    private Text settingsSavePanelButtonLabel;
    private Text settingsLoadPanelButtonLabel;
    private Text settingsHistoryButtonLabel;
    private Text settingsReloadButtonLabel;
    private Text settingsDeleteButtonLabel;
    private Text settingsDebugButtonLabel;
    private Text settingsExitButtonLabel;
    private Button settingsSavePanelButton;
    private Toggle fullscreenToggle;
    private Toggle skipUnreadToggle;
    private Slider textSpeedSlider;
    private Slider autoDelaySlider;
    private Slider volumeSlider;
    private GameObject portraitDebugRoot;
    private Text portraitDebugTitleText;
    private Text portraitDebugSlotLabel;
    private Text portraitDebugCharacterLabel;
    private Text portraitDebugExpressionLabel;
    private Text portraitDebugFacingLabel;
    private Text portraitDebugAnimationLabel;
    private Text portraitDebugBackButtonLabel;
    private Text portraitDebugExitButtonLabel;
    private string debugPortraitSlot = "center";
    private string debugPortraitCharacter = "test";
    private string debugPortraitExpression = "neutral";
    private string debugPortraitFacing = "auto";
    private string debugPortraitAnimation = "shake";

    private string SaveDirectory
    {
        get { return Path.Combine(Application.persistentDataPath, SaveFolderName); }
    }

    private string GetSavePath(int slot)
    {
        return Path.Combine(SaveDirectory, "save_" + Mathf.Clamp(slot, 1, SaveSlotCount).ToString("00") + ".json");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindObjectOfType<GalTemplateRuntime>() != null)
        {
            return;
        }

        GameObject runtimeObject = new GameObject("GAL Template Runtime");
        DontDestroyOnLoad(runtimeObject);
        runtimeObject.AddComponent<GalTemplateRuntime>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;
        LoadSettings();
        LoadStory();
        BuildUi();
        ApplySettings();
        ShowMainMenu();
    }

    private void Update()
    {
        CheckHotReload();

        if (GalFbxSceneController.IsSceneActive)
        {
            UpdateExternalSceneInput();
            return;
        }

        if (isSettingsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitOverlayPages();
            }

            return;
        }

        if (isSaveLoadOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                ExitOverlayPages();
            }

            return;
        }

        if (historyRoot != null && historyRoot.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.H) || Input.GetMouseButtonDown(1))
            {
                ExitOverlayPages();
            }

            return;
        }

        if (portraitDebugRoot != null && portraitDebugRoot.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Input.GetMouseButtonDown(1))
            {
                ExitOverlayPages();
            }

            return;
        }

        if (isInGame && Input.GetKeyDown(KeyCode.Escape))
        {
            ShowMainMenu();
            return;
        }

        if (!isInGame)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            ToggleAutoMode();
        }

        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            ToggleSkipMode();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                SaveGame();
            }
            else
            {
                ShowSavePanel();
            }
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                LoadLatestGame();
            }
            else
            {
                ShowLoadPanel();
            }
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            ShowHistory();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ShowPortraitDebug();
        }

        if (Input.GetMouseButtonDown(1))
        {
            ToggleDialogueHidden();
        }

        if (isExploring || currentNode == null || isAwaitingChoice || isDialogueHidden)
        {
            return;
        }

        bool pressedContinue = Input.GetKeyDown(KeyCode.Space) || (Input.GetMouseButtonDown(0) && !IsPointerOverInteractiveUi());
        if (pressedContinue)
        {
            ContinueStory();
        }
    }

    private void UpdateExternalSceneInput()
    {
        bool overlayOpen = IsOverlayPageOpen();
        GalFbxSceneController.Instance.SetControlsEnabled(!overlayOpen && !IsPointerOverInteractiveUi());

        if (overlayOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                ExitOverlayPages();
            }

            return;
        }

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                SaveGame();
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                LoadLatestGame();
            }
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            ShowLoadPanel();
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            ShowHistory();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitFbxScene();
        }
    }

    private bool IsOverlayPageOpen()
    {
        return isSettingsOpen ||
            isSaveLoadOpen ||
            (historyRoot != null && historyRoot.activeSelf) ||
            (portraitDebugRoot != null && portraitDebugRoot.activeSelf);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void StartNewGame()
    {
        flags.Clear();
        inventory.Clear();
        readNodes.Clear();
        history.Clear();
        currentNode = null;
        currentNodeId = null;
        currentBackgroundId = null;
        if (portraitController != null)
        {
            portraitController.HideAll();
        }

        isInGame = true;
        isExploring = false;
        isAutoMode = false;
        isSkipMode = false;
        isDialogueHidden = false;
        mainMenuRoot.SetActive(false);
        hudRoot.SetActive(true);
        exploreRoot.SetActive(false);
        dialogueRoot.SetActive(true);
        ClearChoices();
        RefreshModeLabels();
        SetBackground(story.defaultBackground);
        PlayNode(story.startNode);
    }

    public void ContinueFromSave()
    {
        if (!LoadLatestGame())
        {
            ShowToast(T("ui.toast.no_save_start_new", "没有找到可读取的存档，已开始新游戏。"));
            StartNewGame();
        }
    }

    public void SaveGame()
    {
        SaveGameToSlot(QuickSaveSlot);
    }

    public void SaveGameToSlot(int slot)
    {
        if (!isInGame || string.IsNullOrEmpty(currentNodeId))
        {
            ShowToast(T("ui.toast.no_progress_to_save", "当前没有可保存的进度。"));
            return;
        }

        Directory.CreateDirectory(SaveDirectory);
        GalTemplateSaveData data = new GalTemplateSaveData();
        data.isExploring = isExploring;
        data.currentNodeId = currentNodeId;
        data.currentBackgroundId = currentBackgroundId;
        data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.flags.AddRange(flags);
        data.inventory.AddRange(inventory);
        data.readNodes.AddRange(readNodes);

        File.WriteAllText(GetSavePath(slot), JsonUtility.ToJson(data, true), Encoding.UTF8);
        RefreshMenuState();
        RefreshSaveLoadPanel();
        ShowToast(string.Format(T("ui.toast.saved_slot", "已保存到槽位 {0}。"), slot));
    }

    public bool LoadGame()
    {
        return LoadLatestGame();
    }

    public bool LoadLatestGame()
    {
        int latestSlot = FindLatestSaveSlot();
        if (latestSlot < 1)
        {
            return false;
        }

        return LoadGameFromSlot(latestSlot);
    }

    public bool LoadGameFromSlot(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            bool wasExternalScene = GalFbxSceneController.IsSceneActive;
            string json = File.ReadAllText(path, Encoding.UTF8);
            GalTemplateSaveData data = JsonUtility.FromJson<GalTemplateSaveData>(json);
            if (data == null || string.IsNullOrEmpty(data.currentNodeId))
            {
                return false;
            }

            flags.Clear();
            inventory.Clear();
            readNodes.Clear();
            history.Clear();
            if (data.flags != null)
            {
                foreach (string flag in data.flags)
                {
                    AddNonEmpty(flags, flag);
                }
            }

            if (data.inventory != null)
            {
                foreach (string item in data.inventory)
                {
                    AddNonEmpty(inventory, item);
                }
            }

            if (data.readNodes != null)
            {
                foreach (string nodeId in data.readNodes)
                {
                    AddNonEmpty(readNodes, nodeId);
                }
            }

            isInGame = true;
            isExploring = data.isExploring;
            isAutoMode = false;
            isSkipMode = false;
            isDialogueHidden = false;
            mainMenuRoot.SetActive(false);
            hudRoot.SetActive(true);
            exploreRoot.SetActive(false);
            dialogueRoot.SetActive(!isExploring);
            ClearChoices();
            RefreshModeLabels();
            SetBackground(string.IsNullOrEmpty(data.currentBackgroundId) ? story.defaultBackground : data.currentBackgroundId);
            if (portraitController != null)
            {
                portraitController.HideAll();
            }

            if (isExploring)
            {
                ShowExplore();
            }
            else
            {
                PlayNode(data.currentNodeId);
            }

            ExitOverlayPages();
            ShowToast(string.Format(T("ui.toast.loaded_slot", "已读取槽位 {0}。"), slot));
            if (wasExternalScene)
            {
                SetExternalSceneHudVisible(false);
                GalFbxSceneController.Instance.Exit(delegate
                {
                    SetGalSceneLayersVisible(true);
                });
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load save: " + exception.Message);
            return false;
        }
    }

    public bool HasSave()
    {
        return FindLatestSaveSlot() > 0;
    }

    public void DeleteSave()
    {
        DeleteAllSaves();
    }

    public void DeleteSaveSlot(int slot)
    {
        string path = GetSavePath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        RefreshMenuState();
        RefreshSaveLoadPanel();
        ShowToast(string.Format(T("ui.toast.deleted_slot", "槽位 {0} 已删除。"), slot));
    }

    public void DeleteAllSaves()
    {
        for (int slot = 1; slot <= SaveSlotCount; slot++)
        {
            string path = GetSavePath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        RefreshMenuState();
        RefreshSaveLoadPanel();
        ShowToast(T("ui.toast.all_saves_deleted", "本地存档已删除。"));
    }

    private int FindLatestSaveSlot()
    {
        int latestSlot = -1;
        DateTime latestTime = DateTime.MinValue;

        for (int slot = 1; slot <= SaveSlotCount; slot++)
        {
            string path = GetSavePath(slot);
            if (!File.Exists(path))
            {
                continue;
            }

            DateTime writeTime = File.GetLastWriteTime(path);
            if (writeTime > latestTime)
            {
                latestTime = writeTime;
                latestSlot = slot;
            }
        }

        return latestSlot;
    }

    public bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
    }

    public void SetFlag(string flag)
    {
        AddNonEmpty(flags, flag);
    }

    public void RemoveFlag(string flag)
    {
        if (!string.IsNullOrEmpty(flag))
        {
            flags.Remove(flag);
        }
    }

    public bool HasItem(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && inventory.Contains(itemId);
    }

    public void AddItem(string itemId)
    {
        AddNonEmpty(inventory, itemId);
    }

    public void RemoveItem(string itemId)
    {
        if (!string.IsNullOrEmpty(itemId))
        {
            inventory.Remove(itemId);
        }
    }

    private void ContinueStory()
    {
        if (isTyping)
        {
            FinishTypingImmediately();
            return;
        }

        if (currentNode == null)
        {
            return;
        }

        CancelAutoAdvance();

        if (!TryExecuteCurrentNodeCommands(out string jumpNode))
        {
            return;
        }

        if (!string.IsNullOrEmpty(jumpNode))
        {
            PlayNode(jumpNode);
            return;
        }

        List<GalStoryChoice> availableChoices = GetAvailableChoices(currentNode);
        if (availableChoices.Count > 0)
        {
            ShowChoices(availableChoices);
            return;
        }

        AdvanceFromNode(currentNode);
    }

    private void PlayNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || !nodesById.TryGetValue(nodeId, out GalStoryNode node))
        {
            Debug.LogWarning("Missing story node: " + nodeId);
            EndStory();
            return;
        }

        currentNode = node;
        currentNodeId = node.id;
        currentNodeCommandsExecuted = false;
        currentNodeWasReadBefore = readNodes.Contains(node.id);
        readNodes.Add(node.id);
        AddHistory(node);
        isExploring = false;
        isDialogueHidden = false;
        isAwaitingChoice = false;
        exploreRoot.SetActive(false);
        ClearChoices();

        if (!string.IsNullOrEmpty(node.background))
        {
            SetBackground(node.background);
        }

        ApplyNodePortrait(node);
        speakerText.text = string.IsNullOrEmpty(node.speaker) ? " " : node.speaker;
        currentLine = node.text ?? string.Empty;
        dialogueRoot.SetActive(true);
        StartTyping(currentLine);
    }

    private void AdvanceFromNode(GalStoryNode node)
    {
        if (!TryExecuteCurrentNodeCommands(out string jumpNode))
        {
            return;
        }

        if (!string.IsNullOrEmpty(jumpNode))
        {
            PlayNode(jumpNode);
            return;
        }

        if (!string.IsNullOrEmpty(node.nextId))
        {
            PlayNode(node.nextId);
            return;
        }

        EndStory();
    }

    private bool TryExecuteCurrentNodeCommands(out string jumpNode)
    {
        jumpNode = null;
        if (currentNodeCommandsExecuted)
        {
            return true;
        }

        currentNodeCommandsExecuted = true;
        return ExecuteCommands(currentNode.commands, out jumpNode);
    }

    private bool ExecuteCommands(List<GalStoryCommand> commands, out string jumpNode)
    {
        jumpNode = null;
        if (commands == null)
        {
            return true;
        }

        foreach (GalStoryCommand command in commands)
        {
            if (command == null || string.IsNullOrEmpty(command.command))
            {
                continue;
            }

            string commandName = command.command.Trim().ToLowerInvariant();
            string target = FirstNonEmpty(command.value, command.key);

            switch (commandName)
            {
                case "set_flag":
                    SetFlag(command.key);
                    break;
                case "remove_flag":
                    RemoveFlag(command.key);
                    break;
                case "add_item":
                    AddItem(command.key);
                    break;
                case "remove_item":
                    RemoveItem(command.key);
                    break;
                case "set_background":
                    SetBackground(target);
                    break;
                case "portrait":
                case "show_portrait":
                    ShowPortrait(command);
                    break;
                case "hide_portrait":
                    HidePortrait(command);
                    break;
                case "hide_portraits":
                case "clear_portraits":
                    if (portraitController != null)
                    {
                        portraitController.HideAll();
                    }
                    break;
                case "portrait_animation":
                case "animate_portrait":
                    AnimatePortrait(command);
                    break;
                case "enter_fbx_scene":
                case "load_fbx_scene":
                    EnterFbxScene(command);
                    return false;
                case "exit_fbx_scene":
                    ExitFbxScene();
                    return false;
                case "jump":
                case "goto":
                    jumpNode = target;
                    break;
                case "save":
                    SaveGame();
                    break;
                case "show_explore":
                case "explore":
                    ShowExplore();
                    return false;
                case "hide_dialogue":
                    HideDialogueWindow();
                    break;
                case "show_dialogue":
                    ShowDialogueWindow();
                    break;
                case "auto":
                    SetAutoMode(target == "true" || target == "on" || target == "1");
                    break;
                case "skip":
                    SetSkipMode(target == "true" || target == "on" || target == "1");
                    break;
                case "settings":
                case "open_settings":
                    ShowSettings();
                    break;
                case "menu":
                case "main_menu":
                    ShowMainMenu();
                    return false;
                case "end":
                case "end_dialogue":
                    EndStory();
                    return false;
                default:
                    Debug.LogWarning("Unknown GAL command: " + command.command);
                    break;
            }
        }

        return true;
    }

    private bool IsPointerOverInteractiveUi()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == null)
            {
                continue;
            }

            if (result.gameObject.GetComponentInParent<Button>() != null ||
                result.gameObject.GetComponentInParent<Slider>() != null ||
                result.gameObject.GetComponentInParent<Toggle>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void ShowChoices(List<GalStoryChoice> choices)
    {
        isAwaitingChoice = true;
        continueHintText.text = T("ui.dialogue.choose", "请选择");
        ClearChoices();

        for (int i = 0; i < choices.Count; i++)
        {
            GalStoryChoice choice = choices[i];
            Button button = CreateButton(choiceContainer, choice.text, delegate { Choose(choice); }, out _);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 42f;
        }
    }

    private void Choose(GalStoryChoice choice)
    {
        isAwaitingChoice = false;
        ClearChoices();
        continueHintText.text = T("ui.dialogue.continue", "点击或空格继续");

        if (!ExecuteCommands(choice.commands, out string jumpNode))
        {
            return;
        }

        string targetNode = FirstNonEmpty(jumpNode, choice.nextId);
        if (!string.IsNullOrEmpty(targetNode))
        {
            PlayNode(targetNode);
        }
        else
        {
            ContinueStory();
        }
    }

    private void ApplyNodePortrait(GalStoryNode node)
    {
        if (portraitController == null || node == null || string.IsNullOrEmpty(node.portraitCharacter))
        {
            return;
        }

        portraitController.Show(new GalPortraitPose
        {
            slot = node.portraitSlot,
            character = node.portraitCharacter,
            expression = node.portraitExpression,
            facing = node.portraitFacing,
            animation = node.portraitAnimation,
            path = node.portraitPath
        });
    }

    private void ShowPortrait(GalStoryCommand command)
    {
        if (portraitController == null || command == null)
        {
            return;
        }

        portraitController.Show(new GalPortraitPose
        {
            slot = FirstNonEmpty(command.slot, command.key),
            character = FirstNonEmpty(command.character, command.value),
            expression = command.expression,
            facing = command.facing,
            animation = command.animation,
            path = command.path
        });
    }

    private void HidePortrait(GalStoryCommand command)
    {
        if (portraitController == null)
        {
            return;
        }

        portraitController.Hide(command == null ? null : FirstNonEmpty(command.slot, command.key));
    }

    private void AnimatePortrait(GalStoryCommand command)
    {
        if (portraitController == null || command == null)
        {
            return;
        }

        portraitController.PlayAnimation(FirstNonEmpty(command.slot, command.key), FirstNonEmpty(command.animation, command.value));
    }

    private void EnterFbxScene(GalStoryCommand command)
    {
        string resourcePath = command == null ? null : FirstNonEmpty(command.path, command.value);
        float pixelSize = command != null ? command.amount : 0f;
        GalFbxSceneController.Instance.Enter(resourcePath, pixelSize, HideGalForExternalScene);
    }

    private void ExitFbxScene()
    {
        GalFbxSceneController.Instance.Exit(ShowGalAfterExternalScene);
    }

    private void ShowMainMenuFromExternalScene()
    {
        if (!GalFbxSceneController.IsSceneActive)
        {
            ShowMainMenu();
            return;
        }

        CloseOverlayPages(true);
        SetExternalSceneHudVisible(false);
        GalFbxSceneController.Instance.Exit(delegate
        {
            SetGalSceneLayersVisible(true);
            ShowMainMenu();
        });
    }

    private void HideGalForExternalScene()
    {
        CancelAutoAdvance();
        if (canvas != null)
        {
            canvas.enabled = true;
        }

        SetGalSceneLayersVisible(false);
        if (mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(false);
        }

        if (hudRoot != null)
        {
            hudRoot.SetActive(false);
        }

        if (exploreRoot != null)
        {
            exploreRoot.SetActive(false);
        }

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(false);
        }

        if (portraitController != null)
        {
            portraitController.HideAll();
        }

        CloseOverlayPages(true);
        SetExternalSceneHudVisible(true);
    }

    private void ShowGalAfterExternalScene()
    {
        if (canvas != null)
        {
            canvas.enabled = true;
        }

        SetExternalSceneHudVisible(false);
        SetGalSceneLayersVisible(true);

        if (!isInGame)
        {
            ShowMainMenu();
            return;
        }

        if (hudRoot != null)
        {
            hudRoot.SetActive(true);
        }

        ShowExplore();
    }

    private void SetGalSceneLayersVisible(bool visible)
    {
        if (backgroundRoot != null)
        {
            backgroundRoot.SetActive(visible);
        }

        if (backgroundWashRoot != null)
        {
            backgroundWashRoot.SetActive(visible);
        }
    }

    private void SetExternalSceneHudVisible(bool visible)
    {
        if (fbxHudRoot != null)
        {
            fbxHudRoot.SetActive(visible);
        }
    }

    private void ShowExplore()
    {
        FinishTypingForMenu();
        CancelAutoAdvance();
        isExploring = true;
        currentNode = null;
        isAwaitingChoice = false;
        isDialogueHidden = false;
        dialogueRoot.SetActive(false);
        exploreRoot.SetActive(true);
        if (portraitController != null)
        {
            portraitController.HideAll();
        }

        RebuildExploreButtons();
    }

    private void RebuildExploreButtons()
    {
        for (int i = exploreButtonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(exploreButtonContainer.GetChild(i).gameObject);
        }

        exploreTitleText.text = T("ui.explore.title", "选择调查地点");
        string activeScene = string.IsNullOrEmpty(currentBackgroundId) ? story.defaultBackground : currentBackgroundId;

        foreach (GalExplorePoint point in story.explorePoints)
        {
            bool hasCommand = point != null && point.commands != null && point.commands.Count > 0;
            if (point == null || string.IsNullOrEmpty(point.displayName) || (string.IsNullOrEmpty(point.nodeId) && string.IsNullOrEmpty(point.background) && !hasCommand))
            {
                continue;
            }

            string pointScene = string.IsNullOrEmpty(point.scene) ? FirstNonEmpty(point.background, activeScene) : point.scene;
            if (!string.IsNullOrEmpty(pointScene) && pointScene != activeScene)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(point.requiredFlag) && !HasFlag(point.requiredFlag) && !HasItem(point.requiredFlag))
            {
                continue;
            }

            CreateExploreHotspot(point);
        }
    }

    private void CreateExploreHotspot(GalExplorePoint point)
    {
        Button button = CreateButton(exploreButtonContainer, point.displayName, delegate { ChooseExplorePoint(point); }, out Text label);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        float width = point.width > 0f ? point.width : 170f;
        float height = point.height > 0f ? point.height : 48f;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;

        Image image = button.GetComponent<Image>();
        image.color = new Color(1f, 0.98f, 0.82f, 0.72f);

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 0.98f, 0.82f, 0.72f);
        colors.highlightedColor = new Color(0.75f, 0.94f, 0.78f, 0.95f);
        colors.pressedColor = new Color(0.48f, 0.68f, 0.52f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        label.fontSize = Mathf.RoundToInt(Mathf.Clamp(height * 0.42f, 16f, 22f));
        label.resizeTextMinSize = 12;
        label.color = new Color(0.06f, 0.06f, 0.055f, 1f);
    }

    private void ChooseExplorePoint(GalExplorePoint point)
    {
        if (isTransitioning)
        {
            return;
        }

        StartCoroutine(ChooseExplorePointRoutine(point));
    }

    private IEnumerator ChooseExplorePointRoutine(GalExplorePoint point)
    {
        if (point.commands != null && point.commands.Count > 0)
        {
            if (!ExecuteCommands(point.commands, out string jumpNode))
            {
                yield break;
            }

            if (!string.IsNullOrEmpty(jumpNode))
            {
                isExploring = false;
                exploreRoot.SetActive(false);
                PlayNode(jumpNode);
                yield break;
            }
        }

        string targetBackground = FirstNonEmpty(point.background, currentBackgroundId);
        if (string.IsNullOrEmpty(targetBackground))
        {
            targetBackground = story.defaultBackground;
        }
        isExploring = false;
        exploreRoot.SetActive(false);
        yield return TransitionToBackground(targetBackground, point.displayName);
        if (string.IsNullOrEmpty(point.nodeId))
        {
            isExploring = true;
            dialogueRoot.SetActive(false);
            exploreRoot.SetActive(true);
            RebuildExploreButtons();
            yield break;
        }

        PlayNode(point.nodeId);
    }

    private IEnumerator TransitionToBackground(string backgroundId, string caption)
    {
        if (string.IsNullOrEmpty(backgroundId) || backgroundId == currentBackgroundId || transitionImage == null)
        {
            SetBackground(backgroundId);
            yield break;
        }

        if (sceneTransitionRoutine != null)
        {
            StopCoroutine(sceneTransitionRoutine);
            sceneTransitionRoutine = null;
        }

        isTransitioning = true;
        transitionRoot.SetActive(true);
        transitionRoot.transform.SetAsLastSibling();
        transitionText.text = string.IsNullOrEmpty(caption) ? "切换场景" : caption;

        yield return FadeTransition(0f, 1f, 0.22f);
        SetBackground(backgroundId);
        yield return new WaitForSecondsRealtime(0.08f);
        yield return FadeTransition(1f, 0f, 0.28f);

        transitionRoot.SetActive(false);
        isTransitioning = false;
    }

    private IEnumerator FadeTransition(float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, Mathf.Clamp01(time / duration));
            SetTransitionAlpha(alpha);
            yield return null;
        }

        SetTransitionAlpha(to);
    }

    private void SetTransitionAlpha(float alpha)
    {
        Color imageColor = transitionImage.color;
        imageColor.a = alpha;
        transitionImage.color = imageColor;

        Color textColor = transitionText.color;
        textColor.a = alpha;
        transitionText.color = textColor;
    }

    private List<GalStoryChoice> GetAvailableChoices(GalStoryNode node)
    {
        List<GalStoryChoice> result = new List<GalStoryChoice>();
        if (node.choices == null)
        {
            return result;
        }

        foreach (GalStoryChoice choice in node.choices)
        {
            if (choice == null || string.IsNullOrEmpty(choice.text))
            {
                continue;
            }

            if (string.IsNullOrEmpty(choice.requiredFlag) || HasFlag(choice.requiredFlag) || HasItem(choice.requiredFlag))
            {
                result.Add(choice);
            }
        }

        return result;
    }

    private void EndStory()
    {
        isTyping = false;
        isAwaitingChoice = false;
        currentNode = null;
        currentLine = string.Empty;
        ClearChoices();
        ShowExplore();
    }

    private void StartTyping(string line)
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        CancelAutoAdvance();
        typingRoutine = StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = string.Empty;
        continueHintText.text = T("ui.dialogue.typing", "显示中...");

        if (isSkipMode && (settings.skipUnreadText || currentNodeWasReadBefore))
        {
            dialogueText.text = line;
            isTyping = false;
            typingRoutine = null;
            continueHintText.text = T("ui.dialogue.skipping", "跳过中");
            QueueAutoOrSkipAdvance();
            yield break;
        }

        float secondsPerCharacter = 1f / Mathf.Max(1f, settings.textSpeed);
        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            yield return new WaitForSecondsRealtime(secondsPerCharacter);
        }

        isTyping = false;
        typingRoutine = null;
        continueHintText.text = T("ui.dialogue.continue", "点击或空格继续");
        QueueAutoOrSkipAdvance();
    }

    private void FinishTypingImmediately()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        dialogueText.text = currentLine;
        isTyping = false;
        continueHintText.text = T("ui.dialogue.continue", "点击或空格继续");
        QueueAutoOrSkipAdvance();
    }

    private void QueueAutoOrSkipAdvance()
    {
        CancelAutoAdvance();

        if (!isInGame || isExploring || isAwaitingChoice || isDialogueHidden || currentNode == null)
        {
            return;
        }

        if (isSkipMode)
        {
            if (settings.skipUnreadText || currentNodeWasReadBefore)
            {
                autoRoutine = StartCoroutine(AutoContinueAfter(0.05f));
                return;
            }

            SetSkipMode(false);
        }

        if (isAutoMode)
        {
            autoRoutine = StartCoroutine(AutoContinueAfter(settings.autoDelay));
        }
    }

    private IEnumerator AutoContinueAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, delay));

        if (isInGame && !isExploring && !isAwaitingChoice && !isTyping && !isDialogueHidden && currentNode != null)
        {
            ContinueStory();
        }

        autoRoutine = null;
    }

    private void CancelAutoAdvance()
    {
        if (autoRoutine != null)
        {
            StopCoroutine(autoRoutine);
            autoRoutine = null;
        }
    }

    private void ToggleAutoMode()
    {
        SetAutoMode(!isAutoMode);
    }

    private void SetAutoMode(bool value)
    {
        isAutoMode = value;
        if (isAutoMode)
        {
            isSkipMode = false;
        }

        RefreshModeLabels();
        QueueAutoOrSkipAdvance();
        ShowToast(isAutoMode ? T("ui.toast.auto_on", "自动播放：开") : T("ui.toast.auto_off", "自动播放：关"));
    }

    private void ToggleSkipMode()
    {
        SetSkipMode(!isSkipMode);
    }

    private void SetSkipMode(bool value)
    {
        isSkipMode = value;
        if (isSkipMode)
        {
            isAutoMode = false;
        }

        RefreshModeLabels();
        QueueAutoOrSkipAdvance();
        ShowToast(isSkipMode ? T("ui.toast.skip_on", "跳过：开") : T("ui.toast.skip_off", "跳过：关"));
    }

    private void RefreshModeLabels()
    {
        if (autoButtonLabel != null)
        {
            autoButtonLabel.text = isAutoMode ? T("ui.hud.auto_on", "自动中") : T("ui.hud.auto", "自动");
        }

        if (skipButtonLabel != null)
        {
            skipButtonLabel.text = isSkipMode ? T("ui.hud.skip_on", "跳过中") : T("ui.hud.skip", "跳过");
        }
    }

    private void ToggleDialogueHidden()
    {
        if (!isInGame || isExploring || currentNode == null)
        {
            return;
        }

        if (isDialogueHidden)
        {
            ShowDialogueWindow();
        }
        else
        {
            HideDialogueWindow();
        }
    }

    private void HideDialogueWindow()
    {
        if (dialogueRoot == null)
        {
            return;
        }

        isDialogueHidden = true;
        dialogueRoot.SetActive(false);
        CancelAutoAdvance();
    }

    private void ShowDialogueWindow()
    {
        if (dialogueRoot == null || isExploring)
        {
            return;
        }

        isDialogueHidden = false;
        dialogueRoot.SetActive(true);
        QueueAutoOrSkipAdvance();
    }

    private void AddHistory(GalStoryNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.text))
        {
            return;
        }

        history.Add(new GalHistoryLine
        {
            speaker = string.IsNullOrEmpty(node.speaker) ? "旁白" : node.speaker,
            text = node.text
        });

        if (history.Count > 100)
        {
            history.RemoveAt(0);
        }
    }

    private void ShowHistory()
    {
        ShowHistory(false);
    }

    private void ShowHistory(bool returnToSettings)
    {
        if (historyRoot == null)
        {
            return;
        }

        previousOverlayPage = returnToSettings ? GalOverlayPage.Settings : GalOverlayPage.None;
        CloseOverlayPages(true);
        StringBuilder builder = new StringBuilder();
        if (history.Count == 0)
        {
            builder.Append(T("ui.history.empty", "暂无历史文本。"));
        }
        else
        {
            foreach (GalHistoryLine line in history)
            {
                builder.Append(line.speaker);
                builder.Append("：");
                builder.AppendLine(line.text);
                builder.AppendLine();
            }
        }

        historyText.text = builder.ToString();
        currentOverlayPage = GalOverlayPage.History;
        historyRoot.SetActive(true);
        RefreshOverlayNavigationButtons();
    }

    private void HideHistory()
    {
        ExitOverlayPages();
    }

    private void ReturnToPreviousOverlayPage()
    {
        if (previousOverlayPage == GalOverlayPage.Settings)
        {
            previousOverlayPage = GalOverlayPage.None;
            ShowSettings();
            return;
        }

        ExitOverlayPages();
    }

    private void ExitOverlayPages()
    {
        previousOverlayPage = GalOverlayPage.None;
        currentOverlayPage = GalOverlayPage.None;
        CloseOverlayPages(true);
        RefreshMenuState();
    }

    private void RefreshOverlayNavigationButtons()
    {
        bool canReturn = previousOverlayPage != GalOverlayPage.None;
        if (saveLoadBackButton != null)
        {
            saveLoadBackButton.gameObject.SetActive(canReturn && currentOverlayPage == GalOverlayPage.SaveLoad);
        }

        if (historyBackButton != null)
        {
            historyBackButton.gameObject.SetActive(canReturn && currentOverlayPage == GalOverlayPage.History);
        }

        if (portraitDebugBackButtonLabel != null)
        {
            portraitDebugBackButtonLabel.transform.parent.gameObject.SetActive(canReturn && currentOverlayPage == GalOverlayPage.PortraitDebug);
        }
    }

    private void CloseOverlayPages(bool includeSettings = true)
    {
        if (includeSettings && settingsRoot != null)
        {
            settingsRoot.SetActive(false);
            isSettingsOpen = false;
        }

        if (saveLoadRoot != null)
        {
            saveLoadRoot.SetActive(false);
            isSaveLoadOpen = false;
        }

        if (historyRoot != null)
        {
            historyRoot.SetActive(false);
        }

        if (portraitDebugRoot != null)
        {
            portraitDebugRoot.SetActive(false);
        }

        currentOverlayPage = GalOverlayPage.None;
        RefreshOverlayNavigationButtons();
    }

    private void LoadStory()
    {
        storyPath = Path.Combine(Application.streamingAssetsPath, StoryRelativePath);
        if (!File.Exists(storyPath))
        {
            story = CreateFallbackStory();
            Debug.LogWarning("Story JSON not found, using fallback story: " + storyPath);
        }
        else
        {
            string json = File.ReadAllText(storyPath, Encoding.UTF8);
            story = JsonUtility.FromJson<GalStoryFile>(json);
            if (story == null)
            {
                story = CreateFallbackStory();
            }
        }

        NormalizeStory();
        LoadTextTable();
        ApplyTextTable();
        storyLastWriteTimeUtc = GetFileWriteTimeUtc(storyPath);
        textTableLastWriteTimeUtc = GetFileWriteTimeUtc(textTablePath);
    }

    private void CheckHotReload()
    {
        hotReloadTimer += Time.unscaledDeltaTime;
        if (hotReloadTimer < 1f)
        {
            return;
        }

        hotReloadTimer = 0f;
        if (string.IsNullOrEmpty(storyPath))
        {
            return;
        }

        if (GetFileWriteTimeUtc(storyPath) == storyLastWriteTimeUtc && GetFileWriteTimeUtc(textTablePath) == textTableLastWriteTimeUtc)
        {
            return;
        }

        ReloadStoryFilesInPlace();
    }

    private void ReloadStoryFilesInPlace()
    {
        string nodeId = currentNodeId;
        bool wasInGame = isInGame;
        bool wasExploring = isExploring;

        LoadStory();
        RefreshLocalizedUi();
        if (portraitController != null)
        {
            portraitController.Configure(story.portraits);
        }

        if (!wasInGame)
        {
            menuTitleText.text = string.IsNullOrEmpty(story.title) ? "GAL Template" : story.title;
            SetBackground(string.IsNullOrEmpty(currentBackgroundId) ? story.defaultBackground : currentBackgroundId);
            ShowToast(T("ui.toast.reloaded_story", "已热重载剧本表。"));
            return;
        }

        if (wasExploring)
        {
            RebuildExploreButtons();
            ShowToast(T("ui.toast.reloaded_explore", "已热重载探索点。"));
            return;
        }

        if (!string.IsNullOrEmpty(nodeId) && nodesById.TryGetValue(nodeId, out GalStoryNode node))
        {
            currentNode = node;
            speakerText.text = string.IsNullOrEmpty(node.speaker) ? " " : node.speaker;
            currentLine = node.text ?? string.Empty;
            ApplyNodePortrait(node);
            if (isTyping)
            {
                FinishTypingImmediately();
            }
            else
            {
                dialogueText.text = currentLine;
            }

            if (isAwaitingChoice)
            {
                ShowChoices(GetAvailableChoices(node));
            }
        }

        ShowToast(T("ui.toast.reloaded_text", "已热重载文案。"));
    }

    private void NormalizeStory()
    {
        if (story.backgrounds == null)
        {
            story.backgrounds = new List<GalBackgroundEntry>();
        }

        if (story.nodes == null)
        {
            story.nodes = new List<GalStoryNode>();
        }

        if (story.explorePoints == null)
        {
            story.explorePoints = new List<GalExplorePoint>();
        }

        if (story.artProfiles == null)
        {
            story.artProfiles = new List<GalArtProfile>();
        }

        if (story.portraits == null)
        {
            story.portraits = new List<GalPortraitEntry>();
        }

        if (story.languages == null)
        {
            story.languages = new List<GalLanguageEntry>();
        }

        backgroundsById.Clear();
        foreach (GalBackgroundEntry background in story.backgrounds)
        {
            if (background != null && !string.IsNullOrEmpty(background.id))
            {
                backgroundsById[background.id] = background;
            }
        }

        explorePointsById.Clear();
        foreach (GalExplorePoint point in story.explorePoints)
        {
            if (point != null && !string.IsNullOrEmpty(point.id))
            {
                explorePointsById[point.id] = point;
            }
        }

        nodesById.Clear();
        foreach (GalStoryNode node in story.nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.id))
            {
                continue;
            }

            if (node.choices == null)
            {
                node.choices = new List<GalStoryChoice>();
            }

            if (node.commands == null)
            {
                node.commands = new List<GalStoryCommand>();
            }

            nodesById[node.id] = node;
        }
    }

    private void LoadTextTable()
    {
        rawTextRowsByKey.Clear();
        textEntriesByKey.Clear();
        string configuredPath = GetConfiguredTextTablePath();
        string relativePath = string.IsNullOrEmpty(configuredPath) ? DefaultTextTableRelativePath : configuredPath;
        textTablePath = Path.IsPathRooted(relativePath) ? relativePath : Path.Combine(Application.streamingAssetsPath, "GAL", relativePath);

        if (!File.Exists(textTablePath))
        {
            return;
        }

        List<string[]> rows = ParseCsv(File.ReadAllText(textTablePath, Encoding.UTF8));
        if (rows.Count == 0)
        {
            return;
        }

        Dictionary<string, int> headerIndexes = new Dictionary<string, int>();
        for (int i = 0; i < rows[0].Length; i++)
        {
            string header = rows[0][i].Trim().TrimStart('\uFEFF');
            if (!string.IsNullOrEmpty(header))
            {
                headerIndexes[header] = i;
            }
        }

        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];
            string key = GetCsvValue(row, headerIndexes, "key");
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            GalRawTextRow entry = new GalRawTextRow();
            entry.key = key;
            foreach (KeyValuePair<string, int> pair in headerIndexes)
            {
                if (pair.Key == "key" || pair.Key == "category" || pair.Key == "description" || pair.Key == "note")
                {
                    continue;
                }

                entry.values[pair.Key] = GetCsvValue(row, headerIndexes, pair.Key);
            }

            rawTextRowsByKey[key] = entry;
        }

        BuildLocalizedTextEntries();
    }

    private void BuildLocalizedTextEntries()
    {
        textEntriesByKey.Clear();
        foreach (GalRawTextRow row in rawTextRowsByKey.Values)
        {
            GalTextEntry entry = new GalTextEntry();
            entry.key = row.key;
            entry.speaker = GetLocalizedCell(row, "speaker");
            entry.text = GetLocalizedCell(row, "text");
            entry.portraitSlot = GetLocalizedCell(row, "portrait_slot");
            entry.portraitCharacter = GetLocalizedCell(row, "portrait_character");
            entry.portraitExpression = GetLocalizedCell(row, "portrait_expression");
            entry.portraitFacing = GetLocalizedCell(row, "portrait_facing");
            entry.portraitAnimation = GetLocalizedCell(row, "portrait_animation");
            entry.portraitPath = GetLocalizedCell(row, "portrait_path");
            textEntriesByKey[row.key] = entry;
        }
    }

    private string GetConfiguredTextTablePath()
    {
        string languageId = string.IsNullOrEmpty(settings.language) ? "zh-CN" : settings.language;
        if (story != null && story.languages != null)
        {
            foreach (GalLanguageEntry language in story.languages)
            {
                if (language != null && language.id == languageId && !string.IsNullOrEmpty(language.textTable))
                {
                    return language.textTable;
                }
            }
        }

        return story != null ? story.textTable : null;
    }

    private string GetLocalizedCell(GalRawTextRow row, string field)
    {
        string language = string.IsNullOrEmpty(settings.language) ? "zh-CN" : settings.language;
        string value = GetRawTextValue(row, language + "." + field);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        value = GetRawTextValue(row, "zh-CN." + field);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        return GetRawTextValue(row, field);
    }

    private static string GetRawTextValue(GalRawTextRow row, string column)
    {
        return row.values.TryGetValue(column, out string value) ? value : string.Empty;
    }

    private string T(string key, string fallback)
    {
        if (textEntriesByKey.TryGetValue(key, out GalTextEntry entry) && !string.IsNullOrEmpty(entry.text))
        {
            return entry.text;
        }

        return fallback;
    }

    private void ApplyTextTable()
    {
        if (textEntriesByKey.Count == 0)
        {
            return;
        }

        if (textEntriesByKey.TryGetValue("game.title", out GalTextEntry titleEntry) && !string.IsNullOrEmpty(titleEntry.text))
        {
            story.title = titleEntry.text;
        }

        foreach (GalStoryNode node in story.nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.id))
            {
                continue;
            }

            if (textEntriesByKey.TryGetValue("node." + node.id, out GalTextEntry nodeEntry))
            {
                if (!string.IsNullOrEmpty(nodeEntry.speaker))
                {
                    node.speaker = nodeEntry.speaker;
                }

                if (!string.IsNullOrEmpty(nodeEntry.text))
                {
                    node.text = nodeEntry.text;
                }

                if (!string.IsNullOrEmpty(nodeEntry.portraitSlot))
                {
                    node.portraitSlot = nodeEntry.portraitSlot;
                }

                if (!string.IsNullOrEmpty(nodeEntry.portraitCharacter))
                {
                    node.portraitCharacter = nodeEntry.portraitCharacter;
                }

                if (!string.IsNullOrEmpty(nodeEntry.portraitExpression))
                {
                    node.portraitExpression = nodeEntry.portraitExpression;
                }

                if (!string.IsNullOrEmpty(nodeEntry.portraitFacing))
                {
                    node.portraitFacing = nodeEntry.portraitFacing;
                }

                if (!string.IsNullOrEmpty(nodeEntry.portraitAnimation))
                {
                    node.portraitAnimation = nodeEntry.portraitAnimation;
                }

                if (!string.IsNullOrEmpty(nodeEntry.portraitPath))
                {
                    node.portraitPath = nodeEntry.portraitPath;
                }
            }

            if (node.choices == null)
            {
                continue;
            }

            for (int i = 0; i < node.choices.Count; i++)
            {
                GalStoryChoice choice = node.choices[i];
                if (choice == null)
                {
                    continue;
                }

                string choiceKey = string.IsNullOrEmpty(choice.id) ? "choice." + node.id + "." + (i + 1).ToString("00") : "choice." + choice.id;
                if (textEntriesByKey.TryGetValue(choiceKey, out GalTextEntry choiceEntry) && !string.IsNullOrEmpty(choiceEntry.text))
                {
                    choice.text = choiceEntry.text;
                }
            }
        }

        foreach (GalExplorePoint point in story.explorePoints)
        {
            if (point == null || string.IsNullOrEmpty(point.id))
            {
                continue;
            }

            if (textEntriesByKey.TryGetValue("explore." + point.id, out GalTextEntry pointEntry) && !string.IsNullOrEmpty(pointEntry.text))
            {
                point.displayName = pointEntry.text;
            }
        }
    }

    private void ApplyLanguageToRuntime()
    {
        BuildLocalizedTextEntries();
        ApplyTextTable();
        RefreshLocalizedUi();

        if (isExploring)
        {
            RebuildExploreButtons();
        }
        else if (currentNode != null)
        {
            if (nodesById.TryGetValue(currentNodeId, out GalStoryNode node))
            {
                currentNode = node;
                speakerText.text = string.IsNullOrEmpty(node.speaker) ? " " : node.speaker;
                currentLine = node.text ?? string.Empty;
                if (isTyping)
                {
                    FinishTypingImmediately();
                }
                else
                {
                    dialogueText.text = currentLine;
                }

                if (isAwaitingChoice)
                {
                    ShowChoices(GetAvailableChoices(node));
                }
            }
        }
    }

    private void RefreshLocalizedUi()
    {
        if (menuTitleText != null)
        {
            menuTitleText.text = string.IsNullOrEmpty(story.title) ? "GAL Template" : story.title;
        }

        SetText(newGameButtonLabel, T("ui.main.new_game", "新游戏"));
        SetText(mainMenuSettingsButtonLabel, T("ui.common.settings", "设置"));
        SetText(quitButtonLabel, T("ui.common.quit", "退出"));

        SetText(hudSaveButtonLabel, T("ui.hud.save", "存档"));
        SetText(hudLoadButtonLabel, T("ui.hud.load", "读档"));
        SetText(hudHideButtonLabel, T("ui.hud.hide", "隐藏"));
        SetText(hudHistoryButtonLabel, T("ui.hud.history", "历史"));
        SetText(hudSettingsButtonLabel, T("ui.common.settings", "设置"));
        SetText(hudDebugButtonLabel, T("ui.hud.portrait_debug", "立绘"));
        SetText(hudTitleButtonLabel, T("ui.hud.title", "标题"));
        RefreshModeLabels();

        SetText(historyTitleText, T("ui.history.title", "历史文本"));
        SetText(historyBackButtonLabel, T("ui.common.back", "返回"));
        SetText(historyExitButtonLabel, T("ui.common.exit", "退出"));

        SetText(saveLoadBackButtonLabel, T("ui.common.back", "返回"));
        SetText(saveLoadExitButtonLabel, T("ui.common.exit", "退出"));
        RefreshSaveLoadPanel();

        SetText(settingsTitleText, T("ui.common.settings", "设置"));
        SetText(settingsTextSpeedLabel, T("ui.settings.text_speed", "文本速度"));
        SetText(settingsAutoDelayLabel, T("ui.settings.auto_delay", "自动间隔"));
        SetText(settingsVolumeLabel, T("ui.settings.volume", "主音量"));
        SetText(settingsFullscreenLabel, T("ui.settings.fullscreen", "全屏显示"));
        SetText(settingsSkipUnreadLabel, T("ui.settings.skip_unread", "允许跳过未读文本"));
        SetText(languageValueText, GetLanguageButtonText());
        SetText(settingsSavePanelButtonLabel, T("ui.settings.open_save", "打开存档"));
        SetText(settingsLoadPanelButtonLabel, T("ui.settings.open_load", "打开读档"));
        SetText(settingsHistoryButtonLabel, T("ui.settings.open_history", "查看历史"));
        SetText(settingsReloadButtonLabel, T("ui.settings.reload_text", "重载文案"));
        SetText(settingsDeleteButtonLabel, T("ui.settings.delete_save", "删除存档"));
        SetText(settingsDebugButtonLabel, T("ui.settings.portrait_debug", "立绘调试"));
        SetText(settingsExitButtonLabel, T("ui.common.exit", "退出"));
        RefreshPortraitDebugLabels();
        RefreshExternalSceneHudLabels();

        RefreshMenuState();
    }

    private void RefreshExternalSceneHudLabels()
    {
        SetText(fbxBackButtonLabel, T("ui.common.back", "Back"));
        SetText(fbxSaveButtonLabel, T("ui.hud.save", "Save"));
        SetText(fbxLoadButtonLabel, T("ui.hud.load", "Load"));
        SetText(fbxHistoryButtonLabel, T("ui.hud.history", "History"));
        SetText(fbxSettingsButtonLabel, T("ui.common.settings", "Settings"));
        SetText(fbxTitleButtonLabel, T("ui.hud.title", "Title"));
    }

    private static void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static List<string[]> ParseCsv(string content)
    {
        List<string[]> rows = new List<string[]>();
        List<string> row = new List<string>();
        StringBuilder cell = new StringBuilder();
        bool quoted = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    cell.Append(c);
                }

                continue;
            }

            if (c == '"')
            {
                quoted = true;
            }
            else if (c == ',')
            {
                row.Add(cell.ToString());
                cell.Length = 0;
            }
            else if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(cell.ToString());
                cell.Length = 0;
                if (row.Count > 1 || !string.IsNullOrEmpty(row[0]))
                {
                    rows.Add(row.ToArray());
                }

                row.Clear();
            }
            else
            {
                cell.Append(c);
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row.ToArray());
        }

        return rows;
    }

    private static string GetCsvValue(string[] row, Dictionary<string, int> headerIndexes, string column)
    {
        if (!headerIndexes.TryGetValue(column, out int index) || index < 0 || index >= row.Length)
        {
            return string.Empty;
        }

        return row[index].Replace("\\n", "\n");
    }

    private static DateTime GetFileWriteTimeUtc(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return DateTime.MinValue;
        }

        return File.GetLastWriteTimeUtc(path);
    }

    private GalStoryFile CreateFallbackStory()
    {
        GalStoryFile fallback = new GalStoryFile();
        fallback.title = "GAL Template";
        fallback.startNode = "start_001";
        fallback.artProfiles.Add(new GalArtProfile { id = "default", displayName = "默认线稿", backgroundFolder = "Backgrounds" });
        fallback.languages.Add(new GalLanguageEntry { id = "zh-CN", displayName = "简体中文", tablePath = "Localization/zh-CN.json" });
        fallback.nodes.Add(new GalStoryNode
        {
            id = "start_001",
            speaker = "旁白",
            text = "没有找到 gal_story.json，所以这里显示了内置备用文本。",
            nextId = "start_002"
        });
        fallback.nodes.Add(new GalStoryNode
        {
            id = "start_002",
            speaker = "系统",
            text = "请编辑 Assets/StreamingAssets/GAL/gal_story.json 来替换剧情。",
            commands = new List<GalStoryCommand>
            {
                new GalStoryCommand { command = "show_explore" }
            }
        });
        fallback.explorePoints.Add(new GalExplorePoint
        {
            id = "fallback_point",
            displayName = "示例地点",
            nodeId = "start_001",
            x = 0.5f,
            y = 0.5f
        });
        return fallback;
    }

    private void SetBackground(string backgroundId)
    {
        if (string.IsNullOrEmpty(backgroundId))
        {
            return;
        }

        currentBackgroundId = backgroundId;

        if (!backgroundsById.TryGetValue(backgroundId, out GalBackgroundEntry background))
        {
            Debug.LogWarning("Missing background id: " + backgroundId);
            return;
        }

        Texture2D texture = LoadBackgroundTexture(background);
        if (texture == null)
        {
            return;
        }

        backgroundImage.texture = texture;
        backgroundAspect.aspectRatio = texture.width / (float)texture.height;
        if (exploreButtonAreaAspect != null)
        {
            exploreButtonAreaAspect.aspectRatio = backgroundAspect.aspectRatio;
        }
    }

    private Texture2D LoadBackgroundTexture(GalBackgroundEntry background)
    {
        if (textureCache.TryGetValue(background.id, out Texture2D cachedTexture))
        {
            return cachedTexture;
        }

        string path = background.path;
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(Application.streamingAssetsPath, "GAL", path);
        }

        if (!File.Exists(path))
        {
            Debug.LogWarning("Missing background file: " + path);
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Destroy(texture);
            return null;
        }

        texture.name = background.id;
        textureCache[background.id] = texture;
        return texture;
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("GAL Template Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildBackground(canvasObject.transform);
        BuildPortraitLayer(canvasObject.transform);
        BuildMainMenu(canvasObject.transform);
        BuildDialogue(canvasObject.transform);
        BuildExplore(canvasObject.transform);
        BuildHud(canvasObject.transform);
        BuildExternalSceneHud(canvasObject.transform);
        BuildTransition(canvasObject.transform);
        BuildToast(canvasObject.transform);
        BuildHistory(canvasObject.transform);
        BuildSaveLoadPanel(canvasObject.transform);
        BuildSettings(canvasObject.transform);
        BuildPortraitDebug(canvasObject.transform);
    }

    private void BuildBackground(Transform parent)
    {
        GameObject frame = CreateUiObject("Background Frame", parent);
        backgroundRoot = frame;
        Stretch(frame.GetComponent<RectTransform>());
        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = new Color(0.96f, 0.95f, 0.92f, 1f);

        GameObject imageObject = CreateUiObject("Story Background", frame.transform);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = new Vector2(1920f, 1080f);
        backgroundImage = imageObject.AddComponent<RawImage>();
        backgroundImage.color = Color.white;
        backgroundAspect = imageObject.AddComponent<AspectRatioFitter>();
        backgroundAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        backgroundAspect.aspectRatio = 16f / 9f;

        GameObject wash = CreateUiObject("Subtle Wash", parent);
        backgroundWashRoot = wash;
        Stretch(wash.GetComponent<RectTransform>());
        Image washImage = wash.AddComponent<Image>();
        washImage.color = new Color(1f, 0.98f, 0.92f, 0.18f);
    }

    private void BuildPortraitLayer(Transform parent)
    {
        GameObject layer = CreateUiObject("Portrait Layer", parent);
        portraitController = layer.AddComponent<GalPortraitController>();
        portraitController.Initialize(GetFont(24));
        portraitController.Configure(story.portraits);
    }

    private void BuildMainMenu(Transform parent)
    {
        mainMenuRoot = CreateUiObject("Main Menu", parent);
        Stretch(mainMenuRoot.GetComponent<RectTransform>());

        GameObject panel = CreateUiObject("Menu Band", mainMenuRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0.42f, 1f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.055f, 0.055f, 0.74f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(72, 72, 92, 72);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        menuTitleText = CreateText(panel.transform, story.title, 42, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        LayoutElement titleLayout = menuTitleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 128f;

        primaryActionButton = CreateButton(panel.transform, T("ui.main.start", "开始游戏"), OnPrimaryAction, out primaryActionLabel);
        AddButtonLayout(primaryActionButton, 56f);
        newGameButton = CreateButton(panel.transform, T("ui.main.new_game", "新游戏"), StartNewGame, out newGameButtonLabel);
        AddButtonLayout(newGameButton, 50f);
        Button settingsButton = CreateButton(panel.transform, T("ui.common.settings", "设置"), ShowSettings, out mainMenuSettingsButtonLabel);
        AddButtonLayout(settingsButton, 50f);
        Button quitButton = CreateButton(panel.transform, T("ui.common.quit", "退出"), QuitGame, out quitButtonLabel);
        AddButtonLayout(quitButton, 50f);

        saveInfoText = CreateText(panel.transform, string.Empty, 18, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.88f, 0.88f, 0.82f, 1f));
        saveInfoText.resizeTextForBestFit = false;
        saveInfoText.horizontalOverflow = HorizontalWrapMode.Wrap;
        saveInfoText.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement infoLayout = saveInfoText.gameObject.AddComponent<LayoutElement>();
        infoLayout.preferredHeight = 100f;
    }

    private void BuildDialogue(Transform parent)
    {
        dialogueRoot = CreateUiObject("Dialogue", parent);
        RectTransform rootRect = dialogueRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.offsetMin = new Vector2(64f, 44f);
        rootRect.offsetMax = new Vector2(-64f, 284f);

        Image panelImage = dialogueRoot.AddComponent<Image>();
        panelImage.color = new Color(0.98f, 0.97f, 0.93f, 0.94f);

        speakerText = CreateText(dialogueRoot.transform, string.Empty, 26, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.1f, 0.1f, 0.1f, 1f));
        RectTransform speakerRect = speakerText.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(0.36f, 1f);
        speakerRect.pivot = new Vector2(0f, 1f);
        speakerRect.offsetMin = new Vector2(36f, -70f);
        speakerRect.offsetMax = new Vector2(0f, -18f);

        dialogueText = CreateText(dialogueRoot.transform, string.Empty, 30, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.08f, 0.08f, 0.08f, 1f));
        dialogueText.resizeTextForBestFit = false;
        dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dialogueText.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform lineRect = dialogueText.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0f, 0f);
        lineRect.anchorMax = new Vector2(1f, 1f);
        lineRect.offsetMin = new Vector2(36f, 40f);
        lineRect.offsetMax = new Vector2(-420f, -78f);

        continueHintText = CreateText(dialogueRoot.transform, T("ui.dialogue.continue", "点击或空格继续"), 18, FontStyle.Normal, TextAnchor.LowerRight, new Color(0.22f, 0.22f, 0.22f, 1f));
        RectTransform hintRect = continueHintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.sizeDelta = new Vector2(260f, 36f);
        hintRect.anchoredPosition = new Vector2(-32f, 22f);

        GameObject choices = CreateUiObject("Choices", dialogueRoot.transform);
        choiceContainer = choices.transform;
        RectTransform choiceRect = choices.GetComponent<RectTransform>();
        choiceRect.anchorMin = new Vector2(1f, 0f);
        choiceRect.anchorMax = new Vector2(1f, 1f);
        choiceRect.pivot = new Vector2(1f, 0.5f);
        choiceRect.offsetMin = new Vector2(-382f, 52f);
        choiceRect.offsetMax = new Vector2(-30f, -36f);
        VerticalLayoutGroup choiceLayout = choices.AddComponent<VerticalLayoutGroup>();
        choiceLayout.spacing = 10f;
        choiceLayout.childControlWidth = true;
        choiceLayout.childControlHeight = false;
        choiceLayout.childForceExpandHeight = false;

        dialogueRoot.SetActive(false);
    }

    private void BuildExplore(Transform parent)
    {
        exploreRoot = CreateUiObject("Explore", parent);
        Stretch(exploreRoot.GetComponent<RectTransform>());

        GameObject titleBackground = CreateUiObject("Explore Title Background", exploreRoot.transform);
        RectTransform backgroundRect = titleBackground.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 1f);
        backgroundRect.anchorMax = new Vector2(0.5f, 1f);
        backgroundRect.pivot = new Vector2(0.5f, 1f);
        backgroundRect.sizeDelta = new Vector2(420f, 54f);
        backgroundRect.anchoredPosition = new Vector2(0f, -92f);
        Image titleImage = titleBackground.AddComponent<Image>();
        titleImage.color = new Color(0.98f, 0.97f, 0.93f, 0.88f);

        exploreTitleText = CreateText(exploreRoot.transform, "选择调查地点", 28, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.08f, 0.08f, 0.08f, 1f));
        RectTransform titleRect = exploreTitleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(420f, 54f);
        titleRect.anchoredPosition = new Vector2(0f, -92f);

        GameObject buttons = CreateUiObject("Explore Points", exploreRoot.transform);
        exploreButtonContainer = buttons.transform;
        exploreButtonAreaRect = buttons.GetComponent<RectTransform>();
        exploreButtonAreaRect.anchorMin = new Vector2(0.5f, 0.5f);
        exploreButtonAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        exploreButtonAreaRect.pivot = new Vector2(0.5f, 0.5f);
        exploreButtonAreaRect.sizeDelta = new Vector2(1920f, 1080f);
        exploreButtonAreaAspect = buttons.AddComponent<AspectRatioFitter>();
        exploreButtonAreaAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        exploreButtonAreaAspect.aspectRatio = 16f / 9f;
        exploreRoot.SetActive(false);
    }

    private void BuildTransition(Transform parent)
    {
        transitionRoot = CreateUiObject("Scene Transition", parent);
        Stretch(transitionRoot.GetComponent<RectTransform>());
        transitionImage = transitionRoot.AddComponent<Image>();
        transitionImage.color = new Color(0.04f, 0.04f, 0.035f, 0f);

        transitionText = CreateText(transitionRoot.transform, string.Empty, 30, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.98f, 0.96f, 0.86f, 0f));
        RectTransform textRect = transitionText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(520f, 70f);
        textRect.anchoredPosition = Vector2.zero;

        transitionRoot.SetActive(false);
    }

    private void BuildHud(Transform parent)
    {
        hudRoot = CreateUiObject("HUD", parent);
        RectTransform hudRect = hudRoot.GetComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(1f, 1f);
        hudRect.anchorMax = new Vector2(1f, 1f);
        hudRect.pivot = new Vector2(1f, 1f);
        hudRect.sizeDelta = new Vector2(1100f, 56f);
        hudRect.anchoredPosition = new Vector2(-32f, -28f);

        HorizontalLayoutGroup layout = hudRoot.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleRight;

        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.save", "存档"), ShowSavePanel, out hudSaveButtonLabel), 46f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.load", "读档"), ShowLoadPanel, out hudLoadButtonLabel), 46f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.auto", "自动"), ToggleAutoMode, out autoButtonLabel), 46f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.skip", "跳过"), ToggleSkipMode, out skipButtonLabel), 46f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.hide", "隐藏"), ToggleDialogueHidden, out hudHideButtonLabel), 46f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.history", "历史"), ShowHistory, out hudHistoryButtonLabel), 46f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.common.settings", "设置"), ShowSettings, out hudSettingsButtonLabel), 46f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.portrait_debug", "立绘"), ShowPortraitDebug, out hudDebugButtonLabel), 46f, 104f);
        AddButtonLayout(CreateButton(hudRoot.transform, T("ui.hud.title", "标题"), ShowMainMenu, out hudTitleButtonLabel), 46f, 104f);
        hudRoot.SetActive(false);
    }

    private void BuildExternalSceneHud(Transform parent)
    {
        fbxHudRoot = CreateUiObject("External Scene HUD", parent);
        RectTransform hudRect = fbxHudRoot.GetComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(1f, 1f);
        hudRect.anchorMax = new Vector2(1f, 1f);
        hudRect.pivot = new Vector2(1f, 1f);
        hudRect.sizeDelta = new Vector2(720f, 58f);
        hudRect.anchoredPosition = new Vector2(-32f, -28f);

        Image background = fbxHudRoot.AddComponent<Image>();
        background.color = new Color(0.03f, 0.03f, 0.05f, 0.42f);

        HorizontalLayoutGroup layout = fbxHudRoot.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 6, 6);
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleRight;

        AddButtonLayout(CreateButton(fbxHudRoot.transform, T("ui.common.back", "Back"), ExitFbxScene, out fbxBackButtonLabel), 46f, 96f);
        AddButtonLayout(CreateButton(fbxHudRoot.transform, T("ui.hud.save", "Save"), ShowSavePanel, out fbxSaveButtonLabel), 46f, 96f);
        AddButtonLayout(CreateButton(fbxHudRoot.transform, T("ui.hud.load", "Load"), ShowLoadPanel, out fbxLoadButtonLabel), 46f, 96f);
        AddButtonLayout(CreateButton(fbxHudRoot.transform, T("ui.hud.history", "History"), ShowHistory, out fbxHistoryButtonLabel), 46f, 112f);
        AddButtonLayout(CreateButton(fbxHudRoot.transform, T("ui.common.settings", "Settings"), ShowSettings, out fbxSettingsButtonLabel), 46f, 112f);
        AddButtonLayout(CreateButton(fbxHudRoot.transform, T("ui.hud.title", "Title"), ShowMainMenuFromExternalScene, out fbxTitleButtonLabel), 46f, 96f);
        fbxHudRoot.SetActive(false);
    }

    private void BuildToast(Transform parent)
    {
        toastRoot = CreateUiObject("Toast", parent);
        RectTransform toastRect = toastRoot.GetComponent<RectTransform>();
        toastRect.anchorMin = new Vector2(0.5f, 1f);
        toastRect.anchorMax = new Vector2(0.5f, 1f);
        toastRect.pivot = new Vector2(0.5f, 1f);
        toastRect.sizeDelta = new Vector2(680f, 46f);
        toastRect.anchoredPosition = new Vector2(0f, -32f);
        Image toastBg = toastRoot.AddComponent<Image>();
        toastBg.color = new Color(0.06f, 0.06f, 0.06f, 0.76f);

        toastText = CreateText(toastRoot.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        Stretch(toastText.GetComponent<RectTransform>());
        toastRoot.transform.SetAsLastSibling();
        toastRoot.SetActive(false);
    }

    private void BuildHistory(Transform parent)
    {
        historyRoot = CreateUiObject("History Overlay", parent);
        Stretch(historyRoot.GetComponent<RectTransform>());
        Image overlay = historyRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.58f);

        GameObject panel = CreateUiObject("History Panel", historyRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(980f, 700f);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.98f, 0.97f, 0.93f, 0.98f);

        historyTitleText = CreateText(panel.transform, T("ui.history.title", "历史文本"), 30, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.08f, 0.08f, 0.08f, 1f));
        RectTransform titleRect = historyTitleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(34f, -72f);
        titleRect.offsetMax = new Vector2(-34f, -24f);

        GameObject scrollObject = CreateUiObject("History Scroll", panel.transform);
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(34f, 84f);
        scrollRectTransform.offsetMax = new Vector2(-34f, -92f);
        Image scrollImage = scrollObject.AddComponent<Image>();
        scrollImage.color = new Color(1f, 1f, 1f, 0.2f);
        Mask scrollMask = scrollObject.AddComponent<Mask>();
        scrollMask.showMaskGraphic = false;
        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 34f;

        GameObject contentObject = CreateUiObject("History Content", scrollObject.transform);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(0f, 0f);
        contentRect.offsetMax = new Vector2(0f, 0f);
        VerticalLayoutGroup contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(14, 14, 14, 14);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        ContentSizeFitter contentFitter = contentObject.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        historyText = CreateText(contentObject.transform, string.Empty, 23, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.08f, 0.08f, 0.08f, 1f));
        historyText.resizeTextForBestFit = false;
        historyText.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement historyLayout = historyText.gameObject.AddComponent<LayoutElement>();
        historyLayout.minHeight = 520f;
        scrollRect.content = contentRect;

        historyBackButton = CreateButton(panel.transform, T("ui.common.back", "返回"), ReturnToPreviousOverlayPage, out historyBackButtonLabel);
        RectTransform backRect = historyBackButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(1f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(1f, 0f);
        backRect.sizeDelta = new Vector2(140f, 48f);
        backRect.anchoredPosition = new Vector2(-190f, 28f);

        Button closeButton = CreateButton(panel.transform, T("ui.common.exit", "退出"), ExitOverlayPages, out historyExitButtonLabel);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = new Vector2(1f, 0f);
        closeRect.pivot = new Vector2(1f, 0f);
        closeRect.sizeDelta = new Vector2(140f, 48f);
        closeRect.anchoredPosition = new Vector2(-34f, 28f);

        historyRoot.SetActive(false);
    }

    private void BuildSaveLoadPanel(Transform parent)
    {
        saveLoadRoot = CreateUiObject("Save Load Overlay", parent);
        Stretch(saveLoadRoot.GetComponent<RectTransform>());
        Image overlay = saveLoadRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.56f);

        GameObject panel = CreateUiObject("Save Load Panel", saveLoadRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(980f, 720f);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.98f, 0.97f, 0.93f, 0.98f);

        saveLoadTitleText = CreateText(panel.transform, T("ui.save.title", "存档"), 32, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.08f, 0.08f, 0.08f, 1f));
        RectTransform titleRect = saveLoadTitleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(34f, -76f);
        titleRect.offsetMax = new Vector2(-34f, -24f);

        GameObject slots = CreateUiObject("Save Slots", panel.transform);
        saveSlotContainer = slots.transform;
        RectTransform slotsRect = slots.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0f, 0f);
        slotsRect.anchorMax = new Vector2(1f, 1f);
        slotsRect.offsetMin = new Vector2(34f, 92f);
        slotsRect.offsetMax = new Vector2(-34f, -92f);
        VerticalLayoutGroup slotLayout = slots.AddComponent<VerticalLayoutGroup>();
        slotLayout.spacing = 10f;
        slotLayout.childControlWidth = true;
        slotLayout.childControlHeight = false;
        slotLayout.childForceExpandHeight = false;

        saveLoadBackButton = CreateButton(panel.transform, T("ui.common.back", "返回"), ReturnToPreviousOverlayPage, out saveLoadBackButtonLabel);
        RectTransform backRect = saveLoadBackButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(1f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(1f, 0f);
        backRect.sizeDelta = new Vector2(140f, 48f);
        backRect.anchoredPosition = new Vector2(-190f, 28f);

        Button closeButton = CreateButton(panel.transform, T("ui.common.exit", "退出"), ExitOverlayPages, out saveLoadExitButtonLabel);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = new Vector2(1f, 0f);
        closeRect.pivot = new Vector2(1f, 0f);
        closeRect.sizeDelta = new Vector2(140f, 48f);
        closeRect.anchoredPosition = new Vector2(-34f, 28f);

        saveLoadRoot.SetActive(false);
    }

    private void ShowSavePanel()
    {
        ShowSavePanel(false);
    }

    private void ShowSavePanel(bool returnToSettings)
    {
        if (!isInGame)
        {
            ShowToast(T("ui.toast.cannot_save_on_title", "标题界面不能存档。"));
            return;
        }

        saveLoadPanelForSaving = true;
        previousOverlayPage = returnToSettings ? GalOverlayPage.Settings : GalOverlayPage.None;
        ShowSaveLoadPanel();
    }

    private void ShowLoadPanel()
    {
        ShowLoadPanel(false);
    }

    private void ShowLoadPanel(bool returnToSettings)
    {
        saveLoadPanelForSaving = false;
        previousOverlayPage = returnToSettings ? GalOverlayPage.Settings : GalOverlayPage.None;
        ShowSaveLoadPanel();
    }

    private void ShowSaveLoadPanel()
    {
        CloseOverlayPages(true);
        isSaveLoadOpen = true;
        currentOverlayPage = GalOverlayPage.SaveLoad;
        saveLoadRoot.SetActive(true);
        RefreshSaveLoadPanel();
        RefreshOverlayNavigationButtons();
    }

    private void HideSaveLoadPanel()
    {
        ExitOverlayPages();
    }

    private void RefreshSaveLoadPanel()
    {
        if (saveSlotContainer == null || saveLoadRoot == null || !saveLoadRoot.activeSelf)
        {
            return;
        }

        saveLoadTitleText.text = saveLoadPanelForSaving ? T("ui.save.title", "存档") : T("ui.load.title", "读档");

        for (int i = saveSlotContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(saveSlotContainer.GetChild(i).gameObject);
        }

        for (int slot = 1; slot <= SaveSlotCount; slot++)
        {
            CreateSaveSlotRow(slot);
        }
    }

    private void CreateSaveSlotRow(int slot)
    {
        GameObject row = CreateUiObject("Save Slot " + slot, saveSlotContainer);
        LayoutElement rowElement = row.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 76f;
        Image rowImage = row.AddComponent<Image>();
        rowImage.color = new Color(1f, 0.99f, 0.95f, 0.96f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 12, 12);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        Text infoText = CreateText(row.transform, GetSlotDescription(slot), 20, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.08f, 0.08f, 0.08f, 1f));
        infoText.resizeTextForBestFit = false;
        LayoutElement infoLayout = infoText.gameObject.AddComponent<LayoutElement>();
        infoLayout.preferredWidth = 520f;

        bool hasSave = File.Exists(GetSavePath(slot));
        string actionLabel = saveLoadPanelForSaving ? T("ui.save.action", "保存") : T("ui.load.action", "读取");
        Button actionButton = CreateButton(row.transform, actionLabel, delegate
        {
            if (saveLoadPanelForSaving)
            {
                SaveGameToSlot(slot);
            }
            else
            {
                if (!LoadGameFromSlot(slot))
                {
                    ShowToast(string.Format(T("ui.toast.empty_slot", "槽位 {0} 没有存档。"), slot));
                }
            }
        }, out _);
        actionButton.interactable = saveLoadPanelForSaving || hasSave;
        AddButtonLayout(actionButton, 46f, 110f);

        Button deleteButton = CreateButton(row.transform, T("ui.common.delete", "删除"), delegate { DeleteSaveSlot(slot); }, out _);
        deleteButton.interactable = hasSave;
        AddButtonLayout(deleteButton, 46f, 110f);
    }

    private string GetSlotDescription(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path))
        {
            return string.Format(T("ui.save.slot_empty", "槽位 {0}  空"), slot);
        }

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            GalTemplateSaveData data = JsonUtility.FromJson<GalTemplateSaveData>(json);
            string savedAt = data != null && !string.IsNullOrEmpty(data.savedAt) ? data.savedAt : File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm");
            string location = data != null && data.isExploring ? "探索中" : "对话中";
            string node = data != null ? data.currentNodeId : "unknown";
            return string.Format(T("ui.save.slot_filled", "槽位 {0}  {1}  {2}  {3}"), slot, savedAt, location, node);
        }
        catch
        {
            return string.Format(T("ui.save.slot_broken", "槽位 {0}  存档损坏"), slot);
        }
    }

    private void BuildSettings(Transform parent)
    {
        settingsRoot = CreateUiObject("Settings Overlay", parent);
        Stretch(settingsRoot.GetComponent<RectTransform>());
        Image overlay = settingsRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.52f);

        GameObject panel = CreateUiObject("Settings Panel", settingsRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(920f, 640f);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.97f, 0.96f, 0.92f, 0.98f);

        settingsTitleText = CreateText(panel.transform, T("ui.common.settings", "设置"), 34, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.08f, 0.08f, 0.08f, 1f));
        RectTransform titleRect = settingsTitleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(38f, -72f);
        titleRect.offsetMax = new Vector2(-38f, -24f);

        textSpeedSlider = CreateSettingsSlider(panel.transform, T("ui.settings.text_speed", "文本速度"), new Vector2(240f, -140f), 12f, 80f, settings.textSpeed, true, out settingsTextSpeedLabel, out textSpeedValueText);
        textSpeedSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.textSpeed = Mathf.Round(value);
            textSpeedValueText.text = settings.textSpeed.ToString("0");
            SaveSettings();
        });

        autoDelaySlider = CreateSettingsSlider(panel.transform, T("ui.settings.auto_delay", "自动间隔"), new Vector2(240f, -225f), 0.3f, 3f, settings.autoDelay, false, out settingsAutoDelayLabel, out autoDelayValueText);
        autoDelaySlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.autoDelay = value;
            autoDelayValueText.text = value.ToString("0.0") + "s";
            SaveSettings();
        });

        volumeSlider = CreateSettingsSlider(panel.transform, T("ui.settings.volume", "主音量"), new Vector2(240f, -310f), 0f, 1f, settings.masterVolume, false, out settingsVolumeLabel, out volumeValueText);
        volumeSlider.onValueChanged.AddListener(delegate(float value)
        {
            settings.masterVolume = value;
            volumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
            ApplySettings();
            SaveSettings();
        });

        fullscreenToggle = CreateSettingsToggle(panel.transform, T("ui.settings.fullscreen", "全屏显示"), new Vector2(570f, -142f), settings.fullscreen, out settingsFullscreenLabel);
        fullscreenToggle.onValueChanged.AddListener(delegate(bool value)
        {
            settings.fullscreen = value;
            ApplySettings();
            SaveSettings();
        });

        skipUnreadToggle = CreateSettingsToggle(panel.transform, T("ui.settings.skip_unread", "允许跳过未读文本"), new Vector2(570f, -202f), settings.skipUnreadText, out settingsSkipUnreadLabel);
        skipUnreadToggle.onValueChanged.AddListener(delegate(bool value)
        {
            settings.skipUnreadText = value;
            SaveSettings();
        });

        Button languageButton = CreateSettingsButton(panel.transform, GetLanguageButtonText(), new Vector2(655f, -275f), new Vector2(340f, 54f), CycleLanguage, out languageValueText);
        settingsSavePanelButton = CreateSettingsButton(panel.transform, T("ui.settings.open_save", "打开存档"), new Vector2(575f, -350f), new Vector2(160f, 50f), ShowSavePanelFromSettings, out settingsSavePanelButtonLabel);
        Button loadPanelButton = CreateSettingsButton(panel.transform, T("ui.settings.open_load", "打开读档"), new Vector2(750f, -350f), new Vector2(160f, 50f), ShowLoadPanelFromSettings, out settingsLoadPanelButtonLabel);
        Button historyButton = CreateSettingsButton(panel.transform, T("ui.settings.open_history", "查看历史"), new Vector2(575f, -410f), new Vector2(160f, 50f), ShowHistoryFromSettings, out settingsHistoryButtonLabel);
        Button reloadTextButton = CreateSettingsButton(panel.transform, T("ui.settings.reload_text", "重载文案"), new Vector2(750f, -410f), new Vector2(160f, 50f), ReloadStoryFilesInPlace, out settingsReloadButtonLabel);
        Button deleteButton = CreateSettingsButton(panel.transform, T("ui.settings.delete_save", "删除存档"), new Vector2(575f, -470f), new Vector2(160f, 50f), DeleteSave, out settingsDeleteButtonLabel);
        Button debugButton = CreateSettingsButton(panel.transform, T("ui.settings.portrait_debug", "立绘调试"), new Vector2(750f, -470f), new Vector2(160f, 50f), ShowPortraitDebugFromSettings, out settingsDebugButtonLabel);

        Button closeButton = CreateButton(panel.transform, T("ui.common.exit", "退出"), ExitOverlayPages, out settingsExitButtonLabel);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = new Vector2(1f, 0f);
        closeRect.pivot = new Vector2(1f, 0f);
        closeRect.sizeDelta = new Vector2(140f, 50f);
        closeRect.anchoredPosition = new Vector2(-38f, 28f);

        settingsRoot.SetActive(false);
    }

    private void BuildPortraitDebug(Transform parent)
    {
        portraitDebugRoot = CreateUiObject("Portrait Debug Overlay", parent);
        Stretch(portraitDebugRoot.GetComponent<RectTransform>());
        Image overlay = portraitDebugRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.52f);

        GameObject panel = CreateUiObject("Portrait Debug Panel", portraitDebugRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(760f, 560f);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.97f, 0.96f, 0.92f, 0.98f);

        portraitDebugTitleText = CreateText(panel.transform, T("ui.portrait_debug.title", "立绘调试"), 32, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.08f, 0.08f, 0.08f, 1f));
        RectTransform titleRect = portraitDebugTitleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(38f, -72f);
        titleRect.offsetMax = new Vector2(-38f, -24f);

        CreateSettingsButton(panel.transform, GetPortraitDebugSlotText(), new Vector2(220f, -140f), new Vector2(320f, 52f), CycleDebugPortraitSlot, out portraitDebugSlotLabel);
        CreateSettingsButton(panel.transform, GetPortraitDebugCharacterText(), new Vector2(540f, -140f), new Vector2(320f, 52f), CycleDebugPortraitCharacter, out portraitDebugCharacterLabel);
        CreateSettingsButton(panel.transform, GetPortraitDebugExpressionText(), new Vector2(220f, -215f), new Vector2(320f, 52f), CycleDebugPortraitExpression, out portraitDebugExpressionLabel);
        CreateSettingsButton(panel.transform, GetPortraitDebugFacingText(), new Vector2(540f, -215f), new Vector2(320f, 52f), CycleDebugPortraitFacing, out portraitDebugFacingLabel);
        CreateSettingsButton(panel.transform, GetPortraitDebugAnimationText(), new Vector2(220f, -290f), new Vector2(320f, 52f), CycleDebugPortraitAnimation, out portraitDebugAnimationLabel);
        CreateSettingsButton(panel.transform, T("ui.portrait_debug.show", "显示/刷新"), new Vector2(540f, -290f), new Vector2(320f, 52f), ShowDebugPortrait, out _);
        CreateSettingsButton(panel.transform, T("ui.portrait_debug.play_animation", "播放动画"), new Vector2(220f, -365f), new Vector2(320f, 52f), PlayDebugPortraitAnimation, out _);
        CreateSettingsButton(panel.transform, T("ui.portrait_debug.hide_slot", "隐藏当前槽位"), new Vector2(540f, -365f), new Vector2(320f, 52f), HideDebugPortraitSlot, out _);
        CreateSettingsButton(panel.transform, T("ui.portrait_debug.hide_all", "隐藏全部立绘"), new Vector2(220f, -440f), new Vector2(320f, 52f), HideAllPortraitsFromDebug, out _);

        Button backButton = CreateButton(panel.transform, T("ui.common.back", "返回"), ReturnToPreviousOverlayPage, out portraitDebugBackButtonLabel);
        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(1f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(1f, 0f);
        backRect.sizeDelta = new Vector2(140f, 50f);
        backRect.anchoredPosition = new Vector2(-196f, 28f);

        Button exitButton = CreateButton(panel.transform, T("ui.common.exit", "退出"), ExitOverlayPages, out portraitDebugExitButtonLabel);
        RectTransform exitRect = exitButton.GetComponent<RectTransform>();
        exitRect.anchorMin = new Vector2(1f, 0f);
        exitRect.anchorMax = new Vector2(1f, 0f);
        exitRect.pivot = new Vector2(1f, 0f);
        exitRect.sizeDelta = new Vector2(140f, 50f);
        exitRect.anchoredPosition = new Vector2(-38f, 28f);

        portraitDebugRoot.SetActive(false);
    }

    private void ShowMainMenu()
    {
        CancelAutoAdvance();
        isInGame = false;
        isExploring = false;
        isAutoMode = false;
        isSkipMode = false;
        isDialogueHidden = false;
        isAwaitingChoice = false;
        currentNode = null;
        ClearChoices();
        FinishTypingForMenu();
        mainMenuRoot.SetActive(true);
        hudRoot.SetActive(false);
        exploreRoot.SetActive(false);
        dialogueRoot.SetActive(false);
        CloseOverlayPages(true);
        if (portraitController != null)
        {
            portraitController.HideAll();
        }

        RefreshModeLabels();
        RefreshMenuState();
        SetBackground(string.IsNullOrEmpty(currentBackgroundId) ? story.defaultBackground : currentBackgroundId);
    }

    private void OnPrimaryAction()
    {
        if (HasSave())
        {
            ContinueFromSave();
        }
        else
        {
            StartNewGame();
        }
    }

    private void RefreshMenuState()
    {
        menuTitleText.text = string.IsNullOrEmpty(story.title) ? "GAL Template" : story.title;
        primaryActionLabel.text = HasSave() ? "继续游戏" : "开始游戏";
        newGameButton.interactable = true;

        if (HasSave())
        {
            int latestSlot = FindLatestSaveSlot();
            DateTime writeTime = File.GetLastWriteTime(GetSavePath(latestSlot));
            saveInfoText.text = string.Format(T("ui.main.save_info_exists", "最新存档：槽位 {0} / {1}\n主按钮会读取最新存档；新游戏会从剧本起点重新开始。"), latestSlot, writeTime.ToString("yyyy-MM-dd HH:mm"));
        }
        else
        {
            saveInfoText.text = T("ui.main.save_info_empty", "本地存档：无\n创建存档后，主按钮会自动从“开始游戏”变为“继续游戏”。");
        }
    }

    private void ShowSettings()
    {
        CloseOverlayPages(true);
        isSettingsOpen = true;
        currentOverlayPage = GalOverlayPage.Settings;
        settingsRoot.SetActive(true);
        textSpeedSlider.SetValueWithoutNotify(settings.textSpeed);
        autoDelaySlider.SetValueWithoutNotify(settings.autoDelay);
        volumeSlider.SetValueWithoutNotify(settings.masterVolume);
        fullscreenToggle.SetIsOnWithoutNotify(settings.fullscreen);
        skipUnreadToggle.SetIsOnWithoutNotify(settings.skipUnreadText);
        textSpeedValueText.text = settings.textSpeed.ToString("0");
        autoDelayValueText.text = settings.autoDelay.ToString("0.0") + "s";
        volumeValueText.text = Mathf.RoundToInt(settings.masterVolume * 100f) + "%";
        languageValueText.text = GetLanguageButtonText();
        settingsSavePanelButton.interactable = isInGame;
        RefreshOverlayNavigationButtons();
    }

    private void HideSettings()
    {
        ExitOverlayPages();
    }

    private void ShowSavePanelFromSettings()
    {
        ShowSavePanel(true);
    }

    private void ShowLoadPanelFromSettings()
    {
        ShowLoadPanel(true);
    }

    private void ShowHistoryFromSettings()
    {
        ShowHistory(true);
    }

    private void ShowPortraitDebug()
    {
        ShowPortraitDebug(false);
    }

    private void ShowPortraitDebugFromSettings()
    {
        ShowPortraitDebug(true);
    }

    private void ShowPortraitDebug(bool returnToSettings)
    {
        if (portraitDebugRoot == null)
        {
            return;
        }

        previousOverlayPage = returnToSettings ? GalOverlayPage.Settings : GalOverlayPage.None;
        CloseOverlayPages(true);
        currentOverlayPage = GalOverlayPage.PortraitDebug;
        portraitDebugRoot.SetActive(true);
        RefreshPortraitDebugLabels();
        RefreshOverlayNavigationButtons();
    }

    private void CycleDebugPortraitSlot()
    {
        debugPortraitSlot = NextValue(debugPortraitSlot, new[] { "left", "center", "right" });
        RefreshPortraitDebugLabels();
    }

    private void CycleDebugPortraitCharacter()
    {
        debugPortraitCharacter = NextValue(debugPortraitCharacter, GetPortraitIds());
        RefreshPortraitDebugLabels();
    }

    private void CycleDebugPortraitExpression()
    {
        debugPortraitExpression = NextValue(debugPortraitExpression, new[] { "neutral", "happy", "sad", "angry", "surprised" });
        RefreshPortraitDebugLabels();
    }

    private void CycleDebugPortraitFacing()
    {
        debugPortraitFacing = NextValue(debugPortraitFacing, new[] { "auto", "right", "left" });
        RefreshPortraitDebugLabels();
    }

    private void CycleDebugPortraitAnimation()
    {
        debugPortraitAnimation = NextValue(debugPortraitAnimation, new[] { "none", "shake", "bounce", "pop", "fade" });
        RefreshPortraitDebugLabels();
    }

    private void ShowDebugPortrait()
    {
        if (portraitController == null)
        {
            return;
        }

        portraitController.Show(new GalPortraitPose
        {
            slot = debugPortraitSlot,
            character = debugPortraitCharacter,
            expression = debugPortraitExpression,
            facing = debugPortraitFacing,
            animation = debugPortraitAnimation
        });
    }

    private void PlayDebugPortraitAnimation()
    {
        if (portraitController != null)
        {
            portraitController.PlayAnimation(debugPortraitSlot, debugPortraitAnimation);
        }
    }

    private void HideDebugPortraitSlot()
    {
        if (portraitController != null)
        {
            portraitController.Hide(debugPortraitSlot);
        }
    }

    private void HideAllPortraitsFromDebug()
    {
        if (portraitController != null)
        {
            portraitController.HideAll();
        }
    }

    private void RefreshPortraitDebugLabels()
    {
        SetText(portraitDebugTitleText, T("ui.portrait_debug.title", "立绘调试"));
        SetText(portraitDebugSlotLabel, GetPortraitDebugSlotText());
        SetText(portraitDebugCharacterLabel, GetPortraitDebugCharacterText());
        SetText(portraitDebugExpressionLabel, GetPortraitDebugExpressionText());
        SetText(portraitDebugFacingLabel, GetPortraitDebugFacingText());
        SetText(portraitDebugAnimationLabel, GetPortraitDebugAnimationText());
        SetText(portraitDebugBackButtonLabel, T("ui.common.back", "返回"));
        SetText(portraitDebugExitButtonLabel, T("ui.common.exit", "退出"));
    }

    private string GetPortraitDebugSlotText()
    {
        return string.Format(T("ui.portrait_debug.slot", "位置：{0}"), debugPortraitSlot);
    }

    private string GetPortraitDebugCharacterText()
    {
        return string.Format(T("ui.portrait_debug.character", "角色：{0}"), debugPortraitCharacter);
    }

    private string GetPortraitDebugExpressionText()
    {
        return string.Format(T("ui.portrait_debug.expression", "差分：{0}"), debugPortraitExpression);
    }

    private string GetPortraitDebugFacingText()
    {
        return string.Format(T("ui.portrait_debug.facing", "朝向：{0}"), debugPortraitFacing);
    }

    private string GetPortraitDebugAnimationText()
    {
        return string.Format(T("ui.portrait_debug.animation", "动画：{0}"), debugPortraitAnimation);
    }

    private string[] GetPortraitIds()
    {
        if (story == null || story.portraits == null || story.portraits.Count == 0)
        {
            return new[] { "test" };
        }

        List<string> ids = new List<string>();
        foreach (GalPortraitEntry portrait in story.portraits)
        {
            if (portrait != null && !string.IsNullOrEmpty(portrait.id))
            {
                ids.Add(portrait.id);
            }
        }

        return ids.Count == 0 ? new[] { "test" } : ids.ToArray();
    }

    private static string NextValue(string current, string[] values)
    {
        if (values == null || values.Length == 0)
        {
            return current;
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == current)
            {
                return values[(i + 1) % values.Length];
            }
        }

        return values[0];
    }

    private void CycleLanguage()
    {
        if (story.languages == null || story.languages.Count == 0)
        {
            ShowToast(T("ui.toast.no_languages", "尚未配置多语言表。"));
            return;
        }

        int index = FindLanguageIndex(settings.language);
        index = (index + 1) % story.languages.Count;
        settings.language = story.languages[index].id;
        SaveSettings();
        ReloadStoryFilesInPlace();
        ShowToast(string.Format(T("ui.toast.language_changed", "语言预设：{0}"), GetLanguageDisplayName(settings.language)));
    }

    private string GetLanguageButtonText()
    {
        return string.Format(T("ui.settings.language", "语言：{0}"), GetLanguageDisplayName(settings.language));
    }

    private string GetLanguageDisplayName(string languageId)
    {
        if (story.languages != null)
        {
            foreach (GalLanguageEntry language in story.languages)
            {
                if (language != null && language.id == languageId)
                {
                    return string.IsNullOrEmpty(language.displayName) ? language.id : language.displayName;
                }
            }
        }

        return string.IsNullOrEmpty(languageId) ? "未配置" : languageId;
    }

    private int FindLanguageIndex(string languageId)
    {
        if (story.languages == null)
        {
            return -1;
        }

        for (int i = 0; i < story.languages.Count; i++)
        {
            if (story.languages[i] != null && story.languages[i].id == languageId)
            {
                return i;
            }
        }

        return -1;
    }

    private void LoadSettings()
    {
        settings.textSpeed = PlayerPrefs.GetFloat("GalTemplate.TextSpeed", settings.textSpeed);
        settings.autoDelay = PlayerPrefs.GetFloat("GalTemplate.AutoDelay", settings.autoDelay);
        settings.masterVolume = PlayerPrefs.GetFloat("GalTemplate.MasterVolume", settings.masterVolume);
        settings.fullscreen = PlayerPrefs.GetInt("GalTemplate.Fullscreen", settings.fullscreen ? 1 : 0) == 1;
        settings.skipUnreadText = PlayerPrefs.GetInt("GalTemplate.SkipUnreadText", settings.skipUnreadText ? 1 : 0) == 1;
        settings.language = PlayerPrefs.GetString("GalTemplate.Language", settings.language);
        settings.artProfile = PlayerPrefs.GetString("GalTemplate.ArtProfile", settings.artProfile);
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("GalTemplate.TextSpeed", settings.textSpeed);
        PlayerPrefs.SetFloat("GalTemplate.AutoDelay", settings.autoDelay);
        PlayerPrefs.SetFloat("GalTemplate.MasterVolume", settings.masterVolume);
        PlayerPrefs.SetInt("GalTemplate.Fullscreen", settings.fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("GalTemplate.SkipUnreadText", settings.skipUnreadText ? 1 : 0);
        PlayerPrefs.SetString("GalTemplate.Language", settings.language);
        PlayerPrefs.SetString("GalTemplate.ArtProfile", settings.artProfile);
        PlayerPrefs.Save();
    }

    private void ApplySettings()
    {
        AudioListener.volume = Mathf.Clamp01(settings.masterVolume);
        Screen.fullScreen = settings.fullscreen;
    }

    private void ShowToast(string message)
    {
        if (toastRoutine != null)
        {
            StopCoroutine(toastRoutine);
        }

        toastRoutine = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastText.text = message;
        toastRoot.SetActive(true);
        yield return new WaitForSecondsRealtime(1.8f);
        toastRoot.SetActive(false);
        toastRoutine = null;
    }

    private void ClearChoices()
    {
        if (choiceContainer == null)
        {
            return;
        }

        for (int i = choiceContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(choiceContainer.GetChild(i).gameObject);
        }
    }

    private void FinishTypingForMenu()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        isTyping = false;
    }

    private Text CreateText(Transform parent, string text, int size, FontStyle style, TextAnchor anchor, Color color)
    {
        GameObject textObject = CreateUiObject("Text", parent);
        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.font = GetFont(size);
        uiText.fontSize = size;
        uiText.fontStyle = style;
        uiText.alignment = anchor;
        uiText.color = color;
        uiText.supportRichText = true;
        uiText.raycastTarget = false;
        uiText.resizeTextForBestFit = true;
        uiText.resizeTextMinSize = Mathf.Max(10, size - 8);
        uiText.resizeTextMaxSize = size;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Truncate;
        return uiText;
    }

    private Button CreateButton(Transform parent, string label, UnityAction onClick, out Text labelText)
    {
        GameObject buttonObject = CreateUiObject(label + " Button", parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.98f, 0.96f, 0.88f, 0.96f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.98f, 0.96f, 0.88f, 0.96f);
        colors.highlightedColor = new Color(0.9f, 0.98f, 0.9f, 1f);
        colors.pressedColor = new Color(0.72f, 0.82f, 0.74f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        labelText = CreateText(buttonObject.transform, label, label.Length > 3 ? 20 : 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.08f, 0.08f, 0.08f, 1f));
        Stretch(labelText.GetComponent<RectTransform>());
        return button;
    }

    private Slider CreateSettingsSlider(Transform parent, string label, Vector2 anchoredPosition, float min, float max, float value, bool wholeNumbers, out Text labelText, out Text valueText)
    {
        GameObject row = CreateUiObject(label + " Setting Row", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(410f, 66f);
        rowRect.anchoredPosition = anchoredPosition;

        labelText = CreateText(row.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.1f, 0.1f, 0.1f, 1f));
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(145f, 42f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);

        Slider slider = CreateSlider(row.transform);
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(0f, 0.5f);
        sliderRect.pivot = new Vector2(0f, 0.5f);
        sliderRect.sizeDelta = new Vector2(200f, 34f);
        sliderRect.anchoredPosition = new Vector2(165f, 0f);
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.value = value;

        string displayValue = wholeNumbers ? Mathf.Round(value).ToString("0") : value.ToString("0.0");
        valueText = CreateText(row.transform, displayValue, 20, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.1f, 0.1f, 0.1f, 1f));
        RectTransform valueRect = valueText.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.sizeDelta = new Vector2(70f, 42f);
        valueRect.anchoredPosition = new Vector2(0f, 0f);

        return slider;
    }

    private Toggle CreateSettingsToggle(Transform parent, string label, Vector2 anchoredPosition, bool value, out Text labelText)
    {
        GameObject row = CreateUiObject(label + " Setting Toggle", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 0.5f);
        rowRect.sizeDelta = new Vector2(330f, 48f);
        rowRect.anchoredPosition = anchoredPosition;

        labelText = CreateText(row.transform, label, 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.1f, 0.1f, 0.1f, 1f));
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(250f, 42f);
        labelRect.anchoredPosition = Vector2.zero;

        GameObject toggleObject = CreateUiObject("Toggle", row.transform);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 0.5f);
        toggleRect.anchorMax = new Vector2(1f, 0.5f);
        toggleRect.pivot = new Vector2(1f, 0.5f);
        toggleRect.sizeDelta = new Vector2(34f, 34f);
        toggleRect.anchoredPosition = Vector2.zero;
        Toggle toggle = toggleObject.AddComponent<Toggle>();

        Image box = toggleObject.AddComponent<Image>();
        box.color = new Color(0.98f, 0.96f, 0.88f, 1f);

        GameObject checkObject = CreateUiObject("Checkmark", toggleObject.transform);
        RectTransform checkRect = checkObject.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(20f, 20f);
        Image check = checkObject.AddComponent<Image>();
        check.color = new Color(0.46f, 0.62f, 0.5f, 1f);

        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = value;
        return toggle;
    }

    private Button CreateSettingsButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 size, UnityAction onClick, out Text labelText)
    {
        Button button = CreateButton(parent, label, onClick, out labelText);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        return button;
    }

    private Slider CreateSliderRow(Transform parent, string label, float min, float max, float value, bool wholeNumbers, out Text valueText)
    {
        GameObject row = CreateUiObject(label + " Row", parent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 54f;
        HorizontalLayoutGroup rowGroup = row.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 16f;
        rowGroup.childAlignment = TextAnchor.MiddleLeft;
        rowGroup.childControlHeight = true;
        rowGroup.childControlWidth = false;

        Text labelText = CreateText(row.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.1f, 0.1f, 0.1f, 1f));
        LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 130f;

        Slider slider = CreateSlider(row.transform);
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.value = value;
        LayoutElement sliderLayout = slider.gameObject.AddComponent<LayoutElement>();
        sliderLayout.preferredWidth = 300f;
        sliderLayout.preferredHeight = 38f;

        string displayValue = wholeNumbers ? Mathf.Round(value).ToString("0") : value.ToString("0.0");
        valueText = CreateText(row.transform, displayValue, 20, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.1f, 0.1f, 0.1f, 1f));
        LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 86f;

        return slider;
    }

    private Slider CreateSlider(Transform parent)
    {
        GameObject sliderObject = CreateUiObject("Slider", parent);
        RectTransform sliderRootRect = sliderObject.GetComponent<RectTransform>();
        sliderRootRect.sizeDelta = new Vector2(200f, 34f);
        Slider slider = sliderObject.AddComponent<Slider>();

        GameObject background = CreateUiObject("Background", sliderObject.transform);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.offsetMin = new Vector2(8f, -5f);
        backgroundRect.offsetMax = new Vector2(-8f, 5f);
        Image backgroundImageComponent = background.AddComponent<Image>();
        backgroundImageComponent.color = new Color(0.2f, 0.2f, 0.18f, 0.28f);

        GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-8f, 0f);

        GameObject fill = CreateUiObject("Fill", fillArea.transform);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(1f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(0f, -5f);
        fillRect.offsetMax = new Vector2(0f, 5f);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.46f, 0.62f, 0.5f, 1f);

        GameObject handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
        Stretch(handleArea.GetComponent<RectTransform>());

        GameObject handle = CreateUiObject("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(24f, 24f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.08f, 0.08f, 0.08f, 1f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private Toggle CreateToggleRow(Transform parent, string label, bool value)
    {
        GameObject row = CreateUiObject(label + " Row", parent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 48f;
        HorizontalLayoutGroup rowGroup = row.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 16f;
        rowGroup.childAlignment = TextAnchor.MiddleLeft;
        rowGroup.childControlHeight = false;
        rowGroup.childControlWidth = false;

        Text labelText = CreateText(row.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.1f, 0.1f, 0.1f, 1f));
        LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 450f;
        labelLayout.preferredHeight = 44f;

        GameObject toggleObject = CreateUiObject("Toggle", row.transform);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(42f, 42f);
        Toggle toggle = toggleObject.AddComponent<Toggle>();

        Image box = toggleObject.AddComponent<Image>();
        box.color = new Color(0.98f, 0.96f, 0.88f, 1f);

        GameObject checkObject = CreateUiObject("Checkmark", toggleObject.transform);
        RectTransform checkRect = checkObject.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(24f, 24f);
        Image check = checkObject.AddComponent<Image>();
        check.color = new Color(0.46f, 0.62f, 0.5f, 1f);

        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = value;
        return toggle;
    }

    private void AddButtonLayout(Button button, float height, float width = -1f)
    {
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        if (width > 0f)
        {
            layout.preferredWidth = width;
        }
    }

    private GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private Font GetFont(int size)
    {
        if (uiFont != null)
        {
            return uiFont;
        }

        try
        {
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, size);
        }
        catch
        {
            uiFont = null;
        }

        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return uiFont;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private static void AddNonEmpty(HashSet<string> set, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            set.Add(value);
        }
    }

    private static string FirstNonEmpty(string first, string second)
    {
        if (!string.IsNullOrEmpty(first))
        {
            return first;
        }

        return second;
    }
}
