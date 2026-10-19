using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayModeRecordingToggle : MonoBehaviour
{
    [SerializeField]
    [Tooltip("在 Unity 编辑器中进入 Play 模式时自动录制 Game 视图。请在开始 Play 前设置。")]
    private bool recordOnPlay;

    public bool RecordOnPlay => recordOnPlay;
}
