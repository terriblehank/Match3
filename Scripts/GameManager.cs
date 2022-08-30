using System.Collections;
using System.Collections.Generic;
using HS.Tools;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class GameManager : SingletonMono<GameManager>
{
    bool eventsFlag;
    public bool EventsFlag { get { return eventsFlag && movingGems.Count <= 0; } }

    public const int row = 7;
    public const int line = 7;

    public int verticalOffset = -2;

    public GameObject gemPrefab;
    public Transform root;

    public Dictionary<Vector2Int, Gem> gems = new Dictionary<Vector2Int, Gem>();

    List<Gem> wait2Pop = new List<Gem>();
    public int Wait2PopCount { get { return wait2Pop.Count; } }

    List<Gem> movingGems = new List<Gem>();

    // Start is called before the first frame update
    void Start()
    {
        SpawnGemsWhenGamstart();
        StartCoroutine(StartCheck());
    }

    // Update is called once per frame
    void Update()
    {

    }

    Gem SpawnGem(Vector2Int pos)
    {
        GameObject gemObject = Instantiate(gemPrefab, root);

        gemObject.transform.position = new Vector3(pos.x, pos.y, 0);

        Gem gem = gemObject.GetComponent<Gem>();
        gem.type = Random.Range(1, 4);
        gem.x = pos.x;
        gem.y = pos.y;

        return gem;
    }

    void SpawnGemsWhenGamstart()
    {
        Vector2Int startPos = new Vector2Int(-(row / 2), -(line / 2) + verticalOffset);

        for (int i = 0; i < row; i++)
        {
            for (int j = 0; j < line; j++)
            {
                Vector2Int pos = new Vector2Int(startPos.x + j, startPos.y + i);
                Gem gem = SpawnGem(pos);
                gems.Add(pos, gem);
            }
        }
    }

    /// <summary>
    /// 开始游戏时，消除3连的Gem
    /// </summary>
    /// <returns></returns>
    IEnumerator StartCheck()
    {
        while (true)
        {
            yield return null;
            for (int i = 0; i < root.childCount; i++)
            {
                root.GetChild(i).GetComponent<Gem>().Check();
            }
            if (wait2Pop.Count <= 0)
            {
                EnableEvents();
                yield break;
            }
            RemoveAndRespawn();
        }
    }

    void RemoveAndRespawn()
    {
        foreach (var gem in wait2Pop)
        {
            Vector2Int pos = new Vector2Int(gem.x, gem.y);
            Destroy(gem.gameObject);
            Gem newGem = SpawnGem(pos);
            gems[pos] = newGem;
        }
        wait2Pop.Clear();
    }

    public void EnableEvents()
    {
        eventsFlag = true;
    }

    public void DisableEvents()
    {
        eventsFlag = false;
    }

    public void RegisterMovingGem(Gem gem)
    {
        if (movingGems.Contains(gem)) return;
        movingGems.Add(gem);
    }

    public void UninstallMovingGem(Gem gem)
    {
        if (!movingGems.Contains(gem)) return;
        movingGems.Remove(gem);
    }

    public void AddPoppingGems(List<Gem> gems)
    {
        foreach (var gem in gems)
        {
            if (wait2Pop.Contains(gem)) continue;
            wait2Pop.Add(gem);
        }
    }

    public void Pop()
    {
        DisableEvents();
        StartCoroutine(DoPop());
    }

    IEnumerator DoPop()
    {
        foreach (var gem in wait2Pop)
        {
            Vector2Int pos = new Vector2Int(gem.x, gem.y);
            Destroy(gem.gameObject);
            gems[pos] = null;
            if (movingGems.Contains(gem)) movingGems.Remove(gem);
        }
        wait2Pop.Clear();

        while (false)
        {
            yield return null;
        }

        StartCoroutine(FillNullAreas());
    }

    IEnumerator FillNullAreas()
    {
        List<Gem> checkList = new List<Gem>();
        yield return new WaitForSeconds(0.2f);

        Vector2Int origin = new Vector2Int(-(row / 2), -(line / 2) + verticalOffset);

        for (int i = 0; i < line; i++)
        {
            int spawnCount = 1;
            for (int j = 0; j < row; j++)
            {
                Vector2Int key = new Vector2Int(origin.x + i, origin.y + j);

                if (gems[key] == null)
                {
                    bool hasFiller = false;

                    if (j < row - 1) //最顶层不需要寻找已有的Gem去填充，直接进入生成新Gem的逻辑
                    {
                        for (int k = 1; k < row - j; k++)   //进行row - j - 1次循环，即查找从j开始向上至顶部的位置是否存在可用的Gem
                        {
                            Vector2Int temp = key;
                            temp.y += k; //计算目标位置
                            if (gems[temp] != null) //目标位置存在Gem
                            {
                                Gem filler = gems[temp];
                                filler.MoveTo(key);
                                checkList.Add(filler);

                                gems[temp] = null;

                                hasFiller = true;
                                break;
                            }
                        }
                    }

                    if (!hasFiller) //上方没有可以用来填补空缺的Gem时，生成新的Gem在正确的位置上
                    {
                        Gem newGem = SpawnGem(key);
                        newGem.transform.position = new Vector3(key.x, line / 2 + verticalOffset + spawnCount, 0);  //位置计算
                        newGem.MoveTo(key);
                        checkList.Add(newGem);
                        spawnCount++;
                    }
                }
            }
        }

        while (movingGems.Count > 0)    //等待所有的Gem都移动完毕
        {
            yield return null;
        }

        foreach (var gem in checkList)  //检查所有需要被Check的Gem
        {
            gem.Check();
        }

        if (Wait2PopCount <= 0) //如果未发生Pop恢复事件控制，否则Pop
        {
            EnableEvents();
        }
        else
        {
            yield return new WaitForSeconds(0.3f);

            Pop();
        }
    }
}
