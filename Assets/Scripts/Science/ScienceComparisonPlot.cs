using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScienceEducation
{
    public class ScienceComparisonPlot : MaskableGraphic, IPointerDownHandler, IDragHandler
    {
        public ScienceConditionsChapter chapter;
        public static readonly Color ColorA = new Color32(39,102,94,255);
        public static readonly Color ColorB = new Color32(173,105,35,255);
        void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 normal=(b-a).normalized;
            normal=new Vector2(-normal.y,normal.x)*width*.5f;
            int n=vh.currentVertCount;
            vh.AddVert(a-normal,tint,Vector2.zero);vh.AddVert(a+normal,tint,Vector2.zero);
            vh.AddVert(b+normal,tint,Vector2.zero);vh.AddVert(b-normal,tint,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(chapter==null)return;
            Rect r=rectTransform.rect;
            var grid=new Color32(202,213,199,255);
            for(int i=0;i<=4;i++)
            {
                float x=r.xMin+r.width*i/4;float y=r.yMin+r.height*i/4;
                Stroke(vh,new Vector2(x,r.yMin),new Vector2(x,r.yMax),1,grid);
                Stroke(vh,new Vector2(r.xMin,y),new Vector2(r.xMax,y),1,grid);
            }
            for(int series=0;series<2;series++)
            {
                Vector2 previous=new Vector2(r.xMin,r.yMin);
                for(int i=1;i<=120;i++)
                {
                    float time=i*.5f;
                    var point=new Vector2(r.xMin+r.width*time/60,r.yMin+r.height*chapter.PercentAt(time,series)/100);
                    Stroke(vh,previous,point,3,series==0?ColorA:ColorB);previous=point;
                }
            }
            float cursor=r.xMin+r.width*chapter.TimeMinutes/60;
            Stroke(vh,new Vector2(cursor,r.yMin),new Vector2(cursor,r.yMax),2,new Color32(108,124,118,255));
            for(int i=0;i<2;i++)
            {
                float y=r.yMin+r.height*chapter.PercentAt(chapter.TimeMinutes,i)/100;
                Stroke(vh,new Vector2(cursor-4,y),new Vector2(cursor+4,y),7,i==0?ColorA:ColorB);
            }
        }
        void Seek(PointerEventData e)
        {
            Vector2 local;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,e.pressEventCamera,out local))
                chapter.SeekTime(Mathf.InverseLerp(rectTransform.rect.xMin,rectTransform.rect.xMax,local.x)*60);
        }
        public void OnPointerDown(PointerEventData e){Seek(e);}
        public void OnDrag(PointerEventData e){Seek(e);}
    }
}
