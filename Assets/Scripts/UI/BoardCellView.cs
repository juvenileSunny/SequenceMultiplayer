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
    [SerializeField] private Outline legalMoveOutline;
    [SerializeField] private Outline sequenceOutline;
    [SerializeField] private Image sequenceRing;

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
        SetHighlighted(false);
    }

    public void UpdateVisual()
    {
        if (boardCell == null)
            return;

        UpdateCardVisual();
        UpdateChipVisual();
        UpdateSequenceVisual();
        UpdateButtonState();
    }

    private void UpdateSequenceVisual()
    {
        if (sequenceRing == null)
            return;

        bool show =
            boardCell != null &&
            !boardCell.IsCorner &&
            boardCell.IsOccupied &&
            boardCell.IsPartOfCompletedSequence;

        sequenceRing.gameObject.SetActive(show);
    }
    public void SetHighlighted(bool highlighted)
    {
        if (legalMoveOutline != null)
        {
            legalMoveOutline.enabled =
                highlighted;
        }
    }

    // =========================================================
    // CARD VISUAL
    // =========================================================

    private void UpdateCardVisual()
    {
        if (coordinateText != null)
        {
            coordinateText.text =
                $"{boardCell.Row},{boardCell.Column}";
        }

        // -----------------------------------------------------
        // SEQUENCE CORNER
        // -----------------------------------------------------

        if (boardCell.IsCorner)
        {
            Sprite cornerSprite =
                Resources.Load<Sprite>(
                    "Cards/SEQUENCE"
                );

            if (cornerSprite != null &&
                cardImage != null)
            {
                cardImage.gameObject.SetActive(
                    true
                );

                cardImage.sprite =
                    cornerSprite;

                cardImage.preserveAspect =
                    true;
            }

            if (cardText != null)
            {
                cardText.gameObject.SetActive(
                    cornerSprite == null
                );

                cardText.text =
                    "Sequence";
            }

            return;
        }

        // -----------------------------------------------------
        // NORMAL CARD
        // -----------------------------------------------------

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
                cardImage.gameObject.SetActive(
                    true
                );

                cardImage.sprite =
                    cardSprite;

                cardImage.preserveAspect =
                    true;
            }

            if (cardText != null)
            {
                cardText.gameObject.SetActive(
                    false
                );
            }
        }
        else
        {
            Debug.LogWarning(
                $"Missing card sprite: Cards/{cardCode}"
            );

            if (cardImage != null)
            {
                cardImage.gameObject.SetActive(
                    false
                );
            }

            if (cardText != null)
            {
                cardText.gameObject.SetActive(
                    true
                );

                cardText.text =
                    cardCode;
            }
        }
    }

    // =========================================================
    // CHIP VISUAL
    // =========================================================

    private void UpdateChipVisual()
    {
        if (chipImage == null)
            return;

        // No owner = hide chip.
        if (!boardCell.IsOccupied)
        {
            chipImage.gameObject.SetActive(
                false
            );

            return;
        }

        chipImage.gameObject.SetActive(
            true
        );

        switch (boardCell.OwnerTeamId)
        {
            case 1:

                chipImage.color =
                    new Color(
                        0.9f,
                        0.05f,
                        0.05f,
                        0.65f
                    );

                break;

            case 2:

                chipImage.color =
                    new Color(
                        0.05f,
                        0.3f,
                        0.9f,
                        0.65f
                    );

                break;

            case 3:

                chipImage.color =
                    new Color(
                        0.05f,
                        0.7f,
                        0.15f,
                        0.65f
                    );

                break;

            default:

                chipImage.gameObject.SetActive(
                    false
                );

                break;
        }
    }

    // =========================================================
    // BUTTON
    // =========================================================

    private void UpdateButtonState()
    {
        if (button == null)
            return;

        /*
         * Do NOT disable occupied cells here.
         *
         * Normal cards:
         *     BoardManager rejects occupied cells.
         *
         * Two-eyed Jacks:
         *     BoardManager rejects occupied cells.
         *
         * One-eyed Jacks:
         *     BoardManager NEEDS occupied cells clickable.
         *
         * Therefore BoardManager is responsible
         * for validating each click.
         */

        button.interactable =
            !boardCell.IsCorner;
    }
}