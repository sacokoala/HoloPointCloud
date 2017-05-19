using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.VR.WSA.WebCam;
using System;

#if !UNITY_EDITOR
using System.Threading.Tasks;
using Windows.Storage;
#endif

public class PhotoCaptureRaw : MonoBehaviour, IInputClickHandler
{

    private PhotoCapture photoCaptureObject = null;

    private bool isCapturing = false;

    public GameObject PointCloudObjects;
    [SerializeField] private GameObject Prefab;
    private int Width;
    private int Height;

    List<Vector3> PointAll = null;
    List<Color> ColorAll = null;
    private int CaptureCount;

    private bool SaveFileFlag;

    // Use this for initialization
    void Start()
    {

        PointAll = new List<Vector3>();
        ColorAll = new List<Color>();
        CaptureCount = 0;

        SaveFileFlag = false;
    }

    // Update is called once per frame
    void Update()
    {

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

        Width = cameraResolution.width;
        Height = cameraResolution.height;
        //Debug.Log(String.Format("width={0}, height={1}", Width, Height));

        //captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
        // Activate the camera
        photoCaptureObject.StartPhotoModeAsync(c, delegate (PhotoCapture.PhotoCaptureResult result) {
            // Take a picture
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        });
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
        isCapturing = false;
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            List<byte> imageBufferList = new List<byte>();
            // Copy the raw IMFMediaBuffer data into our empty byte list.
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

            // In this example, we captured the image using the BGRA32 format.
            // So our stride will be 4 since we have a byte for each rgba channel.
            // The raw image data will also be flipped so we access our pixel data
            // in the reverse order.
            int stride = 4;
            float denominator = 1.0f / 255.0f;
            List<Color> colorArray = new List<Color>();
            for (int i = imageBufferList.Count - 1; i >= 0; i -= stride)
            {
                float a = (int)(imageBufferList[i - 0]) * denominator;
                float r = (int)(imageBufferList[i - 1]) * denominator;
                float g = (int)(imageBufferList[i - 2]) * denominator;
                float b = (int)(imageBufferList[i - 3]) * denominator;

                colorArray.Add(new Color(r, g, b, a));
            }
            // Now we could do something with the array such as texture.SetPixels() or run image processing on the list
            //Debug.Log(String.Format("imageBufferList={0}", imageBufferList.Count));
            CreateMesh(ref colorArray, photoCaptureFrame);

            // save ?
            if (CaptureCount % 1 == 0)
            {
                if (!SaveFileFlag)
                {
                    SaveFileFlag = true;

                    SaveFile(PointAll, ColorAll);
                    Debug.Log(String.Format("SaveFile={0}", CaptureCount));

                    //SaveFileFlag = false;
                }
            }
        }
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }


    void CreateMesh(ref List<Color> colorArray, PhotoCaptureFrame photoCaptureFrame)
    {
        // ------------------------------------------------------
        // make xyxrgb
        //
        int widthHeightRate = 8;
        int texWidth = Width / widthHeightRate;
        int texHeight = Height / widthHeightRate;
        int numPoints = texWidth * texHeight;
        List<Vector3> points = new List<Vector3>();
        List<int> indecies = new List<int>();
        List<Color> colors = new List<Color>();
        for (int y = 0; y < texHeight; ++y)
        {
            for (int x = 0; x < texWidth; ++x)
            {
                int i = y * texWidth + x;
                int i2 = y * widthHeightRate * Width + x * widthHeightRate;
                //int i2 = y * widthHeightRate * Width + (Width - (x + 1)) * widthHeightRate;
                //float param_x = 200;
                float param_x = 24.5F * widthHeightRate; // 200 ?!
                float param_y = 24.0F * widthHeightRate;
                points.Add(new Vector3(((x - 1) - (texWidth / 2)) / param_x,
                                        (y - (texHeight / 2)) / param_y, 0.0F));
                indecies.Add(i);
                colors.Add(new Color(colorArray[i2].r, colorArray[i2].g, colorArray[i2].b, colorArray[i2].a));
            }
        }


        // ------------------------------------------------------
        // pos & rot
        //
        Matrix4x4 cameraToWorldMatrix;
        photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
        Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

        Matrix4x4 projectionMatrix;
        photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix);

        // 0 right
        // 1 up
        // 2 forward
        // 3 position

        // Position the canvas object slightly in front
        // of the real world web camera.
#if UNITY_EDITOR
        Vector3 position = cameraToWorldMatrix.GetColumn(3) + cameraToWorldMatrix.GetColumn(2);
#else
        Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
#endif

        // Rotate the canvas object so that it faces the user.
        Quaternion rotation = Quaternion.LookRotation(cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));


        // ------------------------------------------------------
        // pos -> ray
        //
        for (int y = texHeight - 1; y >= 0; --y)
        {
            for (int x = texWidth - 1; x >= 0; --x)
            {
                int i = y * texWidth + x;
#if false
                Vector3 point = points[i];
#else
                // SetTRS
                Vector3 translation = position;
                //Vector3 eulerAngles;
                Vector3 scale = new Vector3(1.0F, 1.0F, 1.0F);
                Matrix4x4 m = Matrix4x4.identity;
                //Quaternion rotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);

                m.SetTRS(translation, rotation, scale);
                Vector3 point = m.MultiplyPoint3x4(points[i]);
#endif

                Vector3 front = new Vector3(point.x - cameraToWorldMatrix.GetColumn(3).x,
                                            point.y - cameraToWorldMatrix.GetColumn(3).y,
                                            point.z - cameraToWorldMatrix.GetColumn(3).z);

                RaycastHit hit;

                if (Physics.Raycast(cameraToWorldMatrix.GetColumn(3), front, out hit, 50.0F))
                {
                    points[i] = hit.point;
                }
                else
                {
                    // remove nohit point
                    points.RemoveAt(i);
                    indecies.RemoveAt(i);
                    colors.RemoveAt(i);
                }
            }
        }

        // indecies recount
        for (int i = 0; i < indecies.Count; ++i)
        {
            indecies[i] = i;
        }

        // copy for filesave
        for (int i = 0; i < indecies.Count; ++i)
        {
            PointAll.Add(points[i]);
            ColorAll.Add(colors[i]);
        }
        CaptureCount++;


        // ------------------------------------------------------
        // create gameobject
        //
        GameObject PointCloudObject = Instantiate(Prefab, new Vector3(0.0F, 0.0F, 0.0F), Quaternion.identity, PointCloudObjects.transform);
        if (PointCloudObject == null)
        {
            Debug.Log("PointCloudError.");
            return;
        }
        Mesh mesh = PointCloudObject.GetComponent<MeshFilter>().mesh;


        // ------------------------------------------------------
        // create mesh
        //
        mesh.vertices = points.ToArray();
        mesh.colors = colors.ToArray();
        mesh.SetIndices(indecies.ToArray(), MeshTopology.Points, 0);
        Debug.Log(String.Format("points={0}({1})", points.Count, texWidth * texHeight));
    }


    void SaveFile(List<Vector3> PointAll, List<Color> ColorAll)
    {
        string fileName = String.Format("pcd{0}.txt", CaptureCount);
#if UNITY_EDITOR
        string folderName = Application.persistentDataPath;

        StreamWriter sw;
        FileInfo fi;
        fi = new FileInfo(folderName + fileName);
        sw = fi.AppendText();

        sw.WriteLine(@"COFF");
        sw.WriteLine(String.Format("{0} {1} {2}", PointAll.Count, 0, 0));
        for (int i = 0; i < PointAll.Count; ++i)
        {
            sw.WriteLine(String.Format(" {0} {1} {2} {3} {4} {5} {6}",
                                       PointAll[i].x, PointAll[i].y, PointAll[i].z,
                                       ColorAll[i].r, ColorAll[i].g, ColorAll[i].b, ColorAll[i].a));
        }

        sw.Flush();
        sw.Close();
#else
        /*
        Task task = new Task(
                        async () =>
                        {
                            // Create sample file; replace if exists.
                            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                            StorageFile offFile = await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

                            List<string> PointColorList = new List<string>();
                            PointColorList.Add("COFF");
                            PointColorList.Add(String.Format("{0} {1} {2}\n", PointAll.Count, 0, 0));
                            for (int i = 0; i < PointAll.Count; ++i)
                            {
                                PointColorList.Add(String.Format(" {0} {1} {2} {3} {4} {5} {6}\n",
                                                                 -PointAll[i].x, PointAll[i].y, PointAll[i].z,
                                                                  ColorAll[i].r, ColorAll[i].g, ColorAll[i].b, ColorAll[i].a));
                            }
                            IEnumerable<string> IPointColorList = PointColorList;
                            await FileIO.WriteLinesAsync(offFile, IPointColorList);
                        });

        task.Start();
        task.Wait();
        */
        /*
        Task.Run(async () => {
            // Create sample file; replace if exists.
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile offFile = await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

            List<string> PointColorList = new List<string>();
            PointColorList.Add("COFF");
            PointColorList.Add(String.Format("{0} {1} {2}\n", PointAll.Count, 0, 0));
            for (int i = 0; i < PointAll.Count; ++i)
            {
                PointColorList.Add(String.Format(" {0} {1} {2} {3} {4} {5} {6}\n",
                                                 -PointAll[i].x, PointAll[i].y, PointAll[i].z,
                                                  ColorAll[i].r, ColorAll[i].g, ColorAll[i].b, ColorAll[i].a));
            }
            IEnumerable<string> IPointColorList = PointColorList;
            await FileIO.WriteLinesAsync(offFile, IPointColorList);
        });
        */

        StartCoroutine(SaveOffFileCoroutine(PointAll, ColorAll, fileName));

        //Task task = SaveOffFileAsync();
        //task.Wait();
#endif

        // clear list
        //PointAll.Clear();
        //ColorAll.Clear();
    }

#if !UNITY_EDITOR
    private IEnumerator SaveOffFileCoroutine(List<Vector3> PointAll, List<Color> ColorAll, string fileName)
    {
        // WaitWhile
        //textMeshCoroutine.text = "Task.Run before : " + Time.time;

        //var task = Task.Delay(5000);
        Task task = SaveOffFileAsync(PointAll, ColorAll, fileName);

        yield return new WaitWhile( () => !task.IsCompleted);

        SaveFileFlag = false;

        //textMeshCoroutine.text = "Task.Run after  : " + Time.time;
    }

    private static async Task SaveOffFileAsync(List<Vector3> PointAll, List<Color> ColorAll, string fileName)
    {
        // Create sample file; replace if exists.
        StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
        StorageFile offFile = await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

        List<string> PointColorList = new List<string>();
        PointColorList.Add("COFF");
        PointColorList.Add(String.Format("{0} {1} {2}\n", PointAll.Count, 0, 0));
        for (int i = 0; i < PointAll.Count; ++i)
        {
            PointColorList.Add(String.Format(" {0} {1} {2} {3} {4} {5} {6}\n",
                                             -PointAll[i].x, PointAll[i].y, PointAll[i].z,
                                              ColorAll[i].r, ColorAll[i].g, ColorAll[i].b, ColorAll[i].a));
        }
        IEnumerable<string> IPointColorList = PointColorList;
        await FileIO.WriteLinesAsync(offFile, IPointColorList);

    }
#endif

}
