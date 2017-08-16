using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using XInputDotNetPure;

public class PlayerMotor : Bolt.EntityBehaviour<IPlayerStates>
{

    public struct State
    {
        public Quaternion rotation;
        public Vector3 velocity;
        public bool grounded;
    }

    public enum CharacterState
    {
        Walking,
        Jumping,
        Sliding,
        Falling,
        Swinging,
        Shattering,
        Pulled,
        Dead

    }

    public CharacterState _characterState;

    State _state;

    Rigidbody _rb;

    Transform _groundedSphere;

    public PlayerCamera _cam;





    public float speed;
    public float jumpVelocity;
    public float maxVelocity;
    public float reelSpeed;

    public float minHitImpulse;
    public float minSlideSpeed;


    public bool isHooked;
    public int team;
    public float canAttachTime;
    public float canAttachTimer;
    public float swingSpeedMultiplier;

    bool _isGrounded;
    Vector3 _ropeForce;
    Vector3 _movement;
    bool _isDead;
    bool _isSliding;
    bool _reel;


    bool _jump;
    bool _leftMouseDown;
    bool _rightMouseDown;
    bool _isGrappling;

    Hook hook;
    Rope rope;


    public float minYPlayerPos;
    private Vector3 _movementInput;
    public float maxRopeForce;

    public float swingingControlMultiplier;
    public float swingingDrag;
    public float slidingDrag;

    public float movementSmoothSpeed;
    public float minCordLengthDrag;

    Animator anim;

    public bool atMaxSpeed;

    Vector3 _modelForwardDestination;
    Vector3 _modelUpDestination;
    Vector3 _currentModelForward;

    Vector3 _currentModelUp;

    public float modelForwardBaseLerpSpeed;
    float _modelForwardLerpSpeed;
    public float modelForwardLerpRatio;


    Quaternion _camRotation;
    public bool entityAttached;

    Quaternion _respawnLookDirection;
    Vector3 _respawnPos;

    public ParticleSystem deathExplosion;
    public ParticleSystem leftEye;
    public ParticleSystem rightEye;

    public SkinnedMeshRenderer modelRenderer;

    public float deathTime;
    // Use this for initialization
    void Awake()
    {


        _rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

        _groundedSphere = transform.GetChild(0);

        hook = transform.GetChild(2).GetChild(0).GetComponent<Hook>();

        rope = transform.GetChild(2).GetChild(1).GetComponent<Rope>();



    }
    public override void Attached()
    {

        _characterState = CharacterState.Walking;

        SetCharacterState(CharacterState.Falling);


        entityAttached = true;
        hook.entityAttached = true;
        rope.entityAttached = true;

    }

    void SetCharacterState(CharacterState newState)
    {

        if (newState != _characterState)
        {
            if (_characterState == CharacterState.Walking)
            {
                AudioManager.Instance.PlayClipFromObject("Stop_SFX_CHAR_RunLoop", anim.gameObject);
            }
            if (_characterState == CharacterState.Pulled)
            {
                canAttachTimer = 0;
            }
            if (_characterState == CharacterState.Swinging || _characterState == CharacterState.Sliding)
            {
                _rb.drag = 0;
            }
            if (newState != CharacterState.Falling)
            {
                modelForwardLerpSpeed = modelForwardBaseLerpSpeed;
            }

            _characterState = newState;
            StopAllCoroutines();
            switch (_characterState)
            {
                case CharacterState.Walking:
                    StartCoroutine(Walking());
                    break;
                case CharacterState.Jumping:
                    StartCoroutine(Jumping());
                    break;
                case CharacterState.Sliding:
                    StartCoroutine(Sliding());
                    break;
                case CharacterState.Falling:
                    StartCoroutine(Falling());
                    break;
                case CharacterState.Swinging:
                    StartCoroutine(Swinging());

                    break;
                case CharacterState.Shattering:
                    StartCoroutine(Shattering());

                    break;
                case CharacterState.Pulled:
                    StartCoroutine(Pulled());

                    break;
                case CharacterState.Dead:
                    StartCoroutine(Dead());
                    break;
            }
        }
    }

    void AnyStateCheck()
    {
        CheckGrounded();
        if (_jump && _isGrounded)
        {
            SetCharacterState(CharacterState.Jumping);
        }

        if (!_isGrounded)
        {

            SetCharacterState(CharacterState.Falling);
        }

        if (_movement.magnitude < 0.01f && _isGrounded)
        {
            if (_rb.velocity.magnitude > 0.1f)
            {
                SetCharacterState(CharacterState.Sliding);
            }
        }
        else if (_movement.magnitude > 0.01f && _isGrounded)
        {

            SetCharacterState(CharacterState.Walking);

        }

        if (state.RopeAttached)
        {
            SetCharacterState(CharacterState.Swinging);
        }
        if (state.IsHooked)
        {
            SetCharacterState(CharacterState.Pulled);

        }
    }

    IEnumerator Walking()
    {


        anim.ResetTrigger("Fall");

        anim.SetTrigger("Walking");


        while (true)
        {

            //if the player is trying to jump, and can, set their Y velocity to their jump velocity
            //if not, make them fall

            Vector3 flattenedForward = _camRotation * Vector3.forward;
            flattenedForward.y = 0;

            modelForwardDestination = flattenedForward;

            Vector3 newVelocity = Vector3.MoveTowards(_rb.velocity, _movement, movementSmoothSpeed);


            newVelocity.y = _rb.velocity.y;
            _rb.velocity = newVelocity;



            yield return null;




            AnyStateCheck();


        }
    }
    IEnumerator Sliding()
    {
        anim.ResetTrigger("Walking");

        _rb.drag = slidingDrag;

        while (true)
        {
            yield return null;
            AnyStateCheck();

        }
    }
    IEnumerator Jumping()
    {

        _rb.velocity = new Vector3(_rb.velocity.x, jumpVelocity, _rb.velocity.z);

        //_rb.AddForce(jumpVelocity * Vector3.up);
        while (true)
        {

            yield return new WaitForFixedUpdate();

            CheckGrounded();
            if (_isGrounded && !_jump)
            {


                SetCharacterState(CharacterState.Walking);
            }
            if (state.RopeAttached)
            {
                SetCharacterState(CharacterState.Swinging);
            }
        }
    }
    IEnumerator Running()
    {
        yield return null;
    }
    IEnumerator Falling()
    {
        Vector3 cameraRelativeForward = _camRotation * Vector3.forward;
        cameraRelativeForward.y = 0;

        modelForwardDestination = cameraRelativeForward;


        AudioManager.Instance.PlayClipFromProxy("Play_AMB_WindSlow", gameObject);

        anim.ResetTrigger("Walking");
        anim.SetTrigger("Fall");


        while (true)
        {

            yield return new WaitForFixedUpdate();
            if (_rb.velocity.magnitude > maxVelocity)
            {
                _rb.velocity = _rb.velocity * 0.75f;
            }


            CheckGrounded();
            if (_isGrounded)
            {
                SetCharacterState(CharacterState.Walking);
            }
            if (state.RopeAttached)
            {
                SetCharacterState(CharacterState.Swinging);
            }
        }
    }
    IEnumerator Swinging()
    {

        _rb.drag = swingingDrag;


        modelUpDestination = transform.up;
        currentModelUp = transform.up;

        anim.ResetTrigger("Walking");
        anim.SetTrigger("Fall");

        while (true)
        {
            _ropeForce = rope.GetForce();

            yield return new WaitForFixedUpdate();

            _rb.AddForce(_ropeForce);

            //   _rb.velocity *= 0.5f;

            if (!rope.firing)
            {
                _rb.AddForce(_movement * swingingControlMultiplier);

            }

            if (_ropeForce.magnitude > 1)
            {
                Vector3 swingSpeed = _rb.velocity.normalized * swingSpeedMultiplier;



                swingSpeed.y = Mathf.Clamp(swingSpeed.y, -Mathf.Abs(swingSpeed.y), 0);

                _rb.AddForce(swingSpeed);
            }

            _rb.velocity = Vector3.ClampMagnitude(_rb.velocity, maxVelocity);



            CheckGrounded();
            if (rope.points.Count >= 1 && _ropeForce.magnitude >= 0.5f)
            {

                Vector3 pointToRope = (rope.points[rope.points.Count - 1].position - transform.position).normalized;

                modelUpDestination = pointToRope;

                modelForwardDestination = transform.forward;
            }

            if (state.IsHooked)
            {
                SetCharacterState(CharacterState.Pulled);
            }
            if (!state.RopeAttached)
            {
                if (_isGrounded)
                {
                    SetCharacterState(CharacterState.Walking);
                }
                AnyStateCheck();
            }

        }
    }
    IEnumerator Dashing()
    {
        yield return null;
    }
    IEnumerator Shattering()
    {
        yield return null;
    }
    IEnumerator Pulled()
    {
        AudioManager.Instance.PlayClipFromObject("Play_SFX_CHAR_Push", gameObject);
       
        while (true)
        {
            yield return new WaitForFixedUpdate();
            if (!state.IsHooked)
            {
                AnyStateCheck();
            }
        }
    }
    IEnumerator Dead()
    {
        Vector3 spawnEulers = respawnLookDirection.eulerAngles;
        _rb.constraints = RigidbodyConstraints.FreezeAll;
        
        deathExplosion.Play();
        modelRenderer.enabled = false;
        float deathTimer = deathTime;
        GetComponentInChildren<JointEnergy>().HideEnergies();
        leftEye.Stop();
        rightEye.Stop();
        _isDead = true;

        while (deathTimer > 0)
        {
            deathTimer -= BoltNetwork.frameDeltaTime;

            if (BoltNetwork.isServer)
            {
                state.CanAttach = false;

            }
            canAttachTimer = 0;

            yield return new WaitForFixedUpdate();

        }
        leftEye.Play();
        rightEye.Play();
        GetComponentInChildren<JointEnergy>().ShowEnergies();

        deathExplosion.Stop();
        modelRenderer.enabled = true;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        isHooked = false;
        if (BoltNetwork.isServer)
        {
            transform.position = GameManager.instance.SpawnLocation(team).position;
            state.IsHooked = false;
        }
        if (_cam)
        {
            _cam._pitch = spawnEulers.x;
            _cam._yaw = spawnEulers.y;
        }

        _state.velocity = Vector3.zero;


        _rb.velocity = Vector3.zero;


        _isDead = false;

        SetCharacterState(CharacterState.Falling);

    }



    void RemoveRope()
    {
        RemoveCord RemoveCordEvent = RemoveCord.Create(entity);
        RemoveCordEvent.IsRope = true;
        RemoveCordEvent.Send();

    }
    void RemoveHook()
    {
        RemoveCord removeRopePointEvent = RemoveCord.Create(entity);
        removeRopePointEvent.IsRope = false;
        removeRopePointEvent.Send();

    }


    public void SetState(Quaternion rotation, Vector3 velocity)
    {
        _rb.velocity = velocity;

    }
    public void UpdateInputs(Vector3 movementInput, bool jump, bool reel, Quaternion camRotation)
    {

        _camRotation = camRotation;

        Vector3 camRelativeRightMovement = (_camRotation * Vector3.right).normalized;
        Vector3 camRelativeForwardMovement = (_camRotation * Vector3.forward).normalized;

        camRelativeRightMovement.y = 0;
        camRelativeForwardMovement.y = 0;

        if(camRelativeForwardMovement.magnitude < 1 && camRelativeForwardMovement.magnitude > 0)
        {
            camRelativeForwardMovement *= 1 / camRelativeForwardMovement.magnitude;
        }

        if (camRelativeRightMovement.magnitude < 1 && camRelativeRightMovement.magnitude > 0)
        {
            camRelativeRightMovement *= 1 / camRelativeRightMovement.magnitude;
        }



        _movementInput = (movementInput.x * camRelativeRightMovement) + (movementInput.z * camRelativeForwardMovement);
        _movementInput.y = 0;

        anim.SetFloat("HorizontalInput", movementInput.x);
        anim.SetFloat("VerticalInput", movementInput.z);



        _movement = _movementInput * speed;

        _jump = jump;

        _state.grounded = _isGrounded;

        _reel = reel;


    }

    public State GetState()
    {
        _state.velocity = _rb.velocity;

        return _state;
    }





    // Update is called once per frame

    //called on all proxies for every client
    //good for taking in State information (think: updating rope linerenderer positions
    void FixedUpdate()
    {
        if (entityAttached && BoltNetwork.isServer)
        {


            state.IsHooked = isHooked;
            if (!state.CanAttach)
            {
                if (canAttachTimer < canAttachTime)
                {
                    canAttachTimer += BoltNetwork.frameDeltaTime;

                }
                else
                {
                    state.CanAttach = true;
                }
            }
            if (_reel)
            {
                rope.Reel(reelSpeed);
                //       _cordLength -= cordRetractSpeed;

            }

            if (transform.position.y < minYPlayerPos && _characterState != CharacterState.Dead)
            {
                SendDieEvent();
            }




            if (_characterState == CharacterState.Falling)
            {
                modelForwardLerpSpeed = modelForwardBaseLerpSpeed / modelForwardLerpRatio;
            }
            else
            {
                modelForwardLerpSpeed = modelForwardBaseLerpSpeed;
            }

            if (_characterState == CharacterState.Swinging)
            {

                currentModelUp = Vector3.Slerp(currentModelUp, modelUpDestination, modelForwardLerpSpeed);


                transform.up = Vector3.Slerp(transform.up, currentModelUp.normalized, 0.5f);

            }
            else
            {


                float lerpSpeed = 0.5f;

                if (_characterState == CharacterState.Swinging)
                {
                    lerpSpeed = 0.1f;
                }

                currentModelForward = Vector3.Slerp(currentModelForward, modelForwardDestination, modelForwardLerpSpeed);

                    transform.forward = Vector3.Slerp(transform.forward, currentModelForward.normalized, lerpSpeed);
                





            }
            atMaxSpeed = (_rb.velocity.magnitude >= 2 * maxVelocity / 3) && _ropeForce.magnitude >= 1 && _characterState == CharacterState.Swinging && _reel;

            if (atMaxSpeed)
            {
                state.ControllerVibration = 0.5f;
            }
            else
            {
                state.ControllerVibration = 0;

            }

        }

        if (_isDead)
        {
            _isDead = false;
        }
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            SendDieEvent();
        }


    }

    public override void SimulateController()
    {
        base.SimulateController();
        GamePad.SetVibration(0, state.ControllerVibration, state.ControllerVibration);

    }

    void CheckGrounded()
    {
        if (_groundedSphere != null)
        {
            //the player is grounded if this overlap sphere is touching more than one collider
            _isGrounded = Physics.OverlapSphere(_groundedSphere.position, _groundedSphere.localScale.x / 2, ~LayerMask.GetMask("Ignore Raycast")).Length > 1;
        }
        _state.grounded = _isGrounded;

    }
    private void OnCollisionEnter(Collision collision)
    {
        if (BoltNetwork.isServer)
        {
            CheckGrounded();
            if (collision.impulse.magnitude > minHitImpulse && !_isGrounded && !_isDead && _characterState == CharacterState.Pulled)
            {
                SendDieEvent();
            }
        }
    }

    void SendDieEvent()
    {
        if (BoltNetwork.isServer)
        {
            Die dieEvent = Die.Create(entity);
            dieEvent.LookDirection = GameManager.instance.SpawnLocation(team).rotation;
            dieEvent.Position = GameManager.instance.SpawnLocation(team).position;
            dieEvent.Send();
        }
    }



    public void Respawn(Quaternion newLookDirection, Vector3 spawnPos)
    {
        RemoveRope();
        RemoveHook();
        
        HookPoint[] hookPoints = GetComponentsInChildren<HookPoint>();
        for(int i = 0; i < hookPoints.Length; i++)
        {
            BoltNetwork.Destroy(hookPoints[i].gameObject);
        }

        respawnLookDirection = newLookDirection;
        respawnPos = spawnPos;
        AudioManager.Instance.PlayClipFromObject("Play_SFX_CHAR_Shatter", gameObject);

        SetCharacterState(CharacterState.Dead);


    }



}
