using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using PathfindingForVehicles;
using SelfDrivingVehicle;

public class pathFindingBenchmark : MonoBehaviour
{
    public static pathFindingBenchmark current;

    //Map data
    public Map map;

    //External scripts
    private DisplayController displayController;



    void Awake()
    {
        current = this;

        displayController = GetComponent<DisplayController>();
    }

    void Start()
    {
        //Create the map with cell data we need
        map = new Map(Parameters.mapWidth, Parameters.cellWidth);

        //Generate obstacles
        //Has to do it from this script or the obstacles might be created after this script has finished and mess things up
        //Need the start car so we can avoid adding obstacles on it
        //Car startCar = new Car(SimController.current.GetSelfDrivingCarTrans(), SimController.current.GetActiveCarData());
        Vector3 startPos = testBenchmark.current.GetSelfDrivingCarTrans().position;

        int startTime = Environment.TickCount;

        //this.GetComponent<ObstaclesGenerator>().InitObstacles(map, startPos);

        string timeText = DisplayController.GetDisplayTimeText(startTime, Environment.TickCount, "Generate obstacles and Voronoi diagram");

        Debug.Log(timeText);


        //Create the textures showing various stuff, such as the distance to nearest obstacle
        //debugController.DisplayCellObstacleIntersection(map);
        displayController.GenerateTexture(map, DisplayController.TextureTypes.Flowfield_Obstacle);
        displayController.GenerateTexture(map, DisplayController.TextureTypes.Voronoi_Field);
        displayController.GenerateTexture(map, DisplayController.TextureTypes.Voronoi_Diagram);

        Car truckTrailerTarget = new Car(testBenchmark.current.GetCarMouse(), testBenchmark.current.GetActiveCarData());

        StartCoroutine(GeneratePath(truckTrailerTarget));
        Debug.Log("Done with benchmark");

    }

    //Generate a path and send it to the car
    //We have to do it over some time to avoid a sudden stop in the simulation
    IEnumerator GeneratePath(Car goalCar)
    {
        //Get the start positions    

        //The self-driving truck
        Car startCar = new Car(testBenchmark.current.GetSelfDrivingCarTrans(), testBenchmark.current.GetActiveCarData());

        //The trailer
        Car startTrailer = new Car(testBenchmark.current.trailerStart, testBenchmark.current.TryGetTrailerData());
        Car endTrailer = new Car(testBenchmark.current.TryGetTrailerTransMouse(), testBenchmark.current.TryGetTrailerData());

        //First we have to check if the self-driving car is inside of the grid
        if (!map.IsPosWithinGrid(startCar.rearWheelPos))
        {
            Debug.Log("The car is outside of the grid");

            yield break;
        }

        //Which cell do we want to reach? We have already checked that this cell is valid
        IntVector2 targetCell = map.ConvertWorldToCell(goalCar.rearWheelPos);

        //To measure time, is measured in tick counts
        int startTime = 0;
        //To display how long time each part took
        string timeText = "";


        //
        // Calculate Heuristics
        //

        //Calculate euclidean distance heuristic
        startTime = Environment.TickCount;

        HeuristicsController.EuclideanDistance(map, targetCell);

        timeText += DisplayController.GetDisplayTimeText(startTime, Environment.TickCount, "Euclidean Distance");

        yield return new WaitForSeconds(0.05f);


        //Calculate dynamic programing = flow field
        startTime = Environment.TickCount;

        HeuristicsController.DynamicProgramming(map, targetCell);

        timeText += DisplayController.GetDisplayTimeText(startTime, Environment.TickCount, "Dynamic Programming");

        yield return new WaitForSeconds(0.05f);


        //Calculate the final heuristics
        HeuristicsController.GenerateFinalHeuristics(map);


        //
        // Generate the shortest path with Hybrid A*
        //

        //List with all expanded nodes for debugging, so we can display the search tree
        List<Node> expandedNodes = new List<Node>();

        startTime = Environment.TickCount;

        //The output is finalPath and expandedNodes
        List<Node> finalPath = HybridAStar.GeneratePath(startCar, goalCar, map, expandedNodes, startTrailer, endTrailer);

        timeText += DisplayController.GetDisplayTimeText(startTime, Environment.TickCount, "Hybrid A Star");

        if (finalPath == null || finalPath.Count == 0)
        {
            //UIController.current.SetFoundPathText("Failed to find a path!");
            Debug.Log("Failed to find a path!");
        }
        else
        {
            //UIController.current.SetFoundPathText("Found a path!");
            Debug.Log("Found a path!");
        }


        //
        // Display the results
        //

        //Display how long time the different parts took
        Debug.Log(timeText);

        //Reset display
        displayController.ResetGUI();

        //Always display the search tree even if we havent found a path to the goal
        displayController.DisplaySearchTree(expandedNodes);

        //Generate the flow field heuristic texture
        displayController.GenerateTexture(map, DisplayController.TextureTypes.Flowfield_Target);

        //Display the different paths
        displayController.DisplayFinalPath(finalPath, null);

        yield return null;
    }

}
