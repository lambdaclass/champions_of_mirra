using System.Collections.Generic;
using System.Linq;
using MoreMountains.TopDownEngine;
using UnityEngine;

public class AimDirection : MonoBehaviour
{
    private const float AIMSHOT_AMPLITUDE = 10f;

    [SerializeField]
    Color32 characterFeedbackColor = new Color32(255, 255, 255, 255);

    [SerializeField]
    public GameObject cone;
    public SectorController cone_controller;

    [SerializeField]
    public GameObject arrow;

    [SerializeField]
    GameObject arrowHead;

    [SerializeField]
    public GameObject area;

    [SerializeField]
    GameObject surface;
    private Vector3 initialPosition;

    UIIndicatorType activeIndicator = UIIndicatorType.None;

    public float fov = 90f;
    public float skillAngle = 0f;
    public float viewDistance = 50f;
    public int rayCount = 50;
    public float angleIncrease;

    public void InitIndicator(Skill skill, Color32 color)
    {
        // TODO: Add the spread area (angle) depending of the skill.json
        viewDistance = skill.GetSkillRange();
        skillAngle = skill.GetAngle();
        fov = skill.GetIndicatorAngle();
        activeIndicator = skill.GetIndicatorType();
        characterFeedbackColor = color;
        initialPosition = transform.localPosition;

        float hitbox = (Utils.GetGamePlayer(GameServerConnectionManager.Instance.playerId).Radius / 100) * 2;

        float circleArea = skill.GetSkillInfo().usesHitboxAsArea ? hitbox : skill.GetSkillAreaRadius();

        this.area.transform.localScale = new Vector3(circleArea, 0, circleArea);

        SetColor(color);

        if (skill.GetIndicatorType() == UIIndicatorType.Arrow)
        {
            float scaleX = skill.GetArroWidth();
            float scaleY = 1;
            float scaleZ = skill.GetSkillRange();
            arrow.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            arrow.transform.localPosition = new Vector3(0, -scaleY / 2, -0.5f);
        }
        surface.transform.localScale = new Vector3(viewDistance, viewDistance, viewDistance);
        surface.GetComponentInChildren<Renderer>().material.color = new Color32(255, 255, 255, 50);
    }

    public void Rotate(float x, float y, Skill skill)
    {
        var result = Mathf.Atan(x / y) * Mathf.Rad2Deg;
        if (y >= 0)
        {
            result += 180f;
        }
        transform.rotation = Quaternion.Euler(
            90f,
            result,
            skill.GetIndicatorType() == UIIndicatorType.Cone
                ? -(180 - skill.GetIndicatorAngle()) / 2
                : 0
        );
    }

    public void SetConeIndicator()
    {
        cone_controller.SetSectorDegree(fov);

        float coneIndicatorAngle = 0;
        angleIncrease = fov / rayCount;
        Mesh mesh = new Mesh();
        Vector3 origin = Vector3.zero;

        Vector3[] vertices = new Vector3[rayCount + 1 + 1];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rayCount * 3];

        vertices[0] = origin;
        int vertexIndex = 1;
        int trianglesIndex = 0;

        for (int i = 0; i < rayCount; i++)
        {
            Vector3 vertex = origin + GetVectorFromAngle(coneIndicatorAngle) * viewDistance;
            vertices[vertexIndex] = vertex;

            if (i > 0)
            {
                triangles[trianglesIndex + 0] = 0;
                triangles[trianglesIndex + 1] = vertexIndex - 1;
                triangles[trianglesIndex + 2] = vertexIndex;
                trianglesIndex += 3;
            }
            vertexIndex++;
            coneIndicatorAngle -= angleIncrease;
        }

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        cone.GetComponent<MeshFilter>().mesh = mesh;
    }

    private Vector3 GetBisectorDirection()
    {
        Mesh coneMesh = cone.GetComponent<MeshFilter>().mesh;
        Vector3[] coneVertices = coneMesh.vertices;

        // Calculate the vertices of the triangle
        Vector3 vertexA = cone.transform.TransformPoint(coneVertices[0]);
        Vector3 vertexB = cone.transform.TransformPoint(coneVertices[1]);
        Vector3 vertexC = cone.transform.TransformPoint(coneVertices[coneVertices.Length - 2]); // That is the last vertex

        // Calculate the sides of the triangle
        Vector3 sideAB = vertexB - vertexA;
        Vector3 sideAC = vertexC - vertexA;

        // return the bisector direction of the triangle's vertex angle
        return (sideAB.normalized + sideAC.normalized).normalized;
    }

    public bool IsInProximityRange(GameObject player)
    {
        GameObject currentPlayer = Utils.GetPlayer(GameServerConnectionManager.Instance.playerId);
        float distance = Vector3.Distance(
            currentPlayer.transform.position,
            player.transform.position
        );
        Vector3 targetDirection = player.transform.position - currentPlayer.transform.position;
        Vector3 attackDirection = currentPlayer
            .GetComponent<CharacterOrientation3D>()
            .ForcedRotationDirection;
        float playersAngle = Vector3.Angle(attackDirection, targetDirection);

        return distance <= viewDistance && playersAngle <= skillAngle / 2;
    }

    public bool IsInsideCone(GameObject player)
    {
        Vector3 bisectorDirection = GetBisectorDirection();
        Vector3 playerDirection = player.transform.position - cone.transform.position;
        playerDirection = new Vector3(playerDirection.x, 0f, playerDirection.z);
        float playerBisectorAngle = Vector3.Angle(playerDirection, bisectorDirection);
        return playerBisectorAngle <= fov / 2;
    }

    public bool IsInArrowLine(GameObject player)
    {
        GameObject currentPlayer = Utils.GetPlayer(GameServerConnectionManager.Instance.playerId);

        Vector3 arrowDirection = arrow.transform.position - currentPlayer.transform.position;
        arrowDirection = new Vector3(arrowDirection.x, 0f, arrowDirection.z);

        Vector3 playerDirection = player.transform.position - currentPlayer.transform.position;
        playerDirection = new Vector3(playerDirection.x, 0f, playerDirection.z);

        float playerArrowAngle = Vector3.Angle(arrowDirection, playerDirection);

        return playerArrowAngle <= AIMSHOT_AMPLITUDE && playerDirection.magnitude <= viewDistance;
    }

    public Vector3 GetVectorFromAngle(float angle)
    {
        float angleRad = angle * (Mathf.PI / 180f);
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }

    public void ActivateIndicator(UIIndicatorType indicatorType)
    {
        switch (indicatorType)
        {
            case UIIndicatorType.Cone:
                cone_controller.gameObject.SetActive(true);
                cone.SetActive(true);
                break;
            case UIIndicatorType.Arrow:
                arrow.SetActive(true);
                break;
            case UIIndicatorType.Area:
                area.SetActive(true);
                surface.SetActive(true);
                break;
        }
    }

    public void DeactivateIndicator()
    {
        switch (activeIndicator)
        {
            case UIIndicatorType.Cone:
                cone_controller.gameObject.SetActive(false);
                cone.SetActive(false);
                break;
            case UIIndicatorType.Arrow:
                arrow.SetActive(false);
                break;
            case UIIndicatorType.Area:
                area.SetActive(false);
                surface.SetActive(false);
                break;
        }
        Reset();
    }

    private void Reset()
    {
        transform.rotation = Quaternion.Euler(new Vector3(90f, 0, 0));
        transform.localPosition = initialPosition;
        area.transform.localPosition = initialPosition;
    }

    public void CancelableFeedback(bool cancelable)
    {
        Color32 newColor = cancelable ? new Color32(255, 0, 0, 255) : characterFeedbackColor;
        SetColor(newColor);

        newColor.a = 60;
        surface.GetComponentInChildren<Renderer>().material.color = newColor;
    }

    public void SetColor(Color32 color)
    {
        switch (activeIndicator)
        {
            case UIIndicatorType.Cone:
                color.a = 60;
                List<Renderer> coneRenderers = cone_controller
                    .GetComponentsInChildren<Renderer>()
                    .ToList();
                foreach (Renderer renderer in coneRenderers)
                {
                    renderer.material.SetColor("_AlphaColor", color);
                    renderer.material.SetColor("_TintColor", color);
                }
                cone.GetComponent<Renderer>().material.SetColor("_TopColor", color);
                break;
            case UIIndicatorType.Arrow:
                arrow.GetComponent<Renderer>().material.SetColor("_AlphaColor", color);
                arrow.GetComponent<Renderer>().material.SetColor("_TintColor", color);
                arrowHead.GetComponent<Renderer>().material.SetColor("_AlphaColor", color);
                arrowHead.GetComponent<Renderer>().material.SetColor("_TintColor", color);
                break;
            case UIIndicatorType.Area:
                area.GetComponent<Renderer>().material.SetColor("_GlowColor", color);
                area.GetComponent<Renderer>().material.SetColor("_TintColor", color);
                surface.GetComponent<Renderer>().material.SetColor("_GlowColor", color);
                surface.GetComponent<Renderer>().material.SetColor("_TintColor", color);
                break;
        }
    }
}
