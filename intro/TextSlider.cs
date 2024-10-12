using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextSlider : MonoBehaviour
{
    public static TextSlider Instance { get; private set; }

    public GameObject textPrefab;
    private List<string> introTextLines = new();
    public GameObject TextObjectsParent;

    private float spacingBetweenParagraphLines = 90f;
    private float delayBetweenParagraphLines = 0.6f;


    void Awake()
    {
        Instance = this;
    }


    void Start()
    {
        // Load introtext.txt in resources into a list of strings
        TextAsset introText = Resources.Load<TextAsset>("introtext");
        if (introText != null)
        {
            string[] linesArray = introText.text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
            introTextLines = new List<string>(linesArray);
        }
        ScrollText(lineNumber: 0, initialDelay: 0f, yCoordinate: 500f, fontSize: 100f, textStayDuration: 17f);  // Welcome to
        ScrollText(lineNumber: 1, initialDelay: 0.5f, yCoordinate: 350f, fontSize: 150f, textStayDuration: 17f);  // Haus Designr AI
        for (int i = 0; i < 5; i++)
        {
            if (i < 4)
            {
                ScrollText(lineNumber: i + 2, initialDelay: 3f + (delayBetweenParagraphLines * i), yCoordinate: 150f - spacingBetweenParagraphLines * i, textStayDuration: 6f);  // Paragraph 1
            }
            else if (i == 4) // more delay for last line in pg 1
            {
                ScrollText(lineNumber: i + 2, initialDelay: 3f + ((delayBetweenParagraphLines * i) * 1.2f), yCoordinate: 150f - spacingBetweenParagraphLines * i, textStayDuration: 6f);  // Paragraph 1
            }
        }
        for (int i = 0; i < 4; i++)
        {
            ScrollText(lineNumber: i + 7, initialDelay: 14f + (delayBetweenParagraphLines * i), yCoordinate: 150f - spacingBetweenParagraphLines * i, textStayDuration: 4f);  // Paragraph 2
        }
    }

    public void ScrollText(int lineNumber, float initialDelay, float yCoordinate, float fontSize = 80f, float textStayDuration = 5f)
    {
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
        StartCoroutine(SlideText(textObject, initialDelay, textStayDuration));
    }

    private IEnumerator SlideText(GameObject textObject, float initialDelay, float textStayDuration)
    {
        float rotationMagnitude = 5f;
        float oscillationSpeed = 2f;
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();

        yield return new WaitForSeconds(initialDelay);

        textObject.SetActive(true);
        //Randomize character y-positions and slide them back to original positions over time 
        StartCoroutine(RandomizeAndSlideBackCharacterPositions(textObject, 2.1f));

        // Slide in with character animation
        yield return StartCoroutine(Slide(rectTransform, new Vector2(0, rectTransform.anchoredPosition.y), 3.1f, rotationMagnitude, oscillationSpeed));

        if (textObject.GetComponent<TextMeshProUGUI>().text == "godawful AI slop.")
        {
            textObject.GetComponent<TextMeshProUGUI>().text = "impressive professional designs.";
        }

        // Time to read the text
        yield return new WaitForSeconds(textStayDuration);

        StartCoroutine(ExplodeText(textObject));

        // When the last text line starts to move out, also start scene transition
        if (textObject.GetComponent<TextMeshProUGUI>().text == introTextLines[introTextLines.Count - 1])
        {
            StartCoroutine(DelayAndStartSceneTransition());
        }

        yield return StartCoroutine(Slide(rectTransform, new Vector2(-5000, rectTransform.anchoredPosition.y), 4f, rotationMagnitude, oscillationSpeed));

        Destroy(textObject);
    }

    IEnumerator DelayAndStartSceneTransition()
    {
        yield return new WaitForSeconds(1f);
        SceneFadeOut.Instance.StartSceneTransition();
    }
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
            float randomOffsetY = UnityEngine.Random.Range(-200f, 200f);
            for (int j = 0; j < 4; j++)
            {
                initialRandomizedVertices[materialIndex][vertexIndex + j].y += randomOffsetY;
            }
        }

        // Apply the randomized positions to the mesh
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

            // Interpolate each character's position back to the original
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;

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



    private IEnumerator Slide(RectTransform rectTransform, Vector2 targetPosition, float duration, float rotationMagnitude, float oscillationSpeed)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsedTime = 0f;
        Quaternion originalRotation = rectTransform.rotation;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            // Ease-in-out interpolation for smoother movement
            t = t * t * (3f - 2f * t);

            // Update position and rotation
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            float rotationAngle = Mathf.Sin(elapsedTime * oscillationSpeed) * rotationMagnitude;
            rectTransform.rotation = originalRotation * Quaternion.Euler(0, 0, rotationAngle);

            yield return null;
        }

        // Ensure the final positions are set
        rectTransform.anchoredPosition = targetPosition;
        rectTransform.rotation = originalRotation;
    }


    private IEnumerator ExplodeText(GameObject textObject)
    {
        yield return new WaitForSeconds(0.1f);

        TextMeshProUGUI textMesh = textObject.GetComponent<TextMeshProUGUI>();
        textMesh.ForceMeshUpdate();

        // Get the text info
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

            // Generate a random direction inside 180 degrees moving left
            float angle = UnityEngine.Random.Range(180f, 270f);
            float angleInRadians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians)).normalized;
            directions[i] = direction;
        }

        float duration = 2f; // Explosion duration
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float deltaTime = Time.deltaTime;
            elapsedTime += deltaTime;
            float progress = elapsedTime / duration;

            // Loop through each character and update its position
            for (int i = 0; i < characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                Vector3 offset = directions[i] * progress * 100f; // Adjust 100f for speed

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
