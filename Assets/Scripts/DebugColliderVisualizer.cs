using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class DebugColliderVisualizer : MonoBehaviour
{
    public static DebugColliderVisualizer Instance { get; private set; }

    [Header("Settings")]
    public Color colliderColor = new Color(0f, 1f, 0f, 0.8f);
    public Color triggerColor = new Color(1f, 1f, 0f, 0.8f);
    public Color playerColor = new Color(0f, 0.5f, 1f, 1f);
    public Color npcColor = new Color(1f, 0.2f, 0.2f, 1f);

    bool showColliders;
    Material lineMaterial;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
            showColliders = !showColliders;
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (!showColliders) return;
        if (cam != Camera.main) return;
        DrawAllColliders(cam);
    }

    void DrawAllColliders(Camera cam)
    {
        CreateLineMaterial();
        GL.PushMatrix();
        GL.LoadProjectionMatrix(cam.projectionMatrix);
        GL.modelview = cam.worldToCameraMatrix;
        lineMaterial.SetPass(0);

        var colliders = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        foreach (var col in colliders)
        {
            if (col == null || !col.gameObject.activeInHierarchy) continue;

            Color color = col.GetComponent<PlayerController>() != null ? playerColor
                        : col.GetComponentInParent<NPCCar>() != null ? npcColor
                        : col.isTrigger ? triggerColor
                        : colliderColor;

            if (col is BoxCollider2D box)
                DrawBox(box, color);
            else if (col is CircleCollider2D circle)
                DrawCircle(circle, color);
            else if (col is CapsuleCollider2D capsule)
                DrawCapsule(capsule, color);
            else if (col is PolygonCollider2D poly)
                DrawPolygon(poly, color);
        }

        GL.PopMatrix();
    }

    void DrawBox(BoxCollider2D box, Color color)
    {
        var t = box.transform;
        Vector2 center = (Vector2)t.position + RotateVector(box.offset * t.lossyScale, t.eulerAngles.z);
        Vector2 half = box.size * 0.5f;
        float angle = t.eulerAngles.z;
        Vector2 sx = (Vector2)t.lossyScale;

        Vector2[] corners = new Vector2[4];
        corners[0] = center + RotateVector(new Vector2(-half.x * sx.x, -half.y * sx.y), angle);
        corners[1] = center + RotateVector(new Vector2( half.x * sx.x, -half.y * sx.y), angle);
        corners[2] = center + RotateVector(new Vector2( half.x * sx.x,  half.y * sx.y), angle);
        corners[3] = center + RotateVector(new Vector2(-half.x * sx.x,  half.y * sx.y), angle);

        GL.Begin(GL.LINES);
        GL.Color(color);
        for (int i = 0; i < 4; i++)
        {
            GL.Vertex3(corners[i].x, corners[i].y, 0);
            GL.Vertex3(corners[(i + 1) % 4].x, corners[(i + 1) % 4].y, 0);
        }
        GL.End();
    }

    void DrawCircle(CircleCollider2D circle, Color color)
    {
        var t = circle.transform;
        Vector2 center = (Vector2)t.position + RotateVector(circle.offset * t.lossyScale, t.eulerAngles.z);
        float radius = circle.radius * Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
        int segments = 32;

        GL.Begin(GL.LINES);
        GL.Color(color);
        for (int i = 0; i < segments; i++)
        {
            float a1 = (i / (float)segments) * Mathf.PI * 2;
            float a2 = ((i + 1) / (float)segments) * Mathf.PI * 2;
            GL.Vertex3(center.x + Mathf.Cos(a1) * radius, center.y + Mathf.Sin(a1) * radius, 0);
            GL.Vertex3(center.x + Mathf.Cos(a2) * radius, center.y + Mathf.Sin(a2) * radius, 0);
        }
        GL.End();
    }

    void DrawCapsule(CapsuleCollider2D capsule, Color color)
    {
        var t = capsule.transform;
        Vector2 center = (Vector2)t.position + RotateVector(capsule.offset * t.lossyScale, t.eulerAngles.z);
        Vector2 size = capsule.size;
        Vector2 sx = (Vector2)t.lossyScale;
        float w = size.x * Mathf.Abs(sx.x);
        float h = size.y * Mathf.Abs(sx.y);

        bool vertical = capsule.direction == CapsuleDirection2D.Vertical;
        float radius = vertical ? w * 0.5f : h * 0.5f;
        float bodyLen = vertical ? Mathf.Max(0, h - w) : Mathf.Max(0, w - h);

        int segments = 16;
        GL.Begin(GL.LINES);
        GL.Color(color);

        if (vertical)
        {
            float top = center.y + bodyLen * 0.5f;
            float bot = center.y - bodyLen * 0.5f;

            GL.Vertex3(center.x - radius, top, 0);
            GL.Vertex3(center.x - radius, bot, 0);
            GL.Vertex3(center.x + radius, top, 0);
            GL.Vertex3(center.x + radius, bot, 0);

            for (int i = 0; i < segments; i++)
            {
                float a1 = (i / (float)segments) * Mathf.PI;
                float a2 = ((i + 1) / (float)segments) * Mathf.PI;
                GL.Vertex3(center.x + Mathf.Cos(a1) * radius, top + Mathf.Sin(a1) * radius, 0);
                GL.Vertex3(center.x + Mathf.Cos(a2) * radius, top + Mathf.Sin(a2) * radius, 0);
                GL.Vertex3(center.x - Mathf.Cos(a1) * radius, bot - Mathf.Sin(a1) * radius, 0);
                GL.Vertex3(center.x - Mathf.Cos(a2) * radius, bot - Mathf.Sin(a2) * radius, 0);
            }
        }
        else
        {
            float right = center.x + bodyLen * 0.5f;
            float left = center.x - bodyLen * 0.5f;

            GL.Vertex3(left, center.y + radius, 0);
            GL.Vertex3(right, center.y + radius, 0);
            GL.Vertex3(left, center.y - radius, 0);
            GL.Vertex3(right, center.y - radius, 0);

            for (int i = 0; i < segments; i++)
            {
                float a1 = (i / (float)segments) * Mathf.PI;
                float a2 = ((i + 1) / (float)segments) * Mathf.PI;
                GL.Vertex3(right + Mathf.Sin(a1) * radius, center.y + Mathf.Cos(a1) * radius, 0);
                GL.Vertex3(right + Mathf.Sin(a2) * radius, center.y + Mathf.Cos(a2) * radius, 0);
                GL.Vertex3(left - Mathf.Sin(a1) * radius, center.y - Mathf.Cos(a1) * radius, 0);
                GL.Vertex3(left - Mathf.Sin(a2) * radius, center.y - Mathf.Cos(a2) * radius, 0);
            }
        }

        GL.End();
    }

    void DrawPolygon(PolygonCollider2D poly, Color color)
    {
        var t = poly.transform;
        float angle = t.eulerAngles.z;
        Vector2 sx = (Vector2)t.lossyScale;

        GL.Begin(GL.LINES);
        GL.Color(color);

        for (int p = 0; p < poly.pathCount; p++)
        {
            var points = poly.GetPath(p);
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = (Vector2)t.position + RotateVector((points[i] + poly.offset) * sx, angle);
                Vector2 b = (Vector2)t.position + RotateVector((points[(i + 1) % points.Length] + poly.offset) * sx, angle);
                GL.Vertex3(a.x, a.y, 0);
                GL.Vertex3(b.x, b.y, 0);
            }
        }

        GL.End();
    }

    Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    void CreateLineMaterial()
    {
        if (lineMaterial != null) return;
        var shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        lineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
        lineMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
    }

    void OnDestroy()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        if (lineMaterial != null)
            DestroyImmediate(lineMaterial);
    }
}
