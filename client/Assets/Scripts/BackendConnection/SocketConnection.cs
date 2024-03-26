using System;
using System.Collections;
using NativeWebSocket;
using Google.Protobuf;
using Google.Protobuf.Collections;
using System.IO;
using UnityEngine;
using Protobuf;
using System.Collections.Generic;
using System.Threading.Tasks;
using Protobuf.Messages;
using System.Linq;

public class SocketConnection : MonoBehaviour {
    WebSocket ws;

    public static SocketConnection Instance;

    public bool connected = false;

    private WebSocketMessageEventHandler currentMessageHandler;

    async void Awake()
    {
        await Init();
    }

    public async Task Init()
    {
        if (Instance != null)
        {
            if (this.ws != null)
            {
                await this.ws.Close();
            }
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if(!connected)
        {
            connected = true;
            ConnectToSession();
        }
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (ws != null)
        {
            ws.DispatchMessageQueue();
        }
#endif
    }

    private async void OnApplicationQuit()
    {
        await this.ws.Close();
    }

    private void ConnectToSession()
    {
        string url = $"ws://localhost:4001/2";
        ws = new WebSocket(url);
        ws.OnMessage += OnWebSocketMessage;
        ws.OnClose += OnWebsocketClose;
        ws.OnOpen += () =>
        {
            GetUserAndContinue();
        };
        ws.OnError += (e) =>
        {
            Debug.LogError("Received error: " + e);
        };
        ws.Connect();
    }

    private void OnWebsocketClose(WebSocketCloseCode closeCode)
    {
        if (closeCode != WebSocketCloseCode.Normal)
        {
            // Errors.Instance.HandleNetworkError(connectionTitle, connectionDescription);
            Debug.LogError("Your connection to the server has been lost.");
        }
        else
        {
            Debug.Log("ServerConnection websocket closed normally.");
        }
    }

    private void SendWebSocketMessage<T>(IMessage<T> message)
        where T : IMessage<T>
    {
        using (var stream = new MemoryStream())
        {
            message.WriteTo(stream);
            var msg = stream.ToArray();

            if(ws != null) {
                ws.Send(msg);
            }
        }
    }
    
    private void OnWebSocketMessage(byte[] data)
    {
        try
        {
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            switch (webSocketResponse.ResponseTypeCase)
            {
                case WebSocketResponse.ResponseTypeOneofCase.Error:
                    Debug.Log(webSocketResponse.Error.Reason);
                    break;
                // Since the response of type Unit isn't used for anything there isn't a specific handler for it, it is caught here so it doesn't log any confusing messages
                case WebSocketResponse.ResponseTypeOneofCase.Unit:
                    break;
                default:
                    Debug.Log("Request case not handled");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("InvalidProtocolBufferException: " + e);
        }
    }

	private User CreateUserFromData(Protobuf.Messages.User user, List<Character> availableCharacters)
	{
		List<Unit> units = CreateUnitsFromData(user.Units, availableCharacters);
		List<Item> items = new List<Item>();

		foreach (var userItem in user.Items)
		{
			items.Add(CreateItemFromData(userItem));
		}

		Dictionary<Currency, int> currencies = new Dictionary<Currency, int>();

		foreach (var currency in user.Currencies)
		{
			if (Enum.TryParse<Currency>(currency.Currency.Name.Replace(" ", ""), out Currency currencyValue))
			{
				currencies.Add(currencyValue, (int)currency.Amount);
			}
			else
			{
				Debug.LogError($"Currency brought from the backend not found in client: {currency.Currency.Name}");
			}
		}

		return new User
		{
			id = user.Id,
			username = user.Username,
			units = units,
			items = items,
			currencies = currencies,
			level = (int)user.Level,
			experience = (int)user.Experience,
			superCampaignsProgress = user.SuperCampaignProgresses.Select(superCampaignProgress => (superCampaignProgress.SuperCampaignId, superCampaignProgress.LevelId)).ToList()
		};
	}
	
    private Item CreateItemFromData(Protobuf.Messages.Item itemData) {
        return new Item
        {
            id = itemData.Id,
            level = (int)itemData.Level,
            userId = itemData.UserId,
            unitId = itemData.UnitId,
            template = GlobalUserData.Instance.AvailableItemTemplates.Find(itemtemplate => itemtemplate.name.ToLower() == itemData.Template.Name.ToLower())
        };
    }

	private Unit CreateUnitFromData(Protobuf.Messages.Unit unitData, List<Character> availableCharacters)
	{
		if (!availableCharacters.Any(character => character.name.ToLower() == unitData.Character.Name.ToLower()))
		{
			Debug.Log($"Character not found in available characters: {unitData.Character.Name}");
			return null;
		}

		return new Unit
		{
			id = unitData.Id,
			// tier = (int)unitData.Tier,
			character = availableCharacters.Find(character => character.name.ToLower() == unitData.Character.Name.ToLower()),
			rank = (Rank)unitData.Rank,
			level = (int)unitData.Level,
			slot = (int?)unitData.Slot,
			selected = unitData.Selected
		};
	}

    private List<Unit> CreateUnitsFromData(IEnumerable<Protobuf.Messages.Unit> unitsData, List<Character> availableCharacters)
	{
		List<Unit> createdUnits = new List<Unit>();

		foreach (var unitData in unitsData)
		{
			Unit unit = CreateUnitFromData(unitData, availableCharacters);
	
			if(unit != null) {
				createdUnits.Add(unit);
			}
		}

		return createdUnits;
	}

	private List<Campaign> ParseCampaignsFromResponse(Protobuf.Messages.Campaigns campaignsData, List<Character> availableCharacters)
    {
		List<(string campaignId, string levelId)> userCampaignsProgress = GlobalUserData.Instance.User.superCampaignsProgress;
        List<Campaign> campaigns = new List<Campaign>();
		LevelProgressData.Status campaignStatus = LevelProgressData.Status.Completed;

		// Currently looping through all the campaigns like they all belong to the same super campaign
		foreach(Protobuf.Messages.Campaign campaignData in campaignsData.Campaigns_)
        {
			LevelProgressData.Status levelStatus = LevelProgressData.Status.Completed;
            List<LevelData> levels = new List<LevelData>();
			
			foreach(Protobuf.Messages.Level level in campaignData.Levels.OrderBy(level => level.LevelNumber))
            {
				List<Unit> levelUnits = CreateUnitsFromData(level.Units, availableCharacters);

				levelStatus = userCampaignsProgress.Any(cp => cp.campaignId == campaignData.Id && cp.levelId == level.Id) ? LevelProgressData.Status.Unlocked : levelStatus;

                levels.Add(new LevelData
                {
                    id = level.Id,
                    levelNumber = (int)level.LevelNumber,
                    campaignId = level.CampaignId,
                    units = levelUnits,
					rewards = level.CurrencyRewards.ToDictionary(currencyReward => Enum.Parse<Currency>(currencyReward.Currency.Name.Replace(" ", "")), currencyReward => (int)currencyReward.Amount),
                    status = levelStatus
                });

				if(levelStatus == LevelProgressData.Status.Unlocked) {
					levelStatus = LevelProgressData.Status.Locked;
				}
            }

			campaignStatus = userCampaignsProgress.Any(cp => cp.campaignId == campaignData.Id) ? LevelProgressData.Status.Unlocked : campaignStatus;

            campaigns.Add(new Campaign
            {
				campaignId = campaignData.Id,
                status = campaignStatus,
                levels = levels
            });

			if(campaignStatus == LevelProgressData.Status.Unlocked) {
				campaignStatus = LevelProgressData.Status.Locked;
			}
        }

        return campaigns;
    }

    // Better name for this method?
    // This should be refactored, assigning player prefs should not be handled here
    public void GetUserAndContinue()
    {
		string userId = PlayerPrefs.GetString("userId");
		if(String.IsNullOrEmpty(userId)) {
			Debug.Log("No user in player prefs, creating user with username \"testUser\"");
			CreateUser("testUser", (user) => {
				PlayerPrefs.SetString("userId", user.id);
				GlobalUserData.Instance.User = user;
				Debug.Log("User created correctly");
			});
		}
		else {
			Debug.Log($"Found userid: \"{userId}\" in playerprefs, getting the user");
			GetUser(userId, (user) => {
				PlayerPrefs.SetString("userId", user.id);
				GlobalUserData.Instance.User = user;
			});
		}
    }

    public void GetUser(string userId, Action<User> onGetUserDataReceived)
    {
		try{
			GetUser getUserRequest = new GetUser{
				UserId = userId
			};
			WebSocketRequest request = new WebSocketRequest{
				GetUser = getUserRequest
			};
			currentMessageHandler = (data) => AwaitGetUserResponse(data, onGetUserDataReceived);
			ws.OnMessage += currentMessageHandler;
			ws.OnMessage -= OnWebSocketMessage;
			SendWebSocketMessage(request);
		} catch(Exception ex) {
			Debug.LogError(ex.Message);
		}
    }

    public void GetUserByUsername(string username, Action<User> onGetUserDataReceived)
    {
        GetUserByUsername getUserByUsernameRequest = new GetUserByUsername{
            Username = username
        };
        WebSocketRequest request = new WebSocketRequest{
            GetUserByUsername = getUserByUsernameRequest
        };
        currentMessageHandler = (data) => AwaitGetUserResponse(data, onGetUserDataReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitGetUserResponse(byte[] data, Action<User> onGetUserDataReceived)
    {
        try
        {
			ws.OnMessage -= currentMessageHandler;
			ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.User)
			{
				User user = CreateUserFromData(webSocketResponse.User, GlobalUserData.Instance.AvailableCharacters);
				onGetUserDataReceived?.Invoke(user);
			}
			else if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error)
			{
                switch(webSocketResponse.Error.Reason) {
                    case "not_found":
                        Debug.Log("User not found, trying to create new user");
                        CreateUser("testUser",  (user) => {
                            PlayerPrefs.SetString("userId", user.id);
                            GlobalUserData.Instance.User = user;
                            Debug.Log("User created correctly");
                        });
                        break;
                    case "username_taken":
                        Debug.LogError("Tried to create user, username was taken");
                        break;
                    default:
                        Debug.LogError(webSocketResponse.Error.Reason);
                        break;
                }
            }
        } catch (Exception e) {
            Debug.LogError("InvalidProtocolBufferException: " + e);
        }
    }

	public void CreateUser(string username, Action<User> onGetUserDataReceived)
    {
        CreateUser createUserRequest = new CreateUser{
            Username = username
        };
        WebSocketRequest request = new WebSocketRequest{
            CreateUser = createUserRequest
        };
        currentMessageHandler = (data) => AwaitGetUserResponse(data, onGetUserDataReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    public void GetCampaigns(string userId, Action<List<Campaign>> onCampaignDataReceived)
    {
        GetCampaigns getCampaignsRequest = new GetCampaigns{
            UserId = userId
        };
        WebSocketRequest request = new WebSocketRequest{
            GetCampaigns = getCampaignsRequest
        };
        currentMessageHandler = (data) => AwaitGetCampaignsResponse(data, onCampaignDataReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitGetCampaignsResponse(byte[] data, Action<List<Campaign>> onCampaignDataReceived)
    {
        try
        {
			ws.OnMessage -= currentMessageHandler;
			ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Campaigns) {
                List<Character> availableCharacters = GlobalUserData.Instance.AvailableCharacters;
                List<Campaign> campaigns = ParseCampaignsFromResponse(webSocketResponse.Campaigns, availableCharacters);
                onCampaignDataReceived?.Invoke(campaigns);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void SelectUnit(string unitId, string userId, int slot)
    {
        SelectUnit selectUnitRequest = new SelectUnit {
            UserId = userId,
            UnitId = unitId,
            Slot = (uint)slot
        };
        WebSocketRequest request = new WebSocketRequest {
            SelectUnit = selectUnitRequest
        };
        SendWebSocketMessage(request);
    }

    public void UnselectUnit(string unitId, string userId)
    {
        UnselectUnit unselectUnitRequest = new UnselectUnit {
            UserId = userId,
            UnitId = unitId
        };
        WebSocketRequest request = new WebSocketRequest {
            UnselectUnit = unselectUnitRequest
        };
        SendWebSocketMessage(request);
    }

    public void Battle(string userId, string levelId, Action<bool> onBattleResultReceived) {
        FightLevel fightLevelRequest = new FightLevel {
            UserId = userId,
            LevelId = levelId
        };
        WebSocketRequest request = new WebSocketRequest {
            FightLevel = fightLevelRequest
        };
		currentMessageHandler = (data) => AwaitBattleResponse(data, onBattleResultReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitBattleResponse(byte[] data, Action<bool> onBattleResultReceived)
    {
        try
        {
			ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.BattleResult) {
                bool battleResult = webSocketResponse.BattleResult.Result == "win";
                onBattleResultReceived?.Invoke(battleResult);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void EquipItem(string userId, string itemId, string unitId, Action<Item> onItemDataReceived) {
        EquipItem equipItemRequest = new EquipItem {
            UserId = userId,
            ItemId = itemId,
            UnitId = unitId
        };
        WebSocketRequest request = new WebSocketRequest {
            EquipItem = equipItemRequest
        };
        currentMessageHandler = (data) => AwaitItemResponse(data, onItemDataReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    public void UnequipItem(string userId, string itemId, Action<Item> onItemDataReceived) {
        UnequipItem unequipItemRequest = new UnequipItem {
            UserId = userId,
            ItemId = itemId
        };
        WebSocketRequest request = new WebSocketRequest {
            UnequipItem = unequipItemRequest
        };
        currentMessageHandler = (data) => AwaitItemResponse(data, onItemDataReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    public void LevelUpItem(string userId, string itemId, Action<Item> onItemDataReceived, Action<string> onError) {
        LevelUpItem levelUpItemRequest = new LevelUpItem {
            UserId = userId,
            ItemId = itemId
        };
        WebSocketRequest request = new WebSocketRequest {
            LevelUpItem = levelUpItemRequest
        };
        currentMessageHandler = (data) => AwaitItemResponse(data, onItemDataReceived, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitItemResponse(byte[] data, Action<Item> onItemDataReceived, Action<string> onError = null)
    {
        try
        {
			ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Item) {
                Item item = CreateItemFromData(webSocketResponse.Item);
                onItemDataReceived?.Invoke(item);
            }
            else if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error) {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void LevelUpUnit(string userId, string unitId, Action<Protobuf.Messages.UnitAndCurrencies> onUnitAndCurrenciesDataReceived, Action<string> onError) {
        LevelUpUnit levelUpUnitRequest = new LevelUpUnit {
            UserId = userId,
            UnitId = unitId
        };
        WebSocketRequest request = new WebSocketRequest {
            LevelUpUnit = levelUpUnitRequest
        };
        currentMessageHandler = (data) => AwaitUnitAndCurrenciesReponse(data, onUnitAndCurrenciesDataReceived, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitUnitAndCurrenciesReponse(byte[] data, Action<Protobuf.Messages.UnitAndCurrencies> onUnitAndCurrenciesDataReceived, Action<string> onError = null)
    {
        try
        {
			ws.OnMessage -= currentMessageHandler;
			ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.UnitAndCurrencies) {
                onUnitAndCurrenciesDataReceived?.Invoke(webSocketResponse.UnitAndCurrencies);
            }
            else if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error) {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

	public void GetBoxes(Action<List<Box>> onBoxesDataReceived, Action<string> onError = null) {
		GetBoxes getBoxesRequest = new GetBoxes();
        WebSocketRequest request = new WebSocketRequest {
            GetBoxes = getBoxesRequest
        };
        currentMessageHandler = (data) => AwaitBoxesReponse(data, onBoxesDataReceived, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
	}

	private void AwaitBoxesReponse(byte[] data, Action<List<Box>> onBoxesDataReceived, Action<string> onError = null)
	{
		try
        {
			ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Boxes) {
				List<Box> boxes = ParseBoxesFromResponse(webSocketResponse.Boxes);
                onBoxesDataReceived?.Invoke(boxes);
            }
            else if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error) {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
	}

	private List<Box> ParseBoxesFromResponse(Boxes boxesMessage)
	{
		return boxesMessage.Boxes_.Select(box => 
		{
			return new Box {
				id = box.Id,
				name = box.Name,
				description = box.Description,
				factions = box.Factions.ToList(),
				rankWeights = box.RankWeights.ToDictionary(rankWeight => rankWeight.Rank, rankWeight => rankWeight.Weight),
				costs = box.Cost.ToDictionary(cost => Enum.Parse<Currency>(cost.Currency.Name.Replace(" ", "")), cost => cost.Amount)
			};
		}).ToList();
	}

	public void Summon(string userId, string boxId, Action<User, Unit> onSuccess, Action<string> onError = null)
	{
		Summon summonRequest = new Summon {
            UserId = userId,
            BoxId = boxId
        };
        WebSocketRequest request = new WebSocketRequest {
            Summon = summonRequest
        };
        currentMessageHandler = (data) => AwaitSummonResponse(data, onSuccess, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
	}

	private void AwaitSummonResponse(byte[] data, Action<User, Unit> onSuccess, Action<string> onError)
	{
		try
        {
			ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.UserAndUnit) {
				User user = CreateUserFromData(webSocketResponse.UserAndUnit.User, GlobalUserData.Instance.AvailableCharacters);
				Unit unit = CreateUnitFromData(webSocketResponse.UserAndUnit.Unit, GlobalUserData.Instance.AvailableCharacters);
                onSuccess?.Invoke(user, unit);
            }
            else if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error) {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
	}

	public void FuseUnits(string userId, string unitId, string[] consumedUnitsIds, Action<Unit> onSuccess, Action<string> onError = null)
	{
		FuseUnit fuseRequest = new FuseUnit {
            UserId = userId,
            UnitId = unitId,
        };
		// Why on earth repeated fields don't have a setter but you can still add values to them?
		foreach(string consumedUnitId in consumedUnitsIds) {
			fuseRequest.ConsumedUnitsIds.Add(consumedUnitId);
		}
        WebSocketRequest request = new WebSocketRequest {
            FuseUnit = fuseRequest
        };
        currentMessageHandler = (data) => AwaitFuseUnitsResponse(data, onSuccess, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
	}

	private void AwaitFuseUnitsResponse(byte[] data, Action<Unit> onSuccess, Action<string> onError)
	{
		try
        {
			ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Unit) {
				Unit unit = CreateUnitFromData(webSocketResponse.Unit, GlobalUserData.Instance.AvailableCharacters);
                onSuccess?.Invoke(unit);
            }
            else if(webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error) {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
	}
}
