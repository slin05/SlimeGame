using UnityEngine;
using Photon.Pun;

public class Slime : MonoBehaviour, IPunInstantiateMagicCallback
{
    public float lifetime = 5f;
    public SlimeType slimeType;

    private static readonly Color[] slimeColors = { Color.red, Color.green, Color.blue, Color.yellow };

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        slimeType = (SlimeType)(int)info.photonView.InstantiationData[0];
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = slimeColors[(int)slimeType];
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}