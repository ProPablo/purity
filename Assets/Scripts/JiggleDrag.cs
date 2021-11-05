using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JiggleDrag : MonoBehaviour
{
    public float k = 5f;
    public float b = 2f;
    private Rigidbody grabbed = null;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                grabbed = hit.collider.attachedRigidbody;
            }
        }

        if (grabbed)
        {
            if (Input.GetMouseButton(0))
            {
                //Notice how this ray direction is curved to the frustrum of the camera, but if used in orthographic, the ray direction is straight
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
                var pos = ray.origin + ray.direction * 10;
                // grabbed.MovePosition(pos);
                var force = pos - grabbed.position;
                force = force * k;
                var damper = grabbed.velocity * b;
                grabbed.AddForce((force -damper) * Time.deltaTime);
            }

            if (Input.GetMouseButtonUp(0))
            {
                grabbed = null;
            }
            
        }
    }
}