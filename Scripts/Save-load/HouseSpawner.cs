// Spawn multiple houses using HouseLoader

using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class HouseSpawner : MonoBehaviour
{
    public static HouseSpawner Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    private const int numberOfEndRoads = 10;      // number of extra road tiles after the last house
    private const float roadTileSpacing = 2500f;
    public GameObject EmptyRoadsAtTheEnd;
    public GameObject GroundWithoutDriveway;
    public GameObject NoHousesFoundNotice;

    public int CurrentHouseNumber;

    public float HouseSpawningInterval;


    // Houses are spawned in descending order of filenames
    // (e.g. house009.hus is house0, house008.hus is house1, and so on)
    public void SpawnHouses()
    {
        StartCoroutine(SpawnHousesSlowly());
    }

    IEnumerator SpawnHousesSlowly()
    {
        // Get a list of house filenames and sort in descending order.
        // The process of getting a dictionary and sorting it into list is to allow for gaps in numbers.

        string path = Application.persistentDataPath;
        string[] files = Directory.GetFiles(path, "house*.hus");
        if (files.Count() < 2)
        {
            NoHousesFoundNotice.SetActive(true);
        }
        Dictionary<int, string> houseFiles = new Dictionary<int, string>();
        Regex regex = new Regex(@"house(\d{3})\.hus");
        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            Match match = regex.Match(fileName);
            if (match.Success)
            {
                int number = int.Parse(match.Groups[1].Value);
                houseFiles[number] = fileName;
            }
        }
        var sortedHouseFiles = houseFiles.OrderByDescending(kvp => kvp.Key);

        // Load all houses saved on disk, except the one that just got saved this run
        CurrentHouseNumber = 1;
        foreach (var kvp in sortedHouseFiles)
        {
            string fileName = kvp.Value;
            if (fileName != HouseSaver.Instance.fileName)
            {
                HouseLoader.Instance.LoadHouse(fileName, CurrentHouseNumber);
                CurrentHouseNumber++;
                yield return new WaitForSeconds(HouseSpawningInterval);
            }
        }
        StartCoroutine(SpawnEndRoads());
    }

    IEnumerator SpawnEndRoads()
    {
        int startingPosition = (int)(CurrentHouseNumber);

        for (int i = 0; i < numberOfEndRoads; i++)
        {
            startingPosition++;
            GameObject emptyRoad = Instantiate(GroundWithoutDriveway, EmptyRoadsAtTheEnd.transform);
            emptyRoad.name = $"empty road #{i} at the end after house #{startingPosition}";
            emptyRoad.transform.position = new Vector3((startingPosition - 1) * roadTileSpacing, 0, 0);
            yield return new WaitForSeconds(0.2f);
        }
    }
}
