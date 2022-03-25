using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Photon.Pun;
using System.Diagnostics;


[System.Serializable]
public struct PickupData
{
    public PickupType pickupType;
    public int amount;
    public WeaponType weaponType;
    public GadgetType gadgetType;
    public PickupData(PickupType pickupType)
    {
        this.pickupType = pickupType;
        this.amount = 0;
        this.weaponType = WeaponType.PISTOL;
        this.gadgetType = GadgetType.SLOW_FIELD;
    }
    public PickupData(WeaponType weaponType)
    {
        this.pickupType = PickupType.WEAPON;
        this.amount = 0;
        this.weaponType = weaponType;
        this.gadgetType = GadgetType.SLOW_FIELD;
    }
    public PickupData(PickupType type, int amount)
    {
        this.pickupType = type;
        this.amount = amount;
        this.weaponType = WeaponType.PISTOL;
        this.gadgetType = GadgetType.SLOW_FIELD;
    }
}
public enum PickupType
{
    AMMO,
    MONEY,
    WEAPON,
    GADGET,
    INNOCENT_INTEL,
    TRAITOR_BOMB_TERMINAL
}
public class PickupController : MonoBehaviourPun, IPunObservable
{
    public PickupType pickupType;
    public GadgetType gadgetType;
    public WeaponType weaponType;
    public Text descriptionText;
    public int amount = 25;
    public Canvas overheadUI;


    public string Label()
    {
        if (pickupType == PickupType.WEAPON)
        {
            return weaponType.ToString();
        }
        else if (pickupType == PickupType.GADGET)
        {
            return gadgetType.ToString();
        }
        else if (pickupType == PickupType.INNOCENT_INTEL)
        {
            return "Innocent Player Intel";
        }
        else if (pickupType == PickupType.TRAITOR_BOMB_TERMINAL)
        {
            return "Traitorous Bomb Terminal";
        }
        else
        {
            var amountType = pickupType.ToString();
            return $"{amount} {amountType}";
        }
    }

    public void PickedUp(PlayerController player, bool swappingWeapon, WeaponType playersWeaponType = WeaponType.AK47)
    {
        if (pickupType == PickupType.AMMO)
        {
            player.ammoInInventory += amount;
        }
        else if (pickupType == PickupType.MONEY)
        {
            player.money += amount;
        }
        else if (pickupType == PickupType.WEAPON)
        {
            player.gunController.ammoInClip[(int)weaponType] = GameController.Instance.GetWeaponMapping(weaponType).clipSize;
            player.primaryWeapon = weaponType;
            player.SwitchWeapons(weaponType);
            // if we already have this weapon just give ammo
            if (!swappingWeapon && playersWeaponType == weaponType)
            {
                player.ammoInInventory += 10;
            }
            if (swappingWeapon)
            {
                this.SetData((int)PickupType.WEAPON, (int)playersWeaponType, 0);
                weaponType = playersWeaponType;
            }
        }
        else if (pickupType == PickupType.GADGET)
        {
            player.gadgets.Add(gadgetType);
        }
        else if (pickupType == PickupType.INNOCENT_INTEL || pickupType == PickupType.TRAITOR_BOMB_TERMINAL)
        {
            GameController.Instance.photonView.RPC("PickUpObjective", RpcTarget.All, (int)pickupType);
            showBeacon = true;
            GetComponent<Collider>().enabled = false;
        }
        this.SetMesh();
    }

    public bool showBeacon;

    public bool pickedUp = false;
    public float pickupRespawnTime;

    [PunRPC]
    public void PlayerPickedUp(bool isBeacon, int newDistributionIndex)
    {
        photonView.OwnershipTransfer = OwnershipOption.Takeover;
        photonView.RequestOwnership();
        photonView.TransferOwnership(this.photonView.Owner);
        photonView.RequestOwnership();
        pickedUp = true;
        // respawning
        SetRespawn();
        if (isBeacon)
        {
            showBeacon = true;
        }
        SetMesh();
        var newDist = GameController.Instance.pickupDistributions[newDistributionIndex];
        SetData((int)newDist.pickupType, (int)newDist.weaponType, newDist.amount);
    }

    private void SetRespawn()
    {
        var respawnTimeOptions = new float[] { 0.5f, 1, 1.5f };
        pickupRespawnTime = respawnTimeOptions[UnityEngine.Random.Range(0, respawnTimeOptions.Length - 1)] * 60;
    }

    [PunRPC]
    public void SetData(int pickupType, int weaponType, int amount)
    {
        this.pickupType = (PickupType)pickupType;
        this.weaponType = (WeaponType)weaponType;
        this.amount = amount;
        gameObject.name = this.Label();
        SetMesh();
    }

    // do not replicate - needs to be called on each client!
    private bool meshSet = false;


    public void SetMesh()
    {
        meshSet = true;
        var children = transform.GetComponentsInChildren<Transform>();
        string[] pickupTypeNames = System.Enum.GetNames(typeof(PickupType));
        descriptionText.text = this.Label();
        var prefabName = ((PickupType)pickupType).ToString();
        GetComponent<Collider>().enabled = !pickedUp;
        if (pickupType == PickupType.MONEY && amount >= 50)
        {
            prefabName = "LARGE_MONEY";
        }
        foreach (Transform child in transform)
        {
            // if it's picked up then totally disable
            if (pickedUp && child.name != "Overhead UI")
            {
                child.gameObject.SetActive(false);
            }
            else if (child.name == weaponType.ToString() && pickupType == PickupType.WEAPON)
            {
                child.gameObject.SetActive(true);
            }
            else if (child.name == pickupType.ToString() && showBeacon)
            {
                child.gameObject.SetActive(false);
            }
            else if (child.name == pickupType.ToString() && pickupType != PickupType.WEAPON)
            {
                child.gameObject.SetActive(true);
            }
            else if (child.name == "Overhead UI")
            {
                child.gameObject.SetActive(true);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
            if (child.name == "Beacon" && showBeacon)
            {
                child.gameObject.SetActive(true);
                var color = pickupType == PickupType.INNOCENT_INTEL ? Color.green : Color.red;
                color.a = 0.12f;
                transform.Find("Beacon").GetComponent<MeshRenderer>().material.SetColor("_Color", color);
            }
        }
    }
    private void FixedUpdate()
    {
        if (pickupRespawnTime > 0 && pickedUp)
        {
            pickupRespawnTime -= Time.deltaTime;
            var p = Mathf.Round(pickupRespawnTime);
            this.descriptionText.text = $"Respawning in {p} seconds.";
        }
        if (pickupRespawnTime <= 0 && pickedUp)
        {
            this.pickedUp = false;
            this.showBeacon = false;
            pickupRespawnTime = 0;
            this.SetMesh();
        }
        if (!meshSet)
        {
            SetMesh();
        }
        if (GameController.Instance.LocalPlayerInstance != null)
        {
            var relativePos = GameController.Instance.LocalPlayerInstance.transform.position - transform.position;
            var lookAtRot = Quaternion.LookRotation(relativePos, Vector3.up);
            this.transform.rotation = Quaternion.Euler(0, lookAtRot.eulerAngles.y + 180, 0);
        }
    }


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        UnityEngine.Debug.Log(System.Environment.StackTrace);
        if (stream.IsWriting)
        {
            stream.SendNext((int)this.pickupType);
            stream.SendNext((int)this.amount);
            stream.SendNext((int)this.weaponType);
        }
        else
        {
            var oldType = this.pickupType;
            var oldAmount = this.amount;
            var oldWeaponType = this.weaponType;
            this.pickupType = (PickupType)stream.ReceiveNext();
            this.amount = (int)stream.ReceiveNext();
            this.weaponType = (WeaponType)stream.ReceiveNext();
            if (oldType != this.pickupType || this.weaponType != oldWeaponType || oldAmount != this.amount)
            {

                this.SetData((int)this.pickupType, (int)this.weaponType, (int)this.amount);
                this.SetMesh();
            }
        }
    }
}
