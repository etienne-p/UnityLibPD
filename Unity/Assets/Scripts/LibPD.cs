using UnityEngine;
using System;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;

[RequireComponent(typeof(AudioSource))]
public class LibPD : MonoBehaviour
{
	// DEBUG related stuff

	const string PLUGIN_NAME = "AudioPluginLibPD";

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void DebugLogDelegate(string str);

	private static DebugLogDelegate debugLogCallback;

	private static void DebugLog(string str)
	{
		Debug.Log("[NATIVE LIBPD]" + str);
	}

	private static void SetPrintCallback()
	{
		debugLogCallback = new DebugLogDelegate( DebugLog );
		// Convert callback_delegate into a function pointer that can be used in unmanaged code.
		IntPtr intptr_delegate = 
			Marshal.GetFunctionPointerForDelegate(debugLogCallback);
		LibPD_SetDebugFunction( intptr_delegate );
	}

	// PD Bindings

	[DllImport (PLUGIN_NAME)]
	private static extern void LibPD_SetDebugFunction( IntPtr fp );

	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_Create (int id);

	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_Init (int id, float samplerate);

	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_Release (int id);

	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_ReleaseAll ();

	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_WriteArray (int id, 
		[MarshalAs (UnmanagedType.LPStr)] string name, 
		float[] buffer, int numSamples);

	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_SetComputeAudio (int id, bool state);

	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_SendBang (int id, 
		[MarshalAs (UnmanagedType.LPStr)] string dest);

	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_SendFloat (int id, 
		[MarshalAs (UnmanagedType.LPStr)] string dest, float num);

	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_SendSymbol (int id, 
		[MarshalAs (UnmanagedType.LPStr)] string dest, 
		[MarshalAs (UnmanagedType.LPStr)] string symbol);

	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_SendMessage (int id, 
		[MarshalAs (UnmanagedType.LPStr)] string dest, 
		[MarshalAs (UnmanagedType.LPStr)] string message);
	
	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_SendFloat (int id, int channel, int pitch);

	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_ClosePatch (int pdId, int patchId);

	[DllImport (PLUGIN_NAME)]
	private static extern bool LibPD_CloseAllPatches (int id);

	[DllImport (PLUGIN_NAME)]
	private static extern int LibPD_OpenPatch (int id, 
		[MarshalAs (UnmanagedType.LPStr)] string patch, 
		[MarshalAs (UnmanagedType.LPStr)] string  path);

	// Helper,
	// On Android streaming assets are stored compressed, 
	// so we first have to extract them so Pure Data can read them
	#if UNITY_ANDROID && !UNITY_EDITOR 
	private IEnumerator CopyStreamingAssetToPersistentData(string path, Action onComplete)
	{
	if (!System.IO.File.Exists(Application.persistentDataPath + "/" + path)) 
	{
	string filePath = "jar:file://" + Application.dataPath + "!/assets/" + path;
	var www = new WWW(filePath);
	yield return www;
	if (!string.IsNullOrEmpty(www.error))
	{
	Debug.LogError ("Can't read file: " + path);
	}
	System.IO.File.WriteAllText(Application.persistentDataPath + "/" + path, www.text);
	}
	onComplete.Invoke ();
	}
	#endif

	Thread thread;
	float sampleRate = 44100;
	int pdIndex = 0; // TODO: decide on rules to assign those ids
	// likely to pick one available, let's say one id per monobehaviour
	// using a static tracking of used ids
	// need to programmatically sync it with the effect index param

	void Start()
	{
		// set the Pd instance index the audio effect is suposed to pull audio from
		// we expect the audioMixer to be set and expose this parameter
		GetComponent<AudioSource>().outputAudioMixerGroup.audioMixer.SetFloat ("pdIndex", pdIndex);
		SetPrintCallback ();
		OnEnable();
		sampleRate = AudioSettings.outputSampleRate;
		thread = new Thread(InitPd);
		thread.Start ();
	}

	void InitPd()
	{
		Debug.Log("Pd create: " + LibPD_Create(pdIndex));
		Debug.Log("Pd init: " + LibPD_Init(pdIndex, sampleRate)); // assumed samplerate
		LibPD_SetComputeAudio (pdIndex, true);
	}

	void OnEnable()
	{
		LibPD_SetComputeAudio (pdIndex, true);
	}

	void OnDisable()
	{
		LibPD_SetComputeAudio (pdIndex, false);
	}

	public void OpenPatch(string patch, Action<int> ReceivePatchId)
	{
		string path;
		// On Android, we need to extract the .pd file from streaming assets
		#if UNITY_ANDROID && !UNITY_EDITOR 
		path = Application.persistentDataPath;
		StartCoroutine(CopyStreamingAssetToPersistentData(patch, () => {
		ReceivePatchId(LibPD.LibPD_OpenPatch (pdIndex, patch, path));
		}));
		#else 
		path = Application.streamingAssetsPath;
		ReceivePatchId(LibPD.LibPD_OpenPatch (pdIndex, patch, path));
		#endif
	}

	public void ClosePatch(int patchId)
	{
		Debug.Log ("Pd close patch: " + LibPD_ClosePatch(pdIndex, patchId));
	}

	public void CloseAllPatches()
	{
		Debug.Log ("Pd close all patches: " + LibPD_CloseAllPatches(pdIndex));
	}

	// lengthDest: set the array size in the patch
	// we consider it mandatory by convention
	// we do not rely on the [arraysize] object as it is not part of Pd core
	public void LoadClip(string name, AudioClip clip, string lengthDest)
	{
		var data = new float[clip.samples * 2];
		clip.GetData (data, 0);
		// Set PD array size
		Debug.Log("Pd send float: " + LibPD.LibPD_SendFloat (pdIndex, lengthDest, data.Length));
		// Fill it with sample data
		Debug.Log("Pd write array: " + LibPD.LibPD_WriteArray (pdIndex, name, data, data.Length));
	}

	public void SendFloat(string dest, float val)
	{
		Debug.Log("Pd send float: " + LibPD_SendFloat (pdIndex, dest, val));
	}

	public void OnDestroy()
	{
		CloseAllPatches (); // needed?
		Debug.Log("Pd release: " + LibPD_Release(pdIndex));
	}
}
