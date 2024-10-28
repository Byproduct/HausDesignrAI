using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextSlider : MonoBehaviour
{
    public static TextSlider Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    private const float spacingBetweenParagraphLines = 90f;
    private const float delayBetweenParagraphLines = 0.6f;
    private const float paragraphYcoordinateStart = 150f;
    private const float textLineRotationMagnitude = 5f;
    private const float textLineOscillationSpeed = 2f;
    private const float singleCharacterPositionRandomness = 200f;

    public GameObject TextObjectsParent;
    public GameObject textPrefab;

    private List<string> introTextLines = new();



    void Start()
    {
        // Intro text is in resources/introtext.txt
        TextAsset introText = Resources.Load<TextAsset>("introtext");
        if (introText != null)
        {
            string[] linesArray = introText.text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
            introTextLines = new List<string>(linesArray);
        }

        StartCoroutine(ScrollText(lineNumber: 0, initialDelay: 0f, yCoordinate: 500f, fontSize: 100f, textStayDuration: 17f));    // Welcome to
        StartCoroutine(ScrollText(lineNumber: 1, initialDelay: 0.5f, yCoordinate: 350f, fontSize: 150f, textStayDuration: 17f));  // Haus Designr AI

        // Paragraph 1
        ScrollParagraph(
            startingLineNumber: 2,
            numberOfLines: 5,
            initialDelay: 3f,
            textStayDuration: 6f,
            lastLineExtraDelay: delayBetweenParagraphLines * 4 * 0.2f // Extra delay for the "godawful AI slop"
        );

        // Paragraph 2
        ScrollParagraph(
            startingLineNumber: 7,
            numberOfLines: 4,
            initialDelay: 14f,
            textStayDuration: 4f
        );
    }

    void ScrollParagraph(int startingLineNumber, int numberOfLines, float initialDelay, float textStayDuration, float lastLineExtraDelay = 0f)
    {
        for (int index = 0; index < numberOfLines; index++)
        {
            float currentInitialDelay = initialDelay + delayBetweenParagraphLines * index;

            // Extra delay for the "godafwul AI slop"
            if (index == numberOfLines - 1)
            {
                currentInitialDelay += lastLineExtraDelay;
            }

            float currentYCoordinate = paragraphYcoordinateStart - spacingBetweenParagraphLines * index;
            StartCoroutine(ScrollText(
                lineNumber: startingLineNumber + index,
                initialDelay: currentInitialDelay,
                yCoordinate: currentYCoordinate,
                textStayDuration: textStayDuration
            ));
        }
    }

    private IEnumerator ScrollText(int lineNumber, float initialDelay, float yCoordinate, float fontSize = 80f, float textStayDuration = 5f)
    {
        yield return new WaitForSeconds(initialDelay);
        GameObject textObject = Instantiate(textPrefab, TextObjectsParent.transform);
        textObject.name = introTextLines[lineNumber];
        textObject.SetActive(false);
        TextMeshProUGUI textMeshPro = textObject.GetComponent<TextMeshProUGUI>();

        if (textObject != null)
        {
            textMeshPro.text = introTextLines[lineNumber];
            textMeshPro.fontSize = fontSize;
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(2200, yCoordinate);
        }
        StartCoroutine(SlideText(textObject, textStayDuration));
    }

    private IEnumerator SlideText(GameObject textObject, float textStayDuration)
    {
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        textObject.SetActive(true);
 
        // Each character in the text lines has randomness in their y-coordinate and slide in position over time to form a straight line
        StartCoroutine(RandomizeAndSlideBackCharacterPositions(textObject, 2.1f));
        yield return StartCoroutine(Slide(rectTransform, new Vector2(0, rectTransform.anchoredPosition.y), 3.1f));

        // Text swap for that one line at the end of slide animation
        if (textObject.GetComponent<TextMeshProUGUI>().text == "godawful AI slop.")
        {
            textObject.GetComponent<TextMeshProUGUI>().text = "impressive professional designs.";
        }

        yield return new WaitForSeconds(textStayDuration);
        StartCoroutine(ExplodeText(textObject));

        // When the last text line starts to move out, also start scene transition
        if (textObject.GetComponent<TextMeshProUGUI>().text == introTextLines[introTextLines.Count - 1])
        {
            StartCoroutine(DelayAndStartSceneTransition());
        }

        yield return StartCoroutine(Slide(rectTransform, new Vector2(-5000, rectTransform.anchoredPosition.y), 4f));
        Destroy(textObject);
    }

    IEnumerator DelayAndStartSceneTransition()
    {
        yield return new WaitForSeconds(1f);
        SceneFadeOut.Instance.StartSceneTransition();
    }


    private IEnumerator Slide(RectTransform rectTransform, Vector2 targetPosition, float duration)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsedTime = 0f;
        Quaternion originalRotation = rectTransform.rotation;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            t = Util.EaseInOut(t);

            // Update position and rotation
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            float rotationAngle = Mathf.Sin(elapsedTime * textLineOscillationSpeed) * textLineRotationMagnitude;
            rectTransform.rotation = originalRotation * Quaternion.Euler(0, 0, rotationAngle);

            yield return null;
        }

        // Ensure the final positions are set
        rectTransform.anchoredPosition = targetPosition;
        rectTransform.rotation = originalRotation;
    }

    // Each character in the text lines has randomness in their y-coordinate and slide in position over time to form a straight line
    private IEnumerator RandomizeAndSlideBackCharacterPositions(GameObject textObject, float duration)
    {
        TextMeshProUGUI textMeshPro = textObject.GetComponent<TextMeshProUGUI>();
        textMeshPro.ForceMeshUpdate();
        TMP_TextInfo textInfo = textMeshPro.textInfo;

        // Copy the original vertices positions
        Vector3[][] originalVertices = new Vector3[textInfo.meshInfo.Length][];
        Vector3[][] initialRandomizedVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            originalVertices[i] = new Vector3[textInfo.meshInfo[i].vertices.Length];
            initialRandomizedVertices[i] = new Vector3[textInfo.meshInfo[i].vertices.Length];
            Array.Copy(textInfo.meshInfo[i].vertices, originalVertices[i], textInfo.meshInfo[i].vertices.Length);
            Array.Copy(textInfo.meshInfo[i].vertices, initialRandomizedVertices[i], textInfo.meshInfo[i].vertices.Length);
        }

        // Randomize the y-position of each visible character
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            float randomOffsetY = UnityEngine.Random.Range(-singleCharacterPositionRandomness, singleCharacterPositionRandomness);
            for (int j = 0; j < 4; j++)
            {
                initialRandomizedVertices[materialIndex][vertexIndex + j].y += randomOffsetY;
            }
        }
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = initialRandomizedVertices[i];
            textMeshPro.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }

        // Interpolate the vertices back to their original positions over time
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration); // Interpolation factor (0 to 1)

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                for (int j = 0; j < 4; j++)
                {
                    textInfo.meshInfo[materialIndex].vertices[vertexIndex + j] = Vector3.Lerp(
                        initialRandomizedVertices[materialIndex][vertexIndex + j],
                        originalVertices[materialIndex][vertexIndex + j],
                        t
                    );
                }
            }

            // Update the mesh with interpolated vertices
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }
            yield return null;
        }

        // Ensure final positions are exact originals after the interpolation finishes
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = originalVertices[i];
            textMeshPro.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }


    // Make individual characters fly in somewhat random directions when sliding out each text line
    private IEnumerator ExplodeText(GameObject textObject)
    {
        yield return new WaitForSeconds(0.1f);

        TextMeshProUGUI textMesh = textObject.GetComponent<TextMeshProUGUI>();
        textMesh.ForceMeshUpdate();

        TMP_TextInfo textInfo = textMesh.textInfo;
        int characterCount = textInfo.characterCount;

        // Arrays to hold initial positions and random directions
        Vector3[][] initialVertices = new Vector3[characterCount][];
        Vector3[] directions = new Vector3[characterCount];

        // Store initial positions and generate random directions
        for (int i = 0; i < characterCount; i++)
        {
            // Skip invisible characters
            if (!textInfo.characterInfo[i].isVisible)
                continue;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;

            initialVertices[i] = new Vector3[4];
            for (int j = 0; j < 4; j++)
            {
                initialVertices[i][j] = textInfo.meshInfo[materialIndex].vertices[vertexIndex + j];
            }

            // Get a random direction inside 180 degrees moving left
            float angle = UnityEngine.Random.Range(180f, 270f);
            float angleInRadians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians)).normalized;
            directions[i] = direction;
        }

        float explosionDuration = 2f;
        float elapsedTime = 0f;

        while (elapsedTime < explosionDuration)
        {
            float deltaTime = Time.deltaTime;
            elapsedTime += deltaTime;
            float progress = elapsedTime / explosionDuration * 100f;

            // Loop through each character and update its position
            for (int i = 0; i < characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                Vector3 offset = directions[i] * progress;

                for (int j = 0; j < 4; j++)
                {
                    vertices[vertexIndex + j] = initialVertices[i][j] + offset;
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                textMesh.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            yield return null; 
        }
    }
}
