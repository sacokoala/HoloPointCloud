using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;
using System;

public class CameraParentScript : MonoBehaviour {

	// mouse emulation
	public bool emulateMouseInEditor=true;
	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	public RotationAxes axes = RotationAxes.MouseXAndY;
	//public Texture nogyrotexture;
	
#if UNITY_EDITOR
	public float sensitivityX = 5F;
	public float sensitivityY = 5F;
	
	private float minimumX = -360F;
	private float maximumX = 360F;
	
	private float minimumY = -90F;
	private float maximumY = 90F;
	
	float rotationY = 0F;
#endif
	
	
	public static void divecommand(string command){
		#if UNITY_EDITOR
		#elif UNITY_ANDROID
		dive_command(command);
		#elif UNITY_IPHONE
		#endif
		
	}
	
	public static void setFullscreen(){
		#if UNITY_EDITOR
		
		#endif 	
		
		return;
	}
	
	// Use this for initialization
	void Start () {	
		
		// Disable screen dimming
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		Application.targetFrameRate = 60;
		
		
		
#if UNITY_EDITOR
		
		if (GetComponent<Rigidbody>())
			GetComponent<Rigidbody>().freezeRotation = true;
		
#endif
		
	}
	
	// Update is called once per frame
	void Update () {

#if UNITY_EDITOR
				
#endif




#if UNITY_EDITOR
		
		if (emulateMouseInEditor){
			
			
			if (axes == RotationAxes.MouseXAndY)
			{
				float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;
				
				rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
				rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
				
				transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
			}
			else if (axes == RotationAxes.MouseX)
			{
				transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX * -1.0f, 0);
			}
			else
			{
				rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
				rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
				
				transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
			}
		}
#endif

	}
}
