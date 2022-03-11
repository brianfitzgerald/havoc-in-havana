using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardController : MonoBehaviour
{
    [HideInInspector]
    public PlayerController localPlayerController;

    private Text roomNameText;
    private Text teamScoreText;
    private GameObject playerRowPrefab;
    private GameObject playersRoot;
    public void SetUIReferences()
    {
        roomNameText = transform.Find("Room Name").GetComponent<Text>();
        teamScoreText = transform.Find("Team Score").GetComponent<Text>();
        playerRowPrefab = (GameObject)Resources.Load("Player Row");
        playersRoot = transform.Find("Players").gameObject;
    }

    private float lastUpdateTime = 0;
    private float minInterfaceUpdateInterval = 1f;

    public void UpdateScoreboard(List<PlayerController> players, string roomName)
    {
        if (GameController.Instance == null || GameController.Instance.gameState.winsPerTeam == null)
        {
            return;
        }

        if (Time.time < lastUpdateTime + minInterfaceUpdateInterval)
        {
            return;
        }
        var traitorScore = GameController.Instance.gameState.winsPerTeam[(int)PlayerTeam.TRAITOR];
        var innocentScore = GameController.Instance.gameState.winsPerTeam[(int)PlayerTeam.INNOCENT];

        var mapIndex = MapGenerator.Instance.mapName;
        roomNameText.text = $"Room Name: {roomName} Map: {mapIndex}";
        teamScoreText.text = $"Traitor Wins: {traitorScore}\nInnocent Wins: {innocentScore}";

        int childs = playersRoot.transform.childCount;
        foreach (Transform child in playersRoot.transform)
        {
            Destroy(child.gameObject);
        }
        players.Sort((p1, p2) => p1.playerState.score.CompareTo(p2.playerState.score));

        int i = 0;
        foreach (var player in players)
        {
            var row = Instantiate(playerRowPrefab, Vector3.zero, Quaternion.identity, playersRoot.transform);
            row.transform.localPosition = new Vector3(0, i * 50, 0);
            row.transform.Find("Name").GetComponent<Text>().text = player.playerState.name;
            var colorImage = row.transform.Find("Color").gameObject.GetComponent<Image>();
            var visiblePlayerState = player.playerState.alive ? $"{player.playerState.health} HP" : "Dead";
            var color = player.playerColor;
            colorImage.color = color;
            if ((GameStatus)GameController.Instance.gameState.gameStatus == GameStatus.WARMUP)
            {
                visiblePlayerState = player.playerState.ready ? "Ready" : "Not Ready";
            }
            row.transform.Find("Status").GetComponent<Text>().text = visiblePlayerState;
            var roleText = player.getVisibleRole();
            if (player == GameController.Instance.LocalPlayerInstance)
            {
                roleText = "You!";
            }
            row.transform.Find("Role").GetComponent<Text>().text = roleText;
            row.transform.Find("Score").GetComponent<Text>().text = $"Wins: {player.playerState.score.ToString()}";
            i++;
        }
        lastUpdateTime = Time.time;
    }
}
