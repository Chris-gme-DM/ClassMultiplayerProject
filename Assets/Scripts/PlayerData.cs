using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Component.Spawning;
using FishNet.Transporting;
using TMPro;

/// <summary>
/// This script is to hold Player information and data.
/// </summary>
public class PlayerData : NetworkBehaviour
{
    private readonly SyncVar<int> _playerID = new(0);
    public int PlayerID => _playerID.Value;

    private readonly SyncVar<string> _playerName = new("Player");
    public string PlayerName => _playerName.Value;

    private readonly SyncVar<int> _playerScore = new(0);
    public int PlayerScore => _playerScore.Value;

    private readonly SyncVar<int> _HP = new(100);
    public int HP => _HP.Value;

    private readonly SyncVar<Color> _playerColor = new(Color.white);
    public Color PlayerColor => _playerColor.Value;

    public enum PlayerColorToAssign
    {
        White,
        Red,
        Blue,
        Green,
        Yellow
    }

    private GameObject _playerHealthBar;
    private TMP_Text _playerNameText;
    private void Awake()
    {
        _playerName.OnChange += OnNameChanged;
        _HP.OnChange += OnHealthChanged;
        //    _playerScore.OnChange += OnScoreChanged;
        _playerColor.OnChange += OnColorChanged;
        // Find and assign health bar and name text objects
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
    }
    // Assign Id and name and color on client start
    public override void OnStartClient()
    {
        base.OnStartClient();
        AssignID();
        SetName("Player " + PlayerID);
        AssignColor();
        _playerHealthBar = transform.Find("PlayerCanvas/PlayerHealthBar").gameObject;
        _playerNameText = transform.Find("PlayerCanvas/PlayerNameText").GetComponent<TMP_Text>();
    }
    /// <summary>
    /// Server only methods
    /// </summary>
    [ServerRpc]
    public void AssignID()
    {
        _playerID.Value = Owner.ClientId;
    }
    [ServerRpc]
    public void ChangeHealth(int amount)
    {
        if(!IsServerStarted) return;
        _HP.Value += amount;
        Debug.Log($"Player {_playerName.Value} health changed by {amount} to {_HP.Value}");
    }
    /// <summary>
    /// Requested by client, assigned by server On Start
    /// </summary>
    [ServerRpc]
    public void SetName(string newName)
    {
        _playerName.Value = newName;
        Debug.Log($"Player name set to: {newName}");
    }
    [ServerRpc]
    public void AssignColor()
    {
        PlayerColorToAssign assignedColor = (PlayerColorToAssign)Random.Range(0, System.Enum.GetValues(typeof(PlayerColorToAssign)).Length);
        Color color = assignedColor switch
        {
            PlayerColorToAssign.White => Color.white,
            PlayerColorToAssign.Red => Color.red,
            PlayerColorToAssign.Blue => Color.blue,
            PlayerColorToAssign.Green => Color.green,
            PlayerColorToAssign.Yellow => Color.yellow,
            _ => Color.white,
        };
        _playerColor.Value = color;
    }

    /// <summary>
    /// Callbacks for SyncVar changes
    /// </summary>
    /// <param name="prev"></param>
    /// <param name="next"></param>
    /// <param name="asServer"></param>
    public void OnHealthChanged(int prev, int next,  bool asServer)
    {
        Debug.Log($"{PlayerName} health changed to {_HP.Value}");
        // Update health bar visually
        _playerHealthBar.transform.localScale = new Vector3(next / 100f, 1, 1);

        if (next <= 0 && prev > 0)
        {
            // Player has died
            OnDeath();
        }
    }
    public void OnNameChanged(string prev, string next, bool asServer)
    {
        Debug.Log($"Player name changed to {next}");
        // Update player name visually
        _playerNameText.text = next;
    }
    public void OnColorChanged(Color prev, Color next, bool asServer)
    {
        // Update player color visually
        GetComponent<Renderer>().material.color = next;
    }
    public void OnDeath()
    {
        // Handle player death (e.g., respawn, notify others)
        Debug.Log($"{PlayerName} has died.");
        // Disable player until Respawn
        GetComponent<PlayerMovementWithHooks>().enabled = false;
        // Reset health
        _HP.Value = 100;
        // Respawn player at randomw spawn point
        PlayerSpawner spawner = NetworkManager.GetComponent<PlayerSpawner>();
        Transform spawnPoint = spawner.Spawns[Random.Range(0, spawner.Spawns.Length)];
        transform.position = spawnPoint.position;
        // Reenable player
        GetComponent<PlayerMovementWithHooks>().enabled = true;
    }
    public override void OnStopServer()
    {
        // Cleanup if necessary
        base.OnStopServer();
        _HP.OnChange -= OnHealthChanged;
        _playerColor.OnChange -= OnColorChanged;
    }
}
