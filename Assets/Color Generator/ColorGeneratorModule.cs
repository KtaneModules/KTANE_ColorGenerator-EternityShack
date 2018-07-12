using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;
using Newtonsoft.Json;

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
	public BombComponent RealBombModule;
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
		RealBombModule = BombModule.GetComponent<BombComponent>();

		BombModule.OnActivate += getAnswer;
        Red.OnInteract += HandlePressRed;
        Green.OnInteract += HandlePressGreen;
        Blue.OnInteract += HandlePressBlue;
        Multiply.OnInteract += HandlePressMultiply;
		Reset.OnInteract += HandlePressReset;
        Submit.OnInteract += delegate
		{
			HandlePressSubmit("null");
			return false;
		};
		
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

        using (List<string>.Enumerator enumerator = BombInfo.QueryWidgets(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null).GetEnumerator())
	{
		if (enumerator.MoveNext())
		{
			serial = JsonConvert.DeserializeObject<Dictionary<string, string>>(enumerator.Current)["serial"];
		}
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

        if (red > 255 || green > 255 || blue > 255)
        {
            BombModule.HandleStrike();

            red = 0;
            green = 0;
            blue = 0;
            multiplier = 1;

            updateDisplay();

            displayText.color = RGBColor(255, 0, 0);
        }
	}

    bool HandlePressRed()
    {
        red += multiplier;

        updateDisplay();

        HandleButtonPress();

        return false;
    }

    bool HandlePressGreen()
	{
		green += multiplier;

        updateDisplay();

        HandleButtonPress();

        return false;
    }

    bool HandlePressBlue()
	{
		blue += multiplier;

        updateDisplay();

        HandleButtonPress();

        return false;
    }

	IEnumerator ShowFinalColor(string split4)
	{
		Color finalColor = RGBColor(desiredred, desiredgreen, desiredblue);

        for (int i = 0; i <= 255; i++)
        {
            for (int index = 0; index < 3; index++)
            {
                int newRed = UnityEngine.Random.Range(desiredred - (255 - i), desiredred + (255 - i));
                int newGreen = UnityEngine.Random.Range(desiredgreen - (255 - i), desiredgreen + (255 - i));
                int newBlue = UnityEngine.Random.Range(desiredblue - (255 - i), desiredblue + (255 - i));
				    
                Materials[index].color = Color.Lerp(DefaultColors[index], finalColor, (float)i / 100);
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

        BombModule.HandlePass();

		StatusLight light = RealBombModule.StatusLightParent.StatusLight;

		
		switch (split4)
		{
			case "red":
				light.StrikeLight.SetActive(true);
				light.InactiveLight.SetActive(false);
				light.PassLight.SetActive(false);
				break;
			case "off":
				light.StrikeLight.SetActive(false);
				light.InactiveLight.SetActive(true);
				light.PassLight.SetActive(false);
				break;
			case "random":
				var lightColor = UnityEngine.Random.value;
				if (lightColor < (1 / 3f))
					goto case "red";
				if (lightColor < (2 / 3f))
					goto case "off";
				break;
		}
    }



    bool HandlePressSubmit(string split4)
	{
		if (solved) return false;

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

			StartCoroutine(ShowFinalColor(split4));
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

        HandleButtonPress();

        return false;
    }

	bool HandlePressReset()
	{
		red = 0;
		green = 0;
		blue = 0;
		multiplier = 1;

        updateDisplay();

        HandleButtonPress();

        return false;
	}

	bool HandlePressMultiply()
	{
		multiplier *= 10;
        if (multiplier > 100)
        {
            multiplier = 1;
        }

        HandleButtonPress();

        return false;
    }

    public string TwitchHelpMessage = "Submit a color using \"!{0} press bigred 1,smallred 2,biggreen 1,smallblue 1\" !{0} press <buttonname> <amount of times to push>. If you want to be silly, you can have this module change the color of the status light when solved with \"!{0} press smallblue UseRedOnSolve\" or UseOffOnSolve. You can make this module tell a story with !{0} tellmeastory, make a needy sound with !{0} needystart or !{0} needyend, fake strike with !{0} faksestrike, and troll with !{0} troll";

    private IEnumerator TwitchHandleForcedSolve()
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
		command = command.Replace(',', ' ');
		string[] split = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		Dictionary<string, KMSelectable> cmdButtons = new Dictionary<string, KMSelectable>()
		{
			{"bigred", Red},
			{"biggreen", Green},
			{"bigblue", Blue},
			{"smallred", Multiply},
			{"smallgreen", Reset},
			{"smallblue", Submit},
		};

		Dictionary<string, string> statusLightColors = new Dictionary<string, string>()
		{
			{"usegreenonsolve", "green"},
			{"useredonsolve", "red"},
			{"useoffonsolve", "off"},
			{"userandomonsolve", "random"}
		};

		KMSelectable currentSelect = null;
		string statusLightColor = "green"; 

		foreach (string cmd in split)
		{
			if (cmdButtons.ContainsKey(cmd))
			{
				currentSelect = cmdButtons[cmd];
			}
			else if (statusLightColors.ContainsKey(cmd))
			{
				statusLightColor = statusLightColors[cmd];
			}
			else
			{
				int selectCount = 0;
				if (int.TryParse(cmd, out selectCount))
				{
					if (currentSelect == null)
						continue;

					if (currentSelect == Submit)
					{
						yield return null;
						HandlePressSubmit(statusLightColor);
						if(solved) yield return "solve";
					}
					else
					{
						while (selectCount != 0)
						{
							yield return null;
							currentSelect.OnInteract();
							selectCount--;
							yield return new WaitForSeconds(0.1f);
						}
					}
				}
			}
		}
        if (split[0] == "troll")
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
	        KMAudio.KMAudioRef audioRef = this.Audio.PlayGameSoundAtTransformWithRef(KMSoundOverride.SoundEffect.NeedyWarning, base.transform);
	        yield return new WaitForSeconds(5f);
	        audioRef.StopSound();
        }
		else if (split[0] == "tellmeastory")
		{
			yield return null;
			yield return "waiting music";
			string story = "#000000 once upon a time, there was a bomb with the seed " + RealBombModule.Bomb.Seed.ToString() + ", and it had a color generator module. a random lunatic decided to input an incorrect answer, and detonated the bomb. the end #000000";

			for (int i = 0; i < story.Length - 6; i++)
			{
				string subStory = story.Substring(i,7);
				displayText.text = subStory;
				yield return new WaitForSeconds(0.1f);
			}
		}
    }
}
