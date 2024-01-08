using System.Collections.Generic;
using UnityEngine;


public class GlobalUserData : MonoBehaviour
{
    [SerializeField]
    List<Character> characters;

    // Singleton instance
    private static GlobalUserData instance;

    // User
    private User user;

    // Public property to access the user
    public User User
    {
        get { return user; }
        set { user = value; }
    }

    // Public property to access the user's units
    public List<Unit> Units
    {
        get { return user.units; }
    }

    // Method to get the singleton instance
    public static GlobalUserData Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("GlobalUserData").AddComponent<GlobalUserData>();
            }
            return instance;
        }
    }

    // Initialize the user's units here
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // For testing purposes, initialize with sample data
            user = new User
            {
                id = "1",
                username = "SampleUser",
                units = new List<Unit>
                {
                    new Unit { id = "1", level = 5, character = characters.Find(character => "muflus" == character.name.ToLower()), slot = 0, selected = true },
                    new Unit { id = "2", level = 5, character = characters.Find(character => "uma" == character.name.ToLower()), slot = 1, selected = true },
                    new Unit { id = "3", level = 5, character = characters.Find(character => "uma" == character.name.ToLower()), slot = 2, selected = true },
                    new Unit { id = "4", level = 5, character = characters.Find(character => "valtimer" == character.name.ToLower()), slot = 3, selected = true },
                    new Unit { id = "5", level = 5, character = characters.Find(character => "otix" == character.name.ToLower()), slot = 4, selected = true }
                },
                next_unit_id = 6
            };
        } else {
            // Destroy this instance if another one already exists
            Destroy(gameObject);
        }
    }
}
