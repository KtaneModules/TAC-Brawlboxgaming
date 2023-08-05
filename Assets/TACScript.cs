using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using KModkit;
using System.Text.RegularExpressions;

public class TACScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public MeshRenderer[] LEDs, Cards;
    public MeshRenderer Pawn;
    public GameObject PawnObject;
    public GameObject[] CardObjects;
    public Material[] PawnColours, LEDColours, CardImages;

    private static int _moduleIdCounter = 1;
    private int _moduleId, finalPosition, enemyPawn1, enemyPawn2, partnerPawn, steps;
    private bool _moduleSolved, needSwap;
    private string[] numbers = new[] { "1", "2", "3", "-4", "5", "6", "7", "8", "9", "10", "12", "13" },
        powers = new[] { "Trickster", "Warrior" },
        colours = new[] { "Blue", "Green", "Red", "Yellow" },
        newColours = new string[4],
        hand = new string[5];
    private int[] coloursIxShuffle = new[] { 0, 1, 2, 3 }, serialNumber;
    private string defuserColour, swappableCard;
    private static readonly Vector3[] boardPositions = new[] {
        #region Vectors
        new Vector3(-0.04374f, 0.0220647f, 0.0228f),
        new Vector3(-0.03276f, 0.0220647f, 0.0228f),
        new Vector3(-0.02178f, 0.0220647f, 0.0228f),
        new Vector3(-0.0108f, 0.0220647f, 0.0228f),
        new Vector3(-0.00018f, 0.0220647f, 0.0228f),
        new Vector3(0.01116f, 0.0220647f, 0.0228f),
        new Vector3(0.02214f, 0.0220647f, 0.0228f),
        new Vector3(0.03312f, 0.0220647f, 0.0228f),
        new Vector3(0.0441f, 0.0220647f, 0.0228f),
        new Vector3(0.05508f, 0.0220647f, 0.0228f),
        new Vector3(0.06606f, 0.0220647f, 0.0228f),
        new Vector3(0.06606f, 0.0220647f, 0.01182f),
        new Vector3(0.06606f, 0.0220647f, 0.00084f),
        new Vector3(0.06606f, 0.0220647f, -0.01014f),
        new Vector3(0.06606f, 0.0220647f, -0.02112f),
        new Vector3(0.05508f, 0.0220647f, -0.02112f),
        new Vector3(0.0441f, 0.0220647f, -0.02112f),
        new Vector3(0.03312f, 0.0220647f, -0.02112f),
        new Vector3(0.02214f, 0.0220647f, -0.02112f),
        new Vector3(0.01116f, 0.0220647f, -0.02112f),
        new Vector3(-0.00018f, 0.0220647f, -0.02112f),
        new Vector3(-0.0108f, 0.0220647f, -0.02112f),
        new Vector3(-0.02178f, 0.0220647f, -0.02112f),
        new Vector3(-0.03276f, 0.0220647f, -0.02112f),
        new Vector3(-0.04374f, 0.0220647f, -0.02112f),
        new Vector3(-0.05472f, 0.0220647f, -0.02112f),
        new Vector3(-0.0657f, 0.0220647f, -0.02112f),
        new Vector3(-0.0657f, 0.0220647f, -0.01014f),
        new Vector3(-0.0657f, 0.0220647f, 0.00084f),
        new Vector3(-0.0657f, 0.0220647f, 0.01182f),
        new Vector3(-0.0657f, 0.0220647f, 0.0228f),
        new Vector3(-0.05472f, 0.0220647f, 0.0228f),
        #endregion
    },
        homes = new[] {
        #region Vectors
            new Vector3(-0.04374f, 0.0220647f, 0.01182f),
            new Vector3(0.0441f, 0.0220647f, 0.01182f),
            new Vector3(0.0441f, 0.0220647f, -0.01014f),
            new Vector3(-0.04374f, 0.0220647f, -0.01014f)
        #endregion
    };

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        #region Initiate random colours and final position
        coloursIxShuffle.Shuffle();
        int tmp = Random.Range(0, 4);

        for (int i = 0; i < LEDs.Length; i++)
        {
            LEDs[i].material = LEDColours[coloursIxShuffle[i]];
            newColours[i] = colours[coloursIxShuffle[i]];
            if (i == tmp)
            {
                Pawn.material = PawnColours[coloursIxShuffle[i]];
                defuserColour = colours[coloursIxShuffle[i]];
            }
        }
        for (int i = 0; i < newColours.Length; i++)
        {
            if (newColours[i] == defuserColour)
            {
                finalPosition = i;
            }
        }
        #endregion
        #region Calculate other pawn positions
        serialNumber = BombInfo.GetSerialNumber().Select(ch => ch >= '0' && ch <= '9' ? ch - '0' : ch - 'A' + 1).ToArray();
        enemyPawn1 = serialNumber[0] + serialNumber[5];
        enemyPawn2 = serialNumber[1] + serialNumber[4];
        while (enemyPawn1 == enemyPawn2)
            enemyPawn2++;
        partnerPawn = serialNumber[2] + serialNumber[3];
        while (enemyPawn1 == partnerPawn || enemyPawn2 == partnerPawn)
            partnerPawn++;
        enemyPawn1 %= 32;
        enemyPawn2 %= 32;
        partnerPawn %= 32;
        #endregion
        Debug.Log(enemyPawn1 + " " + enemyPawn2 + " " + partnerPawn);
        #region Generate random powers
        powers.Shuffle();
        int powerCount = Random.Range(0, 2);
        #endregion
        #region Generate number cards
        Cards[3].material = CardImages[13];
        #endregion
    }
}
