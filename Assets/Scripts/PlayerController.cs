using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private readonly float forwardSpeed = 10;
    private readonly float backwardSpeed = 5;
    private readonly float rotateSpeed = 120;
    private readonly float jumpHeight = 2.0f;
    private readonly float gravity = 9.81f * 3;

    private float velocity, groundTimer;
    private CharacterController controller;
    private int init;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // fix rotation at start
        if (init == 0)
        {
            Cursor.lockState = CursorLockMode.Locked;
            ++init;
        }
        else if (init == 1)
        {
            Input.ResetInputAxes();
            ++init;
        }

        // check on ground & apply gravity
        bool onGround = controller.isGrounded;
        if (onGround)
        {
            groundTimer = 0.1f;
            if (velocity < 0)
                velocity = 0;
        }
        else
            groundTimer -= Time.deltaTime;
        velocity -= gravity * Time.deltaTime;

        // move
        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");
        Vector3 move;

        int dir;
        if (vertical > 0)
            dir = 10;
        else if (vertical < 0)
            dir = -10;
        else
            dir = 0;

        if (horizontal > 0)
            dir += 1;
        else if (horizontal < 0)
            dir -= 1;

        if (dir != 0)
        {
            var angle = dir switch
            {
                9 => -45,
                11 => 45,
                -1 => -90,
                1 => 90,
                -11 => -135,
                -9 => 135,
                -10 => 180,
                _ => (float)0,
            };
            float speed;
            if (Mathf.Abs(angle) >= 135)
                speed = backwardSpeed;
            else
                speed = forwardSpeed;
            move = Quaternion.Euler(0, angle, 0) * transform.forward * speed;
        }
        else
            move = Vector3.zero;

        // rotate
        transform.Rotate(0, Input.GetAxis("Mouse X") * 5 * rotateSpeed * Time.deltaTime, 0);

        // jumping
        if (groundTimer > 0 && Input.GetButtonDown("Jump"))
        {
            groundTimer = 0;
            velocity += Mathf.Sqrt(jumpHeight * 2.0f * gravity);
        }

        // move controller
        move.y = velocity;
        controller.Move(move * Time.deltaTime);
    }
}
