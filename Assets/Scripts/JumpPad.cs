using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public float jumpForce = 20;

    void OnCollisionEnter(Collision collision)
    {
        var controller = collision.gameObject.GetComponent<PlayerController>();
        if (controller != null)
        {
            if (GameController.Instance.LocalPlayerInstance == controller)
            {
                controller.gameObject.GetComponent<Rigidbody>().AddForce(jumpForce * transform.up, ForceMode.Impulse);
                GetComponent<AudioSource>().Play();
            }
        }
    }
}
