using Bolt;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using XInputDotNetPure;
using UnityEngine;

public class PlayerController : Bolt.EntityEventListener<IPlayerStates>
{

    PlayerCamera _playerCamera;
    Camera _cam;
    CordController _cordController;

    PlayerMotor _motor;
    GameObject _currentPoint;
    Vector3 _movement;
    Vector3 _rawMovement;
    Vector3 _smoothedMovement;
    Vector3 _smoothedMovementSecondary;

    public float movementBezierSpeed;
    Vector3 cameraRelativeMovement;
    Transform cameraTransform;
    Animator _anim;
    Coroutine _analyticsCoroutine;

    public int controllerIndex = 0;


    public Material team0BodyMaterial;
    public Material team0HeadBowRosesMaterial;
    public Material team0SkirtMaterial;

    public Material team1BodyMaterial;
    public Material team1HeadBowRosesMaterial;
    public Material team1SkirtMaterial;

    public Material teamNullBodyMaterial;
    public Material teamNullHeadBowRosesMaterial;
    public Material teamNullSkirtMaterial;

    public Material invisibleArms;

    Material bodyMaterial;
    Material headBowRosesMaterial;
    Material skirtMaterial;
    public SkinnedMeshRenderer bodyRenderer;

    public Transform cameraFollowJoint;


    bool _jump;
    bool _reel;
    bool _leftMouseDown;
    bool _rightMouseDown;
    bool _isGrappling;

    Quaternion _camRotation;
    Vector3 _camPosition;

    public Vector3 attackRayDirection;
  //  bool _startAnalytics;
  //  bool _loadAnalytics;
  //  bool _findDataSet;

    public bool paused;
    public bool isHooked;


    float timeSinceLastResetState;

    /// <summary>
    /// This is invoked when Bolt is aware of this entity (This script attached to an object with Bolt Entity attached to it).
    /// This function is called on the entity OWNER BEFORE this is sent over the network( so you can set initial state) then
    ///  it is called on the non-Owner when the new network entity is instantiated on those nodes.
    /// </summary>
    public override void Attached()
    {
        _anim = GetComponentInChildren<Animator>();

        state.SetTransforms(state.Transform, transform);
        state.SetAnimator(_anim);



        _motor = GetComponent<PlayerMotor>();
        _cordController = GetComponentInChildren<CordController>();
        _cordController.entityAttached = true;



        if (BoltNetwork.isServer)
        {
            state.Team = GameManager.instance.nextTeam;
            state.CanAttach = true;
            transform.position = GameManager.instance.SpawnLocation(state.Team).position;
            GameManager.instance.BroadcastLockedPlayers();
            SendSetTeamEvent(-1);

        }


    }


    public void SendSetTeamEvent(int newTeam)
    {

        SetTeam setTeamEvent = SetTeam.Create(entity);
        setTeamEvent.NewTeam = newTeam;
        setTeamEvent.Send();

    }
    public override void OnEvent(SetTeam evnt)
    {
        base.OnEvent(evnt);
        if (entity.isOwner)
        {
            state.Team = evnt.NewTeam;
        }
        _motor.team = evnt.NewTeam;

        switch (evnt.NewTeam)
        {
            case -1:
                bodyMaterial = teamNullBodyMaterial;
                headBowRosesMaterial = teamNullHeadBowRosesMaterial;
                skirtMaterial = teamNullSkirtMaterial;
                break;
            case 0:
                bodyMaterial = team0BodyMaterial;
                headBowRosesMaterial = team0HeadBowRosesMaterial;
                skirtMaterial = team0SkirtMaterial;
                break;
            case 1:
                bodyMaterial = team1BodyMaterial;
                headBowRosesMaterial = team1HeadBowRosesMaterial;
                skirtMaterial = team1SkirtMaterial;
                break;
        }

        Material[] tempMaterialArray = new Material[4];
        tempMaterialArray[0] = headBowRosesMaterial;
        tempMaterialArray[1] = bodyMaterial;
        tempMaterialArray[2] = invisibleArms;
        tempMaterialArray[3] = skirtMaterial;

        bodyRenderer.materials = tempMaterialArray;
    }


    public override void ControlGained()
    {



        //Camera Instantiate
        Instantiate(Resources.Load("CamPivot"));

        //Game UI Instantiate
        GameObject _canvas = Instantiate(Resources.Load("Canvas")) as GameObject;
        _canvas.GetComponent<GameUI>().SetGameValues(entity);


        UpdateGameState updateGameStateEvent = UpdateGameState.Create();
        updateGameStateEvent.NewGameState = (int)GameManager.GameState.Lobby;
        updateGameStateEvent.Send();

        _playerCamera = FindObjectOfType<PlayerCamera>();
        _playerCamera.SetTarget(cameraFollowJoint);

        _cam = _playerCamera.FindCam();
        _cam = _playerCamera.cam;

        _motor._cam = _playerCamera;

        
        if (entity.isOwner)
        {
            state.HasUI = true;
        }


    }

    void PollKeys(bool mouse)
    {
        if (!paused && controllerIndex != -1)
        {
            GamePadState currentState = GamePad.GetState((PlayerIndex)controllerIndex);

            _rawMovement = new Vector3(currentState.ThumbSticks.Left.X + Input.GetAxisRaw("Horizontal"), 0, currentState.ThumbSticks.Left.Y + Input.GetAxisRaw("Vertical"));
     //       _rawMovement = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            
            _rawMovement = Vector3.ClampMagnitude(_rawMovement, 1);
            _smoothedMovement = Vector3.MoveTowards(_smoothedMovement, _rawMovement, movementBezierSpeed);
            _movement = Vector3.MoveTowards(_movement, _smoothedMovement, movementBezierSpeed);




            _leftMouseDown = currentState.Triggers.Left >= 0.5f || Input.GetButton("Rope");
            //_leftMouseDown = Input.GetAxisRaw("Rope") > 0.5f || Input.GetButton("Rope");
            _rightMouseDown = currentState.Buttons.RightShoulder == ButtonState.Pressed || Input.GetButton("Hook");



            _jump = currentState.Buttons.A == ButtonState.Pressed ||  Input.GetButton("Jump");

            _reel = currentState.Triggers.Right >= 0.5f || Input.GetButton("Reel");

            if (_cam)
            {
                _camRotation = _cam.transform.rotation;
                _camPosition = _cam.transform.position;
            }
            
        }
        else
        {
            _movement = Vector3.zero;
            _leftMouseDown = false;
            _rightMouseDown = false;

            _jump = false;
            _reel = false;
       //     _startAnalytics = false;
       //     _loadAnalytics = false;
       //     _findDataSet = false;
        }
    }

    private void Update()
    {
        PollKeys(true);
        timeSinceLastResetState += Time.deltaTime;


    }

    /// <summary>
    /// Called by bolt if this node is the CONTROLLER for this entity. 
    /// Build a Command that will then be queued for ExecuteCommand()
    /// </summary>
    public override void SimulateController()
    {
        PollKeys(false);

        IPlayerCommandsInput input = PlayerCommands.Create();




        input.Movement = _movement;
        input.Jump = _jump;
        input.LeftMouseDown = _leftMouseDown;
        input.RightMouseDown = _rightMouseDown;
        input.Reel = _reel;
        input.CamRotation = _camRotation;
        input.CamPosition = _camPosition;
        input.AttackRayDirection = attackRayDirection;

        _cordController.LocalHookControl(_rightMouseDown,attackRayDirection);
        entity.QueueInput(input);


    }

    /// <summary>
    /// Called by Bolt if this not is the OWNER or CONTROLLER for this entity. 
    /// Proxies are never called in here. 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="resetState"></param>
    /// 

    public override void ExecuteCommand(Command command, bool resetState)
    {
        PlayerCommands cmd = (PlayerCommands)command;

        if (resetState)
        {
            //if we got a correction from the server, reset ( this only runs on the client)
            //Tie the command Result to Motor 
            _motor.SetState(cmd.Result.Rotation, cmd.Result.Velocity);

        }
        else
        {
            // apply the movement we set up ( this runs on both server and client)

            _motor.UpdateInputs(cmd.Input.Movement, cmd.Input.Jump, cmd.Input.Reel, cmd.Input.CamRotation);
            PlayerMotor.State motorState = _motor.GetState();

            cmd.Result.Rotation = motorState.rotation;
            cmd.Result.Velocity = motorState.velocity;

            if (cmd.IsFirstExecution)
            {
                _cordController.UpdateCords(cmd.Input.LeftMouseDown, cmd.Input.CamPosition, cmd.Input.CamRotation);

            }

            //ANALYTICS RECORD and LOAD
          //  if (_startAnalytics == true || _loadAnalytics == true || _findDataSet == true)
          //  {
          //      if (_analyticsCoroutine == null && _startAnalytics == true)
          //      {
          //          _analyticsCoroutine = StartCoroutine(StartAnaytics());
          //      }
          //      else if (_analyticsCoroutine == null && _loadAnalytics == true)
          //      {
          //          _analyticsCoroutine = StartCoroutine(LoadAnaytics());
          //      }
          //      else if (_analyticsCoroutine == null && _findDataSet == true)
          //      {
          //          _analyticsCoroutine = StartCoroutine(FindDataSession());
          //      }
          //  }
          //  else
          //  {
          //      if (_analyticsCoroutine != null)
          //      {
          //          StopCoroutine(_analyticsCoroutine);
          //          _analyticsCoroutine = null;
          //
          //      }
          //  }
        }
    }



  // IEnumerator StartAnaytics()
  // {
  //     while (_startAnalytics == true)
  //     {
  //         if (entity.isOwner)
  //         {
  //             Analytics analyticsEvent = Analytics.Create();
  //             analyticsEvent.NetworkId = entity.networkId.ToString();
  //             analyticsEvent.PlayerPosition = _playerPosition;
  //             analyticsEvent.isGrounded = _grounded;
  //             analyticsEvent.Send();
  //             yield return new WaitForSecondsRealtime(0.5f);
  //         }
  //     }
  // }
  //
  // IEnumerator LoadAnaytics()
  // {
  //     while (_loadAnalytics == true)
  //     {
  //         if (entity.isOwner)
  //         {
  //             Analytics analyticsEvent = Analytics.Create();
  //             analyticsEvent.LoadAnalytics = true;
  //             analyticsEvent.Send();
  //             yield return new WaitForSecondsRealtime(0.2f);
  //         }
  //     }
  // }
  //
  // IEnumerator FindDataSession()
  // {
  //     while (_findDataSet == true)
  //     {
  //         if (entity.isOwner)
  //         {
  //             Analytics analyticsEvent = Analytics.Create();
  //             analyticsEvent.FindSession = true;
  //             analyticsEvent.Send();
  //             yield return new WaitForSecondsRealtime(0.2f);
  //         }
  //     }
  // }



    public override void OnEvent(PlaySound evnt)
    {
        BoltConsole.Write("Played sound from proxy");

        AudioManager.Instance.PlayClipFromProxy(evnt.eventName, gameObject);
    }

    public override void OnEvent(Die evnt)
    {
        base.OnEvent(evnt);
        _motor.Respawn(evnt.LookDirection, evnt.Position);
    }


    void AnimatePlayer(PlayerCommands cmd, bool isGrounded)
    {
        //   _anim.SetFloat("VerticalInput", cmd.Input.Movement.z);
        //   _anim.SetFloat("HorizontalInput", cmd.Input.Movement.x);
        //   _anim.SetBool("Landed", isGrounded);
        //
        //   if (cmd.Input.Jump && isGrounded)
        //   {
        //       _anim.SetTrigger("Jump");
        //   }
    }

}
