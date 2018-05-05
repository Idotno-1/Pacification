﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HexGameUI : MonoBehaviour
{
    public HexGrid hexGrid;
    HexMapCamera mapCamera;
    ControlsManager controls;

    HexCell currentCell;
    HexCell attackTargetCell;
    public Unit selectedUnit;
    public City selectedCity;

    public GameObject cityUI;
    public GameObject unitUI;
    public GameObject cityAndUnitUI;
    public GameObject ennemiCityUI;

    public Text unitTypeText;
    public Text unitTypeBothText;

    public RectTransform healthUnit;
    public RectTransform healthCity;
    public RectTransform healthUnitBoth;
    public RectTransform healthEnnemiCity;

    bool didPathfinding;

    Client client;

    void Start()
    {
        // Editor should not have the game UI enabled
        if(GameManager.Instance.gamemode == GameManager.Gamemode.EDITOR)
        {
            gameObject.SetActive(false);
            return;
        }

        hexGrid = FindObjectOfType<HexGrid>();
        client = FindObjectOfType<Client>();
        mapCamera = FindObjectOfType<HexMapCamera>();
        controls = FindObjectOfType<ControlsManager>();
    }

    void Update()
    {
        if(!client.player.canPlay || EventSystem.current.IsPointerOverGameObject())
            return;

        if(attackTargetCell)
        {
            attackTargetCell.DisableHighlight();
            attackTargetCell = null;
        }

        if(Input.GetKeyDown(controls.cycleCity))
            mapCamera.CycleBetweenCities();
        else if(Input.GetKeyDown(controls.cycleUnit))
            mapCamera.CycleBetweenUnits();

        if(Input.GetMouseButtonDown(0))
            DoSelection();
        
        if(selectedUnit != null)
        {
            if(Input.GetMouseButton(1))
                DoPathfinding();
            else if(Input.GetMouseButtonUp(1))
            {
                DoMove();
                cityAndUnitUI.SetActive(false);
                unitUI.SetActive(false);
                cityUI.SetActive(false);
                ennemiCityUI.SetActive(false);
            }
            else
                DoAction();
            // After pathfinding clearing
            if(selectedUnit != null && !selectedUnit.HexUnit.location.IsHighlighted())
                selectedUnit.HexUnit.location.EnableHighlight(Color.blue);
        }
    }

    HexCell GetCellUnderCursor()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        return hexGrid.GetCell(inputRay);
    }

    City GetSelectCity(HexCell location)
    {
        City city = client.player.GetCity(location);
        if(city != null)
        {
            cityUI.SetActive(true);
            healthCity.sizeDelta = new Vector2(((float)city.Hp / (float)city.maxHP) * 90f, healthCity.sizeDelta.y);
        }
        else if(location.HasCity)
        {
            City ennemiCity = (City)location.Feature;
            ennemiCityUI.SetActive(true);
            healthEnnemiCity.sizeDelta = new Vector2(((float)ennemiCity.Hp / (float)ennemiCity.maxHP) * 90f, healthEnnemiCity.sizeDelta.y);
        }
        return city;
    }

    Unit GetSelectUnit(HexCell location)
    {
        Unit unit = client.player.GetUnit(currentCell);
        if(unit != null)
        {
            unitUI.SetActive(true);
            unitTypeText.text = unit.TypeToStr();
            healthUnit.sizeDelta = new Vector2(((float)unit.Hp / (float)unit.maxHP) * 90f, healthUnit.sizeDelta.y);
        }
        else if(location.Unit != null)
        {
            Unit ennemiUnit = location.Unit.Unit;
            unitUI.SetActive(true);
            unitTypeText.text = "Ennemi " + ennemiUnit.TypeToStr();
            healthUnit.sizeDelta = new Vector2(((float)ennemiUnit.Hp / (float)ennemiUnit.maxHP) * 90f, healthUnit.sizeDelta.y);
        }

        return unit;
    }

    bool UpdateCurrentCell()
    {
        HexCell cell = GetCellUnderCursor();
        if(cell != currentCell)
        {
            if(currentCell)
                currentCell.DisableHighlight();
            currentCell = cell;
            if(currentCell && currentCell.IsExplored)
                currentCell.EnableHighlight(Color.blue);
            return true;
        }
        return false;
    }

    void DoSelection()
    {
        UpdateCurrentCell();
        if(selectedUnit != null)
            selectedUnit.HexUnit.location.DisableHighlight();
        selectedUnit = null;
        selectedCity = null;

        cityUI.SetActive(false);
        unitUI.SetActive(false);
        cityAndUnitUI.SetActive(false);
        ennemiCityUI.SetActive(false);

        if(currentCell)
        {
            if(currentCell.Unit && currentCell.HasCity)
            {
                selectedUnit = GetSelectUnit(currentCell);
                unitUI.SetActive(false);
                selectedCity = GetSelectCity(currentCell);

                StartCoroutine(mapCamera.FocusSmoothTransition(currentCell.Position));
                cityAndUnitUI.SetActive(true);


                if(selectedUnit != null && selectedCity != null)
                {
                    cityUI.SetActive(true);
                    healthCity.sizeDelta = new Vector2(((float)selectedCity.Hp / (float)selectedCity.maxHP) * 90f, healthCity.sizeDelta.y);
                }
                else
                {
                    ennemiCityUI.SetActive(true);
                    selectedCity = (City)currentCell.Feature;
                    selectedUnit = currentCell.Unit.Unit;
                    healthEnnemiCity.sizeDelta = new Vector2(((float)selectedCity.Hp / (float)selectedCity.maxHP) * 90f, healthEnnemiCity.sizeDelta.y);
                }

                unitTypeBothText.text = selectedUnit.TypeToStr();
                healthUnitBoth.sizeDelta = new Vector2(((float)selectedUnit.Hp / (float)selectedUnit.maxHP) * 90f, healthUnitBoth.sizeDelta.y);

            }
            else if(currentCell.Unit)
            {
                selectedUnit = GetSelectUnit(currentCell);
                if(selectedUnit != null)
                    StartCoroutine(mapCamera.FocusSmoothTransition(currentCell.Position));
            }
            else if(currentCell.HasCity)
            {
                selectedCity = GetSelectCity(currentCell);
                if(selectedCity != null)
                    StartCoroutine(mapCamera.FocusSmoothTransition(currentCell.Position));
            }
        }
    }

    void DoAction()
    {
        if(Input.GetKeyDown(controls.unitPrimaryAction))
        {
            if(selectedUnit.Type == Unit.UnitType.SETTLER)
            {
                ((Settler)selectedUnit).Settle();
                selectedUnit = null;
                currentCell = null;
            }
            else if(selectedUnit.Type == Unit.UnitType.WORKER)
            {
                // TODO: need to implement exploit
                ((Worker)selectedUnit).Exploit();
            }
            else
            {
                attackTargetCell = GetCellUnderCursor();

                Attacker attacker = (Attacker)selectedUnit;
                if(!attackTargetCell || !(attacker.IsInRangeToAttack(attackTargetCell)))
                    return;

                if(attackTargetCell.Unit && selectedUnit.owner != attackTargetCell.Unit.Unit.owner)
                {
                    attackTargetCell.EnableHighlight(Color.red);
                    attacker.Attack(attackTargetCell.Unit.Unit);
                }
                else if(attackTargetCell.HasCity && selectedUnit.owner != attackTargetCell.Feature.owner)
                {
                    attackTargetCell.EnableHighlight(Color.red);
                    attacker.Attack((City)attackTargetCell.Feature);
                }
            }
        }
        else if(Input.GetKeyDown(controls.unitSecondaryAction))
        {
            if(selectedUnit.Type == Unit.UnitType.WORKER)
            {
                HexCell roadCell = GetCellUnderCursor();
                if(!roadCell || roadCell.IsUnderWater || !roadCell.IsExplored || roadCell.Unit)
                    return;
                bool roadOk = ((Worker)selectedUnit).AddRoad(roadCell);
                if(roadOk)
                    currentCell = roadCell;
            }
        }   
    }

    void DoPathfinding()
    {
        if(UpdateCurrentCell())
        {
            didPathfinding = true;
            if(currentCell)
                hexGrid.FindPath(selectedUnit.HexUnit.location, currentCell, selectedUnit.HexUnit);
            else
                hexGrid.ClearPath();
        }
    }

    void DoMove()
    {
        if(!didPathfinding)
            return;
        if(didPathfinding && !hexGrid.HasPath)
        {
            currentCell = null;
            didPathfinding = false;
            hexGrid.ClearPath();
            return;
        }

        client.player.MoveUnit(selectedUnit, currentCell);
    }

    public void NetworkRoad(string data)
    {
        string[] receivedData = data.Split('#');

        int x = int.Parse(receivedData[0]);
        int z = int.Parse(receivedData[1]);

        HexCell cell = hexGrid.GetCell(new HexCoordinates(x, z));

        if(receivedData[2] == "1")
            cell.SetRoad(int.Parse(receivedData[3]), true);
        else
            cell.NetworkRemoveRoad();
    }
}