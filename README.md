# Self Parking Truck Trailer

## Implementation of an Anti-Jackknife Controller to Enhance Motion Planning for Truck-Trailers.

The development of automated truck trailers has the potential to improve transportation efficiency at loading docks and 
warehouses. One crucial aspect of an automated truck trailer is the motion planning system, which generates a feasible 
path for the vehicle to follow. However, when a path requires the truck trailer to move in reverse, there is a 
significant risk of the trailer folding inwards and hitting the truck, a phenomenon known as jackknifing. This paper 
describes a method to mitigate this issue by implementing an MPC to counteract jackknifing. This enables robust backward
driving of a truck-trailer combination, as well as a more flexible path planning algorithm for use in a warehouse 
parking scenario.

This GitHub page contains all code used to implement an Anti-Jackknifing controller together with a motion planner based 
on a Hybrid A* path finding algorithm with a Trajectory optimization algorithm.

As a basis for this project the code from Erik Nordeus has been used: https://github.com/Habrador/Self-driving-vehicle.
Big improvements have been made for the scenario with a truck and trailer as this was a TODO item for Nordeus. New 
heuristic costs have been developed to increase the performance of the path finding algorithm. 

Videos of these parts can be seen here:

[![Link to youtube video](https://img.youtube.com/vi/L591fS51F4I/0.jpg)](https://www.youtube.com/watch?v=L591fS51F4I)


## How this project works

A scenario has been created with different parking spots. A truck trailer model can be placed in one of these spots.
The Hybrid A* algorithm will find a path between the start position and the end position with the lowest costs. This 
path is found by expanding nodes in forward and backward direction under different steering angles to find new nodes. 
For each node a cost is calculated and the node with the lowest cost is expanded upon. These costs are based on two parts:
1. Cost to go
   - Driven distance forward
   - Driven distance backward
   - Absolute steering wheel angle
   - Change in steering wheel angle
   - Hitch Angle

2. Heuristics
   - Absolute distance between current and end trailer
   - Sideways distance between current and end trailer
   - Forward distance between current and end trailer
   - Angle between the current and end trailer
   - Switching direction of movement

After a path has been found this path is saved and sent to the trajectory optimization code. This algorithm optimizes
the trajectory by making the path smooth, so it can be more easily followed by the controller, and by optimizing a cost
function.

The optimized path is sent back to the truck. The MPC controller will follow this path while making sure no jackkniffing 
occurs.

## FAQ 

* **What software do I need?** To make this project work you need [Unity](https://unity.com/) together with 
[Python](https://www.python.org/). 
* **Where can I find the Hybrid A\* algorithm?**  This implementation can be found under 
Assets/Scripts/Pathfinding/Hybrid A star/HybridAStar.cs. The parameters used can be found in 
Assets/Scripts/Pathfinding/Parameters.cs
* **Where can I find the Trajectory Optimization code?**
* **Where can I find the MPC controller code?** 


TU Delft BEP by:
Cedric Pelsma, Erwin Bus, Kik Kramer, Matthijs Steyerberg, Mitchel Castelyns
