using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;

[InitializeOnLoad]
internal static class GameViewAutoRecorder
{
    private static RecorderController controller;

    static GameViewAutoRecorder()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            var toggle = UnityEngine.Object.FindObjectOfType<PlayModeRecordingToggle>();
            if (toggle != null && toggle.RecordOnPlay)
                StartRecording();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            StopRecording();
        }
    }

    private static void StartRecording()
    {
        if (controller != null && controller.IsRecording())
            return;

        var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
        var outputDirectory = Path.Combine(projectDirectory, "Record Videos");
        Directory.CreateDirectory(outputDirectory);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 30;

        var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieSettings.name = "Poker-DZ Game View";
        movieSettings.Enabled = true;
        movieSettings.CaptureAudio = true;
        movieSettings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080
        };
#pragma warning disable CS0618 // Unity Recorder 4.0.1 supports this MP4 setting.
        movieSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
#pragma warning restore CS0618
        var fileName = "Poker-DZ_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        movieSettings.OutputFile = Path.Combine(outputDirectory, fileName).Replace('\\', '/');

        controllerSettings.AddRecorderSettings(movieSettings);

        try
        {
            controller = new RecorderController(controllerSettings);
            controller.PrepareRecording();
            if (!controller.StartRecording())
            {
                Debug.LogError("Game 视图录制启动失败，请查看 Unity Console 中的 Recorder 错误信息。");
                controller = null;
                return;
            }

            Debug.Log("Game 视图开始录制，保存目录：" + outputDirectory);
        }
        catch (Exception exception)
        {
            controller = null;
            Debug.LogException(exception);
        }
    }

    private static void StopRecording()
    {
        if (controller == null)
            return;

        try
        {
            if (controller.IsRecording())
                controller.StopRecording();
            Debug.Log("Game 视图录制已结束，视频保存在 Record Videos 文件夹。");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            controller = null;
        }
    }
}
