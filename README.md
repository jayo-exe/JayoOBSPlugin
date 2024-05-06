# Jayo's Crowd Control Plugin for VNyan

A VNyan Plugin that allows you to connect to OBS over the OBS WEbsocket API and use VNyan Parameters to control OBS through your VNyan node graphs. Change scenes, enable/disable Sources and Filters, mute and unmute Audio inputs, and more!

# Table of contents
1. [Installation](#installation)
2. [Usage](#usage)
    1. [Connecting to OBS](#connecting-to-obs)
    2. [Controlling OBS](#controlling-obs)
        1. [Control Parameters](#control-parameters)
        2. [Status Parameters](#status-parameters)
3. [Development](#development)

## Installation
1. Grab the ZIP file from the [latest release](https://github.com/jayo-exe/JayoOBSPlugin/releases/latest) of the plugin.
2. Extract the contents of the ZIP file _directly into your VNyan installation folder_.  This will add the plugin files to yor VNyan `Items\Objects` and `Item\Assemblies` folders.
3. Launch VNyan, open your Node Graphs, and add a `Spawn CObject` Node attached to an `Application Start` Node.  Ensure that this node is set to spawn the `JayoOBSPlugin` Object, and that "Destroy after Seconds" is set to `-1`
4. Open the VNyan Settings window, go to the "Misc" section, and ensure that **Allow 3rd Party Mods/Plugins** is enabled. This is required for this plugin  (or any plugin) to function correctly, so if you've already got other plugins installed you can probably skip this step.
5. Save your Node Graph and restart VNyan to allow the plugin and libraries to be loaded
6. One VNyan loads, confirm that a button for the plugin now exists in your Plugins window!

## Usage
### Connecting to OBS
In order for the plugin to interact with your OBS, you'll need to authorize the plugin access the OBS Websocket API.  You can do this by providing the password for your API and asjusting the pre-set IP address and Port if needed. This authorization is saved in the plugin's settings, so you won't need to do this every time!

If you're unsure about whether or not your OBS Websocket API is running or need to find our password, follow these steps in OBS:
1. In the top menu, go to Tools -> Websocket Server Settings
2. Make sure "Enable Websocket Server" is checked off, set or generate a password if needed, and click the "Show Connect Info" button
3. Take note of the Server IP, Port, and Password, and enter these in the relevant fields in the OBS Plugin Window

Once the plugin has been authorized, it'll be able to retrieve status information from OBS as well as control certain actions through VNyan Parameters.  A sample node graph is included which provides various examples!

### Controlling OBS
When the plugin is connected, the plugin will set some "Status Parameters" related to some critial statuses within OBS. It'll also watch for your node graphs to set specially-named "Control Parameters" to make targeted actions within OBS.

#### Control Parameters
These are the Control Parameters that can be set to make somethig happen in OBS:

| Title                 | Parameter Name         | Description of Value                                     | Example Value                |
|-----------------------|------------------------|----------------------------------------------------------|------------------------------|
| Change Scene          | `_xjo_scenetoswitch`   | The name of the Scene to switch over to                  | `MySceneName`                |
| Fire a Hotkey         | `_xjo_hotkeytofire`    | The name of the Hotkey to activate                       | `ObsBrowser.Refresh`         |
| Mute Audio Input      | `_xjo_audiotomute`     | The name of the Audio Input to mute                      | `MyInputName`                |
| Unmute Audio Input    | `_xjo_audiotounmute`   | The name of the Audio Input to unmute                    | `MyInputName`                |
| Set Input Volume      | `_xjo_audiotoset`      | The name and desired volume of the audio input to adjust | `MyInputName;;0.69`          |
| Enable Scene Item     | `_xjo_itemtoenable`    | The name of the Scene and Source of the item to enable   | `MySceneName;;MySourceName`  |
| Disable Scene Item    | `_xjo_itemtodisable`   | The name of the Scene and Source of the item to disable  | `MySceneName;;MySourceName`  |
| Enable Source Filter  | `_xjo_filtertoenable`  | The name of the Source and Filter to enable              | `MySourceName;;MyFilterName` |
| Disable Source Filter | `_xjo_filtertodisable` | The name of the Source and Filter to disable             | `MySourceName;;MyFilterName` |
| Start Virtual Cam     | `_xjo_startvcam`       | set to 1 to enable the OBS virtual camera                | `1`                          |
| Stop Virtual Cam      | `_xjo_stopvcam`        | set to 1 to disable the OBS virtual camera               | `1`                          |
| Start Recording       | `_xjo_startrecord`     | set to 1 to start recording                              | `1`                          |
| Stop Recording        | `_xjo_stoprecord`      | set to 1 to stop recording                               | `1`                          |
| Start Streaming       | `_xjo_startstream`     | set to 1 to start streaming                              | `1`                          |
| Stop Streaming        | `_xjo_stopstream`      | set to 1 to stop streaming                               | `1`                          |

#### Status Parameters
There are the Status Parameters that will automatically get set in response to certain things happening in OBS:

| Title                  | Paramter Name       | Description of Value                                         | Example Value   |
|------------------------|---------------------|--------------------------------------------------------------|-----------------|
| Current Active Scene   | `_xjo_currentscene` | The name of the currently-active Scene in OBS                | `MySampleScene` |
| Virtual Camera State   | `_xjo_vcamactive`   | The active state of the OBS Virtual Camera                   | `0`             |
| Recording State        | `_xjo_recordactive` | The active state of the OBS Recording                        | `1`             |
| Recording Paused State | `_xjo_recordpaused` | Whether or not an active recording state is currently paused | `0`             |
| Stream State           | `_xjo_streamactive` | The active state of the OBS Streaming                        | `1`             |


## Development
(Almost) Everything you'll need to develop a fork of this plugin (or some other plugin based on this one)!  The main VS project contains all of the code for the plugin DLL, and the `dist` folder contains a `unitypackage` that can be dragged into a project to build and modify the UI and export the modified Custom Object.

It's worth noting that per VNyan's requirements, this plugni in built under **Unity 2020.3.40f1** , so you'll need to develop on this version to maintain compatability with VNyan.
You'll also need the [VNyan SDK](https://suvidriel.itch.io/vnyan) imported into your project for it to function properly.
Your Visual C# project will need to mave the paths to all dependencies updated to match their locations on your machine.  Most should point to Unity Engine libraries for the correct Engine version **2020.3.40f1**.
