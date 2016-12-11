using UnityEngine;
using System;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;

[RequireComponent(typeof(AudioSource))]
public class LibPD : MonoBehaviour
{

	/*
	Dummy static variables to be used when receiving messages from Pure Data. Add custom variables here
	*/
	public static float receivedFloatValue = 0.0f;
	public static string receivedString = "";
	

	// DEBUG related stuff
	const string PLUGIN_NAME = "AudioPluginLibPD";

	/*
	Pointer to callback functions to receive the messages, prints, floats, etc, from Pure Data
	*/
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void DebugLogDelegate(string str);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void ReceiveBangDelegate(string source);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void ReceiveFloatDelegate(string source, float num);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void ReceiveSymbolDelegate(string source, string symbol);

	/*
	Instances to the pointers to the callback functions 
	*/
	private static DebugLogDelegate 		debugLogCallback;
	private static ReceiveBangDelegate 		bangCallback;
	private static ReceiveFloatDelegate 	floatCallback;
	private static ReceiveSymbolDelegate 	symbolCallback;

	/*
	Actual Functions to be called when an event is raised and sometehing is received with a tag
	to which this client has previously subscribed. The function to be called depends on the type of recived data.
	Insert custom code in these functions
	*/
	private static void DebugLog(string str)
	{
		// Called when a Print is sent
		Debug.Log("[NATIVE LIBPD]" + str);
	}

	private static void ReceivedBang(string source)
	{
		// Called when a Bang is received
		Debug.Log("[NATIVE LIBPD BANG] " + "from " + source );
	}

	private static void ReceivedFloat(string source, float num)
	{
		// Called when a Float is received
		Debug.Log("[NATIVE LIBPD FLOAT] " + "from " + source + " and value: " + num);
		receivedFloatValue = num;
	}

	private static void ReceivedSymbol(string source, string symbol)
	{
		// Called when a symbol is received
		Debug.Log("[NATIVE LIBPD SYMBOL] " + "from " + source + " and symbol: " + symbol);
	}


	/*
	Initialise all Callbacks, pass the poiner of functions to the hooks in LibPD through the methods 
	defined in UnityPdReceiver.cpp
	*/
	private static void SetAllCallbacks()
	{
		// callback_delegate are converted into a function pointer that can be used in unmanaged code.
		
		debugLogCallback = new DebugLogDelegate( DebugLog );
		IntPtr intptr_delegatePrint = 
			Marshal.GetFunctionPointerForDelegate(debugLogCallback);
		LibPD_SetDebugFunction( intptr_delegatePrint );

		bangCallback = new ReceiveBangDelegate ( ReceivedBang );
		IntPtr intptr_delegateBang = 
			Marshal.GetFunctionPointerForDelegate (bangCallback);
		LibPD_SetBangFunction( intptr_delegateBang );

		floatCallback = new ReceiveFloatDelegate( ReceivedFloat );
		IntPtr intptr_delegateFloat = 
			Marshal.GetFunctionPointerForDelegate( floatCallback );
		LibPD_SetFloatFunction( intptr_delegateFloat );

		symbolCallback = new ReceiveSymbolDelegate( symbolCallback );
		IntPtr intptr_delegateSymbol = 
			Marshal.GetFunctionPointerForDelegate( symbolCallback);
		LibPD_SetSymbolFunction( intptr_delegateSymbol );
	}

	// PD Bindings

	[DllImport (PLUGIN_NAME)]
	private static extern void LibPD_SetDebugFunction( IntPtr fp );

	[DllImport (PLUGIN_NAME)]
	private static extern void LibPD_SetBangFunction( IntPtr fp);

	[DllImport (PLUGIN_NAME)]
	private static extern void LibPD_SetFloatFunction( IntPtr fp );

	[DllImport (PLUGIN_NAME)]
	private static extern void LibPD_SetSymbolFunction( IntPtr fp);

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
		SetAllCallbacks ();
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

	public void Subscribe(string source)
	{
		Debug.Log("PD subscribed to: " + source + " " + LibPD_Subscribe(pdIndex, source));
	}

	pubic void Unsubscribe(string source)
	{
		Debug.Log("PD unsubscribed from: " + source + " " + LibPD_Unsubscribe(pdIndex, source));
	}

	public void OnDestroy()
	{
		CloseAllPatches (); // needed?
		Debug.Log("Pd release: " + LibPD_Release(pdIndex));
	}
}
