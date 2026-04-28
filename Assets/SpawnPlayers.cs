using UnityEngine;
using Photon.Pun;
using TMPro;

public class SpawnPlayers : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;
    public float minX;
    public float maxX;
    public float z;
    public float y;

    void OnEnable()
    {
        if (PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }
    }

    public override void OnJoinedRoom()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null) return;

        float x = Random.Range(minX, maxX);
        Vector3 spawnPosition = new Vector3(x, y, z);
        GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);

        TextMeshProUGUI healthUI = GameObject.Find("HealthText").GetComponent<TextMeshProUGUI>();
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null && healthUI != null)
        {
            playerController.healthText = healthUI;
        }
    }
}