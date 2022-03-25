using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;
using UnityEngine.UI;

public enum SoundEffect
{
    IDLE,
    SHOOT,
    SHOOT_SHOTGUN,
    SHOOT_AR,
    SHOOT_AK,
    SHOOT_PISTOL,
    WALK,
    RUN,
    RELOAD,
    HEADSHOT_MARKER,
    HIT_MARKER,
    WAS_SHOT,
    PICKED_UP_WEAPON,
    PICKED_UP_AMMO,
}

public class GunController : MonoBehaviourPun
{

    //Animator component attached to weapon
    Animator anim;
    private Transform armsWeapon;

    [Header("Gun Camera")]
    //Main gun camera
    public Camera gunCamera;

    [Header("Gun Camera Options")]
    //How fast the camera field of view changes when aiming 
    [Tooltip("How fast the camera field of view changes when aiming.")]
    public float fovSpeed = 15.0f;
    //Default camera field of view
    [Tooltip("Default value for camera field of view (40 is recommended).")]
    public float defaultFov = 40.0f;


    public Transform armsRoot;
    public Transform bodyRoot;

    [Header("Weapon Attachments (Only use one scope attachment)")]
    [Space(10)]
    //Toggle weapon attachments (loads at start)
    //Toggle scope 01
    public bool scope1;
    public Sprite scope1Texture;
    public float scope1TextureSize = 0.0045f;
    //Scope 01 camera fov
    [Range(5, 40)]
    public float scope1AimFOV = 10;
    [Space(10)]
    //Toggle iron sights
    public bool ironSights;
    public bool alwaysShowIronSights;
    //Iron sights camera fov
    [Range(5, 40)]
    public float ironSightsAimFOV = 16;
    [Space(10)]
    //Toggle silencer
    public bool silencer;
    //Weapon attachments components
    [System.Serializable]
    public class weaponAttachmentRenderers
    {
        [Header("Scope Model Renderers")]
        [Space(10)]
        //All attachment renderer components
        public SkinnedMeshRenderer scope1Renderer;
        public SkinnedMeshRenderer ironSightsRenderer;
        public SkinnedMeshRenderer silencerRenderer;
        [Header("Scope Sight Mesh Renderers")]
        [Space(10)]
        //Scope render meshes
        public GameObject scope1RenderMesh;
        public GameObject scope2RenderMesh;
        public GameObject scope3RenderMesh;
        public GameObject scope4RenderMesh;
        [Header("Scope Sight Sprite Renderers")]
        [Space(10)]
        //Scope sight textures
        public SpriteRenderer scope1SpriteRenderer;
        public SpriteRenderer scope2SpriteRenderer;
        public SpriteRenderer scope3SpriteRenderer;
        public SpriteRenderer scope4SpriteRenderer;
    }
    public weaponAttachmentRenderers WeaponAttachmentRenderers;


    public List<AudioClip> reloadSounds = new List<AudioClip>();

    public Transform armatureWeapon;

    public GameObject headBone;
    public GameObject headCollider;


    [HideInInspector]
    public PlayerController playerController;

    // HACK until we sync player ammo count


    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float swaySmoothValue = 4.0f;

    private Vector3 initialSwayPosition;

    //Used for fire rate
    private float lastFired;
    [Header("Weapon Settings")]
    //Eanbles auto reloading when out of ammo
    [Tooltip("Enables auto reloading when out of ammo.")]
    public bool autoReload;
    //Delay between shooting last bullet and reloading
    public float autoReloadDelay;
    //Check if reloading
    private bool isReloading;

    //Holstering weapon
    private bool hasBeenHolstered = false;
    //If weapon is holstered
    private bool holstered;
    //Check if running
    private bool isRunning;
    //Check if aiming
    private bool isAiming;
    //Check if walking
    private bool isWalking;
    //Check if inspecting weapon
    private bool isInspecting;

    //Check if out of ammo
    private bool outOfAmmo;

    [Header("Bullet Settings")]
    //Bullet
    [Tooltip("How much force is applied to the bullet when shooting.")]
    public float bulletForce = .0f;
    [Tooltip("How long after reloading that the bullet model becomes visible " +
        "again, only used for out of ammo reload animations.")]
    public float showBulletInMagDelay = 0.6f;
    [Tooltip("The bullet model inside the mag, not used for all weapons.")]
    public SkinnedMeshRenderer bulletInMagRenderer;

    [Header("Grenade Settings")]
    public float grenadeSpawnDelay = 0.35f;

    [Header("Muzzleflash Settings")]
    public bool randomMuzzleflash = false;
    //min should always bee 1
    private int minRandomValue = 1;

    [Range(2, 25)]
    public int maxRandomValue = 5;

    private int randomMuzzleflashValue;

    public bool enableMuzzleflash = true;
    public ParticleSystem muzzleParticles;
    public bool enableSparks = true;
    public ParticleSystem sparkParticles;
    public int minSparkEmission = 1;
    public int maxSparkEmission = 7;

    [Header("Muzzleflash Light Settings")]
    public Light muzzleflashLight;
    public Transform armatureWeaponComponents;
    public float lightDuration = 0.05f;

    [Header("Audio Source")]
    //Main audio source
    public AudioSource mainAudioSource;
    //Audio source used for shoot sound
    public AudioSource hitMarkerAudioSource;
    public AudioSource weaponAudioSource;

    [System.Serializable]
    public class prefabs
    {
        [Header("Prefabs")]
        public Transform bulletPrefab;
        public Transform casingPrefab;
        public Transform grenadePrefab;
    }
    public prefabs Prefabs;

    [System.Serializable]
    public class spawnpoints
    {
        [Header("Spawnpoints")]
        //Array holding casing spawn points 
        //(some weapons use more than one casing spawn)
        //Casing spawn point array
        public Transform casingSpawnPoint;
        //Bullet prefab spawn from this point
        public Transform bulletSpawnPoint;

        public Transform grenadeSpawnPoint;
    }
    public spawnpoints Spawnpoints;



    public Dictionary<int, int> ammoInClip = new Dictionary<int, int>();

    public void InitAmmo(List<WeaponData> weaponDatas)
    {
        //Set current ammo to total ammo value
        ammoInClip[(int)currentWeapon] = currentWeaponData.clipSize;
        // HACK
        playerController.ammoInInventory = 0;
        foreach (var val in weaponDatas)
        {
            var initialAmmo = val.weaponType == WeaponType.PISTOL ? val.clipSize : 0;
            ammoInClip[(int)val.weaponType] = initialAmmo;
        }
    }


    [PunRPC]
    public void PlaySound(int soundIndex)
    {
        var audioSourceToUse = mainAudioSource;
        var soundEffect = (SoundEffect)soundIndex;
        switch (soundEffect)
        {
            case SoundEffect.IDLE:
            case SoundEffect.WALK:
            case SoundEffect.RUN:
                audioSourceToUse = mainAudioSource;
                break;
            case SoundEffect.SHOOT_AK:
            case SoundEffect.SHOOT_AR:
            case SoundEffect.SHOOT_PISTOL:
            case SoundEffect.SHOOT_SHOTGUN:
            case SoundEffect.SHOOT:
            case SoundEffect.RELOAD:
            case SoundEffect.PICKED_UP_AMMO:
            case SoundEffect.PICKED_UP_WEAPON:
                audioSourceToUse = weaponAudioSource;
                break;
            case SoundEffect.HEADSHOT_MARKER:
            case SoundEffect.HIT_MARKER:
                audioSourceToUse = hitMarkerAudioSource;
                break;
            default:
                audioSourceToUse = mainAudioSource;
                break;
        }
        if (soundEffect == SoundEffect.IDLE)
        {
            audioSourceToUse.Stop();
            return;
        }
        if (!soundEffectClips.ContainsKey(soundEffect))
        {
            Debug.Log($"did not find sound effect {soundEffect}");
            return;
        }
        audioSourceToUse.clip = soundEffectClips[soundEffect];
        if (soundEffect == SoundEffect.RELOAD)
        {
            var weaponSounds = new List<WeaponType>(new WeaponType[] { WeaponType.AK47, WeaponType.ASSAULT_RIFLE, WeaponType.PISTOL, WeaponType.SHOTGUN });
            audioSourceToUse.clip = reloadSounds[weaponSounds.IndexOf(currentWeapon)];
        }
        audioSourceToUse.Play();
    }



    public List<string> GetRoleDirectionText()
    {
        var directions = "";
        var team = (PlayerTeam)playerController.playerState.team;
        var teamText = "";
        var numTraitors = GameController.Instance.gameState.numTraitors;
        switch (team)
        {
            case PlayerTeam.DETECTIVE:
                teamText = "YOU ARE A DETECTIVE";
                break;
            case PlayerTeam.INNOCENT:
                teamText = "YOU ARE INNOCENT";
                break;
            case PlayerTeam.TRAITOR:
                teamText = "YOU ARE A TRAITOR";
                break;
        }
        if (team == PlayerTeam.DETECTIVE || team == PlayerTeam.INNOCENT)
        {
            var traitorsText = numTraitors > 1 ? $"{numTraitors} traitors are" : "traitor is";
            directions = $"There are traitors among you, but you don't know who they are! Find out who the {traitorsText}, and kill them, before they kill everyone else.";
        }
        if (team == PlayerTeam.DETECTIVE)
        {
            directions += "As detective, you can see what team a killed player was on by looking at their grave. No one else can see this!";
        }
        if (team == PlayerTeam.TRAITOR)
        {
            directions = $"Kill all non-traitors to win - you can see what team someone is on by looking at their name card, or pressing tab.";
            directions += " If the timer runs out, you lose.\n";
            // var otherTraitors = UIController.Instance.getOtherTraitorsText();
            // directions += otherTraitors;
            // directions += "\nIF THE OTHER PLAYERS COMPLETE THE OBJECTIVE, THE TIMER GETS REDUCED, GIVING YOU LESS TIME TO ACT.";
        }
        else
        {
            directions += "\nIf the timer runs out, you win.";
            // directions += "\nIF YOU COMPLETE THE OBJECTIVE, THE TIMER GETS REDUCED, BRINGING YOU CLOSER TO VICTORY.";
        }
        var objectives = new List<string>();
        objectives.Add(teamText);
        objectives.Add(directions);
        return objectives;
    }


    [HideInInspector]
    public Transform overheadUI;


    private Text overheadPlayerName;
    private Text overheadPlayerHealth;
    private Text overheadPlayerRole;
    public void Setup()
    {

        //Get weapon name from string to text
        //Set total ammo text from total ammo int
        // objectiveText.text = "";

        //Weapon sway
        initialSwayPosition = transform.localPosition;

        //Set the shoot sound to audio source
        this.overheadUI = this.transform.Find("Overhead UI");
        overheadPlayerHealth = this.overheadUI.Find("Health").GetComponent<Text>();
        overheadPlayerRole = this.overheadUI.Find("Role").GetComponent<Text>();
        overheadPlayerName = this.overheadUI.Find("Name").GetComponent<Text>();
        var playerCanvas = GameObject.Find("Player Canvas").transform;
        scoreboardRoot = playerCanvas.Find("Scoreboard").gameObject;
        scoreboardRoot.GetComponent<ScoreboardController>().localPlayerController = this.playerController;
        scoreboardRoot.SetActive(false);
        spectatorCamera = GameObject.Find("Spectator Camera").GetComponent<SpectatorCameraController>();
        spectatorCamera.StopLooking();
        if (playerController.isLocalPlayer())
        {
            weaponAudioSource.volume = 0.5f;
        }
    }

    public SpectatorCameraController spectatorCamera;

    private WeaponType currentWeapon;
    public WeaponData currentWeaponData;
    public void SwitchWeapons(Transform arms, WeaponType weaponType, WeaponData weaponMapping)
    {
        gunCamera.transform.SetParent(arms);
        gunCamera.transform.localRotation = Quaternion.identity;
        this.anim = arms.GetComponent<Animator>();
        this.currentWeapon = weaponType;
        this.currentWeaponData = weaponMapping;
        this.armatureWeapon = arms.Find("Armature").Find("weapon");
        this.armatureWeaponComponents = this.armatureWeapon.Find("Components");
        this.muzzleflashLight = this.armatureWeaponComponents.Find("Muzzleflash Light").GetComponent<Light>();
        var weaponPrefabName = this.GetPrefabName(playerController.currentWeaponType);
        this.armsWeapon = arms.Find("arms").Find(weaponPrefabName);
        var bulletInMagRendererObj = this.armsWeapon.Find("bullet");
        if (bulletInMagRendererObj == null)
        {
            bulletInMagRendererObj = this.armsWeapon.Find("big_bullet");
        }
        if (bulletInMagRendererObj == null)
        {
            bulletInMagRendererObj = this.armsWeapon.Find("shellcasing_full");
        }
        this.bulletInMagRenderer = bulletInMagRendererObj.GetComponent<SkinnedMeshRenderer>();
        this.sparkParticles = this.armatureWeaponComponents.Find("SparkParticles").GetComponent<ParticleSystem>();
        this.muzzleParticles = this.armatureWeaponComponents.Find("Muzzleflash Particles").GetComponent<ParticleSystem>();
        this.Spawnpoints.bulletSpawnPoint = this.armatureWeaponComponents.Find("Bullet Spawn Point");
        this.Spawnpoints.casingSpawnPoint = this.armatureWeaponComponents.Find("Casing Spawn Point");
        this.Spawnpoints.grenadeSpawnPoint = arms.Find("Grenade_Spawn_Point");
    }

    public GameObject GetWeaponRoot(WeaponType weaponType)
    {
        string prefabName = "";
        switch (weaponType)
        {
            case WeaponType.AK47:
                prefabName = "arms_assault_rifle_01";
                break;
            case WeaponType.ASSAULT_RIFLE:
                prefabName = "arms_assault_rifle_03";
                break;
            case WeaponType.PISTOL:
                prefabName = "arms_handgun_01";
                break;
            case WeaponType.SHOTGUN:
                prefabName = "arms_shotgun_01";
                break;
        }
        var children = GetComponentsInChildren<Animator>(true);
        foreach (var child in children)
        {
            if (child.gameObject.name == prefabName)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    public string GetPrefabName(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.SHOTGUN:
                return "shotgun_01";
            case WeaponType.PISTOL:
                return "handgun_01";
            case WeaponType.ASSAULT_RIFLE:
                return "assault_rifle_03";
            case WeaponType.AK47:
                return "assault_rifle_01";
            default:
                return "";
        }
    }

    private bool showScoreboard = false;

    private GameObject scoreboardRoot;

    private bool lookingAtObject;
    private bool lookingAtPlayer;


    private GameObject gunshotDecalPrefab;

    private Dictionary<int, SoundEffect> soundAnimationHashes = new Dictionary<int, SoundEffect>();
    private Dictionary<SoundEffect, AudioClip> soundEffectClips = new Dictionary<SoundEffect, AudioClip>();

    private GameObject pickupPrefab;

    private void Awake()
    {
        pickupPrefab = Resources.Load<GameObject>("Pickup");
        UIController.Instance = GameObject.Find("Player Canvas").GetComponent<UIController>();
        gunshotDecalPrefab = (GameObject)Resources.Load("GunshotDecal");
        playerController = GetComponent<PlayerController>();
        UIController.Instance.SetUIReferences(this.playerController);
        Prefabs.bulletPrefab = Resources.Load<GameObject>("Bullet_Prefab").transform;
        armsRoot = transform.Find("Arms");
        bodyRoot = transform.Find("Body");
        soundAnimationHashes.Add(81563449, SoundEffect.WALK);
        soundAnimationHashes.Add(-827840423, SoundEffect.RUN);
        soundAnimationHashes.Add(-50000, SoundEffect.SHOOT);
        soundAnimationHashes.Add(-50001, SoundEffect.WAS_SHOT);
        soundAnimationHashes.Add(-50002, SoundEffect.HIT_MARKER);
        soundAnimationHashes.Add(-50003, SoundEffect.HEADSHOT_MARKER);
        soundAnimationHashes.Add(-50004, SoundEffect.RELOAD);
        soundAnimationHashes.Add(-50005, SoundEffect.SHOOT_AK);
        soundAnimationHashes.Add(-50006, SoundEffect.SHOOT_AR);
        soundAnimationHashes.Add(-50007, SoundEffect.SHOOT_PISTOL);
        soundAnimationHashes.Add(-50008, SoundEffect.SHOOT_SHOTGUN);
        soundAnimationHashes.Add(-50009, SoundEffect.PICKED_UP_AMMO);
        soundAnimationHashes.Add(-50010, SoundEffect.PICKED_UP_WEAPON);
        soundAnimationHashes.Add(1432961145, SoundEffect.IDLE);
        var soundEffects = Resources.LoadAll("Sounds");
        var soundNames = soundAnimationHashes.Values.Select(i => i.ToString());
        foreach (AudioClip sound in soundEffects)
        {
            if (soundNames.Contains(sound.name))
            {
                soundEffectClips.Add((SoundEffect)System.Enum.Parse(typeof(SoundEffect), sound.name), sound);
            }
        }
    }

    private int currentAnimationStateNameHash;
    private float objectiveColorCountdown = 0.5f;

    void FixedUpdate()
    {
        if (headBone != null)
        {
            headCollider.transform.position = headBone.transform.position;
        }
    }

    private void Update()
    {
        Debug.Log(GameController.GetStackTrace());
        // this should only happen if we've just joined
        if (GameController.Instance == null)
        {
            return;
        }
        if (GameController.Instance.LocalPlayerInstance == null)
        {
            return;
        }
        if (!playerController.isLocalPlayer() && overheadUI != null && GameController.Instance != null && GameController.Instance.LocalPlayerInstance != null)
        {
            var relativePos = GameController.Instance.LocalPlayerInstance.transform.position - transform.position;
            var lookAtRot = Quaternion.LookRotation(relativePos, Vector3.up);
            overheadUI.transform.rotation = Quaternion.Euler(0, lookAtRot.eulerAngles.y + 180, 0);
        }

        if (!playerController.isSetup)
        {
            return;
        }

        var currentState = anim.GetCurrentAnimatorStateInfo(0);
        if (currentState.fullPathHash != currentAnimationStateNameHash)
        {
            if (soundAnimationHashes.ContainsKey(currentState.fullPathHash))
            {
                photonView.RPC("PlaySound", RpcTarget.All, (int)soundAnimationHashes[currentState.fullPathHash]);
            }
            currentAnimationStateNameHash = currentState.fullPathHash;
        }

        // all local player logic below this line
        if ((!photonView.IsMine && PhotonNetwork.IsConnected == true) || !playerController.isLocalPlayer())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            Application.Quit();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            playerController.ammoInInventory = 1000;
        }

        // INPUT

        if (!playerController.playerState.ready && Input.GetKeyDown(KeyCode.H))
        {
            GameController.Instance.photonView.RPC("PlayerReadiedUp", RpcTarget.All);
            playerController.playerState.ready = true;
            UIController.Instance.UpdateWaitingText(this.playerController);
        }


        if (Input.GetKeyDown(KeyCode.Tab))
        {
            scoreboardRoot.SetActive(true);
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            scoreboardRoot.SetActive(false);
        }

        if (anim == null)
        {
            return;
        }

        var isInMatch = (GameStatus)GameController.Instance.gameState.gameStatus == GameStatus.PLAYING;

        // ANIMATION DEPENDENT INPUT
        //Aiming
        //Toggle camera FOV when right click is held down
        if (Input.GetMouseButton(1) && !isReloading && !isRunning && !isInspecting && !UIController.Instance.showExitConfirm && !UIController.Instance.showShop && isInMatch)
        {
            if (ironSights == true)
            {
                gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
                    ironSightsAimFOV, fovSpeed * Time.deltaTime);
            }
            if (scope1 == true)
            {
                gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
                    scope1AimFOV, fovSpeed * Time.deltaTime);
            }

            isAiming = true;

            //If iron sights are enabled, use normal aim
            if (ironSights == true)
            {
                anim.SetBool("Aim", true);
            }
            //If scope 1 is enabled, use scope 1 aim in animation
            if (scope1 == true)
            {
                anim.SetBool("Aim Scope 1", true);
            }

            //If scope 1 is true, show scope sight texture when aiming
            if (scope1 == true)
            {
                WeaponAttachmentRenderers.scope1SpriteRenderer.GetComponent
                    <SpriteRenderer>().enabled = true;
            }
        }
        else
        {
            //When right click is released
            gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
                defaultFov, fovSpeed * Time.deltaTime);

            isAiming = false;

            //If iron sights are enabled, use normal aim out
            if (ironSights == true)
            {
                anim.SetBool("Aim", false);
            }
            //If scope 1 is enabled, use scope 1 aim out animation
            if (scope1 == true)
            {
                anim.SetBool("Aim Scope 1", false);
            }

            //If scope 1 is true, disable scope sight texture when not aiming
            if (scope1 == true)
            {
                WeaponAttachmentRenderers.scope1SpriteRenderer.GetComponent
                    <SpriteRenderer>().enabled = false;
            }
        }
        //Aiming end

        //If randomize muzzleflash is true, genereate random int values
        if (randomMuzzleflash == true)
        {
            randomMuzzleflashValue = Random.Range(minRandomValue, maxRandomValue);
        }

        UIController.Instance.UpdatePlayerUIText(this.playerController);

        if (isReloading || waitingToReload)
        {
            AnimationCheck();
        }

        // //Play knife attack 1 animation when Q key is pressed
        // if (Input.GetKeyDown(KeyCode.Q) && !isInspecting)
        // {
        //     anim.Play("Knife Attack 1", 0, 0f);
        // }
        // //Play knife attack 2 animation when F key is pressed
        // if (Input.GetKeyDown(KeyCode.F) && !isInspecting)
        // {
        //     anim.Play("Knife Attack 2", 0, 0f);
        // }

        // //Throw grenade when pressing G key
        // if (Input.GetKeyDown(KeyCode.G) && !isInspecting)
        // {
        //     StartCoroutine(GrenadeSpawnDelay());
        //     //Play grenade throw animation
        //     anim.Play("GrenadeThrow", 0, 0.0f);
        // }

        //If out of ammo
        if (ammoInClip[(int)currentWeapon] == 0)
        {
            //Toggle bool
            outOfAmmo = true;
            //Auto reload if true
            if (autoReload == true && !isReloading)
            {
                StartCoroutine(AutoReload());
            }
        }
        else
        {

            //Toggle bool
            outOfAmmo = false;
            //anim.SetBool ("Out Of Ammo", false);
        }

        // pickup check
        var pos = Spawnpoints.bulletSpawnPoint.transform.position;
        var rot = Spawnpoints.bulletSpawnPoint.transform.forward;
        RaycastHit hit;
        if (Physics.Raycast(pos, rot, out hit, Mathf.Infinity))
        {
            var playing = (GameStatus)GameController.Instance.gameState.gameStatus == GameStatus.PLAYING;
            var pickup = hit.transform.gameObject.GetComponent<PickupController>();
            var player = hit.transform.gameObject.GetComponent<PlayerController>();
            if (hit.transform.gameObject.name.Contains("Shop") && hit.distance <= 3)
            {
                lookingAtObject = true;
                UIController.Instance.SetIncidentalText("Press E to use shop", true);
                if (Input.GetKeyDown(KeyCode.E) && playing)
                {
                    UIController.Instance.ShowHideShop(true);
                }
            }
            else if (pickup != null && hit.distance <= 3)
            {
                var label = pickup.Label();
                var hasMaxGadgets = pickup.pickupType == PickupType.GADGET && playerController.gadgets.Count >= 3;

                var text = playing ? $"Press E to pick up {label}" : "Cannot pick up until round starts";
                var swapping = pickup.pickupType == PickupType.WEAPON && playerController.hasPrimaryWeapon;
                if (playerController.hasPrimaryWeapon && !swapping && pickup.pickupType == PickupType.WEAPON && pickup.weaponType == this.playerController.currentWeaponType)
                {
                    text = "You already have this weapon - take 10 ammo.";
                }
                if (swapping)
                {
                    text = $"Press E to swap {label} with {playerController.currentWeaponType}";
                }
                UIController.Instance.SetIncidentalText(text, true);
                lookingAtObject = true;
                if (Input.GetKeyDown(KeyCode.E) && playing && !hasMaxGadgets)
                {
                    // client side logic
                    pickup.PickedUp(this.playerController, swapping, playerController.currentWeaponType);
                    UIController.Instance.UpdatePlayerUIText(this.playerController);
                    // replication - need new weapon type to be consistent
                    var newIndex = UnityEngine.Random.Range(0, GameController.Instance.pickupDistributions.Count - 1);
                    playerController.hasPrimaryWeapon = true;
                    if (pickup.pickupType == PickupType.AMMO)
                    {
                        PlaySound((int)SoundEffect.PICKED_UP_AMMO);
                    }
                    else
                    {
                        PlaySound((int)SoundEffect.PICKED_UP_WEAPON);
                    }
                    pickup.photonView.RPC("PlayerPickedUp", RpcTarget.All, pickup.showBeacon, newIndex);
                }
            }
            else if (player != null && player.playerState.name != playerController.playerState.name)
            {
                UIController.Instance.playerNameHoverText.text = $"{player.playerState.name} - {player.getVisibleRole()}";
                lookingAtPlayer = true;
            }
            else if (lookingAtObject)
            {
                lookingAtObject = false;
                UIController.Instance.SetIncidentalText("");
            }
            else if (lookingAtPlayer)
            {
                UIController.Instance.playerNameHoverText.text = "";
                lookingAtPlayer = false;
            }
        }

        //Automatic fire
        var hasAmmoInInventory = this.playerController.ammoInInventory > 0;
        //Left click hold 
        if (Input.GetMouseButton(0) && !isReloading && !isInspecting && !UIController.Instance.showExitConfirm && !UIController.Instance.showShop && playerController.playerState.alive)
        {
            if (!isInMatch)
            {
                UIController.Instance.SetIncidentalText("Can't shoot until the round starts!");
            }
            else if (outOfAmmo)
            {
                if (!hasAmmoInInventory)
                {
                    UIController.Instance.SetIncidentalText("No more ammo for this weapon!");
                }
                else
                {
                    UIController.Instance.SetIncidentalText("Press R to reload");
                }
            }
            else if (isRunning)
            {
                UIController.Instance.SetIncidentalText("Stop running to shoot!");
            }
            //Shoot automatic
            else if (Time.time - lastFired > 1 / currentWeaponData.fireRate)
            {
                lastFired = Time.time;

                //Remove 1 bullet from ammo
                ammoInClip[(int)currentWeapon] -= 1;

                var shootSound = SoundEffect.SHOOT_PISTOL;
                switch (currentWeapon)
                {
                    case WeaponType.SHOTGUN:
                        shootSound = SoundEffect.SHOOT_SHOTGUN;
                        break;
                    case WeaponType.AK47:
                        shootSound = SoundEffect.SHOOT_AK;
                        break;
                    case WeaponType.ASSAULT_RIFLE:
                        shootSound = SoundEffect.SHOOT_AR;
                        break;
                }

                photonView.RPC("PlaySound", RpcTarget.All, (int)shootSound);

                if (!isAiming) //if not aiming
                {
                    anim.Play("Fire", 0, 0f);
                    //If random muzzle is false
                    if (!randomMuzzleflash &&
                        enableMuzzleflash == true && !silencer)
                    {
                        muzzleParticles.Emit(1);
                        //Light flash start
                        StartCoroutine(MuzzleFlashLight());
                    }
                    else if (randomMuzzleflash == true)
                    {
                        //Only emit if random value is 1
                        if (randomMuzzleflashValue == 1)
                        {
                            if (enableSparks == true)
                            {
                                //Emit random amount of spark particles
                                sparkParticles.Emit(Random.Range(minSparkEmission, maxSparkEmission));
                            }
                            if (enableMuzzleflash == true && !silencer)
                            {
                                muzzleParticles.Emit(1);
                                //Light flash start
                                StartCoroutine(MuzzleFlashLight());
                            }
                        }
                    }
                }
                else //if aiming
                {
                    if (ironSights == true)
                    {
                        anim.Play("Aim Fire", 0, 0f);
                    }
                    if (scope1 == true)
                    {
                        anim.Play("Aim Fire Scope 1", 0, 0f);
                    }
                    //If random muzzle is false
                    if (!randomMuzzleflash && !silencer)
                    {
                        muzzleParticles.Emit(1);
                        //If random muzzle is true
                    }
                    else if (randomMuzzleflash == true)
                    {
                        //Only emit if random value is 1
                        if (randomMuzzleflashValue == 1)
                        {
                            if (enableSparks == true)
                            {
                                //Emit random amount of spark particles
                                sparkParticles.Emit(Random.Range(minSparkEmission, maxSparkEmission));
                            }
                            if (enableMuzzleflash == true && !silencer)
                            {
                                muzzleParticles.Emit(1);
                                //Light flash start
                                StartCoroutine(MuzzleFlashLight());
                            }
                        }
                    }
                }


                // all non LPFS shoot logic goes here 
                var bulletPoint = gunCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                //Spawn bullet from bullet spawnpoint
                var bulletPos = bulletPoint.origin + (bulletPoint.direction * 1);
                var sprayOffset = setGunSprayVector(true);
                if (playerController.currentWeaponType == WeaponType.SHOTGUN)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        sprayOffset = setGunSprayVector();
                        var shotOffsetRay = new Ray(bulletPoint.origin, bulletPoint.direction + sprayOffset * 0.05f);
                        ShootCheck(shotOffsetRay, bulletPos);
                        var bullet = (Transform)Instantiate(
                            Prefabs.bulletPrefab,
                            Spawnpoints.bulletSpawnPoint.transform.position,
                            Quaternion.Euler(shotOffsetRay.direction));

                        //Add velocity to the bullet
                        bullet.GetComponent<Rigidbody>().velocity =
                            shotOffsetRay.direction * bulletForce;
                    }
                }
                ShootCheck(bulletPoint, bulletPos);
                photonView.RPC("PlayerShoot", RpcTarget.All);

                sprayOffset = setGunSprayVector(true);
                playerController.firstPersonArms.Rotate(sprayOffset);
            }
        }


        //Inspect weapon when T key is pressed
        if (Input.GetKeyDown(KeyCode.T))
        {
            anim.SetTrigger("Inspect");
        }

        //Reload 
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && !isInspecting)
        {
            //Reload
            Reload();
        }

        //Walking when pressing down WASD keys
        if (Input.GetKey(KeyCode.W) && !isRunning ||
            Input.GetKey(KeyCode.A) && !isRunning ||
            Input.GetKey(KeyCode.S) && !isRunning ||
            Input.GetKey(KeyCode.D) && !isRunning)
        {
            anim.SetBool("Walk", true);
        }
        else
        {
            anim.SetBool("Walk", false);
        }

        //Running when pressing down W and Left Shift key
        if ((Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.LeftShift)))
        {
            isRunning = true;
        }
        else
        {
            isRunning = false;
        }

        //Run anim toggle
        if (isRunning == true && !anim.GetBool("Run"))
        {
            anim.SetBool("Run", true);
        }
        else if (isRunning == false && anim.GetBool("Run"))
        {
            anim.SetBool("Run", false);
        }
    }

    public Vector3 setGunSprayVector(bool scaleVertically = false)
    {
        var forwardRotation = Spawnpoints.bulletSpawnPoint.transform.forward;
        var randomHemi = Random.onUnitSphere;

        if (scaleVertically)
        {
            randomHemi = Vector3.Scale(randomHemi, new Vector3(1f, 0.25f, 0));
        }
        var weaponSprayAmount = Vector2.zero;
        // TODO make this a dict
        foreach (var weapon in GameController.Instance.weaponMappings)
        {
            if (weapon.weaponType == currentWeapon)
            {
                weaponSprayAmount = weapon.sprayMinMax;
            }
        }

        var amt = Random.Range(weaponSprayAmount.x, weaponSprayAmount.y);
        randomHemi *= amt / 10;

        if (scaleVertically)
        {
            randomHemi.x = -Mathf.Abs(randomHemi.x);
        }

        return randomHemi;
    }

    // this is bad, but we need to network the bullet shot somehow
    [PunRPC]
    public void PlayerShoot()
    {
        anim.Play("Fire", 0, 0f);
        // all non LPFS shoot logic goes here 
        var bulletPoint = gunCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        //Spawn bullet from bullet spawnpoint
        var bulletPos = bulletPoint.origin + (bulletPoint.direction * 1);
        var bullet = (Transform)Instantiate(
            Prefabs.bulletPrefab,
            Spawnpoints.bulletSpawnPoint.transform.position,
            Quaternion.Euler(bulletPoint.direction));

        //Add velocity to the bullet
        bullet.GetComponent<Rigidbody>().velocity =
            bulletPoint.direction * bulletForce;
    }

    [PunRPC]
    public void PlayerReload()
    {
        anim.Play("Reload Ammo Left", 0, 0f);
    }


    private void ShootCheck(Ray bulletPoint, Vector3 bulletPos)
    {
        RaycastHit hit;
        Debug.DrawRay(bulletPos, bulletPoint.direction, Color.yellow, 1);
        if (Physics.Raycast(bulletPos, bulletPoint.direction, out hit, Mathf.Infinity))
        {
            // render bullet hole
            var collider = hit.collider.gameObject.GetComponent<MeshCollider>();
            if (collider != null)
            {
                var hitMesh = hit.collider.gameObject.GetComponent<MeshCollider>().sharedMesh;
                var decal = Instantiate(gunshotDecalPrefab, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
                GameController.Instance.roundObjectsToDestroy.Add(decal);
            }
            var shotPlayer = hit.collider.gameObject.transform.root.GetComponentInChildren<PlayerController>();
            if (shotPlayer != null && shotPlayer.photonView.Owner.ActorNumber != playerController.photonView.Owner.ActorNumber)
            {
                PhotonView shotPlayerView = hit.transform.root.GetComponent<PhotonView>();
                var isHeadshot = hit.collider.gameObject.name == "HeadCollider";
                shotPlayerView.RPC("ShootPlayer", RpcTarget.All, (int)playerController.currentWeaponType, isHeadshot, this.photonView.Owner.ActorNumber);
                StartCoroutine(UIController.Instance.HitMarker());
                if (isHeadshot)
                {
                    PlaySound((int)SoundEffect.HEADSHOT_MARKER);
                }
                else
                {

                    PlaySound((int)SoundEffect.HIT_MARKER);
                }


            }
        }
    }


    public void UpdateOverheadUI()
    {
        if (!playerController.playerState.alive)
        {
            return;
        }
        overheadPlayerName.text = playerController.playerState.name;
        var visibleRole = playerController.getVisibleRole();
        overheadPlayerHealth.text = $"{playerController.playerState.health}HP";
        overheadPlayerRole.text = visibleRole;
    }

    [PunRPC]
    public void KillPlayer(int weaponTypeInt, int attackerID)
    {
        var attackingPlayer = GameController.Instance.playerReferences[attackerID];
        var weaponType = (WeaponType)weaponTypeInt;
        playerController.playerState.alive = false;
        var localPlayerIsDetective = (PlayerTeam)GameController.Instance.LocalPlayerInstance.playerState.team == PlayerTeam.DETECTIVE;
        var team = playerController.playerState.team == (int)PlayerTeam.TRAITOR ? "a traitorous scoundrel" : "an innocent man";
        var deathText = $"Here Lies {playerController.playerState.name}";
        if (localPlayerIsDetective)
        {
            // deathText += $",\nkilled with a {weaponType.ToString()}";
            deathText += $",\n {team}";
        }
        overheadPlayerRole.text = deathText;
        overheadPlayerName.text = "";
        overheadPlayerHealth.text = "";
        var death = transform.Find("Death").gameObject;
        death.SetActive(true);
        playerController.firstPersonArms.gameObject.SetActive(false);
        armsRoot.gameObject.SetActive(false);
        bodyRoot.gameObject.SetActive(false);
        overheadUI.localPosition = new Vector3(overheadUI.localPosition.x, 1, overheadUI.localPosition.z);
        if (photonView.IsMine)
        {
            var killerDescription = (PlayerTeam)attackingPlayer.playerState.team == PlayerTeam.TRAITOR ? "a Traitor" : "an Innocent";
            var subtitle = $"By {attackingPlayer.playerState.name}, {killerDescription}";
            if ((GameStatus)GameController.Instance.gameState.gameStatus != GameStatus.WON)
            {

                UIController.Instance.ShowPopUpText("YOU WERE KILLED", subtitle);
            }
            spectatorCamera.transform.position = new Vector3(-25, 5, 120);
            spectatorCamera.transform.rotation = Quaternion.Euler(new Vector3(0, -90, 0));
            spectatorCamera.GetComponent<SpectatorCameraController>().StartLooking();
        }
    }

    // reset player state for the next round
    [PunRPC]
    public void RevivePlayer()
    {
        playerController.playerState.alive = true;
        playerController.playerState.teamAssigned = false;
        playerController.playerState.health = 100;
        InitAmmo(GameController.Instance.weaponMappings);
        playerController.playerState.ready = false;
        var death = transform.Find("Death").gameObject;
        death.SetActive(false);
        armsRoot.gameObject.SetActive(true);
        playerController.firstPersonArms.gameObject.SetActive(true);
        playerController.transform.Find("Body").gameObject.SetActive(true);
        playerController.gadgets = new List<GadgetType>();
        playerController.money = 0;
        overheadUI.localPosition = new Vector3(overheadUI.localPosition.x, 2, overheadUI.localPosition.z);
        playerController.SwitchWeapons(WeaponType.PISTOL);
        UpdateOverheadUI();
    }

    private IEnumerator GrenadeSpawnDelay()
    {

        //Wait for set amount of time before spawning grenade
        yield return new WaitForSeconds(grenadeSpawnDelay);
        //Spawn grenade prefab at spawnpoint
        Instantiate(Prefabs.grenadePrefab,
            Spawnpoints.grenadeSpawnPoint.transform.position,
            Spawnpoints.grenadeSpawnPoint.transform.rotation);
    }

    private IEnumerator AutoReload()
    {
        //Wait set amount of time
        yield return new WaitForSeconds(autoReloadDelay);

        if (outOfAmmo == true)
        {
            //Play diff anim if out of ammo
            anim.Play("Reload Out Of Ammo", 0, 0f);

            PlaySound((int)SoundEffect.RELOAD);

            //If out of ammo, hide the bullet renderer in the mag
            //Do not show if bullet renderer is not assigned in inspector
            if (bulletInMagRenderer != null)
            {
                bulletInMagRenderer.GetComponent
                <SkinnedMeshRenderer>().enabled = false;
                //Start show bullet delay
                StartCoroutine(ShowBulletInMag());
            }
        }
        //Restore ammo when reloading
        ammoInClip[(int)currentWeapon] = currentWeaponData.clipSize;
        this.playerController.ammoInInventory -= currentWeaponData.clipSize;

        outOfAmmo = false;
    }

    private void ReloadAmmo()
    {

        var ammoInInventory = this.playerController.ammoInInventory;
        var newAmmoAmount = Mathf.Clamp(ammoInInventory + ammoInClip[(int)currentWeapon], 0, currentWeaponData.clipSize);
        var ammoDiff = newAmmoAmount - ammoInClip[(int)currentWeapon];
        ammoInClip[(int)currentWeapon] = newAmmoAmount;
        this.playerController.ammoInInventory -= ammoDiff;
    }
    private bool waitingToReload = false;
    //Reload
    private void Reload()
    {

        if (this.playerController.ammoInInventory < 1)
        {
            UIController.Instance.SetIncidentalText("No more ammo to reload with!");
            return;
        }

        // if has ammo available and can reload / needs to
        if (ammoInClip[(int)currentWeapon] < currentWeaponData.clipSize)
        {
            waitingToReload = true;

            // play animation
            if (outOfAmmo == true)
            {
                //Play diff anim if out of ammo
                photonView.RPC("PlayerReload", RpcTarget.All);


                //If out of ammo, hide the bullet renderer in the mag
                //Do not show if bullet renderer is not assigned in inspector
                if (bulletInMagRenderer != null)
                {
                    bulletInMagRenderer.GetComponent
                    <SkinnedMeshRenderer>().enabled = false;
                    //Start show bullet delay
                    StartCoroutine(ShowBulletInMag());
                }
            }
            else
            {
                //Play diff anim if ammo left
                photonView.RPC("PlayerReload", RpcTarget.All);

                PlaySound((int)SoundEffect.RELOAD);

                //If reloading when ammo left, show bullet in mag
                //Do not show if bullet renderer is not assigned in inspector
                if (bulletInMagRenderer != null)
                {
                    bulletInMagRenderer.GetComponent
                    <SkinnedMeshRenderer>().enabled = true;
                }
            }
        }
        outOfAmmo = false;
    }

    //Enable bullet in mag renderer after set amount of time
    private IEnumerator ShowBulletInMag()
    {

        //Wait set amount of time before showing bullet in mag
        yield return new WaitForSeconds(showBulletInMagDelay);
        bulletInMagRenderer.GetComponent<SkinnedMeshRenderer>().enabled = true;
    }

    //Show light when shooting, then disable after set amount of time
    private IEnumerator MuzzleFlashLight()
    {

        muzzleflashLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleflashLight.enabled = false;
    }

    //Check current animation playing
    private void AnimationCheck()
    {

        //Check if reloading
        //Check both animations
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Reload Out Of Ammo") ||
            anim.GetCurrentAnimatorStateInfo(0).IsName("Reload Ammo Left") ||
            anim.GetCurrentAnimatorStateInfo(0).IsName("Insert Shell 1") ||
            anim.GetCurrentAnimatorStateInfo(0).IsName("Insert Shell"))
        {
            isReloading = true;
        }
        else
        {
            isReloading = false;
            if (waitingToReload)
            {
                waitingToReload = false;
                ReloadAmmo();
            }
        }

        //Check if inspecting weapon
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Inspect"))
        {
            isInspecting = true;
        }
        else
        {
            isInspecting = false;
        }
    }
}
