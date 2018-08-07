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
    private int _moduleId = 0;
    private bool _isSolved = false, _lightsOn = false;

    public KMSelectable numUp, numDown, letUp, letDown;

    public TextMesh numberText, letterText;

    public MeshRenderer LED;

    public Material LEDOn, LEDOff;

    private String[] numbers = new String[] { "*", "", "", "", "" };
    private String[] letters = new String[] { "*", "", "", "", "" };
    private int currentLetDis = 0, currentNumDis = 0;

    private Char[] serialLetters;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        module.OnActivate += Activate;
    }

    void Activate()
    {
        Init();
        _lightsOn = true;
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
        if (!_lightsOn || _isSolved) return;
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
        if (!_lightsOn || _isSolved) return;
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
        if (!_lightsOn || _isSolved) return;
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
        if (!_lightsOn || _isSolved) return;
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

    void Init()
    {
        serialLetters = info.GetSerialNumberLetters().ToArray();
        x = info.GetPortCount() + serialLetters.Count();
        y = info.GetIndicators().Count() + info.GetSerialNumberNumbers().Count();

        SetupLettersNumbers();
        DisplayScreen();
        StartCoroutine(MainSystem());
        Debug.LogFormat("[Character Shift #{0}] Static variables X = {1}. Y = {2}.", _moduleId, x, y);
        Debug.LogFormat("[Character Shift #{0}] The displayed letters are as follows: {1}, {2}, {3}, {4}. The displayed numbers are as follows: {5}, {6}, {7}, {8}.", _moduleId, letters[1], letters[2], letters[3], letters[4], numbers[1], numbers[2], numbers[3], numbers[4]);
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
                garInt -= 3;
                break;
            case 1:
                garInt =- x;
                break;
            case 2:
                garInt =+ y;
                break;
            case 3:
                garInt = (garInt - y) + info.GetPortPlateCount();
                break;
            case 4:
                garInt =- info.GetSerialNumberNumbers().ToArray().Last();
                break;
            case 5:
                garInt = (garInt + info.GetBatteryHolderCount()) - (x * 2);
                break;
            case 6:
                garInt = (garInt - info.GetOnIndicators().Count() - y) + info.GetOffIndicators().Count();
                break;
            case 7:
                if (info.IsIndicatorOn(KMBombInfoExtensions.KnownIndicatorLabel.SIG))
                {
                    garInt -= x;
                }
                else
                {
                    garInt -= y;
                }
                break;
            case 8:
                garInt = (garInt - x - y) + info.GetIndicators().Count() - info.GetBatteryCount(KMBombInfoExtensions.KnownBatteryType.D);
                break;
            case 9:
                int z = garInt;
                if (info.GetBatteryCount() > 3)
                {
                    z -= x;
                }
                else
                {
                    z += x;
                }
                if (info.GetIndicators().Count() > 3)
                {
                    z -= y;
                }
                else
                {
                    z += y;
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
        for(int i = 1; i < 5; i++)
        {
            letters[i] = getLetter(Random.Range(1, 27));
            numbers[i] = Random.Range(0, 10).ToString();
        }
        letters[Random.Range(1, 5)] = getLetter(garInt);
        numbers[Random.Range(1, 5)] = op.ToString();
    }

    int functions(int c, int let)
    {
        switch (c)
        {
            case 0:
                return let + 3;
            case 1:
                return let + x;
            case 2:
                return let - y;
            case 3:
                return (let + y) - info.GetPortPlateCount();
            case 4:
                return let + info.GetSerialNumberNumbers().ToArray().Last();
            case 5:
                return (let - info.GetBatteryHolderCount()) + (x * 2);
            case 6:
                return (let + info.GetOnIndicators().Count() + y) - info.GetOffIndicators().Count();
            case 7:
                if (info.IsIndicatorOn(KMBombInfoExtensions.KnownIndicatorLabel.SIG))
                {
                    return let + x;
                } else
                {
                    return let + y;
                }
            case 8:
                return (let + x + y) - info.GetIndicators().Count() + info.GetBatteryCount(KMBombInfoExtensions.KnownBatteryType.D);
            case 9:
                int z = let; 
                if(info.GetBatteryCount() > 3)
                {
                    z += x;
                } else
                {
                    z -= x;
                }
                if(info.GetIndicators().Count() > 3)
                {
                    z += y;
                } else
                {
                    z -= y;
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
        while (!_isSolved)
        {
            yield return new WaitForSeconds(0.1f);
            int last = (int)(info.GetTime() % 10);
            if(last == 3 && !_isSolved)
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
            } else if(last == 1)
            {
                if (currentLetDis != 0 && currentNumDis != 0 && !_isSolved && !submit)
                {
                    submit = true;
                    trySubmit();
                }
                LED.material = LEDOff;
            }
        }
        LED.material = LEDOff;   
    }
#pragma warning disable 414

    private string TwitchHelpMessage = "Submit A4 with !submit A4";

#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string input)
    {
        Regex rgx = new Regex(@"^(submit) [A-Z][0-9]$");
        if (rgx.IsMatch(input))
        {
            string[] split = input.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            yield return null;
            string submit = split[1];
            string letter = submit.ToCharArray()[0].ToString();
            string number = submit.ToCharArray()[1].ToString();
            if (letters.Contains(letter) && numbers.Contains(number))
            {
                while (!letters[currentLetDis].Equals(letter))
                {
                    handleLetUp();
                }
                while (!numbers[currentNumDis].Equals(number))
                {
                    handleNumUp();
                }
                trySubmit();
            } else
            {
                yield return "sendtochat Can't submit " + letter + number;
            }
        }
    }

}
