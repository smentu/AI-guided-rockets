# Training Rockets to Fly Using Reinforcement Learning
The project aims to teach a virtual rocket how to land itself like the SpaceX Falcon 9. Both the rocket simulation and machine learning is done using Unity. The project contains two scenarios: a rocket that tries to hit a target above the launch pad, and a Falcon 9 style lander that attempts to land softly on a small platform below. The gif below shows a successful landing

![Alt Text](Media/landing-movie-3.gif)

There are more videos of the projects on my YouTube channel ([link](https://www.youtube.com/user/SanteriMentu/videos)).

## Installation instructions

In order to get project running in Unity and to be able to train models you need to perform the following steps:
1. Install the Unity ML-Agents toolkit and it's dependencies as explained [here](https://github.com/Unity-Technologies/ml-agents/blob/master/docs/Installation.md)
   1. install Python 3.6 or higher and create an new virtual environment
   1. clone the mlagents repository
   1. install the required Unity and Python packages
1. Clone this repository
1. Copy the directory ML-Agents from the Assets directory of the ML-Agents repo into the Assets directory of the AI-guided-rockets repo
