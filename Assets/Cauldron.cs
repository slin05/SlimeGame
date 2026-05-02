using UnityEngine;
using Photon.Pun;

public class Cauldron : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Slime slime = other.GetComponent<Slime>();
        if (slime == null) return;

        PhotonView slimePV = other.GetComponent<PhotonView>();
        if (slimePV == null) return;

        string playerName = slimePV.Owner != null ? slimePV.Owner.NickName : "Unknown";

        CookingManager.Instance.photonView.RPC(
            nameof(CookingManager.RPC_SlimeAdded),
            RpcTarget.All,
            (int)slime.slimeType,
            playerName
        );

        PhotonNetwork.Destroy(other.gameObject);
    }
}