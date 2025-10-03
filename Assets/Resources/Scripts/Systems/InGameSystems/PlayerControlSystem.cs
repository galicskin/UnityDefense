using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlayerContext
{
    public List<BlockBase> TargetBlocks;
    public List<TowerBase> Towers;
    public List<Worker> Workers;
    public byte[,] AStarMapData;
    public MapData mapData;

    
}

//여기서 UI랑 연결하든 일단 플레이어가 무언가를 할때 이 system을 꼭 지나야함
public class PlayerControlSystem : SystemBase
{
    SelectionSystem _selectionSystem;
    SelectionSystem SelectionSystem
    {
        get 
        {
            if (_selectionSystem == null)
                _selectionSystem = GameSystemManager.Instance.GetSystem<SelectionSystem>();
            return _selectionSystem;
        }
    }
    PlayerContext _playerContext;
    public PlayerContext GetContext()
    {
        return _playerContext;
    }
    public void SetContext(PlayerContext playerContext)
    {
        _playerContext = playerContext;
    }
    
    public void Awake()
    {
        _playerContext.TargetBlocks = new();
        _playerContext.Towers = new();
        _playerContext.Workers = new();
        _playerContext.AStarMapData = null;
    }

    public void CreatedMap(byte[,] AStarMapData,MapData mapData)
    {
        _playerContext.AStarMapData = AStarMapData;
        _playerContext.mapData = mapData;
    }

    //현재 Mines를 선택할지, 아님 Worker, buidling같은 명령을 내릴 오브젝트를 선택할지 결정
    public void OnChangeSelectMode(bool isOn)
    {
        if (isOn)
        {
            SelectionSystem.SetMultipleMode();
            SelectionSystem.SetSelectableTypes(SelectionType.Special);
        }
        else
        {
            SelectionSystem.SetSingleMode();
            SelectionSystem.SetSelectableTypes(SelectionType.Worker | SelectionType.Building);
        }
    }

    public void OnDetermineSelectBlocks()
    {
        var list = SelectionSystem.GetSelectedList();

        List<Worker> workers = new();
        List<TowerBase> towerBases = new();
        List<BlockBase> blockBases = new();
        foreach (var e in list)
        {
            if (e is Worker)
            {
                workers.Add(e as Worker);
            }
            else if (e is TowerBase)
            {
                towerBases.Add(e as TowerBase);
            }
            else if (e is BlockBase)
            {
                blockBases.Add(e as BlockBase);
            }
        }

        _playerContext.Workers = workers;
        _playerContext.Towers = towerBases;
        _playerContext.TargetBlocks = blockBases;
  
    }
}
