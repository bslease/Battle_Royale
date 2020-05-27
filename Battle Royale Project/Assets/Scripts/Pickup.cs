using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // required by fix to destroy pickups by RPC

public enum PickupType
{
    Health,
    Ammo
}

public class Pickup : MonoBehaviourPun
{
    public PickupType type;
    public int value;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Pickup triggered.");

        if (!PhotonNetwork.IsMasterClient)
            return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = GameManager.instance.GetPlayer(other.gameObject);

            if (type == PickupType.Health)
                player.photonView.RPC("Heal", player.photonPlayer, value);
            else if (type == PickupType.Ammo)
                player.photonView.RPC("GiveAmmo", player.photonPlayer, value);

            // destroy the object
            // PhotonNetwork.Destroy(gameObject);
            // BUG: pickups don't get removed from game and throw error:
            // "Failed to 'network-remove' GameObject because it is missing a valid InstantiationId on view"
            // https://forum.photonengine.com/discussion/15373/failed-to-network-remove-gameobject-because-it-is-missing-a-valid-instantiationid-on-view
            photonView.RPC("DestroyPickup", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    public void DestroyPickup()
    {
        Destroy(gameObject);
    }
}
