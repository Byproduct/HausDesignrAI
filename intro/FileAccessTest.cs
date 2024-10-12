using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

// Test access to persistent data folder and abord demo if it's inaccessible
public class FileAccessTest : MonoBehaviour
{
    public GameObject IntroManager;
    public GameObject IntroUI;
    public GameObject ErrorMessage;

    private void Start()
    {
        bool accessOk = true;
        string testFilePath = Path.Combine(Application.persistentDataPath, "testfile.txt");
        string errorMessage = "";

        try
        {
            // Write test
            File.WriteAllText(testFilePath, "Test");

            // Read test
            string content = File.ReadAllText(testFilePath);

            // Delete test
            File.Delete(testFilePath);
        }
        catch (System.Exception ex)
        {
            accessOk = false;
            errorMessage = ($"Unable to access user folder\n{Application.persistentDataPath}\n\n{ex.GetType().Name}\n\nDemo aborted.\nPls enable access and retry. :>");
            ErrorMessage.GetComponent<TextMeshProUGUI>().text = errorMessage;
            Debug.Log(errorMessage);
            Destroy(IntroManager);
            foreach (Transform child in IntroUI.transform)
            {
                Destroy(child.gameObject);
            }
        }
        if (accessOk)
        {
            Debug.Log("Persistent data folder access OK");
            Destroy(gameObject);
        }
    }
}
