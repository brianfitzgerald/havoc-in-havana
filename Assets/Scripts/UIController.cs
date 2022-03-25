
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;
public class UIController : MonoBehaviourPun
{

    [HideInInspector]
    public Text objectiveText;
    private Text objectiveSubtext;
    private CanvasGroup objectiveCanvasGroup;
    [HideInInspector]
    public CanvasGroup shotOverlayGroup;
    [HideInInspector]
    public Text currentWeaponText;
    private Text healthText;
    private Text currentAmmoText;
    private Text totalAmmoText;
    private Text ammoInInventoryText;
    [HideInInspector]
    public Text interactionPromptText;
    private Text roleSubtitle;
    private GameObject shopButtonPrefab;
    private Text roleInfo;
    private Text timeLeftInRound;
    private Text allAmmoPlayerText;
    private Text roleDisplay;
    [HideInInspector]
    public ScoreboardController scoreboardController;
    public static UIController Instance;
    void Start()
    {
        Instance = this;
    }
    private PlayerController playerController;
    private Transform pauseCanvas;
    public Slider sensitivitySlider;
    public Text playerNameHoverText;
    public Toggle scrollToggle;
    public Button resumeButton;
    public Button unstuckButton;
    public Button exitButton;
    public Text moneyAmountText;
    public void SetUIReferences(PlayerController playerController)
    {
        this.playerController = playerController;
        shopButtonPrefab = Resources.Load<GameObject>("ShopButton");
        var canvas = GameObject.Find("Player Canvas").transform;
        currentWeaponText = canvas.Find("CurrentWeaponText").GetComponent<Text>();
        healthText = canvas.Find("Health").GetComponent<Text>();
        currentAmmoText = canvas.Find("CurrentAmmoText").GetComponent<Text>();
        totalAmmoText = canvas.Find("TotalAmmoText").GetComponent<Text>();
        roleDisplay = canvas.Find("RoleDisplay").GetComponent<Text>();
        roleSubtitle = canvas.Find("RoleSubtitle").GetComponent<Text>();
        roleInfo = canvas.Find("RoleInfo").GetComponent<Text>();
        timeLeftInRound = canvas.Find("TimeLeftInRound").GetComponent<Text>();
        allAmmoPlayerText = canvas.Find("AllAmmoText").GetComponent<Text>();
        ammoInInventoryText = canvas.Find("AmmoInInventoryText").GetComponent<Text>();
        moneyAmountText = canvas.Find("MoneyAmount").GetComponent<Text>();
        interactionPromptText = canvas.Find("Interaction").GetComponent<Text>();
        objectiveCanvasGroup = canvas.Find("Objective").GetComponent<CanvasGroup>();
        playerNameHoverText = canvas.Find("PlayerNameHover").GetComponent<Text>();
        shotOverlayGroup = canvas.Find("ShotOverlay").GetComponent<CanvasGroup>();
        objectiveText = objectiveCanvasGroup.transform.Find("ObjectiveTitleText").GetComponent<Text>();
        objectiveSubtext = objectiveCanvasGroup.transform.Find("ObjectiveSubtext").GetComponent<Text>();
        shopUI = canvas.Find("Shop").GetComponent<GridLayoutGroup>();
        shopUI.gameObject.SetActive(false);
        hitMarker = canvas.Find("HitMarker").GetComponent<CanvasGroup>();
        hitMarker.alpha = 0;

        objectiveCanvasGroup.alpha = 0;
        roleInfo.text = "";
        scoreboardController = canvas.Find("Scoreboard").GetComponent<ScoreboardController>();
        scoreboardController.SetUIReferences();

        playerNameHoverText.text = "";
        timeLeftInRound.text = "";
        interactionPromptText.text = "";
        pauseCanvas = canvas.Find("Pause Panel");
        sensitivitySlider = pauseCanvas.Find("Slider").GetComponent<Slider>();
        scrollToggle = pauseCanvas.Find("ScrollToggle").GetComponent<Toggle>();
        pauseCanvas.gameObject.SetActive(false);

        exitButton = pauseCanvas.Find("Exit").GetComponent<Button>();
        unstuckButton = pauseCanvas.Find("Unstuck").GetComponent<Button>();
        resumeButton = pauseCanvas.Find("Resume").GetComponent<Button>();


        scrollToggle.onValueChanged.AddListener(delegate
        {
            scrollEnabled = scrollToggle.isOn;
        });

        scrollEnabled = true;

        exitButton.onClick.AddListener(delegate
        {
            GameController.Instance.LocalPlayerInstance.playerState.teamAssigned = false;
            showExitConfirm = false;
            PhotonNetwork.LeaveRoom();
        });

        unstuckButton.onClick.AddListener(delegate
        {
            GameController.Instance.LocalPlayerInstance.transform.position = GameController.Instance.spawnsParent.transform.position;
        });

        resumeButton.onClick.AddListener(delegate
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            showExitConfirm = false;
            pauseCanvas.gameObject.SetActive(false);
        });

        if (PlayerPrefs.HasKey(sensitivitySliderPrefsKey))
        {
            var val = PlayerPrefs.GetFloat(sensitivitySliderPrefsKey);
            PlayerInput.mouseSensitivity = val;
            sensitivitySlider.value = val;
        }

        sensitivitySlider.onValueChanged.AddListener(delegate
        {
            var val = sensitivitySlider.value;
            PlayerInput.mouseSensitivity = val;
            PlayerPrefs.SetFloat(sensitivitySliderPrefsKey, val);
        });
    }

    private static string sensitivitySliderPrefsKey = "MouseSensitivity";
    private Coroutine activePopup;
    private Coroutine activeIncidentalTextGroup;
    public bool scrollEnabled = true;

    public bool showExitConfirm = false;
    public bool showShop = false;
    public string getAmmoText(PlayerController playerController)
    {
        var resultText = "";
        var i = 1;
        
        var weaponTypes = new List<WeaponType>();
        weaponTypes.Add(WeaponType.PISTOL);
        if (playerController.hasPrimaryWeapon) {
            weaponTypes.Add(playerController.primaryWeapon);
        }
        foreach (var t in weaponTypes)
        {
            var ammoAmount = playerController.gunController.ammoInClip[(int)t];
            resultText += $"[{i}] : {t}: {ammoAmount}  ";
            i += 1;
        }
        return resultText;
    }
    void Update()
    {
        Debug.Log(GameController.GetStackTrace());

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            showExitConfirm = true;
            pauseCanvas.gameObject.SetActive(true);
            // PhotonNetwork.LeaveRoom();
            // Application.Quit();
        }

    }
    public void ShowPopUpText(string title = "", string subtitle = "", float duration = 15f)
    {
        if (activePopup != null)
        {
            StopCoroutine(activePopup);
        }
        if (title != "")
        {
            objectiveText.text = title;
        }
        if (subtitle != "")
        {
            objectiveSubtext.text = subtitle;
        }
        var group = PopUpGroup(objectiveCanvasGroup, 3f, duration);
        activePopup = StartCoroutine(group);
    }

    public string getOtherTraitorsText(bool addPrefixAndOmitLocalPlayer = true)
    {
        GameController.Instance.RefreshPlayers();
        var otherPlayers = GameController.Instance.playerReferences;
        var otherTraitors = otherPlayers.Where(p => (PlayerTeam)p.Value.playerState.team == PlayerTeam.TRAITOR && (!addPrefixAndOmitLocalPlayer || !p.Value.isLocalPlayer())).Select(p => p.Value.playerState.name).ToList();
        var otherTraitorStr = System.String.Join(", ", otherTraitors);
        if (!addPrefixAndOmitLocalPlayer)
        {
            return otherTraitorStr;
        }
        if (otherTraitors.Count < 1)
        {
            return "No fellow traitors";
        }
        return $"Fellow traitors: {otherTraitorStr}";
    }


    public void UpdateWaitingText(PlayerController playerController, bool subtractOne = false)
    {
        Debug.Log(GameController.GetStackTrace());
        GameController.Instance.RefreshPlayers();
        var unReadyPlayers = GameController.Instance.playerReferences.Values.Where(p => !p.playerState.ready).Count();
        if (subtractOne)
        {
            unReadyPlayers--;
        }
        var pWord = unReadyPlayers > 1 ? "players" : "player";
        if (!playerController.isLocalPlayer())
        {
            return;
        }

        var gameStatus = (GameStatus)GameController.Instance.gameState.gameStatus;
        var gameState = GameController.Instance.gameState;
        if (GameController.Instance.LocalPlayerInstance == null)
        {
            return;
        }
        var newTeam = (PlayerTeam)GameController.Instance.LocalPlayerInstance.playerState.team;
        // Debug.Log($"update text: {newTeam} {gameStatus}");
        if (gameStatus == GameStatus.PLAYING || gameStatus == GameStatus.WON)
        {
            roleSubtitle.text = newTeam == PlayerTeam.INNOCENT ? "You are" : "You are a";
            roleDisplay.text = newTeam.ToString();
            if (newTeam == PlayerTeam.TRAITOR)
            {
                // Debug.Log("get traitor text");
                roleInfo.text = getOtherTraitorsText();
            }
            else
            {
                // Debug.Log("do not get traitor text");
                roleInfo.text = "";
            }

        }
        else if (!playerController.playerState.ready)
        {
            roleSubtitle.text = $"Waiting for {unReadyPlayers} {pWord} to ready up.";
            roleDisplay.text = "Press H to Ready Up!";
            roleInfo.text = "";
            timeLeftInRound.text = "";
            // objectiveText.text = "";
        }
        else if (GameController.Instance.gameState.numPlayers < GameController.Instance.minPlayersToStart)
        {
            roleDisplay.text = $"Need {GameController.Instance.minPlayersToStart} players to begin.";
        }
        else
        {
            roleSubtitle.text = $"Waiting for {unReadyPlayers} {pWord} to ready up.";
            roleDisplay.text = "You are ready.";
        }
    }

    public void UpdatePlayerUIText(PlayerController playerController)
    {
        if (playerController != GameController.Instance.LocalPlayerInstance)
        {
            return;
        }
        var gameState = GameController.Instance.gameState;
        var timeLeft = gameState.timeLimit - GameController.Instance.elapsedInRound;
        timeLeftInRound.text = System.TimeSpan.FromSeconds((double)timeLeft).ToString(@"mm\:ss");
        healthText.text = $"{playerController.playerState.health.ToString()} HP";
        //Set current ammo text from ammo int
        var currentWeapon = playerController.currentWeaponType;
        currentAmmoText.text = playerController.gunController.ammoInClip[(int)currentWeapon].ToString();
        // totalAmmoText.text = ammoInInventory[currentWeapon].ToString();
        totalAmmoText.text = playerController.gunController.currentWeaponData.clipSize.ToString();
        var ammoInInventory = playerController.ammoInInventory.ToString();
        // var ammoLimit = GameController.Instance.weaponMappings[(int)currentWeapon].inventoryLimit;
        ammoInInventoryText.text = $"{ammoInInventory} rounds in inventory";
        allAmmoPlayerText.text = getAmmoText(playerController);
        moneyAmountText.text = "";
    }

    public void SetIncidentalText(string interactionPrompt = null, bool instantShow = false)
    {
        Debug.Log(GameController.GetStackTrace());
        if (activeIncidentalTextGroup != null)
        {
            StopCoroutine(activeIncidentalTextGroup);
        }
        var interactionGroup = interactionPromptText.GetComponent<CanvasGroup>();
        if (interactionPrompt != null)
        {
            interactionPromptText.text = interactionPrompt;
        }
        if (!instantShow)
        {
            var group = PopUpGroup(interactionGroup, 0.1f, 2f, 1f);
            activeIncidentalTextGroup = StartCoroutine(group);
        }
        else
        {
            interactionGroup.alpha = 1;
        }
    }

    private GridLayoutGroup shopUI;
    private CanvasGroup hitMarker;

    public void ShowHideShop(bool show)
    {
        SetIncidentalText("");
        showShop = show;
        GenerateShopUI();
        shopUI.gameObject.SetActive(show);
        Cursor.visible = show;
        if (show)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    public void GenerateShopUI()
    {
        foreach (Transform child in shopUI.gameObject.transform)
        {
            if (!child.name.Contains("Exit"))
            {
                Destroy(child.gameObject);
            }
        }
        AddShopButton("Buy 5 Ammo", 50, delegate
        {
            playerController.ammoInInventory += 5;
        });
        AddShopButton("Buy 10 Health", 50, delegate
        {
            playerController.playerState.health += 20;
        });
    }

    private int yOffset = 0;

    private void AddShopButton(string title, int cost, UnityEngine.Events.UnityAction clickAction)
    {
        var button = Instantiate(shopButtonPrefab, new Vector3(0, yOffset, 20), Quaternion.identity, shopUI.gameObject.transform);
        var player = GameController.Instance.LocalPlayerInstance;
        if (player == null)
        {
            return;
        }
        var color = player.money >= cost ? Color.black : Color.gray;
        button.transform.Find("Text").GetComponent<Text>().text = $"{title}: ${cost}";
        button.transform.Find("Text").GetComponent<Text>().color = color;
        button.GetComponent<Button>().onClick.AddListener(delegate
        {
            if (player.money >= cost)
            {
                clickAction();
                player.money -= cost;
                ShowHideShop(false);
            }
        });
        yOffset += 40;
    }

    public IEnumerator HitMarker()
    {
        hitMarker.alpha = 1f;
        yield return new WaitForSeconds(0.1f);
        hitMarker.alpha = 0;
    }

    public IEnumerator PopUpGroup(CanvasGroup group, float fadeInDuration = 3f, float stayDuration = 15f, float fadeOutDuration = 3f)
    {
        var animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        var targetAlpha = 1f;
        float elapsed = 0;
        // fade in
        while (Mathf.Abs(group.alpha - targetAlpha) > 0.0001f)
        {
            group.alpha = animationCurve.Evaluate(elapsed / fadeInDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0;
        while (elapsed < stayDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        group.alpha = 1f;
        elapsed = 0;
        targetAlpha = 0.0f;
        while (Mathf.Abs(group.alpha - targetAlpha) > 0.0001f)
        {
            group.alpha = animationCurve.Evaluate(1 - elapsed / fadeOutDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

}