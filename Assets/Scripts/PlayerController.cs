using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float jumpForce = 16f;
    public float gravity = -40f;

    [Header("Collision Settings")]
    public LayerMask groundLayer;
    public Vector2 colliderSize = new Vector2(0.9f, 1.8f);
    public Vector2 colliderOffset = Vector2.zero;

    [Header("Coyote Time Settings")]
    public float coyoteTime = 0.1f;
    private float coyoteTimeCounter = 0f;

    [Header("Jump Buffer Settings")]
    public float jumpBufferTime = 0.1f;
    private float jumpBufferCounter = 0f;

    [Header("Grapple Settings")]
    public float grappleSpeed = 25f;
    public float grappleRadius = 10f;
    public GameObject grappleMarker;
    int currentTargetIndex = 0;

    [Header("Grapple Duration UI")]
    public float grappleModeDuration = 2.5f;
    private float grappleModeTimer;
    public GameObject grappleTimerUI;
    public Image grappleTimerBar;

    private Vector2 velocity;
    private bool isGrounded;

    private bool isGrappleMode = false;
    private Transform currentGrappleTarget;
    private List<Transform> grappleTargets = new List<Transform>();
    private bool isGrappling = false;

    private LineRenderer ropeLine;
    private SpriteRenderer markerRenderer;

    private void Start()
    {
        if (grappleMarker != null)
        {
            markerRenderer = grappleMarker.GetComponent<SpriteRenderer>();
            markerRenderer.enabled = false;
        }

        ropeLine = GetComponentInChildren<LineRenderer>();
        if (ropeLine != null)
            ropeLine.enabled = false;

        if (grappleTimerUI != null)
            grappleTimerUI.SetActive(false);
    }

    private void Update()
    {
        // Toggle grapple mode
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isGrappleMode)
            {
                isGrappleMode = false;
                Time.timeScale = 1f;

                if (currentGrappleTarget != null)
                    isGrappling = true;

                if (markerRenderer != null)
                    markerRenderer.enabled = false;

                if (grappleTimerUI != null)
                    grappleTimerUI.SetActive(false);
            }
            else if (!isGrappling)
            {
                EnterGrappleMode();
            }
        }

        // Grapple target selection and timer
        if (isGrappleMode)
        {
            HandleGrappleTargetSelection();

            grappleModeTimer -= Time.unscaledDeltaTime;

            if (grappleTimerBar != null)
                grappleTimerBar.fillAmount = grappleModeTimer / grappleModeDuration;

            if (grappleModeTimer <= 0f)
            {
                isGrappleMode = false;
                Time.timeScale = 1f;

                if (markerRenderer != null)
                    markerRenderer.enabled = false;

                if (grappleTimerUI != null)
                    grappleTimerUI.SetActive(false);
            }

            return;
        }

        // Grappling logic with weight-based behavior
        if (isGrappling && currentGrappleTarget != null)
        {
            string tag = currentGrappleTarget.tag;
            Vector3 dir = (currentGrappleTarget.position - transform.position).normalized;

            switch (tag)
            {
                case "Light":
                    currentGrappleTarget.position = Vector3.MoveTowards(
                        currentGrappleTarget.position,
                        transform.position,
                        grappleSpeed * Time.deltaTime
                    );
                    break;

                case "Medium":
                    transform.position += dir * (grappleSpeed / 2f) * Time.deltaTime;
                    currentGrappleTarget.position -= dir * (grappleSpeed / 2f) * Time.deltaTime;
                    break;

                case "Heavy":
                default:
                    transform.position += dir * grappleSpeed * Time.deltaTime;
                    break;
            }

            if (ropeLine != null)
            {
                ropeLine.enabled = true;
                ropeLine.SetPosition(0, transform.position);
                ropeLine.SetPosition(1, currentGrappleTarget.position);
            }

            float dist = Vector2.Distance(transform.position, currentGrappleTarget.position);
            if (dist < 0.2f)
            {
                isGrappling = false;
                currentGrappleTarget = null;

                if (ropeLine != null)
                    ropeLine.enabled = false;
            }

            return;
        }

        // Jump input buffering
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.unscaledDeltaTime;

        float moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput > 0)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (moveInput < 0)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        velocity.x = moveInput * moveSpeed;
        velocity.y += gravity * Time.deltaTime;

        isGrounded = Physics2D.BoxCast(transform.position + (Vector3)colliderOffset, colliderSize, 0f, Vector2.down, 0.05f, groundLayer);

        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.unscaledDeltaTime;

        if (coyoteTimeCounter > 0f && jumpBufferCounter > 0f)
        {
            velocity.y = jumpForce;
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;
        }

        if (isGrounded && velocity.y <= 0f)
            velocity.y = -2f;

        Vector2 move = velocity * Time.deltaTime;

        if (move.x != 0)
        {
            Vector2 dir = Vector2.right * Mathf.Sign(move.x);
            RaycastHit2D hitX = Physics2D.BoxCast(transform.position + (Vector3)colliderOffset, colliderSize, 0f, dir, Mathf.Abs(move.x), groundLayer);
            if (hitX.collider != null)
            {
                move.x = 0f;
                velocity.x = 0f;
            }
        }

        if (move.y != 0)
        {
            Vector2 dir = Vector2.up * Mathf.Sign(move.y);
            RaycastHit2D hitY = Physics2D.BoxCast(transform.position + (Vector3)colliderOffset, colliderSize, 0f, dir, Mathf.Abs(move.y), groundLayer);
            if (hitY.collider != null)
            {
                float allowedMove = Mathf.Min(Mathf.Abs(move.y), hitY.distance - 0.01f);
                allowedMove = Mathf.Max(allowedMove, 0f);
                move.y = allowedMove * Mathf.Sign(move.y);
                velocity.y = 0f;
            }
        }

        transform.position += (Vector3)move;
    }

    void EnterGrappleMode()
    {
        isGrappleMode = true;
        Time.timeScale = 0.1f;
        grappleTargets.Clear();
        grappleModeTimer = grappleModeDuration;

        if (grappleTimerUI != null)
            grappleTimerUI.SetActive(true);

        if (grappleTimerBar != null)
            grappleTimerBar.fillAmount = 1f;

        string[] grappleTags = new string[] { "Light", "Medium", "Heavy" };

        foreach (string tag in grappleTags)
        {
            GameObject[] found = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in found)
            {
                if (Vector2.Distance(transform.position, obj.transform.position) <= grappleRadius)
                {
                    Vector3 viewportPoint = Camera.main.WorldToViewportPoint(obj.transform.position);
                    if (viewportPoint.z > 0 && viewportPoint.x >= 0 && viewportPoint.x <= 1 && viewportPoint.y >= 0 && viewportPoint.y <= 1)
                    {
                        grappleTargets.Add(obj.transform);
                    }
                }

            }
        }

        currentGrappleTarget = FindNearestTarget();
        currentTargetIndex = grappleTargets.IndexOf(currentGrappleTarget);
        UpdateGrappleMarker();
    }

    void HandleGrappleTargetSelection()
    {
        if (grappleTargets.Count <= 1) return;

        bool cycleLeft = Input.GetKeyDown(KeyCode.A);
        bool cycleRight = Input.GetKeyDown(KeyCode.D);

        if (cycleLeft)
        {
            currentTargetIndex = (currentTargetIndex - 1 + grappleTargets.Count) % grappleTargets.Count;
            currentGrappleTarget = grappleTargets[currentTargetIndex];
            UpdateGrappleMarker();
        }
        else if (cycleRight)
        {
            currentTargetIndex = (currentTargetIndex + 1) % grappleTargets.Count;
            currentGrappleTarget = grappleTargets[currentTargetIndex];
            UpdateGrappleMarker();
        }

        // Optional: still support W/S for vertical direction-based target switching
        Vector2 input = Vector2.zero;
        if (Input.GetKeyDown(KeyCode.W)) input = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.S)) input = Vector2.down;

        if (input != Vector2.zero)
        {
            Transform best = null;
            float bestDot = -1f;

            foreach (Transform t in grappleTargets)
            {
                if (t == currentGrappleTarget) continue;

                Vector2 dir = (t.position - transform.position).normalized;
                float dot = Vector2.Dot(input.normalized, dir);
                if (dot > bestDot)
                {
                    best = t;
                    bestDot = dot;
                }
            }

            if (best != null)
            {
                currentGrappleTarget = best;
                currentTargetIndex = grappleTargets.IndexOf(best);
                UpdateGrappleMarker();
            }
        }
    }

    Transform FindNearestTarget()
    {
        Transform best = null;
        float closest = Mathf.Infinity;
        foreach (Transform t in grappleTargets)
        {
            float d = Vector2.Distance(transform.position, t.position);
            if (d < closest)
            {
                best = t;
                closest = d;
            }
        }
        return best;
    }

    void UpdateGrappleMarker()
    {
        if (markerRenderer == null || grappleMarker == null) return;

        if (currentGrappleTarget != null)
        {
            grappleMarker.transform.position = currentGrappleTarget.position;
            markerRenderer.enabled = true;
        }
        else
        {
            markerRenderer.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3)colliderOffset, colliderSize);
    }
}
