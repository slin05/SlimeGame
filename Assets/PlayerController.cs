using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public float speed = 5f;
    public int maxHp = 100;
    public float bulletSpeed = 10f;
    public TextMeshProUGUI healthText;

    [Header("World-Space Health Bar")]
    public Slider healthBar;

    [Header("Complexity Feature: World-Space HP Number")]
    public TextMeshProUGUI worldHpText;

    private int currentHp;

    void Start()
    {
        currentHp = maxHp;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHp;
            healthBar.value = maxHp;
        }

        if (photonView.IsMine) UpdateHealthUI();

        UpdateWorldHpText();

        if (photonView.IsMine)
        {
            Hashtable props = new Hashtable { { "hp", currentHp } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        transform.position += new Vector3(h, 0, v) * speed * Time.deltaTime;

        if (Input.GetMouseButtonDown(0)) Shoot();
    }

    void Shoot()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint = ray.GetPoint(10f);
        Vector3 direction = (targetPoint - transform.position).normalized;

        GameObject bullet = PhotonNetwork.Instantiate("PhotonBullet",
                                                       transform.position,
                                                       Quaternion.identity);
        bullet.GetComponent<Rigidbody>().linearVelocity = direction * bulletSpeed;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine) return;

        if (!collision.gameObject.CompareTag("Bullet")) return;

        PhotonView bulletPV = collision.gameObject.GetComponent<PhotonView>();

        if (bulletPV == null || bulletPV.IsMine) return;

        string attackerName = bulletPV.Owner.NickName;
        int attackerActorNum = bulletPV.Owner.ActorNumber;

        string victimName = PhotonNetwork.LocalPlayer.NickName;
        int victimActorNum = PhotonNetwork.LocalPlayer.ActorNumber;

        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.BroadcastHitMessage(
                attackerName, victimName,
                attackerActorNum, victimActorNum,
                20);
        }

        photonView.RPC(nameof(TakeDamage), RpcTarget.AllBuffered,
                       20, attackerActorNum, attackerName);
    }

    [PunRPC]
    void TakeDamage(int damage, int attackerActorNumber, string attackerName)
    {
        currentHp -= damage;

        if (photonView.IsMine)
        {
            Hashtable props = new Hashtable { { "hp", currentHp } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            UpdateHealthUI();
        }

        if (healthBar != null) healthBar.value = currentHp;
        UpdateWorldHpText();

        if (currentHp <= 0)
            Die(attackerActorNumber, attackerName);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer != photonView.Owner) return;

        if (changedProps.ContainsKey("hp"))
        {
            currentHp = (int)changedProps["hp"];

            if (healthBar != null) healthBar.value = currentHp;

            UpdateWorldHpText();

            if (photonView.IsMine) UpdateHealthUI();
        }
    }

    void Die(int attackerActorNumber, string attackerName)
    {
        if (!photonView.IsMine) return;

        string victimName = PhotonNetwork.LocalPlayer.NickName;
        int victimActorNum = PhotonNetwork.LocalPlayer.ActorNumber;

        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.BroadcastKillMessage(
                attackerName, victimName,
                attackerActorNumber, victimActorNum);

            GameSceneManager.Instance.photonView.RPC(
                nameof(GameSceneManager.RPC_PlayerDied),
                RpcTarget.MasterClient,
                victimActorNum);
        }

        photonView.RPC(nameof(HandleDeath), RpcTarget.AllBuffered);
    }

    [PunRPC]
    void HandleDeath()
    {
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
    }

    [PunRPC]
    void HandleRespawn()
    {
        GetComponent<Renderer>().enabled = true;
        GetComponent<Collider>().enabled = true;
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = "HP: " + currentHp;
    }

    void UpdateWorldHpText()
    {
        if (worldHpText != null)
            worldHpText.text = currentHp + " / " + maxHp;
    }
}