using System.IO;
using TMPro;
using UnityEngine;

/// Test access to persistent data folder and abort demo in case of any error
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
            File.WriteAllText(testFilePath, "Test");
            string content = File.ReadAllText(testFilePath);
            File.Delete(testFilePath);
        }
        catch (System.Exception ex)
        {
            accessOk = false;
            errorMessage = ($"Unable to access user folder\n{Application.persistentDataPath}\n\n{ex.GetType().Name}\n\nDemo aborted.\nPls enable access and retry. ^^");
            ErrorMessage.GetComponent<TextMeshProUGUI>().text = errorMessage;
            Util.WriteLog(errorMessage);
            Destroy(IntroManager);
            foreach (Transform child in IntroUI.transform)
            {
                Destroy(child.gameObject);
            }
        }
        if (accessOk)
        {
            Util.WriteLog("Persistent data folder access OK");
            Destroy(gameObject);
        }
    }
}
