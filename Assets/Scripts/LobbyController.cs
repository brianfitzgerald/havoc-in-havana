using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class LobbyController : MonoBehaviourPunCallbacks
{

    string gameVersion = "1";
    [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
    [SerializeField]
    private byte maxPlayersPerRoom = 4;

    public InputField playerNameInput;
    public InputField lobbyNameInput;
    private string playerName;

    const string playerNamePrefKey = "PlayerName";
    const string lobbyNamePrefKey = "LobbyName";

    public Text connectingText;

    public Button connectButton;
    public Button exitButton;
    public Text subtitle;

    bool isConnecting;

    private static string[] subtitles = new string[]{
        "Thar be bugs!",
        "Still more stable than Fallout 76!",
        "Mo' Havoc, Mo' Havana",
        "Now Featuring: Pirate Ships",
        "The real havoc is the friends you make along the way.",
        "Still more stable than Battlfield 2042!",
        "Microtransaction free!",
        "Don't trust your friends!",
        "Look behind you!",
    };

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        if (PlayerPrefs.HasKey(playerNamePrefKey))
        {
            string storedName = PlayerPrefs.GetString(playerNamePrefKey);
            playerNameInput.text = storedName;
            PhotonNetwork.NickName = storedName;
        }
        if (PlayerPrefs.HasKey(lobbyNamePrefKey))
        {
            string storedName = PlayerPrefs.GetString(lobbyNamePrefKey);
            lobbyNameInput.text = storedName;
        }
        playerNameInput.onValueChanged.AddListener(delegate
        {
            PhotonNetwork.NickName = playerNameInput.text;
            PlayerPrefs.SetString(playerNamePrefKey, playerNameInput.text);
        });
        lobbyNameInput.onValueChanged.AddListener(delegate
        {
            PlayerPrefs.SetString(lobbyNamePrefKey, lobbyNameInput.text);
        });
        connectButton.onClick.AddListener(delegate
        {
            Connect();
            connectingText.gameObject.SetActive(true);
            connectButton.gameObject.SetActive(false);
        });
        exitButton.onClick.AddListener(delegate
        {
            Application.Quit();
        });
        var catchphrase = subtitles[Random.Range(0, subtitles.Length)];
        var gameVersion = (TextAsset)Resources.Load("Version");
        subtitle.text = $"{catchphrase} (Version {gameVersion.text})";
    }

    void Start()
    {
        connectingText.gameObject.SetActive(false);
        connectButton.gameObject.SetActive(false);
        if (!PhotonNetwork.IsConnected)
        {
            // #Critical, we must first and foremost connect to Photon Online Server.
            isConnecting = PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = gameVersion;
        }
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        connectButton.gameObject.SetActive(true);
        PhotonPeer.RegisterType(typeof(PlayerState), (byte)'P', SerializeStruct, DeserializeStruct);
        PhotonPeer.RegisterType(typeof(GameState), (byte)'G', SerializeStruct, DeserializeStruct);
    }

    public static byte[] SerializeStruct(object playerState)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using (var ms = new MemoryStream())
        {
            bf.Serialize(ms, playerState);
            return ms.ToArray();
        }
    }

    public static object DeserializeStruct(byte[] arrBytes)
    {
        using (var memStream = new MemoryStream())
        {
            var binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = binForm.Deserialize(memStream);
            return obj;
        }
    }

    public void Connect()
    {
        connectingText.gameObject.SetActive(true);
        var options = new RoomOptions();
        options.IsVisible = true;
        options.IsOpen = true;
        Debug.Log(lobbyNameInput.text);
        PhotonNetwork.JoinOrCreateRoom(lobbyNameInput.text, options, null);
    }


    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            Debug.Log("We load the 'Room for 1' ");


            // #Critical
            // Load the Room Level.
            PhotonNetwork.LoadLevel(1);
        }
    }


    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogErrorFormat("Room creation failed with error code {0} and error message {1}", returnCode, message);
        PhotonNetwork.CreateRoom(lobbyNameInput.text, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
    }


}
