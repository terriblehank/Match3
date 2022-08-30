using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using DG.Tweening;
using HS.Tools;
using static UnityEngine.RuleTile.TilingRuleOutput;
using System;

[RequireComponent(typeof(Draggable2D))]
public class Gem : MonoBehaviour
{
    public int type;

    public float dragDistanceLimit = 0.2f;

    public float movementSpeed = 0.2f;

    public int x;

    public int y;

    public Vector3 CurPos { get { return new Vector3(x, y, 0); } }

    public Draggable2D drag;

    bool dragging = false;

    bool moving;


    // Start is called before the first frame update
    void Start()
    {
        GetComponent<SpriteRenderer>().color = TempColor(); //临时差异化显示

        //设置drag鼠标按下回调
        drag.SetMouseDownCallback(() =>
        {
            if (!GameManager.Instance.EventsFlag) //如果事件总开关关闭，通知drag停止拖拽
            {
                drag.StopDragging();
                return;
            }
            GameManager.Instance.RegisterMovingGem(this);   //注册至正在移动的Gem，停用其他的拖拽事件
            GetComponent<SpriteRenderer>().sortingOrder = 1;
            dragging = true;
        });

        //设置drag鼠标抬起回调
        drag.SetMouseUpCallback(() =>
        {
            if (dragging)   //如果鼠标松开时dragging仍为真，说明未发生交换逻辑，Gem返回至初始位置并且卸载注册
            {
                GetComponent<SpriteRenderer>().sortingOrder = 0;
                transform.DOMove(drag.originPos, movementSpeed).onComplete = () =>
                {
                    GameManager.Instance.UninstallMovingGem(this);
                };
                dragging = false;
            }
        });

    }

    // Update is called once per frame
    void Update()
    {
        if (dragging)   //拖拽时进入此逻辑
        {
            float distance = Vector3.Distance(CurPos, transform.position);
            if (distance >= dragDistanceLimit)  //当拖拽距离超过预定距离时，触发
            {
                drag.StopDragging();    //停止drag的拖拽
                Gem neighbor = TryGetNeighor(this, transform.position - CurPos);    //获取拖拽方向上的邻居Gem
                if (neighbor != null)   //成功获取
                {
                    ExChangePosData(neighbor);  //交换位置数据
                    neighbor.transform.DOMove(neighbor.drag.originPos, movementSpeed);  //移动邻居至新位置
                    transform.DOMove(drag.originPos, movementSpeed).onComplete = () =>  //移动自身至新位置，并设置完成回调
                    {
                        //对自身和邻居都调用check，检查3连情况
                        Check();
                        neighbor.Check();
                        if (GameManager.Instance.Wait2PopCount <= 0)    //没有三连
                        {
                            //交换回原来的位置
                            ExChangePosData(neighbor);
                            neighbor.transform.DOMove(neighbor.drag.originPos, movementSpeed);
                            transform.DOMove(drag.originPos, movementSpeed).onComplete = () =>
                            {
                                GameManager.Instance.UninstallMovingGem(this);
                                GetComponent<SpriteRenderer>().sortingOrder = 0;
                            };
                        }
                        else //存在三连
                        {
                            //取消注册并爆破
                            GameManager.Instance.UninstallMovingGem(this);
                            GetComponent<SpriteRenderer>().sortingOrder = 0;
                            GameManager.Instance.Pop();
                        }
                    };
                }
                else    //没有获取到邻居，那么返回原始位置并且取消注册
                {
                    transform.DOMove(drag.originPos, movementSpeed).onComplete = () =>
                    {
                        GameManager.Instance.UninstallMovingGem(this);
                        GetComponent<SpriteRenderer>().sortingOrder = 0;
                    };
                }
                dragging = false;   //只要进入了此逻辑，就会覆盖鼠标抬起回调
            }
        }
    }

    /// <summary>
    /// 寻找从自身出发，指定方向上同类型Gem的边界，并且返回边界上的Gem
    /// </summary>
    /// <param name="g"></param>
    /// <param name="dirc"></param>
    /// <returns></returns>
    public Gem FindEdge(Gem g, Vector3 dirc)
    {
        Gem n = TryGetNeighor(g, dirc);
        if (n != null && n.type == g.type)
        {
            return n.FindEdge(n, dirc);
        }
        else
        {
            return g;
        }
    }

    /// <summary>
    /// 寻找从自身出发，指定方向上同类型Gem的边界，并且返回所有同类型的Gems
    /// </summary>
    /// <param name="g"></param>
    /// <param name="dirc"></param>
    /// <param name="gems"></param>
    public void FindEdge(Gem g, Vector3 dirc, ref List<Gem> gems)
    {
        if (g != null) gems.Add(g);
        Gem n = TryGetNeighor(g, dirc);
        if (n != null && n.type == g.type) n.FindEdge(n, dirc, ref gems);
    }

    /// <summary>
    /// 检测水平和垂直方向是否构成3连，如果构成添加至等待Pop的列表
    /// </summary>
    public void Check()
    {
        Gem bottom = FindEdge(this, Vector3.down);
        Gem left = FindEdge(this, Vector3.left);

        List<Gem> verticals = new List<Gem>();
        List<Gem> horizontals = new List<Gem>();
        FindEdge(bottom, Vector3.up, ref verticals);
        FindEdge(left, Vector3.right, ref horizontals);

        if (verticals.Count >= 3) GameManager.Instance.AddPoppingGems(verticals);
        if (horizontals.Count >= 3) GameManager.Instance.AddPoppingGems(horizontals);
    }

    List<Gem> FindNeighbors()
    {
        List<Gem> gems = new List<Gem>();
        Vector2 dirc = Vector2.up;
        for (int i = 0; i < 4; i++)
        {
            dirc = dirc.x * new Vector2(0, 1) + dirc.y * new Vector2(-1, 0);
            Gem n = TryGetNeighor(this, dirc);

            if (n != null) gems.Add(n);
        }
        return gems;
    }

    Gem TryGetNeighor(Gem gem, Vector2 dirc)
    {
        Gem n = null;
        Vector2 normalizedDirc = dirc.normalized;
        int trueX = (int)Mathf.Sign(normalizedDirc.x) * Mathf.RoundToInt(Mathf.Abs(normalizedDirc.x));
        int trueY = (int)Mathf.Sign(normalizedDirc.y) * Mathf.RoundToInt(Mathf.Abs(normalizedDirc.y));
        Vector2Int trueDirc = new Vector2Int(trueX, trueY);
        GameManager.Instance.gems.TryGetValue(new Vector2Int(gem.x, gem.y) + trueDirc, out n);
        return n;
    }

    /// <summary>
    /// 和一个Gem交换位置，仅数据
    /// </summary>
    /// <param name="target"></param>
    void ExChangePosData(Gem target)
    {
        Vector3Int tempInt = new Vector3Int(target.x, target.y);

        target.x = x;
        target.y = y;

        x = tempInt.x;
        y = tempInt.y;

        drag.originPos = CurPos;
        target.drag.originPos = target.CurPos;

        GameManager.Instance.gems[new Vector2Int(x, y)] = this;
        GameManager.Instance.gems[new Vector2Int(target.x, target.y)] = target;
    }

    /// <summary>
    /// 将Gem移动至指定位置，这个位置必须是空位
    /// </summary>
    /// <param name="pos"></param>
    public void MoveTo(Vector2Int pos)
    {
        if (GameManager.Instance.gems.ContainsKey(pos))
        {
            if (GameManager.Instance.gems[pos] != null)
            {
                throw new Exception("无法将Gem移动至非空位的位置！");
            }
        }
        else
        {
            throw new Exception("目标位置非法！");
        }

        x = pos.x;
        y = pos.y;
        GameManager.Instance.gems[new Vector2Int(x, y)] = this;

        transform.DOMove(CurPos, movementSpeed).onComplete = () =>
        {
            drag.originPos = CurPos;
            GameManager.Instance.UninstallMovingGem(this);
        };
        GameManager.Instance.RegisterMovingGem(this);
    }

    Color TempColor()
    {
        switch (type)
        {
            case 1:
                return Color.red;
            case 2:
                return Color.green;
            case 3:
                return Color.blue;
            default:
                return Color.gray;
        }
    }
}
