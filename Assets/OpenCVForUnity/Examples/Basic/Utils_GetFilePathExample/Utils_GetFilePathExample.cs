using OpenCVForUnity.UnityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Utils_GetFilePath Example
    /// An example of how to get the readable path of a file in the "StreamingAssets" folder using the Utils class.
    /// </summary>
    public class Utils_GetFilePathExample : MonoBehaviour
    {
        /// <summary>
        /// The file path dropdown.
        /// </summary>
        public Dropdown filePathDropdown;

        /// <summary>
        /// The refresh toggle.
        /// </summary>
        public Toggle refreshToggle;

        /// <summary>
        /// The timeout dropdown.
        /// </summary>
        public Dropdown timeoutDropdown;

        /// <summary>
        /// The get file path button.
        /// </summary>
        public Button getFilePathButton;

        /// <summary>
        /// The get multiple file paths button.
        /// </summary>
        public Button getMultipleFilePathsButton;

        /// <summary>
        /// The get file path async button.
        /// </summary>
        public Button getFilePathAsyncButton;

        /// <summary>
        /// The get multiple file paths async button.
        /// </summary>
        public Button getMultipleFilePathsAsyncButton;

        /// <summary>
        /// The get file path async task button.
        /// </summary>
        public Button getFilePathAsyncTaskButton;

        /// <summary>
        /// The get multiple file paths async task button.
        /// </summary>
        public Button getMultipleFilePathsAsyncTaskButton;

        /// <summary>
        /// The abort button.
        /// </summary>
        public Button abortButton;

        /// <summary>
        /// The file path input field.
        /// </summary>
        public Text filePathInputField;

        string[] filePathPreset = new string[] {
            "OpenCVForUnity/768x576_mjpeg.mjpeg",
            "/OpenCVForUnity/objdetect/lbpcascade_frontalface.xml",
            "OpenCVForUnity/objdetect/calibration_images/left01.jpg",
            "xxxxxxx.xxx"
        };

        IEnumerator getFilePath_Coroutine;

        CancellationTokenSource cancellationTokenSource = default;

        // Use this for initialization
        void Start()
        {
            abortButton.interactable = false;
        }

        private void GetFilePath(string filePath, bool refresh, int timeout)
        {
            var readableFilePath = Utils.getFilePath(filePath, refresh, timeout);

#if UNITY_WEBGL
            Debug.Log("The Utils.getFilePath() method is not supported on WebGL platform.");
            filePathInputField.text = filePathInputField.text + "The Utils.getFilePath() method is not supported on WebGL platform." + "\n";
            if (!string.IsNullOrEmpty(readableFilePath))
            {
                Debug.Log("completed: " + "readableFilePath=" + readableFilePath);
                filePathInputField.text = filePathInputField.text + "completed: " + "readableFilePath=" + readableFilePath;
            }
#else
            if (string.IsNullOrEmpty(readableFilePath))
            {
                Debug.LogWarning("# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
                filePathInputField.text = filePathInputField.text + "# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder." + "\n";
            }
            else
            {
                Debug.Log("# completed: " + "readableFilePath= " + readableFilePath);
                filePathInputField.text = filePathInputField.text + "# completed: " + "readableFilePath= " + readableFilePath + "\n";
            }
#endif
        }

        private void GetMultipleFilePaths(string[] filePaths, bool refresh, int timeout)
        {
            var readableFilePaths = Utils.getMultipleFilePaths(filePaths, refresh, timeout);

#if UNITY_WEBGL
            Debug.Log("The Utils.getFilePath() method is not supported on WebGL platform.");
            filePathInputField.text = filePathInputField.text + "The Utils.getMultipleFilePaths() method is not supported on WebGL platform." + "\n";
            for (int i = 0; i < readableFilePaths.Count; i++)
            {
                if (!string.IsNullOrEmpty(readableFilePaths[i]))
                {
                    Debug.Log("readableFilePath[" + i + "]=" + readableFilePaths[i]);
                    filePathInputField.text = filePathInputField.text + "readableFilePath[" + i + "]=" + readableFilePaths[i];
                }
            }
#else
            Debug.Log("### allCompleted:" + "\n");
            filePathInputField.text = filePathInputField.text + "### allCompleted:" + "\n";
            for (int i = 0; i < readableFilePaths.Count; i++)
            {
                if (string.IsNullOrEmpty(readableFilePaths[i]))
                {
                    Debug.LogWarning("readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
                    filePathInputField.text = filePathInputField.text + "readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder." + "\n";
                }
                else
                {
                    Debug.Log("readableFilePath[" + i + "]= " + readableFilePaths[i]);
                    filePathInputField.text = filePathInputField.text + "readableFilePath[" + i + "]= " + readableFilePaths[i] + "\n";
                }
            }
#endif
        }

        private void GetFilePathAsync(string filePath, bool refresh, int timeout)
        {
            HideButton();

            getFilePath_Coroutine = Utils.getFilePathAsync(
                filePath,
                (result) =>
                { // completed callback
                    getFilePath_Coroutine = null;
                    ShowButton();


                    string readableFilePath = result;

                    if (string.IsNullOrEmpty(readableFilePath))
                    {
                        Debug.LogWarning("# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
                        filePathInputField.text = filePathInputField.text + "# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder." + "\n";
                    }

                    Debug.Log("# completed: " + "readableFilePath= " + readableFilePath);
                    filePathInputField.text = filePathInputField.text + "# completed: " + "readableFilePath= " + readableFilePath + "\n";

                },
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress);
                    filePathInputField.text = filePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    getFilePath_Coroutine = null;
                    ShowButton();

                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode);
                    filePathInputField.text = filePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout);

            StartCoroutine(getFilePath_Coroutine);
        }

        private void GetMultipleFilePathsAsync(string[] filePaths, bool refresh, int timeout)
        {
            HideButton();

            getFilePath_Coroutine = Utils.getMultipleFilePathsAsync(
                filePaths,
                (result) =>
                { // allCompleted callback
                    getFilePath_Coroutine = null;
                    ShowButton();

                    var readableFilePaths = result;

                    Debug.Log("### allCompleted:" + "\n");
                    filePathInputField.text = filePathInputField.text + "### allCompleted:" + "\n";
                    for (int i = 0; i < readableFilePaths.Count; i++)
                    {
                        if (string.IsNullOrEmpty(readableFilePaths[i]))
                        {
                            Debug.LogWarning("readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
                            filePathInputField.text = filePathInputField.text + "readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder." + "\n";
                        }
                        else
                        {
                            Debug.Log("readableFilePath[" + i + "]= " + readableFilePaths[i]);
                            filePathInputField.text = filePathInputField.text + "readableFilePath[" + i + "]= " + readableFilePaths[i] + "\n";
                        }
                    }

                },
                (path) =>
                { // completed callback
                    Debug.Log("# completed: " + "path= " + path);
                    filePathInputField.text = filePathInputField.text + "# vcompleted: " + "path= " + path + "\n";

                },
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress);
                    filePathInputField.text = filePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode);
                    filePathInputField.text = filePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout);

            StartCoroutine(getFilePath_Coroutine);
        }

        private async Task GetFilePathAsyncTask(string filePath, bool refresh, int timeout, CancellationToken cancellationToken = default)
        {
            HideButton();

            var result = await Utils.getFilePathAsyncTask(
                filePath,
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress);
                    filePathInputField.text = filePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    getFilePath_Coroutine = null;
                    ShowButton();

                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode);
                    filePathInputField.text = filePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout, cancellationToken);


            ShowButton();

            string readableFilePath = result;

            if (string.IsNullOrEmpty(readableFilePath))
            {
                Debug.LogWarning("# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
                filePathInputField.text = filePathInputField.text + "# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder." + "\n";
            }

            Debug.Log("# completed: " + "readableFilePath= " + readableFilePath);
            filePathInputField.text = filePathInputField.text + "# completed: " + "readableFilePath= " + readableFilePath + "\n";
        }

        private async Task GetMultipleFilePathsAsyncTask(string[] filePaths, bool refresh, int timeout, CancellationToken cancellationToken = default)
        {
            HideButton();

            var result = await Utils.getMultipleFilePathsAsyncTask(
                filePaths,
                (path) =>
                { // completed callback
                    Debug.Log("# completed: " + "path= " + path);
                    filePathInputField.text = filePathInputField.text + "# vcompleted: " + "path= " + path + "\n";

                },
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress);
                    filePathInputField.text = filePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode);
                    filePathInputField.text = filePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout, cancellationToken);


            ShowButton();

            var readableFilePaths = result;

            Debug.Log("### allCompleted:" + "\n");
            filePathInputField.text = filePathInputField.text + "### allCompleted:" + "\n";
            for (int i = 0; i < readableFilePaths.Count; i++)
            {
                if (string.IsNullOrEmpty(readableFilePaths[i]))
                {
                    Debug.LogWarning("readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
                    filePathInputField.text = filePathInputField.text + "readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder." + "\n";
                }
                else
                {
                    Debug.Log("readableFilePath[" + i + "]= " + readableFilePaths[i]);
                    filePathInputField.text = filePathInputField.text + "readableFilePath[" + i + "]= " + readableFilePaths[i] + "\n";
                }
            }
        }

        private void ShowButton()
        {
            getFilePathButton.interactable = true;
            getMultipleFilePathsButton.interactable = true;
            getFilePathAsyncButton.interactable = true;
            getMultipleFilePathsAsyncButton.interactable = true;
            getFilePathAsyncTaskButton.interactable = true;
            getMultipleFilePathsAsyncTaskButton.interactable = true;
            abortButton.interactable = false;
        }

        private void HideButton()
        {
            getFilePathButton.interactable = false;
            getMultipleFilePathsButton.interactable = false;
            getFilePathAsyncButton.interactable = false;
            getMultipleFilePathsAsyncButton.interactable = false;
            getFilePathAsyncTaskButton.interactable = false;
            getMultipleFilePathsAsyncTaskButton.interactable = false;
            abortButton.interactable = true;
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
            }

            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the get file path button click event.
        /// </summary>
        public void OnGetFilePathButtonClick()
        {
            bool refresh = refreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[timeoutDropdown.value], true);

            filePathInputField.text = "";

            GetFilePath(filePathPreset[filePathDropdown.value], refresh, timeout);
        }

        /// <summary>
        /// Raises the get multiple file paths button click event.
        /// </summary>
        public void OnGetMultipleFilePathsButtonClick()
        {
            bool refresh = refreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[timeoutDropdown.value], true);

            filePathInputField.text = "";

            GetMultipleFilePaths(filePathPreset, refresh, timeout);
        }

        /// <summary>
        /// Raises the get file path async button click event.
        /// </summary>
        public void OnGetFilePathAsyncButtonClick()
        {
            bool refresh = refreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[timeoutDropdown.value], true);

            filePathInputField.text = "";

            GetFilePathAsync(filePathPreset[filePathDropdown.value], refresh, timeout);
        }

        /// <summary>
        /// Raises the get multiple file paths async button click event.
        /// </summary>
        public void OnGetMultipleFilePathsAsyncButtonClick()
        {
            bool refresh = refreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[timeoutDropdown.value], true);

            filePathInputField.text = "";

            GetMultipleFilePathsAsync(filePathPreset, refresh, timeout);
        }

        /// <summary>
        /// Raises the get file path async task button click event.
        /// </summary>
        public async void OnGetFilePathAsyncTaskButtonClick()
        {
            bool refresh = refreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[timeoutDropdown.value], true);

            filePathInputField.text = "";

            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await GetFilePathAsyncTask(filePathPreset[filePathDropdown.value], refresh, timeout, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("# canceled: " + "The task was canceled externally. OperationCanceledException");
                filePathInputField.text = filePathInputField.text + "# canceled: " + "The task was canceled externally. OperationCanceledException" + "\n";
            }
        }

        /// <summary>
        /// Raises the get multiple file paths async task button click event.
        /// </summary>
        public async void OnGetMultipleFilePathsAsyncTaskButtonClick()
        {
            bool refresh = refreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[timeoutDropdown.value], true);

            filePathInputField.text = "";

            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await GetMultipleFilePathsAsyncTask(filePathPreset, refresh, timeout, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("# canceled: " + "The task was canceled externally. OperationCanceledException");
                filePathInputField.text = filePathInputField.text + "# canceled: " + "The task was canceled externally. OperationCanceledException" + "\n";
            }
        }

        /// <summary>
        /// Raises the abort button click event.
        /// </summary>
        public void OnAbortButtonClick()
        {
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();

                Debug.Log("# canceled: " + "The getFilePath_Coroutine was stoped externally.");
                filePathInputField.text = filePathInputField.text + "# canceled: " + "The getFilePath_Coroutine was stoped externally." + "\n";
            }

            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }

            ShowButton();
        }

        /// <summary>
        /// Raises the on scroll rect value changed event.
        /// </summary>
        public void OnScrollRectValueChanged()
        {
            if (filePathInputField.text.Length > 10000)
            {
                filePathInputField.text = filePathInputField.text.Substring(filePathInputField.text.Length - 10000);
            }
        }

        public enum TimeoutPreset : int
        {
            _0 = 0,
            _1 = 1,
            _10 = 10,
        }
    }
}