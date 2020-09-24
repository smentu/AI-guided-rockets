# Rockets Learning to Fly
The aim of the projects was to have a simulated rocket learn to land itself like the SpaceX Falcon 9. Both the rocket simulation and machine learning is done using Unity. The project contains three scenarios: a rocket that tries to hit a target above the launch pad, a rocket that tries to maneuvre itself inside a target volume and stay inside it, and a Falcon 9 style lander that attempts to land softly on a small platform below. In the end I managed to achieve fairly good results using a training scenario that replaces the landing pad with a target volume. The gif below shows a successful landing.

![Alt Text](Media/landing-movie-3.gif)

There are more videos of the projects on my YouTube channel ([link](https://www.youtube.com/user/SanteriMentu/videos)).

## Installation instructions

The project has been tested on the following:
* Unity 2020.1.1f1
* Python 3.7.7
* Tensorflow 2.3.0
* mlagents 0.19.0 (Python package)
* ML Agents 1.3.0 preview (Unity package)

In order to get project running in Unity and to be able to train models you need to perform the following steps:
1. Install the Unity ML-Agents toolkit and it's dependencies as explained [here](https://github.com/Unity-Technologies/ml-agents/blob/master/docs/Installation.md)
   1. install Python 3.6 or higher and create an new virtual environment
   1. clone the mlagents repository
   1. install the required Unity and Python packages
1. Clone this repository
1. Copy the folder "ml-agents" from the Assets directory of the Unity ml-agents repo into the Assets directory of the AI-guided-rockets repo.

In order to train a model do the following:
1. Open any of the three training scenarios in Unity
1. In the command prompt activate the Python environment with mlagents and the other required packages
1. In the command prompt write `mlagents-learn <location of the config file> --run-id <name of the run>`
1. Press the play button in the Unity editor when prompted
1. If you wish to track the training, use Tensorboard and define the log directory
