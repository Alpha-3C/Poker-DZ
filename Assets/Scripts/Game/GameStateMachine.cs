using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    None,
    Setup,  //游戏开始
    Dealing,    //发牌
    Bidding,    //叫分
    KittySelection,     //底牌
    Playing,    //出牌阶段
    TrickEnd,   //墩
    RoundEnd,   //轮
    GameEnd
}


public class GameStateMachine : MonoBehaviour
{
    public static GameStateMachine Instance;

    [Header("Current State")]
    public GameState gameState;


    public static event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(Instance);
        }

        gameState = GameState.Setup;
    }

    public void ChangeGameState(GameState newGameState)
    {
        GameState oldGameState = gameState;
        gameState = newGameState;

        Debug.Log($"Game state has changed: {oldGameState} -> {newGameState}");

        OnGameStateChanged?.Invoke(newGameState);
    }
}
