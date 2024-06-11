using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateClonesForTesting : MonoBehaviour
{
    public float turnSpeed = 2f;
    public float moveSpeed = 0.8f;
    private float pitch = 10f;
    private float yaw = 180f;

    public KeyCode forwardKey = KeyCode.E;
    public KeyCode backKey = KeyCode.D;
    public KeyCode leftKey = KeyCode.S;
    public KeyCode rightKey = KeyCode.F;
    public KeyCode upKey = KeyCode.R;
    public KeyCode downKey = KeyCode.W;


    public GameObject avatar;
    // Start is called before the first frame update
    void Start()
    {
        Camera.main.transform.SetPositionAndRotation(new Vector3(0, 0.94f, 1.55f), Quaternion.Euler(10, 180, 0));
        pitch = 10;
        yaw = 180;

        for (int z = 1; z < 6; z++)
        {
            for (int x = 0; x < z + 1; x++)
            {
                GameObject clone = Instantiate(avatar);
                clone.transform.position = new Vector3((z * -0.5f) + x, 0, -z * 0.25f);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        moveCamera();

        if (Input.GetKey(KeyCode.Alpha1))
        {
            Camera.main.transform.SetPositionAndRotation(new Vector3(0, 0.94f, 0.15f), Quaternion.Euler(10, 180, 0));
            pitch = 10;
            yaw = 180;
        }
        if (Input.GetKey(KeyCode.Alpha2))
        {
            Camera.main.transform.SetPositionAndRotation(new Vector3(0, 0.94f, 0.55f), Quaternion.Euler(10, 180, 0));
            pitch = 10;
            yaw = 180;
        }
        if (Input.GetKey(KeyCode.Alpha3))
        {
            Camera.main.transform.SetPositionAndRotation(new Vector3(0, 0.94f, 1.55f), Quaternion.Euler(10, 180, 0));
            pitch = 10;
            yaw = 180;
        }
        if (Input.GetKey(KeyCode.Alpha4))
        {
            Camera.main.transform.SetPositionAndRotation(new Vector3(0, 0.94f, 2.55f), Quaternion.Euler(10, 180, 0));
            pitch = 10;
            yaw = 180;
        }
        if (Input.GetKey(KeyCode.Alpha5))
        {
            Camera.main.transform.SetPositionAndRotation(new Vector3(0, 0.94f, 3.55f), Quaternion.Euler(10, 180, 0));
            pitch = 10;
            yaw = 180;
        }
        if (Input.GetKey(KeyCode.Alpha6))
        {
            Camera.main.transform.SetPositionAndRotation(new Vector3(0, 0.94f, 4.55f), Quaternion.Euler(10, 180, 0));
            pitch = 10;
            yaw = 180;
        }
        if (Input.GetKey(KeyCode.Alpha7))
        {
            Camera.main.transform.SetPositionAndRotation(new Vector3(0, 0.94f, 5.55f), Quaternion.Euler(10, 180, 0));
            pitch = 10;
            yaw = 180;
        }
        if (Input.GetKey(KeyCode.Alpha8))
        {
            Camera.main.transform.SetPositionAndRotation(new Vector3(0, 0.94f, 6.55f), Quaternion.Euler(10, 180, 0));
            pitch = 10;
            yaw = 180;
        }
        if (Input.GetKey(KeyCode.Alpha9))
        {
            Camera.main.transform.SetPositionAndRotation(new Vector3(0, 0.94f, 7.55f), Quaternion.Euler(10, 180, 0));
            pitch = 10;
            yaw = 180;
        }
    }


    private void moveCamera()
    {
        float speedChange = Input.GetAxis("Mouse ScrollWheel");
        if (speedChange > 0) moveSpeed *= (1 + (speedChange * 10));
        if (speedChange < 0) moveSpeed /= (1 - (speedChange * 10));
        if (moveSpeed < 0.01f) moveSpeed = 0.01f;

        // Only work with the Right Mouse Button pressed
        if (Input.GetMouseButton(1))
        {
            yaw += turnSpeed * Input.GetAxis("Mouse X");
            pitch -= turnSpeed * Input.GetAxis("Mouse Y");
            Camera.main.transform.eulerAngles = new Vector3(pitch, yaw, 0f);

            if (Input.GetKey(KeyCode.E))
            {
                Camera.main.transform.Translate(0, 0, 0.001f * moveSpeed);
            }
            if (Input.GetKey(KeyCode.D))
            {
                Camera.main.transform.Translate(0, 0, -0.001f * moveSpeed);
            }
            if (Input.GetKey(KeyCode.R))
            {
                Camera.main.transform.Translate(0, 0.001f * moveSpeed, 0);
            }
            if (Input.GetKey(KeyCode.W))
            {
                Camera.main.transform.Translate(0, -0.001f * moveSpeed, 0);
            }
            if (Input.GetKey(KeyCode.F))
            {
                Camera.main.transform.Translate(0.001f * moveSpeed, 0, 0);
            }
            if (Input.GetKey(KeyCode.S))
            {
                Camera.main.transform.Translate(-0.001f * moveSpeed, 0, 0);
            }
        }
    }
}
