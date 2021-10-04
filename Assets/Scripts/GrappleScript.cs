using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleScript : MonoBehaviour
{
    public PlayerController controller;
    public Rigidbody rb;
    public Transform hands, gunTip;
    public float maxGrapple = 9999f;
    public LayerMask whatIsGrappleable;
    public LineRenderer grappleRope;

    private Vector3 grapplePoint;
    private SpringJoint spring;
    // public float grappleForce = 100f;
    public float directionForce = 300f;
    public float grappleClamp = 100f;

    [Header("Grapple Options")] public float springForce = 8.5f;
    public float minDist = 0.1f;

    public float maxDist = 0.4f;

    //if player mass is low, increase this to get more effect from the grapple
    public float massScale = 4.5f;

    public Animator gunAnim;

    public PhysicMaterial grappleMat;
    public PhysicMaterial normalMat;

    private Collider col;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        hands = controller.hands;
        grappleRope.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1) && spring == null)
        {
            StartGrapple();
        }

        if (Input.GetMouseButtonUp(1))
        {
            StopGrapple();
        }
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void FixedUpdate()
    {
        if (spring != null)
        {
            Vector3 dir = (grapplePoint - transform.position).normalized;
            rb.AddForce(dir * directionForce * Time.deltaTime);
        }
    }

    void StartGrapple()
    {
        print("start grapple");

        gunAnim.SetBool("isGrapple", true);

        RaycastHit hit;
        if (Physics.Raycast(hands.position, hands.forward, out hit, maxGrapple, whatIsGrappleable))
        {
            // controller.reverseReset = false;
            // controller.currentForce = grappleForce;
            grappleRope.enabled = true;
            col.material = grappleMat;

            grapplePoint = hit.point;

            spring = gameObject.AddComponent<SpringJoint>();
            spring.autoConfigureConnectedAnchor = false;
            spring.connectedAnchor = grapplePoint;
            float distance = Vector3.Distance(hands.position, grapplePoint);
            spring.maxDistance = maxDist;
            spring.minDistance = minDist;

            //spring force
            spring.spring = springForce;
            //reduces oscillation
            spring.damper = 2f;
            //effect of mass of object (keep low for gravity to keep affecting)
            spring.massScale = massScale;

            //also tune the actual weight of the character otherwise they will go in much higher y than x
        }
    }

    void StopGrapple()
    {
        print("stop grapple");
        // controller.reverseReset = true;
        // controller.currentForce = controller.walkingForce;
        col.material = normalMat;
        grappleRope.enabled = false;

        gunAnim.SetBool("isGrapple", false);
        Destroy(spring);
    }

    void DrawRope()
    {
        if (!spring) return;
        grappleRope.SetPosition(0, gunTip.position);
        grappleRope.SetPosition(1, grapplePoint);
    }

    public bool IsGrappling()
    {
        return spring != null;
    }
}