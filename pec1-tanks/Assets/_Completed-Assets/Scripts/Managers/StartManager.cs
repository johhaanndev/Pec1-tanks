using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartManager : MonoBehaviour
{
    private string numPrefsName = "num_players";
    private int num_players;

    private void Awake()
    {
        LoadData();
        //Debug.Log("Load data: " + num_players + " players");
    }

    public void TwoPlayers()
    {
        num_players = 2;
        SceneManager.LoadScene("_Complete-Game");
    }

    public void ThreePlayers()
    {
        num_players = 3;
        SceneManager.LoadScene("_Complete-Game");
    }

    public void FourPlayers()
    {
        num_players = 4;
        SceneManager.LoadScene("_Complete-Game");
    }

    private void SaveData()
    {
        PlayerPrefs.SetInt(numPrefsName, num_players);
    }

    private void LoadData()
    {
        num_players = PlayerPrefs.GetInt(numPrefsName, 2);
    }

    public int GetStartPlayers()
    {
        return num_players;
    }

    private void OnDestroy()
    {
        SaveData();
    }
}
