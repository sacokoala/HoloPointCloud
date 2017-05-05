using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VR.WSA.WebCam;
using System;

public class PhotoCaptureScript : MonoBehaviour, IInputClickHandler
{

    PhotoCapture photoCaptureObject = null;

    private bool isCapturing = false;

    // Use this for initialization
    void Start () {

    }
	
	// Update is called once per frame
	void Update () {

    }


    void IInputClickHandler.OnInputClicked(InputClickedEventData eventData)
    {
        if (!isCapturing)
        {
            if (photoCaptureObject != null)
            {
                photoCaptureObject.Dispose();
                photoCaptureObject = null;
            }
            PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
            isCapturing = true;
        }
    }


    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        photoCaptureObject = captureObject;

        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.BGRA32;

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
        isCapturing = false;
    }


    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            string filename = string.Format(@"CapturedImage{0}_n.jpg", Time.time);
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);

            photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
            isCapturing = false;
        }
    }

    void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            Debug.Log("Saved Photo to disk!");
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }
        else
        {
            Debug.Log("Failed to save Photo to disk");
        }
        isCapturing = false;
    }

}
