using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private float sensX = 100f;
    [SerializeField] private float sensY = 100f;

    [SerializeField] private Transform cam;
    [SerializeField] private Transform orientation;

    private float mouseX;
    private float mouseY;

    private float multiplier = 0.01f;

    private float xRotation;
    private float yRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Menu.Instance.CurrentGameState != Menu.GameState.Playing) return;
        mouseX = Input.GetAxisRaw("Mouse X");
        mouseY = Input.GetAxisRaw("Mouse Y");

        yRotation += mouseX * sensX * multiplier;
        xRotation -= mouseY * sensY * multiplier;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cam.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f); ;
        orientation.transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }
    /// <summary>
    /// Sets mouse Y sensitivity
    /// </summary>
    /// <param name="value"></param>
    private void SetMouseSensitivityY(float value)
    {
        sensY = value;
    }
    /// <summary>
    /// Sets mouse X sensitivity
    /// </summary>
    /// <param name="value"></param>
    private void SetMouseSensitivityX(float value)
    {
        sensX = value;
    }
    /// <summary>
    /// Sets all mouse sensitivity
    /// </summary>
    /// <param name="slider"></param>
    public void SetMouseSensitivity(Slider slider)
    {
        SetMouseSensitivityY(slider.value);
        SetMouseSensitivityX(slider.value);
    }
}