using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace tui
{
    internal class DebugRender
    {
        public static GUIStyle StringStyle { get; set; } = new GUIStyle(GUI.skin.label);

        public static Vector2 ScreenCenter
        {
            get
            {
                return new Vector2((float)Screen.width / 2f, (float)Screen.height / 2f);
            }
        }

        public static Color Color
        {
            get
            {
                return GUI.color;
            }
            set
            {
                GUI.color = value;
            }
        }

        public static Vector2 DrawString(Vector2 position, string label, Color color, bool centered = true)
        {
            DebugRender.Color = color;
            return DebugRender.DrawString(position, label, centered);
        }

        public static void GetContentAndSize(string label, out GUIContent content, out Vector2 size)
        {
            content = new GUIContent(label);
            size = DebugRender.StringStyle.CalcSize(content);
        }

        public static Vector2 DrawString(Vector2 position, string label, bool centered = true)
        {
            GUIContent content;
            Vector2 vector;
            DebugRender.GetContentAndSize(label, out content, out vector);
            GUI.Label(new Rect(centered ? (position - vector / 2f) : position, vector), content);
            return vector;
        }

        public static void DrawCrosshair(Vector2 position, float size, Color color, float thickness)
        {
            DebugRender.Color = color;
            Texture2D whiteTexture = Texture2D.whiteTexture;
            GUI.DrawTexture(new Rect(position.x - size, position.y, size * 2f + thickness, thickness), whiteTexture);
            GUI.DrawTexture(new Rect(position.x, position.y - size, thickness, size * 2f + thickness), whiteTexture);
        }

        public static void DrawBox(float x, float y, float w, float h, float thickness, Color color)
        {
            DebugRender.Color = color;
            Texture2D whiteTexture = Texture2D.whiteTexture;
            GUI.DrawTexture(new Rect(x, y, w + thickness, thickness), whiteTexture);
            GUI.DrawTexture(new Rect(x, y, thickness, h + thickness), whiteTexture);
            GUI.DrawTexture(new Rect(x + w, y, thickness, h + thickness), whiteTexture);
            GUI.DrawTexture(new Rect(x, y + h, w + thickness, thickness), whiteTexture);
        }

        public static void DrawLine(Vector2 lineStart, Vector2 lineEnd, float thickness, Color color)
        {
            DebugRender.Color = color;
            Vector2 vector = lineEnd - lineStart;
            float num = 57.29578f * Mathf.Atan(vector.y / vector.x);
            if (vector.x < 0f)
            {
                num += 180f;
            }
            if (thickness < 1f)
            {
                thickness = 1f;
            }
            int num2 = checked((int)Mathf.Ceil(thickness / 2f));
            GUIUtility.RotateAroundPivot(num, lineStart);
            GUI.DrawTexture(new Rect(lineStart.x, lineStart.y - (float)num2, vector.magnitude, thickness), Texture2D.whiteTexture);
            GUIUtility.RotateAroundPivot(-num, lineStart);
        }

        public static void DrawCircle(Vector2 center, float radius, Color color, float width, int segmentsPerQuarter)
        {
            float num = radius / 2f;
            Vector2 vector = new Vector2(center.x, center.y - radius);
            Vector2 endTangent = new Vector2(center.x - num, center.y - radius);
            Vector2 startTangent = new Vector2(center.x + num, center.y - radius);
            Vector2 vector2 = new Vector2(center.x + radius, center.y);
            Vector2 endTangent2 = new Vector2(center.x + radius, center.y - num);
            Vector2 startTangent2 = new Vector2(center.x + radius, center.y + num);
            Vector2 vector3 = new Vector2(center.x, center.y + radius);
            Vector2 startTangent3 = new Vector2(center.x - num, center.y + radius);
            Vector2 endTangent3 = new Vector2(center.x + num, center.y + radius);
            Vector2 vector4 = new Vector2(center.x - radius, center.y);
            Vector2 startTangent4 = new Vector2(center.x - radius, center.y - num);
            Vector2 endTangent4 = new Vector2(center.x - radius, center.y + num);
            DebugRender.DrawBezierLine(vector, startTangent, vector2, endTangent2, color, width, segmentsPerQuarter);
            DebugRender.DrawBezierLine(vector2, startTangent2, vector3, endTangent3, color, width, segmentsPerQuarter);
            DebugRender.DrawBezierLine(vector3, startTangent3, vector4, endTangent4, color, width, segmentsPerQuarter);
            DebugRender.DrawBezierLine(vector4, startTangent4, vector, endTangent, color, width, segmentsPerQuarter);
        }

        public static void DrawBezierLine(Vector2 start, Vector2 startTangent, Vector2 end, Vector2 endTangent, Color color, float width, int segments)
        {
            Vector2 lineStart = DebugRender.CubeBezier(start, startTangent, end, endTangent, 0f);
            checked
            {
                for (int i = 1; i < segments + 1; i++)
                {
                    Vector2 vector = DebugRender.CubeBezier(start, startTangent, end, endTangent, (float)i / (float)segments);
                    DebugRender.DrawLine(lineStart, vector, width, color);
                    lineStart = vector;
                }
            }
        }

        private static Vector2 CubeBezier(Vector2 s, Vector2 st, Vector2 e, Vector2 et, float t)
        {
            float num = 1f - t;
            return num * num * num * s + 3f * num * num * t * st + 3f * num * t * t * et + t * t * t * e;
        }

        public static void DrawBoxOnBounds3d(Camera cam, Bounds bounds, Color color)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            Vector3[] worldPoint = new Vector3[]
            {
                new Vector3(center.x + extents.x, center.y + extents.y, center.z + extents.z),
                new Vector3(center.x + extents.x, center.y + extents.y, center.z - extents.z),
                new Vector3(center.x + extents.x, center.y - extents.y, center.z - extents.z),
                new Vector3(center.x + extents.x, center.y - extents.y, center.z + extents.z),
                new Vector3(center.x - extents.x, center.y + extents.y, center.z + extents.z),
                new Vector3(center.x - extents.x, center.y + extents.y, center.z - extents.z),
                new Vector3(center.x - extents.x, center.y - extents.y, center.z - extents.z),
                new Vector3(center.x - extents.x, center.y - extents.y, center.z + extents.z)
            };

            // convert world point to screen point
            Vector2[] screenPoint = new Vector2[worldPoint.Length];
            for (int i = 0; i < screenPoint.Length; i++)
            {
                screenPoint[i] = cam.WorldToScreenPoint(worldPoint[i]);
                screenPoint[i].y = (float)Screen.height - screenPoint[i].y;
            }

            float thickness = 1.5f;

            DebugRender.DrawLine(screenPoint[0], screenPoint[1], thickness, color);
            DebugRender.DrawLine(screenPoint[1], screenPoint[2], thickness, color);
            DebugRender.DrawLine(screenPoint[2], screenPoint[3], thickness, color);
            DebugRender.DrawLine(screenPoint[3], screenPoint[0], thickness, color);
            DebugRender.DrawLine(screenPoint[4], screenPoint[5], thickness, color);
            DebugRender.DrawLine(screenPoint[5], screenPoint[6], thickness, color);
            DebugRender.DrawLine(screenPoint[6], screenPoint[7], thickness, color);
            DebugRender.DrawLine(screenPoint[7], screenPoint[4], thickness, color);
            DebugRender.DrawLine(screenPoint[0], screenPoint[4], thickness, color);
            DebugRender.DrawLine(screenPoint[1], screenPoint[5], thickness, color);
            DebugRender.DrawLine(screenPoint[2], screenPoint[6], thickness, color);
            DebugRender.DrawLine(screenPoint[3], screenPoint[7], thickness, color);
        }

        public static void DrawBoxCollider(Camera cam, BoxCollider boxCollider, Color color)
        {
            // Get the collider's world space information
            Transform transform = boxCollider.transform;
            Vector3 center = transform.TransformPoint(boxCollider.center); // Collider's center is local
            Vector3 halfExtents = boxCollider.size / 2f;

            // Calculate the eight corner points of the box collider in world space
            Vector3[] worldPoint = new Vector3[]
            {
            center + transform.rotation * new Vector3(halfExtents.x, halfExtents.y, halfExtents.z),
            center + transform.rotation * new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z),
            center + transform.rotation * new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z),
            center + transform.rotation * new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z),
            center + transform.rotation * new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z),
            center + transform.rotation * new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z),
            center + transform.rotation * new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z),
            center + transform.rotation * new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z)
            };

            // Convert world points to screen points
            Vector2[] screenPoint = new Vector2[worldPoint.Length];
            for (int i = 0; i < screenPoint.Length; i++)
            {
                screenPoint[i] = cam.WorldToScreenPoint(worldPoint[i]);
                screenPoint[i].y = (float)Screen.height - screenPoint[i].y;
            }

            float thickness = 1.5f;

            // Draw the lines connecting the corners
            DebugRender.DrawLine(screenPoint[0], screenPoint[1], thickness, color);
            DebugRender.DrawLine(screenPoint[1], screenPoint[2], thickness, color);
            DebugRender.DrawLine(screenPoint[2], screenPoint[3], thickness, color);
            DebugRender.DrawLine(screenPoint[3], screenPoint[0], thickness, color);
            DebugRender.DrawLine(screenPoint[4], screenPoint[5], thickness, color);
            DebugRender.DrawLine(screenPoint[5], screenPoint[6], thickness, color);
            DebugRender.DrawLine(screenPoint[6], screenPoint[7], thickness, color);
            DebugRender.DrawLine(screenPoint[7], screenPoint[4], thickness, color);
            DebugRender.DrawLine(screenPoint[0], screenPoint[4], thickness, color);
            DebugRender.DrawLine(screenPoint[1], screenPoint[5], thickness, color);
            DebugRender.DrawLine(screenPoint[2], screenPoint[6], thickness, color);
            DebugRender.DrawLine(screenPoint[3], screenPoint[7], thickness, color);
        }

        public static void DrawPlainAxes(Camera camera, Vector3 worldPos, Color color)
        {
            // draws 3d axis lines on world point:

            //      |  
            //      |/
            // -----·-----
            //     /|
            //      |

            float size = 0.2f;

            Vector2 xa = camera.WorldToScreenPoint(worldPos - new Vector3(size, 0f, 0f));
            Vector2 xb = camera.WorldToScreenPoint(worldPos + new Vector3(size, 0f, 0f));
            Vector2 ya = camera.WorldToScreenPoint(worldPos - new Vector3(0f, size, 0f));
            Vector2 yb = camera.WorldToScreenPoint(worldPos + new Vector3(0f, size, 0f));
            Vector2 za = camera.WorldToScreenPoint(worldPos - new Vector3(0f, 0f, size));
            Vector2 zb = camera.WorldToScreenPoint(worldPos + new Vector3(0f, 0f, size));

            // inverting screen space y coordinate
            xa.y = (float)Screen.height - xa.y;
            xb.y = (float)Screen.height - xb.y;
            ya.y = (float)Screen.height - ya.y;
            yb.y = (float)Screen.height - yb.y;
            za.y = (float)Screen.height - za.y;
            zb.y = (float)Screen.height - zb.y;

            float thickness = 1.5f;

            DebugRender.DrawLine(xa, xb, thickness, color);
            DebugRender.DrawLine(ya, yb, thickness, color);
            DebugRender.DrawLine(za, zb, thickness, color);

            Vector2 letterOffset = new Vector2(0, 10);
            DebugRender.DrawString(xb + letterOffset, "x", color);
            DebugRender.DrawString(yb + letterOffset, "y", color);
            DebugRender.DrawString(zb + letterOffset, "z", color);
        }
    }
}
