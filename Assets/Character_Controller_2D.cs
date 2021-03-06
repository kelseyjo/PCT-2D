﻿using UnityEngine;
using UnityEngine.Events;
using UnityStandardAssets.CrossPlatformInput;
// DISCLAIMER: This script is from a Brackey's Youtube Tutorial. 
public class Character_Controller_2D : MonoBehaviour
{

	
	[Header("For Movement")]
	[SerializeField] private float m_JumpForce = 400f;							// Amount of force added when the player jumps.
	[Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;			// Amount of maxSpeed applied to crouching movement. 1 = 100%
	[Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;	// How much to smooth out the movement
	[SerializeField]  float air_move_speed = 20f;
	[SerializeField]  float move_speed = 30f;

	[Header("For Jumping")]
	[SerializeField] private bool m_AirControl = false;							// Whether or not a player can steer while jumping;
	[SerializeField] private LayerMask m_WhatIsGround;							// A mask determining what is ground to the character
	[SerializeField] private Transform m_GroundCheck;							// A position marking where to check if the player is grounded.
	[SerializeField] private bool m_jump = true;

	//for wall sliding
	[Header("For Wall Sliding")]
	[SerializeField] private float wall_slide_speed = 0f;
	[SerializeField] LayerMask wall_layer;
	[SerializeField] Transform wall_check;
	[SerializeField] Vector2 wall_check_size;
	private bool is_touching_wall;
	private bool is_wall_sliding;

	//from other script
	[Header("For Character Controller")]
	public Character_Controller_2D controller;
    float horizontal_move = 0;
    bool jump = false;
    public float run_speed = 30f;

	[Header("For Wall Jumping")]
	[SerializeField] float wall_jump_force = 18f;
	[SerializeField] float wall_jump_direction = -1;
	[SerializeField] Vector2 wall_jump_angle; //set in inspector

	[SerializeField] private Transform m_CeilingCheck;							// A position marking where to check for ceilings
	[SerializeField] private Collider2D m_CrouchDisableCollider;				// A collider that will be disabled when crouching

	const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
	private bool is_grounded;            // Whether or not the player is grounded.
	const float k_CeilingRadius = .2f; // Radius of the overlap circle to determine if the player can stand up
	private Rigidbody2D m_Rigidbody2D;
	private bool m_FacingRight = true;  // For determining which way the player is currently facing.
	private Vector3 m_Velocity = Vector3.zero;

	[Header("Events")]
	[Space]

	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	public BoolEvent OnCrouchEvent;
	private bool m_wasCrouching = false;

	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();
		wall_jump_angle.Normalize();

		if (OnLandEvent == null)
			OnLandEvent = new UnityEvent();

		if (OnCrouchEvent == null)
			OnCrouchEvent = new BoolEvent();
	}

	private void Update()
	{
		//get input from player
        horizontal_move = CrossPlatformInputManager.GetAxis("Horizontal") * run_speed;
        if(CrossPlatformInputManager.GetButtonDown("Jump"))
        {
            jump = true;
        }
	}


	private void FixedUpdate() //movement, jumping, animation control
	{
		//move player (apply input to player)
        controller.Move((horizontal_move * Time.fixedDeltaTime), false, jump); //character controller script method
        jump = false;
		bool wasGrounded = is_grounded;
		is_grounded = false;

		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		// This can be done using layers instead but Sample Assets will not overwrite your project settings.
		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
			{
				is_grounded = true;
				if (!wasGrounded)
					OnLandEvent.Invoke();
			}
		}

		is_touching_wall = Physics2D.OverlapBox(wall_check.position, wall_check_size, 0, wall_layer);
		
		WallSlide();
		WallJump();

	}


	public void Move(float move, bool crouch, bool jump)
	{
		m_jump = jump;
		// If crouching, check to see if the character can stand up
		if (!crouch)
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
			{
				crouch = true;
			}
		}

		//only control the player if grounded or airControl is turned on
		if (is_grounded || m_AirControl)
		{

			// If crouching
			if (crouch)
			{
				if (!m_wasCrouching)
				{
					m_wasCrouching = true;
					OnCrouchEvent.Invoke(true);
				}

				// Reduce the speed by the crouchSpeed multiplier
				move *= m_CrouchSpeed;

				// Disable one of the colliders when crouching
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = false;
			} else
			{
				// Enable the collider when not crouching
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = true;

				if (m_wasCrouching)
				{
					m_wasCrouching = false;
					OnCrouchEvent.Invoke(false);
				}
			}

			// Move the character by finding the target velocity
			Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
			// And then smoothing it out and applying it to the character
			if(is_grounded)
			{
				//Debug.Log("reg move");
				m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);
			}
			else if(!is_grounded && (!is_wall_sliding && (move !=0)))
			{
				//Debug.Log("air movement");
				m_Rigidbody2D.AddForce(new Vector2(air_move_speed * move, 0));
				if(Mathf.Abs(m_Rigidbody2D.velocity.x) > move_speed)
				{
					m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);
				}
			}
			// If the input is moving the player right and the player is facing left...
			if (move > 0 && !m_FacingRight)
			{
				// ... flip the player.
				Flip();
			}
			// Otherwise if the input is moving the player left and the player is facing right...
			else if (move < 0 && m_FacingRight)
			{
				// ... flip the player.
				Flip();
			}
		}
		// If the player should jump...
		if (is_grounded && m_jump)
		{
			// Add a vertical force to the player.
			is_grounded = false;
			m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
			//Debug.Log("jumping (grounded & jump)");
		}
	}

	private void WallSlide()
	{
		if(is_touching_wall && !is_grounded && m_Rigidbody2D.velocity.y <0)
		{
			is_wall_sliding = true;
		}
		else
		{
			is_wall_sliding = false;
		}

		//wall slide
		if(is_wall_sliding)
		{
			//Debug.Log("wall sliding");
			m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, wall_slide_speed);
		}
	}

	private void WallJump()
	{
		if((is_wall_sliding || (is_touching_wall && !is_grounded)) && m_jump)
		{
			//we can wall jump
			//Debug.Log("is wall sliding:  " + is_wall_sliding);
			//Debug.Log("is touching wall :  " + is_touching_wall);
		    //Debug.Log("m_jump:  " + m_jump);
			m_Rigidbody2D.AddForce(new Vector2(wall_jump_force * wall_jump_direction * wall_jump_angle.x, wall_jump_force * wall_jump_angle.y), ForceMode2D.Impulse);
			m_jump = false;
		}
	}


	private void Flip()
	{
		if(!is_wall_sliding)
		{
			wall_jump_direction *= -1; //flip direction when jumping off wall

			// Switch the way the player is labelled as facing.
			m_FacingRight = !m_FacingRight;

			// Multiply the player's x local scale by -1.
			Vector3 theScale = transform.localScale;
			theScale.x *= -1;
			transform.localScale = theScale;

		}
		
	}

	private void OnDrawGizmosSelected() //allows us to see the bounding box
	{
		//for ground check
		Gizmos.color = Color.blue;
		Gizmos.DrawCube(wall_check.position, wall_check_size);


	}
}