using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Profiling;

public enum WeaponType
{
    SHOTGUN,
    PISTOL,
    AK47,
    ASSAULT_RIFLE
}


public enum PlayerTeam
{
    INNOCENT,
    TRAITOR,
    DETECTIVE,
    SPECTATOR
}

[Serializable]
public struct PlayerState
{
    public bool ready;
    public string name;
    // PlayerTeam
    public int team;
    public bool teamAssigned;
    public int health;

    // AmmoType
    // WeaponType
    public bool alive;
    public int colorIndex;
    public int score;
}

[Serializable]
public struct WeaponData
{
    public WeaponType weaponType;
    public float fireRate;
    public bool automatic;
    public bool spray;
    public int clipSize;
    public int inventoryLimit;
    public int damagePerShot;
    public KeyCode keyBinding;
    public Vector2 sprayMinMax;
    public WeaponData(WeaponType weaponType, KeyCode keyBinding, int damagePerShot, int clipSize, bool spray, bool automatic, float fireRate, Vector2 sprayAmount)
    {
        this.keyBinding = keyBinding;
        this.damagePerShot = damagePerShot;
        this.inventoryLimit = 100000;
        this.clipSize = clipSize;
        this.spray = spray;
        this.automatic = automatic;
        this.fireRate = fireRate;
        this.weaponType = weaponType;
        this.sprayMinMax = sprayAmount;
    }
}

public enum GadgetType
{
    HEALTH_SHOT,
    SLOW_FIELD
}

public class PlayerController : MonoBehaviourPun, IPunObservable
{


    public PlayerState playerState;
    public int money;

    // the current gun rig root
    public Transform firstPersonArms;

    [Tooltip("The position of the arms and gun camera relative to the fps controller GameObject."), SerializeField]
    private Vector3 armPositionOffset;

    [Header("Audio Clips")]
    [Tooltip("The audio clip that is played while walking."), SerializeField]
    private AudioClip walkingSound;

    [Tooltip("The audio clip that is played while running."), SerializeField]
    private AudioClip runningSound;

    [Header("Movement Settings")]
    [Tooltip("How fast the player moves while walking and strafing."), SerializeField]
    private float walkingSpeed = 5f;

    [Tooltip("How fast the player moves while running."), SerializeField]
    private float runningSpeed = 9f;

    [Tooltip("Approximately the amount of time it will take for the player to reach maximum running or walking speed."), SerializeField]

    private float jumpForce = 10f;

    [Tooltip("Minimum rotation of the arms and camera on the x axis."),
     SerializeField]
    private float minVerticalAngle = -90f;

    [Tooltip("Maximum rotation of the arms and camera on the axis."),
     SerializeField]
    private float maxVerticalAngle = 90f;

#pragma warning restore 649


    public bool isSetup = false;

    private Rigidbody playerRigidbody;
    private CapsuleCollider capsuleCollider;
    private AudioSource _audioSource;
    [HideInInspector]
    public bool _isGrounded = true;

    private readonly RaycastHit[] _groundCastResults = new RaycastHit[8];
    private readonly RaycastHit[] _wallCastResults = new RaycastHit[8];

    [HideInInspector]
    public GunController gunController;


    public int ammoInInventory;

    public WeaponType primaryWeapon;
    public bool hasPrimaryWeapon;
    public List<GadgetType> gadgets = new List<GadgetType>();
    private Transform armsRoot;
    private Transform bodyRoot;


    public bool isLocalPlayer()
    {
        // either the controlling character and online, or isn't a bot in offline mode
        return photonView.IsMine && PhotonNetwork.IsConnected;
    }

    public void Awake()
    {
        armsRoot = transform.Find("Arms");
        bodyRoot = transform.Find("Body");
        slowFieldPrefab = Resources.Load<GameObject>("SlowField");
    }

    void Start()
    {
        var playerIndex = photonView.Owner.ActorNumber;
        SetupPlayer();
        gunController.Setup();
        SwitchWeapons(WeaponType.PISTOL);
        GameController.Instance.playerReferences[playerIndex] = this;
    }

    private GameObject slowFieldPrefab;

    public PlayerInput input;

    [PunRPC]
    public void SetTeam(int team)
    {
        this.playerState.team = team;
        this.playerState.teamAssigned = true;
        UIController.Instance.UpdateWaitingText(this);
        if (gunController == null || !isSetup)
        {
            return;
        }
        var text = gunController.GetRoleDirectionText();
        if (photonView.IsMine && isLocalPlayer())
        {
            UIController.Instance.ShowPopUpText(text[0], text[1], 10f);
        }
    }


    public void SetupPlayer()
    {
        playerState.name = photonView.Owner.NickName;
        playerState.teamAssigned = false;
        gunController = GetComponent<GunController>();
        playerRigidbody = GetComponent<Rigidbody>();
        if (isLocalPlayer())
        {
            playerRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            capsuleCollider = GetComponent<CapsuleCollider>();
        }
        else
        {
            Destroy(playerRigidbody);
        }
        _audioSource = GetComponent<AudioSource>();
        firstPersonArms = AssignCharactersCamera();
        _audioSource.clip = walkingSound;
        _audioSource.loop = true;
        Cursor.lockState = CursorLockMode.Locked;
        ValidateRotationRestriction();
        print($"set up {playerState.name}");

        gunController.playerController = this;
        gunController.InitAmmo(GameController.Instance.weaponMappings);
        if (isLocalPlayer())
        {
            GameController.Instance.LocalPlayerInstance = this;
        }
        else
        {
            // SetColor(this.playerColor);
            // render player body if not the local player
            var fpc = this.gameObject.transform.Find("Body").Find("third_person_character");
            fpc.gameObject.layer = 0;
            fpc.Find("backpack").gameObject.layer = 0;
            fpc.Find("headset").gameObject.layer = 0;
            fpc.Find("helmet").gameObject.layer = 0;
            var camera = transform.GetComponentInChildren<Camera>();
            camera.enabled = false;
            gunController.armsRoot.Find("Camera").GetComponent<AudioListener>().enabled = false;
            armPositionOffset = new Vector3(0, 1.22f, 0.34f);
        }
        // DontDestroyOnLoad(this.gameObject);

        isSetup = true;
    }

    public string getVisibleRole()
    {
        if (!isSetup || GameController.Instance.LocalPlayerInstance == null)
        {
            return "ROLE NOT AVAILABLE";
        }
        var localPlayerTeam = (PlayerTeam)GameController.Instance.LocalPlayerInstance.playerState.team;
        var playerTeam = (PlayerTeam)playerState.team;
        var visibleRole = playerTeam == PlayerTeam.DETECTIVE ? "Detective" : "Role Unknown";
        if (playerTeam == PlayerTeam.TRAITOR && localPlayerTeam == PlayerTeam.TRAITOR)
        {
            visibleRole = "Fellow Traitor";
        }
        if (playerTeam == PlayerTeam.INNOCENT && localPlayerTeam == PlayerTeam.TRAITOR)
        {
            visibleRole = "Innocent";
        }
        return visibleRole;
    }


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        Debug.Log(GameController.GetStackTrace());
        if (stream.IsWriting)
        {
            // Input
            stream.SendNext(this.input.Move);
            stream.SendNext(this.input.Strafe);
            stream.SendNext(this.input.RotateX);
            stream.SendNext(this.input.RotateY);
            stream.SendNext(this.input.Jump);
            stream.SendNext(this.input.Run);
            // State
            stream.SendNext(this.playerState.alive);
            stream.SendNext(this.playerState.health);
            stream.SendNext(this.playerState.name);
            stream.SendNext(this.playerState.ready);
            stream.SendNext(this.playerState.team);
            stream.SendNext(this.playerState.colorIndex);
            stream.SendNext(this.playerState.score);
            stream.SendNext(this.armRotation);
        }
        else
        {
            this.oldArmRotation = this.armRotation;
            // Input
            this.input.Move = (float)stream.ReceiveNext();
            this.input.Strafe = (float)stream.ReceiveNext();
            this.input.RotateX = (float)stream.ReceiveNext();
            this.input.RotateY = (float)stream.ReceiveNext();
            this.input.Jump = (bool)stream.ReceiveNext();
            this.input.Run = (bool)stream.ReceiveNext();
            // State
            this.playerState.alive = (bool)stream.ReceiveNext();
            this.playerState.health = (int)stream.ReceiveNext();
            this.playerState.name = (string)stream.ReceiveNext();
            this.playerState.ready = (bool)stream.ReceiveNext();
            this.playerState.team = (int)stream.ReceiveNext();
            this.playerState.colorIndex = (int)stream.ReceiveNext();
            this.playerState.score = (int)stream.ReceiveNext();
            this.armRotation = (Vector3)stream.ReceiveNext();
            armsLerpTime = Time.time + Time.fixedDeltaTime;
        }
    }


    private float armsLerpTime = 0;
    private Vector3 oldArmRotation = Vector3.zero;

    [PunRPC]
    // refers to shooting this player
    public void ShootPlayer(int weaponTypeInt, bool isHeadshot, int attackerID)
    {
        var weaponType = (WeaponType)weaponTypeInt;
        if ((GameStatus)GameController.Instance.gameState.gameStatus != GameStatus.PLAYING)
        {
            return;
        }
        if (playerState.alive == false)
        {
            return;
        }
        var damageAmount = GameController.Instance.weaponMappings[(int)weaponType].damagePerShot;
        if (isHeadshot)
        {
            damageAmount = 75;
        }
        playerState.health -= damageAmount;
        gunController.UpdateOverheadUI();
        if (photonView.IsMine)
        {
            var group = UIController.Instance.PopUpGroup(UIController.Instance.shotOverlayGroup, 0.05f, 0.1f, 0.05f);
            StartCoroutine(group);
        }
        if (playerState.health <= 0)
        {
            gunController.photonView.RPC("KillPlayer", RpcTarget.All, (int)weaponType, attackerID);
        }
    }

    private Transform AssignCharactersCamera()
    {
        var t = armsRoot.transform;
        firstPersonArms.SetPositionAndRotation(t.position, t.rotation);
        return firstPersonArms;
    }

    /// Clamps <see cref="minVerticalAngle"/> and <see cref="maxVerticalAngle"/> to valid values and
    /// ensures that <see cref="minVerticalAngle"/> is less than <see cref="maxVerticalAngle"/>.
    private void ValidateRotationRestriction()
    {
        minVerticalAngle = ClampRotationRestriction(minVerticalAngle, -90, 90);
        maxVerticalAngle = ClampRotationRestriction(maxVerticalAngle, -90, 90);
        if (maxVerticalAngle >= minVerticalAngle) return;
        Debug.LogWarning("maxVerticalAngle should be greater than minVerticalAngle.");
        var min = minVerticalAngle;
        minVerticalAngle = maxVerticalAngle;
        maxVerticalAngle = min;
    }

    private static float ClampRotationRestriction(float rotationRestriction, float min, float max)
    {
        if (rotationRestriction >= min && rotationRestriction <= max) return rotationRestriction;
        var message = string.Format("Rotation restrictions should be between {0} and {1} degrees.", min, max);
        Debug.LogWarning(message);
        return Mathf.Clamp(rotationRestriction, min, max);
    }

    /// Processes the character movement and the camera rotation every fixed framerate frame.
    private void FixedUpdate()
    {
        if (isLocalPlayer())
        {
            input.UpdateFromLocal();
        }
        // FixedUpdate is used instead of Update because this code is dealing with physics and smoothing.
        if (isSetup && playerState.alive && !UIController.Instance.showExitConfirm && !UIController.Instance.showShop)
        {
            RotateCameraAndCharacter();
            MoveCharacter();
        }
        // _isGrounded = false;
    }

    private PlayerState lastFramePlayerState;
    private bool colorSet = false;

    private float lastUpdateFrame = 0;
    private float minInterfaceUpdateInterval = 0.5f;

    // process character movement only; input goes in GunController
    private void Update()
    {
        return;
        Debug.Log(GameController.GetStackTrace());
        if (!isSetup) return;
        var canUpdatePlayer = (Time.time + minInterfaceUpdateInterval) > lastUpdateFrame;
        if (this.playerState.colorIndex != lastFramePlayerState.colorIndex && !colorSet && canUpdatePlayer)
        {
            SetPlayerInfo(this.playerState.colorIndex);
            colorSet = true;
        }
        if (!photonView.IsMine)
        {
            firstPersonArms.position = transform.position + transform.TransformVector(armPositionOffset);
            var lerpRatio = 1 - Mathf.Clamp01((armsLerpTime - Time.time) / Time.fixedDeltaTime);
            firstPersonArms.rotation = Quaternion.Lerp(Quaternion.Euler(this.oldArmRotation), Quaternion.Euler(this.armRotation), lerpRatio);
        }
        if (!playerState.Equals(lastFramePlayerState) && canUpdatePlayer)
        {
            gunController.UpdateOverheadUI();
            GameController.Instance.UpdateScoreboard();
            UIController.Instance.UpdateWaitingText(this);
            lastUpdateFrame = Time.time;
        }
        lastFramePlayerState = playerState;
        if (!isLocalPlayer() || !playerState.alive)
        {
            return;
        }
        Jump();

        var holdingPistol = currentWeaponType == WeaponType.PISTOL;
        // Debug.Log($"{holdingPistol} {primaryWeapon} {Input.GetKeyDown(KeyCode.Alpha2)}");
        if (Input.GetKeyDown(KeyCode.Alpha1) && !holdingPistol)
        {
            photonView.RPC("SwitchWeapons", RpcTarget.All, (int)WeaponType.PISTOL);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && holdingPistol)
        {
            photonView.RPC("SwitchWeapons", RpcTarget.All, (int)primaryWeapon);
        }
    }

    public float scrollThreshold = 1;
    [SerializeField]
    public WeaponType currentWeaponType;

    [PunRPC]
    public void SwitchWeapons(WeaponType weaponType)
    {
        gunController.playerController = this;
        this.currentWeaponType = weaponType;
        UIController.Instance.currentWeaponText.text = weaponType.ToString();
        foreach (var w in GameController.Instance.weaponMappings)
        {
            var weaponRoot = gunController.GetWeaponRoot(w.weaponType);
            if (w.weaponType == weaponType)
            {
                weaponRoot.SetActive(true);
                weaponRoot.transform.rotation = this.firstPersonArms.transform.rotation;
                this.firstPersonArms = weaponRoot.transform;
                gunController.SwitchWeapons(firstPersonArms, weaponType, w);
            }
            else if (weaponRoot != null)
            {
                weaponRoot.SetActive(false);
            }
        }
    }


    public bool isInsideSlowField = false;

    [PunRPC]
    public void UseGadget(int index)
    {
        var gadgetType = gadgets[index];
        if (gadgetType == GadgetType.HEALTH_SHOT)
        {
            if (playerState.health >= 100)
            {
                UIController.Instance.SetIncidentalText("Cannot use at full health!");
                return;
            }
            else
            {
                playerState.health += 30;
                gadgets.RemoveAt(index);
                UIController.Instance.SetIncidentalText("+30 HP");
            }
        }
        else if (gadgetType == GadgetType.SLOW_FIELD)
        {
            var bulletPoint = gunController.gunCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var bulletPos = bulletPoint.origin + (bulletPoint.direction * 1);
            RaycastHit hit;
            if (Physics.Raycast(bulletPos, bulletPoint.direction, out hit, 30))
            {
                var field = PhotonNetwork.Instantiate("SlowField", hit.point + (transform.up * 1.5f), Quaternion.identity);
                GameController.Instance.roundObjectsToDestroy.Add(field);
                gadgets.RemoveAt(index);
            }
            else
            {
                UIController.Instance.SetIncidentalText("Too far!");
            }

        }
    }

    private void RotateCameraAndCharacter()
    {
        if (!isSetup)
        {
            return;
        }
        if (!isLocalPlayer() || !photonView.IsMine)
        {
            return;
        }
        var clampedY = RestrictVerticalRotation(input.RotateY);
        var worldUp = firstPersonArms.InverseTransformDirection(Vector3.up);
        var armRotation = firstPersonArms.rotation *
                       Quaternion.AngleAxis(input.RotateX, worldUp) *
                       Quaternion.AngleAxis(clampedY, Vector3.left);
        var rot = armRotation.eulerAngles;
        this.armRotation = rot;
        this.transform.eulerAngles = new Vector3(0f, rot.y, 0f);
        // Debug.Log($"Rotate from input for {playerState.name}");
        firstPersonArms.rotation = Quaternion.Euler(rot);
    }

    public Color playerColor;

    public static Color[] playerColors = {
        Color.red, Color.yellow, Color.green, Color.cyan, Color.magenta, Color.blue,
        Color.red, Color.yellow, Color.green, Color.cyan, Color.magenta, Color.blue,
    };

    [PunRPC]
    public void SetPlayerInfo(int colorIndex)
    {
        Debug.Log($"set color index {colorIndex} for {playerState.name}");
        this.playerState.colorIndex = colorIndex;
        var color = playerColors[colorIndex];
        playerColor = color;
        bodyRoot.Find("third_person_character").Find("backpack").GetComponent<SkinnedMeshRenderer>().material.SetColor("_Color", color);
        bodyRoot.Find("third_person_character").Find("helmet").GetComponent<SkinnedMeshRenderer>().material.SetColor("_Color", color);
    }

    /// Clamps the rotation of the camera around the x axis
    /// between the <see cref="minVerticalAngle"/> and <see cref="maxVerticalAngle"/> values.
    private float RestrictVerticalRotation(float mouseY)
    {
        var currentAngle = NormalizeAngle(firstPersonArms.eulerAngles.x);
        var minY = minVerticalAngle + currentAngle;
        var maxY = maxVerticalAngle + currentAngle;
        return Mathf.Clamp(mouseY, minY + 0.01f, maxY - 0.01f);
    }

    /// Normalize an angle between -180 and 180 degrees.
    /// <param name="angleDegrees">angle to normalize</param>
    /// <returns>normalized angle</returns>
    private static float NormalizeAngle(float angleDegrees)
    {
        while (angleDegrees > 180f)
        {
            angleDegrees -= 360f;
        }

        while (angleDegrees <= -180f)
        {
            angleDegrees += 360f;
        }

        return angleDegrees;
    }


    public Animator bodyAnimator;

    private void MoveCharacter()
    {
        var direction = new Vector3(input.Move, 0f, input.Strafe).normalized;
        // Debug.Log($"{input.Move} {input.Strafe}");
        var worldDirection = transform.TransformDirection(direction);
        var speed = input.Run ? runningSpeed : walkingSpeed;
        if (isInsideSlowField)
        {
            speed /= 4;
        }
        var velocity = worldDirection * speed;
        bodyAnimator.SetFloat("Vertical", input.Strafe, 0, Time.deltaTime);
        bodyAnimator.SetFloat("Horizontal", input.Move, 0, Time.deltaTime);
        if (playerRigidbody == null)
        {
            return;
        }

        var rigidbodyVelocity = playerRigidbody.velocity;
        var force = velocity - rigidbodyVelocity;
        force.y = 0;
        playerRigidbody.AddForce(force, ForceMode.Impulse);
    }

    public float speed = 1;
    private Vector3 armRotation;

    private bool CheckCollisionsWithWalls(Vector3 velocity)
    {
        if (_isGrounded) return false;
        var bounds = capsuleCollider.bounds;
        var radius = capsuleCollider.radius;
        var halfHeight = capsuleCollider.height * 0.5f - radius * 1.0f;
        var point1 = bounds.center;
        point1.y += halfHeight;
        var point2 = bounds.center;
        point2.y -= halfHeight;
        Physics.CapsuleCastNonAlloc(point1, point2, radius, velocity.normalized, _wallCastResults,
            radius * 0.04f, ~0, QueryTriggerInteraction.Ignore);
        var collides = _wallCastResults.Any(hit => hit.collider != null && hit.collider != capsuleCollider);
        if (!collides) return false;
        for (var i = 0; i < _wallCastResults.Length; i++)
        {
            _wallCastResults[i] = new RaycastHit();
        }

        return true;
    }
    private void Jump()
    {
        // Debug.Log($"{_isGrounded} {input.Jump}");
        if (!_isGrounded || !input.Jump || !playerState.alive) return;
        _isGrounded = false;
        playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

}