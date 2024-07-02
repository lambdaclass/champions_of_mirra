using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerNameHandler : MonoBehaviour
{
    private const string PLAYER_NAME_KEY = "playerName";
    private const string PLAYER_NAME_PLACEHOLDER = "Player Name";

    [SerializeField]
    GameObject playerNameHandler;

    [SerializeField]
    Text playerNameInput;

    [SerializeField]
    Sprite selectedButtonSprite;

    [SerializeField]
    GameObject errorMessage;

    [SerializeField]
    InputField placeholder;

    [SerializeField]
    TextMeshProUGUI topLeftPlayerName;

    [SerializeField]
    TextMeshProUGUI currentPlayerName;

    [SerializeField]
    TextMeshProUGUI profilePlayerName;

    private string playerName;

    const string NAME_LENGHT_MESSAGE = "Player Name should contain more than 3 characters.";

    const string EMPTY_NAME_MESSAGE = "Player name cannot be empty.";

    const string CLOSE_WITHOUT_NAME_MESSAGE = "You need to set a player name to play the game.";

    public void SetPlayerName()
    {
        this.playerName = playerNameInput.text.Trim();
        if (this.playerName == "" || this.playerName.Length <= 3)
        {
            HandleErrorMessage();
            this.errorMessage.SetActive(true);
            return;
        }
        this.errorMessage.SetActive(false);
        GetComponent<Image>().sprite = selectedButtonSprite;
        PlayerPrefs.SetString(PLAYER_NAME_KEY, this.playerName);
        this.Hide();
    }

    private void HandleErrorMessage(){
        TextMeshProUGUI textMesh = this.errorMessage.GetComponent<TextMeshProUGUI>();
        if(this.playerName.Length <= 3){
            textMesh.text = NAME_LENGHT_MESSAGE;
        } 

        if(this.playerName == ""){
            textMesh.text = EMPTY_NAME_MESSAGE;
        }

    }

    public void ChangePlayersName()
    {
        SetPlayerName();
        if (!this.errorMessage.activeSelf) 
        {
            topLeftPlayerName.text = PlayerPrefs.GetString("playerName");  
            currentPlayerName.text = "Current name: " + PlayerPrefs.GetString("playerName"); 
            Hide();
        }
    }

    public void ChangePlayersNameProfile() 
    {
        SetPlayerName();
        if (!this.errorMessage.activeSelf) 
        {
            profilePlayerName.text = PlayerPrefs.GetString("playerName");  
            Hide();
        }
    }

    public string GetPlayerName()
    {
        return this.playerName;
    }

    public void Show()
    {
        if (PlayerPrefs.HasKey(PLAYER_NAME_KEY))
        {
            this.playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY);
            this.placeholder.text = this.playerName.ToUpper();
        }
        this.playerNameHandler.SetActive(true);
        this.playerNameHandler.GetComponent<CanvasGroup>().alpha = 1;
    }

    public void ClearPlaceholder()
    {
        if (this.placeholder.text == PLAYER_NAME_PLACEHOLDER)
        {
            this.placeholder.text = "";
        }
    }

    private void Hide()
    {
        this.playerNameHandler.SetActive(false);
    }

    public void ClosePopup()
    {
        TextMeshProUGUI textMesh = this.errorMessage.GetComponent<TextMeshProUGUI>();
        if (!PlayerPrefs.HasKey(PLAYER_NAME_KEY))
        {
            textMesh.text = CLOSE_WITHOUT_NAME_MESSAGE;
            this.errorMessage.SetActive(true);
            return;
        }
        this.errorMessage.SetActive(false);
        this.Hide();
    }
}
