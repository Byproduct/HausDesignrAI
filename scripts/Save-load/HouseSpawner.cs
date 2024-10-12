using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class HouseSpawner : MonoBehaviour
{
    public static HouseSpawner Instance { get; private set; }
    public float HouseSpawningInterval = 1f;
    public GameObject GroundWithoutDriveway;
    public GameObject EmptyRoadsAtTheEnd;
    public int HouseNumber = 0;
    public GameObject NoHousesFoundNotice;

    void Awake()
    {
        Instance = this;
    }

    // Spawn houses in descending order of filenames (e.g. house009.hus is house0, house008.hus is house1, and so on)
    public void SpawnHouses()
    {
        StartCoroutine(SpawnHousesSlowly());
    }

    IEnumerator SpawnHousesSlowly()
    {
        HouseNumber++;
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
        foreach (var kvp in sortedHouseFiles)
        {
            string fileName = kvp.Value;
            // Load all houses saved on disc, except the one that just got saved this run
            if (fileName != HouseSaver.Instance.fileName)
            {
                HouseLoader.Instance.LoadHouse(fileName, HouseNumber);
                HouseNumber++;
                yield return new WaitForSeconds(HouseSpawningInterval);
            }
        }
        StartCoroutine(SpawnEndRoads());
    }

    IEnumerator SpawnEndRoads()
    {
        int numberOfEndRoads = 10;
        int startingPosition = (int)(HouseNumber);
        float roadTileSpacing = 2500f;
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
