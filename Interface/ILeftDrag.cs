using UnityEngine;

namespace HyperModule
{
    public interface ILeftDrag
    {
        void OnLeftDragStart(Vector2 dragStartPos, Vector2 dragEndPos);
        void OnLeftDrag(Vector2 dragStartPos, Vector2 dragEndPos);
        void OnLeftDragEnd(Vector2 dragStartPos, Vector2 dragEndPos);
    }
}