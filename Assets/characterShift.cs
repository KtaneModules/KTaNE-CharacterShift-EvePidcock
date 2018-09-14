using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using KMHelper;
using System;
using Random = UnityEngine.Random;
using UnityEngine;

public class characterShift : MonoBehaviour {

    private static int _moduleIdCounter = 1;
    public KMAudio newAudio;
    public KMBombModule module;
    public KMBombInfo info;
    public KMRuleSeedable RuleSeedable;
    private int _moduleId = 0;
    private bool _isSolved = false;

    private int[] usableNumbers = new int[] { 1, 1, 2, 2, 3, 3, 4, 5, 6 };
    private bool[] addFunc = new bool[] { true, true, false, true, false, true, false, true, true, true, false, true, true, false, true, true, true }; //true = add, false = subtract
    private int[] stopNumbers = new int[] { 1, 0, 2, 3, 4, 5, 6, 7, 8, 9 };
    private int stopNumber;

    private bool ZenModeActive = false;

    public KMSelectable numUp, numDown, letUp, letDown;

    public TextMesh numberText, letterText;

    public MeshRenderer LED;

    public Material LEDOn, LEDOff;

    private String[] numbers = new String[] { "*", "", "", "", "" };
    private String[] letters = new String[] { "*", "", "", "", "" };
    private int currentLetDis = 0, currentNumDis = 0;

    private Char[] serialLetters;

    int math(int var1, int var2, bool add)
    {
        if(add)
        {
            return var1 + var2;
        } else
        {
            return var1 - var2;
        }
    }

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        serialLetters = info.GetSerialNumberLetters().ToArray();
        x = info.GetPortCount() + serialLetters.Count();
        y = info.GetIndicators().Count() + info.GetSerialNumberNumbers().Count();

        SetupRuleSeed();

        SetupLettersNumbers();
        DisplayScreen();
        StartCoroutine(MainSystem());
        Debug.LogFormat("[Character Shift #{0}] Static variables X = {1}. Y = {2}.", _moduleId, x, y);
        Debug.LogFormat("[Character Shift #{0}] The displayed letters are as follows: {1}, {2}, {3}, {4}. The displayed numbers are as follows: {5}, {6}, {7}, {8}.", _moduleId, letters[1], letters[2], letters[3], letters[4], numbers[1], numbers[2], numbers[3], numbers[4]);
        logAnswers();
    }

    void SetupRuleSeed()
    {
        var rnd = RuleSeedable.GetRNG();
        if (rnd.Seed == 1)
        {
            stopNumber = stopNumbers[0];
            return;
        } else
        {
            usableNumbers.Shuffle(rnd);
            addFunc.Shuffle(rnd);
            stopNumbers.Shuffle(rnd);
            stopNumber = stopNumbers[0];
        }
    }

    private void Awake()
    {
        numUp.OnInteract += delegate
        {
            handleNumUp();
            return false;
        };
        numDown.OnInteract += delegate
        {
            handleNumDown();
            return false;
        };
        letUp.OnInteract += delegate
        {
            handleLetUp();
            return false;
        };
        letDown.OnInteract += delegate
        {
            handleLetDown();
            return false;
        };
    }

    void handleNumUp()
    {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, numUp.transform);
        if (_isSolved) return;
        currentNumDis++;
        if(currentNumDis == 5)
        {
            currentNumDis = 0;
        }
        DisplayScreen();
    }

    private int x, y;

    void handleNumDown()
    {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, numDown.transform);
        if (_isSolved) return;
        currentNumDis--;
        if (currentNumDis == -1)
        {
            currentNumDis = 4;
        }
        DisplayScreen();
    }

    void handleLetUp()
    {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, letUp.transform);
        if (_isSolved) return;
        currentLetDis++;
        if (currentLetDis == 5)
        {
            currentLetDis = 0;
        }
        DisplayScreen();
    }

    void handleLetDown()
    {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, letDown.transform);
        if (_isSolved) return;
        currentLetDis--;
        if (currentLetDis == -1)
        {
            currentLetDis = 4;
        }
        DisplayScreen();
    }

    void trySubmit()
    {
        Debug.LogFormat("[Character Shift #{0}] Attempting to submit solution: {1}, {2}.", _moduleId, letters[currentLetDis], numbers[currentNumDis]);
        
        if(currentLetDis == 0 || currentNumDis == 0)
        {
            Debug.LogFormat("[Character Shift #{0}] Strike with solution: {1}, {2}.", _moduleId, letters[currentLetDis], numbers[currentNumDis]);
            module.HandleStrike();
            currentNumDis = 0;
            currentLetDis = 0;
            DisplayScreen();
            return;
        }

        string currentLetter = letters[currentLetDis];
        int currentNumber = Int32.Parse(numbers[currentNumDis]);

        int letter = getNumber(currentLetter);
        letter = functions(currentNumber, letter);
        while (letter > 26)
        {
            letter -= 26;
        }
        while (letter < 1)
        {
            letter += 26;
        }
        string submitted = getLetter(letter);
        if (serialLetters.Contains(submitted.ToCharArray()[0]))
        {
            Debug.LogFormat("[Character Shift #{0}] Solved with solution: {1}, {2}. Letter: {3}.", _moduleId, letters[currentLetDis], numbers[currentNumDis], submitted);
            module.HandlePass();
            _isSolved = true;
            newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, letUp.transform);

        }
        else
        {
            Debug.LogFormat("[Character Shift #{0}] Strike with solution: {1}, {2}. Letter: {3}.", _moduleId, letters[currentLetDis], numbers[currentNumDis], submitted);
            module.HandleStrike();
            currentNumDis = 0;
            currentLetDis = 0;
            DisplayScreen();
        }
    }

    void logAnswers()
    {
        bool firstLetter = true;
        foreach (String letter in letters)
        {
            if (firstLetter)
            {
                firstLetter = false;
                continue;
            }
            bool firstNumber = true;
            foreach (String number in numbers)
            {
                if (firstNumber)
                {
                    firstNumber = false;
                    continue;
                }
                int ansInt = functions(Int32.Parse(number), getNumber(letter));
                while (ansInt > 26)
                {
                    ansInt -= 26;
                }
                while (ansInt < 1)
                {
                    ansInt += 26;
                }
                String ans = getLetter(ansInt);
                Debug.LogFormat("[Character Shift #{0}] {1}{2} would give you {3}, which would be {4}.", _moduleId, letter, number, ans, serialLetters.Contains(ans[0]) ? "CORRECT" : "WRONG");
            }
        }
    }

    void DisplayScreen()
    {
        letterText.text = letters[currentLetDis];
        numberText.text = numbers[currentNumDis];
    }

    void SetupLettersNumbers()
    {
        char gar = serialLetters[Random.Range(0, serialLetters.Length)];
        int op = Random.Range(0, 10);
        int garInt = getNumber(gar.ToString());
        switch (op)
        {
            case 0:
                garInt = math(garInt, usableNumbers[4], !addFunc[0]);
                break;
            case 1:
                garInt = math(garInt, x, !addFunc[1]);
                break;
            case 2:
                garInt = math(garInt, y, !addFunc[2]);
                break;
            case 3:
                garInt = math(math(garInt, y, !addFunc[3]), info.GetPortPlateCount(), !addFunc[4]);
                break;
            case 4:
                garInt = math(garInt, info.GetSerialNumberNumbers().ToArray().Last(), !addFunc[5]); 
                break;
            case 5:
                garInt = math(math(garInt, info.GetBatteryHolderCount(), !addFunc[6]), (x*usableNumbers[2]), !addFunc[7]);
                break;
            case 6:
                garInt = math(math(math(garInt, info.GetOnIndicators().Count(), !addFunc[8]), y, !addFunc[9]), info.GetOffIndicators().Count(), !addFunc[10]);
                break;
            case 7:
                if (info.IsIndicatorOn(KMBombInfoExtensions.KnownIndicatorLabel.SIG))
                {
                    garInt = math(garInt, x, !addFunc[11]);
                }
                else
                {
                    garInt = math(garInt, y, !addFunc[11]);
                }
                break;
            case 8:
                garInt = math(math(math(math(garInt, x, !addFunc[12]), y, !addFunc[12]), info.GetIndicators().Count(), !addFunc[13]), info.GetBatteryCount(KMBombInfoExtensions.KnownBatteryType.D), !addFunc[14]);
                break;
            case 9:
                int z = garInt;
                if (info.GetBatteryCount() > usableNumbers[5])
                {
                    z = math(z, x, !addFunc[15]);
                }
                else
                {
                    z = math(z, x, addFunc[15]);
                }
                if (info.GetIndicators().Count() > usableNumbers[4])
                {
                    z = math(z, y, !addFunc[16]);
                }
                else
                {
                    z = math(z, y, addFunc[16]);
                }
                garInt = z;
                break;
        }
        while(garInt > 26)
        {
            garInt -= 26;
        }
        while (garInt < 1)
        {
            garInt += 26;
        }
        letters[1] = getLetter(Random.Range(1, 27));
        do
        {
            letters[2] = getLetter(Random.Range(1, 27));
        } while (letters[2]==letters[1]);
        do
        {
            letters[3] = getLetter(Random.Range(1, 27));
        } while (letters[3] == letters[1] || letters[3] == letters[2]);
        do
        {
            letters[4] = getLetter(Random.Range(1, 27));
        } while (letters[4] == letters[1] || letters[4] == letters[2] || letters[4]==letters[3]);

        numbers[1] = Random.Range(0, 10).ToString();
        do
        {
            numbers[2] = Random.Range(0, 10).ToString();
        } while (numbers[2] == numbers[1]);
        do
        {
            numbers[3] = Random.Range(0, 10).ToString();
        } while (numbers[3] == numbers[1] || numbers[3] == numbers[2]);
        do
        {
            numbers[4] = Random.Range(0, 10).ToString();
        } while (numbers[4] == numbers[1] || numbers[4] == numbers[2] || numbers[4] == numbers[3]);

        if (!letters.Contains(getLetter(garInt)))
        {
            letters[Random.Range(1, 5)] = getLetter(garInt);
        }
        if (!numbers.Contains(op.ToString()))
        {
            numbers[Random.Range(1, 5)] = op.ToString();
        }
    }

    int functions(int c, int let)
    {
        switch (c)
        {
            case 0:
                return math(let, usableNumbers[4], addFunc[0]);
            case 1:
                return math(let, x, addFunc[1]);
            case 2:
                return math(let, y, addFunc[2]);
            case 3:
                return math(math(let, y, addFunc[3]), info.GetPortPlateCount(), addFunc[4]);
            case 4:
                return math(let, info.GetSerialNumberNumbers().ToArray().Last(), addFunc[5]);
            case 5:
                return math(math(let, info.GetBatteryHolderCount(), addFunc[6]), (x * usableNumbers[2]), addFunc[7]);
            case 6:
                return math(math(math(let, info.GetOnIndicators().Count(), addFunc[8]), y, addFunc[9]), info.GetOffIndicators().Count(), addFunc[10]);
            case 7:
                if (info.IsIndicatorOn(KMBombInfoExtensions.KnownIndicatorLabel.SIG))
                {
                    return math(let, x, addFunc[11]);
                }
                else
                {
                    return math(let, y, addFunc[11]);
                }
            case 8:
                return math(math(math(math(let, x, addFunc[12]), y, addFunc[12]), info.GetIndicators().Count(), addFunc[13]), info.GetBatteryCount(KMBombInfoExtensions.KnownBatteryType.D), addFunc[14]);
            case 9:
                int z = let;
                if (info.GetBatteryCount() > usableNumbers[5])
                {
                    z = math(z, x, addFunc[15]);
                }
                else
                {
                    z = math(z, x, !addFunc[15]);
                }
                if (info.GetIndicators().Count() > usableNumbers[4])
                {
                    z = math(z, y, addFunc[16]);
                }
                else
                {
                    z = math(z, y, !addFunc[16]);
                }
                return z;
            default:
                return 0;
        }
    }

    string getLetter(int index)
    {
        switch (index)
        {
            case 1:
                return "A";
            case 2:
                return "B";
            case 3:
                return "C";
            case 4:
                return "D";
            case 5:
                return "E";
            case 6:
                return "F";
            case 7:
                return "G";
            case 8:
                return "H";
            case 9:
                return "I";
            case 10:
                return "J";
            case 11:
                return "K";
            case 12:
                return "L";
            case 13:
                return "M";
            case 14:
                return "N";
            case 15:
                return "O";
            case 16:
                return "P";
            case 17:
                return "Q";
            case 18:
                return "R";
            case 19:
                return "S";
            case 20:
                return "T";
            case 21:
                return "U";
            case 22:
                return "V";
            case 23:
                return "W";
            case 24:
                return "X";
            case 25:
                return "Y";
            case 26:
                return "Z";
            default:
                return "*";
        }
    }
    int getNumber(string index)
    {
        switch (index)
        {
            case "A":
                return 1;
            case "B":
                return 2;
            case "C":
                return 3;
            case "D":
                return 4;
            case "E":
                return 5;
            case "F":
                return 6;
            case "G":
                return 7;
            case "H":
                return 8;
            case "I":
                return 9;
            case "J":
                return 10;
            case "K":
                return 11;
            case "L":
                return 12;
            case "M":
                return 13;
            case "N":
                return 14;
            case "O":
                return 15;
            case "P":
                return 16;
            case "Q":
                return 17;
            case "R":
                return 18;
            case "S":
                return 19;
            case "T":
                return 20;
            case "U":
                return 21;
            case "V":
                return 22;
            case "W":
                return 23;
            case "X":
                return 24;
            case "Y":
                return 25;
            case "Z":
                return 26;
            default:
                return 0;
        }
    }

    private IEnumerator MainSystem()
    {
        bool submit = false;
        yield return new WaitForSeconds(0.2f);
        Debug.LogFormat("[Character Shift #{0}] Main Coroutine started.", _moduleId);
        int stop = 1;
        while (!_isSolved)
        {
            stop = stopNumber;
            yield return new WaitForSeconds(0.1f);
            int last = (int)(info.GetTime() % 10);
            if(last == (stop+2)%10 && !_isSolved && !ZenModeActive)
            {
                submit = false;
                switch (info.GetStrikes())
                {
                    case 0:
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f);
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f);
                        LED.material = LEDOn;
                        break;
                    case 1:
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f/1.25f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f/1.25f);
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f/1.25f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f/1.25f);
                        LED.material = LEDOn;
                        break;
                    case 2:
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 1.5f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 1.5f);
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 1.5f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 1.5f);
                        LED.material = LEDOn;
                        break;
                    case 3:
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 1.75f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 1.75f);
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 1.75f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 1.75f);
                        LED.material = LEDOn;
                        break;
                    default:
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 2f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 2f);
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 2f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 2f);
                        LED.material = LEDOn;
                        break;

                }
            } else if (last == (stop + 8) % 10 && !_isSolved && ZenModeActive)
            {
                submit = false;
                switch (info.GetStrikes())
                {
                    case 0:
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f);
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f);
                        LED.material = LEDOn;
                        break;
                    case 1:
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 1.25f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 1.25f);
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 1.25f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 1.25f);
                        LED.material = LEDOn;
                        break;
                    case 2:
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 1.5f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 1.5f);
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 1.5f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 1.5f);
                        LED.material = LEDOn;
                        break;
                    case 3:
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 1.75f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 1.75f);
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 1.75f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 1.75f);
                        LED.material = LEDOn;
                        break;
                    default:
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 2f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 2f);
                        LED.material = LEDOn;
                        yield return new WaitForSeconds(0.25f / 2f);
                        LED.material = LEDOff;
                        yield return new WaitForSeconds(0.25f / 2f);
                        LED.material = LEDOn;
                        break;

                }
            }
            else if(last == (stop) % 10)
            {
                if ((currentLetDis != 0 || currentNumDis != 0 ) && !_isSolved && !submit)
                {
                    submit = true;
                    trySubmit();
                }
                
            } else if((last == (stop + 9) % 10 || last == (stop + 8) % 10) && !ZenModeActive)
            {
                LED.material = LEDOff;
            } else if((last== (stop + 1) % 10 || last== (stop + 2) % 10) && ZenModeActive)
            {
                LED.material = LEDOff;
            }
        }
        LED.material = LEDOff;   
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "Submit A4 with “!{0} submit A4”, cycle through the numbers and letters with “!{0} cycle”, or just one with “!{0} cycle letters” or “!{0} cycle numbers”.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        string[] split = input.ToLowerInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        if (split[0].Equals("submit"))
        {
            if (split.Length != 2) { yield break; }
            if (split[1].Length != 2) { yield break; }
            string submit = split[1];
            string letter = submit[0].ToString();
            string number = submit[1].ToString();
            yield return null;
            var doNotStartAt = ZenModeActive ? new[] { (stopNumber + 8) % 10, (stopNumber + 9) % 10, stopNumber } : new[] { stopNumber, (stopNumber + 1) % 10, (stopNumber + 2) % 10 };
            yield return new WaitUntil(() => !doNotStartAt.Contains(((int) info.GetTime()) % 10));
            for (int i = 0; i < letters.Length && !letters[currentLetDis].Equals(letter, StringComparison.InvariantCultureIgnoreCase); i++)
            {
                letUp.OnInteract();
                yield return new WaitForSeconds(0.15f);
            }
            for (int i = 0; i < numbers.Length && !numbers[currentNumDis].Equals(number); i++)
            {
                numUp.OnInteract();
                yield return new WaitForSeconds(0.15f);
            }

            // Did we find the combination?
            if (!letters[currentLetDis].Equals(letter, StringComparison.InvariantCultureIgnoreCase) || !numbers[currentNumDis].Equals(number))
            {
                yield return string.Format("sendtochat The combination {0}{1} isn’t there.", letter, number);

                // Nope. Set it back to */*
                for (int i = 0; i < letters.Length && !letters[currentLetDis].Equals("*"); i++)
                {
                    letUp.OnInteract();
                    yield return new WaitForSeconds(0.15f);
                }
                for (int i = 0; i < numbers.Length && !numbers[currentNumDis].Equals("*"); i++)
                {
                    numUp.OnInteract();
                    yield return new WaitForSeconds(0.15f);
                }
            }
        }
        else if (split[0].Equals("cycle"))
        {
            var startAt = ZenModeActive ? new[] { (stopNumber + 1) % 10, (stopNumber + 2) % 10, (stopNumber + 3) % 10, (stopNumber + 4) % 10, (stopNumber + 5) % 10 } : new[] { (stopNumber + 9) % 10, (stopNumber + 8) % 10, (stopNumber + 7) % 10, (stopNumber + 6) % 10, (stopNumber + 5) % 10 };

            if (split.Length == 1 || (split.Length == 2 && (split[1] == "letters" || split[1] == "numbers")))
            {
                yield return null;

                if (split.Length == 1 || (split.Length == 2 && split[1] == "letters"))
                {
                    yield return new WaitUntil(() => startAt.Contains(((int) info.GetTime()) % 10));
                    for (int i = 0; i < 5; i++)
                    {
                        letUp.OnInteract();
                        yield return new WaitForSeconds(0.75f);
                    }
                }
                if (split.Length == 1 || (split.Length == 2 && split[1] == "numbers"))
                {
                    yield return new WaitUntil(() => startAt.Contains(((int) info.GetTime()) % 10));
                    for (int i = 0; i < 5; i++)
                    {
                        numUp.OnInteract();
                        yield return new WaitForSeconds(0.75f);
                    }
                }
            }
        }
    }
}
public static class Extensions
{

    // Fisher-Yates Shuffle

    public static IList<T> Shuffle<T>(this IList<T> list, MonoRandom rnd)
    {

        int i = list.Count;

        while (i > 1)
        {

            int index = rnd.Next(i);

            i--;

            T value = list[index];

            list[index] = list[i];

            list[i] = value;

        }

        return list;

    }

}