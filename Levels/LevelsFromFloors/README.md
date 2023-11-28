

# Levels from Floors

Construct Levels in between Floors. Useful when importing or manually drawing floors.

|Input Name|Type|Description|
|---|---|---|
|Construct Volume at top level|boolean|Whether or not to construct a level volume at the topmost level of any stack of floors|
|Default Level Height|number|The distance to the next highest floor will typically determine level height, but if no next floor is present, this value will govern the height.|
|Floor Merge Tolerance|number|This number is used to determine if two floors are close enough to be merged into one level. If 0, no merging will occur. This is useful in cases of Revit export, where a given "floor" might be modeled as separate floor elements.|


<br>

|Output Name|Type|Description|
|---|---|---|


<br>

## Additional Information