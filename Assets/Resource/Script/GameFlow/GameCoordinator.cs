using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCoordinator
{
    public SceneLoader SceneLoader { get; private set; }
    public GameStateMachine StateMachine { get; private set; }

    public void Initialize()
    {
        SceneLoader = new SceneLoader();
        StateMachine = new GameStateMachine();
    }
    
}
