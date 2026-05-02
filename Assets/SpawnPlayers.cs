using UnityEngine;
using Photon.Pun;

public class SpawnPlayers : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;
    public float minX;
    public float maxX;
    public float z;
    public float y;

    public override void OnEnable()
    {
        if (PhotonNetwork.InRoom)
            SpawnPlayer();
    }

    public override void OnJoinedRoom()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null) return;
        float x = Random.Range(minX, maxX);
        PhotonNetwork.Instantiate(playerPrefab.name, new Vector3(x, y, z), Quaternion.identity);
    }
}