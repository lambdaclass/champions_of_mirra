using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomLevelManager : LevelManager
{
    private const float DEATH_FEEDBACK_DURATION = 1.5f;
    bool paused = false;

    [SerializeField]
    GameObject deathSplash;
    private ulong totalPlayers = 1;
    private ulong playerId;

    // private GameObject prefab;
    public CustomInputManager inputManager;

    public Entity playerToFollow;

    [SerializeField]
    public GameObject UiControls;
    public CinemachineCameraController camera;

    private bool deathSplashIsShown = false;

    [SerializeField]
    GameObject colliderPrefab;
    EndGameManager endGameManager;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        StartCoroutine(InitializeLevel());
    }

    private IEnumerator InitializeLevel()
    {
        yield return new WaitUntil(checkPlayerHasJoined);
        this.totalPlayers = (ulong)GameServerConnectionManager.Instance.gamePlayers.Count();
        playerId = GameServerConnectionManager.Instance.playerId;
        GeneratePlayers();
        SetPlayersSkills(playerId);
        SetOrientationArrow(playerId);

        Entity gamePlayer = Utils.GetGamePlayer(playerId);
        GameObject player = Utils.GetPlayer(playerId);
        player.GetComponent<Health>().CurrentHealth = gamePlayer.Player.Health;

        Transform hitbox = player.GetComponent<CustomCharacter>().characterBase.Hitbox.transform;
        player.GetComponent<CharacterController>().radius = gamePlayer.Radius / 100;
        float hitboxSize = Utils.TransformBackenUnitToClientUnit(gamePlayer.Radius) * 2;
        hitbox.localScale = new Vector3(hitboxSize, hitbox.localScale.y, hitboxSize);

        endGameManager = deathSplash.GetComponentInChildren<EndGameManager>();
        endGameManager.SetDeathSplashCharacter();

        GameServerConnectionManager
            .Instance
            .obstacles
            .ForEach(el =>
            {
                GenerateColliders(el.Vertices.ToList(), el.Name);
            });
    }

    private void GenerateColliders(List<Position> vertices, string name)
    {
        GameObject collider = Instantiate(colliderPrefab);
        collider.name = name;
        collider.GetComponent<LineRenderer>().positionCount = vertices.Count;
        for (int i = 0; i < vertices.Count; i++)
        {
            var vertice = vertices[i];
            var position = new Vector3(vertice.X / 100, 0, vertice.Y / 100);
            collider.GetComponent<LineRenderer>().SetPosition(i, position);
        }
        ;
    }

    void Update()
    {
        Entity gamePlayer = Utils.GetGamePlayer(playerId);
        GameObject player = Utils.GetPlayer(playerId);
        if (GameHasEndedOrPlayerHasDied(gamePlayer) && !deathSplashIsShown)
        {
            StartCoroutine(ShowDeathSplash(player));
            deathSplashIsShown = true;
        }
        if (GameHasEnded())
        {
            // TODO: Redirect to EndGameScreen
            //SceneManager.LoadScene("EndGame");
            endGameManager.finalSplash.SetActive(true);
            endGameManager.modelManager.ShowEndGameCharacterAnimation();
            endGameManager.ShowWinner();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GUIManager.Instance.SetPauseScreen(paused == false ? true : false);
            paused = !paused;
        }

        if (gamePlayer != null && gamePlayer.Player.Health <= 0)
        {
            SetCameraToAlivePlayer();
        }
    }

    private void GeneratePlayers()
    {
        // prefab = prefab == null ? quickGamePrefab : prefab;
        foreach (Entity player in GameServerConnectionManager.Instance.gamePlayers)
        {
            GameObject prefab = CharactersManager
                .Instance
                .AvailableCharacters
                .Find(el => el.name.ToLower() == player.Player.CharacterName)
                .prefab;

            if (GameServerConnectionManager.Instance.playerId == player.Id)
            {
                // Player1 is the ID to match with the client InputManager
                prefab.GetComponent<CustomCharacter>().PlayerID = "Player1";
            }
            else
            {
                prefab.GetComponent<CustomCharacter>().PlayerID = "";
            }
            Vector3 backToFrontPosition = Utils.transformBackendOldPositionToFrontendPosition(
                player.Position
            );
            CustomCharacter newPlayer = Instantiate(
                prefab.GetComponent<CustomCharacter>(),
                new Vector3(backToFrontPosition.x, 1.0f, backToFrontPosition.z),
                Quaternion.identity
            );

            if (GameServerConnectionManager.Instance.playerId == player.Id)
            {
                Instantiate(
                    newPlayer.characterBase.StaminaCharges,
                    newPlayer.characterBase.CharacterCard.transform
                );
                GameServerConnectionManager.Instance.clientPrediction.startingPosition =
                    player.Position;
            }
            newPlayer.CharacterHealth.CurrentHealth = player.Player.Health;
            newPlayer.CharacterHealth.InitialHealth = player.Player.Health;
            newPlayer.CharacterHealth.MaximumHealth = player.Player.Health;
            newPlayer.name = "Player" + player.Id;
            newPlayer.PlayerID = player.Id.ToString();
            newPlayer.characterBase.PlayerName.GetComponent<TextMeshProUGUI>().text = player.Name;
            SetPlayerHealthBar(
                GameServerConnectionManager.Instance.playerId == player.Id,
                newPlayer
            );
            GameServerConnectionManager.Instance.players.Add(newPlayer.gameObject);
            this.Players.Add(newPlayer);

            newPlayer.RotatePlayer(player.Direction);
        }
        this.PlayerPrefabs = (this.Players).ToArray();
    }

    int RoundUpByTen(int i)
    {
        return (int)(Math.Ceiling(i / 10.0d) * 10);
    }

    private void SetOrientationArrow(ulong playerID)
    {
        foreach (CustomCharacter player in this.PlayerPrefabs)
        {
            player
                .GetComponentInChildren<CharacterBase>()
                .OrientationArrow
                .SetActive(UInt64.Parse(player.PlayerID) == playerID);
        }
    }

    private void SetCameraToPlayer(ulong playerID)
    {
        foreach (CustomCharacter player in this.PlayerPrefabs)
        {
            if (
                UInt64.Parse(player.PlayerID) == playerID
                && UInt64.Parse(this.camera.TargetCharacter.PlayerID) != playerID
            )
            {
                this.camera.SetTarget(player);
                this.camera.StartFollowing();
            }
        }
    }

    // private void SetSkillAngles(List<SkillInfo> skillsClone)
    // {
    //     // var skills = ServerConnection.Instance.engineServerSettings.Skills;

    //     // List<SkillConfigItem> jsonSkills = Utils.ToList(skills);

    //     // float basicSkillInfoAngle = jsonSkills.Exists(skill => skillsClone[0].Equals(skill))
    //     //     ? float.Parse(jsonSkills.Find(skill => skillsClone[0].Equals(skill)).Mecha.Angle)
    //     //     : 0;
    //     skillsClone[0].angle = 45; // basicSkillInfoAngle;
    //     skillsClone[0].skillConeAngle = 45; // basicSkillInfoAngle;

    //     // float skill1InfoAngle = jsonSkills.Exists(skill => skillsClone[1].Equals(skill))
    //     //     ? float.Parse(jsonSkills.Find(skill => skillsClone[1].Equals(skill)).Angle)
    //     //     : 0;
    //     skillsClone[1].angle = 45; // skill1InfoAngle;
    //     skillsClone[1].skillConeAngle = 45; // skill1InfoAngle;
    // }

    private List<SkillInfo> InitSkills(CoMCharacter characterInfo, string id)
    {
        ConfigCharacter configCharacter = GameServerConnectionManager
            .Instance
            .config
            .Characters
            .ToList()
            .Find(character => character.Name == characterInfo.name.ToLower());
        List<SkillInfo> skills = new List<SkillInfo>();
        List<ConfigSkill> configSkills = configCharacter.Skills.Values.ToList();
        for (int index = 0; index < characterInfo.skillsInfo.Count; index++)
        {
            SkillInfo skillClone = Instantiate(characterInfo.skillsInfo[index]);
            skillClone.InitWithBackend(configSkills[index], id);
            skills.Add(skillClone);
        }

        return skills;
    }

    public void DestroySkillsClone(CustomCharacter player)
    {
        player
            .GetComponentsInChildren<Skill>()
            .ToList()
            .ForEach(skillInfo => Destroy(skillInfo.GetSkillInfo()));
    }

    private void SetPlayersSkills(ulong clientPlayerId)
    {
        inputManager.Setup();

        List<Skill> skillList = new List<Skill>();
        foreach (CustomCharacter player in this.PlayerPrefabs)
        {
            Skill1 skill1 = player.gameObject.AddComponent<Skill1>();
            Skill2 skill2 = player.gameObject.AddComponent<Skill2>();
            Skill3 skill3 = player.gameObject.AddComponent<Skill3>();

            skillList.Add(skill1);
            skillList.Add(skill2);
            skillList.Add(skill3);

            CoMCharacter characterInfo = CharactersManager
                .Instance
                .AvailableCharacters
                .Find(el => el.name.ToLower() == player.CharacterModel.name.ToLower());

            List<SkillInfo> skillInfoClone = InitSkills(characterInfo, player.PlayerID);
            // SetSkillAngles(skillInfoClone);

            skill1.SetSkill("1", skillInfoClone[0]);
            skill2.SetSkill("2", skillInfoClone[1]);
            skill3.SetSkill("3", skillInfoClone[2]);

            // var skills = ServerConnection.Instance.engineServerSettings.Skills;

            if (UInt64.Parse(player.PlayerID) == clientPlayerId)
            {
                inputManager.InitializeInputSprite(characterInfo);
                inputManager.AssignSkillToInput(
                    UIControls.Skill1,
                    skillInfoClone[0].inputType,
                    skill1
                );
                inputManager.AssignSkillToInput(
                    UIControls.Skill2,
                    skillInfoClone[1].inputType,
                    skill2
                );
                inputManager.AssignSkillToInput(
                    UIControls.Skill3,
                    skillInfoClone[2].inputType,
                    skill3
                );
            }

            StartCoroutine(inputManager.ShowInputs());
        }
    }

    private void SetPlayerHealthBar(bool isClientId, Character character)
    {
        Image healthBarFront = character
            .GetComponent<MMHealthBar>()
            .TargetProgressBar
            .ForegroundBar
            .GetComponent<Image>();

        healthBarFront.color = isClientId ? Utils.healthBarGreen : Utils.healthBarRed;
    }

    private IEnumerator ShowDeathSplash(GameObject player)
    {
        MMFeedbacks deathFeedback = player
            .GetComponent<CustomCharacter>()
            .GetComponent<Health>()
            .DeathMMFeedbacks;
        yield return new WaitForSeconds(DEATH_FEEDBACK_DURATION);
        deathSplash.SetActive(true);
        UiControls.SetActive(false);
    }

    private void SetCameraToAlivePlayer()
    {
        ulong saveKillerId = KillFeedManager.instance.GetSaveKillerId();
        ulong currentTrackedPlayer = KillFeedManager.instance.GetCurrentTrackedPlayer();

        if (saveKillerId != 0)
        {
            playerToFollow = Utils.GetGamePlayer(saveKillerId);
            StartCoroutine(WaitToChangeCamera(playerToFollow));
        }
        else if (Utils.GetAlivePlayers().Count() > 0 && saveKillerId == currentTrackedPlayer)
        {
            playerToFollow = Utils.GetAlivePlayers().ElementAt(0);
            SetCameraToPlayer(playerToFollow.Id);
            KillFeedManager.instance.SetCurrentTrackedPlayer(playerToFollow.Id);
        }
    }

    private IEnumerator WaitToChangeCamera(Entity player)
    {
        yield return new WaitUntil(() => player != null);
        SetCameraToPlayer(player.Id);
        KillFeedManager.instance.SetSaveKillerId(0);
        KillFeedManager.instance.SetCurrentTrackedPlayer(player.Id);
    }

    private bool GameHasEndedOrPlayerHasDied(Entity gamePlayer)
    {
        return GameServerConnectionManager.Instance.GameHasEnded()
            || gamePlayer != null && (gamePlayer.Player.Health == 0);
    }

    private bool GameHasEnded()
    {
        return GameServerConnectionManager.Instance.GameHasEnded();
    }

    private bool checkPlayerHasJoined()
    {
        return GameServerConnectionManager.Instance.gamePlayers != null
            && GameServerConnectionManager
                .Instance
                .gamePlayers
                .Any((player) => player.Id == GameServerConnectionManager.Instance.playerId);
    }
}
