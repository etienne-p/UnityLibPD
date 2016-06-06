using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LibPD))]
public class TestLibPD : MonoBehaviour 
{
	public string patch;
	public AudioClip clip;

	int patchId = -1;
	bool patchOpened = false;
	float pitch = .5f;
	LibPD libPd;

	void Start()
	{
		libPd = GetComponent<LibPD> ();
	}

	void OnGUI()
	{
		float x = 0;
		float y = 0;
		float btnWidth = Screen.width * .4f;
		float btnHeight = btnWidth * .2f;
		float margin = 20.0f;

		if (!patchOpened)
		{
			if (GUI.Button (new Rect (x, y, btnWidth, btnHeight), "Open Patch")) 
			{
				patchOpened = true;
				libPd.OpenPatch (patch, id => patchId = id);
				libPd.LoadClip ("sample0", clip, "sample_length");
			}
		} 
		else if (patchId != -1 && GUI.Button (new Rect (x, y, btnWidth, btnHeight), "Close patch")) 
		{
			libPd.ClosePatch (patchId);
			patchOpened = false;
		}

		y += btnHeight + margin;
		GUI.Label (new Rect(x, y, btnWidth, btnHeight), "Pitch:");
		y += margin;
		pitch = GUI.HorizontalSlider (new Rect(x, y, btnWidth, btnHeight), pitch, .0f, 1.0f);
		libPd.SendFloat ("pitch", pitch);
	}
}
