# Scaled Collider System

## About

When playing VRChat, your player's physical collider is always the same size regardless of your avatar's size or the scale it's set to. While this helps maintain a consistent user experience, it comes at the cost of immersion, and limits the kinds of worlds creators can make. Want to experience an Alice in Wonderland type world where you might get scaled down and able to run into a mouse hole or under a bed, or scaled up and able to step over buildings? While there are some present solutions such as selectively disabling colliders (tedious), scaling up or down the world (requires all player at the same scale or else positional desync occurs), or the use of stations (complex and with their own set of issues).

Given the options, I decided to add another to the mix that doesn't use any stations, works independently on each user without requiring synchronization, and works in a simple drop-in prefab capacity. Simply install the package, include the prefab in your hierarchy, specify a parent world object, configure any other values, and it should work.

The result is the scale set on the player should match their collisions with the world. Optional features also included are the ability to scale the player movement/physics to their size, scale their volume, control manual scaling and min/max sizes, enabling/disabling the system altogether, switching out the scalable world parent, etc.

## Installation

[Add to VRChat Creator Companion](https://prismic247.github.io/ScaledColliderSystem/)

## Setup

1. Install the package above and add the `Scaled Collider System` prefab to your project, which can be found under `Prismic247.ScaledColliders`.
2. Define the `World Parent` object on the prefeb. This is a game object which contains the world colliders you want scaled. In theory you could have your entire world under this and it should work for most cases, but in practice and for performance reasons, try to limit it to objects with colliders that the players will interact with and move around. An enclosed room can likely exclude the floor or ceiling for example, there's no need to scale video players and canvases, etc.
3. Define the other parameters as you see fit. You can customize Each element has a tooltip and should be fairly explanatory for what they do.
4. (Optional) If `Manual Scaling Allowed` and/or `Enable Scaled Movement` is enabled, it's recommended that you disable the Udon Behavior on the `VRCWorld` that controls avatar scaling and player movement respectively, as these should override those anyways.
5. (Optional) `Show Collider Ghosts` allows for debugging the visible colliders to see how things work/line up. A material is provided, but you can always include your own as well.

