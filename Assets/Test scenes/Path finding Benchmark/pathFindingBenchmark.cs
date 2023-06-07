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

    private class TestPosition
    {
        public Vector3 startPosition;
        public Vector3 endPosition;
        public int startRotation;
        public int endRotation;

        public TestPosition(Vector3 startPosition, Vector3 endPosition, int startRotation, int endRotation)
        {
            this.startPosition = startPosition;
            this.endPosition = endPosition;
            this.startRotation = startRotation;
            this.endRotation = endRotation;
        }
    }

    private List<TestPosition> testPositions = new List<TestPosition>();
    private List<int> results = new List<int>();
    private int total = 0;
    private int notFound = 0;

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

        this.GetComponent<ObstaclesGenerator>().InitObstacles(map, startPos);

        string timeText = DisplayController.GetDisplayTimeText(startTime, Environment.TickCount, "Generate obstacles and Voronoi diagram");

        Debug.Log(timeText);


        //Create the textures showing various stuff, such as the distance to nearest obstacle
        //debugController.DisplayCellObstacleIntersection(map);
        displayController.GenerateTexture(map, DisplayController.TextureTypes.Flowfield_Obstacle);
        displayController.GenerateTexture(map, DisplayController.TextureTypes.Voronoi_Field);
        displayController.GenerateTexture(map, DisplayController.TextureTypes.Voronoi_Diagram);

        //testPositions.Add(new TestPosition(new Vector3(48f, 0f, 67f), new Vector3(5f, 0f, 14f), 0, 0));

        for(int i = 5; i < 105; i += 10)
        {
            // Right
            testPositions.Add(new TestPosition(new Vector3(35f, 0f, 25f), new Vector3(i, 0f, 14f), 90, 0));
            // Backward
            testPositions.Add(new TestPosition(new Vector3(50f, 0f, 60f), new Vector3(i, 0f, 14f), 90, 0));
            // Left
            testPositions.Add(new TestPosition(new Vector3(110f, 0f, 25f), new Vector3(i, 0f, 14f), 90, 0));
            // Forward
            testPositions.Add(new TestPosition(new Vector3(50f, 0f, 60f), new Vector3(i, 0f, 14f), 90, 0));
        }

        StartCoroutine(PositionTester());
        
    }

    private IEnumerator PositionTester()
    {
        int positionsTested = 0;

        //Test different start end positions
        foreach (TestPosition testPosition in testPositions)
        {
            yield return new WaitForSeconds(0.5f);
            Transform truckBegin = testBenchmark.current.GetSelfDrivingCarTrans();
            Transform trailerBegin = testBenchmark.current.trailerStart;
            Transform truckGoal = testBenchmark.current.GetCarMouse();
            Debug.Log(truckBegin.rotation);
            //Transform trailerGoal = testBenchmark.current.TryGetTrailerTransMouse();
            truckBegin.gameObject.SetActive(false);
            trailerBegin.gameObject.SetActive(false);
            truckBegin.position = testPosition.startPosition;
            truckBegin.Rotate(Vector3.up * testPosition.startRotation);
            Debug.Log(truckBegin.rotation);
            trailerBegin.position = testPosition.startPosition;
            trailerBegin.Rotate(Vector3.up * testPosition.startRotation);
            truckBegin.gameObject.SetActive(true);
            trailerBegin.gameObject.SetActive(true);
            truckGoal.position = testPosition.endPosition;

            //The self-driving truck
            Car truckStart = new Car(testBenchmark.current.GetSelfDrivingCarTrans(), testBenchmark.current.GetActiveCarData());
            Car truckEnd = new Car(truckGoal, testBenchmark.current.GetActiveCarData());
            Car trailerStart = new Car(testBenchmark.current.trailerStart, testBenchmark.current.TryGetTrailerData());
            Car trailerEnd = new Car(testBenchmark.current.TryGetTrailerTransMouse(), testBenchmark.current.TryGetTrailerData());

            displayController.ResetGUI();

            yield return StartCoroutine(GeneratePathBench(truckStart, trailerStart, truckEnd, trailerEnd));
            positionsTested += 1;
        }

        Debug.Log($"Tested {positionsTested} positions");
        Debug.Log($"Expanded nodes:");
        /*foreach (int value in results)
        {
            Debug.Log(value);
        }*/
        Debug.Log($"Total expanded nodes: {total}");
        Debug.Log($"Path not found: {notFound}");
        Debug.Log("Done with benchmark");
    }

    //Generate a path and send it to the car
    //We have to do it over some time to avoid a sudden stop in the simulation
    IEnumerator GeneratePathBench(Car startTruck, Car startTrailer, Car endTruck, Car endTrailer)
    {
        //First we have to check if the self-driving car is inside of the grid
        if (!map.IsPosWithinGrid(startTruck.rearWheelPos))
        {
            Debug.Log("The car is outside of the grid");

            yield break;
        }

        //Which cell do we want to reach? We have already checked that this cell is valid
        IntVector2 targetCell = map.ConvertWorldToCell(endTruck.rearWheelPos);

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
        List<Node> finalPath = HybridAStar.GeneratePath(startTruck, endTruck, map, expandedNodes, startTrailer, endTrailer);

        timeText += DisplayController.GetDisplayTimeText(startTime, Environment.TickCount, "Hybrid A Star");

        if (finalPath == null || finalPath.Count == 0)
        {
            //UIController.current.SetFoundPathText("Failed to find a path!");
            Debug.Log("Failed to find a path!");
            notFound += 1;
        }
        else
        {
            //UIController.current.SetFoundPathText("Found a path!");
            Debug.Log("Found a path!");
            total += expandedNodes.Count;
        }


        //
        // Display the results
        //

        results.Add(expandedNodes.Count);

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
