using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    private readonly float forwardSpeed = 10;
    private readonly float backwardSpeed = 5;
    private readonly float rotateSpeed = 120;
    private readonly float jumpHeight = 1.5f;
    private readonly float gravity = 9.81f * 3;

    private float velocity, groundTimer, airTimer;
    private CharacterController controller;
    private Vector3 fallingPos;
    private int init;
    private bool falling;

    public GameManager gameManager;
    public Animator animator;
    public GameObject marker;
    public AudioSource hitGround;

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

        // restart after falling into darkness
        if (transform.position.y < -10)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (falling)
        {
            // force move into center of hole so player don't get stuck on edge
            velocity -= gravity * Time.deltaTime;
            fallingPos.y = transform.position.y + velocity * Time.deltaTime;
            Vector3 targetPos = Vector3.Lerp(transform.position, fallingPos, Time.deltaTime * 10);
            targetPos.y = fallingPos.y;
            transform.position = targetPos;
        }
        else
        {
            // check on ground & apply gravity
            bool onGround = controller.isGrounded;
            if (onGround)
            {
                groundTimer = 0.1f;
                if (airTimer > 0.5f)
                    hitGround.Play();
                airTimer = 0;
                if (velocity < 0)
                    velocity = 0;
                if (gameManager.CheckForTile(transform.position + transform.forward * 2.0f, marker))
                {
                    marker.SetActive(true);
                    if (Input.GetMouseButtonDown(0))
                    {
                        gameManager.DestroyMarkedTile();
                        animator.Play("Idle_CheckWatch");
                    }
                }
                else
                    marker.SetActive(false);
            }
            else
            {
                groundTimer -= Time.deltaTime;
                airTimer += Time.deltaTime;
                marker.SetActive(false);
            }
            velocity -= gravity * Time.deltaTime;

            // move
            Vector3 move;
            float vertical = Input.GetAxisRaw("Vertical");
            float horizontal = Input.GetAxisRaw("Horizontal");

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
                {
                    speed = backwardSpeed;
                    animator.SetFloat("Speed_f", 0.4f);
                }
                else
                {
                    speed = forwardSpeed;
                    animator.SetFloat("Speed_f", 1.0f);
                }
                move = Quaternion.Euler(0, angle, 0) * transform.forward * speed;
            }
            else
            {
                move = Vector3.zero;
                animator.SetFloat("Speed_f", 0.0f);
            }

            // rotate
            transform.Rotate(0, Input.GetAxis("Mouse X") * 5 * rotateSpeed * Time.deltaTime, 0);

            // jumping
            if (groundTimer > 0 && Input.GetButtonDown("Jump"))
            {
                groundTimer = 0;
                velocity += Mathf.Sqrt(jumpHeight * 2.0f * gravity);
                animator.SetTrigger("Jump_trig");
            }

            // move controller
            move.y = velocity;
            controller.Move(move * Time.deltaTime);
            if (transform.position.y < 0.5f)
                animator.SetBool("Grounded", false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (falling)
            return;

        if (other.CompareTag("Finish"))
        {
            gameObject.SetActive(false);
            gameManager.Win();
            return;
        }

        GameObject tile = other.gameObject.transform.parent.gameObject;
        if (gameManager.StepOn(tile))
        {
            fallingPos = tile.transform.position;
            falling = true;
            animator.SetBool("Grounded", false);
            marker.SetActive(false);
            GetComponent<Collider>().enabled = false;
        }
    }
}
