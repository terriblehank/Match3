using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
 author: HankSng.
 */
namespace HS.Tools
{
    public enum TYPE
    {
        VERTICAL,
        HORIZONTAL,
        CROSS,
        FREE
    }

    [RequireComponent(typeof(Collider2D))]
    public class Draggable2D : MonoBehaviour
    {
        public delegate void Callback();
        Callback mouseDown;
        Callback mouseUp;

        [SerializeField]
        float mouseBuffer = 0.1f;   //鼠标移动超过这个距离才会开始拖拽

        public TYPE type = TYPE.FREE;

        public bool limiting;   //是否限制拖拽的范围
        public Vector2 limitRectLeftBottom; //范围限制矩形左下角 负负
        Vector2 LRLB { get { return limitRectLeftBottom + new Vector2(originPos.x, originPos.y); } }
        public Vector2 limitRectRightTop;   //范围限制矩形右下角 正正
        Vector2 LRRT { get { return limitRectRightTop + new Vector2(originPos.x, originPos.y); } }
        public Vector3 originPos;  //transform初始位置

        [SerializeField]
        bool clicked;
        public bool Clicked { get { return clicked; } }

        [SerializeField]
        bool dragging;   //是否正在拖拽，鼠标点击并且移动后才会判定为拖拽中
        public bool Dragging { get { return dragging; } }

        [SerializeField]
        private bool isVertical;
        public bool IsVertical { get { return isVertical; } }    //此参数仅在type为CROSS时启用，判定此次拖拽的方向

        [SerializeField]
        bool aborted;
        public bool Aborted { get { return aborted; } }

        // Start is called before the first frame update
        void Start()
        {
            originPos = transform.position; //记录默认的初始位置
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetMouseDownCallback(Callback c)
        {
            mouseDown = c;
        }

        public void SetMouseUpCallback(Callback c)
        {
            mouseUp = c;
        }

        Vector3 clickPos;       //用以记录鼠标点下的位置
        Vector3 positionOffset;     //用以保存鼠标和transform偏差的计算结果

        private void OnMouseDown()
        {
            clicked = true;
            clickPos = MousePos();
            Vector3 mousePosition = MousePos();
            if (mouseDown != null) mouseDown.Invoke();
        }

        private void OnMouseDrag()
        {
            if (Aborted == true) return;

            Vector3 mousePosition = MousePos();

            if (!dragging)//进入了拖拽事件但鼠标位置还没有开始移动的状态
            {
                if (Vector3.Distance(mousePosition, clickPos) >= mouseBuffer)
                {
                    positionOffset = mousePosition - transform.position;

                    Vector3 temp = clickPos - mousePosition;
                    isVertical = Mathf.Abs(temp.y) > Mathf.Abs(temp.x);
                    if (temp != Vector3.zero)
                    {
                        dragging = true;
                    }
                }
            }

            if (dragging)   //开始拖拽后计算位置变化
            {
                Vector3 newPos = mousePosition - positionOffset;
                transform.position = Format(newPos);
            }
        }

        private void OnMouseUp()
        {
            if (mouseUp != null) mouseUp.Invoke();
            dragging = false;
            clicked = false;
            aborted = false;
        }

        Vector3 MousePos()
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = transform.position.z;
            return mousePosition;
        }

        Vector3 Format(Vector3 pos)
        {
            TYPE temp = type;
            if (temp == TYPE.CROSS)
            {
                temp = IsVertical ? TYPE.VERTICAL : TYPE.HORIZONTAL;
            }

            switch (temp)
            {
                case TYPE.HORIZONTAL:
                    if (limiting)
                    {
                        pos.x = Mathf.Clamp(pos.x, LRLB.x, LRRT.x);
                    }
                    pos.y = transform.position.y;
                    break;
                case TYPE.VERTICAL:
                    pos.x = transform.position.x;
                    if (limiting)
                    {
                        pos.y = Mathf.Clamp(pos.y, LRLB.y, LRRT.y);
                    }
                    break;
                case TYPE.FREE:
                    if (limiting)
                    {
                        pos.x = Mathf.Clamp(pos.x, LRLB.x, LRRT.x);
                        pos.y = Mathf.Clamp(pos.y, LRLB.y, LRRT.y);
                    }
                    break;
            }
            return pos;
        }

        public void StopDragging()
        {
            if (clicked) aborted = true;
        }
    }
}
