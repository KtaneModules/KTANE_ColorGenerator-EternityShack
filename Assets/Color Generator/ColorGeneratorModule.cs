using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;

public class ColorGeneratorModule : MonoBehaviour
{
	public KMBombInfo BombInfo;
	public KMBombModule BombModule;
	public KMAudio Audio;
	public KMSelectable Red;
	public KMSelectable Green;
	public KMSelectable Blue;
	public KMSelectable Multiply;
	public KMSelectable Reset;
	public KMSelectable Submit;
    public TextMesh displayText;
    public GameObject displayBG;
	Material[] Materials; // Red, Green, Blue, Submit, Multiply
	private static Color[] DefaultColors = new Color[] { RGBColor(237, 28, 36), RGBColor(34, 177, 76), RGBColor(63, 72, 204) };

	int[] serialNumbers = new int[] { 0, 0, 0, 0, 0, 0 };

	int multiplier = 1;
	int red = 0;
	int green = 0;
	int blue = 0;
	int desiredred = 0;
	int desiredgreen = 0;
	int desiredblue = 0;
	bool solved = false;

    string displayAnswer;

    bool activated = false;

	static int idCounter = 1;
	int moduleID;

	private static Color RGBColor(int r, int g, int b)
	{
        if (r < 0)
        {
            r = 1;
        }
        if (g < 0)
        {
            g = 1;
        }
        if (b < 0)
        {
            b = 1;
        }
        if (r > 255)
        {
            r = 255;
        }
        if (g > 255)
        {
            g = 255;
        }
        if (b > 255)
        {
            b = 255;
        }

        return new Color((float) r / 255, (float) g / 255, (float) b / 255);
	}

	protected void Start()
    {
		moduleID = idCounter++;

		BombModule.OnActivate += getAnswer;
        Red.OnInteract += HandlePressRed;
        Green.OnInteract += HandlePressGreen;
        Blue.OnInteract += HandlePressBlue;
        Multiply.OnInteract += HandlePressMultiply;
		Reset.OnInteract += HandlePressReset;
        Submit.OnInteract += HandlePressSubmit;
		
		Materials = new KMSelectable[] { Red, Green, Blue, Multiply, Reset, Submit }.Select(selectable => selectable.GetComponent<Renderer>().material).ToArray();

		int index = 0;
		foreach (Material mat in Materials)
		{
			mat.color = DefaultColors[index % 3];
			index++;
		}
        displayBG.GetComponent<Renderer>().material.color = RGBColor(0, 0, 0);
	}

    void updateDisplay()
    {
        displayText.color = RGBColor(255, 255, 255);
        displayText.text = "#" + red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");
    }

	public void Log(params object[] args)
	{
		Log(string.Join(" ", args.Select(x => x.ToString()).ToArray()));
	}

	public void Log(string format, params object[] args)
	{
		Debug.LogFormat(string.Format("[Color Generator #{0}] {1}", moduleID, format), args);
	}

	void getAnswer()
    {
        activated = true;

        updateDisplay();

        string serial = "AB1CD2";

        List<string> data = BombInfo.QueryWidgets(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null);

        foreach (string response in data)
        {
            Dictionary<string, string> responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            serial = responseDict["serial"];
            break;
        }

		serialNumbers = serial.Select(c =>
		{
			int n;
			if (char.IsLetter(c))
			{
				n = c - 'A' + 1;
			}
			else if (char.IsDigit(c))
			{
				n = c - '0';
			}
			else
			{
				BombModule.HandlePass();
				throw new NotSupportedException("The serial number contains something that's not a letter or a number.");
			}

			return n % 16;
		}).ToArray();
        Log("Serial numbers converted to numbers, modulo 16: {0} {1} {2} {3} {4} {5}", serialNumbers[0], serialNumbers[1], serialNumbers[2], serialNumbers[3], serialNumbers[4], serialNumbers[5]);

        desiredred = (serialNumbers[0] * 16) + (serialNumbers[1] * 1);
		desiredgreen = (serialNumbers[2] * 16) + (serialNumbers[3] * 1);
		desiredblue = (serialNumbers[4] * 16) + (serialNumbers[5] * 1);

		Log("Your color code is {0} {1} {2} (#{3})", desiredred, desiredgreen, desiredblue, string.Join("", serialNumbers.Select(x => x.ToString("X")).ToArray()));
    }

	private void HandleButtonPress()
	{
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		GetComponent<KMSelectable>().AddInteractionPunch();
	}

    bool HandlePressRed()
    {
		HandleButtonPress();

        red += multiplier;

        updateDisplay();

        return false;
    }

    bool HandlePressGreen()
	{
		HandleButtonPress();

		green += multiplier;

        updateDisplay();

        return false;
    }

    bool HandlePressBlue()
	{
		HandleButtonPress();

		blue += multiplier;

        updateDisplay();

        return false;
    }

	IEnumerator ShowFinalColor()
	{
		Color finalColor = RGBColor(desiredred, desiredgreen, desiredblue);

		for (int i = 0; i <= 255; i++)
		{
			for (int index = 0; index < 3; index++)
			{
                int newRed = UnityEngine.Random.Range(desiredred - (255 - i), desiredred + (255 - i));
                int newGreen = UnityEngine.Random.Range(desiredgreen - (255 - i), desiredgreen + (255 - i));
                int newBlue = UnityEngine.Random.Range(desiredblue - (255 - i), desiredblue + (255 - i));

                Materials[index].color = Color.Lerp(DefaultColors[index], finalColor, (float) i / 100);
                displayText.color = RGBColor(newRed, newGreen, newBlue);

                if (newRed < 0)
                {
                    newRed = 0;
                }
                if (newGreen < 0)
                {
                    newGreen = 0;
                }
                if (newBlue < 0)
                {
                    newBlue = 0;
                }

                displayText.text = "#" + newRed.ToString("X2") + newGreen.ToString("X2") + newBlue.ToString("X2");
            }

			yield return new WaitForSeconds(0.01f);
		}

        displayText.text = displayAnswer;
        displayText.color = finalColor;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);

        Camera.main.transform.Translate(new Vector3(-0.05f, 0, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0.05f, 0.05f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0.05f, -0.05f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(-0.05f, -0.05f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0, 0.05f, 0));
        yield return new WaitForSeconds(0.01f);
        //
        Camera.main.transform.Translate(new Vector3(-0.04f, 0, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0.04f, 0.04f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0.04f, -0.04f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(-0.04f, -0.04f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0, 0.04f, 0));
        yield return new WaitForSeconds(0.01f);
        //
        Camera.main.transform.Translate(new Vector3(-0.03f, 0, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0.03f, 0.03f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0.03f, -0.03f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(-0.03f, -0.03f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0, 0.03f, 0));
        yield return new WaitForSeconds(0.01f);
        //
        Camera.main.transform.Translate(new Vector3(-0.02f, 0, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0.02f, 0.02f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0.02f, -0.02f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(-0.02f, -0.02f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0, 0.02f, 0));
        yield return new WaitForSeconds(0.01f);
        //
        Camera.main.transform.Translate(new Vector3(-0.01f, 0, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0.01f, 0.01f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0.01f, -0.01f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(-0.01f, -0.01f, 0));
        yield return new WaitForSeconds(0.01f);
        Camera.main.transform.Translate(new Vector3(0, 0.01f, 0));

        BombModule.HandlePass();
    }



    bool HandlePressSubmit()
	{
		if (solved) return false;

		HandleButtonPress();

        if (!activated)
        {
            
            BombModule.HandleStrike();
            Log("Attempted to submit before bomb activated. Module reset.");

            red = 0;
            green = 0;
            blue = 0;
            multiplier = 1;
        }
		else if (red == desiredred && green == desiredgreen && blue == desiredblue)
        {
			Log("Submitted the correct color! Module solved.");
            solved = true;

            displayAnswer = displayText.text;

			StartCoroutine(ShowFinalColor());
		}
        else
        {
            BombModule.HandleStrike();
            Log("Submitted an incorrect color! ({0}, {1}, {2}) Module reset.", red, green, blue);

			red = 0;
            green = 0;
			blue = 0;
			multiplier = 1;
            displayText.text = "#000000";
            displayText.color = RGBColor(255, 0, 0);
		}

        return false;
    }

	bool HandlePressReset()
	{
		HandleButtonPress();

		red = 0;
		green = 0;
		blue = 0;
		multiplier = 1;

        updateDisplay();

		return false;
	}

	bool HandlePressMultiply()
	{
		HandleButtonPress();

		multiplier *= 10;
        if (multiplier > 100)
        {
            multiplier = 1;
        }

        return false;
    }

    public string TwitchHelpMessage = "Submit a color using !{0} submit 123 123 123.";

    public void TwitchHandleForcedSolve()
    {
        StartCoroutine(Solver());
    }

    private IEnumerator Solver()
    {
        Reset.OnInteract();
        yield return new WaitForSeconds(0.1f);

        KMSelectable[] buttons = new KMSelectable[] { Red, Green, Blue };
        int[] values = new int[] { desiredred, desiredgreen, desiredblue };
        for (int i = 0; i < 3; i++)
        {
            for (int index = 0; index < 3; index++)
            {
                for (int x = 0; x < values[index] % 10; x++)
                {
                    buttons[index].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }

                values[index] /= 10;
            }

            Multiply.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        Submit.OnInteract();
    }

    public IEnumerator ProcessTwitchCommand(string command)
	{
		string[] split = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (split.Length == 4 && split[0] == "submit")
		{
			int myRed;
			int myGreen;
			int myBlue;

			if (int.TryParse(split[1], out myRed) && int.TryParse(split[2], out myGreen) && int.TryParse(split[3], out myBlue))
			{
				yield return null;

				Reset.OnInteract();
				yield return new WaitForSeconds(0.1f);

				KMSelectable[] buttons = new KMSelectable[] { Red, Green, Blue };
				int[] values = new int[] { myRed, myGreen, myBlue };
				for (int i = 0; i < 3; i++)
				{
					for (int index = 0; index < 3; index++)
					{
						for (int x = 0; x < values[index] % 10; x++)
						{
							buttons[index].OnInteract();
							yield return new WaitForSeconds(0.1f);
						}

						values[index] /= 10;
					}

					Multiply.OnInteract();
					yield return new WaitForSeconds(0.1f);
				}

				Submit.OnInteract();

                if (myRed == desiredred && myGreen == desiredgreen && myBlue == desiredblue)
                {
                    yield return "solve";
                }
            }
            else
            {
                yield return "sendtochaterror Please input numbers. (You know what a number is, right?)";
            }
		}
        
        else if (split[0] == "troll")
        {
            yield return "Color Generator";
            yield return "waiting music";
            yield return "sendtochat /me HAHAHAHA";
            Reset.OnInteract();
            yield return new WaitForSeconds(0.1f);

            KMSelectable[] buttons = new KMSelectable[] { Red, Green, Blue };
            int[] values = new int[] { 25, 25, 25 };
            for (int i = 0; i < 3; i++)
            {
                for (int index = 0; index < 3; index++)
                {
                    for (int x = 0; x < values[index]; x++)
                    {
                        buttons[index].OnInteract();
                        displayText.text = "HAHAHAHA";
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }

            Reset.OnInteract();
        }
        else if (split[0] == "fakestrike")
        {
            yield return null;
            yield return "multiple strikes";
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
        }
        else if (split[0] == "needystart")
        {
            yield return null;
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.NeedyActivated, transform);
        }
        else if (split[0] == "needyend")
        {
            yield return null;
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.NeedyWarning, transform);
        }
    }
}
