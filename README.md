# Rockets That Learn to Land Without Exploding
The aim of the project was to have a simulated rocket learn to land itself like the SpaceX Falcon 9. Control of the rocket and the physics simulation was done in Unity 3d, and the machine learning was done using Unity ml-agents. The algorithm used was the implementation of proximal policy optimization (PPO) that comes with ml-agents. The physics model for the rocket is quite rudimentary, and is cobbled together based on a few blog posts about rocket physics.

The project contains three scenarios: a rocket that tries to hit a target above the launch pad, a rocket that tries to manoeuvre itself inside a target volume and stay inside it, and a Falcon 9 style lander that attempts to land softly on a small platform below. In the end I managed to achieve fairly good results in the lander scenario by using a training environement that replaces the landing pad with a target volume. The gif below shows a successful landing.

![Alt Text](Media/landing-movie-3.gif)

There are more videos of the project on my YouTube channel ([link](https://www.youtube.com/user/SanteriMentu/videos)).

## Installation instructions

The project has been tested on the following:
* Unity 2020.1.1f1
* Python 3.7.7
* Tensorflow 2.3.0
* mlagents 0.19.0 (Python package)
* ML Agents 1.3.0 preview (Unity package)

In order to get project running in Unity and to be able to train models, you need to perform the following steps:
1. Install the Unity ML-Agents toolkit and it's dependencies as explained [here](https://github.com/Unity-Technologies/ml-agents/blob/master/docs/Installation.md)
   1. install Python 3.6 or higher and create an new virtual environment
   1. clone the mlagents repository
   1. install the required Unity and Python packages
1. Clone this repository
1. Copy the folder "ml-agents" from the Assets directory of the Unity ml-agents repo into the Assets directory of the AI-guided-rockets repo.

## How to train a model
The repo contains trained models for all three scenarios, but if you wish to train one with a different configuration, or make changes to the rocket, then you should do the following

Steps to train a new model:
1. Open any of the three training scenarios in Unity
1. If using a Python virtual environment (recommended), activate the environment that has mlagents and the other required packages installed
1. In the command prompt write `mlagents-learn <location of the config file> --run-id <name of the run>`
1. Press the play button in the Unity editor when prompted
1. If you wish to track the training, use Tensorboard and define the log directory
