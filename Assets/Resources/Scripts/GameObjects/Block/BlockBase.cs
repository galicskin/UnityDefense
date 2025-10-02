using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockBase : MonoBehaviour, ISelectable
{
    public static MapBlockData blockData;

    private float _hardness;
    private float _density;
    private float _durability;
    private float _mineDropInterval;
    private const float _maxDurability = 100.0f;
    private Dictionary<DropRule,int> _dropRatesMinDropCountDict = new();
    private ObjectPoolSystem _objectPoolSystem = null;

    Transform ISelectable.Transform
    {
        get { return this.transform; }
    }

    // SelectionType 구현
    SelectionType ISelectable.SelectionType
    {
        get { return SelectionType.Special; }
    }
    Renderer cachedRenderer = null; 
    Bounds ISelectable.SelectionBounds
    {
        get
        {
            if(!cachedRenderer)
                cachedRenderer = GetComponentInChildren<Renderer>();
            return cachedRenderer != null ? cachedRenderer.bounds : new Bounds(transform.position, Vector3.one);
        }
    }
    Renderer ISelectable.SelectionRenderer
    {
        get
        {
            if (!cachedRenderer)
                cachedRenderer = GetComponentInChildren<Renderer>();
            return cachedRenderer;
        }
    }

    void ISelectable.OnSelected()
    {
        Debug.Log($"{name} 선택됨");
        // TODO: 아웃라인 표시 or UI 갱신
    }

    void ISelectable.OnDeselected()
    {
        Debug.Log($"{name} 선택 해제");
    }

    public virtual void CreatedBlock(BlockProp blockProp)
    {
        _hardness = (float)blockProp.hardness;
        _density = (float)blockProp.density;
        _durability = 100.0f;
        _mineDropInterval = 25.0f;

        foreach(var dropRule in blockProp.drops)
        {
            _dropRatesMinDropCountDict[dropRule] = dropRule.bonusDropMinCount;
        }
    }

    public virtual void Broken(Worker worker,float deltaTime)
    {
        if (worker == null) return;
        const float EPS = 1e-4f;

        float prevDurability = _durability;

        // 1) 효율 계산(0, NaN, 무한대 방지)
        float denom = Mathf.Max(_hardness * _density, 1e-4f);
        float efficiency = Mathf.Max(0f, worker.workEfficiency / denom);

        // 2) 내구도 감소 (프레임 독립)
        _durability -= efficiency * deltaTime;
        _durability = Mathf.Clamp(_durability, 0f, _maxDurability);

        // 3) 드랍 개수 계산(임계선 몇 개 넘었는가)
        float interval = Mathf.Max(_mineDropInterval, 1e-4f);

        // 진행도: max에서 얼마나 깎였는가
        float prevProgress = _maxDurability - prevDurability;
        float currProgress = _maxDurability - _durability;


        // 경계값에서의 부동소수점 오차를 줄이기 위해 EPS 더해줌
        int prevStep = Mathf.FloorToInt((prevProgress + EPS) / interval);
        int currStep = Mathf.FloorToInt((currProgress + EPS) / interval);

        int dropCount = Mathf.Max(0, currStep - prevStep);
        if (dropCount > 0)
        {
            DropMines(GetWorkerPlane(worker), dropCount);
        }

        // 4) 파괴 처리
        if (_durability <= 0f)
            Collapse();
    }

    protected virtual void Collapse()
    {
        
        Vector3 dropPosition = this.gameObject.transform.position;
        dropPosition.y = 0f;
        foreach (var _dropRule in _dropRatesMinDropCountDict.Keys)
        {
            SpawnDrop(dropPosition,_dropRule.oreType, _dropRule.CollapseDropCount);
        }
        gameObject.SetActive(false);
    }

    protected virtual void DropMines(Vector3 position, int count)
    {
        if (count <= 0 || _dropRatesMinDropCountDict.Count == 0)
            return;

        // 키 스냅샷(열거 중 구조 변경 방지용)
        var keys = _dropRatesMinDropCountDict.Keys.ToList();

        foreach (var rule in keys)
        {
            float p = Mathf.Clamp01(rule.bonusDropRate);
            int remain = _dropRatesMinDropCountDict[rule];
            if (remain <= 0) continue;

            // count번 시행 중 성공 횟수(= 이번에 줄일 양)
            int hits = 0;
            for (int i = 0; i < count; i++)
            {
                if (Random.value < p)
                    hits++;
            }

            if (hits > 0)
            {
                int dec = Mathf.Min(remain, hits); // 0 미만 방지
                _dropRatesMinDropCountDict[rule] = remain - dec;

                // 실제 드랍 스폰(룰에 따라 dec개)
                SpawnDrop(position, rule.oreType,dec );
            }
        }
    }

    protected virtual Vector3 GetWorkerPlane(Worker worker)
    {
        // 1) XZ 평면으로 평탄화
        Vector3 dropPosition = worker.transform.position;
        dropPosition.y = 0f;
        
        return dropPosition;
    }

    protected virtual void SpawnDrop(Vector3 pos, OreType oreType, int count)
    {
        if(_objectPoolSystem == null)
            _objectPoolSystem = GameSystemManager.Instance.GetSystem<ObjectPoolSystem>();
        GameObject prefab = MineData.Instance.GetPrefabOrNull(oreType);
        if (prefab == null)
        { 
            Debug.Log($"{oreType}에 해당하는 prefab없음");
            return;
        }
        _objectPoolSystem.SpawnLazy(oreType.ToString(), prefab, pos, Quaternion.identity);
    }

    public void OnSelected()
    {
        throw new System.NotImplementedException();
    }

    public void OnDeselected()
    {
        throw new System.NotImplementedException();
    }
}
