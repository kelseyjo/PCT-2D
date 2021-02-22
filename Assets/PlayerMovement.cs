using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class PlayerMovement : MonoBehaviour
{
    public Character_Controller_2D controller;
    float horizontal_move = 0;
    bool jump = false;
    public float run_speed = 30f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //get input from player
        horizontal_move = CrossPlatformInputManager.GetAxis("Horizontal") * run_speed;
        if(CrossPlatformInputManager.GetButtonDown("Jump"))
        {
            jump = true;
        }
        // if(Input.GetButtonDown)
    }

    void FixedUpdate()
    {
        //move player (apply input to player)
        controller.Move((horizontal_move * Time.fixedDeltaTime), false, jump); //character controller script method
        jump = false;

    }
}
