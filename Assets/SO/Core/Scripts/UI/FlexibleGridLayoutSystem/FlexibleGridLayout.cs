using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlexibleGridLayout : LayoutGroup
{
    public enum FitType
    {
        Uniform,
        Width,
        Height,
        FixedRows,
        FixedColumns,
        FixedRowsElastic,
        FixedColumnsElastic

    }

    public FitType fitType;

    public int rows;
    public int columns;

    public Vector2 cellSize;
    public Vector2 spacing;

    public bool fitX = false;
    public bool fitY = false;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        if (fitType == FitType.Uniform
            || fitType == FitType.Width
            || fitType == FitType.Height)
        {
            fitX = true;
            fitY = true;

            float sqrRt = Mathf.Sqrt(transform.childCount);
            rows = Mathf.CeilToInt(sqrRt);
            columns = Mathf.CeilToInt(sqrRt);
        }

        if(fitType == FitType.Width
            || fitType == FitType.FixedColumns
            || fitType == FitType.FixedColumnsElastic)
        {

            rows = Mathf.CeilToInt(transform.childCount / (float)columns);
        }
        if (fitType == FitType.Height
            || fitType == FitType.FixedRows
            || fitType == FitType.FixedRowsElastic)
        {
            columns = Mathf.CeilToInt(transform.childCount / (float)rows);
        }

        float parentWidth = rectTransform.rect.width;
        float parentHeight = rectTransform.rect.height;

        //Странно работает с чётным числом столбцов
        float cellWidth = (parentWidth / (float)columns) 
            - ((spacing.x / (float)columns)) 
            - (padding.left / (float)columns)
            - (padding.right / (float)columns);
        float cellHeight = (parentHeight / (float)rows) 
            - ((spacing.y / (float)rows) * 2)
            - (padding.top / (float)rows)
            - (padding.bottom / (float)rows);

        cellSize.x = fitX ? cellWidth : cellSize.x;
        cellSize.y = fitY ? cellHeight : cellSize.y;

        int columnsCount = 0;
        int rowsCount = 0;

        for(int a = 0; a < rectChildren.Count; a++)
        {
            rowsCount = a / columns;
            columnsCount = a % columns;

            var item = rectChildren[a];

            var xPos = (cellSize.x * columnsCount) 
                + (spacing.x * columnsCount)
                + padding.left;
            var yPos = (cellSize.y * rowsCount) 
                + (spacing.y * rowsCount)
                + padding.top;

            SetChildAlongAxis(item, 0, xPos, cellSize.x);
            SetChildAlongAxis(item, 1, yPos, cellSize.y);
        }

        if (fitType == FitType.FixedRowsElastic)
        {
            float requiredWidth = ((columnsCount + 1) * cellSize.x)
                + (spacing.x * columnsCount)
                + padding.left
                + padding.right;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth);

            /*float requiredHeight = ((rowsCount + 1) * cellSize.y)
                + (spacing.y * rowsCount)
                + padding.top
                + padding.bottom;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);*/
        }

        if (fitType == FitType.FixedColumnsElastic)
        {
            /*float requiredWidth = ((columnsCount + 1) * cellSize.x)
                + (spacing.x * columnsCount)
                + padding.left
                + padding.right;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth);*/

            float requiredHeight = ((rowsCount + 1) * cellSize.y)
                + (spacing.y * rowsCount)
                + padding.top
                + padding.bottom;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);
        }
    }

    public override void CalculateLayoutInputVertical()
    {

    }

    public override void SetLayoutHorizontal()
    {

    }

    public override void SetLayoutVertical()
    {

    }
}
