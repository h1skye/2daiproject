using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
    public Vector2 speed = new Vector2(5, 5);
    public float dodgeSpeed = 3;

    private bool dodging = false;
    private Vector3 movement;
    private float inputX, inputY;

    private Animator animator;


    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    // Update is called once per frame
    void Update()
    {
        
        if (dodging == false)
        {
            XYmovement();
            dodgeMovement();
        }
    }

    private void XYmovement()
    {
        inputX = Input.GetAxis("Horizontal");
        inputY = Input.GetAxis("Vertical");

        if (inputX != 0 || inputY != 0)
        {
            animator.SetBool("walking", true);
            animator.SetFloat("moveX", inputX);
            animator.SetFloat("moveY", inputY);
        }
        else
        {
            animator.SetBool("walking", false);
        }

        movement = new Vector3(speed.x * inputX, speed.y * inputY, 0);
        movement *= Time.deltaTime;

        transform.Translate(movement);
    }

    private void dodgeMovement()
    {
        bool inputDodge = Input.GetKey(KeyCode.Space);
        if (inputDodge == true)
        {
            dodging = true;
            inputX = Input.GetAxis("Horizontal");
            inputY = Input.GetAxis("Vertical");

            movement = new Vector3(speed.x * dodgeSpeed * inputX, speed.y * dodgeSpeed * inputY, 0);
            movement *= Time.deltaTime;

            transform.Translate(movement);
            dodging = false;
        }
        inputDodge = false;
    }
}
