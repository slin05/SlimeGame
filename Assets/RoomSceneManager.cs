using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class RoomSceneManager : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI playerListText;
    public Button leaveButton;
    public Button startGameButton;
    public Button readyButton;

    private bool isReady = false;

    void Start()
    {
        leaveButton.onClick.AddListener(OnClickLeaveRoom);
        startGameButton.onClick.AddListener(OnClickStartGame);
        readyButton.onClick.AddListener(OnClickReady);

        UpdateRoomInfo();
        UpdatePlayerList();
    }

    void UpdateRoomInfo()
    {
        roomNameText.text = "Current Room: " + PhotonNetwork.CurrentRoom.Name;
    }

    void UpdatePlayerList()
    {
        playerListText.text = "Players:\n";

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            bool playerReady = false;
            if (player.CustomProperties.ContainsKey("isReady"))
            {
                playerReady = (bool)player.CustomProperties["isReady"];
            }

            string readyStatus = playerReady ? "- READY" : "";
            playerListText.text += "- " + player.NickName + readyStatus + "\n";
        }

        UpdateStartButton();
    }

    //Update button when all players are ready!
    void UpdateStartButton()
    {
        bool allReady = true;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("isReady") ||
                !(bool)player.CustomProperties["isReady"])
            {
                allReady = false;
                break;
            }
        }

        startGameButton.interactable = PhotonNetwork.IsMasterClient && allReady;
    }

    // What I decided to do this week, a ready button!
    void OnClickReady()
    {
        isReady = !isReady;

        Hashtable props = new Hashtable();
        props["isReady"] = isReady;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        UpdatePlayerList();
    }

    void OnClickLeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.LoadLevel("LobbyScene");
    }

    void OnClickStartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.LoadLevel("GameScene");
        }
    }
}