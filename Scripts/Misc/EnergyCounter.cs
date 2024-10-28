// Faux energy expenditure counter while casting blocks

using System.Collections;
using System.Numerics;
using TMPro;
using UnityEngine;

public class EnergyCounter : MonoBehaviour
{
    public static EnergyCounter Instance { get; private set; }
    public void Awake()
    {
        Instance = this;
    }

    public float currentTime = 0f;
    public TextMeshProUGUI energyText;
    public GameObject energyTextObject;

    private float counterDuration = 20f;
    private BigInteger totalEnergySpent; // when "long" isn't long enough
    private bool isCounting = false;


    public void Initiate()
    {
        StartCoroutine(Startup());
    }

    IEnumerator Startup()
    {
        float startupDelay = 5f;
        if (Configuration.Speed != Configuration.SpeedType.Normal)
        {
            startupDelay = 1f;
        }
        counterDuration = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 20f,
            Configuration.SpeedType.Fast => 12f,
            Configuration.SpeedType.Dev => 4f,
            _ => 20f,
        };

        yield return new WaitForSeconds(startupDelay);
        energyText.text = "";
        energyTextObject.SetActive(true);
        isCounting = true;
    }

    private BigInteger targetValue = BigInteger.Parse("932452351432529345909403");

    void Update()
    {
        if (isCounting)
        {
            currentTime += Time.deltaTime;
            if (counterDuration < 1f)
            {
                counterDuration = 1f;
            }
            float t = Mathf.Clamp01(currentTime / counterDuration);
            float easedT = Mathf.Pow(t, 10f); 
            BigInteger totalEnergySpent = InterpolateBigInteger(BigInteger.Zero, targetValue, easedT);
            if (currentTime >= counterDuration)
            {
                energyText.text = $"Energy spent re-training AI model: <color=red>unknown</color>";
                isCounting = false;
            }
            else
            {
                energyText.text = $"Energy spent re-training AI model: {totalEnergySpent.ToString("N0")} GW";
            }
        }
    }

    // Helper function to interpolate between two BigIntegers
    BigInteger InterpolateBigInteger(BigInteger start, BigInteger end, float t)
    {
        t = Mathf.Clamp01(t);
        BigInteger range = end - start;
        BigInteger interpolatedValue = start + BigInteger.Multiply(range, (BigInteger)(t * 100000000000000000)) / 100000000000000000;

        return interpolatedValue;
    }
}
