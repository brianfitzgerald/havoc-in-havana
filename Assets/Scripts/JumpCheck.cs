using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class JumpCheck : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        playerController = transform.GetComponentInChildren<PlayerController>();
    }


    private GameObject currentGroundObject = null;

    // Update is called once per frame
    void Update()
    {
    }

    void OnCollisionEnter(Collision collision)
    {
        CollisionCheck(collision);
    }

    void CollisionCheck(Collision collision = null)
    {
        if (playerController != null && !playerController.isLocalPlayer())
        {
            return;
        }
        if (Time.time < lastCollisionCheckTime + 0.5f)
        {
            return;
        }
        if (playerController == null)
        {
            playerController = GameController.Instance.LocalPlayerInstance;
        }
        if (playerController == null)
        {
            return;
        }
        if (collision != null)
        {
            var vertical = Vector3.Dot(collision.contacts[0].normal, transform.up);
            if (vertical > 0.75f)
            {
                currentGroundObject = collision.contacts[0].otherCollider.gameObject;
            }
        }
        playerController._isGrounded = currentGroundObject != null;
        lastCollisionCheckTime = Time.time;
    }

    void OnCollisionStay(Collision collision)
    {
        CollisionCheck(collision);
    }

    private float lastCollisionCheckTime = 0;


    void OnCollisionExit()
    {
        CollisionCheck();
    }
    public PlayerController playerController;
}
