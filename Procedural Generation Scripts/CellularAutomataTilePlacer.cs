using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CellularAutomataTilePlacer : MonoBehaviour
{
  
    public Tilemap targetTilemap;
    [Tooltip("The selected tile will be placed to the selected Tilemap where cellular automata is present.")]
    public TileBase tileToPlace;

    public Vector2Int size;
    public CellularAutomataRules cellularAutomataRules;

    [HideInInspector]
    public bool[] cellularAutomataValues;

    public bool showTestResults;

    public void GeneratePreview()
    {
        if(cellularAutomataRules.seed == "")
        {
            cellularAutomataValues = CellularAutomataGenerator.GenerateCellularAutomata(size, cellularAutomataRules.iterationCount, 
                cellularAutomataRules.fillPercent, cellularAutomataRules.lowerBoundRule, 
                cellularAutomataRules.upperBoundRule, cellularAutomataRules.borderRule);
        }
        else
        {
            cellularAutomataValues = CellularAutomataGenerator.GenerateCellularAutomata(size, cellularAutomataRules.seed,
                cellularAutomataRules.iterationCount, cellularAutomataRules.fillPercent, cellularAutomataRules.lowerBoundRule,
                cellularAutomataRules.upperBoundRule, cellularAutomataRules.borderRule);
        }
        
    }

    public void PlaceTiles()
    {
        if (cellularAutomataValues == null || cellularAutomataValues.Length == 0)
        {
            Debug.LogError("No valid cellular automata values.");
            return;
        }

        if(tileToPlace == null)
        {
            Debug.LogError("No tile selected for placement.");
            return;
        }

        if(targetTilemap == null)
        {
            Debug.LogError("No tilemap selected for tile placement.");
            return;
        }

        for (int i = 0; i < size.x * size.y; i++)
        {
            int x = Mathf.RoundToInt(transform.position.x) + (i % size.x) - (size.x / 2);
            int y = Mathf.RoundToInt(transform.position.y) + (i / size.x) - (size.y / 2);

            if (cellularAutomataValues[i])
            {
                targetTilemap.SetTile(targetTilemap.WorldToCell(new Vector3(x,y, targetTilemap.transform.position.z)), tileToPlace);
            }
            

        }
   
    }

    public void OnDrawGizmos()
    {
        if (!showTestResults || cellularAutomataValues == null || cellularAutomataValues.Length == 0)
        {
            return;
        }

        for (int i = 0; i < size.x * size.y; i++)
        {
            int x = Mathf.RoundToInt(transform.position.x) + (i % size.x) - (size.x / 2);
            int y = Mathf.RoundToInt(transform.position.y) + (i / size.x) - (size.y / 2);

            if (cellularAutomataValues[i])
            {
                Gizmos.color = new Color(0, 0, 0, 0.7f);
            }
            else
            {
                Gizmos.color = new Color(1, 1, 1, 0.5f);
            }

            Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0), Vector3.one * 0.9f);
        }


    }

   
}
