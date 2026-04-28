using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class LobbySceneManager : MonoBehaviourPunCallbacks
{
    // Input field for room name, Create/Join buttons, text panel for room list
    [Header("UI References")]
    public TMP_InputField roomNameInput;
    public Button createButton;
    public Button joinButton;
    public Button randomJoinButton;
    public TextMeshProUGUI roomListText;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    void Start()
    {
        PhotonNetwork.NickName = "Player_" + Random.Range(1000, 9999);
        PhotonNetwork.AutomaticallySyncScene = true;

        // Join lobby
        PhotonNetwork.ConnectUsingSettings();

        createButton.onClick.AddListener(OnCreateButtonClicked);
        joinButton.onClick.AddListener(OnJoinButtonClicked);
        randomJoinButton.onClick.AddListener(OnRandomJoinButtonClicked);

        roomListText.text = "Connecting to server...";
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        roomListText.text = "Joining lobby...";
    }

    public override void OnJoinedLobby()
    {
        roomListText.text = "No rooms available. Create one!";
        cachedRoomList.Clear();
    }

    // Create room 
    void OnCreateButtonClicked()
    {
        string roomName = roomNameInput.text;

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("Room name is empty!");
            return;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    // Join room
    void OnJoinButtonClicked()
    {
        string roomName = roomNameInput.text;

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("Room name is empty!");
            return;
        }

        PhotonNetwork.JoinRoom(roomName);
    }

    // Extra feature: Join a random room
    void OnRandomJoinButtonClicked()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    // Load room
    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("RoomScene");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Create Room Failed: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Join Room Failed: " + message);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogError("Join Random Room Failed: " + message);
    }

    // List of all current rooms
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                cachedRoomList.Remove(room.Name);
            }
            else
            {
                cachedRoomList[room.Name] = room;
            }
        }

        UpdateRoomListUI();
    }

    void UpdateRoomListUI()
    {
        if (cachedRoomList.Count == 0)
        {
            roomListText.text = "No rooms available. Create one!";
            return;
        }

        string displayText = "AVAILABLE ROOMS:\n\n";

        foreach (var room in cachedRoomList.Values)
        {
            displayText += $"• {room.Name}\n";
        }

        roomListText.text = displayText;
    }
}