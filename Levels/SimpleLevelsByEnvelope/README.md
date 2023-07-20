

# Simple Levels By Envelope

Creates Levels and LevelPerimeters from one or more Envelopes, with variable level heights.

|Input Name|Type|Description|
|---|---|---|
|Base Levels|array|Supply a list of level floor-to-floor heights. The last supplied value will be treated as the typical level height and repeated until the top level.|
|Top Level Height|number|Height of the penthouse level.|
|Subgrade Level Height|number|Height of levels below grade.|


<br>

|Output Name|Type|Description|
|---|---|---|
|Level Quantity|Number|Total number of levels.|
|Total Level Area|Number|Total aggregate area of all interior levels, excluding the roof.|
|Subgrade Level Area|Number|Total area of all levels below grade.|
|Above Grade Level Area|Number|Total area of all levels above or at grade, excluding the roof.|


<br>

## Additional Information
