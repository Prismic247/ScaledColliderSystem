# Scaled Collider System

Prefab for VRChat worlds to allow for player interactions with world colliders that are scaled relative to your avatar size. PC and Quest compatible. Future support planned for height specific scaling when crouching/going prone.

## About

When playing VRChat, your player's physical collider is always the same size regardless of your avatar's size or the scale it's set to. While this helps maintain a consistent user experience, it comes at the cost of immersion, and limits the kinds of worlds creators can make. Want to experience an Alice in Wonderland type world where you might get scaled down and able to run into a mouse hole or under a bed, or scaled up and able to step over buildings? While there are some present solutions such as selectively disabling colliders (tedious), scaling up or down the world (requires all player at the same scale or else positional desync occurs), or the use of stations (complex and with their own set of issues).

Given the options, I decided to add another to the mix that doesn't use any stations, works independently on each user without requiring synchronization, and works in a simple drop-in prefab capacity. Simply install the package, include the prefab in your hierarchy, specify a parent world object, configure any other values, and it should work.

The result is the scale set on the player should match their collisions with the world. Optional features also included are the ability to scale the player movement/physics to their size, scale their volume, control manual scaling and min/max sizes, enabling/disabling the system altogether, switching out the scalable world parent, etc.

## Demo

You can see the system in action in the world [Scalable Avatar Collider Demo](https://vrchat.com/home/world/wrld_5f61ec26-37fc-491a-84af-14f4cbafbba5)

## Installation

[Add to VRChat Creator Companion](https://prismic247.github.io/ScaledColliderSystem/)

Alternatively, go to [Releases](https://github.com/Prismic247/ScaledColliderSystem/releases) on the right and download the latest the zip or unitypackage.

## Setup

1. Install the package above into your VRChat Unity world project and add the `Scaled Collider System` prefab to your project hierarchy. The prefab can be found under `Packages/com.prismic247.scaledcolliders`.
2. Define the `World Parent` object on the prefab. This is a game object which contains the world colliders you want scaled. **DO NOT** include the world descriptor, scripts, etc, since this can cause some performance issues such as frame drops, logic running twice, etc. Ensure any colliders you intend to use aren't set to Static. In practice and for performance reasons, try to limit it to objects with colliders that the players will interact with and move around. An enclosed room can likely exclude the floor or ceiling for example, there's no need to scale video players and canvases, etc.
3. Define the other parameters as fits for your use case, such as default eye height, whether players can scale themselves and within what bounds, whether or not to scale player speed alongside size, to scale voice/audio, etc. Each element has a tooltip, and should be fairly self explanatory for what they do.

#### Optional Steps

- If `Manual Scaling Allowed` and/or `Enable Scaled Movement` is enabled, it's recommended that you disable the Udon Behavior on the `VRCWorld` that controls avatar scaling and player movement respectively, as these should override those anyways.
- Enabling `Show Collider Ghosts` allows for debugging the invisible colliders to see how things work/line up. A `ColliderGhost` material is provided, but you can always use your own as well.
- You can control and get data from the system by including `using Prismic247.ScaledColliders;` in your own Udon# script, creating a `public ScaledColliderSystem scaledColliderSystem;` on your UdonSharpBehavior, and setting the value in the inspector to the prefab. Alongside the properties, see the **Udon# Public API** section below for other functions you can utilize in your own scripts.

## Known Issues / Important Notes

- This has been built for and tested in Unity 2022. Whether or not it works for Unity 2019 I do not know.
- The system works by creating a clone of the `World Parent` and its children, stripping out the unnecessary elements, and scaling and repositioning the clone inversely to the player. The original world parent has its colliders disabled for local player collision as well. This is how the system functions at its core, but it's not without its issues:
	- Movement on/against an effectively moving surface isn't perfect, and can lead to some jittery-ness, especially at small scales and near walls.
	- Scaling small also means the world is scaled up very large, which can lead to some floating point movemnet areas, especially far from the world origin.
	- Held objects can, with a little effort, be moved through surfaces.
- The TerrainCollider is not supported, due to limitations in Udon's access to their workings. If ever this is fixed I'll try to add support for it, but as is it's not a major issue, since terain has limited slopes and no overhangs anyways, so scenarios where scaling them would matter are rare.
- An avatar's default eye height doesn't always correlate to the default VRChat player collider height the same way. Thus it is possible for two avatars with different eye heights scaled to the exact same value to have different collidable sizes. So be sure to test world features with different avatars.
- Toggled colliders on the `World Parent` (such as doors, removed walls, etc) won't work as normally intended since the clone doesn't have any connection to the original, meaning something like a door that disables to let you through won't have an effect, so try to keep toggles colliders out of the world parent hierarchy if you can help it. One workaround is to reinitialize the scaled colliders after the collider is toggled, such that a new copy of the world parent is created. This can be done either by toggling the `ToggleScaledColliders(state)` function off and on, or by calling `InitializeScaledColliders()` to reset it.
- Doesn't work with static colliders, since it has to be movable and scalable. This is the intended functionality of static colliders, so not really an issue, just important to keep in mind when setting up your world parent and the colliders on its children.

## Udon# Public API

Alongside the configurable properties, the following functions are made available to further control the behavior.

### float GetLocalPlayerScale()
Returns the local player's scale relative to the base eye height. Get the inverse of this value for the local world scale (1 / GetLocalPlayerScale()).

### float GetPlayerScale(VRCPlayerApi player)
Returns a given player's scale relative to the base eye height. Get the inverse of this value for the player's world scale (1 / GetPlayerScale(player)).

### bool ToggleScaledColliders(bool state)
Sets the `enabledScaledColliders` state, and returns the state afterwards. Certain conditions such as a missing `worldParent` may cause this to reject attempts at enabling, hence the return value.

### bool ToggleColliderGhosts(bool state)
Sets the `showColliderGhosts` state, and returns the state afterwards. Enabling this without having a `ghostMaterial` set will simple not render the objects, and it will also not work when `enabledScaledColliders` is disabled.

### void InitializeScaledColliders(GameObject newWorldParent = null)
This allows you to swap out the `worldParent` for a new one, or to reset the current one if the parameter is null. The old parent will automatically be restored to its original state.

