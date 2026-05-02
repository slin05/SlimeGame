using UnityEngine;
using Photon.Pun;

public class CameraFollow : MonoBehaviour
{
    private Transform target;
    public Vector3 offset = new Vector3(0, 0.5f, 0); 

    void LateUpdate()
    {
        if (target == null)
        {
            foreach (var pv in FindObjectsByType<PhotonView>(FindObjectsSortMode.None))
            {
                if (pv.IsMine && pv.CompareTag("Player"))
                {
                    target = pv.transform;
                    break;
                }
            }
        }
        else
        {
            transform.position = target.position + offset;

            float mouseX = Input.GetAxis("Mouse X") * 2f;
            transform.Rotate(0, mouseX, 0);
            target.rotation = transform.rotation;
        }
    }
}