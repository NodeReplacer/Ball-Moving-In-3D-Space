# Moving Ball With Complex Gravity
A variety of scenes following about moving in 3D space.

Controls for the ball:

WASD is movement. Relative to the camera.

IJKL control the camera movement.

Space is jump. Underwater space is ascend

X is descend while underwater.

F button is climb.

Each scene is a testing zone for a variety of features. There is no one scene that puts them all together.

There are a lot of scenes and they are listed below, but they're there mostly to remind me what I did. It would do me no good to forget.


AccelZonesAndWhatnot - This creates a variety of zones that engage in behaviours. There are varieties in the behaviour but the first one was to change the acceleration of the ball in the zone. At a strong enough acceleration it becomes a bounce pad. 

Climbing is self explanatory, but the orange blocks indicate a "no-climbing" object

InsideBox is just climbing around inside a box. It was used to ensure the ceiling couldn't be "climbed".

InsideBoxMultipleGravities uses multiple gravity sources to let the ball walk along the inside of the box

InsideBoxOneGravBox involves using a single gravity source to do the same as above. The way it works is the same as a sphere's gravitiy but inverted with its shape changed.

Layered Planets has two spheres one inside the other. The player can jump from the inside ball onto the gravity of the outer ball and back. It's easier seen than explained.

MainScene was the very first scene and is utterly obsolete. It was of a time when moving was just setting the velocity to whatever instead of working with acceleration.

MovingPlatforms was made to make sure that the ball moved properly on platforms (because I messed with friction)

OrbitCamera was where the player following camera was made, whether it collides with all objects or only some, and how the player moves relative to the camera.

Slopes is very basic and very 2D. I needed to figure out what counted as "ground" for jumping because a normal vector of 1 relative to the ground is too extreme.

Sphere Gravity involves the gravity of multiple rounded objects. You can jump from one sphere to the other and get captured by the gravity.

Stairs used layer masks to ignore the stairs and instead of use the ramp hidden inside of it for movement instead of hitting every single step on the way up.

Water involves creating zones that simulated water behaviour by messing with gravity.
