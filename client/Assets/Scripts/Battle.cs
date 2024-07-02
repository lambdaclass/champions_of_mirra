using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.TopDownEngine;
using UnityEngine;

public class Battle : MonoBehaviour
{
    public HashSet<SkillInfo> skillInfoSet;

    [SerializeField]
    LeftMMTouchJoystick joystickL;

    [SerializeField]
    CustomInputManager InputManager;

    [SerializeField]
    CustomGUIManager CustomGUIManager;
    public bool showClientPredictionGhost;
    public bool zoneActive;
    public bool botsActive;
    public bool showInterpolationGhosts;
    public bool animationsEnabled;
    public List<GameObject> InterpolationGhosts = new List<GameObject>();
    public GameObject clientPredictionGhost;
    public bool useClientPrediction;
    public bool useInterpolation;
    public CharacterStates.MovementStates[] BlockingMovementStates;
    public CharacterStates.CharacterConditions[] BlockingConditionStates;
    public long accumulatedTime;
    public long firstTimestamp;
    public GameObject mapGrid;

    private Loot loot;
    private CratesManager cratesManager;
    private bool playerMaterialColorChanged;
    private bool sendMovementStarted = false;
    private long lastMovementUpdate;
    private long lastForcedMovementUpdate;
    private bool playersSetupCompleted = false;

    [SerializeField]
    private CustomLevelManager levelManager;
    private PlayerControls playerControls;
    private PowerUpsManager powerUpsManager;
    private CustomCharacter myClientCharacter = null;

    [SerializeField]
    private PoolsHandler poolsHandler;

    public Dictionary<ulong, PlayerReferences> playersReferences =
        new Dictionary<ulong, PlayerReferences>();

    [SerializeField]
    MeshFilter mesh;

    public struct PlayerReferences
    {
        public GameObject player;
        public CustomCharacter character;

        public CharacterFeedbacks characterFeedbacks;
        public CharacterFeedbackManager feedbackManager;
        public Animator modelAnimator;
    }

    void Start()
    {
        InitBlockingStates();
        SetupInitialState();
        StartCoroutine(InitializeProjectiles());
        StartCoroutine(poolsHandler.SetUpPoolsSkills());
        StartCoroutine(SetupPlayersReferences());
        loot = GetComponent<Loot>();
        cratesManager = GetComponent<CratesManager>();
        playerMaterialColorChanged = false;
        playerControls = GetComponent<PlayerControls>();
        powerUpsManager = GetComponent<PowerUpsManager>();
        lastMovementUpdate = 0;
        lastForcedMovementUpdate = 0;
    }

    private void InitBlockingStates()
    {
        BlockingMovementStates = new CharacterStates.MovementStates[2];
        BlockingMovementStates[0] = CharacterStates.MovementStates.Attacking;
        BlockingMovementStates[1] = CharacterStates.MovementStates.Pushing;
    }

    private void SetupInitialState()
    {
        useClientPrediction = true;
        useInterpolation = true;
        accumulatedTime = 0;
        showClientPredictionGhost = false;
        showInterpolationGhosts = false;
        zoneActive = true;
        botsActive = true;
        animationsEnabled = true;
    }

    private IEnumerator SetupPlayersReferences()
    {
        yield return new WaitUntil(() => GameServerConnectionManager.Instance.players.Count > 0);
        foreach (Entity serverPlayer in GameServerConnectionManager.Instance.gamePlayers)
        {
            GameObject player = Utils.GetPlayer(serverPlayer.Id);
            PlayerReferences playerReference = new PlayerReferences();
            playerReference.player = player;
            playerReference.character = player.GetComponent<CustomCharacter>();
            playerReference.characterFeedbacks = player.GetComponent<CharacterFeedbacks>();
            playerReference.feedbackManager = playerReference
                .character
                .characterBase
                .GetComponent<CharacterFeedbackManager>();
            playerReference.modelAnimator = playerReference
                .character
                .CharacterModel
                .GetComponent<Animator>();
            playersReferences.Add(serverPlayer.Id, playerReference);
        }
        playersSetupCompleted = true;
    }

    private IEnumerator InitializeProjectiles()
    {
        yield return new WaitUntil(() => GameServerConnectionManager.Instance.players.Count > 0);
        CreateProjectilesPoolers();
    }

    void CreateProjectilesPoolers()
    {
        skillInfoSet = GameServerConnectionManager.Instance.players
            .SelectMany(player => player.GetComponents<Skill>())
            .Select(skill => skill.GetSkillInfo())
            .Where(skill => skill.hasProjectile)
            .ToHashSet();

        GetComponent<ProjectileHandler>().CreateProjectilesPoolers(skillInfoSet);
    }

    void Update()
    {
        // MoveEntities();
        if (
            playersSetupCompleted
            && GameServerConnectionManager.Instance.gamePlayers != null
            && GameServerConnectionManager.Instance.players.Count > 0
            && GameServerConnectionManager.Instance.gamePlayers.Count > 0
        )
        {
            SetAccumulatedTime();
            UpdateBattleState();
        }

        if (
            GameServerConnectionManager.Instance.eventsBuffer.Count() > 1
            && !sendMovementStarted
            && GameServerConnectionManager.Instance.gameStatus == GameStatus.Running
        )
        {
            long nowMiliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            float clientActionRate = GameServerConnectionManager.Instance.serverTickRate_ms;

            if ((nowMiliseconds - lastMovementUpdate) >= clientActionRate)
            {
                SendPlayerMovement();
                lastMovementUpdate = nowMiliseconds;
            }
        }
    }

    // private void MoveEntities()
    // {
    //     // TODO: For now we hardcode to only move entity 1 which is the single spawned player entity
    //     var entity = levelManager.PlayerPrefabs[0];
    //     var playerOnePosition = GameServerConnectionManager.Instance.playersIdPosition[1];
    //     entity.transform.position = new Vector3(playerOnePosition.X, 0f, playerOnePosition.Y);
    // }

    void UpdateBattleState()
    {
        UpdatePlayerActions();
        UpdateProjectileActions();
        poolsHandler.UpdatePoolsActions();
        loot.UpdateLoots();
        cratesManager.UpdateCrates();
        powerUpsManager.UpdatePowerUps();
    }

    private void SetAccumulatedTime()
    {
        if (firstTimestamp == 0)
        {
            firstTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        accumulatedTime = (currentTimestamp - firstTimestamp);
    }

    public bool PlayerMovementAuthorized(CustomCharacter character)
    {
        if ((BlockingMovementStates != null) && (BlockingMovementStates.Length > 0))
        {
            for (int i = 0; i < BlockingMovementStates.Length; i++)
            {
                if (BlockingMovementStates[i] == (character.MovementState.CurrentState))
                {
                    return false;
                }
            }
        }

        if ((BlockingConditionStates != null) && (BlockingConditionStates.Length > 0))
        {
            for (int i = 0; i < BlockingConditionStates.Length; i++)
            {
                if (BlockingConditionStates[i] == (character.ConditionState.CurrentState))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void SendPlayerMovement()
    {
        Entity entity = Utils.GetGamePlayer(GameServerConnectionManager.Instance.playerId);

        if (myClientCharacter == null)
        {
            myClientCharacter = Utils.GetCharacter(GameServerConnectionManager.Instance.playerId);
        }

        bool isPlayerAlive = entity.Player.Health > 0;
        if (myClientCharacter && isPlayerAlive)
        {
            if (PlayerMovementAuthorized(myClientCharacter))
            {
                var inputFromVirtualJoystick = joystickL is not null;
                if (
                    inputFromVirtualJoystick
                    && (joystickL.RawValue.x != 0 || joystickL.RawValue.y != 0)
                )
                {
                    // Using joysticks
                    playerControls.SendJoystickValues(joystickL.RawValue.x, joystickL.RawValue.y);
                    GameServerConnectionManager.Instance.clientPrediction.didFirstMovement = true;
                }
                else if (playerControls.KeysPressed())
                {
                    // Using keyboard
                    playerControls.SendAction();
                    GameServerConnectionManager.Instance.clientPrediction.didFirstMovement = true;
                }
                else
                {
                    // Not pressing anything
                    playerControls.SendJoystickValues(0, 0);
                }
            }
        }
    }

    void UpdatePlayerActions()
    {
        long currentTime;
        long pastTime;
        GameObject interpolationGhost = null;
        EventsBuffer buffer = GameServerConnectionManager.Instance.eventsBuffer;
        GameState gameEvent;

        currentTime = buffer.firstTimestamp + accumulatedTime;
        pastTime = currentTime - buffer.deltaInterpolationTime;

        if (buffer.firstTimestamp == 0)
        {
            buffer.firstTimestamp = buffer.lastEvent().ServerTimestamp;
        }

        foreach (Entity player in GameServerConnectionManager.Instance.gamePlayers)
        {
            if (showInterpolationGhosts)
            {
                interpolationGhost = FindGhostPlayer(player.Id.ToString());
            }

            if (
                useInterpolation
                && (
                    GameServerConnectionManager.Instance.playerId != player.Id
                    || !useClientPrediction
                )
            )
            {
                gameEvent = buffer.getNextEventToRender(pastTime).Item1;
            }
            else
            {
                gameEvent = buffer.lastEvent();
            }

            // There are a few frames during which this is outdated and produces an error
            if (GameServerConnectionManager.Instance.gamePlayers.Count == gameEvent.Players.Count)
            {
                // This call to `new` here is extremely important for client prediction. If we don't make a copy,
                // prediction will modify the player in place, which is not what we want.
                Entity serverPlayerUpdate = new Entity(gameEvent.Players[player.Id]);
                if (
                    serverPlayerUpdate.Id == (ulong)GameServerConnectionManager.Instance.playerId
                    && useClientPrediction
                    && serverPlayerUpdate.Player.Health > 0
                )
                {
                    // Move the ghost BEFORE client prediction kicks in, so it only moves up until
                    // the last server update.
                    if (clientPredictionGhost != null)
                    {
                        UpdatePlayer(clientPredictionGhost, serverPlayerUpdate, pastTime);
                    }

                    GameServerConnectionManager
                        .Instance
                        .clientPrediction
                        .SimulatePlayerState(
                            serverPlayerUpdate,
                            gameEvent.PlayerTimestamps[player.Id],
                            gameEvent.ServerTimestamp
                        );
                }

                if (interpolationGhost != null)
                {
                    UpdatePlayer(
                        interpolationGhost,
                        buffer.lastEvent().Players[player.Id],
                        pastTime
                    );
                }

                GameObject currentPlayer = playersReferences[serverPlayerUpdate.Id].player;
                // TODO: try to optimize GetComponent calls
                CustomCharacter playerCharacter = playersReferences[
                    serverPlayerUpdate.Id
                ].character;

                if (currentPlayer.activeSelf)
                {
                    UpdatePlayer(currentPlayer, serverPlayerUpdate, pastTime);

                    if (!buffer.timestampAlreadySeen(player.Id, gameEvent.ServerTimestamp))
                    {
                        foreach (
                            PlayerAction playerAction in serverPlayerUpdate.Player.CurrentActions
                        )
                        {
                            if (
                                (
                                    playerCharacter.MovementState.CurrentState
                                        == CharacterStates.MovementStates.Pushing
                                    || PlayerMovementAuthorized(playerCharacter)
                                ) && !playerCharacter.currentActions.Contains(playerAction)
                            )
                            {
                                playerCharacter.currentActions.Add(playerAction);
                                ExecuteSkillFeedback(
                                    currentPlayer,
                                    playerAction.Action,
                                    serverPlayerUpdate,
                                    playerAction.Duration,
                                    playerAction.Destination
                                );
                            }

                            if (playerAction.Destination != null) // Maybe add playerAction key to differentiate ?
                            {
                                playerCharacter.IsTeleporting = true;
                                playerCharacter.TeleportingDestination = playerAction.Destination;
                            }
                        }

                        List<PlayerAction> actionsToDelete = playerCharacter
                            .currentActions
                            .Except(serverPlayerUpdate.Player.CurrentActions)
                            .ToList();

                        foreach (PlayerAction playerAction in actionsToDelete)
                        {
                            playerCharacter.currentActions.Remove(playerAction);
                        }

                        buffer.setLastTimestampSeen(player.Id, gameEvent.ServerTimestamp);
                    }

                    playerCharacter.HandleTeleport(serverPlayerUpdate.Position);
                }

                playerCharacter.UpdatePowerUpsCount(serverPlayerUpdate.Player.PowerUps);

                if (serverPlayerUpdate.Player.Health <= 0)
                {
                    playerCharacter.SetPlayerDead();
                }

                Transform hitbox = playerCharacter.characterBase.Hitbox.transform;
                playerCharacter.GetComponent<CharacterController>().radius =
                    serverPlayerUpdate.Radius / 100;
                float hitboxSize =
                    Utils.TransformBackenUnitToClientUnit(serverPlayerUpdate.Radius) * 2;
                hitbox.localScale = new Vector3(hitboxSize, hitbox.localScale.y, hitboxSize);
            }
        }
    }

    private void ExecuteSkillFeedback(
        GameObject currentPlayer,
        PlayerActionType playerAction,
        Entity entity,
        ulong skillDuration,
        Position destination
    )
    {
        CustomCharacter character = currentPlayer.GetComponent<CustomCharacter>();
        Direction direction = entity.Direction;
        // TODO: Refactor
        switch (playerAction)
        {
            case PlayerActionType.ExecutingSkill1:
                currentPlayer
                    .GetComponent<Skill1>()
                    .ExecuteFeedbacks(skillDuration, true, destination);
                character.RotatePlayer(direction);
                break;
            case PlayerActionType.ExecutingSkill2:
                currentPlayer
                    .GetComponent<Skill2>()
                    .ExecuteFeedbacks(skillDuration, true, destination);
                character.RotatePlayer(direction);
                break;
            case PlayerActionType.ExecutingSkill3:
                currentPlayer
                    .GetComponent<Skill3>()
                    .ExecuteFeedbacks(skillDuration, true, destination);
                character.RotatePlayer(direction);
                break;
        }
    }

    void UpdateProjectileActions()
    {
        Dictionary<int, GameObject> projectiles = GameServerConnectionManager.Instance.projectiles;
        List<Entity> gameProjectiles = GameServerConnectionManager.Instance.gameProjectiles;
        ClearProjectiles(projectiles, gameProjectiles);
        ProcessProjectilesCollision(projectiles, gameProjectiles);
        UpdateProjectiles(projectiles, gameProjectiles);
    }

    void UpdateProjectiles(Dictionary<int, GameObject> projectiles, List<Entity> gameProjectiles)
    {
        float tickRate = 1000f / GameServerConnectionManager.Instance.serverTickRate_ms;
        GameObject projectile;
        for (int i = 0; i < gameProjectiles.Count; i++)
        {
            Vector3 backToFrontPosition = Utils.transformBackendOldPositionToFrontendPosition(
                gameProjectiles[i].Position
            );
            if (projectiles.TryGetValue((int)gameProjectiles[i].Id, out projectile))
            {
                float velocity = tickRate * gameProjectiles[i].Speed / 100f;
                Vector3 movementDirection = new Vector3(
                    gameProjectiles[i].Direction.X,
                    0f,
                    gameProjectiles[i].Direction.Y
                );
                movementDirection.Normalize();
                Vector3 newProjectilePosition =
                    projectile.transform.position + movementDirection * velocity * Time.deltaTime;
                projectile
                    .GetComponent<SkillProjectile>()
                    .UpdatePosition(
                        new Vector3(newProjectilePosition[0], 3f, newProjectilePosition[2])
                    );
            }
            else if (gameProjectiles[i].Projectile.Status == ProjectileStatus.Active)
            {
                float angle = Vector3.SignedAngle(
                    new Vector3(1f, 0, 0),
                    new Vector3(
                        (long)(gameProjectiles[i].Direction.Y * 100),
                        0f,
                        -(long)(gameProjectiles[i].Direction.X * 100)
                    ),
                    Vector3.up
                );

                string projectileKey = gameProjectiles[i].Projectile.SkillKey;
                ulong skillOwner = gameProjectiles[i].Projectile.OwnerId;

                SkillInfo info = skillInfoSet
                    .Where(el => el.projectileSkillKey == projectileKey && el.ownerId == skillOwner)
                    .FirstOrDefault();

                if (info != null)
                {
                    GameObject projectileFromSkill = info?.projectilePrefab;

                    GameObject skillProjectile = GetComponent<ProjectileHandler>()
                        .InstanceProjectile(
                            projectileFromSkill,
                            angle,
                            new Vector3(backToFrontPosition[0], 3f, backToFrontPosition[2])
                        );
                    projectiles.Add((int)gameProjectiles[i].Id, skillProjectile);
                }
                else
                {
                    print(
                        "The projectile name does not match the projectile prefab assign to the scriptable object of the skill"
                    );
                }
            }
        }
    }

    void ClearProjectiles(Dictionary<int, GameObject> projectiles, List<Entity> gameProjectiles)
    {
        foreach (int projectileId in projectiles.Keys.ToList())
        {
            if (!gameProjectiles.Exists(x => (int)x.Id == projectileId))
            {
                projectiles[projectileId].GetComponent<SkillProjectile>().Remove();
                projectiles.Remove(projectileId);
            }
        }
    }

    void ProcessProjectilesCollision(
        Dictionary<int, GameObject> projectiles,
        List<Entity> gameProjectiles
    )
    {
        foreach (var pr in projectiles.ToList())
        {
            Entity gameProjectile = gameProjectiles.Find(x => (int)x.Id == pr.Key);
            if (gameProjectile.Projectile.Status == ProjectileStatus.Exploded)
            {
                pr.Value.GetComponent<SkillProjectile>().ProcessCollision();
                projectiles.Remove(pr.Key);
            }
        }
    }

    private void UpdatePlayer(GameObject player, Entity playerUpdate, long pastTime)
    {
        /*
        Player has a speed of 3 tiles per tick. A tile in unity is 0.3f a distance of 0.3f.
        There are 50 ticks per second. A player's velocity is 50 * 0.3f

        In general, if a player's velocity is n tiles per tick, their unity velocity
        is 50 * (n / 10f)

        The above is the player's velocity's magnitude. Their velocity's direction
        is the direction of deltaX, which we can calculate (assumming we haven't lost socket
        frames, but that's fine).
        */

        CharacterFeedbacks characterFeedbacks = playersReferences[
            playerUpdate.Id
        ].characterFeedbacks;
        CustomCharacter character = playersReferences[playerUpdate.Id].character;
        CharacterFeedbackManager feedbackManager = playersReferences[
            playerUpdate.Id
        ].feedbackManager;
        Animator modelAnimator = playersReferences[playerUpdate.Id].modelAnimator;

        var characterSpeed = playerUpdate.Speed / 100f;

        feedbackManager.ManageStateFeedbacks(playerUpdate, character);
        feedbackManager.HandlePickUpItemFeedback(playerUpdate, characterFeedbacks);

        if (!GameServerConnectionManager.Instance.GameHasEnded() && playerUpdate.Player.Health > 0)
        {
            HandleMovement(player, playerUpdate, pastTime, characterSpeed);
        }
        else
        {
            modelAnimator.SetBool("Walking", false);
        }

        character.HandlePlayerHealth(playerUpdate);

        if (playerUpdate.Id == GameServerConnectionManager.Instance.playerId)
        {
            if (GameServerConnectionManager.Instance.damageDone.ContainsKey(playerUpdate.Id))
            {
                character.HandleHit(
                    GameServerConnectionManager.Instance.damageDone[playerUpdate.Id]
                );
            }
            /*
                - We divided the milliseconds time in two parts because
                - rustler can't handle u128, so instead of developing those functions
                - we decided to use 2 u64 fields to represent the time in milliseconds

                - If you need to use complete time in milliseconds, you should use both
                - If you need to use remaining time in milliseconds, you can use only low field
                - because high field will be 0
            */

            float skill2Cooldown =
                playerUpdate.Player.Cooldowns.FirstOrDefault(cooldown => cooldown.Key == "2").Value
                / 1000.0f;
            float skill3Cooldown =
                playerUpdate.Player.Cooldowns.FirstOrDefault(cooldown => cooldown.Key == "3").Value
                / 1000.0f;

            InputManager.CheckSkillCooldown(
                UIControls.Skill1,
                // (float)playerUpdate.BasicSkillCooldownLeft.Low / 1000f,
                0f,
                player.GetComponent<Skill1>().GetSkillInfo().useCooldown
            );
            InputManager.CheckSkillCooldown(
                UIControls.Skill2,
                // (float)playerUpdate.Skill1CooldownLeft.Low / 1000f,
                skill2Cooldown,
                player.GetComponent<Skill2>().GetSkillInfo().useCooldown
            );
            InputManager.CheckSkillCooldown(
                UIControls.Skill3,
                // (float)playerUpdate.Skill1CooldownLeft.Low / 1000f,
                skill3Cooldown,
                player.GetComponent<Skill3>().GetSkillInfo().useCooldown
            );
        }
    }

    private void HandleMovement(
        GameObject player,
        Entity playerUpdate,
        long pastTime,
        float characterSpeed
    )
    {
        // This is tickRate * characterSpeed. Once we decouple tickRate from speed on the backend
        // it'll be changed.
        float tickRate = 1000f / GameServerConnectionManager.Instance.serverTickRate_ms;
        float velocity = tickRate * characterSpeed;

        var frontendPosition = Utils.transformBackendOldPositionToFrontendPosition(
            playerUpdate.Position
        );

        float xChange = frontendPosition.x - player.transform.position.x;
        float yChange = frontendPosition.z - player.transform.position.z;

        CustomCharacter character = playersReferences[playerUpdate.Id].character;

        Animator modelAnimator = playersReferences[playerUpdate.Id].modelAnimator;

        bool walking = false;

        if (useClientPrediction)
        {
            walking =
                (playerUpdate.Id == GameServerConnectionManager.Instance.playerId)
                    ? (InputsAreBeingUsed())
                    : GameServerConnectionManager
                        .Instance
                        .eventsBuffer
                        .playerIsMoving(playerUpdate.Id, (long)pastTime);
        }
        else
        {
            if (playerUpdate.Id == GameServerConnectionManager.Instance.playerId)
            {
                walking = GameServerConnectionManager
                    .Instance
                    .eventsBuffer
                    .playerIsMoving(playerUpdate.Id, (long)pastTime);
            }
        }

        Vector2 movementChange = new Vector2(xChange, yChange);

        // This magnitude allow us to not reconciliate the player's position if the change is too small
        if (movementChange.magnitude > 0.2f)
        {
            Vector3 movementDirection = new Vector3(xChange, 0f, yChange);
            movementDirection.Normalize();

            // The idea here is, when moving, we never want to go past the position the backend is telling us we are in.
            // Let's say the movementChange vector is (1, 0), i.e., we are moving horizontally to the right.
            // Let's also say frontendPosition is (2, y, 1)
            // If newPosition is (2.1, y, 1), we want it to just be (2, y, 1).
            // In this case, all we are doing is saying that the `x` coordinate should be min(2, newPosition.x)
            // If the movement were left, we would take max(2, newPosition.x)
            // Let's now say that the movement is in the (1, 1) normalized direction, so diagonally up and right.
            // If frontendPosition is (2, y, 1), I can't go past it in the (1, 1) direction. What we need to do here is
            // simply take the `x` coordinate to be min(2, newPosition.x) and the `z` coordinate to be min(1, newPosition.z)

            // In general, if the movementDirection vector is (x, y, z) normalized, then if its `x` coordinate is positive, we should
            // take newPosition.x = min(frontendPosition.x, newPosition.x)
            // If, on the other hand, its `x` coordinate is negative, we take newPosition.x = max(frontendPosition.x, newPosition.x)
            // The exact same thing applies to `z`
            Vector3 newPosition =
                player.transform.position + movementDirection * velocity * Time.deltaTime;

            if (movementDirection.x > 0)
            {
                newPosition.x = Math.Min(frontendPosition.x, newPosition.x);
            }
            else
            {
                newPosition.x = Math.Max(frontendPosition.x, newPosition.x);
            }

            if (movementDirection.z > 0)
            {
                newPosition.z = Math.Min(frontendPosition.z, newPosition.z);
            }
            else
            {
                newPosition.z = Math.Max(frontendPosition.z, newPosition.z);
            }

            player.transform.position = new Vector3(newPosition.x, 0, newPosition.z);

            // FIXME: This is a temporary solution to solve unwanted player rotation until we handle movement blocking on backend
            // if the player is in attacking state, movement rotation from movement should be ignored
            Direction direction = GetPlayerDirection(playerUpdate);

            if (PlayerMovementAuthorized(character))
            {
                character.RotatePlayer(direction);
            }
        }

        character.RotateCharacterOrientation();

        modelAnimator.SetBool("Walking", walking);
    }

    // CLIENT PREDICTION UTILITY FUNCTIONS , WE USE THEM IN THE MMTOUCHBUTTONS OF THE PAUSE SPLASH
    public void ToggleClientPrediction()
    {
        useClientPrediction = !useClientPrediction;
        if (!useClientPrediction)
        {
            TurnOffClientPredictionGhost();
        }
    }

    public void ToggleClientPredictionGhost()
    {
        showClientPredictionGhost = !showClientPredictionGhost;
        if (showClientPredictionGhost && clientPredictionGhost == null)
        {
            SpawnClientPredictionGhost();
        }
        else
        {
            TurnOffClientPredictionGhost();
        }
    }

    public void ToggleZone()
    {
        zoneActive = !zoneActive;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        GameServerConnectionManager.Instance.SendToggleZone(timestamp);
    }

    public void ToggleBots()
    {
        botsActive = !botsActive;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        GameServerConnectionManager.Instance.SendToggleBots(timestamp);
    }

    public void ToggleGrid()
    {
        mapGrid.SetActive(!mapGrid.activeSelf);
    }

    public void ToggleAnimations()
    {
        animationsEnabled = !animationsEnabled;
        foreach (var playerReference in playersReferences)
        {
            playerReference.Value.modelAnimator.enabled = animationsEnabled;
        }
    }

    private void SpawnClientPredictionGhost()
    {
        GameObject player = playersReferences[GameServerConnectionManager.Instance.playerId].player;
        clientPredictionGhost = Instantiate(player, player.transform.position, Quaternion.identity);
        clientPredictionGhost.GetComponent<CustomCharacter>().PlayerID = GameServerConnectionManager
            .Instance
            .playerId
            .ToString();
        clientPredictionGhost.GetComponent<CustomCharacter>().name =
            $"Client Prediction Ghost {GameServerConnectionManager.Instance.playerId}";
        showClientPredictionGhost = true;
    }

    private void TurnOffClientPredictionGhost()
    {
        if (!showClientPredictionGhost && clientPredictionGhost != null)
        {
            clientPredictionGhost
                .GetComponent<CustomCharacter>()
                .GetComponent<Health>()
                .SetHealth(0);
            clientPredictionGhost.SetActive(false);
            Destroy(clientPredictionGhost);
            clientPredictionGhost = null;
        }
    }

    // ENTITY INTERPOLATION UTILITY FUNCTIONS, WE USE THEM IN THE MMTOUCHBUTTONS OF THE PAUSE SPLASH
    public void ToggleInterpolationGhosts()
    {
        showInterpolationGhosts = !showInterpolationGhosts;
        if (showInterpolationGhosts)
        {
            SpawnInterpolationGhosts();
        }
        else
        {
            TurnOffInterpolationGhosts();
        }
    }

    private void SpawnInterpolationGhosts()
    {
        for (int i = 0; i < GameServerConnectionManager.Instance.gamePlayers.Count; i++)
        {
            GameObject player = Utils.GetPlayer(
                GameServerConnectionManager.Instance.gamePlayers[i].Id
            );
            GameObject interpolationGhost;
            interpolationGhost = Instantiate(
                player,
                player.transform.position,
                Quaternion.identity
            );
            interpolationGhost.GetComponent<CustomCharacter>().PlayerID =
                GameServerConnectionManager.Instance.gamePlayers[i].Id.ToString();
            interpolationGhost.GetComponent<CustomCharacter>().name =
                $"Interpolation Ghost #{GameServerConnectionManager.Instance.gamePlayers[i].Id}";

            InterpolationGhosts.Add(interpolationGhost);
        }
    }

    private void TurnOffInterpolationGhosts()
    {
        foreach (GameObject interpolationGhost in InterpolationGhosts)
        {
            interpolationGhost.GetComponent<CustomCharacter>().GetComponent<Health>().SetHealth(0);
            interpolationGhost.SetActive(false);
            Destroy(interpolationGhost);
        }
        InterpolationGhosts = new List<GameObject>();
    }

    public bool InputsAreBeingUsed()
    {
        var inputFromVirtualJoystick = joystickL is not null;

        return (
                inputFromVirtualJoystick && (joystickL.RawValue.x != 0 || joystickL.RawValue.y != 0)
            )
            || (
                (Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
                || (Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W))
                || (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
                || (Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A))
            );
    }

    public Direction GetPlayerDirection(Entity playerUpdate)
    {
        return playerUpdate.Direction;
    }

    private GameObject FindGhostPlayer(string playerId)
    {
        return InterpolationGhosts.Find(
            g => g.GetComponent<CustomCharacter>().PlayerID == playerId
        );
    }

    public GameObject GetMapGrid()
    {
        return mapGrid;
    }
}
