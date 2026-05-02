using UnityEngine;
using TMPro;
using Photon.Pun;

public class PlayerNameTag : MonoBehaviourPun
{
    public TextMeshProUGUI nameText;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        nameText.text = photonView.Owner.NickName;
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            nameText.transform.parent.LookAt(
                nameText.transform.parent.position + mainCamera.transform.forward
            );
        }
    }
}