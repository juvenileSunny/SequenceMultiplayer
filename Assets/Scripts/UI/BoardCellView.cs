using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardCellView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Image chipImage;

    [SerializeField] private TMP_Text cardText;
    [SerializeField] private TMP_Text coordinateText;

    [SerializeField] private Button button;

    private BoardCell boardCell;

    public BoardCell Cell => boardCell;

    public void Initialize(
        BoardCell cell,
        Action<BoardCellView> onClicked)
    {
        boardCell = cell;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(() =>
            {
                onClicked?.Invoke(this);
            });
        }

        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (boardCell == null)
            return;

        UpdateCardVisual();
        UpdateChipVisual();
        UpdateButtonState();
    }

    private void UpdateCardVisual()
    {
        if (coordinateText != null)
        {
            coordinateText.text =
                $"{boardCell.Row},{boardCell.Column}";
        }

        // -------------------------
        // SEQUENCE CORNER
        // -------------------------

        if (boardCell.IsCorner)
        {
            Sprite cornerSprite =
                Resources.Load<Sprite>("Cards/SEQUENCE");

            if (cornerSprite != null && cardImage != null)
            {
                cardImage.gameObject.SetActive(true);
                cardImage.sprite = cornerSprite;
                cardImage.preserveAspect = true;
            }

            if (cardText != null)
            {
                cardText.gameObject.SetActive(
                    cornerSprite == null
                );

                cardText.text = "Sequence";
            }

            return;
        }

        // -------------------------
        // NORMAL CARD
        // -------------------------

        if (boardCell.Card == null)
            return;

        string cardCode =
            boardCell.Card.GetCode();

        Sprite cardSprite =
            Resources.Load<Sprite>(
                $"Cards/{cardCode}"
            );

        if (cardSprite != null)
        {
            if (cardImage != null)
            {
                cardImage.gameObject.SetActive(true);
                cardImage.sprite = cardSprite;
                cardImage.preserveAspect = true;
            }

            if (cardText != null)
            {
                cardText.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning(
                $"Missing card sprite: Cards/{cardCode}"
            );

            if (cardImage != null)
            {
                cardImage.gameObject.SetActive(false);
            }

            if (cardText != null)
            {
                cardText.gameObject.SetActive(true);
                cardText.text = cardCode;
            }
        }
    }

    private void UpdateChipVisual()
    {
        if (chipImage == null)
            return;

        if (!boardCell.IsOccupied)
        {
            chipImage.gameObject.SetActive(false);
            return;
        }

        chipImage.gameObject.SetActive(true);

        switch (boardCell.OwnerId)
        {
            case 1:
                chipImage.color =
                    new Color(0.9f, 0.05f, 0.05f, 0.65f);
                break;

            case 2:
                chipImage.color =
                    new Color(0.05f, 0.3f, 0.9f, 0.65f);
                break;

            case 3:
                chipImage.color =
                    new Color(0.05f, 0.7f, 0.15f, 0.65f);
                break;

            default:
                chipImage.color =
                    Color.clear;
                break;
        }
    }

    private void UpdateButtonState()
    {
        if (button == null)
            return;

        // Corner spaces cannot be clicked.
        // Occupied spaces cannot be clicked again.
        button.interactable =
            !boardCell.IsCorner &&
            !boardCell.IsOccupied;
    }
}