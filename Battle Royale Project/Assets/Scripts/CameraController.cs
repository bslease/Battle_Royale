﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Look Sensitivity")]
    public float sensX;
    public float sensY;

    [Header("Clamping")]
    public float minY;
    public float maxY;

    [Header("Spectator")]
    public float spectatorMoveSpeed;

    private float rotX;
    private float rotY;
    private bool isSpectator;

    void Start()
    {
        // lock the cursor to the middle of the screen
        Cursor.lockState = CursorLockMode.Locked;
    }

    // check input and appy rotations in LateUpdate becuase doing this in Update can cause jittering
    private void LateUpdate()
    {
        // get the mouse movement inputs
        rotX += Input.GetAxis("Mouse X") * sensX;
        rotY += Input.GetAxis("Mouse Y") * sensY;

        // clamp the vertical rotation
        rotY = Mathf.Clamp(rotY, minY, maxY);

        // are we spectating?
        if (isSpectator)
        {
            // rotate the cam vertically
            transform.rotation = Quaternion.Euler(-rotY, rotX, 0);

            // movement
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical"); // vertical means thumbstick vertical or w and s (see project settings > input)
            float y = 0;
            if (Input.GetKey(KeyCode.E))
                y = 1;
            else if (Input.GetKey(KeyCode.Q))
                y = -1;
            Vector3 dir = transform.right * x + transform.up * y + transform.forward * z;
            transform.position += dir * spectatorMoveSpeed * Time.deltaTime;
        }
        else
        {
            // rotate the camera vertically
            // translate mouse y movement to rotate around the camera's x-axis
            transform.localRotation = Quaternion.Euler(-rotY, 0, 0);

            // rotate the player horizontally
            // likewise we want mouse x movement to rotate the player around it's y-axis
            //transform.parent.rotation = Quaternion.Euler(transform.rotation.x, rotX, 0);
            transform.parent.rotation = Quaternion.Euler(0, rotX, 0);
        }
    }

    public void SetAsSpectator()
    {
        isSpectator = true;
        transform.parent = null; // detach from player avatar
    }
}
