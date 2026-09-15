using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomCodeUI : MonoBehaviour
{
    [Header("Session")]
    [SerializeField]
    private RoomSessionContext roomSessionContext;

    [Header("UI")]
    [SerializeField]
    private TMP_Text roomCodeText;

    [SerializeField]
    private Button copyButton;

    [SerializeField]
    private TMP_Text copyButtonText;

    private void OnEnable()
    {
        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged +=
                Refresh;
        }

        if (copyButton != null)
        {
            copyButton.onClick.AddListener(
                CopyRoomCode
            );
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged -=
                Refresh;
        }

        if (copyButton != null)
        {
            copyButton.onClick.RemoveListener(
                CopyRoomCode
            );
        }
    }

    public void Refresh()
    {
        if (roomCodeText == null)
            return;

        if (roomSessionContext == null ||
            !roomSessionContext.HasRoom)
        {
            roomCodeText.text =
                "ROOM: ------";

            return;
        }

        roomCodeText.text =
            $"ROOM: {roomSessionContext.RoomCode}";
    }

    private void CopyRoomCode()
    {
        if (roomSessionContext == null ||
            !roomSessionContext.HasRoom)
        {
            return;
        }

        GUIUtility.systemCopyBuffer =
            roomSessionContext.RoomCode;

        if (copyButtonText != null)
        {
            copyButtonText.text =
                "COPIED";
        }

        Debug.Log(
            $"Copied room code " +
            $"{roomSessionContext.RoomCode}."
        );
    }
}
