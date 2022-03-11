
using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

/// A helper for assistance with smoothing the camera rotation.
/// Input mappings
[Serializable]
public class PlayerInput
{

    public static float mouseSensitivity = 2f;
    public void UpdateFromLocal()
    {
        RotateX = Input.GetAxisRaw(rotateX) * mouseSensitivity;
        RotateY = Input.GetAxisRaw(rotateY) * mouseSensitivity;
        Move = Input.GetAxisRaw(move);
        Strafe = Input.GetAxisRaw(strafe);
        Run = Input.GetButton(run);
        Jump = Input.GetButton(jump);
    }

    public bool isRemote = false;

    [Tooltip("The name of the virtual axis mapped to rotate the camera around the y axis."),
     SerializeField]
    private string rotateX = "Mouse X";

    [Tooltip("The name of the virtual axis mapped to rotate the camera around the x axis."),
     SerializeField]
    private string rotateY = "Mouse Y";

    [Tooltip("The name of the virtual axis mapped to move the character back and forth."),
     SerializeField]
    private string move = "Horizontal";

    [Tooltip("The name of the virtual axis mapped to move the character left and right."),
     SerializeField]
    private string strafe = "Vertical";

    [Tooltip("The name of the virtual button mapped to run."),
     SerializeField]
    private string run = "Fire3";

    [Tooltip("The name of the virtual button mapped to jump."),
     SerializeField]
    private string jump = "Jump";

    /// Returns the value of the virtual axis mapped to rotate the camera around the y axis.
    public float RotateX;

    /// Returns the value of the virtual axis mapped to rotate the camera around the x axis.        
    public float RotateY;

    /// Returns the value of the virtual axis mapped to move the character back and forth.        
    public float Move;

    /// Returns the value of the virtual axis mapped to move the character left and right.         
    public float Strafe;

    /// Returns true while the virtual button mapped to run is held down.          
    public bool Run;

    /// Returns true during the frame the user pressed down the virtual button mapped to jump.          
    public bool Jump;
}