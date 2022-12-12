using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public GameObject earth;
    Rigidbody rb;
    public float cameraMoveSpeed;
    private float horizontalInput;
    private float verticalInput;
    private float mouseInput;
    private float distanceScale;
    private float distanceToEarth;

    private bool moveCamera;

    private bool moveHorizontal;
    private bool moveVertical;

    void Start()
    {
        //Fetch the Rigidbody component you attach from your GameObject
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        mouseInput = Mathf.Max(-1, Mathf.Min(1, Input.mouseScrollDelta.y));

        transform.LookAt(earth.transform.position);

        float verticalRotation = gameObject.transform.localRotation.eulerAngles.x;

        if (verticalRotation > 180f)
        {
            verticalRotation -= 360f;
        }

        distanceToEarth = Vector3.Distance(gameObject.transform.position, earth.transform.position);

        if (verticalInput != 0)
        {
            // If going up and at top of earth
            if (verticalInput > 0 && (verticalRotation > 70))
            {
                //return;
            } else if (verticalInput < 0 && (verticalRotation < -70))
            {
                //return;
            } else
            {
                moveVertical = true;
            }
            
            //moveCamera = true;


        }

        if (horizontalInput != 0)
        {
            moveHorizontal = true;
        }

        if (mouseInput != 0)
        {
            if ((distanceToEarth <= 30 && mouseInput > 0) || (distanceToEarth >= 1300 && mouseInput < 0))
            {
                // Dont zoom
            } else
            {
                ZoomCamera();
            }

            
        }

        if (distanceToEarth < 25)
        {
            rb.transform.Translate(Vector3.back);
        }

        if (distanceToEarth > 1350)
        {
            rb.transform.Translate(Vector3.forward);
        }
    }

    private void FixedUpdate()
    {

        if (moveHorizontal)
        {
            MoveHorizontal();
            moveHorizontal = false;
        }

        if (moveVertical)
        {
            MoveVertical();
            moveVertical = false;
        }
    }

    private void MoveHorizontal()
    {
        // Ensure same speed no matter the distance from earth
        distanceScale = distanceToEarth / 10;

        Vector3 movement = new Vector3(horizontalInput, 0f, 0f);
        Vector3 totalMovement = movement * cameraMoveSpeed * distanceScale * Time.fixedDeltaTime;
        rb.transform.Translate(totalMovement);


    }

    private void MoveVertical()
    {
        // Ensure same speed no matter the distance from earth
        distanceScale = distanceToEarth / 10;

        Vector3 movement = new Vector3(0f, verticalInput, 0f);
        Vector3 totalMovement = movement * cameraMoveSpeed * distanceScale * Time.fixedDeltaTime;
        rb.transform.Translate(totalMovement);


    }



    private void ZoomCamera()
    {
        // Ensure same speed no matter the distance from earth
        distanceScale = distanceToEarth / 10;

        Vector3 movement = new Vector3(0f, 0f, mouseInput);
        Vector3 totalMovement = movement * cameraMoveSpeed * distanceScale * Time.deltaTime;
        rb.transform.Translate(totalMovement);
    }

    public void PresetPosition(GameObject position)
    {
        rb.transform.position = position.transform.position;
    }


}
