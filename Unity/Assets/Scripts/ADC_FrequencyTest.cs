/*
This script is based on the TestLibPD.cs but working with another PureData patch. 
Such new patch is called ADC_FreqTest.pd , it receives audio from the physical Microphone and outputs it 
directly into the [dac~] within PureData. So be CAREFUL, if you have your microphone close to your speakers 
this would crate a very intense feedback. Go around that by using alternative sound interfaces 
(i.e. speakers away from the laptop, a alnternative microphone, or headphones)

The patch also includes Frequency analysis. To test this, a simple pure tone is created using the
[osc~] object, and the same inputGain float that is sent to the patch will control the frequency of 
this oscillator. Look for the frequency reported by the patch in the console. 
*/
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LibPD))]
public class ADC_FrequencyTest : MonoBehaviour 
{
	public string patch;

	int 	patchId 	= -1;
	float 	inputGain 	= .5f;
	bool 	patchOpened = false;

	LibPD 	libPd;

	void Start()
	{
		libPd = GetComponent<LibPD> ();
	}

	void OnGUI()
	{
		float x = 0;
		float y = 0;
		float btnWidth 	= Screen.width * .4f;
		float btnHeight = btnWidth * .2f;
		float margin 	= 20.0f;

		if (!patchOpened)
		{
			if (GUI.Button (new Rect (x, y, btnWidth, btnHeight), "Open Patch")) 
			{
				patchOpened = true;
				libPd.OpenPatch (patch, id => patchId = id);
				libPd.Subscribe("frequency");

				/*
				Microphone stream 
				*/
				var audio = GetComponent<AudioSource>();
				audio.clip = Microphone.Start("Built-in Microphone", true, 1, 44100);
				audio.loop = true;
				while (!(Microphone.GetPosition(null) > 0)){}
				audio.Play();

				libPd.SendFloat ("metroOnOff", 1); // Turn frequency snapshots ON

			}
		} 
		else if (patchId != -1 && GUI.Button (new Rect (x, y, btnWidth, btnHeight), "Close Patch")) 
		{
			libPd.ClosePatch (patchId);
			patchOpened = false;
		}

		y += btnHeight + margin;
		GUI.Label (new Rect(x, y, btnWidth, btnHeight), "Pitch:");
		y += margin;
		inputGain = GUI.HorizontalSlider (new Rect(x, y, btnWidth, btnHeight), inputGain, .0f, 1.0f);
		libPd.SendFloat ("inputGain", inputGain);
		Debug.Log("STREAM OF FLOATS: " + LibPD.receivedFloatValue);
	}
}
