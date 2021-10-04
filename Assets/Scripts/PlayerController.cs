using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public Rigidbody rb;
    public Transform hands;
    public Transform feet;
    public float mouseSens = 100f;
    public float walkingForce = 3000f;
    public float airForce = 900f;
    public float airDrag = 0.5f;
    public float groundDrag = 2f;
    
    public float maxVel = 3f;
    public float currentForce;
    public bool reverseReset = true;
    public bool clampVel = true;
    public bool isWalking;
    
    public float distFromFeet = 0.5f;
    public LayerMask whatIsGround;
    public float jumpForce = 200f;

    Vector2 input;
    float verticalLookRotation = 0;
    bool isGrounded = true;
    private bool jump = false;
    public GrappleScript grapple;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        currentForce = walkingForce;
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSens * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSens * Time.deltaTime;
        verticalLookRotation += mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -60, 60);

        transform.Rotate(Vector3.up * mouseX);
        hands.localEulerAngles = Vector3.left * verticalLookRotation;


        input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));


        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jump = true;
            isGrounded = false;
        }
    }

    private void FixedUpdate()
    {
        Move();
        DetectGround();

        if (jump)
        {
            rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
            isGrounded = false;
            jump = false;
        }
    }

    private void DetectGround()
    {
        Debug.DrawLine(feet.position, feet.position + Vector3.down * distFromFeet, Color.green);
        if (Physics.Raycast(feet.position, Vector3.down, distFromFeet, whatIsGround))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    public void Move()
    {
        Vector3 moveDir = transform.TransformDirection(new Vector3(input.x, 0, input.y));
        //Make powerup (instead if no input, add force negatively)
        if (reverseReset && isGrounded && !grapple.IsGrappling())
        {
            Vector2 currDir = new Vector2(rb.velocity.x, rb.velocity.z).normalized;
            Vector2 horizontalMov = new Vector2(moveDir.x, moveDir.z).normalized;
            if (Vector3.Dot(horizontalMov, currDir) < 0)
            {
                print("reversing");
                rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
                
            }
        }

        if (!isGrounded)
        {
            currentForce = airForce;
            rb.drag = airDrag;
        }
        else
        {
            currentForce = walkingForce;
            rb.drag = groundDrag;
        }
        
        
        rb.AddForce(moveDir * Time.deltaTime * currentForce);
        

        if (clampVel)
        {
            Vector2 horizontalVel = new Vector2(rb.velocity.x, rb.velocity.z);
            horizontalVel = Vector2.ClampMagnitude(horizontalVel, maxVel);
            rb.velocity = new Vector3(horizontalVel.x, rb.velocity.y, horizontalVel.y);
        }
    }
}