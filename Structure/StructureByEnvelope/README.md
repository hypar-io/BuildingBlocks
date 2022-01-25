<img src="preview.png" width="512">

# Structure

Generates a structural frame from a set of Levels and an Envelope.

|Input Name|Type|Description|
|---|---|---|
|Grid X-Axis Interval|number|Grix interval in the X direction.|
|Grid Y-Axis Interval|number|Grid interval in the Y direction.|
|Slab Edge Offset|number|The offset of the grid lines from the slab edge.|
|Display Grid|boolean|Display the grid on the ground plane?|
|Type of Construction|string|The system used for construction.|
|Column Type|string|The wide flange section shape to use for all columns.|
|Girder Type|string|The wide flange section shape to use for all girders.|
|Beam Type|string|The wide flange section shape to use for all beams.|
|Beam Spacing|number|The spacing of the beams.|
|Create Beams On First Level|boolean|Should beams be created at the lowest level of the structure?|
|Slab Thickness|number|The slab thickness. Control the offset of the structure from a level.|
|Insert Columns At External Edges|boolean|Should columns be created at external locations?|
|Maximum Neighbor Span|number|The maximum allowable span between an off grid vertex location and its closest on-grid neighbor. If the distance between the two is greater than this value, a column will be placed.|


<br>

|Output Name|Type|Description|
|---|---|---|
|Maximum Beam Length|Number|The maximum beam length.|

