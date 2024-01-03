using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionPlayerItem : MonoBehaviour
{
    [SerializeField]
    public GameObject hostLabel;

    [SerializeField]
    public TextMeshProUGUI playerText;

    [SerializeField]
    public TextMeshProUGUI characterText;

    [SerializeField]
    public Image characterImage;

    [SerializeField]
    public Sprite playerBackground;

    [SerializeField]
    public Image background;
    public ulong id;
    public string playerName;
    public string characterName;

    public ulong GetId()
    {
        return id;
    }

    public void SetId(ulong id)
    {
        this.id = id;
    }

    public string GetName()
    {
        return name;
    }

    public void SetCharacterName(string name)
    {
        this.characterText.text = name;
        this.characterName = name;
    }

    public void SetPlayerName(string name)
    {
        this.playerName = name;
    }

    public void SetPlayerItemText()
    {
        this.playerText.text = playerName;
        if (id == 1)
        {
            this.hostLabel.SetActive(true);
        }
        if (ServerConnection.Instance.playerId == id)
        {
            this.background.color = new Color32(255, 255, 255, 255);
            this.background.sprite = playerBackground;
        }
    }

    public void SetSprite(Sprite characterImage)
    {
        this.characterImage.sprite = characterImage;
    }
}
