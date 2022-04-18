using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShipController : MonoBehaviour
{

    public float forwardSpeed = 25f, strafeSpeed = 7.5f, hoverSpeed = 5f;
    //private float activeForwardSpeed, activeStrafeSpeed, activeHoverSpeed;
    //private float forwardAcceleration = 2.5f, strafeAcceleration = 2f, hoverAcceleration = 2f;

    public float lookRateSpeed = 90f;
    private Vector2 lookInput, screenCenter, mouseDistance;

    private float rollInput;
    public float rollSpeed = 90f, rollAcceleration = 3.5f;

    private Vector2 _move;
    private float _forward;
    private float _turn;

    // Start is called before the first frame update
    void Start()
    {
        screenCenter.x = Screen.width * .5f;
        screenCenter.y = Screen.height * .5f;

        Cursor.lockState = CursorLockMode.Confined;
    }

    private void OnMove(InputValue input) {
        _move = -input.Get<Vector2>();
        //Debug.Log(_move);
    }

    private void OnForward(InputValue input) {
        _forward = input.Get<float>();
        //Debug.Log(_forward);
    }

    private void OnTurn(InputValue input)
    {
        _turn = input.Get<Vector2>().x;
    }

    // Update is called once per frame
    void Update()
    {
        rollInput = Mathf.Lerp(rollInput, _move.x, rollAcceleration * Time.deltaTime);

        transform.Rotate(_move.y * lookRateSpeed * Time.deltaTime, _move.x * lookRateSpeed * Time.deltaTime, rollInput * Time.deltaTime, Space.Self);
        transform.RotateAround(transform.position, Vector3.up, _turn * lookRateSpeed * Time.deltaTime);

        //activeForwardSpeed = Mathf.Lerp(activeForwardSpeed, Input.GetAxisRaw("Vertical") * forwardSpeed, forwardAcceleration * Time.deltaTime);
        //activeStrafeSpeed = Mathf.Lerp(activeStrafeSpeed, Input.GetAxisRaw("Horizontal") * strafeSpeed, strafeAcceleration * Time.deltaTime);
        //activeHoverSpeed = Mathf.Lerp(activeHoverSpeed, Input.GetAxisRaw("Hover") * hoverSpeed, hoverAcceleration * Time.deltaTime);

        transform.position += transform.up * forwardSpeed * _forward * Time.deltaTime;
        //transform.position += (transform.right * activeStrafeSpeed * Time.deltaTime) + (transform.up * activeHoverSpeed * Time.deltaTime);
    }
}
