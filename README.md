# MIRAGE: Enabling Real-Time Automotive Mediated Reality

Mediated Reality (MR) concepts have applications for all SAE levels. However, due to technical hurdles, these concepts are often never evaluated in real vehicles on the real road. We present MIRAGE, a first approach aimed at simplifying the task of evaluating MR
visualization concepts in-vehicle. We implemented nine visualization concepts covering the entire MR spectrum, from diminished reality (DR) to augmented reality (AR). MIRAGE uses state-of-the-art computational models for detection and segmentation, depth estimation, and inpainting to enable the selective application of MR concepts in real-time, achieving up to 34 FPS on an RTX 4080 Super. We evaluated MIRAGE in an expert user study (N=9). Participants
enjoyed the experience while pointing out technical limitations and identifying use cases and additional parameters relevant to MR. We discuss these results in context with related work and give an outlook on implications for MR regarding ethics and interaction
concepts.

## [MIRAGE Application](./MIRAGE%20Unity/)

Built with Unity URP (2022.3.49f1)


## Demo Quickstart
0. To test in VR, open the `Scenes/Pipeline/VR` scene. To test in desktop mode, open the `Scene/Pipeline/Pipeline` scene as well as the `UI` scene (drag it into the hierarchy to load more than one scene at a time)
1. Download the required onnx models from [here](https://cloudstore.uni-ulm.de/s/rdw3yx4EaLHRf2B) and add them to the Assets folder:
    - `depth_anything_v2_vits_outdoor_dynamic`
    - `migan 512 no clipping`
    - `yolo11s-seg`(other yolo models work too but this is the best quality/performance trade-off)
2. Also download the demo video `Morning4_720p.mp4` demo video
3. In your scene, find the `Models` Object, then for each child assign the correct `model asset`
4. Find the `Camera Input > VideoPlayer` GameObject and assign the demo video. Select "Use Debug Video Input" on the `Camera Input` object 
5. Hit play

__Note__: The VR scene was tested exclusively with the `Oculus Runtime`. The project settings are set accordingly and may require adjustments. They can be found in `Edit > Project Settings > XR Plug-In Development`

## UI Controls
- Keyboard: Mouse or Arrow Keys, Enter/Space, ESC
- Gamepad (XBOX): Left Stick, A Button, B Button

__Note__: When using the desktop scene, the debug UI is disabled by default and can be turned on/off via the editor by activating the game object.

## Models
The `.models` directory contains files needed for exporting models to ONNX if they have been modified by me

### Lama
Dependencies are not working and took a while to get working, this is why here is a modified manual as to how to export it to ONNX
1. Follow the Jupiter Notebook file downloaded from [huggingface](https://huggingface.co/Carve/LaMa-ONNX) for cloning the repository, downloading big-lama and unzipping it (do it manually if unzip doesnt work)
2. When installing dependencies begins, instead use the `lama-requirements.txt` file (used under Python 3.10)
3. add the `lama_to_onnx.py` file to the `lama`directory and execute it.
4. Done!

### MI-GAN
ONNX Export script derived from https://github.com/Picsart-AI-Research/MI-GAN.

Follow the instructions on ONNX support but replace the `create_onnx_pipeline.py` file with the one found in this repo. The script disables various post- and preoprocessing effects.