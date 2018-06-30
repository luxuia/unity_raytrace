
using UnityEngine;
using System.Collections;

public class CameraMove : MonoBehaviour {


    public float near = 20.0f;
    public float far = 100.0f;

    public float sensitivetyZ = 2f;
    public float sensitivityX = 10f;
    public float sensitivityY = 10f;
    public float sensitivetyMove = 2f;
    public float sensitivetyMouseWheel = 2f;


    void Update() {
        if (Input.GetMouseButton(1)) {
            float rotationX = Input.GetAxis("Mouse X") * sensitivityX;
            float rotationY = Input.GetAxis("Mouse Y") * sensitivityY;
            transform.Rotate(-rotationY, rotationX, 0);
        }

        if (Input.GetAxis("Horizontal") != 0) {
            float rotationZ = Input.GetAxis("Horizontal") * sensitivetyZ;
            transform.position = transform.position + transform.right * rotationZ;
        }
        if (Input.GetAxis("Vertical") != 0) {
            float rotationZ = Input.GetAxis("Vertical") * sensitivetyZ;
            transform.position = transform.position + transform.forward * rotationZ;
        }
    }
}
