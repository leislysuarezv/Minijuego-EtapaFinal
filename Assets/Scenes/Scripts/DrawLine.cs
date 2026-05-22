using System.Collections.Generic;
using UnityEngine;

public class DrawLine : MonoBehaviour
{
    [SerializeField] private float paintingStartX = -30.45f;
    [SerializeField] private float accuracyTolerance = 1.15f;

    private LineRenderer line;
    private readonly List<Vector3> points = new List<Vector3>();
    private readonly List<float> pointAccuracyScores = new List<float>();
    private ScoreManager scoreManager;
    private Vector2[] guideRoute;
    private float[] guideDistances;
    private float guideLength;
    private float furthestRouteDistance;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 0;
        scoreManager = FindObjectOfType<ScoreManager>();
        BuildGuideRoute();

        // 🔥 GROSOR DE LA LÍNEA
        line.startWidth = 0.2f;
        line.endWidth = 0.2f;
    }

    void OnEnable()
    {
        CursorInputRouter.Instance.Held += HandlePaintingInput;
    }

    void OnDisable()
    {
        if (!CursorInputRouter.HasInstance)
        {
            return;
        }

        CursorInputRouter.Instance.Held -= HandlePaintingInput;
    }

    void HandlePaintingInput(Vector3 _)
    {
        if (PauseMenuUI.IsPaused || !StartIntroAnimator.GameStarted || ScoreManager.CurrentPhase != ScoreManager.GamePhase.Painting)
        {
            return;
        }

        if (line == null)
        {
            line = GetComponent<LineRenderer>();
        }

        Vector3 playerPosition = transform.position;

        if (playerPosition.x < paintingStartX)
        {
            return;
        }

        if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], playerPosition) > 0.1f)
        {
            points.Add(playerPosition);
            line.positionCount = points.Count;
            line.SetPositions(points.ToArray());
            ReportPaintAccuracy(playerPosition);
        }
    }

    private void BuildGuideRoute()
    {
        guideRoute = new[]
        {
            new Vector2(-30.69f, 6.78f),
            new Vector2(-27.76f, 6.78f),
            new Vector2(-25.46f, 6.78f),
            new Vector2(-24.63f, 6.78f),
            new Vector2(-24.10f, 6.51f),
            new Vector2(-23.69f, 5.31f),
            new Vector2(-20.55f, 5.31f),
            new Vector2(-17.42f, 5.31f),
            new Vector2(-17.21f, 6.24f),
            new Vector2(-17.06f, 7.24f),
            new Vector2(-14.75f, 7.24f),
            new Vector2(-12.09f, 7.24f),
            new Vector2(-11.72f, 6.47f),
            new Vector2(-11.47f, 5.38f),
            new Vector2(-7.24f, 5.38f),
            new Vector2(-3.52f, 5.38f),
            new Vector2(-3.25f, 6.25f),
            new Vector2(-3.00f, 7.18f),
            new Vector2(1.65f, 7.18f),
            new Vector2(5.83f, 7.18f),
            new Vector2(9.07f, 7.18f),
            new Vector2(9.38f, 6.41f),
            new Vector2(9.74f, 5.31f),
            new Vector2(15.49f, 5.31f),
            new Vector2(21.34f, 5.31f),
            new Vector2(21.58f, 5.81f),
            new Vector2(21.86f, 6.31f),
            new Vector2(26.57f, 6.71f),
            new Vector2(30.23f, 6.81f),
            new Vector2(33.36f, 6.84f),
            new Vector2(35.87f, 6.68f),
            new Vector2(37.64f, 6.31f)
        };

        guideDistances = new float[guideRoute.Length];
        guideLength = 0f;

        for (int i = 1; i < guideRoute.Length; i++)
        {
            guideLength += Vector2.Distance(guideRoute[i - 1], guideRoute[i]);
            guideDistances[i] = guideLength;
        }
    }

    private void ReportPaintAccuracy(Vector3 playerPosition)
    {
        if (scoreManager == null || guideRoute == null || guideRoute.Length < 2)
        {
            return;
        }

        float routeDistance;
        float distanceFromGuide = GetClosestDistanceToGuide(playerPosition, out routeDistance);
        furthestRouteDistance = Mathf.Max(furthestRouteDistance, routeDistance);

        float pointScore = Mathf.Clamp01(1f - (distanceFromGuide / Mathf.Max(0.01f, accuracyTolerance)));
        pointAccuracyScores.Add(pointScore);

        float precision = GetAveragePointAccuracy();
        float coverage = guideLength > 0f ? Mathf.Clamp01(furthestRouteDistance / guideLength) : 0f;
        scoreManager.ReportPlayerPaintAccuracy(precision * coverage * 100f);
    }

    private float GetClosestDistanceToGuide(Vector2 point, out float routeDistance)
    {
        float closestDistance = float.MaxValue;
        routeDistance = 0f;

        for (int i = 1; i < guideRoute.Length; i++)
        {
            Vector2 segmentStart = guideRoute[i - 1];
            Vector2 segmentEnd = guideRoute[i];
            Vector2 segment = segmentEnd - segmentStart;
            float segmentLengthSquared = Mathf.Max(0.0001f, segment.sqrMagnitude);
            float t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / segmentLengthSquared);
            Vector2 closestPoint = segmentStart + segment * t;
            float distance = Vector2.Distance(point, closestPoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                routeDistance = guideDistances[i - 1] + Vector2.Distance(segmentStart, closestPoint);
            }
        }

        return closestDistance;
    }

    private float GetAveragePointAccuracy()
    {
        if (pointAccuracyScores.Count == 0)
        {
            return 0f;
        }

        float total = 0f;

        for (int i = 0; i < pointAccuracyScores.Count; i++)
        {
            total += pointAccuracyScores[i];
        }

        return total / pointAccuracyScores.Count;
    }
}

