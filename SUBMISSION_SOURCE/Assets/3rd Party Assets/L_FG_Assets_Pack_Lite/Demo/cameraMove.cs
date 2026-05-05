using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class cameraMove : MonoBehaviour {

    private float rSpeed = 3.0f;
    private float mSpeed = 20.0f;
    private float X = 0.0f;
    private float Y = 0.0f;

    void Update()
    {
        float mouseX = 0f;
        float mouseY = 0f;
        
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            mouseX = delta.x * 0.05f; // Scale down pixel delta
            mouseY = delta.y * 0.05f;
        }

        X += mouseX * rSpeed;
        Y += mouseY * rSpeed;
        transform.localRotation = Quaternion.AngleAxis(X, Vector3.up);
        transform.localRotation *= Quaternion.AngleAxis(Y, Vector3.left);

        float vertical = 0f;
        float horizontal = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
        }

        transform.position += transform.forward * mSpeed * vertical * Time.deltaTime;
        transform.position += transform.right * mSpeed * horizontal * Time.deltaTime;
    }
}
