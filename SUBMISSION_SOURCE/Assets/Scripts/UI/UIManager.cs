// Assets/Scripts/UI/UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Owns all HUD and screen UI.
/// Reacts to GameEvents — never called directly by gameplay systems.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    [Header("Pickup Prompt")]
    [SerializeField] private TMP_Text pickupPromptText;

    [Header("Death Screen")]
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Win Screen")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private Button winMainMenuButton;
    [SerializeField] private Button winQuitButton;
    
    [Header("Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseOptionsButton;
    [SerializeField] private Button pauseMainMenuButton;
    [SerializeField] private Button pauseQuitButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private GameObject mainMenuCanvas; // The Main Menu Canvas/Panel
    [SerializeField] private Button backFromOptionsButton;
    [SerializeField] private Button fullScreenButton;

    [Header("Combat UI")]
    [SerializeField] private GameObject crosshairImage;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private GameObject ammoPanel;
    [SerializeField] private Image damageFlashOverlay; // Fullscreen red image

    [Header("HUD")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private TMP_Text narrativeText;


    // Internal state
    public static bool IsPaused { get; private set; }
    public static void SetPaused(bool paused) 
    { 
        IsPaused = paused; 
        // Force the UIManager instance to update HUD visibility if it exists
        UIManager instance = FindFirstObjectByType<UIManager>();
        if (instance != null) instance.SyncHUDVisibility();
    }

    public void SyncHUDVisibility()
    {
        bool showHUD = !IsPaused && !isDead;
        bool isAtMainMenu = (mainMenuCanvas != null && mainMenuCanvas.activeInHierarchy);
        if (isAtMainMenu) showHUD = false;

        if (hudPanel != null) hudPanel.SetActive(showHUD);
        if (healthSlider != null) healthSlider.gameObject.SetActive(showHUD);
        if (healthText != null) healthText.gameObject.SetActive(showHUD);
        
        Debug.Log($"[UIManager] HUD Visibility Synced: {showHUD} (Paused: {IsPaused}, Dead: {isDead}, MainMenu: {isAtMainMenu})");
    }
    private WeaponType currentEquippedType = WeaponType.None;
    private PlayerAmmo playerAmmo;
    private Coroutine narrativeCoroutine;
    private bool isPaused = false;
    private bool isDead = false;

    private void OnEnable()
    {
        GameEvents.OnHealthChanged += UpdateHealthBar;
        GameEvents.OnPlayerDied += ShowDeathScreen;
        GameEvents.OnPickupEnterRange += ShowPickupPrompt;
        GameEvents.OnPickupExitRange += HidePickupPrompt;
        GameEvents.OnNarrativeMessage += HandleNarrativeMessage;
        GameEvents.OnGameWin += ShowWinScreen;

        GameEvents.OnAimStateChanged += ToggleCrosshair;
        GameEvents.OnMagazineChanged += UpdateAmmoUI;
        GameEvents.OnAmmoChanged += UpdateReserveAmmoUI;
        GameEvents.OnWeaponEquipped += HandleWeaponEquipped;
        GameEvents.OnWeaponDropped += HandleWeaponDropped;
        GameEvents.OnPlayerDamaged += TriggerDamageFeedback;

        InputManager.OnPausePressed += HandlePausePressed;
    }

    private void OnDisable()
    {
        GameEvents.OnHealthChanged -= UpdateHealthBar;
        GameEvents.OnPlayerDied -= ShowDeathScreen;
        GameEvents.OnPickupEnterRange -= ShowPickupPrompt;
        GameEvents.OnPickupExitRange -= HidePickupPrompt;
        GameEvents.OnNarrativeMessage -= HandleNarrativeMessage;
        GameEvents.OnGameWin -= ShowWinScreen;

        GameEvents.OnAimStateChanged -= ToggleCrosshair;
        GameEvents.OnMagazineChanged -= UpdateAmmoUI;
        GameEvents.OnAmmoChanged -= UpdateReserveAmmoUI;
        GameEvents.OnWeaponEquipped -= HandleWeaponEquipped;
        GameEvents.OnWeaponDropped -= HandleWeaponDropped;
        GameEvents.OnPlayerDamaged -= TriggerDamageFeedback;

        InputManager.OnPausePressed -= HandlePausePressed;
    }

    private void Start()
    {
        // Find PlayerAmmo in the scene to pull reserve counts during weapon switches
        playerAmmo = FindAnyObjectByType<PlayerAmmo>();

        // --- UI AUTO-FIXER ---
        // Ensure an EventSystem exists so buttons are clickable!
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem_AutoCreated");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[UIManager] Created missing EventSystem to ensure buttons are clickable.");
        }

        if (damageFlashOverlay == null)
        {
            // Search more broadly for the flash overlay
            Image[] allImages = GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                string n = img.gameObject.name.ToLower();
                if (n.Contains("flash") || n.Contains("damage") || n.Contains("overlay"))
                {
                    damageFlashOverlay = img;
                    damageFlashOverlay.raycastTarget = false;
                    break;
                }
            }
        }

        if (deathScreen == null)
        {
            Transform dp = transform.Find("DeathPanel");
            if (dp == null) dp = transform.Find("Canvas/DeathPanel");
            if (dp == null) dp = transform.Find("Canvas/Canvas/MenuCanvas/DeathPanel");
            if (dp != null) deathScreen = dp.gameObject;
        }

        if (mainMenuCanvas == null)
        {
            Transform mmc = transform.Find("MenuCanvas");
            if (mmc == null) mmc = transform.Find("Canvas/MenuCanvas");
            if (mmc != null) mainMenuCanvas = mmc.gameObject;
        }

        EnsureCanvasScaling();
        // FixUILayout(deathScreen, true, true);
        FixUILayout(pauseMenu, true, true);   
        FixUILayout(optionsMenu, true, true);  
        FixUILayout(winScreen, true, true);   

        // Safety checks before using references
        if (deathScreen != null)
            deathScreen.SetActive(false);
            
        if (damageFlashOverlay != null)
            damageFlashOverlay.gameObject.SetActive(false);

        if (ammoPanel != null)
            ammoPanel.SetActive(false);

        if (narrativeText != null)
            narrativeText.gameObject.SetActive(false);

        if (hudPanel != null)
        {
            // If the Main Menu is active at start, hide the HUD.
            // Otherwise, show it.
            bool isAtMainMenu = (mainMenuCanvas != null && mainMenuCanvas.activeInHierarchy);
            hudPanel.SetActive(!isAtMainMenu);
            if (healthSlider != null) healthSlider.gameObject.SetActive(!isAtMainMenu);
            if (healthText != null) healthText.gameObject.SetActive(!isAtMainMenu);
        }

        HidePickupPrompt();

        // Wire buttons
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        // Pause Menu Wiring
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(HandlePausePressed);
        if (pauseOptionsButton != null) pauseOptionsButton.onClick.AddListener(OpenOptions);
        if (pauseMainMenuButton != null) pauseMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (pauseQuitButton != null) pauseQuitButton.onClick.AddListener(OnQuitClicked);
        if (pauseRestartButton != null) pauseRestartButton.onClick.AddListener(OnRetryClicked);
        if (backFromOptionsButton != null) backFromOptionsButton.onClick.AddListener(CloseOptions);
        if (fullScreenButton != null) fullScreenButton.onClick.AddListener(ToggleFullScreen);

        if (winScreen != null) winScreen.SetActive(false);
        if (winMainMenuButton != null) winMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (winQuitButton != null) winQuitButton.onClick.AddListener(OnQuitClicked);

        // Settings Initialization
        VolumeManager.Initialize();
        LoadVolumeSettings();

        // --- AUTO-WIRE BUTTONS ---
        AutoWireButtons();
        AutoWireDeathScreen();
        AutoWireSettings();
    }

    private void AutoWireButtons()
    {
        // 1. Wire Pause Menu
        if (pauseMenu != null)
        {
            void WirePause(ref Button btn, string nameKeyword, UnityEngine.Events.UnityAction action)
            {
                if (btn != null) return;
                Button[] allButtons = pauseMenu.GetComponentsInChildren<Button>(true);
                foreach (var b in allButtons)
                {
                    if (b.gameObject.name.ToLower().Contains(nameKeyword.ToLower()))
                    {
                        btn = b;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(action);
                        Debug.Log($"[UIManager] Pause Menu: Wired {b.gameObject.name} to {action.Method.Name}");
                        return;
                    }
                }
            }

            WirePause(ref resumeButton, "Resume", ResumeGame);
            WirePause(ref resumeButton, "Back", ResumeGame); // In Pause menu, 'Back' should Resume
            WirePause(ref pauseOptionsButton, "Option", OpenOptions);
            WirePause(ref pauseMainMenuButton, "Main", OnMainMenuClicked);
            WirePause(ref pauseQuitButton, "Quit", OnQuitClicked);
            WirePause(ref pauseRestartButton, "Restart", OnRetryClicked);
        }

        // 2. Wire Options Menu
        if (optionsMenu != null)
        {
            if (backFromOptionsButton == null)
            {
                Button[] allButtons = optionsMenu.GetComponentsInChildren<Button>(true);
                foreach (var b in allButtons)
                {
                    if (b.gameObject.name.ToLower().Contains("back"))
                    {
                        backFromOptionsButton = b;
                        backFromOptionsButton.onClick.RemoveAllListeners();
                        backFromOptionsButton.onClick.AddListener(CloseOptions);
                        Debug.Log($"[UIManager] Options Menu: Wired {b.gameObject.name} to CloseOptions");
                        break;
                    }
                }
            }
        }
    }

    private void AutoWireDeathScreen()
    {
        if (deathScreen == null) return;

        void WireDeath(ref Button btn, string nameKeyword, UnityEngine.Events.UnityAction action)
        {
            if (btn != null) return;
            Button[] allButtons = deathScreen.GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                string bName = b.gameObject.name.ToLower();
                if (bName.Contains(nameKeyword.ToLower()))
                {
                    btn = b;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(action);
                    Debug.Log($"[UIManager] Death Screen: Wired {b.gameObject.name} to {action.Method.Name}");
                    return;
                }
            }
        }

        // Redirect Retry to Main Menu as requested
        WireDeath(ref retryButton, "Retry", OnMainMenuClicked);
        WireDeath(ref retryButton, "Restart", OnMainMenuClicked);
        WireDeath(ref retryButton, "Again", OnMainMenuClicked);
        
        // Ensure Main Menu button is wired
        WireDeath(ref mainMenuButton, "Main", OnMainMenuClicked);
        WireDeath(ref mainMenuButton, "Menu", OnMainMenuClicked);
        WireDeath(ref mainMenuButton, "Title", OnMainMenuClicked);
        
        WireDeath(ref quitButton, "Quit", OnQuitClicked);

        if (retryButton == null)
        {
            Debug.LogWarning("[UIManager] Could not find a 'Retry' button in the Death Screen children. Please name your button 'Retry' or 'Restart'.");
        }
    }

    private void ResumeGame()
    {
        Debug.Log("[UIManager] Resume button clicked.");
        if (isPaused) HandlePausePressed();
    }

    private void AutoWireSettings()
    {
        if (optionsMenu == null) return;

        Slider[] allSliders = optionsMenu.GetComponentsInChildren<Slider>(true);
        foreach (var s in allSliders)
        {
            if (s.gameObject.name.ToLower().Contains("master"))
            {
                if (masterVolumeSlider == null) masterVolumeSlider = s;
            }
            else if (s.gameObject.name.ToLower().Contains("sfx"))
            {
                if (sfxVolumeSlider == null) sfxVolumeSlider = s;
            }
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    private void LoadVolumeSettings()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = VolumeManager.MasterVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = VolumeManager.SFXVolume;
    }

    private void OnMasterVolumeChanged(float value)
    {
        VolumeManager.MasterVolume = value;
    }

    private void OnSFXVolumeChanged(float value)
    {
        VolumeManager.SFXVolume = value;
    }

    // ── Event Handlers ────────────────────────────────────────────────

    private void UpdateHealthBar(float current, float max)
    {
        if (healthSlider != null)
            healthSlider.value = current / max;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void TriggerDamageFeedback(float amount)
    {
        if (damageFlashOverlay != null)
        {
            StopCoroutine("DamageFlashRoutine");
            StartCoroutine(DamageFlashRoutine());
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        damageFlashOverlay.gameObject.SetActive(true);
        damageFlashOverlay.raycastTarget = false; // MUST be false so it doesn't block buttons!
        Color c = damageFlashOverlay.color;
        
        // Initial flash
        c.a = 0.5f;
        damageFlashOverlay.color = c;

        // Fade out
        float elapsed = 0f;
        float duration = 0.4f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0.5f, 0f, elapsed / duration);
            damageFlashOverlay.color = c;
            yield return null;
        }

        damageFlashOverlay.gameObject.SetActive(false);
    }

    private void ShowDeathScreen()
    {
        Debug.Log("[UIManager] Player Died. Starting Death Sequence.");
        isDead = true;

        // Force-clear ANY red overlay found
        if (damageFlashOverlay != null)
        {
            StopAllCoroutines(); 
            damageFlashOverlay.gameObject.SetActive(false);
            damageFlashOverlay.enabled = false;
        }

        // Start the delayed activation of the death screen buttons
        StartCoroutine(DelayedDeathScreen(5f));
    }

    private IEnumerator DelayedDeathScreen(float delay)
    {
        // Pause the game immediately but don't show buttons yet
        Time.timeScale = 0f;
        
        // Wait for 5 seconds in REAL time
        yield return new WaitForSecondsRealtime(delay);

        if (deathScreen != null)
        {
            deathScreen.SetActive(true);
            
            // Ensure interactivity
            CanvasGroup cg = deathScreen.GetComponent<CanvasGroup>();
            if (cg == null) cg = deathScreen.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;
            cg.alpha = 1f;

            AutoWireDeathScreen();
        }

        // Unlock cursor only after the buttons appear
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    [Header("Victory Visual")]
    [SerializeField] private GameObject winImagePanel;
    [SerializeField] private float winImageDuration = 5f;

    private void ShowWinScreen()
    {
        Debug.Log("[UIManager] Showing Win Screen Sequence (Image)");
        
        // Hide HUD
        if (hudPanel != null) hudPanel.SetActive(false);
        if (healthSlider != null) healthSlider.gameObject.SetActive(false);
        if (healthText != null) healthText.gameObject.SetActive(false);

        // If a victory image/panel is assigned, show it first
        if (winImagePanel != null)
        {
            StartCoroutine(ShowWinImageRoutine());
        }
        else
        {
            FinalizeWin();
        }
    }

    private IEnumerator ShowWinImageRoutine()
    {
        winImagePanel.SetActive(true);
        
        // Wait for the duration or until a click/key
        float timer = 0f;
        while (timer < winImageDuration)
        {
            timer += Time.unscaledDeltaTime;
            
            // New Input System check for "any key" or "any button"
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) break;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) break;
            
            yield return null;
        }

        Debug.Log("[UIManager] Victory sequence complete. Quitting game.");
        winImagePanel.SetActive(false);
        
        // Automatically quit the game
        OnQuitClicked();
    }

    private void FinalizeWin()
    {
        if (winScreen != null)
            winScreen.SetActive(true);
        else
            Debug.LogWarning("[UIManager] Win Screen GameObject is NOT assigned!");

        // Pause the game and unlock cursor
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Automatically quit after a few seconds if no win image routine was used
        StartCoroutine(AutoQuitAfterDelay(5f));
    }

    private IEnumerator AutoQuitAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Debug.Log("[UIManager] Auto-quitting after victory.");
        OnQuitClicked();
    }

    private void HandlePausePressed()
    {
        Debug.Log($"[UIManager] Pause Pressed. Current State - Dead: {isDead}, Paused: {isPaused}");
        if (isDead) return;

        isPaused = !isPaused;
        IsPaused = isPaused;
        
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPaused);
        }
        else
        {
            Debug.LogWarning("[UIManager] Pause Menu GameObject is NOT assigned in the Inspector!");
        }

        SyncHUDVisibility();
        
        // If we unpause, make sure options is also closed
        if (!isPaused && optionsMenu != null)
            optionsMenu.SetActive(false);

        Time.timeScale = isPaused ? 0f : 1f;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;

        GameEvents.RaisePauseToggled(isPaused);
    }

    private void OpenOptions()
    {
        if (optionsMenu != null)
        {
            Debug.Log("[UIManager] Opening local Options panel.");
            
            // If the options panel is part of the Main Menu canvas, make sure the canvas is on
            if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);
            
            optionsMenu.SetActive(true);

            // If the options panel is part of the MainMenuManager system, call its ShowOptions function
            MainMenuManager menuManager = optionsMenu.GetComponentInParent<MainMenuManager>();
            if (menuManager != null)
            {
                menuManager.ShowOptions();
            }

            if (pauseMenu != null) pauseMenu.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(false); // Hide HUD
        }
        else
        {
            Debug.LogWarning("[UIManager] Options Menu Panel is NOT assigned!");
        }
    }

    private void CloseOptions()
    {
        if (optionsMenu != null) optionsMenu.SetActive(false);

        // If we are in-game (Paused), hide the Main Menu background and return to Pause
        if (isPaused)
        {
            if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
            if (pauseMenu != null) pauseMenu.SetActive(true);
        }
        else
        {
            // If we are at the actual title screen, just switch panels within the Main Menu
            if (mainMenuCanvas != null)
            {
                MainMenuManager menuManager = mainMenuCanvas.GetComponent<MainMenuManager>();
                if (menuManager != null)
                {
                    menuManager.ShowMainMenu();
                }
            }
        }
    }

    private void ShowPickupPrompt(string label)
    {
        if (pickupPromptText != null)
        {
            pickupPromptText.gameObject.SetActive(true);
            pickupPromptText.text = label;
        }
    }

    private void HidePickupPrompt()
    {
        if (pickupPromptText != null)
            pickupPromptText.gameObject.SetActive(false);
    }

    private void HandleNarrativeMessage(string msg, float dur)
    {
        if (narrativeCoroutine != null)
            StopCoroutine(narrativeCoroutine);

        narrativeCoroutine = StartCoroutine(ShowNarrativeRoutine(msg, dur));
    }

    private IEnumerator ShowNarrativeRoutine(string msg, float dur)
    {
        if (narrativeText != null)
        {
            narrativeText.gameObject.SetActive(true);
            narrativeText.text = msg;
            
            yield return new WaitForSeconds(dur);
            
            narrativeText.gameObject.SetActive(false);
        }
    }

    // ── Combat UI ────────────────────────────────────────────────────────

    private void ToggleCrosshair(bool isAiming)
    {
        if (crosshairImage != null)
            crosshairImage.SetActive(isAiming);
    }

    private void HandleWeaponEquipped(WeaponType type)
    {
        currentEquippedType = type;
        bool isGun = (type == WeaponType.Pistol || type == WeaponType.Rifle);

        if (ammoPanel != null)
            ammoPanel.SetActive(isGun);

        if (ammoText != null)
            ammoText.gameObject.SetActive(isGun);

        if (isGun && playerAmmo != null)
        {
            AmmoType aType = (type == WeaponType.Pistol) ? AmmoType.Pistol : AmmoType.Rifle;
            lastReserve = playerAmmo.GetAmmo(aType);
            RefreshAmmoText();
        }
    }

    private void HandleWeaponDropped(WeaponType type)
    {
        if (ammoPanel != null)
            ammoPanel.SetActive(false);

        if (ammoText != null)
            ammoText.gameObject.SetActive(false);
    }

    private int lastMag = 0;
    private int lastReserve = 0;
    private int lastMaxReserve = 0;

    private void UpdateAmmoUI(WeaponType type, int currentMag, int maxMag)
    {
        lastMag = currentMag;
        RefreshAmmoText();
    }

    private void UpdateReserveAmmoUI(AmmoType type, int currentReserve, int maxReserve)
    {
        AmmoType currentWeaponAmmoType = (currentEquippedType == WeaponType.Pistol) ? AmmoType.Pistol : AmmoType.Rifle;
        if (type != currentWeaponAmmoType) return;

        lastReserve = currentReserve;
        lastMaxReserve = maxReserve;
        RefreshAmmoText();
    }

    private void RefreshAmmoText()
    {
        if (ammoText != null)
        {
            if (lastMag <= 0)
            {
                ammoText.text = $"({lastMag}/{lastReserve})\nPress R to Reload";
            }
            else
            {
                ammoText.text = $"({lastMag}/{lastReserve})";
            }
        }
    }

    private void EnsureCanvasScaling()
    {
        CanvasScaler scaler = GetComponentInParent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void FixUILayout(GameObject panel, bool stretch, bool isLeftAligned = false)
    {
        if (panel == null) return;
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt != null)
        {
            if (stretch)
            {
                // Make the panel fill the entire screen
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                
                // Add a dark background
                Image img = panel.GetComponent<Image>();
                if (img == null) img = panel.AddComponent<Image>();
                
                img.enabled = true;
                img.sprite = null; // Ensure it's a solid color
                img.color = new Color(0f, 0f, 0f, 0.85f); // Deep dark overlay
                img.raycastTarget = true; // Block clicks to the game world
                
                // Add a VerticalLayoutGroup to auto-stack buttons
                VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
                if (layout == null) layout = panel.AddComponent<VerticalLayoutGroup>();
                
                layout.childAlignment = isLeftAligned ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                layout.padding = isLeftAligned ? new RectOffset(150, 0, 0, 0) : new RectOffset(0, 150, 0, 0);
                layout.spacing = isLeftAligned ? 80f : 60f; // Increased spacing for labels
                layout.childControlHeight = false;
                layout.childControlWidth = false;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = false;

                // Ensure all children are anchored correctly
                foreach (RectTransform child in rt)
                {
                    // Skip ONLY actual text objects that act as titles to keep them centered
                    // If it has a Button component, we definitely want to format it
                    string childName = child.gameObject.name.ToLower();
                    bool isButton = child.GetComponent<Button>() != null;
                    bool isTextOnly = (child.GetComponent<TMP_Text>() != null || child.GetComponent<Text>() != null) && !isButton;

                    if (isTextOnly)
                    {
                        // Set labels on the dark panel to WHITE
                        var labelText = child.GetComponent<TMP_Text>();
                        if (labelText != null) labelText.color = Color.white;
                        else
                        {
                            var legacyLabel = child.GetComponent<Text>();
                            if (legacyLabel != null) legacyLabel.color = Color.white;
                        }

                        if (childName.Contains("title") || childName.Contains("menu")) continue;
                    }

                    // Special case: "Back" button in corners
                    if (childName.Contains("back"))
                    {
                        // Add LayoutElement to ignore the VerticalLayoutGroup
                        LayoutElement le = child.GetComponent<LayoutElement>();
                        if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
                        le.ignoreLayout = true;

                        // Position it small in the bottom-right corner
                        child.anchorMin = new Vector2(1f, 0f); 
                        child.anchorMax = new Vector2(1f, 0f);
                        child.pivot = new Vector2(1f, 0f);     
                        child.sizeDelta = new Vector2(180f, 50f); 
                        child.anchoredPosition = new Vector2(-50f, 50f); 
                    }
                    else
                    {
                        if (isLeftAligned)
                        {
                            child.anchorMin = new Vector2(0f, 0.5f); 
                            child.anchorMax = new Vector2(0f, 0.5f);
                            child.pivot = new Vector2(0f, 0.5f);     
                            child.sizeDelta = new Vector2(400f, 60f); // Medium
                        }
                        else
                        {
                            child.anchorMin = new Vector2(1f, 0.5f); 
                            child.anchorMax = new Vector2(1f, 0.5f);
                            child.pivot = new Vector2(1f, 0.5f);     
                            child.sizeDelta = new Vector2(500f, 100f); // Large
                        }
                        child.anchoredPosition = Vector2.zero;   

                        // Ensure text inside buttons is centered and visible (Black on White buttons)
                        // But nested labels in sliders should be WHITE
                        var texts = child.GetComponentsInChildren<TMP_Text>();
                        foreach (var txt in texts)
                        {
                            if (isButton)
                            {
                                txt.alignment = TextAlignmentOptions.Center;
                                txt.rectTransform.anchorMin = Vector2.zero;
                                txt.rectTransform.anchorMax = Vector2.one;
                                txt.rectTransform.sizeDelta = Vector2.zero;
                                txt.color = Color.black; 
                            }
                            else
                            {
                                // Slider labels or other nested text
                                txt.color = Color.white;
                            }
                        }

                        var legacyTexts = child.GetComponentsInChildren<Text>();
                        foreach (var txt in legacyTexts)
                        {
                            if (isButton)
                            {
                                txt.alignment = TextAnchor.MiddleCenter;
                                txt.rectTransform.anchorMin = Vector2.zero;
                                txt.rectTransform.anchorMax = Vector2.one;
                                txt.rectTransform.sizeDelta = Vector2.zero;
                                txt.color = Color.black;
                            }
                            else
                            {
                                txt.color = Color.white;
                            }
                        }
                    }
                }
            }
            else
            {
                // Just center it
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
            rt.localScale = Vector3.one;       
        }
    }

    public void ToggleFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        Debug.Log($"[UIManager] Full Screen Toggled: {Screen.fullScreen}");
    }

    // ── Button Handlers ───────────────────────────────────────────────

    public void OnRetryClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnMainMenuClicked()
    {
        Debug.Log("[UIManager] OnMainMenuClicked called.");
        Time.timeScale = 1f; 
        
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(true);
            
            // Explicitly look for and enable the main menu panel child
            string[] possiblePanelNames = { "MainPanel", "MainPanel", "mainmenuPanel", "MenuPanel", "StartPanel" };
            Transform mainPanel = null;
            foreach (string pName in possiblePanelNames)
            {
                mainPanel = mainMenuCanvas.transform.Find(pName);
                if (mainPanel != null) break;
            }
            
            if (mainPanel != null) 
            {
                mainPanel.gameObject.SetActive(true);
                Debug.Log($"[UIManager] Activated Main Menu panel: {mainPanel.name}");
            }
            else
            {
                Debug.LogWarning("[UIManager] Could not find a child named 'MainPanel' under MenuCanvas. Please ensure your main menu panel has this name.");
            }

            MainMenuManager menuManager = mainMenuCanvas.GetComponent<MainMenuManager>();
            if (menuManager != null)
            {
                menuManager.ShowMainMenu();
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (pauseMenu != null) pauseMenu.SetActive(false);
            if (deathScreen != null) deathScreen.SetActive(false);
            if (optionsMenu != null) optionsMenu.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[UIManager] Main Menu Canvas not found. Loading scene 0.");
            SceneManager.LoadScene(0);
        }
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}