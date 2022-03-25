using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Profiling;
using System;

public enum GameStatus
{
    WARMUP,
    PLAYING,
    WON
}

[System.Serializable]
public struct GameState
{
    // seconds
    // seconds
    public float timeLimit;
    public int numPlayers;
    // GameStatus
    public int gameStatus;
    public int intelPickedUp;
    // PlayerTeam -> wins
    public Dictionary<int, int> winsPerTeam;
    public int playersReadiedUp;
    public int randomSeed;
    // PlayerTeam
    public int lastRoundWinner;
    public int numTraitors;

    public int mapIndex;

}

[System.Serializable]
public struct PickupSpawnParams
{
    public int num_aks;
    public int num_assault_rifles;
    public int num_shotguns;
    public int num_innocent_intel;
    public int num_traitor_intel;
    public int num_ammo_sm;
    public int num_ammo_lg;
}

public class GameController : MonoBehaviourPunCallbacks, IInRoomCallbacks, IPunObservable
{
    public GameObject playerPrefab;
    public PlayerController LocalPlayerInstance;
    public static GameController Instance;

    public float elapsedInRound;
    public GameObject spawnsParent;
    public GameObject pickupsParent;

    public GameObject pickupPrefab;

    // photonID to controller
    public Dictionary<int, PlayerController> playerReferences = new Dictionary<int, PlayerController>();

    private int randomSeed;

    public List<GameObject> roundObjectsToDestroy = new List<GameObject>();

    public GameState gameState;


    [HideInInspector]
    public List<Vector3> spawnPositions = new List<Vector3>();


    public PickupSpawnParams spawnParams;

    public List<WeaponData> weaponMappings = new List<WeaponData>();
    [HideInInspector]
    public List<PickupData> pickupDistributions = new List<PickupData>();
    private List<PickupData> previousPickups = new List<PickupData>();

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.OfflineMode = true;
            PhotonNetwork.CreateRoom("blah");
        }
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RequestOwnership();
            gameState = new GameState();
            elapsedInRound = 0;
            gameState.gameStatus = (int)GameStatus.WARMUP;
            gameState.numPlayers = 1;
            gameState.playersReadiedUp = 0;
            gameState.randomSeed = 0;
            gameState.timeLimit = defaultTimeLimit;
            gameState.winsPerTeam = new Dictionary<int, int>();
            gameState.winsPerTeam[(int)PlayerTeam.INNOCENT] = 0;
            gameState.winsPerTeam[(int)PlayerTeam.TRAITOR] = 0;
            gameState.mapIndex = 0;
        }
        var randomSeed = UnityEngine.Random.Range(0, 50000);
        UnityEngine.Random.InitState(randomSeed);
        if (PhotonNetwork.IsMasterClient && !useFirstMapOnly)
        {
            this.gameState.mapIndex = GetNextMapIndex();
        }
        MapGenerator.Instance = GetComponent<MapGenerator>();
        MapGenerator.Instance.Initialize();
        GenerateMapFromIndex(this.gameState.mapIndex);
        InitializeLobby();
        waitingForMapBeforeSpawning = true;

    }
    public bool useFirstMapOnly = false;

    private bool waitingForMapBeforeSpawning = false;

    private static int defaultTimeLimit = 60 * 4;

    public WeaponData GetWeaponMapping(WeaponType weaponType)
    {
        foreach (var mapping in weaponMappings)
        {
            if (mapping.weaponType == weaponType)
            {
                return mapping;
            }
        }
        return weaponMappings[0];
    }
    public List<Texture2D> maps;

    public void InitLocalPlayerInstance()
    {

        Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManagerHelper.ActiveSceneName);


        // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
        RefreshPlayers();
        var spawn = getSpawnPosition();
        var newPlayer = PhotonNetwork.Instantiate(this.playerPrefab.name, spawn, Quaternion.identity, 0);
        RefreshPlayers();
        newPlayer.GetComponent<PlayerController>().SetPlayerInfo(PhotonNetwork.PlayerList.Length);
        // if we join mid round, just kill the player and set team to spectator
        if (roomDataReceived == false)
        {
            waitingOnRoomData = true;
        }
        // need to call before we set up arena


    }

    public void InitializeLobby()
    {
        weaponMappings = new List<WeaponData>(new WeaponData[] {
            new WeaponData(WeaponType.PISTOL, KeyCode.Alpha1, 12, 8, false, false, 4, new Vector2(6, 10)),
            new WeaponData(WeaponType.ASSAULT_RIFLE, KeyCode.Alpha2, 6, 12, false, true, 8, new Vector2(6, 10)),
            new WeaponData(WeaponType.AK47, KeyCode.Alpha3, 10, 24, false, true, 12, new Vector2(9, 14)),
            new WeaponData(WeaponType.SHOTGUN, KeyCode.Alpha4, 6, 4, true, false, 1, new Vector2(10, 30)),
        });

        for (int i = 0; i < spawnParams.num_ammo_sm; i++)
        {
            pickupDistributions.Add(new PickupData(PickupType.AMMO, 10));
        }

        for (int i = 0; i < spawnParams.num_ammo_lg; i++)
        {
            pickupDistributions.Add(new PickupData(PickupType.AMMO, 15));
        }

        for (int i = 0; i < spawnParams.num_ammo_lg; i++)
        {
            pickupDistributions.Add(new PickupData(PickupType.AMMO, 25));
        }

        for (int i = 0; i < spawnParams.num_shotguns; i++)
        {
            pickupDistributions.Add(new PickupData(WeaponType.SHOTGUN));
        }

        for (int i = 0; i < spawnParams.num_assault_rifles; i++)
        {
            pickupDistributions.Add(new PickupData(WeaponType.ASSAULT_RIFLE));
        }

        for (int i = 0; i < spawnParams.num_aks; i++)
        {
            pickupDistributions.Add(new PickupData(WeaponType.AK47));
        }

        for (int i = 0; i < spawnParams.num_innocent_intel; i++)
        {
            pickupDistributions.Add(new PickupData(PickupType.INNOCENT_INTEL));
        }

        for (int i = 0; i < spawnParams.num_traitor_intel; i++)
        {
            pickupDistributions.Add(new PickupData(PickupType.TRAITOR_BOMB_TERMINAL));
        }


        var newDistributions = new List<PickupData>();
        newDistributions.Add(new PickupData(WeaponType.ASSAULT_RIFLE));
        pickupDistributions = pickupDistributions.OrderBy(x => UnityEngine.Random.value).ToList();

        for (int i = 0; i < pickupDistributions.Count; i++)
        {
            var item = pickupDistributions[UnityEngine.Random.Range(0, pickupDistributions.Count)];
            if (pickupDistributions.Count > 0)
            {
                var previous = newDistributions[newDistributions.Count - 1];
                for (int k = 0; k < 1000; k++)
                {
                    item = pickupDistributions[UnityEngine.Random.Range(0, pickupDistributions.Count)];
                    if (previous.pickupType == PickupType.WEAPON)
                    {
                        var ammoAmounts = new int[] { 15, 25, 10 };
                        item = new PickupData(PickupType.AMMO, ammoAmounts[UnityEngine.Random.Range(0, ammoAmounts.Length)]);
                        break;
                    }
                    if (previous.pickupType != item.pickupType)
                    {
                        break;
                    }
                }
            }
            newDistributions.Add(item);
        }

        pickupDistributions = newDistributions;

    }


    private bool usingGeneratedMap = true;

    private Vector3 getSpawnPosition()
    {
        var spawnIndex = PhotonNetwork.PlayerList.Length;
        if (spawnIndex >= spawnPositions.Count - 1)
        {
            spawnIndex = 0;
        }
        var spawn = spawnPositions[spawnIndex];
        Debug.Log($"spawn with {spawn} {spawnIndex} {spawnPositions.Count}");
        return spawn;
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
    }

    void LoadArena()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("PhotonNetwork : Trying to Load a level but we are not the master Client");
        }
        Debug.LogFormat("PhotonNetwork : Loading Level : {0}", PhotonNetwork.CurrentRoom.PlayerCount);
        PhotonNetwork.LoadLevel(1);
    }

    void SpawnPickups()
    {
        var pickups = GameObject.FindObjectsOfType<PickupController>();
        foreach (var pickup in pickups)
        {
            photonView.RequestOwnership();
            pickup.pickedUp = false;
            pickup.pickupRespawnTime = 0;
            pickup.showBeacon = false;
            pickup.SetMesh();
        }

        var weaponsGoal = gameState.numPlayers;
        foreach (Transform child in pickupsParent.transform)
        {
            if (UnityEngine.Random.Range(0, 1f) > 0.25f) {
                SpawnPickup(child);
            }
        }
    }
    public float pickupSpawnChance = 0.5f;

    private bool pickupsSpawnedOnce = false;

    public static int intelTarget = 3;

    // intel is 
    [PunRPC]
    public void PickUpObjective(int pickupTypeInt)
    {
        var pickupType = (PickupType)pickupTypeInt;
        if (pickupType == PickupType.INNOCENT_INTEL)
        {
            var subtext = "";
            this.gameState.intelPickedUp += 1;
            if (this.gameState.intelPickedUp >= intelTarget)
            {
                this.gameState.timeLimit -= 60;
                subtext = "The traitors have less time to kill the innocents!";
                this.gameState.intelPickedUp = 0;
            }
            else
            {
                subtext = $"The innocents have to collect {intelTarget - this.gameState.intelPickedUp} more intel to reduce the timer!";
            }
            UIController.Instance.ShowPopUpText("INTEL PICKED UP", subtext);
        }
        else if (pickupType == PickupType.TRAITOR_BOMB_TERMINAL)
        {
            this.gameState.timeLimit += 60;
            UIController.Instance.ShowPopUpText("TERMINAL ACTIVATED", "The traitors have a minute longer to kill everyone!");
        }
    }
    private int pickupIndex;

    private void SpawnPickup(Transform child)
    {
        GameObject spawn;
        if (PhotonNetwork.IsConnected)
        {
            spawn = PhotonNetwork.Instantiate(this.pickupPrefab.name, child.position, Quaternion.identity, 0);
        }
        else
        {
            spawn = Instantiate(pickupPrefab, child.position, child.rotation);
        }
        if (pickupIndex > pickupDistributions.Count - 1)
        {
            pickupIndex = 0;
        }
        PickupData pickupData = pickupDistributions[pickupIndex];
        pickupIndex++;

        spawn.GetComponent<PickupController>().photonView.RPC("SetData", RpcTarget.All, (int)pickupData.pickupType, (int)pickupData.weaponType, pickupData.amount);
    }

    public override void OnPlayerEnteredRoom(Player other)
    {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName); // not seen if you're the player connecting
        RefreshPlayers();
        UpdateScoreboard();
        UIController.Instance.UpdateWaitingText(LocalPlayerInstance);
    }


    public void RefreshPlayers()
    {
        Debug.Log("refresh");
        var players = GameObject.FindObjectsOfType<PlayerController>();
        var i = 0;
        var didSetupPlayer = false;
        foreach (var player in players)
        {
            var playerIndex = player.photonView.Owner.ActorNumber;
            i++;
            if (!player.isSetup)
            {
                Debug.Log($"found non setup player {playerIndex}");
                player.SetupPlayer();
                player.gunController.Setup();
                player.SwitchWeapons(WeaponType.PISTOL);
                playerReferences[playerIndex] = player;
                didSetupPlayer = true;
            }
        }
        if (didSetupPlayer)
        {
            UpdateScoreboard();
        }
        gameState.numPlayers = PhotonNetwork.PlayerList.Length;
    }


    public override void OnPlayerLeftRoom(Player other)
    {
        Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName); // seen when other disconnects
        var playerIndex = other.ActorNumber;
        if (playerReferences.ContainsKey(playerIndex))
        {
            playerReferences[playerIndex].photonView.RPC("KillPlayer", RpcTarget.All, (int)WeaponType.PISTOL, playerIndex);
            Destroy(playerReferences[playerIndex].gameObject);
        }
        playerReferences.Remove(playerIndex);
        RefreshPlayers();
        UpdateScoreboard();
        UIController.Instance.UpdateWaitingText(LocalPlayerInstance);
    }


    public int minPlayersToStart = 2;

    [PunRPC]
    public void PlayerReadiedUp(PhotonMessageInfo info)
    {
        UIController.Instance.UpdateWaitingText(LocalPlayerInstance, true);
        var id = info.Sender.ActorNumber;
        playerReferences[id].playerState.ready = true;
        gameState.playersReadiedUp++;
        RefreshPlayers();
        if (gameState.playersReadiedUp >= PhotonNetwork.PlayerList.Length && gameState.numPlayers >= minPlayersToStart)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                StartRound();
            }
            var pickups = GameObject.FindObjectsOfType<PickupController>();
            foreach (var pickup in pickups)
            {
                // HACK
                pickup.pickedUp = false;
                pickup.pickupRespawnTime = 0;
                pickup.showBeacon = false;
                pickup.SetMesh();
            }
        }
        UpdateScoreboard();
    }
    private GameState lastGameState;

    private float lastPlayerRefresh = 0;

    void Update()
    {
        Debug.Log(GameController.GetStackTrace());
        if (waitingForMapBeforeSpawning)
        {
            InitLocalPlayerInstance();
            waitingForMapBeforeSpawning = false;
        }
        if (waitingForMapBeforeSpawning)
        {
            Debug.Log("waiting for room to spawn");
            return;
        }
        if (waitingOnRoomData && roomDataReceived)
        {
            if ((GameStatus)gameState.gameStatus == GameStatus.PLAYING)
            {
                waitingOnRoomData = false;
                RefreshPlayers();
                if (!LocalPlayerInstance.playerState.teamAssigned)
                {
                    // for late joiner detection
                    var hasTraitor = false;
                    foreach (var player in playerReferences)
                    {
                        var playerTeam = (PlayerTeam)player.Value.playerState.team;
                        if (playerTeam == PlayerTeam.TRAITOR && player.Value.playerState.ready)
                        {
                            hasTraitor = true;
                        }
                    }
                    var newPlayerRole = PlayerTeam.INNOCENT;
                    if (!hasTraitor)
                    {
                        newPlayerRole = PlayerTeam.TRAITOR;
                    }
                    LocalPlayerInstance.photonView.RPC("SetTeam", RpcTarget.All, (int)newPlayerRole);
                    LocalPlayerInstance.playerState.ready = true;
                }
            }
        }
        if (!lastGameState.Equals(gameState))
        {
            RefreshPlayers();
            UpdateScoreboard();
            UIController.Instance.UpdateWaitingText(LocalPlayerInstance);
        }
        lastGameState = gameState;
        // only want to run this on master
        if (PhotonNetwork.IsMasterClient)
        {
            if (gameState.gameStatus == (int)GameStatus.PLAYING)
            {
                checkIfTeamWon();
            }
            if (gameState.gameStatus == (int)GameStatus.WON)
            {
                endOfMatch();
            }
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            endOfMatch();
        }
    }

    private bool showOneMinuteWarning = false;

    private void checkIfTeamWon()
    {
        elapsedInRound += Time.deltaTime;
        if (!showOneMinuteWarning && elapsedInRound > gameState.timeLimit - 60)
        {
            UIController.Instance.ShowPopUpText("One minute left!", " ");
            showOneMinuteWarning = true;
        }
        // T run out of time, so I win
        if (elapsedInRound >= gameState.timeLimit)
        {
            photonView.RPC("TeamWon", RpcTarget.All, (int)PlayerTeam.INNOCENT, true);
        }
        // check that all members of a team are dead
        // if there is still 1 alive I or T, then that team hasn't won
        var aliveCounts = new Dictionary<PlayerTeam, int>();
        foreach (var player in this.playerReferences)
        {
            var team = (PlayerTeam)player.Value.playerState.team;
            if (team == PlayerTeam.DETECTIVE)
            {
                team = PlayerTeam.INNOCENT;
            }
            if (player.Value.playerState.alive)
            {
                if (aliveCounts.ContainsKey(team))
                {
                    aliveCounts[team] += 1;
                }
                else
                {
                    aliveCounts[team] = 1;
                }
            }
            else if (!aliveCounts.ContainsKey(team))
            {
                aliveCounts[team] = 0;
            }
        }
        if (aliveCounts.ContainsKey(PlayerTeam.TRAITOR) && aliveCounts.ContainsKey(PlayerTeam.INNOCENT))
        {
            if (aliveCounts[PlayerTeam.INNOCENT] == 0 && aliveCounts[PlayerTeam.TRAITOR] > 0)
            {
                photonView.RPC("TeamWon", RpcTarget.All, (int)PlayerTeam.TRAITOR, false);
            }
            if (aliveCounts[PlayerTeam.TRAITOR] == 0 && aliveCounts[PlayerTeam.INNOCENT] > 0)
            {
                photonView.RPC("TeamWon", RpcTarget.All, (int)PlayerTeam.INNOCENT, false);
            }
        }
    }
    private void endOfMatch()
    {
        elapsedInRound = 0;
        // show winning team for 15 seconds then reset
        if (endOfRoundCountdown > 0)
        {
            endOfRoundCountdown -= Time.deltaTime;
        }
        else
        {
            gameState.gameStatus = (int)GameStatus.WARMUP;
            // TODO move player ready status to game controller
            gameState.playersReadiedUp = 0;
            gameState.timeLimit = defaultTimeLimit;
            gameState.intelPickedUp = 0;
            if (PhotonNetwork.IsMasterClient)
            {
                this.gameState.mapIndex = GetNextMapIndex();
            }
            GenerateMapFromIndex(this.gameState.mapIndex);
            var spawn = getSpawnPosition();
            spawn.y = 15;
            LocalPlayerInstance.transform.position = spawn;
            LocalPlayerInstance.transform.rotation = Quaternion.identity;
            if (PhotonNetwork.IsMasterClient && !pickupsSpawnedOnce)
            {
                SpawnPickups();
                pickupsSpawnedOnce = true;
            }
            LocalPlayerInstance.gunController.spectatorCamera.GetComponent<SpectatorCameraController>().StopLooking();
            foreach (var player in this.playerReferences)
            {
                player.Value.gunController.photonView.RPC("RevivePlayer", RpcTarget.All);
                player.Value.SwitchWeapons(WeaponType.PISTOL);
            }
            Debug.Log($"spawn next round with {spawn} ");
            LocalPlayerInstance.transform.position = spawn;
            LocalPlayerInstance.transform.rotation = Quaternion.identity;
            UIController.Instance.UpdateWaitingText(LocalPlayerInstance);
        }
        UpdateScoreboard();
    }

    private float endOfRoundCountdown = 0;

    [PunRPC]
    private void TeamWon(int team, bool outOfTime)
    {
        showOneMinuteWarning = false;
        var winningTeam = (PlayerTeam)team;
        var playerTeam = (PlayerTeam)GameController.Instance.LocalPlayerInstance.playerState.team;
        if (winningTeam == playerTeam || winningTeam == PlayerTeam.DETECTIVE && winningTeam == PlayerTeam.INNOCENT)
        {
            GameController.Instance.LocalPlayerInstance.playerState.score += 1;
        }
        gameState.gameStatus = (int)GameStatus.WON;
        gameState.lastRoundWinner = (int)winningTeam;
        gameState.winsPerTeam[(int)winningTeam]++;
        var teamText = $"{winningTeam.ToString()}S WIN";
        var otherTeam = winningTeam == PlayerTeam.TRAITOR ? "innocents" : "traitors";
        var winReason = outOfTime ? "Traitors ran out of time!" : $"All {otherTeam} were eliminated!";
        var traitors = UIController.Instance.getOtherTraitorsText(false);
        winReason += $"\nThe traitors were: {traitors}";
        UIController.Instance.ShowPopUpText(teamText, winReason, 5f);
        // 5 seconds
        endOfRoundCountdown = 5;
        UpdateScoreboard();
    }

    public void UpdateScoreboard()
    {
        if (UIController.Instance.scoreboardController != null)
        {
            UIController.Instance.scoreboardController.UpdateScoreboard(playerReferences.Values.ToList(), PhotonNetwork.CurrentRoom.Name);
        }
    }

    Dictionary<PlayerTeam, int> generateRolePool(int playerCount)
    {

        Dictionary<PlayerTeam, int> rolePool = new Dictionary<PlayerTeam, int>();
        rolePool.Add(PlayerTeam.INNOCENT, 0);
        rolePool.Add(PlayerTeam.TRAITOR, 1);
        rolePool.Add(PlayerTeam.DETECTIVE, 0);
        var rolesAdded = 1;
        if (playerCount > 3)
        {
            rolePool[PlayerTeam.TRAITOR]++;
            rolesAdded += 1;
        }
        if (playerCount > 4)
        {
            rolePool[PlayerTeam.DETECTIVE]++;
            rolesAdded += 1;
        }
        if (playerCount > 6)
        {
            rolePool[PlayerTeam.TRAITOR]++;
            rolePool[PlayerTeam.DETECTIVE]++;
            rolesAdded += 2;
        }
        // make sure there are at least enough to cover the rest of the players
        rolePool[PlayerTeam.INNOCENT] += playerCount - rolesAdded;
        return rolePool;
    }

    private void ClearRoundObjects()
    {
        for (int i = 0; i < roundObjectsToDestroy.Count; i++)
        {
            var obj = roundObjectsToDestroy[i];
            if (obj != null)
            {
                var view = obj.GetPhotonView();
                if (view != null)
                {
                    view.OwnershipTransfer = OwnershipOption.Takeover;
                    view.RequestOwnership();
                    view.TransferOwnership(this.photonView.Owner);
                }
                obj.transform.parent = null;
                Destroy(roundObjectsToDestroy[i]);
            }
        }
        Debug.Log("clear spawn");
        spawnPositions.Clear();
        roundObjectsToDestroy.Clear();
    }

    // only called by master client
    void StartRound()
    {
        ClearRoundObjects();
        if (!pickupsSpawnedOnce)
        {
            SpawnPickups();
            pickupsSpawnedOnce = true;
        }

        // TODO make these dependent on team size
        elapsedInRound = 0;
        var rolePool = generateRolePool(this.playerReferences.Count);
        // List<Dictionary<PlayerTeam, int>> rolePoolList = new List<Dictionary<PlayerTeam, int>>();
        // for (int i = 0; i < 500; i++)
        // {
        //     rolePoolList.Add(generateRolePool(UnityEngine.Random.Range(1, 10)));
        // }
        gameState.numTraitors = rolePool[PlayerTeam.TRAITOR];
        PlayerTeam[] rolesAvailable = rolePool.Keys.ToArray();

        Debug.Log($"start round with {rolePool[PlayerTeam.INNOCENT]} innocents and {rolePool[PlayerTeam.TRAITOR]} traitors");

        foreach (var player in this.playerReferences)
        {
            for (int i = 0; i < 1000; i++)
            {
                var team = rolesAvailable[UnityEngine.Random.Range(0, rolesAvailable.Length)];
                if (rolePool[team] > 0)
                {
                    player.Value.photonView.RPC("SetTeam", RpcTarget.All, (int)team);
                    Debug.Log($"set {team}");
                    rolePool[team]--;
                    break;
                }
            }
        }
        gameState.gameStatus = (int)GameStatus.PLAYING;
        UpdateScoreboard();
    }

    private bool roomDataReceived = false;
    private bool waitingOnRoomData = false;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(this.elapsedInRound);
            stream.SendNext(this.gameState.timeLimit);
            stream.SendNext(this.gameState.numPlayers);
            stream.SendNext(this.gameState.gameStatus);
            stream.SendNext(this.gameState.playersReadiedUp);
            stream.SendNext(this.gameState.randomSeed);
            stream.SendNext(this.gameState.lastRoundWinner);
            stream.SendNext(this.endOfRoundCountdown);
            stream.SendNext(this.gameState.winsPerTeam);
            stream.SendNext(this.gameState.numTraitors);
            stream.SendNext(this.gameState.mapIndex);
        }
        else
        {
            this.elapsedInRound = (float)stream.ReceiveNext();
            this.gameState.timeLimit = (float)stream.ReceiveNext();
            this.gameState.numPlayers = (int)stream.ReceiveNext();
            this.gameState.gameStatus = (int)stream.ReceiveNext();
            this.gameState.playersReadiedUp = (int)stream.ReceiveNext();
            this.gameState.randomSeed = (int)stream.ReceiveNext();
            this.gameState.lastRoundWinner = (int)stream.ReceiveNext();
            this.endOfRoundCountdown = (float)stream.ReceiveNext();
            this.gameState.winsPerTeam = (Dictionary<int, int>)stream.ReceiveNext();
            this.gameState.numTraitors = (int)stream.ReceiveNext();
            var newMapIndex = (int)stream.ReceiveNext();
            if (roomDataReceived == false)
            {
                roomDataReceived = true;
            }
            if (this.gameState.mapIndex != newMapIndex || !mapGenerated)
            {
                GenerateMapFromIndex(newMapIndex);
                mapGenerated = true;
            }
            this.gameState.mapIndex = newMapIndex;
        }
    }

    private bool mapGenerated = false;

    // TODO at some point we want to dedupe so there's always a rotation

    private int lastMapIndex = 1;
    public int GetNextMapIndex()
    {
        lastMapIndex = this.gameState.mapIndex;
        var idx = UnityEngine.Random.Range(0, maps.Count);
        for (int i = 0; i < 20; i++)
        {
            idx = UnityEngine.Random.Range(0, maps.Count);
            if (idx != lastMapIndex)
            {
                return idx;
            }
        }
        return idx;
    }
    public void GenerateMapFromIndex(int index)
    {
        ClearRoundObjects();
        var tex = maps[index];
        MapGenerator.Instance.GenerateFromTexture(tex);
    }
    public static string GetStackTrace()
    {
        var st = new System.Diagnostics.StackTrace(1,
                                true);
        var frames = st.GetFrames();
        var traceString = new System.Text.StringBuilder();

        foreach (var frame in frames)
        {
            if (frame.GetFileLineNumber() < 1)
                continue;

            traceString.Append("File: " + frame.GetFileName());
            traceString.Append(", Method:" + frame.GetMethod().Name);
            traceString.Append(", LineNumber: " + frame.GetFileLineNumber());
        }

        return traceString.ToString();
    }

}
