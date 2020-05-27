using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPun
{
    [Header("Stats")]
    public float moveSpeed;
    public float jumpForce;

    [Header("Components")]
    public Rigidbody rig;

    [Header("Photon")]
    public int id;
    public Player photonPlayer;

    [Header("Stats")]
    public int curHp;
    public int maxHp;
    public int kills;
    public bool dead;
    private bool flashingDamage;
    public MeshRenderer mr;

    private int curAttackerId;
    public PlayerWeapon weapon;

    [PunRPC]
    public void Initialize(Player player)
    {
        id = player.ActorNumber;
        photonPlayer = player;
        GameManager.instance.players[id - 1] = this;

        // is this not our local player?
        if (!photonView.IsMine)
        {
            // deactivate other players' cameras in my game
            GetComponentInChildren<Camera>().gameObject.SetActive(false);

            // turn off other players' physics in my game (let Photon tell us what's happening to them)
            rig.isKinematic = true;
        }
        else
        {
            GameUI.instance.Initialize(this);
        }
    }

    void Update()
    {
        if (!photonView.IsMine || dead)
        {
            // we'll handle movement for other players via the PhotonTransformView, so just return if this player isn't me
            return;
        }

        Move();
        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();
        if (Input.GetMouseButtonDown(0))
            weapon.TryShoot();
    }

    void Move()
    {
        // get the input axis
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // calculate a direction relative to where we're facing
        Vector3 dir = (transform.forward * z + transform.right * x) * moveSpeed;
        dir.y = rig.velocity.y;

        // set that as our velocity
        rig.velocity = dir;
    }

    void TryJump()
    {
        // create a ray facing down
        Ray ray = new Ray(transform.position, Vector3.down);

        // shoot the raycast
        if (Physics.Raycast(ray, 1.5f))
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    [PunRPC]
    public void TakeDamage(int attackerId, int damage)
    {
        if (dead)
            return;

        curHp -= damage;
        curAttackerId = attackerId;

        // flash the player red
        // we don't need to call this on ourselves because we can't see our own body
        photonView.RPC("DamageFlash", RpcTarget.Others);

        // update the health bar UI
        GameUI.instance.UpdateHealthBar();

        // die if no health left
        if (curHp <= 0)
            photonView.RPC("Die", RpcTarget.All);
    }

    [PunRPC]
    void DamageFlash()
    {
        if (flashingDamage)
            return;

        StartCoroutine(DamageFlashCoRoutine());

        IEnumerator DamageFlashCoRoutine()
        {
            flashingDamage = true;
            Color defaultColor = mr.material.color;
            mr.material.color = Color.red;

            yield return new WaitForSeconds(0.05f);

            mr.material.color = defaultColor;
            flashingDamage = false;
        }
    }

    [PunRPC]
    void Die()
    {
        // Q: How does it know which player this is being called about?
        // A: In TakeDamage, the PlayerController that is dying tells all clients to run the Die function.
        //      Photon Network then runs the die function on the playercontroller that sent it.
        curHp = 0;
        dead = true;

        GameManager.instance.alivePlayers--;
        GameUI.instance.UpdatePlayerInfoText();

        // host will check win condition
        // CheckWinCondition doesn't just check, but also ends the game, so flow would stop there
        if (PhotonNetwork.IsMasterClient)
            GameManager.instance.CheckWinCondition();

        if (photonView.IsMine)
        {
            // check if I'm dying to a player or the force field
            if (curAttackerId != 0)
                GameManager.instance.GetPlayer(curAttackerId).photonView.RPC("AddKill", RpcTarget.All);

            // set the cam to spectator mode
            GetComponentInChildren<CameraController>().SetAsSpectator();

            // disable physics and hide the player avatar
            rig.isKinematic = true;
            transform.position = new Vector3(0, -50, 0);
        }
    }

    [PunRPC]
    public void AddKill()
    {
        kills++;
        GameUI.instance.UpdatePlayerInfoText();
    }

    [PunRPC]
    public void Heal(int amountToHeal)
    {
        curHp = Mathf.Clamp(curHp + amountToHeal, 0, maxHp);

        // update the health bar UI
        GameUI.instance.UpdateHealthBar();
    }

}
